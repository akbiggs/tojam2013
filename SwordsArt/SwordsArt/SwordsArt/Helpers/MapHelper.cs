using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TiledLib;

namespace SwordsArt.Helpers
{
    public static class MapHelper
    {
        public static int WidthInPixels(this Map map)
        {
            return map.WidthInTiles * map.TileWidth;
        }

        public static int HeightInPixels(this Map map)
        {
            return map.HeightInTiles * map.TileHeight;
        }
    }
}
