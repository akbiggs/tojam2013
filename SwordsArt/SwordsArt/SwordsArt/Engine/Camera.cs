using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SwordsArt.Rooms;
using SwordsArt.Objects;
using Microsoft.Xna.Framework.Graphics;

namespace SwordsArt.Engine
{
    public class Camera
    {
        #region Constants


        private const float ZOOM_SPEED = 0.05f;
        private const float MIN_ZOOM = 0.1f;
        private const float PAN_SPEED = 20f;

        private const float CAMERA_BOUND_FACTOR = 1.95f;
        #endregion

        #region Members

        public float Rotation;
        public GameObject target;
        public Vector2 targetPosition;

        public Matrix Transformation;
        private Vector2 position;
        private Vector2 nextPos;
        private float zoom;
        private float nextZoom;

        private int roomWidth;
        private int roomHeight;

        #endregion

        /// <summary>
        /// Make a new camera focusing on the given game object.
        /// </summary>
        /// <param name="target">The target to focus on.</param>
        /// <param name="roomWidth">The width of the room this camera resides in. Used to prevent the camera from looking
        /// out of bounds.</param>
        /// <param name="roomHeight">The height of the room this camera resides in. Used to prevent the camera from looking
        /// out of bounds.</param>
        /// <param name="zoomLevel">The zoom level of the camera.</param>
        public Camera(GameObject target, int roomWidth, int roomHeight, float zoomLevel)
        {
            Zoom = zoomLevel;
            Rotation = 0.0f;
            position = target.Position;
            this.target = target;

            this.roomWidth = roomWidth;
            this.roomHeight = roomHeight;
        }

        public float Zoom
        {
            get { return zoom; }
            set
            {
                zoom = value;
                if (zoom < MIN_ZOOM) zoom = MIN_ZOOM;
            }
        }

        private bool InZoomTransition
        {
            get { return nextZoom >= MIN_ZOOM; }
        }

        public void Update(Room room, GameTime gameTime)
        {
            // if not panning, just follow our target, otherwise pan
            if (!InPan)
            {
                // but we might just be centered on a point, so check that we have a target.
                if (target != null)
                    position = target.Position;
            }
            else
            {
                // to avoid a jarring jump, we need to update the pan position to match up with the
                // target's position if we have one.
                if (target != null)
                    nextPos = target.Position;
                position = position.PushTowards(nextPos, PAN_SPEED * Vector2.One);
                if (position == nextPos)
                    nextPos = Vector2.Zero;
            }

            // if we are still in the middle of zooming to a spot
            if (InZoomTransition)
            {
                Zoom = nextZoom > Zoom ? Math.Min(Zoom + ZOOM_SPEED, nextZoom) : Math.Max(Zoom - ZOOM_SPEED, nextZoom);

                // done zooming?
                if (Zoom == nextZoom) nextZoom = 0;
            }
        }

        public void ChangeTarget(GameObject target)
        {
            this.target = target;
            targetPosition = Vector2.Zero;
            Pan(target.Position);
        }

        public void ChangeTarget(Vector2 target)
        {
            this.target = null;
            targetPosition = target;
            Pan(targetPosition);
        }

        public void Pan(Vector2 newPosition)
        {
            nextPos = newPosition;
        }

        public void ZoomTo(float newZoom)
        {
            nextZoom = newZoom;
            //Matrix.CreateScale()
        }

        public void Move(Vector2 amount)
        {
            position += amount;
        }

        public Matrix GetTransformation(GraphicsDevice graphicsDevice)
        {
            float posX = MathHelper.Clamp(position.X, graphicsDevice.Viewport.Width / (zoom * CAMERA_BOUND_FACTOR),
                                          roomWidth - graphicsDevice.Viewport.Width / (zoom * CAMERA_BOUND_FACTOR));
            float posY = MathHelper.Clamp(position.Y, graphicsDevice.Viewport.Height / (zoom * CAMERA_BOUND_FACTOR),
                                          roomHeight - graphicsDevice.Viewport.Height / (zoom * CAMERA_BOUND_FACTOR));
            Transformation = Matrix.CreateTranslation(new Vector3(-posX, -posY, 0)) *
                             Matrix.CreateRotationZ(Rotation) *
                             Matrix.CreateScale(new Vector3(Zoom, Zoom, 1)) *
                             Matrix.CreateTranslation(
                                 new Vector3(graphicsDevice.Viewport.Width * 0.5f,
                                             graphicsDevice.Viewport.Height * 0.5f, 0));
            return Transformation;
        }

        public bool InPan
        {
            get { return nextPos != Vector2.Zero && nextPos != position; }
        }
    }
}
