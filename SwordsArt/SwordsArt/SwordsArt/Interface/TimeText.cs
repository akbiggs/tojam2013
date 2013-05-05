using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SwordsArt.Engine;

namespace SwordsArt.Interface
{
    public class TimeText
    {
        SpriteFont font;

        private TimeSpan startTime;
        private TimeSpan time;
        private Color startColor;
        private Color endColor;
        private Color curColor;

        public TimeText(TimeSpan time, Color startColor, Color endColor)
        {
            this.time = startTime = time;
            this.startColor = curColor = startColor;
            this.endColor = endColor;

            font = ResourceManager.GetFont("TimeText");
        }

        public void Update(TimeSpan newTime)
        {
            time = newTime;
            curColor = Color.Lerp(startColor, endColor, GetTimeRemainingLerpFactor());
        }

        public void Draw(SpriteBatch spr)
        {
            String timeLeft = String.Format("{0}.{1}", time.Seconds.ToString(), Math.Round((decimal)time.Milliseconds, 2).ToString());
            float width = font.MeasureString(timeLeft).X;
            spr.DrawString(font, timeLeft, new Vector2(GameEngine.ScreenWidth / 2 - width / 2, 50), curColor);
        }

        private float GetTimeRemainingLerpFactor()
        {
            TimeSpan difference = startTime.Subtract(time);
            float result = (float)difference.Ticks / (float)startTime.Ticks; 
            return result;
        }
    }
}
