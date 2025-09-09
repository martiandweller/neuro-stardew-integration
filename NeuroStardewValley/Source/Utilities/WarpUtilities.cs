using Microsoft.Xna.Framework;
using NeuroStardewValley.Debug;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.TerrainFeatures;
using Object = StardewValley.Object;

namespace NeuroStardewValley.Source.Utilities;

public static class WarpUtilities
{
    public static List<string> GetTilesInLocation(GameLocation location, Character? startTile = null,int radius = 0)
    {
        List<string> tileList = new() {"These tiles are sent in the format of X,Y with a \\n separating each tile"};
        List<Building> sentBuildings = new();
        WaterTiles.WaterTileData[,] waterTileData = { };
        if (location.waterTiles is not null)
        {
            waterTileData = location.waterTiles.waterTiles;
        }
        
        int maxX = location.Map.DisplayWidth / Game1.tileSize;
        int maxY = location.Map.DisplayHeight / Game1.tileSize;
        
        Rectangle rangeRect = new();
        if (startTile is not null)
        {
            Logger.Info($"Character pos: {startTile.TilePoint}   radius: {radius}");
            rangeRect = new(Math.Clamp((startTile.TilePoint.X - radius) * 64,0,maxX * 64),
                Math.Clamp((startTile.TilePoint.Y - radius) * 64,0,maxY * 64),
                (radius * 2) * 64, (radius * 2) * 64); // this should reach to startTile.x + radius
            Logger.Info($"rect: {rangeRect}");
        }

        Logger.Info($"map size X: {maxX}  maxY: {maxY}");

        for (int x = 0; x < maxX; x++)
        {
            for (int y = 0; y < maxY; y++)
            {
                Rectangle rect = new Rectangle(x * Game1.tileSize, y * Game1.tileSize, Game1.tileSize, Game1.tileSize);
                Logger.Info($"tile rect: {rect}   range rect: {rangeRect}");
                if (startTile is not null && !rangeRect.Intersects(rect))
                {
                    continue;
                }
                if (x < waterTileData.GetLength(0) && y < waterTileData.GetLength(1) && waterTileData[x, y].isWater)
                {
                    tileList.Add($"Water: {x},{y}");
                    continue;
                }

                if (!Game1.currentLocation.isCollidingPosition(rect, Game1.viewport, true, 0, 
                        false, Game1.player, true,false,false,true))
                    continue;

                object? obj = GetTileType(location, new Point(x, y));
                if (obj is null)
                {
                    if (x < waterTileData.GetLength(0) && y < waterTileData.GetLength(1) && waterTileData[x, y].isWater)
                    {
                        tileList.Add($"Water: {x},{y}");
                        continue;
                    }

                    tileList.Add($"Blocks: {x},{y}");
                    continue;
                }

                switch (obj)
                {
                    case Object objectValue:
                        tileList.Add($"Tile: {x},{y}, name: {objectValue.Name}," +
                                     $" Type: {objectValue.Type}");
                        break;
                    case Building building:
                        if (sentBuildings.Contains(building)) continue; // we do this as buildings take up multiple tiles
                        sentBuildings.Add(building);
                        int buildX = building.tileX.Value;
                        int buildY = building.tileY.Value;
                        int buildWidth = building.tilesWide.Value;
                        int buildHeight = building.tilesHigh.Value;
                        tileList.Add($"The top left tile of the {building.buildingType.Value} is: {buildX},{buildY}." +
                                     $" the bottom right is {buildX + buildWidth}, {buildY + buildHeight}");
                        break;
                    case ResourceClump resourceClump:
                        tileList.Add($"{resourceClump.modData.Name} is at tile: {x},{y}");
                        break;
                    case TerrainFeature terrainFeature:
                        tileList.Add($"{terrainFeature.modData.Name} is at tile: {x},{y}");
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

    public static Dictionary<Point, string> GetWarpsAsPoint(string warps)
    {
        string[] warpExtracts = warps.Split(' ');
        Dictionary<Point, string> warpLocation = new();
        for (int i = 0; i < warpExtracts.Length / 5; i += 5) // divide by five to only get tile locations
        {
            Logger.Info($"tile: {warpExtracts[i]} next tile: {warpExtracts[i + 1]}");
            Point tile = new Point(int.Parse(warpExtracts[i]), int.Parse(warpExtracts[i + 1]));

            string locationName = warpExtracts[i + 2];
            warpLocation.Add(tile,locationName);
        }

        return warpLocation;
    }

    public static string GetWarpTilesString(string warpTiles)
    {
        var warpLocation = GetWarpsAsPoint(warpTiles);
        
        string s = "";
        foreach (var kvp in warpLocation)
        {
            Logger.Info($"key: {kvp.Key.ToString()}  value: {kvp.Value}");
            s +=  "\n" + kvp.Value + ": " + kvp.Key;
        }

        return s;
    }
}