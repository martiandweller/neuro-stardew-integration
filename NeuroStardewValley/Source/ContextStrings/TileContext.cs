using Microsoft.Xna.Framework;
using NeuroStardewValley.Debug;
using NeuroStardewValley.Source.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using Object = StardewValley.Object;

namespace NeuroStardewValley.Source.ContextStrings;

public static class TileContext
{
    public static readonly HashSet<Point> ActionableTiles = new();
    /// <summary>
    /// Get tiles in the specified location, to be used as context.
    /// </summary>
    /// <param name="location">The <see cref="GameLocation"/> you want to get the tiles of.</param>
    /// <param name="startTile">The Character you want to base the radius off</param>
    /// <param name="radius">the radius of tiles.</param>
    public static List<string> GetTilesInLocation(GameLocation location, Point? startTile = null,int radius = 0)
    {
        string str = radius == 0 ? "" : $"closest {radius} ";
        List<string> tileList = new() {$"These are the {str}tiles in this location, they are sent in the format of X,Y with a \\n separating each tile." +
                                       " If a tile has an action you can try to use it with the interact_with_tile action." +
                                       " If a tile is \"block\" that means it has collisions."};
        WaterTiles.WaterTileData[,] waterTileData = {};
        if (location.waterTiles is not null)
        {
            waterTileData = location.waterTiles.waterTiles;
        }

        Point tile = startTile ?? new Point();
        int minX = startTile is null ? 0 : tile.X - radius;
        int minY = startTile is null ? 0 : tile.Y - radius;
        
        int maxX = startTile is null ? location.Map.DisplayWidth / Game1.tileSize : tile.X + radius;
        int maxY = startTile is null ? location.Map.DisplayHeight / Game1.tileSize : tile.Y + radius;
        
        Rectangle rangeRect = new();
        if (startTile is not null)
        {
            rangeRect = new(Math.Clamp((tile.X - radius) * 64,0,maxX * 64),
                Math.Clamp((tile.Y - radius) * 64,0,maxY * 64),
                (radius * 2) * 64, (radius * 2) * 64); // this should reach to startTile.x + radius
        }

        Logger.Info($"map size X: {maxX}  maxY: {maxY}");

        for (int x = minX; x < maxX; x++)
        {
            for (int y = minY; y < maxY; y++)
            {
                Rectangle rect = new Rectangle(x * Game1.tileSize, y * Game1.tileSize, Game1.tileSize, Game1.tileSize);
                if (startTile is not null && !rangeRect.Intersects(rect)) continue; // is outside of range

                if (x < waterTileData.GetLength(0) && y < waterTileData.GetLength(1) && waterTileData[x, y].isWater)
                {
                    tileList.Add($"Water: {x},{y}");
                    continue;
                }
                object? obj = TileUtilities.GetTileType(location, new Point(x, y));
                
                if (!Game1.currentLocation.isCollidingPosition(rect, Game1.viewport, true, 0, 
                        false, Game1.player, true,false,false,true)
                    && obj is null)
                    continue;

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
                        Logger.Info($"tile: {x},{y}  length: {action.Length}  action: {string.Join(" ", action)}");
                    }
                    tileList.Add(tileString);
                    continue;
                }
                string? objectContext = GetObjectContext(location, x, y);
                if (objectContext is null) continue;
                
                tileList.Add(objectContext);
            }
        }
        return tileList;
    }

    public static string? GetObjectContext(GameLocation location, int x, int y)
    {
        HashSet<Building> sentBuildings = new();
        object? obj = TileUtilities.GetTileType(location, new Point(x, y));
        if (obj is null) return null;
        switch (obj)
            {
                case Chest chest:
                    string name = chest.giftbox.Value ? "Gift-box" : "Chest";
                    string tileString = $"{name} tile: {x},{y}";
                    if (!chest.giftbox.Value) tileString += $", colour: {chest.getCategoryColor()}";
                    return tileString;
                case Object objectValue:
                    return $"Tile: {x},{y}, name: {objectValue.DisplayName}, Type: {objectValue.Type}";
                case Building building:
                    if (building.isActionableTile(x, y, Main.Bot._farmer))
                    {
                        return $"{x},{y} has an action for the {StringUtilities.TokenizeBuildingName(building)}";
                    }
                    if (!sentBuildings.Add(building)) return null; // we do this as buildings take up multiple tiles
                    int buildX = building.tileX.Value;
                    int buildY = building.tileY.Value;
                    Point humanDoor = building.getPointForHumanDoor();
                    string contextString = $"The top left tile of the {StringUtilities.TokenizeBuildingName(building)} is: {buildX},{buildY}." +
                                        $" the bottom right is {buildX + building.tilesWide.Value}, {buildY + building.tilesHigh.Value}. ";
                    if (humanDoor != new Point(-1,-1)) contextString += $" The door is at {humanDoor.X},{humanDoor.Y}.";
                    if (building.animalDoor.Value != new Point(-1, -1)) 
                        contextString += $" The animal door is at: {building.animalDoor.Value}.";
                    return contextString;
                case ResourceClump resourceClump:
                    // substring will always get "ResourceClump" as object name is not a part of modData
                    int start = resourceClump.modData.Name.IndexOf('(');
                    string subStr = resourceClump.modData.Name.Substring(start + 1,
                        resourceClump.modData.Name.IndexOf(')') - start - 1);
                    return $"{subStr} is at: {x},{y}";
                case TerrainFeature terrainFeature:
                    switch (terrainFeature)
                    {
                        case HoeDirt dirt:
                            string context = $"{dirt.Tile}: empty dirt";
                            if (dirt.crop is not null)
                            {
                                Item item = ItemRegistry.Create(dirt.crop.indexOfHarvest.Value);
                                context = $"{dirt.Tile}: {item.DisplayName} fully grown: {dirt.crop.fullyGrown.Value}";
                            }
                            return context;
                        default:
                            int startIndex = terrainFeature.modData.Name.IndexOf('(');
                            string substring = terrainFeature.modData.Name.Substring(startIndex + 1,
                                terrainFeature.modData.Name.IndexOf(')') - startIndex - 1);
                            return $"{substring} is at: {x},{y}";
                    }
            }

        return "";
    }
    
    public static string GetWarpTiles(GameLocation location,bool addBuildings = false)
    {
        location.TryGetMapProperty("Warp", out var warps);
        if (!addBuildings) return warps;

        warps += GetBuildingWarps(location);
        return warps;
    }

    private static string GetBuildingWarps(GameLocation location)
    {
        string warps = "";
        foreach (var building in location.buildings)
        {
            Rectangle door = building.getRectForHumanDoor();
            if (!building.HasIndoors() || building.getPointForHumanDoor() == new Point(-1,-1)) continue;
            
            door.Inflate(128,128); // adjust by two tiles as warp teleport location typically a few tiles away from entrance
            foreach (var warp in building.GetIndoors().warps.Where(warp => door.Contains(new Vector2(warp.TargetX,warp.TargetY) * 64))) 
            {
                warps += $" {warp.TargetX} {warp.TargetY} {building.GetIndoors().Name} {warp.X} {warp.Y}";
            }
        }
        return warps;
    }

    public static Dictionary<Point, string> GetWarpsAsPoint(string warps)
    {
        string[] warpExtracts = warps.Split(' ');
        Dictionary<Point, string> warpLocation = new();
        Logger.Info($"length: {warpExtracts.Length}   {warpExtracts[0]}");
        if (warpExtracts.Length < 5) return new();
        for (int i = 0; i < warpExtracts.Length; i += 5) // divide by five to only get tile locations
        {
            Point tile = new Point(int.Parse(warpExtracts[i]), int.Parse(warpExtracts[i + 1]));
            Logger.Info($"tile: {tile}   name: {warpExtracts[i+2]}");
            
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
            s += $"\n{kvp.Value}: {kvp.Key}";
        }

        return s;
    }

    public static List<string> GetWarpTilesStrings(string warpTiles)
    {
        var warpLocation = GetWarpsAsPoint(warpTiles);
        
        List<string> s = new();
        foreach (var kvp in warpLocation)
        {
            Logger.Info($"key: {kvp.Key.ToString()}  value: {kvp.Value}");
            s.Add($"{kvp.Value}: {kvp.Key.X},{kvp.Key.Y}");
        }

        return s;
    }
}