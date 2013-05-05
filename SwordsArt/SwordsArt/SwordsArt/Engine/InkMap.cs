using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace SwordsArt.Engine
{
    /// <summary>
    /// Tracks and draws out how ink has been splattered onto the screen.
    /// </summary>
    public class InkMap
    {
        #region Constants

        private const float MAX_MAP_WIDTH = 2000;
        private const float MAX_MAP_HEIGHT = 2000;

        #endregion

        #region Members

        public RenderTarget2D[,] Maps;

        private bool[,] hasBeenDrawn;
        private int numRows;
        private int numCols;

        #endregion

        public InkMap(GraphicsDevice device, float width, float height)
        {
            int numMapRows = (int)Math.Ceiling(width / MAX_MAP_WIDTH);
            int numMapCols = (int)Math.Ceiling(height / MAX_MAP_HEIGHT);
            Maps = new RenderTarget2D[numMapCols, numMapRows];
            hasBeenDrawn = new bool[numMapCols, numMapRows];

            numRows = numMapRows;
            numCols = numMapCols;

            for (int i = 0; i < numMapCols; i++)
                for (int j = 0; j < numMapRows; j++)
                {
                    Maps[i, j] = new RenderTarget2D(device, (int)MAX_MAP_WIDTH, (int)MAX_MAP_HEIGHT, false, SurfaceFormat.Vector2,
                                                    DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
                    hasBeenDrawn[i, j] = false;
                }
        }

        public void Update()
        {

        }

        public void Draw(SpriteBatch spriteBatch)
        {
            for (int i = 0; i < numCols; i++)
                for (int j = 0; j < numRows; j++)
                {
                    spriteBatch.Draw(Maps[i, j], new Vector2(j * MAX_MAP_WIDTH, i * MAX_MAP_HEIGHT), Color.White);
                }
        }

        public RenderTarget2D GetMap(Vector2 position)
        {
            if (!InBounds(position))
                return null;

            Point mapIndex = GetMapIndex(position);
            return Maps[mapIndex.X, mapIndex.Y];
        }

        private bool InBounds(Vector2 position)
        {
            return !(position.X < 0 || position.X >= MAX_MAP_WIDTH * numRows ||
                position.Y < 0 || position.Y >= MAX_MAP_HEIGHT * numCols);
        }

        public Point GetMapIndex(Vector2 position)
        {
            return new Point((int)position.X / (int)MAX_MAP_WIDTH, (int)position.Y / (int)MAX_MAP_HEIGHT);
        }

        public bool HasBeenDrawn(Vector2 position)
        {
            Point mapIndex = GetMapIndex(position);
            return hasBeenDrawn[mapIndex.X, mapIndex.Y];
        }

        public void SetDrawn(Vector2 position)
        {
            Point mapIndex = GetMapIndex(position);
            hasBeenDrawn[mapIndex.X, mapIndex.Y] = true;
        }
    }
}
