using Microsoft.Xna.Framework;
using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Websocket;
using NeuroStardewValley.Debug;
using NeuroStardewValley.Source.RegisterActions;
using NeuroStardewValley.Source.Utilities;
using StardewBotFramework.Source.Modules.Pathfinding.Base;
using StardewBotFramework.Source.ObjectDestruction;
using StardewValley;
using StardewValley.Tools;

namespace NeuroStardewValley.Source.Actions;

public static class ToolActions
{
	
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
                await Main.Bot.Pathfinding.Goto(new Goal.GoalPosition(_tile.X, _tile.Y)); // get direction of final this to point
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
    
	
	public class RefillWateringCan : NeuroAction
	{
		public override string Name => "refill_watering_can";
		protected override string Description => "This will attempt to refill your watering can in the nearest water.";
		protected override JsonSchema Schema => new ();
		protected override ExecutionResult Validate(ActionData actionData)
		{
			return ExecutionResult.Success("Refilling watering can.");
		}

		protected override void Execute()
		{
			Task.Run(async () => await ExecuteFunctions());
			
		}
		private static async Task ExecuteFunctions()
		{
			await Main.Bot.Tool.RefillWateringCan();
			RegisterMainGameActions.RegisterPostAction();
		}
	}
	
	public class DestroyObject : NeuroAction<Point>
	{
		public override string Name => "destroy_object";
		protected override string Description => "Destroy an object at the provided position.";
		protected override JsonSchema? Schema => new()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "tile_x","tile_y" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["tile_x"] = QJS.Type(JsonSchemaType.Integer),
				["tile_y"] = QJS.Type(JsonSchemaType.Integer)
			}
		};
		protected override ExecutionResult Validate(ActionData actionData, out Point resultData)
		{
			string? xStr = actionData.Data?.Value<string>("tile_x");
			string? yStr = actionData.Data?.Value<string>("tile_y");

			if (xStr is null || yStr is null)
			{
				resultData = new Point(-1, -1);
				return ExecutionResult.Failure("You did not provide a correct tile_x or tile_y schema");
			}

			int x = int.Parse(xStr);
			int y = int.Parse(yStr);
			if (TileUtilities.MaxX < x ||
			    TileUtilities.MaxY < y)
			{
				resultData = new Point(-1, -1);
				return ExecutionResult.Failure("The value you provided is larger than the map");
			}

			if (x < 0 || y < 0)
			{
				resultData = new Point(-1, -1);
				return ExecutionResult.Failure("You should not provide a value less than 0.");
			}

			if (DestroyLitterObject.IsDestructible(new Point(x, y)) ||
			    DestroyResourceClump.IsDestructible(new Point(x, y)) ||
			    DestroyTerrainFeature.IsDestructible(new Point(x, y)))
			{
				resultData = new Point(x, y);
				return ExecutionResult.Success($"You are now going to destroy the object at: {new Point(x,y)}");	
			}

			resultData = new Point(-1, -1);
			return ExecutionResult.Failure("You either cannot destroy the object at that location, or there is not object to destroy.");
		}

		protected override void Execute(Point resultData)
		{
			Task.Run(async () => await ExecuteFunctions(resultData));
		}
		private static async Task ExecuteFunctions(Point point)
		{
			await Main.Bot.Tool.RemoveObject(point);
			RegisterMainGameActions.RegisterPostAction();
		}
	}

	public class WaterFarmLand : NeuroAction<List<int>>
	{
		public override string Name => "water_farm_land";
		protected override string Description => "This will water farm land in the provided region, the farmland will not be watered if it has already been";
		protected override JsonSchema Schema => new()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "left_x","top_y","right_x","bottom_y" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["left_x"] = QJS.Type(JsonSchemaType.Integer),
				["top_y"] = QJS.Type(JsonSchemaType.Integer),
				["right_x"] = QJS.Type(JsonSchemaType.Integer),
				["bottom_y"] = QJS.Type(JsonSchemaType.Integer),
			}
		};
		protected override ExecutionResult Validate(ActionData actionData, out List<int> resultData)
		{
			string? leftXStr = actionData.Data?.Value<string>("left_x");
			string? topYStr = actionData.Data?.Value<string>("top_y");
			string? rightXStr = actionData.Data?.Value<string>("right_x");
			string? bottomYStr = actionData.Data?.Value<string>("bottom_y");

			resultData = new ();
			if (leftXStr is null || topYStr is null || rightXStr is null || bottomYStr is null)
			{
				return ExecutionResult.Failure("You did not provide a correct schema");
			}

			int leftX = int.Parse(leftXStr);
			int topY = int.Parse(topYStr);
			int rightX = int.Parse(rightXStr);
			int bottomY = int.Parse(bottomYStr);
			if (leftX < 0 || topY < 0 || rightX < 0 || bottomY < 0) 
			{
				return ExecutionResult.Failure("The value you provided is less than 0");
			}
			
			if (rightX > TileUtilities.MaxX || bottomY > TileUtilities.MaxY) 
			{
				return ExecutionResult.Failure("The value you provided is larger than the map");
			}
			
			resultData = new () {leftX,topY,rightX,bottomY};
			return ExecutionResult.Success($"You are now watering the farm-land");
		}

		protected override void Execute(List<int>? resultData)
		{
			if (resultData is null) return;
			
			Task.Run(async () => await ExecuteFunctions(resultData));
		}
		
		private static async Task ExecuteFunctions(List<int> resultData)
		{
			await Main.Bot.Tool.WaterSelectPatches(resultData[0],resultData[1],resultData[2],resultData[3]);
			RegisterMainGameActions.RegisterPostAction();
		}
	}

	public class Fishing : NeuroAction<int>
	{
		public override string Name => "use_fishing_rod";
		protected override string Description => "Power should be between 1 and 100 if the value provided does not adhere to that the value will be clamped." +
		                                         " You must be looking towards water tiles to fish or it will not work.";

		protected override JsonSchema Schema => new()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "power" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["power"] = QJS.Type(JsonSchemaType.Integer)
			}
		};
		protected override ExecutionResult Validate(ActionData actionData, out int resultData)
		{
			int? selectedPower = actionData.Data?.Value<int>("power");

			resultData = -1;
			if (!Game1.player.Items.Any(item => item is FishingRod))
			{
				return ExecutionResult.Failure($"You do not have a fishing rod in your inventory, you can buy one from the beach.");
			}
			int power = 100;
			Logger.Info($"power: {selectedPower}");
			if (selectedPower is not null && selectedPower != 0) 
			{
				power = selectedPower.Value;
			}

			if (power > 100 || power < 1)
			{
				power = Math.Clamp(power, 1, 100);
			}

			int x = Game1.player.TilePoint.X;
			int y = Game1.player.TilePoint.Y;
			Point startValue;
			switch (Game1.player.FacingDirection)
			{
				case 0:
					startValue = new Point(x,y + 3);
					break;
				case 1:
					startValue = new Point(x - 3,y);
					break;
				case 2:
					startValue = new Point(x,y - 3);
					break;
				case 4:
					startValue = new Point(x + 3,y);
					break;
				default:
					startValue = new Point(x,y);
					break;
			}
			if (!Game1.currentLocation.isTileFishable(startValue.X,startValue.Y))
			{
				return ExecutionResult.Failure($"You cannot fish at the provided tile.");
			}

			resultData = power;
			return ExecutionResult.Success($"You are now going to be fishing with the power: {power}");
		}

		protected override void Execute(int resultData)
		{
			Task.Run(async () =>
			{
				if (!Main.Bot.FishingBar.Fish(resultData))
				{
					RegisterMainGameActions.RegisterPostAction();
				}

				await Task.Delay(1500);
				if (Game1.player.CurrentTool is FishingRod rod && (rod.isCasting || rod.isFishing || rod.isNibbling || rod.isReeling) )
				{
					return;
				}
				RegisterMainGameActions.RegisterPostAction(); // could not fish due to small pond
			});
		}
	}
}