
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
    /// The title screen of the game.
    /// Might not end up getting used...we'll see.
    /// </summary>
    public class TitleScreen : IController
    {
        private bool finished;
        private MainMenu mainMenu;

        public TitleScreen()
        {
            mainMenu = new MainMenu();
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            mainMenu.Draw(spriteBatch);
        }

        public bool Finished
        {
            get { return finished; }
        }

        public void Finish()
        {
            finished = true;
        }

        public bool ExitSelected { get; set; }

        public void Update(GameTime gameTime)
        {
            // TODO: Title screen.
            Finish();
        }

    }
}
