using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace SwordsArt.Rooms
{
    /// <summary>
    /// Collection of frames into an animation.
    /// </summary>
    public class AnimationSet
    {
        int animationTimer = 0;
        int animationFrame = 0;
        public int CurFrameNumber
        {
            get { return animationFrame; }
        }

        public readonly String Name;
        Texture2D texture;
        public readonly int NumFrames;
        private int startFrame;
        int width;
        public readonly int FrameDuration;

        private bool shouldLoop;

        /// <summary>
        /// Make a new animation set.
        /// </summary>
        /// <param name="name">The name of the animation.</param>
        /// <param name="texture">The texture from which the animation will be extracted.</param>
        /// <param name="frames">The number of frames in the animation.</param>
        /// <param name="frameWidth">The width of each frame.</param>
        /// <param name="frameDuration">How long each frame should last for.</param>
        /// <param name="shouldLoop">Whether or not the animation should loop.</param>
        /// <param name="startFrame">The starting frame of the animation.</param>
        public AnimationSet(String name, Texture2D texture, int frames, int frameWidth, int frameDuration, bool shouldLoop = true, int startFrame = 0)
        {
            Name = name;
            this.texture = texture;
            NumFrames = frames;
            this.startFrame = startFrame;
            width = frameWidth;
            FrameDuration = frameDuration;
            this.shouldLoop = shouldLoop;
        }

        /// <summary>
        /// Update the animation.
        /// </summary>
        public void Update()
        {
            if (shouldLoop || !IsDonePlaying())
            {
                animationTimer++;
                if (animationTimer > FrameDuration)
                {
                    animationTimer = 0;
                    animationFrame++;
                }

                if (animationFrame == NumFrames)
                {
                    animationFrame = 0;
                }
            }
        }

        public void Reset()
        {
            animationTimer = 0;
            animationFrame = 0;
        }

        /// <summary>
        /// Whether or not the name of this set is the
        /// given name.
        /// </summary>
        /// <param name="name">The name to check.</param>
        /// <returns>True if they are the same name, false otherwise.</returns>
        public bool IsCalled(String name)
        {
            return Name == name;
        }

        /// <summary>
        /// The texture from which the animation is extracted.
        /// </summary>
        /// <returns>The texture of the animation.</returns>
        public Texture2D GetTexture()
        {
            return texture;
        }

        /// <summary>
        /// Returns a rectangle corresponding to the given frame.
        /// </summary>
        /// <param name="frame">The frame to get the rectangle from.</param>
        /// <returns>A rectangle of the same size as the frame.</returns>
        public Rectangle GetFrameRect(int frame)
        {
            return new Rectangle((frame + startFrame) * width, 0, width, texture.Height);
        }

        /// <summary>
        /// Return a rectangle corresponding to the animation's current frame.
        /// </summary>
        /// <returns>A rectangle of the same size as the frame.</returns>
        public Rectangle GetFrameRect()
        {
            return GetFrameRect(animationFrame);
        }

        /// <summary>
        /// Returns whether or not the animation is done playing.
        /// </summary>
        /// <returns></returns>
        public bool IsDonePlaying()
        {
            return animationTimer > FrameDuration - 1 && animationFrame == NumFrames - 1;
        }
    }
}
