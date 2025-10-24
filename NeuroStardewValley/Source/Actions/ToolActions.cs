using Microsoft.Xna.Framework;
using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Messages.Outgoing;
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
        private static Point _tile;
        public static readonly IEnumerable<string> Directions = new[] { "north", "east", "south", "west" };

        public override string Name => "use_item";

        protected override string Description => "This will use the currently selected item at either the specified direction or tile" +
                                                 ", if you specify a tile and direction at the end of pathfinding you will look in that direction." +
                                                 " If you use a fishing rod, you can also specify a power for it, " +
                                                 "this should be between 0 and 1 if the value you specify isn't valid it will be clamped.";
        protected override JsonSchema Schema => new()
        {
            Type = JsonSchemaType.Object,
            Required = new List<string> { "item" },
            Properties = new Dictionary<string, JsonSchema>
            {
                ["item"] = QJS.Enum(Main.Bot.Inventory.Inventory.Where(item => item is not null).Select(item => item.DisplayName).ToList()),
                ["direction"] = QJS.Enum(Directions),
                ["tile_x"] = QJS.Type(JsonSchemaType.Integer),
                ["tile_y"] = QJS.Type(JsonSchemaType.Integer),
                ["power"] = QJS.Type(JsonSchemaType.Float)
            }
        };

        protected override ExecutionResult Validate(ActionData actionData, out Item? selectedItem)
        {
            string? item = actionData.Data?.Value<string>("item");
            string? direction = actionData.Data?.Value<string>("direction");
            int? x = actionData.Data?.Value<int>("tile_x");
            int? y = actionData.Data?.Value<int>("tile_y");

            Logger.Info($"item: {item}   direction: {direction}   xStr: {x}    yStr: {y}");

            selectedItem = null;
            if (item is null)
            {
                return ExecutionResult.Failure($"You have not provided the item to use");
            }

            string[] items = Main.Bot.Inventory.Inventory.Where(item1 => item1 is not null).Select(tool => tool.DisplayName).ToArray();
            if (!items.Contains(item)) ExecutionResult.Failure($"{item} is not a valid item");

            _direction = "";
            if (!string.IsNullOrEmpty(direction))
            {
	            _direction = direction;
            }

            _tile = new();
            _pathfind = false;
            // is 0 if not specified
            if (x is not 0 and not null && y is not 0 and not null)
            {
                _tile = new((int)x, (int)y);
                _pathfind = true;
            }

            if (_direction == "" && _tile == new Point() && !_pathfind)
            {
	            return ExecutionResult.Failure($"You must specify a tile or direction or both to use this action.");
            }
            
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

            // this is for item specific validation
            string reason = $"Using {selectedItem.DisplayName}";
            switch (selectedItem)
            {
	            case FishingRod:
		            if (!FishingMethods.Validate(actionData, out var returnPower, out reason)) return ExecutionResult.Failure(reason);
		            _power = returnPower;
		            break;
            }

            return ExecutionResult.Success(reason);
        }

        private int _power;
        protected override async void Execute(Item? selectedItem)
        {
            if (selectedItem is null) return;
            
            if (_pathfind)
            {
	            try
	            {
		            await Main.Bot.Pathfinding.Goto(new Goal.GetToTile(_tile.X, _tile.Y)); // get direction of final this to point
		            await TaskDispatcher.SwitchToMainThread();
	            }
	            catch (Exception e)
	            {
		            Logger.Error($"{e}");
		            await TaskDispatcher.SwitchToMainThread();
		            RegisterMainActions.RegisterPostAction();
		            return;
	            }
            }
            
            // get to tile radius
            if (!Utility.tileWithinRadiusOfPlayer(_tile.X, _tile.Y, 1, Main.Bot._farmer))
            {
	            Context.Send($"The pathfinding could not get you withing radius to the tile you specified, you should try to do something else.");
	            RegisterMainActions.RegisterPostAction();
	            return;
            }
            
            string meleeString = "";
            if (selectedItem is MeleeWeapon) meleeString = selectedItem.Name == "Scythe" ? "Scythe" : "Weapon";
            SwapItemHandler.SwapItem(selectedItem.GetType(),meleeString);

			if (_direction != "") Main.Bot._farmer.FacingDirection = Directions.ToList().IndexOf(_direction);

            switch (selectedItem)
            {
	            case not Tool:
		            if (selectedItem is Object obj && obj.Edibility != -300)
		            {
			            Main.Bot.Player.EatHeldItem();
		            }
		            
		            // this method divides by 64.
		            if (Utility.isThereAnObjectHereWhichAcceptsThisItem(Main.Bot._currentLocation, selectedItem, _tile.X * 64, _tile.Y * 64))
		            {
			            Object objAt = Main.Bot._currentLocation.getObjectAtTile(_tile.X, _tile.Y);
			            Main.Bot.Player.AddItemToObject(objAt, Main.Bot._farmer.ActiveItem);
		            }

		            break;
		        case FishingRod:
			        FishingMethods.FishingExecute(_power);
			        break;
			    default:
		            int direction = Directions.ToList().IndexOf(_direction);
		            Main.Bot.Tool.UseTool(direction);
		            break;
            }

            RegisterMainActions.RegisterPostAction();
        }
    }

    #region UseItemExtension

    private static class FishingMethods
    {
	    public static bool Validate(ActionData actionData, out int returnPower, out string reason)
        {
	        string? selectedDirection = actionData.Data?.Value<string>("direction");
	        float? selectedPower = actionData.Data?.Value<float>("power");
	        
	        returnPower = -1;
	        if (selectedDirection is null)
	        {
		        reason = "You must specify a direction when using a fishing rod";
		        return false;
	        }
	        if (!Main.Bot._farmer.Items.Any(item => item is FishingRod))
	        {
		        reason = "You do not have a fishing rod in your inventory, you can buy one from the beach.";
		        return false;
	        }

	        if (!Main.Bot._currentLocation.canFishHere())
	        {
		        reason = "You cannot fish in this location.";
		        return false;
	        }
	        
	        float power = 1;
	        Logger.Info($"power: {selectedPower}");
	        if (selectedPower is not null && selectedPower != 0) 
	        {
		        power = selectedPower.Value;
	        }

	        if (power > 1 || power < 0)
	        {
		        power = Math.Clamp(power, 0, 1);
	        }

	        int x = Main.Bot._farmer.TilePoint.X;
	        int y = Main.Bot._farmer.TilePoint.Y;
	        Point startValue;
	        int indexDirection = UseItem.Directions.ToList().IndexOf(selectedDirection);
	        int? direction = indexDirection == -1 ? Main.Bot._farmer.FacingDirection : indexDirection;
	        switch (direction)
	        {
		        case 0:
			        startValue = new Point(x,y - 3);
			        break;
		        case 1:
			        startValue = new Point(x + 3,y);
			        break;
		        case 2:
			        startValue = new Point(x,y + 3);
			        break;
		        case 3:
			        startValue = new Point(x - 3,y);
			        break;
		        default:
			        startValue = new Point(x,y);
			        break;
	        }
	        if (!Main.Bot._currentLocation.isTileFishable(startValue.X,startValue.Y))
	        {
		        reason = $"You cannot fish at the provided tile.";
		        return false;
	        }

	        returnPower = (int)power;
	        reason = $"You are now going to be fishing with the power: {power}";
	        return true;
        }
	    
	    public static void FishingExecute(float power)
	    {
		    if (Main.Bot._farmer.CurrentItem is not FishingRod) return;

		    Main.Bot.FishingBar.Fish(power);
	    }
    }
    
    #endregion

    #region ToolSpecific
    
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
	
	#endregion

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
}