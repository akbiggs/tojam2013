using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SwordsArt.Engine
{
    /// <summary>
    /// Use for anything that can control the logic of the game.
    /// </summary>
    public interface IController
    {
        /// <summary>
        /// Updates the controller.
        /// </summary>
        /// <param name="gameTime">The game time.</param>
        void Update(GameTime gameTime);

        /// <summary>
        /// Draws the controller.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch.</param>
        void Draw(SpriteBatch spriteBatch);

        /// <summary>
        /// Whether or not the controller is finished with its role in the
        /// game (i.e. whether or not we're ready to yield control).
        /// </summary>
        bool Finished { get; }

        /// <summary>
        /// Clean up and yield control of the game's logic back to the engine.
        /// </summary>
        void Finish();
    }
}
