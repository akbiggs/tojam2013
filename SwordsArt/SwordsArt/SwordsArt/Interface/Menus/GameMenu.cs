using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace SwordsArt.Interface
{
    /// <summary>
    /// The in-game menu.
    /// </summary>
    public class GameMenu : Menu
    {
        public GameMenu()
            : base(new Dictionary<string, Action>() 
            {
                {"Quit", () => {} }
            }, "Quit")
        {

        }

        public override void Update(GameTime gameTime)
        {
            throw new NotImplementedException();
        }
    }
}
