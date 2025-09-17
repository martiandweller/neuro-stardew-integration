using Microsoft.Xna.Framework;
using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Websocket;
using NeuroStardewValley.Debug;
using NeuroStardewValley.Source.RegisterActions;
using NeuroStardewValley.Source.Utilities;
using StardewBotFramework.Source;
using StardewBotFramework.Source.Modules.Pathfinding.Algorithms;
using StardewBotFramework.Source.Modules.Pathfinding.Base;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Monsters;

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
        public override string Name => "move_to_exit";
        protected override string Description =>
            "This will move the character to the provided tile to go to an exit";
        protected override JsonSchema Schema => new()
        {
            Type = JsonSchemaType.Object,
            Required = new List<string> { "exit" },
            Properties = new Dictionary<string, JsonSchema>
            {
                ["exit"] = QJS.Enum(WarpUtilities.GetWarpTilesStrings(WarpUtilities.GetWarpTiles(Main.Bot._currentLocation,true))),
                ["destructive"] = QJS.Type(JsonSchemaType.Boolean)
            }
        };

        protected override ExecutionResult Validate(ActionData actionData, out Goal? goal)
        {
            string? pointStr = actionData.Data?.Value<string>("exit");
            bool? destructive = actionData.Data?.Value<bool>("destructive");

            Logger.Info($"data: {pointStr}");
            goal = null;
            if (!Game1.currentLocation.Equals(Main.Bot._currentLocation))
            {
                return ExecutionResult.ModFailure($"This action has been called in a different location than it was registered. This is most likely an issue with the integration");
            }
            
            if (pointStr is null || destructive is null)
            {
                Logger.Error($"data or yData is null");
                return ExecutionResult.Failure($"A value you gave was null");
            }
            
            string[] coords = pointStr.Split(',');

            Point exitPoint = new Point(int.Parse(coords[0]), int.Parse(coords[1]));

            if (!WarpUtilities.GetWarpsAsPoint(WarpUtilities.GetWarpTiles(Main.Bot._currentLocation,true)).ContainsKey(exitPoint))
            { 
                return ExecutionResult.Failure($"The provided tile is not an exit");
            }

            if (exitPoint.X > TileUtilities.MaxX || exitPoint.X < -1 || // some exits are at -1
                exitPoint.Y > TileUtilities.MaxY || exitPoint.Y < -1)
            {
                Logger.Error($"Values are invalid due to either being larger than map size or less than 0");
                return ExecutionResult.Failure($"The value was either less than 0 or greater than the size of the map. If you were provided this position by the game, it is an issue with the mod.");
            }

            Main.Bot.Pathfinding.BuildCollisionMap();
            AlgorithmBase.IPathing pathing = new AStar.Pathing();
            if (Main.Bot.Pathfinding.IsBlocked(exitPoint.X, exitPoint.Y) && (bool)!destructive)
            {
                return ExecutionResult.Failure("You gave a position that is blocked. Maybe try something else!");
            }

            if (pathing.FindPath(new PathNode(Main.Bot._farmer.TilePoint.X, Main.Bot._farmer.TilePoint.Y, null),
                    new Goal.GoalPosition(exitPoint.X, exitPoint.Y), Game1.currentLocation, 10000,_destructive).Result.Count == 0)
            {
                return ExecutionResult.Failure("You cannot make it to this exit, you should try something else.");
            }

            goal = new Goal.GoalPosition(exitPoint.X,exitPoint.Y);
            _destructive = (bool)destructive;
            return ExecutionResult.Success();
        }

        protected override void Execute(Goal? goal)
        {
            if (goal is null) return; // probably fine
            Logger.Warning($"post execute null check");

            Task.Run(async () => await ExecuteFunctions(goal));
        }
        
        private async Task ExecuteFunctions(Goal goal)
        {
            Logger.Warning($"async execute functions");
            Vector2 vec2 = goal.VectorLocation.ToVector2() * 64;
            var buildings = Main.Bot._currentLocation.buildings
                .Where(building => building.GetBoundingBox().Contains(vec2)).ToList();
            if (buildings.Count > 0)
            {
                Building building = buildings[0];
                Point door = building.getPointForHumanDoor();
                await Main.Bot.Pathfinding.Goto(new Goal.GetToTile(door.X,door.Y), _destructive);
                Main.Bot.Building.UseHumanDoor(building);
                RegisterMainGameActions.RegisterPostAction();
                return;
            }
            
            await Main.Bot.Pathfinding.Goto(goal, _destructive);
            RegisterMainGameActions.RegisterPostAction();
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

    public class AttackMonster : NeuroAction<Monster>
    {
        public override string Name => "attack_monster";
        protected override string Description => "Select a monster to attack.";

        protected override JsonSchema Schema => new()
        {
            Type = JsonSchemaType.Object,
            Required = new List<string> { "monster" },
            Properties = new Dictionary<string, JsonSchema>
            {
                ["monster"] = QJS.Enum(Game1.currentLocation.characters.Where(monster => monster.IsMonster)
                    .Select(monster => $"{monster.Name}").ToList())
            }
        };
        protected override ExecutionResult Validate(ActionData actionData, out Monster? resultData)
        {
            string? monsterName = actionData.Data?.Value<string>("monster");

            resultData = null;
            if (string.IsNullOrEmpty(monsterName))
            {
                return ExecutionResult.Failure($"You need to provide a monster to attack.");
            }
            
            NPC monster = Main.Bot._currentLocation.characters.Where(monster => monster.IsMonster)
                .Where(monster => $"{monster.Name}" == monsterName).ToArray()[0];

            if (monster is null)
            {
                return ExecutionResult.Failure($"That monster no longer exists in this location");
            }
            resultData = monster as Monster;
            return ExecutionResult.Success($"You are attacking a {monster.Name}");
        }

        protected override void Execute(Monster? resultData)
        {
            if (resultData is null) return;
            Task.Run(async () =>
            {                
                await Main.Bot.Pathfinding.AttackMonster(new Goal.GoalDynamic(resultData, 1));
                RegisterMainGameActions.RegisterPostAction();
            });
        }
    }
}