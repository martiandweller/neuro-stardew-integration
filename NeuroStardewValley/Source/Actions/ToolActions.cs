using Microsoft.Xna.Framework;
using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Websocket;
using NeuroStardewValley.Debug;
using NeuroStardewValley.Source.RegisterActions;
using NeuroStardewValley.Source.Utilities;
using StardewBotFramework.Source.Modules.Pathfinding.Base;
using StardewBotFramework.Source.ObjectDestruction;
using StardewBotFramework.Source.ObjectToolSwaps;
using StardewValley;
using StardewValley.Tools;
using Object = StardewValley.Object;

namespace NeuroStardewValley.Source.Actions;

public static class ToolActions
{
    public class UseItem : NeuroAction<Item?>
    {
        private static bool _pathfind;
        private static string _direction = "";
        private static readonly Point Tile = new();
        private readonly IEnumerable<string> _directions = new[] { "north", "east", "south", "west" };

        public override string Name => "use_item";

        protected override string Description => "This will use the currently selected item at either the specified direction or tile.";

        protected override JsonSchema Schema => new()
        {
            Type = JsonSchemaType.Object,
            Required = new List<string> { "item", "direction" },
            Properties = new Dictionary<string, JsonSchema>
            {
                ["item"] = QJS.Enum(Main.Bot.Inventory.Inventory.Where(item => item is not null).Select(item => item.DisplayName).ToList()),
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

            if (item is null)
            {
                selectedItem = null;
                return ExecutionResult.Failure($"You have not provided the item to use");
            }

            string[] items = Main.Bot.Inventory.Inventory.Where(item1 => item1 is not null).Select(tool => tool.DisplayName).ToArray();
            if (!items.Contains(item)) ExecutionResult.Failure($"{item} is not a valid item");

            if (direction is not null && xStr is not null && yStr is not null)
            {
                _direction = direction;
                _pathfind = true;
            }
            else if (direction is not null && xStr is null && yStr is null)
            {
                _direction = direction;
                _pathfind = false;
            }
            else
            {
                ExecutionResult.Failure($"You cannot use both direction and specify a tile position");
            }

            selectedItem = null;
            foreach (var i in Main.Bot.Inventory.Inventory)
            {
                if (i is null) continue;

                if (i.Name == item)
                {
                    selectedItem = i;
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
            if (selectedItem is null) return;
            
            string meleeString = "";
            if (selectedItem is MeleeWeapon) meleeString = selectedItem.Name == "Scythe" ? "Scythe" : "Weapon";
            SwapItemHandler.SwapItem(selectedItem.GetType(),meleeString);
            
            if (selectedItem is not Tool)
            {
	            if (selectedItem is Object obj && obj.Edibility != -300)
	            {
		            Main.Bot.Player.EatHeldItem();
	            }

	            if (Utility.isThereAnObjectHereWhichAcceptsThisItem(Main.Bot._currentLocation, selectedItem, Tile.X,
		                Tile.Y))
	            {
		            Object objAt = Main.Bot._currentLocation.getObjectAtTile(Tile.X, Tile.Y);
		            Main.Bot.Player.AddItemToObject(objAt, Main.Bot._farmer.ActiveItem);
	            }
            }
            if (_pathfind)
            {
	            try
	            {
		            await Main.Bot.Pathfinding.Goto(new Goal.GetToTile(Tile.X, Tile.Y)); // get direction of final this to point
		            await TaskDispatcher.SwitchToMainThread();
	            }
	            catch (Exception e)
	            {
		            Logger.Error($"{e}");
		            await TaskDispatcher.SwitchToMainThread();
		            RegisterMainActions.RegisterPostAction();
		            return;
	            }
	            int direction = _directions.ToList().IndexOf(_direction);
	            Main.Bot.Tool.UseTool(direction);
            }
            else
            {
	            int direction = _directions.ToList().IndexOf(_direction);
	            Logger.Info($"direction int: {direction}");
	            Main.Bot.Tool.UseTool(direction);
            }

            RegisterMainActions.RegisterPostAction();
        }
    }
	public class RefillWateringCan : NeuroAction
	{
		public override string Name => "refill_watering_can";
		protected override string Description => "This will attempt to refill your watering can at the nearest water.";
		protected override JsonSchema Schema => new ();
		protected override ExecutionResult Validate(ActionData actionData)
		{
			return ExecutionResult.Success("Refilling watering can.");
		}

		protected override void Execute()
		{
			Main.Bot.Tool.RefillWateringCan();
			RegisterMainActions.RegisterPostAction();	
		}
	}
	public class DestroyObject : NeuroAction<Point>
	{
		public override string Name => "destroy_object";
		protected override string Description => "Destroy an object at the provided position, if there is no object or the " +
		                                         "object is not able to be destroyed this action will fail.";
		protected override JsonSchema Schema => new()
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
			Main.Bot.Tool.RemoveObject(resultData);
			RegisterMainActions.RegisterPostAction();
		}
	}
	public class WaterFarmLand : NeuroAction<List<int>>
	{
		public override string Name => "water_farm_land";
		protected override string Description => "This will water farm land in the provided region, the farmland will not" +
		                                         " be watered if it has already been and will automatically refill watering can." +
		                                         " You should think of this as a rectangle containing the tiles you want to water.";
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
			
			for (int i = 0; i < resultData.Count; i++)
			{
				resultData[i] *= 64;
			}
			Rectangle rect = new(resultData[0], resultData[1], resultData[2] - resultData[0],
				(resultData[3] - resultData[1]) + 64); // add extra tile to get what is expected
			Main.Bot.Tool.WaterSelectPatches(rect);
			RegisterMainActions.RegisterPostAction();
		}
	}
	public class UseToolInRect : NeuroAction<KeyValuePair<Tool, Rectangle>>
	{
		public override string Name => "use_tool_in_rectangle";

		protected override string Description =>
			"Use the selected tool in a specified rectangle, if you want to water farmland you can do that with another action." +
			" You should use this if you want to create farmland or use another tool for a similar purpose.";
		protected override JsonSchema Schema => new()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "tool", "left_x", "top_y", "right_x", "bottom_y" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["tool"] = QJS.Enum(Main.Bot.Inventory.Inventory.Where(item => item is Tool).Select(tool => tool.DisplayName)
					.ToList()),
				["left_x"] = QJS.Type(JsonSchemaType.Integer),
				["top_y"] = QJS.Type(JsonSchemaType.Integer),
				["right_x"] = QJS.Type(JsonSchemaType.Integer),
				["bottom_y"] = QJS.Type(JsonSchemaType.Integer),
			}
		};
		protected override ExecutionResult Validate(ActionData actionData, out KeyValuePair<Tool, Rectangle> resultData)
		{
			string? toolName = actionData.Data?.Value<string>("tool");
			string? leftXStr = actionData.Data?.Value<string>("left_x");
			string? topYStr = actionData.Data?.Value<string>("top_y");
			string? rightXStr = actionData.Data?.Value<string>("right_x");
			string? bottomYStr = actionData.Data?.Value<string>("bottom_y");

			resultData = new ();
			if (toolName is null || leftXStr is null || topYStr is null || rightXStr is null || bottomYStr is null)
			{
				return ExecutionResult.Failure("You did not provide a correct schema");
			}

			List<Item> items = Main.Bot.Inventory.Inventory.Where(item1 => item1 is Tool tool && tool.DisplayName == toolName).ToList();
			if (items.Count < 1)
			{
				return ExecutionResult.Failure($"The value you provided as the tool name does not exist.");
			}
			Item toolItem = items[0];
			int leftX = int.Parse(leftXStr) * 64;
			int topY = int.Parse(topYStr) * 64;
			int rightX = int.Parse(rightXStr) * 64;
			int bottomY = int.Parse(bottomYStr) * 64;
			if (leftX < 0 || topY < 0 || rightX < 0 || bottomY < 0) 
			{
				return ExecutionResult.Failure("The value you provided is less than 0");
			}
			
			if (rightX > TileUtilities.MaxX * 64 || bottomY > TileUtilities.MaxY * 64) 
			{
				return ExecutionResult.Failure("The value you provided is larger than the map");
			}

			Tool? tool = null;
			if (toolItem is Tool item) tool = item;
			if (tool is WateringCan) return ExecutionResult.Failure($"You should use either the water_farm_land or refill_watering_can action instead.");

			if (tool is null) return ExecutionResult.Failure($"The tool you provided is not available");
			
			resultData = new (tool, new Rectangle(leftX,topY, (rightX - leftX) + 64, (bottomY - topY) + 64));
			return ExecutionResult.Success($"You are now using the {tool.Name}");
		}

		protected override void Execute(KeyValuePair<Tool, Rectangle> resultData)
		{
			SwapItemHandler.SwapItem(resultData.Key.GetType(),"");
			if (resultData.Key is Hoe)
			{
				var tiles = Main.Bot.Tool.CreateFarmLandTiles(resultData.Value);
				Main.Bot.Tool.MakeFarmLand(tiles);
				RegisterMainActions.RegisterPostAction();
				return;
			}

			if (resultData.Key is WateringCan)
			{
				Main.Bot.Tool.WaterSelectPatches(resultData.Value);
				RegisterMainActions.RegisterPostAction();
				return;
			}

			Rectangle rect = resultData.Value;
			Main.Bot.Tool.RemoveObjectsInDimension(rect);
			RegisterMainActions.RegisterPostAction();
		}
	}
	public class Fishing : NeuroAction<int>
	{
		public override string Name => "use_fishing_rod";
		protected override string Description => "Use a fishing rod in your inventory to fish, power should be between 1" +
		                                         " and 100 if the value provided does not adhere to that the value will " +
		                                         "be clamped. You must be looking towards water tiles to fish or it will not work.";

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
					RegisterMainActions.RegisterPostAction();
				}

				await Task.Delay(1500);
				if (Game1.player.CurrentTool is FishingRod rod && (rod.isCasting || rod.isFishing || rod.isNibbling || rod.isReeling) )
				{
					return;
				}
				RegisterMainActions.RegisterPostAction(); // could not fish due to small pond
			});
		}
	}
}