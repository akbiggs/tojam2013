using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SwordsArt.Engine;

namespace SwordsArt.Rooms
{
    public class Background
    {
        int width, height;
        Color color;

        public Background(int width, int height, Color color)
        {
            this.width = width;
            this.height = height;
            this.color = color;
        }

        public virtual void Update()
        {

        }

        public virtual void Draw(SpriteBatch spr)
        {
            spr.Draw(ResourceManager.Pixel, new Rectangle(0, 0, width, height), color);
        }

        internal void Update(GameTime gameTime)
        {
        }
    }
}
