using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SwordsArt.Engine;

namespace SwordsArt.Objects
{
    /// <summary>
    ///     A bounding box for an object.
    ///     Can be used for collision detection.
    /// </summary>
    public class BBox
    {
        private Rectangle box;

        public BBox(Rectangle rectangle)
        {
            box = rectangle;
        }

        public BBox(int x, int y, int width, int height)
            : this(new Rectangle(x, y, width, height))
        {

        }

        public Vector2 Position
        {
            get { return new Vector2(box.X, box.Y); }
            set
            {
                box.X = (int)value.X;
                box.Y = (int)value.Y;
            }
        }

        public Vector2 Size
        {
            get { return new Vector2(box.Width, box.Height); }
        }

        public float Width
        {
            get { return box.Width; }
        }

        public float Height
        {
            get { return box.Height; }
        }

        public float Area
        {
            get { return box.Width * box.Height; }
        }

        public float Top
        {
            get { return box.Top; }
        }

        public float Bottom
        {
            get { return box.Bottom; }
        }

        public float Left
        {
            get { return box.Left; }
        }

        public float Right
        {
            get { return box.Right; }
        }

        public Rectangle Rectangle
        {
            get { return box; }
        }

        public static BBox SmallerOf(BBox box1, BBox box2)
        {
            return box1.Area < box2.Area ? box1 : box2;
        }
        public BBox Intersect(BBox other)
        {
            Rectangle intersection = Rectangle.Intersect(box, other.Rectangle);
            if (intersection != Rectangle.Empty)
                return new BBox(intersection);

            return null;
        }

        public bool IsEmpty()
        {
            return box.IsEmpty;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(ResourceManager.Pixel, Rectangle, new Color(255, 0, 0, 50));
        }

        public Vector2 Center
        {
            get { return new Vector2(Position.X + Size.X / 2, Position.Y + Size.Y / 2); }
        }
    }
}
