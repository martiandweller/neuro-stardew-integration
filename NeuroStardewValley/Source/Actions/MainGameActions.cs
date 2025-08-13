using Microsoft.Xna.Framework;
using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Websocket;
using NeuroStardewValley.Debug;
using NeuroStardewValley.Source.RegisterActions;
using NeuroStardewValley.Source.Utilities;
using StardewBotFramework.Source.Modules.Pathfinding.Algorithms;
using StardewBotFramework.Source.Modules.Pathfinding.Base;
using StardewValley;
using Object = StardewValley.Object;

namespace NeuroStardewValley.Source.Actions;

public static class MainGameActions
{
    public class Pathfinding : NeuroAction<Goal?>
    {
        private bool _destructive;

        public override string Name => "move_character";

        protected override string Description =>
            "This will move the character to the provided tile location in the world.";

        protected override JsonSchema Schema => new()
        {
            Type = JsonSchemaType.Object,
            Required = new List<string> { "x_position", "y_position" },
            Properties = new Dictionary<string, JsonSchema>
            {
                ["x_position"] = QJS.Type(JsonSchemaType.Integer),
                ["y_position"] = QJS.Type(JsonSchemaType.Integer),
                ["destructive"] = QJS.Type(JsonSchemaType.Boolean)
            }
        };

        protected override ExecutionResult Validate(ActionData actionData, out Goal? goal)
        {
            string? xStr = actionData.Data?.Value<string>("x_position");
            string? yStr = actionData.Data?.Value<string>("y_position");
            bool? destructive = actionData.Data?.Value<bool>("destructive");

            Logger.Info($"data: {xStr}  yData: {yStr}");

            if (xStr is null || yStr is null || destructive is null)
            {
                Logger.Error($"data or yData is null");
                goal = new Goal();
                return ExecutionResult.Failure($"A value you gave was null");
            }

            if (!int.TryParse(xStr, out int x) || !int.TryParse(yStr, out int y))
            {
                Logger.Error("Invalid or missing x/y position values.");
                goal = null;
                return ExecutionResult.Failure("Invalid or missing x/y position values.");
            }

            if (!TileUtilities.IsValidTile(new Point(x, y), out var reason, _destructive))
            {
                goal = null;
                return ExecutionResult.Failure(reason);
            }
            
            goal = new Goal.GoalPosition(int.Parse(xStr), int.Parse(yStr));
            _destructive = (bool)destructive;
            return ExecutionResult.Success();
        }

        protected override void Execute(Goal? goal)
        {
            if (goal is null) return; // probably find

            Task.Run(async () => await ExecuteFunctions(goal));
        }

        private async Task ExecuteFunctions(Goal goal)
        {
            await Main.Bot.Pathfinding.Goto(goal, false, _destructive);
            RegisterMainGameActions.RegisterPostAction();
        }
    }

    public class PathFindToExit : NeuroAction<Goal?> // TODO: remove and resend when location changes
    {
        private bool _destructive;
        private GameLocation sentLocation;

        private IEnumerable<string> ExitStrings(List<Point> exits)
        {
            sentLocation = Game1.currentLocation;
            IEnumerable<string> exitStrings = new List<string>();
            foreach (var point in exits)
            {
                string exitFormat = $"{point.X},{point.Y}";
                exitStrings = exitStrings.Append(exitFormat);
            }

            return exitStrings;
        }
        public override string Name => "move_to_exit";

        protected override string Description =>
            "This will move the character to the provided tile to go to an exit";

        protected override JsonSchema Schema => new()
        {
            Type = JsonSchemaType.Object,
            Required = new List<string> { "exit" },
            Properties = new Dictionary<string, JsonSchema>
            {
                ["exit"] = QJS.Enum(ExitStrings(GetExits())),
                ["destructive"] = QJS.Type(JsonSchemaType.Boolean)
            }
        };

        protected override ExecutionResult Validate(ActionData actionData, out Goal? goal)
        {
            string? pointStr = actionData.Data?.Value<string>("exit");
            bool? destructive = actionData.Data?.Value<bool>("destructive");

            Logger.Info($"data: {pointStr}");
            if (!Game1.currentLocation.Equals(sentLocation))
            {
                goal = null;
                return ExecutionResult.ModFailure($"This action has been called in a different location than it was registered. This is most likely an issue with the integration");
            }
            
            if (pointStr is null || destructive is null)
            {
                Logger.Error($"data or yData is null");
                goal = new Goal();
                return ExecutionResult.Failure($"A value you gave was null");
            }
            
            string[] coords = pointStr.Split(',');

            Point exitPoint = new Point(int.Parse(coords[0]), int.Parse(coords[1]));

            if (!GetExits().Contains(exitPoint))
            {
                goal = null;
                return ExecutionResult.Failure($"The provided tile is not an exit");
            }

            if (exitPoint.X > Game1.currentLocation.Map.DisplayWidth / Game1.tileSize || exitPoint.X < 0 ||
                exitPoint.Y > Game1.currentLocation.Map.DisplayWidth / Game1.tileSize || exitPoint.Y < 0)
            {
                Logger.Error($"Values are invalid due to either being larger than map size or less than 0");
                goal = null;
                return ExecutionResult.Failure($"The value was either less than 0 or greater than the size of the map. If you were provided this position by the game, it is an issue with the mod.");
            }

            Main.Bot.Pathfinding.BuildCollisionMap();
            AlgorithmBase.IPathing pathing = new AStar.Pathing();
            if (Main.Bot.Pathfinding.IsBlocked(exitPoint.X, exitPoint.Y) && (bool)!destructive)
            {
                goal = null;
                return ExecutionResult.Failure("You gave a position that is blocked. Maybe try something else!");
            }

            if (pathing.FindPath(new PathNode(Game1.player.TilePoint.X, Game1.player.TilePoint.Y, null),
                    new Goal.GoalPosition(exitPoint.X, exitPoint.Y), Game1.currentLocation, 10000,_destructive).Result.Count == 0)
            {
                goal = null;
                return ExecutionResult.Failure("You cannot make it to this exit, you should try something else.");
            }

            goal = new Goal.GoalPosition(exitPoint.X,exitPoint.Y);
            _destructive = (bool)destructive;
            return ExecutionResult.Success();
        }

        protected override void Execute(Goal? goal)
        {
            if (goal is null) return; // probably fine

            Task.Run(async () => await ExecuteFunctions(goal));
        }
        
        private async Task ExecuteFunctions(Goal goal)
        {
            await Main.Bot.Pathfinding.Goto(goal, false, _destructive);
            // TODO: add check for if character does not make it in a select amount of time to prevent softlock. 
        }

        private List<Point> GetExits()
        {
            string warps = GetWarpTiles(Game1.currentLocation);
            string[] warpExtracts = warps.Split(' ');
            List<Point> warpLocation = new();
            int runs = 0;
            for (int i = 0; i < warpExtracts.Length / 5; i++)
            {
                Point tile = new Point(int.Parse(warpExtracts[runs]), int.Parse(warpExtracts[runs + 1]));
                
                warpLocation.Add(tile);
                runs += 5;
            }

            return warpLocation;
        }
        
        private static string GetWarpTiles(GameLocation location)
        {
            if (location.Name == "Farm")
            {
                location.TryGetMapProperty("FarmHouseEntry", out var FarmHousewarps);
                location.TryGetMapProperty("ShippingBinLocation", out var ShippingBin);
            }
            location.TryGetMapProperty("Warp", out var warps);
            return warps;
        }
    }

    public class UseItem : NeuroAction<Item?>
    {
        private static bool _pathfind;
        private static string _direction = "";
        private static Point _tile = new();

        private readonly IEnumerable<string> _directions = new[] { "north", "east", "south", "west" };

        public override string Name => "use_item";

        protected override string Description => "This will use the currently selected item in a specified direction.";

        protected override JsonSchema Schema => new()
        {
            Type = JsonSchemaType.Object,
            Required = new List<string> { "item", "direction" },
            Properties = new Dictionary<string, JsonSchema>
            {
                ["item"] = QJS.Enum(GetAvailableItems()),
                ["direction"] = QJS.Enum(_directions),
                ["tile_x"] = QJS.Type(JsonSchemaType.Integer),
                ["tile_y"] = QJS.Type(JsonSchemaType.Integer)
            }
        };

        protected override ExecutionResult Validate(ActionData actionData, out Item? selectedItem)
        {
            string? item = actionData.Data?.Value<string>("item");
            string? direction = actionData.Data?.Value<string>("direction");
            string? xStr = actionData.Data?.Value<string>("tile_x");
            string? yStr = actionData.Data?.Value<string>("tile_y");

            Logger.Info($"item: {item}   direction: {direction}   xStr: {xStr}    yStr: {yStr}");
            Console.WriteLine($"item: {item}   direction: {direction}   xStr: {xStr}    yStr: {yStr}");

            if (item is null)
            {
                selectedItem = null;
                return ExecutionResult.Failure($"You have not provided the item to use");
            }

            string[] items = GetAvailableItems().ToArray();
            if (!items.Contains(item)) ExecutionResult.Failure($"{item} is not a valid item");

            if (direction is not null && xStr is not null && yStr is not null)
            {
                _direction = direction!;
                _pathfind = true;
            }
            else if (direction is not null && xStr is null && yStr is null)
            {
                _direction = direction;
                _pathfind = false;
            }
            else
            {
                ExecutionResult.Failure($"This combination of arguments is not allowed");
            }

            selectedItem = null;
            foreach (var tool in Main.Bot.Inventory.GetInventory())
            {
                if (tool is null) continue;

                if (tool.Name == item)
                {
                    selectedItem = tool;
                    break;
                }

                selectedItem = null;
            }

            if (selectedItem is null)
            {
                return ExecutionResult.Failure($"the item you tried to use could not be found in your inventory");
            }

            return ExecutionResult.Success();
        }

        protected override async void Execute(Item? selectedItem)
        {
            for (int i = 0; i < Main.Bot.Inventory.GetInventory().Count; i++)
            {
                if (Main.Bot.Inventory.GetInventory()[i] is null)
                {
                    Logger.Info($"item at {i} is null");
                    continue;
                }

                Logger.Info($"{Main.Bot.Inventory.GetInventory()[i].Name} is at {i}");
            }

            int index = Main.Bot.Inventory.GetInventory().ToList().IndexOf(selectedItem);

            if (index > 11) // first line
            {
                Main.Bot.Inventory.SelectInventoryRowForToolbar(true);
                if (index > 23) // second line
                {
                    Main.Bot.Inventory.SelectInventoryRowForToolbar(true);
                }
            }

            int itemIndex = Main.Bot.Inventory.GetInventory().IndexOf(selectedItem);
            Main.Bot.Inventory.SelectSlot(itemIndex);

            if (_pathfind)
            {
                await Main.Bot.Pathfinding.Goto(new Goal.GoalPosition(_tile.X, _tile.Y), false); // get direction of final this to point
                int direction = _directions.ToList().IndexOf(_direction);
                Main.Bot.Tool.UseTool(direction);
            }
            else
            {
                int direction = _directions.ToList().IndexOf(_direction);
                Logger.Info($"direction int: {direction}");
                Main.Bot.Tool.UseTool(direction);
            }

            RegisterMainGameActions.RegisterPostAction();
        }

        private static IEnumerable<string> GetAvailableItems()
        {
            foreach (var item in Main.Bot.PlayerInformation.Inventory)
            {
                if (item is Tool)
                {
                    yield return item.Name;
                }
            }
        }
    }
    
    public class InteractWithFurniture : NeuroAction<Object>
    {
        public override string Name => "interact_object";

        protected override string Description =>
            "Will interact with an object, This should primarily be used with furniture";

        protected override JsonSchema Schema => new()
        {
            Type = JsonSchemaType.Object,
            Required = new List<string> { "object_tile_x","object_tile_y" },
            Properties = new Dictionary<string, JsonSchema>
            {
                ["object_tile_x"] = QJS.Type(JsonSchemaType.Integer),
                ["object_tile_y"] = QJS.Type(JsonSchemaType.Integer),
            }
        };
        protected override ExecutionResult Validate(ActionData actionData, out Object? resultData)
        {
            int? objectTileX = actionData.Data?.Value<int>("object_tile_x");
            int? objectTileY = actionData.Data?.Value<int>("object_tile_y");
            
            if (objectTileX is null || objectTileY is null)
            {
                resultData = null;
                return ExecutionResult.Failure($"You have provided a null value.");
            }
            if (Main.Bot.ObjectInteraction.GetObjectAtTile((int)objectTileX, (int)objectTileY) is null)
            {
                resultData = null;
                return ExecutionResult.Failure($"There is no object at the provided tile.");
            }

            resultData = Main.Bot.ObjectInteraction.GetObjectAtTile((int)objectTileX, (int)objectTileY);
            return ExecutionResult.Success();
        }

        protected override void Execute(Object? resultData)
        {
            if (resultData is null) return;
            
            Main.Bot.ObjectInteraction.InteractWithObject(resultData);
        }
    }
}