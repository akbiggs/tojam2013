using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SwordsArt.Engine;

namespace SwordsArt.Objects
{
    public class Target : GameObject
    {
        public Target(Vector2 position, Color color)
            : base(position, Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, color, true, new Vector2(32, 32), 
            ResourceManager.Pixel, 0)
        {
        }

        public override void Update(Rooms.Room room, GameTime gameTime)
        {
            if (Vector2.Distance(room.Player.Center, Center) < 32)
            {
                Explode(room);
                room.Player.RestoreLife();
            }

            base.Update(room, gameTime);
        }

        private void Explode(Rooms.Room room)
        {
            // TODO: Add particles to the room in an explosion.
            room.Remove(this);
        }
    }
}
