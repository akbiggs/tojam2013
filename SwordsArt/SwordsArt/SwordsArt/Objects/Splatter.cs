using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace SwordsArt.Rooms
{
    public class Splatter
    {
        #region Constant

        private const byte FADE_SPEED = 2;
        #endregion

        #region Members

        private Vector2 position;
        public Vector2 Position
        {
            get { return position; }
        }
        private Vector2 size;
        private float rotation;
        private Texture2D texture;
        private Color color;
        #endregion

        public Splatter(Vector2 position, Vector2 size, float rotation, Texture2D texture, Color color)
        {
            this.position = position;
            this.size = size;
            this.rotation = rotation;
            this.texture = texture;
            this.color = color;
        }

        public void Update()
        {
            color.A = (byte)Math.Max(color.A - FADE_SPEED, 0);
            color.R = color.R != 0 ? (byte)Math.Max(color.R - FADE_SPEED, 0) : (byte)0;
            color.G = color.G != 0 ? (byte)Math.Max(color.G - FADE_SPEED, 0) : (byte)0;
            color.B = color.B != 0 ? (byte)Math.Max(color.B - FADE_SPEED, 0) : (byte)0;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(texture, new Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y), null,
                             color, rotation, Vector2.Zero, SpriteEffects.None, 0);
        }

        public bool IsVisible()
        {
            return color.PackedValue > 0;
        }
    }
}
