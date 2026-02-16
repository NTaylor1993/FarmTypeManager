using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;

namespace FarmTypeManager
{
    public partial class ModEntry : Mod
    {
        /// <summary>Methods used repeatedly by other sections of this mod, e.g. to locate tiles.</summary>
        private static partial class Utility
        {
            /// <summary>Determines whether a specific tile on a map is valid for object placement.</summary>
            /// <param name="location">The game location to check.</param>
            /// <param name="tile">The tile to validate for object placement. If the object is larger than 1 tile, this is the top left corner.</param>
            /// <param name="size">A point representing the object's size in tiles (horizontal, vertical).</param>
            /// <returns>True if the tile is currently valid for object placement.</returns>
            public static bool IsTileValid(GameLocation location, Vector2 tile, Point size, string strictTileChecking)
            {
                if (strictTileChecking.Equals("none", StringComparison.OrdinalIgnoreCase)) //no validation required
                    return true;

                //fast path for 1x1 objects
                if (size.X == 1 && size.Y == 1)
                    return IsSingleTileValid(location, tile, strictTileChecking);

                //multi-tile path: check each sub-tile directly without allocating a list
                for (int x = 0; x < size.X; x++)
                {
                    for (int y = 0; y < size.Y; y++)
                    {
                        if (!IsSingleTileValid(location, new Vector2(tile.X + x, tile.Y + y), strictTileChecking))
                            return false;
                    }
                }

                return true; //all relevant tests passed, so the tile is valid
            }

            /// <summary>Determines whether a single tile is valid for object placement at the given strictness level.</summary>
            /// <param name="location">The game location to check.</param>
            /// <param name="tile">The tile to validate.</param>
            /// <param name="strictTileChecking">The strictness level string.</param>
            /// <returns>True if the tile is currently valid for object placement.</returns>
            private static bool IsSingleTileValid(GameLocation location, Vector2 tile, string strictTileChecking)
            {
                if (strictTileChecking.Equals("low", StringComparison.OrdinalIgnoreCase)) //low-strictness validation
                {
                    return !location.isObjectAtTile((int)tile.X, (int)tile.Y); //false if this tile is blocked by an object
                }
                else if (strictTileChecking.Equals("medium", StringComparison.OrdinalIgnoreCase)) //medium-strictness validation
                {
                    return !location.IsTileOccupiedBy(tile); //false if this tile is occupied
                }
                else if (strictTileChecking.Equals("high", StringComparison.OrdinalIgnoreCase)) //high-strictness validation
                {
                    return !location.IsTileOccupiedBy(tile) && location.CanItemBePlacedHere(tile); //false if the tile is occupied OR not clear for placement
                }
                else //max-strictness validation
                {
                    return !location.IsNoSpawnTile(tile) && !location.IsTileOccupiedBy(tile) && location.CanItemBePlacedHere(tile); //false if this tile has "NoSpawn", is *not* totally clear
                }
            }
        }
    }
}