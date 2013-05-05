using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SwordsArt.Engine;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;

namespace SwordsArt.Interface
{
    /// <summary>
    /// Basic generic menu.
    /// Offers options to player, waits for them to select one.
    /// </summary>
    public class Menu
    {
        List<String> options;
        String exitOption;
        Dictionary<String, Action> handlers;

        public Menu(Dictionary<String, Action> options, String exitOption)
        {
            Debug.Assert(options.Keys.Contains(exitOption), "Exit option not present in menu.");
            this.handlers = options;
            this.options = options.Keys.ToList<String>();
            this.exitOption = exitOption;
        }

        public void AddOption(String name, Action handler)
        {
            Debug.Assert(!options.Contains(name), "Option already present in menu.");
            this.options.Add(name);
            handlers[name] = handler;
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
        }
    }
}
