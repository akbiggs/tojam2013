using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace SwordsArt.Rooms
{
    /// <summary>
    /// A section of the room.
    /// Changes how the camera behaves, and maybe some other stuff.
    /// </summary>
    public class Section
    {
        #region Constants

        public const float DefaultZoom = 1.5f;

        #endregion

        #region Members

        private Room room;
        private Rectangle box;
        private float zoomLevel;
        public float ZoomLevel
        {
            get { return zoomLevel; }
        }

        private bool centered;
        public bool Centered
        {
            get { return centered; }
        }

        public float Area
        {
            get { return box.Width * box.Height; }
        }

        public Vector2 Center
        {
            get { return new Vector2(box.Center.X, box.Center.Y); }
        }

        #endregion

        /// <summary>
        /// Make a new section.
        /// </summary>
        /// <param name="room">The room this section is dividing up.</param>
        /// <param name="box">The position and size of this section.</param>
        /// <param name="centered">Whether or not the camera should be centered on this section.</param>
        public Section(Room room, Rectangle box, bool centered)
        {
            this.room = room;
            this.box = box;
            this.centered = centered;
            if (!centered)
                zoomLevel = DefaultZoom;
        }

        public Section(Room room, Rectangle box, float zoomLevel)
        {
            this.room = room;
            this.box = box;
            centered = false;
            this.zoomLevel = zoomLevel;
        }

        public bool Contains(GameObject obj)
        {
            return obj.Box.Rectangle.Intersects(box);
        }
    }
}
