using System.Reflection.Metadata;
using Microsoft.Xna.Framework;
using NeuroSDKCsharp.Messages.Outgoing;
using NeuroStardewValley.Debug;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.TerrainFeatures;
using Object = StardewValley.Object;

namespace NeuroStardewValley.Source.Utilities;

public static class WarpUtilities
{
    public static List<string> GetTilesInLocation(GameLocation location)
    {
        List<string> tileList = new();
        List<Building> sentBuildings = new();
        WaterTiles.WaterTileData[,] waterTileData = { };
        if (location.waterTiles is not null)
        {
            waterTileData = location.waterTiles.waterTiles;
        }

        int maxX = location.Map.DisplayWidth / Game1.tileSize;
        int maxY = location.Map.DisplayHeight / Game1.tileSize;

        Logger.Info($"map size X: {maxX}  maxY: {maxY}");

        for (int x = 0; x < maxX; x++)
        {
            for (int y = 0; y < maxY; y++)
            {
                Rectangle rect = new Rectangle(x * Game1.tileSize, y * Game1.tileSize, Game1.tileSize, Game1.tileSize);
                if (x < waterTileData.GetLength(0) && y < waterTileData.GetLength(1) && waterTileData[x, y].isWater)
                {
                    tileList.Add($"Tile: {new Point(x, y)}, is water.");
                    continue;
                }

                if (!Game1.currentLocation.isCollidingPosition(rect, Game1.viewport, true, 0, false, Game1.player))
                    continue;

                object? obj = GetTileType(location, new Point(x, y));
                if (obj is null)
                {
                    if (x < waterTileData.GetLength(0) && y < waterTileData.GetLength(1) && waterTileData[x, y].isWater)
                    {
                        tileList.Add($"Tile: {new Point(x, y)}, is water.");
                        continue;
                    }

                    tileList.Add($"Tile: {new Point(x, y)}, This is a border of the map.");
                    continue;
                }

                switch (obj)
                {
                    case Object objectValue:
                        tileList.Add($"Tile: {objectValue.TileLocation.ToPoint()}, object name: {objectValue.Name}," +
                                     $" object Type: {objectValue.Type}");
                        break;
                    case Building building:
                        if (sentBuildings.Contains(building)) continue; // we do this as buildings take up multiple tiles
                        sentBuildings.Add(building);
                        int buildX = building.tileX.Value;
                        int buildY = building.tileY.Value;
                        int buildWidth = building.tilesWide.Value;
                        int buildHeight = building.tilesHigh.Value;
                        tileList.Add(
                            $"The top left tile of the {building.buildingType.Value} is: {buildX},{buildY}. the bottom right is {buildX + buildWidth}, {buildY + buildHeight}");
                        break;
                    case ResourceClump resourceClump:
                        tileList.Add($"{resourceClump.modData.Name} is at tile: {resourceClump.Tile.ToPoint()}");
                        break;
                    case TerrainFeature terrainFeature:
                        tileList.Add($"{terrainFeature.modData.Name} is at tile: {terrainFeature.Tile.ToPoint()}");
                        break;
                }
            }
        }
        return tileList;
    }
    
    public static object? GetTileType(GameLocation location,Point tile)
    {
        if (location.Objects.ContainsKey(tile.ToVector2()))
        {
            return location.Objects[tile.ToVector2()];
        }

        foreach (var building in location.buildings)
        {
            if (tile.X < building.tileX.Value || tile.X > building.tileX.Value + building.tilesWide.Value)
            {
                if (tile.Y < building.tileY.Value || tile.Y < building.tileY.Value + building.tilesHigh.Value)
                {
                    return building;
                }
            }
        }
        
        foreach (var resourceClump in location.resourceClumps)
        {
            if (resourceClump.getBoundingBox().Contains(tile))
            {
                return resourceClump;
            }
        }

        foreach (var dict in location.terrainFeatures)
        {
            if (!dict.ContainsKey(tile.ToVector2())) continue;
            if (dict[tile.ToVector2()].getBoundingBox().Contains(tile))
            {
                return dict[tile.ToVector2()];
            }
        }

        return null;
    }
    
    public static string GetWarpTiles(GameLocation location)
    {
        location.TryGetMapProperty("Warp", out var warps);
        return warps;
    }

    public static string GetWarpTilesString(string warpTiles)
    {
        string[] warpExtracts = warpTiles.Split(' ');
        Dictionary<Point, string> warpLocation = new();
        int runs = 0;
        for (int i = 0; i < warpExtracts.Length / 5; i++)
        {
            Logger.Info($"tile: {warpExtracts[runs]} next tile: {warpExtracts[runs + 1]}");
            Point tile = new Point(int.Parse(warpExtracts[runs]), int.Parse(warpExtracts[runs + 1]));
            
            string locationName = warpExtracts[runs + 2];
            // Point LocationTile = new Point(int.Parse(warpExtracts[runs + 3]), int.Parse(warpExtracts[runs + 4]));
            warpLocation.Add(tile,locationName);
            runs += 5;
        }

        string s = "";
        foreach (var kvp in warpLocation)
        {
            Logger.Info(kvp.Key.ToString());
            Logger.Info(kvp.Value);
            s +=  "\n" + kvp.Value + ": " + kvp.Key;
        }

        return s;
    }
}