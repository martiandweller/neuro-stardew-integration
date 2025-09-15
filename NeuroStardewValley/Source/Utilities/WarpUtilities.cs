using Microsoft.Xna.Framework;
using NeuroStardewValley.Debug;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using Object = StardewValley.Object;

namespace NeuroStardewValley.Source.Utilities;

public static class WarpUtilities
{
    public static HashSet<Point> ActionableTiles = new();
    public static List<string> GetTilesInLocation(GameLocation location, Character? startTile = null,int radius = 0)
    {
        List<string> tileList = new() {"These tiles are sent in the format of X,Y with a \\n separating each tile." +
                                       " If a tile has an action you can try to use it with the interact_with_tile action"};
        HashSet<Building> sentBuildings = new();
        WaterTiles.WaterTileData[,] waterTileData = {};
        if (location.waterTiles is not null)
        {
            waterTileData = location.waterTiles.waterTiles;
        }
        
        int maxX = location.Map.DisplayWidth / Game1.tileSize;
        int maxY = location.Map.DisplayHeight / Game1.tileSize;
        
        Rectangle rangeRect = new();
        if (startTile is not null)
        {
            rangeRect = new(Math.Clamp((startTile.TilePoint.X - radius) * 64,0,maxX * 64),
                Math.Clamp((startTile.TilePoint.Y - radius) * 64,0,maxY * 64),
                (radius * 2) * 64, (radius * 2) * 64); // this should reach to startTile.x + radius
        }

        Logger.Info($"map size X: {maxX}  maxY: {maxY}");

        for (int x = 0; x < maxX; x++)
        {
            for (int y = 0; y < maxY; y++)
            {
                Rectangle rect = new Rectangle(x * Game1.tileSize, y * Game1.tileSize, Game1.tileSize, Game1.tileSize);
                if (startTile is not null && !rangeRect.Intersects(rect)) // is outside of range
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
                    string tileString = $"Block: {x},{y}";
                    if (location.isActionableTile(x, y, Main.Bot._farmer))
                    {
                        tileString += " has an action";
                        ActionableTiles.Add(new Point(x, y));
                        string[] action = ArgUtility.SplitBySpace(Main.Bot._currentLocation.doesTileHaveProperty(x, y, "Action", "Buildings"));
                        if (action.Length < 1)
                        {
                            tileList.Add(tileString);
                            continue;
                        }
                        
                        switch (action[0])
                        {
                            case "Dialogue":
                            case "Message":
                            case "MessageOnce":
                            case "NPCMessage":
                            case "MessageSpeech":
                                tileList.Add(tileString);
                                continue;
                            case "Letter":
                                tileList.Add(action[0]);
                                continue;
                        }
                        tileString += $": {string.Join(" ", action)}";
                        Logger.Info($"tile: {x},{y}  length: {action.Length}  action: {string.Concat(action.ToList())}");                            
                    }
                    tileList.Add(tileString);
                    continue;
                }

                switch (obj)
                {
                    case Chest chest:
                        string name = chest.giftbox.Value ? "Gift-box" : "Chest";
                        string tileString = $"{name} tile: {x},{y}";
                        if (!chest.giftbox.Value) tileString += $", colour: {chest.getCategoryColor()}";
                        tileList.Add(tileString);
                        break;
                    case Object objectValue:
                        tileList.Add($"Tile: {x},{y}, name: {objectValue.Name}, Type: {objectValue.Type}");
                        break;
                    case Building building:
                        if (building.isActionableTile(x, y, Main.Bot._farmer))
                        {
                            tileList.Add($"{x},{y} has an action for the {StringUtilities.TokenizeBuildingName(building)}");
                            break;
                        }
                        if (!sentBuildings.Add(building)) continue; // we do this as buildings take up multiple tiles
                        int buildX = building.tileX.Value;
                        int buildY = building.tileY.Value;
                        Point humanDoor = building.getPointForHumanDoor();
                        string contextString = $"The top left tile of the {StringUtilities.TokenizeBuildingName(building)} is: {buildX},{buildY}." +
                                            $" the bottom right is {buildX + building.tilesWide.Value}, {buildY + building.tilesHigh.Value}. ";
                        if (humanDoor != new Point(-1,-1)) contextString += $" The door is at {humanDoor.X},{humanDoor.Y}.";
                        if (building.animalDoor.Value != new Point(-1, -1)) 
                            contextString += $" The animal door is at: {building.animalDoor.Value}.";
                        tileList.Add(contextString);
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
    
    private static object? GetTileType(GameLocation location,Point tile)
    {
        if (location.Objects.ContainsKey(tile.ToVector2()))
        {
            return location.Objects[tile.ToVector2()];
        }

        foreach (var building in location.buildings)
        {
            if (building.occupiesTile(tile.ToVector2()))
            {
                return building;
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

    private static Dictionary<Point, string> GetWarpsAsPoint(string warps)
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