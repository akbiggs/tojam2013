using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SwordsArt.Objects;
using System.Diagnostics;

namespace SwordsArt.Rooms
{
    /// <summary>
    ///     Any object that should abide by the game's laws and physics.
    /// </summary>
    public class GameObject
    {
        #region Constants
        private const float COLLISION_FORGIVENESS_FACTOR = 0.5f;
        private const float SPOUT_CFF = 0.001f;

        private const float BBOX_OFFSET_X = 0.2f;
        private const float BBOX_OFFSET_Y = 0f;
        #endregion

        #region Members

        protected readonly List<AnimationSet> animations;
        protected Vector2 deceleration;
        protected Vector2 maxSpeed;

        protected float rotation;
        protected Vector2 velocity;
        public Vector2 Velocity
        {
            get { return velocity; }
        }

        private Vector2 acceleration;
        protected BBox box;
        public BBox Box
        {
            get { return box; }
        }

        private bool colorable;
        protected Color color;
        public Color Color
        {
            get { return color; }
        }

        // TODO: Refactor this, very bad
        public Direction Facing = Direction.None;

        protected virtual AnimationSet curAnimation { get; set; }
        private bool hasMoved;
        protected bool hasCollidedWithWall;
        protected bool hasCollidedWithCeiling = false;
        protected bool hasCollidedWithGround = false;
        private Vector2 maxPosition;
        private Vector2 minPosition;
        protected Vector2 position;

        protected Vector2 size;
        public Vector2 Size
        {
            get { return size; }
        }

        public Vector2 Position
        {
            get { return position; }
            set
            {
                position = value;
                box.Position = value + BoxOffset;
            }
        }

        public Vector2 Center
        {
            get { return Position + Size / 2; }
        }

        public virtual Vector2 BoxOffset
        {
            get { return new Vector2(BBOX_OFFSET_X * Size.X, BBOX_OFFSET_Y * Size.Y); }
        }

        public virtual bool IsCollidable
        {
            get { return true; }
        }

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="GameObject" /> class.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="initialVelocity">The initial velocity.</param>
        /// <param name="maxSpeed">The max speed.</param>
        /// <param name="acceleration">The acceleration.</param>
        /// <param name="deceleration">The deceleration.</param>
        /// <param name="color">The color.</param>
        /// <param name="colorable">
        ///     if set to <c>true</c> [colorable].
        /// </param>
        /// <param name="size">The size.</param>
        /// <param name="animations">The animations.</param>
        /// <param name="startAnimationName">Start name of the animation.</param>
        /// <param name="rotation">The rotation.</param>
        public GameObject(Vector2 position, Vector2 initialVelocity, Vector2 maxSpeed, Vector2 acceleration,
                          Vector2 deceleration, Color color, bool colorable, Vector2 size, List<AnimationSet> animations,
                          String startAnimationName, float rotation)
        {
            this.position = position;
            velocity = initialVelocity;
            this.maxSpeed = maxSpeed;
            this.acceleration = acceleration;
            this.deceleration = deceleration;
            this.color = color;
            this.colorable = colorable;
            Debug.Assert(size.X >= 0 && size.Y >= 0, "Invalid object size.");
            this.size = size;

            this.animations = animations;
            curAnimation = GetAnimationByName(startAnimationName);
            Debug.Assert(curAnimation != null, "Couldn't find the starting animation.");

            this.rotation = rotation;

            box = new BBox((int)(Position.X + BBOX_OFFSET_X * Size.X), (int)(Position.Y + BBOX_OFFSET_Y * Size.Y),
                (int)((1 - 2 * BBOX_OFFSET_X) * Size.X), (int)((1 - 2 * BBOX_OFFSET_Y) * Size.Y));
        }

        /// <summary>
        ///     Initializes a new unanimated instance of the <see cref="GameObject" /> class.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="initialVelocity">The initial velocity.</param>
        /// <param name="maxSpeed">The max speed.</param>
        /// <param name="acceleration">The acceleration.</param>
        /// <param name="deceleration">The deceleration.</param>
        /// <param name="color">The color.</param>
        /// <param name="colorable">
        ///     if set to <c>true</c> [object's color can be changed].
        /// </param>
        /// <param name="size">The size.</param>
        /// <param name="texture">The texture.</param>
        /// <param name="rotation">The rotation.</param>
        public GameObject(Vector2 position, Vector2 initialVelocity, Vector2 maxSpeed, Vector2 acceleration,
                          Vector2 deceleration, Color color, bool colorable, Vector2 size, Texture2D texture,
                          float rotation)
            : this(position, initialVelocity, maxSpeed, acceleration, deceleration, color, colorable, size,
                   new List<AnimationSet>
                       {
                           new AnimationSet("texture", texture, 1, texture.Width, 1)
                       }, "texture", rotation)
        {
        }

        /// <summary>
        ///     Update the object.
        /// </summary>
        /// <param name="gameTime">The current time of the game.</param>
        public virtual void Update(Room room, GameTime gameTime)
        {
            hasCollidedWithWall = false;
            hasCollidedWithGround = false;
            if (velocity.Y > 0 && hasCollidedWithCeiling)
            {
                hasCollidedWithCeiling = false;
                Console.WriteLine("Done ceiling colliding!");
            }
            curAnimation.Update();
        }

        /// <summary>
        ///     Moves in the specified direction.
        /// </summary>
        /// <param name="room">The room that this object is moving around in.</param>
        /// <param name="direction">The direction to move in.</param>
        internal virtual void Move(Room room, Vector2 direction)
        {
            if (direction != Vector2.Zero)
                direction.Normalize();

            ApplyGravity(room);
            Accelerate(direction);

            // TODO: Figure out a way to resolve collisions and do only one function call.

            // resolve collisions on x-axis first
            UpdateBounds(room);
            ChangeXPosition(room);

            // then resolve any remaining on the y-axis
            UpdateBounds(room);
            ChangeYPosition(room);
        }

        /// <summary>
        /// Returns whether or not the given object is colliding with this object.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <param name="collisionRegion">The region of collision. Empty if no collision occurred.</param>
        /// <returns>True if the objects are colliding, false otherwise.</returns>
        public virtual bool IsColliding(GameObject obj, out BBox collisionRegion)
        {
            float collisionForgiveness = (this is Spout || obj is Spout) ? SPOUT_CFF : COLLISION_FORGIVENESS_FACTOR;
            collisionRegion = box.Intersect(obj.box);

            // only do forgiveness on the smaller box, a small enough box might not even be allowed
            // to collide on the larger box if we don't care
            BBox smallerBox = BBox.SmallerOf(box, obj.box);

            // because boxes are cruel, and not very true to the shape of the objects, only return true
            // if the area of collision is larger than we're willing to forgive.
            return collisionRegion != null &&
                !collisionRegion.IsEmpty() &&
                collisionRegion.Area >= smallerBox.Area * collisionForgiveness;
        }

        public virtual void CollideWithObject(GameObject obj, Room room, BBox collision)
        {
        }

        /// <summary>
        ///     Collides with a wall.
        /// </summary>
        /// <param name="room">The room containing the wall.</param>
        public virtual void CollideWithWall(Room room)
        {
            hasCollidedWithWall = true;
        }

        /// <summary>
        ///     Collides with the ground. Should be called alongside CollideWithWall.
        /// </summary>
        /// <param name="room">The room containing the ground.</param>
        public virtual void CollideWithGround(Room room)
        {
            velocity.Y = 0;
        }

        /// <summary>
        ///     Applies gravity to the object.
        /// </summary>
        /// <param name="room">The room that the object is in.</param>
        protected virtual void ApplyGravity(Room room)
        {
            velocity.Y += room.Gravity;
        }

        /// <summary>
        ///     Changes the object's velocity towards the specified direction.
        /// </summary>
        /// <param name="direction">The direction to move.</param>
        private void Accelerate(Vector2 direction)
        {
            var change = new Vector2(acceleration.X * direction.X, acceleration.Y * direction.Y);
            // let gravity handle decelleration on the y-axis.
            if (Math.Abs(change.X - 0) < float.Epsilon)
                Decellerate();
            velocity = Vector2.Clamp(velocity += change, -maxSpeed, maxSpeed);
        }

        private void Decellerate()
        {
            velocity = velocity.PushBack(deceleration);
        }

        private void UpdateBounds(Room room, bool collidable = true)
        {
            minPosition = room.GetMinPosition(box.Position, box.Size, color, collidable);
            maxPosition = room.GetMaxPosition(box.Position, box.Size, color, collidable);
        }

        private void ChangeXPosition(Room room)
        {
            // check for colliding against a wall
            if (Box.Position.X + velocity.X < minPosition.X || Box.Position.X + velocity.X > maxPosition.X)
                CollideWithWall(room);
            Position = new Vector2(MathHelper.Clamp(Box.Position.X + velocity.X, minPosition.X, maxPosition.X) - BoxOffset.X, Position.Y);
        }

        private void ChangeYPosition(Room room)
        {
            bool shouldCollideWithGround = false;
            bool shouldCollideWithCeiling = false;

            // check for colliding against a wall
            if (Box.Position.Y + velocity.Y < minPosition.Y)
                shouldCollideWithCeiling = true;

            else if (Box.Position.Y + velocity.Y > maxPosition.Y)
            {
                // if we're falling off-stage, die
                if (maxPosition.Y + Box.Height * 2 >= room.Height)
                {
                    room.Remove(this);
                    return;
                }

                // handle the ground collision after we've moved, to prevent weirdness when player is moving too fast
                shouldCollideWithGround = true;
            }

            Position = new Vector2(Position.X, MathHelper.Clamp(Box.Position.Y + velocity.Y, minPosition.Y, maxPosition.Y) + BoxOffset.Y);

            if (shouldCollideWithGround)
            {
                hasCollidedWithGround = true;
                CollideWithWall(room);
                CollideWithGround(room);
            }
            if (shouldCollideWithCeiling)
            {
                hasCollidedWithCeiling = true;
                CollideWithWall(room);
                CollideWithCeiling(room);
            }
        }

        protected virtual void CollideWithCeiling(Room room)
        {
            hasCollidedWithCeiling = true;

        }

        /// <summary>
        ///     Draw the object.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch of the game.</param>
        public virtual void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(curAnimation.GetTexture(), new Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y), curAnimation.GetFrameRect(),
                             color, rotation, Vector2.Zero, SpriteEffects.None, 0);
        }

        /// <summary>
        ///     Changes the animation being played. Doesn't do anything if called with the name of the currently
        ///     playing animation.
        /// </summary>
        /// <param name="name">The name of the new animation.</param>
        /// <exception cref="System.InvalidOperationException">Specified animation doesn't exist.</exception>
        protected virtual void ChangeAnimation(string name)
        {
            if (!curAnimation.IsCalled(name))
            {
                AnimationSet newAnimation = GetAnimationByName(name);
                if (newAnimation == null)
                    throw new InvalidOperationException("Specified animation doesn't exist.");
                newAnimation.Reset();
                newAnimation.Update();
                curAnimation = newAnimation;
            }
        }

        private AnimationSet GetAnimationByName(string name)
        {
            return animations.Find(animset => animset.IsCalled(name));
        }
    }
}
