using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SwordsArt.Interface
{
    /// <summary>
    /// The main menu of the game.
    /// </summary>
    public class MainMenu : Menu
    {
        public MainMenu() :
            base(new Dictionary<String, Action>() 
            {
                { "Quit", () => {} }
            }, "Quit")
        {

        }
    }
}
