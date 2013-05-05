using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SwordsArt.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SwordsArt.Interface
{
    /// <summary>
    /// The intro of the game.
    /// </summary>
    public class Intro : IController
    {
        bool finished = false;
        SpriteFont font;
        Texture2D logo;

        /// <summary>
        /// Make a new intro.
        /// </summary>
        public Intro()
        {

        }

        public void Initialize()
        {

        }

        /// <summary>
        /// Play the intro of the game.
        /// </summary>
        internal void Play()
        {
            // TODO: Implement intro.
            Finish();
        }

        public void Draw(SpriteBatch spriteBatch)
        {
        }

        public virtual bool Finished
        {
            get { return finished; }
        }

        public void Finish()
        {
            GameEngine.FadeOut(Color.White, FadeSpeed.Fast);
            finished = true;
        }

        public void Update(GameTime gameTime)
        {
        }
    }
}
