using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TiledLib;
using Microsoft.Xna.Framework;

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
        private List<Portal> portals = new List<Portal>();
        public Portal CorrectPortal;
        public List<Portal> Portals
        {
            get { return portals; }
        }
        private List<WaveGenerator> waveGenerators = new List<WaveGenerator>();
        private List<Section> sections = new List<Section>();
        private List<Barrier> barriers = new List<Barrier>();
        private Vector2 tilesize;
        public Vector2 Tilesize
        {
            get { return tilesize; }
        }

        private MiniMap miniMap;

        private Toolbar toolbar;

        private bool firstDraw = true;
        private bool failed;
        private bool miniMapIsVisible;
        private const string SPIKE_OBJECT_NAME = "Spikes";
        private const string WAVE_OBJECT_NAME = "Wave";

        // used for collisions to shove some things just outside of walls
        private float Epsilon
        {
            get { return 0.001f; }
        }

        private Vector2 StageBounds
        {
            get { return new Vector2(width - Epsilon * 2, height - Epsilon); }
        }

        public RoomType Type;

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="Room" /> class.
        /// </summary>
        /// <param name="color">The color of the map.</param>
        /// <param name="map">The map this room is based on.</param>
        /// <param name="gravity">The gravity of the room.</param>
        public Room(RoomType type, Color color, Map map, GraphicsDevice device, float gravity = DEFAULT_GRAVITY)
        {
            this.color = color;
            Gravity = gravity;
            this.map = map;
            Type = type;
            miniMapIsVisible = GameEngine.ShouldShowMinimap;

            width = map.WidthInPixels();
            height = map.HeightInPixels();
            background = new Background(type, width, height);

            /* Step 1: Load tiles from map. */
            tilesize = new Vector2(map.TileWidth, map.TileHeight);
            Debug.Assert(Math.Abs(map.WidthInPixels() % tilesize.X) < float.Epsilon,
                         "Map doesn't divide evenly into rows of tiles.");
            Debug.Assert(Math.Abs(map.HeightInPixels() % tilesize.Y) < float.Epsilon,
                         "Map doesn't divide evenly into columns of tiles.");

            tiles = new Tile[map.Height, map.Width];
            TileGrid grid = ((TileLayer)map.GetLayer(TILE_LAYER_NAME)).Tiles;
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                {
                    TiledLib.Tile tile = grid[x, y];
                    tiles[y, x] = new Tile(new Vector2(x * tilesize.X, y * tilesize.Y), tilesize, tile, Color.White);
                }

            /* Step 2: Load objects from map. */

            /* Player */
            MapObject playerObj = map.FindObject((layer, obj) => obj.Name == PLAYER_OBJECT_NAME);
            Add(new Player(new Vector2(playerObj.Bounds.X, playerObj.Bounds.Y)));

            /* Ink Generators */
            IEnumerable<MapObject> generatorObjs = map.FindObjects((layer, obj) => obj.Type == GENERATOR_OBJECT_NAME);
            foreach (MapObject generatorObj in generatorObjs)
            {
                // get all the properties of the generator from the map object
                Property directionProperty;
                Debug.Assert(generatorObj.Properties.TryGetValue(DIRECTION_PROPERTY_NAME, out directionProperty),
                             "Generator with no direction.");
                Vector2? genDirection = VectorHelper.FromDirectionString(directionProperty.RawValue);
                Debug.Assert(genDirection != null, "Invalid direction name specified.");

                float genX = genDirection.Value.X > 0 ? generatorObj.Bounds.Right : generatorObj.Bounds.Left;
                float genY = genDirection.Value.Y < 0 ? generatorObj.Bounds.Bottom : generatorObj.Bounds.Top;

                bool isWaterGenerator = false;
                Property colorProperty;
                Color? genColor = Color.Transparent;
                Debug.Assert(generatorObj.Properties.TryGetValue(COLOR_PROPERTY_NAME, out colorProperty),
                             "Generator with no color.");
                if (colorProperty.RawValue == WATER)
                    isWaterGenerator = true;
                else
                {
                    genColor = ColorHelper.FromString(colorProperty.RawValue);
                    Debug.Assert(genColor != null, "Invalid generator color was specified.");
                }

                int genInterval = DEFAULT_GENERATOR_INTERVAL;
                Property intervalProperty;
                if (generatorObj.Properties.TryGetValue(INTERVAL_PROPERTY_NAME, out intervalProperty))
                    genInterval = FloatHelper.ParseIntervalString(intervalProperty.RawValue);

                float genSpeed = DEFAULT_GENERATOR_VELOCITY;
                Property speedProperty;
                if (generatorObj.Properties.TryGetValue(SPEED_PROPERTY_NAME, out speedProperty))
                    genSpeed = FloatHelper.ParseSpeedString(speedProperty.RawValue);

                if (!isWaterGenerator)
                {
                    Debug.Assert(genColor != Color.Transparent, "Invalid generator color specified.");
                    Add(new InkGenerator(new Vector2(genX, genY), genDirection.Value, genColor.Value, genInterval,
                                         genSpeed));
                }
                else
                    Add(new WaterGenerator(new Vector2(genX, genY), genDirection.Value, genInterval, genSpeed));
            }

            /* Portals */
            IEnumerable<MapObject> portalObjs = map.FindObjects((layer, obj) => obj.Type == PORTAL_OBJECT_NAME);
            foreach (MapObject portalObj in portalObjs)
            {
                Property colorProperty;
                Debug.Assert(portalObj.Properties.TryGetValue(COLOR_PROPERTY_NAME, out colorProperty), "Portal found with no color.");
                Color? portalColor = ColorHelper.FromString(colorProperty.RawValue);
                Debug.Assert(portalColor != null, "Invalid portal color was specified.");

                bool isCorrectPortal = false;
                Property correctProperty;
                if (portalObj.Properties.TryGetValue("Correct", out correctProperty))
                    isCorrectPortal = true;

                Vector2 portalPos = new Vector2(portalObj.Bounds.X, portalObj.Bounds.Y);
                Vector2 portalSize = new Vector2(portalObj.Bounds.Width, portalObj.Bounds.Height);

                Add(new Portal(Type, portalPos, portalSize, portalColor.Value, isCorrectPortal));
            }

            /* Spikes */
            IEnumerable<MapObject> spikeObjs = map.FindObjects((layer, obj) => obj.Type == SPIKE_OBJECT_NAME);
            foreach (MapObject spikeObj in spikeObjs)
            {
                Color? spikeColor = Color.White;
                Vector2? spikeDirection;

                Property colorProperty;
                if (spikeObj.Properties.TryGetValue(COLOR_PROPERTY_NAME, out colorProperty))
                    spikeColor = ColorHelper.FromString(colorProperty.RawValue);
                Debug.Assert(spikeColor.HasValue, "Invalid spike color specified.");

                Property directionProperty;
                Debug.Assert(spikeObj.Properties.TryGetValue(DIRECTION_PROPERTY_NAME, out directionProperty), "Spikes without direction");
                spikeDirection = VectorHelper.FromDirectionString(directionProperty.RawValue);
                Debug.Assert(spikeDirection.HasValue, "Invalid spike direction specified.");
                bool alignedHorizontally = (spikeDirection.Value.X == 0);
                bool facingLeft = (spikeDirection.Value.X < 0);
                int numSpikes = alignedHorizontally
                    ? (int)(spikeObj.Bounds.Width / (tilesize.X))
                    : (int)(spikeObj.Bounds.Height / (tilesize.Y));

                for (int i = facingLeft ? 1 : 0; i < (facingLeft ? numSpikes : numSpikes - 1); i++)
                {
                    spikes.Add(new Spike(alignedHorizontally
                        ? new Vector2(spikeObj.Bounds.X + i * tilesize.X, spikeObj.Bounds.Y)
                        : new Vector2(spikeObj.Bounds.X, spikeObj.Bounds.Y + i * tilesize.Y),
                        new Vector2(tilesize.X * 2, tilesize.Y), spikeColor.Value, spikeDirection.Value));
                }
            }

            /* Waves */
            IEnumerable<MapObject> waveObjs = map.FindObjects((layer, obj) => obj.Type == WAVE_OBJECT_NAME);
            foreach (MapObject waveObj in waveObjs)
            {
                Property colorProperty;
                Color? waveColor = Color.White;
                bool isWater = false;
                Debug.Assert(waveObj.Properties.TryGetValue(COLOR_PROPERTY_NAME, out colorProperty), "Found a wave with no color.");

                if (colorProperty.RawValue == WATER)
                    isWater = true;
                else
                {
                    waveColor = ColorHelper.FromString(colorProperty.RawValue);
                    Debug.Assert(waveColor.HasValue, "Invalid wave color specified.");
                }

                Property directionProperty;
                Vector2? direction = new Vector2(1, 0);
                if (waveObj.Properties.TryGetValue(DIRECTION_PROPERTY_NAME, out directionProperty))
                {
                    direction = VectorHelper.FromDirectionString(directionProperty.RawValue);
                }

                Add(new WaveGenerator(new Vector2(waveObj.Bounds.X, waveObj.Bounds.Y + (1f / 2f) * waveObj.Bounds.Height),
                    new Vector2(waveObj.Bounds.Width, waveObj.Bounds.Height * 1f / 2f),
                    direction.Value.X < 0, isWater ? Color.LightBlue : waveColor.Value,
                    isWater));
            }

            /* Spouts */
            IEnumerable<MapObject> spoutObjs = map.FindObjects((layer, obj) => obj.Type == "Spout");
            foreach (MapObject spoutObj in spoutObjs)
            {
                bool isWater = false;
                Color? spoutColor;
                Property colorProperty;
                Debug.Assert(spoutObj.Properties.TryGetValue(COLOR_PROPERTY_NAME, out colorProperty), "Spout has no color.");
                if (colorProperty.RawValue == "Water")
                {
                    spoutColor = Color.LightBlue;
                    isWater = true;
                }
                else
                {
                    spoutColor = ColorHelper.FromString(colorProperty.RawValue);
                }
                Debug.Assert(spoutColor.HasValue, "Invalid color specified for spout.");

                Vector2? spoutDirection;
                Property directionProperty;
                Debug.Assert(spoutObj.Properties.TryGetValue(DIRECTION_PROPERTY_NAME, out directionProperty), "Spout has no direction.");
                spoutDirection = VectorHelper.FromDirectionString(directionProperty.RawValue);
                Debug.Assert(spoutDirection.HasValue, "Invalid direction specified for spout.");

                Add(new SpoutGenerator(new Vector2(spoutObj.Bounds.X, spoutObj.Bounds.Y), spoutDirection.Value, spoutColor.Value, isWater));
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

            IEnumerable<MapObject> barrierObjs = Map.FindObjects((layer, obj) => obj.Type == "Barrier");
            foreach (MapObject barrierObj in barrierObjs)
            {
                Property colorProperty;
                Color? barrierColor;
                Debug.Assert(barrierObj.Properties.TryGetValue(COLOR_PROPERTY_NAME, out colorProperty), "No color specified for barrier.");
                barrierColor = ColorHelper.FromString(colorProperty.RawValue);
                Debug.Assert(barrierColor.HasValue, "Invalid color specified for barrier.");

                Property directionProperty;
                Vector2? barrierDirection;
                Debug.Assert(barrierObj.Properties.TryGetValue(DIRECTION_PROPERTY_NAME, out directionProperty), "No direction specified for barrier.");
                barrierDirection = VectorHelper.FromDirectionString(directionProperty.RawValue);
                Debug.Assert(barrierDirection.HasValue, "Invalid direction specified for barrier.");

                Vector2 barrierPos = new Vector2(barrierObj.Bounds.X, barrierObj.Bounds.Y);
                Vector2 barrierSize = new Vector2(barrierObj.Bounds.Width, barrierObj.Bounds.Height);
                Add(new Barrier(barrierPos, barrierSize, barrierDirection.Value, barrierColor.Value));

                Vector2 tilespan = GetTilespan(barrierPos, barrierSize);
                Vector2 startPos = GetTileIndexByPixel(barrierPos);
                for (int y = (int)startPos.Y; y < startPos.Y + tilespan.Y; y++)
                    for (int x = (int)startPos.X; x < startPos.X + tilespan.X; x++)
                    {
                        tiles[y, x] = new Tile(new Vector2(x * tilesize.X, y * tilesize.Y), tilesize, TileType.Solid, barrierColor.Value);
                    }
            }
            inkMap = new InkMap(device, width, height);
            miniMap = new MiniMap(this, new Vector2(device.Viewport.Width - MINIMAP_X_OFFSET, 0 + MINIMAP_Y_OFFSET));

            toolbar = new Toolbar(new Vector2(device.Viewport.Width - TOOLBAR_X_OFFSET, 0 + TOOLBAR_Y_OFFSET),
                new Vector2(TOOLBAR_ICONSIZE_X, TOOLBAR_ICONSIZE_Y), new List<Texture2D>
                    {
                        ResourceManager.GetTexture("Misc_Navigation"),
                        ResourceManager.GetTexture("Misc_Reset")
                    }, Orientation.Vertical);
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
                GameEngine.FadeOut(Color.White, Type == RoomType.Acceptance ? FadeSpeed.Slow : FadeSpeed.Fast);
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
                ResourceManager.PlaySong(GameEngine.GetTypeName(Type));
                ShouldPlayMusic = false;
            }
            // Add and remove any buffered objects.
            AddAllBuffered();
            RemoveAllBuffered();

            background.Update(gameTime);

            if (player == null)
            {
                Fail();
            }

            if (!Failed)
            {
                // Update all objects in the room.
                if (portals.Find((portal) => portal.Symbolizing) == null)
                    player.Update(this, gameTime);
                foreach (Portal portal in portals)
                {
                    portal.Update(this, gameTime);
                    if (portal.Unlocked)
                        Finish();
                }
                foreach (InkGenerator generator in generators)
                    generator.Update(this, gameTime);
                foreach (WaveGenerator generator in waveGenerators)
                    generator.Update(this, gameTime);
                foreach (SpoutGenerator generator in spoutGenerators)
                    generator.Update(this, gameTime);
                foreach (InkBlob blob in blobs)
                    blob.Update(this, gameTime);
                foreach (Wave wave in waves)
                    wave.Update(this, gameTime);
                foreach (Spout spout in spouts)
                    spout.Update(this, gameTime);

                // handle any collisions with the player
                BBox collision;
                foreach (Portal portal in portals)
                    if (portal.IsColliding(player, out collision))
                    {
                        player.CollideWithObject(portal, this, collision);
                        portal.CollideWithObject(player, this, collision);
                    }

                foreach (InkGenerator generator in generators)
                {
                    if (generator.IsColliding(player, out collision))
                    {
                        player.CollideWithObject(generator, this, collision);
                        generator.CollideWithObject(player, this, collision);
                    }
                }
                foreach (InkBlob blob in blobs)
                {
                    if (blob.IsColliding(player, out collision))
                    {
                        player.CollideWithObject(blob, this, collision);
                        blob.CollideWithObject(player, this, collision);
                    }
                }

                foreach (Spike spike in spikes)
                {
                    if (spike.IsColliding(player, out collision))
                    {
                        player.CollideWithObject(spike, this, collision);
                        spike.CollideWithObject(player, this, collision);
                    }
                }

                collision = new BBox(0, 0, 0, 0);
                foreach (Wave wave in waves)
                {
                    if (player.IsColliding(wave, out collision))
                    {
                        player.CollideWithObject(wave, this, collision);
                        wave.CollideWithObject(player, this, collision);
                    }
                }

                foreach (Spout spout in spouts)
                    if (spout.ShouldDamage && player.IsColliding(spout, out collision))
                    {
                        player.CollideWithObject(spout, this, collision);
                        spout.CollideWithObject(player, this, collision);
                    }

                // check to see if we entered a new section of the room
                Section newSection = GetDeepestSection(player);
                if (GetDeepestSection(player) != curSection)
                    ChangeSection(newSection);

                // now that we've handled all those objects, update the camera to track whatever it wants to track
                camera.Update(this, gameTime);

                inkMap.Update();
                miniMap.Update(this, gameTime);
                toolbar.Update(gameTime);

                if (toolbar.IsSelected(NAVIGATION))
                {
                    miniMapIsVisible = !miniMapIsVisible;
                    GameEngine.ShouldShowMinimap = miniMapIsVisible;
                }
                if (toolbar.IsSelected(UNDO))
                {
                    player.Die(this);
                }
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

                foreach (Splatter splatter in splatterBuffer)
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
                }

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
            inkMap.Draw(spriteBatch);

            foreach (InkGenerator generator in generators)
                generator.Draw(spriteBatch);
            foreach (Portal portal in portals)
                portal.Draw(spriteBatch);
            foreach (Barrier barrier in barriers)
                barrier.Draw(spriteBatch);
            if (!Failed) player.Draw(spriteBatch);
            foreach (InkBlob blob in blobs)
                blob.Draw(spriteBatch);
            foreach (Wave wave in waves)
                wave.Draw(spriteBatch);
            foreach (Spout spout in spouts)
                spout.Draw(spriteBatch);
            foreach (Spike spike in spikes)
                spike.Draw(spriteBatch);

            spriteBatch.End();

            // ...and now do the interface (unless we're in the last stage of the game
            if (Type != RoomType.Acceptance)
            {
                spriteBatch.Begin();
                if (miniMapIsVisible)
                    miniMap.Draw(spriteBatch);
                toolbar.Draw(spriteBatch);
                spriteBatch.End();
            }
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
                    miniMap.Mappings.Add(obj, ResourceManager.GetTexture("Player_Icon"));
                }
                else if (obj is Portal)
                {
                    portals.Add((Portal)obj);
                    if (toAdd.FindAll((_obj) => _obj is Portal).Count == 1)
                    {
                        CorrectPortal = (Portal)obj;
                        miniMap.Mappings.Add(obj, ResourceManager.GetTexture("Portal_Icon"));
                    }
                    else if ((toAdd.Find((_obj) => _obj is Portal && ((Portal)_obj).IsCorrect) != null))
                    {
                        if (!((Portal)obj).IsCorrect)
                            miniMap.Mappings.Add(obj, ResourceManager.GetTexture("Portal_Icon"));
                        else
                            CorrectPortal = (Portal)obj;
                    }
                    else
                        throw new InvalidOperationException("Multiple portals but no correct portal specified.");

                }
                else if (obj is InkGenerator)
                {
                    generators.Add((InkGenerator)obj);
                    miniMap.Mappings.Add(obj, ResourceManager.GetTexture("Generator_Icon"));
                }
                else if (obj is InkBlob)
                    blobs.Add((InkBlob)obj);
                else if (obj is WaveGenerator)
                    waveGenerators.Add((WaveGenerator)obj);
                else if (obj is Wave)
                    waves.Add((Wave)obj);
                else if (obj is SpoutGenerator)
                    spoutGenerators.Add((SpoutGenerator)obj);
                else if (obj is Spout)
                    spouts.Add((Spout)obj);
                else if (obj is Barrier)
                    barriers.Add((Barrier)obj);
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
                else if (obj is Portal)
                    portals.Remove((Portal)obj);
                else if (obj is InkGenerator)
                    generators.Remove((InkGenerator)obj);
                else if (obj is InkBlob)
                    blobs.Remove((InkBlob)obj);
                else if (obj is WaveGenerator)
                    waveGenerators.Remove((WaveGenerator)obj);
                else if (obj is Wave)
                    waves.Remove((Wave)obj);
                else if (obj is SpoutGenerator)
                    spoutGenerators.Remove((SpoutGenerator)obj);
                else if (obj is Spout)
                    spouts.Remove((Spout)obj);
                else if (obj is Barrier)
                    barriers.Remove((Barrier)obj);
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
                for (var x = (int)curTileIndex.X; x < map.Width; x++)
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
                for (var y = (int)curTileIndex.Y; y < map.Height; y++)
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
            Debug.Assert(0 <= col && col < map.Height, "Column out of range.");
            Debug.Assert(0 <= row && row < map.Width, "Row out of range.");
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

        public bool CanHaveMoreBlobs()
        {
            return blobs.Count < MAX_NUM_BLOBS;
        }

        public Predicate<GameObject> _obj { get; set; }
    }
}
