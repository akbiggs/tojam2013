using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace SwordsArt.Interface
{
    public class TimeText
    {
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
        }

        public void Update(TimeSpan newTime)
        {
            time = newTime;
            curColor = Color.Lerp(startColor, curColor, GetTimeRemainingLerpFactor());
        }

        

        private float GetTimeRemainingLerpFactor()
        {
            TimeSpan difference = startTime.Subtract(time);
            return difference.Ticks/startTime.Ticks;
        }
    }
}
