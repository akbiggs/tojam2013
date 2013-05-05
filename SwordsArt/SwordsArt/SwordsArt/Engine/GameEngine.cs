using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SwordsArt.Rooms;
using Microsoft.Xna.Framework.Media;
using SwordsArt.Interface;
using SwordsArt.Helpers;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace SwordsArt.Engine
{
    /// <summary>
    /// The main engine controlling all of the game's components.
    /// </summary>
    public class GameEngine : Microsoft.Xna.Framework.Game
    {
        const float FADE_FACTOR = 25;

        public static GraphicsDeviceManager graphics;
        public static SpriteBatch spriteBatch;
        public static bool ShouldShowMinimap = true;

        public static float ScreenWidth
        {
            get { return spriteBatch.GraphicsDevice.Viewport.Width; }
        }

        /// <summary>
        /// All of the rooms in the game, in order.
        /// </summary>
        List<String> roomNames = new List<String>
            {
                "WallJump"
                // TODO: Room names go here.
                //"First",
                //"MultiJump"
            };

        Intro intro;
        TitleScreen titleScreen;
        GameMenu gameMenu;
        Room curRoom;
        Song curSong;
        Credits credits;
        GameState state;

        static Color fade = Color.Transparent;
        private static Color lastFade = Color.Transparent;
        private static Color nextFade = Color.Transparent;
        private static byte fadeSpeed;

        private bool ShouldFade
        {
            get { return fade != nextFade; }
        }

        /// <summary>
        /// The main component in charge of the game at the moment.
        /// </summary>
        IController controller;

        public GameEngine(int width, int height, bool stretch)
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            graphics.PreferredBackBufferWidth = width;
            graphics.PreferredBackBufferHeight = height;
            graphics.PreferMultiSampling = false;
            IsMouseVisible = true;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            state = GameState.Intro;
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load content from managers
            ResourceManager.LoadContent(Content);

            // Initialize anything depending on loaded content.
            SetupState();
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                Exit();

            Input.Update();

            if (!ShouldFade)
            {
                GameState oldState = state;
                state = GetNextState();

                // handle state transition
                if (state != oldState)
                {
                    if (state == GameState.Exit)
                        Exit();
                    else
                        SetupState();
                }

                if (controller is Room && controller.Finished)
                {
                    Room oldRoom = curRoom;
                    controller = curRoom = NextRoom();
                    if (oldRoom == null)
                    {
                        curRoom.ShouldPlayMusic = true;
                    }
                }
                if (controller != null)
                    controller.Update(gameTime);
            }
            else
                fade = fade.PushTowards(nextFade, fadeSpeed);

            base.Update(gameTime);
        }


        /// <summary>
        /// Set up the state of the game.
        /// </summary>
        private void SetupState()
        {

            switch (state)
            {
                case GameState.Intro:
                    intro = new Intro();
                    intro.Play();
                    controller = intro;
                    break;
                case GameState.TitleScreen:
                    titleScreen = new TitleScreen();
                    controller = titleScreen;
                    break;
                case GameState.Room:
                    Room oldRoom = curRoom;
                    curRoom = NextRoom();
                    if (oldRoom == null)
                    {
                        curRoom.ShouldPlayMusic = true;
                    }
                    controller = curRoom;
                    break;
                case GameState.GameMenu:
                    controller = gameMenu;
                    break;
                case GameState.Credits:
                    credits = new Credits();
                    controller = credits;
                    break;
                default:
                    throw new InvalidOperationException("Can't setup this state.");
            }
        }

        /// <summary>
        /// Get the next room.
        /// </summary>
        /// <returns>The next room(level) of the game.</returns>
        private Room NextRoom()
        {
            if (curRoom != null && curRoom.Failed)
                return new Room(curRoom.ColorDict, curRoom.Map, GraphicsDevice);

            if (!MoreLevels())
                throw new InvalidOperationException();

            String nextRoomName = roomNames.Pop();
            return new Room(GetNextColorDict(), ResourceManager.LoadMap(nextRoomName, Content), GraphicsDevice);
        }

        private Dictionary<string, Color> GetNextColorDict()
        {
            if (roomNames.Count >= 10)
                return new Dictionary<string, Color>
                {
                    { "Player", Color.LightPink },
                    { "Background", Color.Purple }
                };

            if (roomNames.Count >= 6)
                return new Dictionary<string, Color>
                {
                    { "Player", Color.LightGreen },
                    { "Background", Color.DarkGreen }
                };

            return new Dictionary<string, Color>
            {
                { "Player", Color.LightGreen },
                { "Background", Color.DarkGreen }
            };
        }

        /// <summary>
        /// Get the next state the game should be in.
        /// </summary>
        /// <returns>The next state of the game.</returns>
        private GameState GetNextState()
        {
            if (!ShouldFade)
                switch (state)
                {
                    case GameState.Intro:
                        if (intro.Finished)
                            return GameState.TitleScreen;
                        break;

                    case GameState.TitleScreen:
                        if (titleScreen.Finished)
                            return titleScreen.ExitSelected ? GameState.Exit : GameState.Room;
                        break;

                    case GameState.Room:
                        if (curRoom.MenuRequested)
                            return GameState.GameMenu;
                        if (curRoom.Finished && !curRoom.Failed && !MoreLevels())
                            return GameState.Credits;
                        break;

                    case GameState.Credits:
                        if (credits.Finished)
                            return GameState.Exit;
                        break;

                    case GameState.GameMenu:
                        if (gameMenu.Finished)
                            return GameState.Room;
                        break;

                    case GameState.Exit:
                        return GameState.Exit;

                    // if we've defaulted, the game is in an unknown/unimplemented state, so crash.
                    default:
                        throw new InvalidOperationException();
                }

            // if one of the cases fell through without returning or we're in a fade, return
            // the current state of the game.
            return state;
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            if (controller != null)
                controller.Draw(spriteBatch);
            // fill the screen with the fade color
            spriteBatch.Begin();
            spriteBatch.Draw(ResourceManager.Pixel, new Rectangle(0, 0, 4000, 4000), fade);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        /// <summary>
        /// Fade out the given screen.
        /// </summary>
        /// <param name="color">The color to fade out with.</param>
        /// <param name="speed">The speed to fade out at.</param>
        public static void FadeOut(Color color, FadeSpeed speed)
        {
            nextFade = color;
            fadeSpeed = (byte)speed;
        }

        public static void FadeIn(FadeSpeed speed)
        {
            nextFade = Color.Transparent;
            fadeSpeed = (byte)speed;
        }

        /// <summary>
        /// Whether or not there are any more levels remaining in the game.
        /// </summary>
        /// <returns></returns>
        private bool MoreLevels()
        {
            return roomNames.Count > 0;
        }
    }

    enum GameState
    {
        Intro,
        TitleScreen,
        Room,
        GameMenu,
        Credits,
        Exit
    }

    public enum FadeSpeed
    {
        Slow = 1,
        Medium = 3,
        Fast = 5
    }
}
