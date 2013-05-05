using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TiledLib;
using Microsoft.Xna.Framework;
using SwordsArt.Engine;
using SwordsArt.Interface;
using SwordsArt.Objects;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;
using SwordsArt.Helpers;

namespace SwordsArt.Rooms
{
    /// <summary>
    ///     A room of the game.
    ///     At any point, all the action is taking place in one room.
    ///     Rooms are not revisited.
    /// </summary>
    public class Room : IController
    {
        #region Constants

        /* Map parsing */
        private const string TILE_LAYER_NAME = "Tiles";

        private const string PLAYER_OBJECT_NAME = "Player";
        private const string GENERATOR_OBJECT_NAME = "Generator";
        private const string PORTAL_OBJECT_NAME = "Portal";
        private const string SECTION_OBJECT_NAME = "Section";

        private const string NEAR = "Close";
        private const float NEAR_ZOOM_LEVEL = 2f;
        private const string MEDIUM = "Medium";
        private const float MEDIUM_ZOOM_LEVEL = 1.2f;
        private const string FAR = "Far";
        private const float FAR_ZOOM_LEVEL = 0.9f;
        private const string VERY_FAR = "VeryFar";
        private const float VERY_FAR_ZOOM_LEVEL = 0.6f;

        private const float DEFAULT_ZOOM = FAR_ZOOM_LEVEL;

        private const string WATER = "Water";

        private const string CENTER = "Center";

        private const string COLOR_PROPERTY_NAME = "Color";
        private const string DIRECTION_PROPERTY_NAME = "Direction";
        private const string INTERVAL_PROPERTY_NAME = "Interval";
        private const string SPEED_PROPERTY_NAME = "Speed";
        private const string ZOOM_PROPERTY_NAME = "Zoom";

        private const float DEFAULT_GRAVITY = 0.75f;

        private const float DEFAULT_GENERATOR_VELOCITY = 5;
        private const int DEFAULT_GENERATOR_INTERVAL = 45;

        // can be used to determine splatter proportions based on object speed
        private const float SPLATTER_SIZE_FACTOR = 0.1f;

        // limit the amount of blobs in the room (or maybe the section of the room) for sanity
        private const int MAX_NUM_BLOBS = 200;

        // where the toolbar should be located (in screen coordinates)
        private const int TOOLBAR_X_OFFSET = 50;
        private const int TOOLBAR_Y_OFFSET = 10;

        private const int PADDING = 20;
        private const int TOOLBAR_ICONSIZE_X = 50;
        private const int TOOLBAR_ICONSIZE_Y = 50;

        // where the minimap should be located (in screen coordinates)
        private const int MINIMAP_X_OFFSET = 125 + TOOLBAR_X_OFFSET + PADDING;
        private const int MINIMAP_Y_OFFSET = TOOLBAR_Y_OFFSET;

        // toolbar indices
        private int NAVIGATION = 0;
        private int UNDO = 1;

        #endregion

        #region Members

        /* Actual room information */
        public readonly float Gravity;
        private readonly Background background;

        private readonly int height;
        public int Height
        {
            get { return height; }
        }
        private readonly int width;
        public int Width
        {
            get { return width; }
        }

        private InkMap inkMap;
        private readonly Map map;
        public Map Map
        {
            get { return map; }
        }
        private Tile[,] tiles;
        private List<GameObject> toAdd = new List<GameObject>();

        private List<GameObject> toRemove = new List<GameObject>();
        public bool MenuRequested;
        private Camera camera;
        public bool ShouldPlayMusic = false;
        
        private Color color;
        public Color Color
        {
            get { return color; }
        }

        private Section curSection = null;
        private List<Dialogue> dialogues = new List<Dialogue>();
        private bool finished;

        private Player player;
        private List<Section> sections = new List<Section>();
        private Vector2 tilesize;
        public Vector2 Tilesize
        {
            get { return tilesize; }
        }

        private bool firstDraw = true;
        private bool failed;
        private bool miniMapIsVisible;
        private const string SPIKE_OBJECT_NAME = "Spikes";
        private const string WAVE_OBJECT_NAME = "Wave";
        private List<Splatter> splatterBuffer = new List<Splatter>();
        private List<Target> targets = new List<Target>();

        // used for collisions to shove some things just outside of walls
        private float Epsilon
        {
            get { return 1f; }
        }

        private Vector2 StageBounds
        {
            get { return new Vector2(width - Epsilon/4, height - Epsilon/2); }
        }

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="Room" /> class.
        /// </summary>
        /// <param name="color">The color of the map.</param>
        /// <param name="map">The map this room is based on.</param>
        /// <param name="gravity">The gravity of the room.</param>
        public Room(Color color, Map map, GraphicsDevice device, float gravity = DEFAULT_GRAVITY)
        {
            this.color = color;
            Gravity = gravity;
            this.map = map;
            miniMapIsVisible = GameEngine.ShouldShowMinimap;

            width = map.WidthInPixels;
            height = map.HeightInPixels;
            background = new Background(width, height, Color.DarkGreen);

            /* Step 1: Load tiles from map. */
            tilesize = new Vector2(map.TileWidth, map.TileHeight);
            Debug.Assert(Math.Abs(map.WidthInPixels % tilesize.X) < float.Epsilon,
                         "Map doesn't divide evenly into rows of tiles.");
            Debug.Assert(Math.Abs(map.HeightInPixels % tilesize.Y) < float.Epsilon,
                         "Map doesn't divide evenly into columns of tiles.");

            tiles = new Tile[map.HeightInTiles, map.WidthInTiles];
            TileGrid grid = ((TileLayer)map.GetLayer(TILE_LAYER_NAME)).Tiles;
            for (int y = 0; y < map.HeightInTiles; y++)
                for (int x = 0; x < map.WidthInTiles; x++)
                {
                    TiledLib.Tile tile = grid[x, y];
                    tiles[y, x] = new Tile(new Vector2(x * tilesize.X, y * tilesize.Y), tilesize, tile, Color.White);
                }

            /* Step 2: Load objects from map. */

            /* Player */
            MapObject playerObj = map.FindObject((layer, obj) => obj.Name == PLAYER_OBJECT_NAME);
            Add(new Player(new Vector2(playerObj.Bounds.X, playerObj.Bounds.Y), TimeSpan.FromSeconds(5)));

            /* Targets */
            IEnumerable<MapObject> targetObjs = map.FindObjects((layer, obj) => obj.Type == "Target");
            foreach (MapObject targetObj in targetObjs)
            {
                Vector2 targetPos = new Vector2(targetObj.Bounds.Left, targetObj.Bounds.Top);
                targets.Add(new Target(targetPos, Color.Blue));
            }

            /* Sections */
            IEnumerable<MapObject> sectionObjs = map.FindObjects((layer, obj) => obj.Type == SECTION_OBJECT_NAME);
            foreach (MapObject sectionObj in sectionObjs)
            {
                Property zoomProperty;
                float zoomLevel = -1;
                bool centered = false;

                if (sectionObj.Properties.TryGetValue(ZOOM_PROPERTY_NAME, out zoomProperty))
                    switch (zoomProperty.RawValue)
                    {
                        case NEAR:
                            zoomLevel = NEAR_ZOOM_LEVEL;
                            break;
                        case MEDIUM:
                            zoomLevel = MEDIUM_ZOOM_LEVEL;
                            break;
                        case FAR:
                            zoomLevel = FAR_ZOOM_LEVEL;
                            break;
                        case VERY_FAR:
                            zoomLevel = VERY_FAR_ZOOM_LEVEL;
                            break;
                        case CENTER:
                            centered = true;
                            break;
                        default:
                            throw new InvalidOperationException("Invalid zoom level.");
                    }

                if (centered)
                    sections.Add(new Section(this, sectionObj.Bounds, centered));
                else
                    sections.Add(new Section(this, sectionObj.Bounds, zoomLevel));
            }

            inkMap = new InkMap(device, width, height);

            GameEngine.FadeIn(FadeSpeed.Fast);
        }

        /// <summary>
        ///     Whether or not the room is completed.
        /// </summary>
        public bool Finished
        {
            get { return finished; }
        }

        /// <summary>
        ///     Finish(exit) the room.
        /// </summary>
        public void Finish()
        {
            if (Failed)
                GameEngine.FadeOut(Color.Black, FadeSpeed.Fast);
            else
            {
                GameEngine.FadeOut(Color.White, FadeSpeed.Fast);
            }
            finished = true;
        }

        /// <summary>
        ///     Updates the room.
        /// </summary>
        /// <param name="gameTime">The game time.</param>
        public virtual void Update(GameTime gameTime)
        {
            if (ShouldPlayMusic)
            {
                // TODO: Play music
                ShouldPlayMusic = false;
            }
            // Add and remove any buffered objects.
            AddAllBuffered();
            RemoveAllBuffered();

            background.Update(gameTime);

            if (player == null)
                Fail();

            if (!Failed)
            {
                // update all objects in the room
                player.Update(this, gameTime);

                foreach (Target target in targets)
                    target.Update(this, gameTime);

                // handle any collisions with the player
                BBox collision;

                // check to see if we entered a new section of the room
                Section newSection = GetDeepestSection(player);
                if (GetDeepestSection(player) != curSection)
                    ChangeSection(newSection);

                

                // now that we've handled all those objects, update the camera to track whatever it wants to track
                camera.Update(this, gameTime);
            }
        }

        private void Fail()
        {
            GameEngine.FadeOut(Color.Black, FadeSpeed.Fast);
            Failed = true;
            finished = true;
        }

        public bool Failed
        {
            get { return failed; }
            set { failed = value; }
        }

        public Player Player
        {
            get { return player; }
        }

        private void ChangeSection(Section newSection)
        {
            if (newSection == null)
            {
                camera.ChangeTarget(player);
                camera.ZoomTo(DEFAULT_ZOOM);
            }
            else
            {

                if (newSection.Centered)
                {
                    camera.ChangeTarget(newSection.Center);
                    // TODO: Use section size to determine the camera zoom
                    camera.ZoomTo(DEFAULT_ZOOM);
                }
                else
                {
                    camera.ChangeTarget(player);
                    camera.ZoomTo(newSection.ZoomLevel);
                }
            }
            curSection = newSection;
        }

        /// <summary>
        ///     Draws the room.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch.</param>
        public virtual void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.GraphicsDevice.BlendState = BlendState.AlphaBlend;
            spriteBatch.GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

            if (player != null && InBounds(player.Position))
            {

                RenderTarget2D curRenderTarget = inkMap.GetMap(player.Position);
                spriteBatch.GraphicsDevice.SetRenderTarget(curRenderTarget);
                if (!inkMap.HasBeenDrawn(player.Position))
                {
                    spriteBatch.GraphicsDevice.Clear(Color.Transparent);
                    inkMap.SetDrawn(player.Position);
                }

                // flushing all the ink splatters onto the inkmap
                spriteBatch.Begin();

                /*foreach (Splatter splatter in splatterBuffer)
                {
                    RenderTarget2D newTarget;
                    if ((newTarget = inkMap.GetMap(splatter.Position)) != curRenderTarget && newTarget != null)
                    {
                        curRenderTarget = newTarget;
                        spriteBatch.GraphicsDevice.SetRenderTarget(newTarget);
                        if (!inkMap.HasBeenDrawn(splatter.Position))
                        {
                            spriteBatch.GraphicsDevice.Clear(Color.Transparent);
                            inkMap.SetDrawn(splatter.Position);
                        }
                    }

                    splatter.Draw(spriteBatch);
                }*/

                splatterBuffer.Clear();

                spriteBatch.End();
            }

            spriteBatch.GraphicsDevice.SetRenderTarget(null);
            spriteBatch.GraphicsDevice.Clear(Color.Transparent);

            // set up everything else to be drawn relative to the camera
            spriteBatch.Begin(SpriteSortMode.Deferred,
                              BlendState.AlphaBlend,
                              null,
                              null,
                              null,
                              null,
                              camera.GetTransformation(spriteBatch.GraphicsDevice));

            background.Draw(spriteBatch);

            map.Draw(spriteBatch);

            foreach (Target target in targets)
            {
                target.Draw(spriteBatch);
            }
            if (!Failed) player.Draw(spriteBatch);

            spriteBatch.End();
        }



        public void Add(GameObject obj)
        {
            toAdd.Add(obj);
        }

        private void AddAllBuffered()
        {
            foreach (GameObject obj in toAdd)
            {
                if (obj is Player)
                {
                    player = (Player)obj;
                    Section curSection = GetDeepestSection(player);
                    float zoomLevel = curSection == null ? DEFAULT_ZOOM : curSection.ZoomLevel;
                    camera = new Camera(player, width, height, zoomLevel);
                }
            }

            toAdd.Clear();
        }

        public void Remove(GameObject obj)
        {
            toRemove.Add(obj);
        }

        private void RemoveAllBuffered()
        {
            foreach (GameObject obj in toRemove)
            {
                if (obj is Player)
                    player = null;

                if (obj is Target)
                {
                    targets.Remove((Target) obj);
                }
            }

            toRemove.Clear();
        }

        /// <summary>
        ///     Gets the minimum position of the given object.
        /// </summary>
        /// <param name="position">The position of the object.</param>
        /// <param name="size">The size of the object.</param>
        /// <returns>Vector2.</returns>
        public virtual Vector2 GetMinPosition(Vector2 position, Vector2 size, Color color, bool collidable = true)
        {
            Vector2 tilespan = GetTilespan(position, size);

            // the current tile index being examined
            Vector2 curTileIndex = GetTileIndexByPixel(position);
            bool done;
            // calculate x-bound
            float boundX = 15;
            if (collidable)
            {
                done = false;
                for (var x = (int)curTileIndex.X; x >= 0; x--)
                {
                    for (var y = (int)curTileIndex.Y; y <= curTileIndex.Y + tilespan.Y; y++)
                        if (tiles[y, x].Type == TileType.Solid && !(tiles[y, x].Color != Color.White && tiles[y, x].Color == color))
                        {
                            boundX = x * tilesize.X + tilesize.X + Epsilon;
                            done = true;
                            break;
                        }
                    if (done)
                        break;
                }
            }

            // calculate y-bound
            float boundY = 15;
            if (collidable)
            {
                done = false;
                for (var y = (int)curTileIndex.Y; y >= 0; y--)
                {
                    for (var x = (int)curTileIndex.X; x <= curTileIndex.X + tilespan.X; x++)
                        if (tiles[y, x].Type == TileType.Solid && !(tiles[y, x].Color != Color.White && tiles[y, x].Color == color))
                        {
                            boundY = y * tilesize.Y + tilesize.Y + Epsilon;
                            done = true;
                            break;
                        }
                    if (done)
                        break;
                }
            }

            return new Vector2(boundX, boundY);
        }

        public virtual Vector2 GetMaxPosition(Vector2 position, Vector2 size, Color color, bool collidable = true)
        {
            Vector2 tilespan = GetTilespan(position, size);

            Vector2 curTileIndex = GetTileIndexByPixel(position);
            bool done;

            // calculate x-bound
            float boundX = StageBounds.X - size.X - 10;
            if (collidable)
            {
                done = false;
                for (var x = (int)curTileIndex.X; x < map.WidthInTiles; x++)
                {
                    for (var y = (int)curTileIndex.Y; y <= curTileIndex.Y + tilespan.Y; y++)
                        if (tiles[y, x].Type == TileType.Solid && !(tiles[y, x].Color != Color.White && tiles[y, x].Color == color))
                        {
                            boundX = x * tilesize.X - size.X - Epsilon;
                            done = true;
                            break;
                        }
                    if (done)
                        break;
                }
            }

            // calculate y-bound
            float boundY = StageBounds.Y - size.Y - 10;
            done = false;
            if (collidable)
                for (var y = (int)curTileIndex.Y; y < map.HeightInTiles; y++)
                {
                    for (var x = (int)curTileIndex.X; x <= curTileIndex.X + tilespan.X; x++)
                        if ((tiles[y, x].Type == TileType.Solid ||
                            (tiles[y, x].Type == TileType.TopSolid && Math.Abs(y - curTileIndex.Y) > tilespan.Y))
                            && !(tiles[y, x].Color != Color.White && tiles[y, x].Color == color))
                        {
                            boundY = y * tilesize.Y - size.Y - Epsilon;
                            done = true;
                            break;
                        }
                    if (done)
                        break;
                }

            return new Vector2(boundX, boundY);
        }

        /// <summary>
        ///     Gets the tilespan of a object.
        /// </summary>
        /// <param name="position">The position of the object.</param>
        /// <param name="size">The size of the object.</param>
        /// <returns>How many tiles the object spans on the x-axis and y-axis(not including the one the position is on).</returns>
        private Vector2 GetTilespan(Vector2 position, Vector2 size)
        {
            Tile startTile = GetTileByPixel(Vector2.Clamp(position, Vector2.Zero, StageBounds));
            Tile endTile = GetTileByPixel(Vector2.Clamp(position + size, Vector2.Zero, StageBounds));
            float spanX = (endTile.Position.X - startTile.Position.X) / tilesize.X;
            float spanY = (endTile.Position.Y - startTile.Position.Y) / tilesize.Y;
            return new Vector2(spanX, spanY);
        }

        public bool WallAt(Vector2 pixel)
        {
            return GetTileByPixel(pixel).Type == TileType.Solid;
        }

        public bool WallAtIndex(int col, int row)
        {
            Debug.Assert(0 <= col && col < map.HeightInTiles, "Column out of range.");
            Debug.Assert(0 <= row && row < map.WidthInTiles, "Row out of range.");
            return GetTileByPixel(new Vector2(row * tilesize.X, col * tilesize.Y)).Type == TileType.Solid;
        }

        /// <summary>
        ///     Gets the tile corresponding to the given pixel.
        /// </summary>
        /// <param name="position">The pixel position.</param>
        /// <returns>The tile at the given position.</returns>
        public Tile GetTileByPixel(Vector2 position)
        {
            if (InBounds(position))
                return tiles[(int)(position.Y / tilesize.Y), (int)(position.X / tilesize.X)];

            return null;
        }

        public Vector2 GetTileIndexByPixel(Vector2 position)
        {
            if (InBounds(position))
                return new Vector2((int)(position.X / tilesize.X), (int)(position.Y / tilesize.Y));

            int correctionX = (int)(position.X / tilesize.X), correctionY = (int)(position.Y / tilesize.Y);
            if (position.X < 0)
                correctionX = 0;
            else if (position.X > StageBounds.X)
                correctionX = (int)(StageBounds.X / tilesize.X);

            if (position.Y < 0)
                correctionY = 0;
            else if (position.Y > StageBounds.Y)
                correctionY = (int)(StageBounds.Y / tilesize.Y);

            return new Vector2(correctionX, correctionY);
        }

        private bool InBounds(Vector2 position)
        {
            return position == Vector2.Clamp(position, Vector2.Zero, StageBounds);
        }

        /// <summary>
        /// Makes a splatter in the room at the given 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="size"></param>
        /// <param name="splatColor"></param>
        /// <param name="velocity"></param>
        public void Splat(Vector2 position, Vector2 size, Color splatColor, Vector2 velocity)
        {
            Random jitterRandom = new Random();

            // rotate the splatter with the velocity of the angle, but don't flip vertically.
            float rotation = (new Vector2(velocity.X, -Math.Abs(velocity.Y))).ToAngle();

            rotation += (float)jitterRandom.NextDouble() * 0.25f * FloatHelper.RandomSign();
            Vector2 splatSize;

            // only acccomodate for the velocity if it's non-zero. Also, need to take the absolute value to avoid
            // getting negative size values in return (that's invalid behavior).
            if (Math.Abs(velocity.X) > 0)
                splatSize.X = size.X * Math.Abs(velocity.X) * SPLATTER_SIZE_FACTOR;
            else splatSize.X = size.X * SPLATTER_SIZE_FACTOR;

            if (Math.Abs(velocity.Y) > 0)
                splatSize.Y = size.Y * Math.Abs(velocity.Y) * SPLATTER_SIZE_FACTOR;
            else splatSize.Y = size.Y * SPLATTER_SIZE_FACTOR;

            Vector2 splatPos = position;

            if (velocity.X > 0)
                splatPos.X -= size.X;
            if (velocity.Y > 0)
                splatPos.Y += size.Y * (3 / 4);
            // if one of the components is huge but the other is really small, the splatter will look weird, so balance
            // it out.
            splatSize = splatSize.Balance();

            splatSize = splatSize.PushBack(Vector2.Max((float)jitterRandom.NextDouble() * splatSize.LargestComponent() * Vector2.One,
                new Vector2(5, 5)));

            Texture2D splatterTexture = ResourceManager.GetRandomSplatter();
            splatColor.A = 150;
            splatterBuffer.Add(new Splatter(position, splatSize, rotation, splatterTexture, splatColor));
        }

        public Section GetDeepestSection(GameObject obj)
        {
            Section deepestSection = null;
            foreach (Section section in sections)
                if (section.Contains(obj) && (deepestSection == null || section.Area < deepestSection.Area))
                    deepestSection = section;

            return deepestSection;
        }

        public Predicate<GameObject> _obj { get; set; }
    }
}
