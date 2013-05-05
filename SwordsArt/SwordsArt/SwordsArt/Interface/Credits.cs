using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SwordsArt.Engine;

namespace SwordsArt.Interface
{
    public class Credits : IController
    {
        bool finished = false;
        public void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            Finish();
        }

        public void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
        {
        }

        public bool Finished
        {
            get { return finished; }
        }

        public void Finish()
        {
            finished = true;
        }
    }
}
