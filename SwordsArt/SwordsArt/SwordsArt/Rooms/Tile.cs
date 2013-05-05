using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using TiledLib;

namespace SwordsArt.Rooms
{
    /// <summary>
    /// A tile of the room.
    /// Can be solid, passable on the underside, or transparent.
    /// </summaryx>
    public class Tile
    {
        private const string TYPE_PROPERTY_NAME = "Type";
        private const string SOLID = "Solid";
        private const string TOP_SOLID = "TopSolid";
        private const string TRANSPARENT = "Transparent";

        private Vector2 position;
        private Vector2 size;
        private TileType type;
        public TileType Type
        {
            get { return type; }
        }
        public Color Color;

        /// <summary>
        /// Initializes a new instance of the <see cref="Tile" /> class.
        /// </summary>
        /// <param name="tile">The tile this is based off of.</param>
        public Tile(Vector2 pos, Vector2 size, TiledLib.Tile tile, Color color)
        {
            position = pos;
            this.size = size;
            Color = color;

            // get the tile type from its properties in the map
            Property tiletype;
            if (tile == null)
                type = TileType.Transparent;
            else if (!tile.Properties.TryGetValue(TYPE_PROPERTY_NAME, out tiletype))
                // TODO: Make this throw an error when we have the actual tileset.
                type = TileType.Solid;
            else
                type = GetTypeByProperty(tiletype);
        }

        public Tile(Vector2 pos, Vector2 size, TileType type, Color color)
        {
            position = pos;
            this.size = size;
            Color = color;
            this.type = type;
        }

        public Vector2 Position
        {
            get { return position; }
        }

        private TileType GetTypeByProperty(Property type)
        {
            switch (type.RawValue)
            {
                case SOLID:
                    return TileType.Solid;
                case TOP_SOLID:
                    return TileType.TopSolid;
                case TRANSPARENT:
                    return TileType.Transparent;
                default:
                    throw new InvalidOperationException("Invalid tile type name.");
            }
        }
    }

    public enum TileType
    {
        Solid,
        TopSolid,
        Transparent
    }
}
