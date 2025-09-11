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

namespace NeuroStardewValley.Source.Actions;

public static class PathFindingActions
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
            await Main.Bot.Pathfinding.Goto(goal, _destructive);
            RegisterMainGameActions.RegisterPostAction();
        }
    }

    public class PathFindToExit : NeuroAction<Goal?>
    {
        private bool _destructive;
        private GameLocation? _sentLocation;

        private IEnumerable<string> ExitStrings(List<Point> exits)
        {
            _sentLocation = Game1.currentLocation;
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
            if (!Game1.currentLocation.Equals(_sentLocation))
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

            if (exitPoint.X > Game1.currentLocation.Map.DisplayWidth / Game1.tileSize || exitPoint.X < -1 || // some exits are at -1 IDK why I hate it
                exitPoint.Y > Game1.currentLocation.Map.DisplayWidth / Game1.tileSize || exitPoint.Y < -1)
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
            await Main.Bot.Pathfinding.Goto(goal, _destructive);
        }

        private List<Point> GetExits()
        {
            string warps = GetWarpTiles(Game1.currentLocation);
            if (string.IsNullOrEmpty(warps)) return new List<Point>();
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
                location.TryGetMapProperty("FarmHouseEntry", out _);
                location.TryGetMapProperty("ShippingBinLocation", out _);
            }
            location.TryGetMapProperty("Warp", out var warps);
            return warps;
        }
    }

    public class GoToCharacter : NeuroAction<KeyValuePair<NPC,bool>>
    {
        public override string Name => "go_to_character";
        protected override string Description => "Go to a character that is in this location.";
        protected override JsonSchema Schema => new()
        {
            Type = JsonSchemaType.Object,
            Required = new List<string> { "character","interact" },
            Properties = new Dictionary<string, JsonSchema>
            {
                ["character"] = QJS.Enum(Game1.currentLocation.characters.Select(npc => npc.Name).ToList()),
                ["interact"] = QJS.Type(JsonSchemaType.Boolean)
            }
        };
        protected override ExecutionResult Validate(ActionData actionData, out KeyValuePair<NPC,bool> resultData)
        {
            string? charName = actionData.Data?.Value<string>("character");
            bool? interact = actionData.Data?.Value<bool>("interact");

            resultData = new();
            if (string.IsNullOrEmpty(charName) || interact is null)
            {
                ExecutionResult.Failure($"You provided either an empty or null string");
            }

            int index = Game1.currentLocation.characters.Select(npc => npc.Name).ToList().IndexOf(charName!);
            if (index == -1)
            {
                return ExecutionResult.Failure($"The value you provided was invalid.");
            }
            
            resultData = new(Game1.currentLocation.characters[index],interact!.Value);
            return ExecutionResult.Success();
        }

        protected override void Execute(KeyValuePair<NPC,bool> resultData)
        {
            Task.Run(async () =>
            {
                await Main.Bot.Pathfinding.Goto(new Goal.GoalDynamic(resultData.Key, 1));
                if (resultData.Value)
                {
                    Main.Bot.Characters.InteractWithCharacter(resultData.Key);
                }
                RegisterMainGameActions.RegisterPostAction(); // should not get registered if character starts talking
            });
        }
    }
}