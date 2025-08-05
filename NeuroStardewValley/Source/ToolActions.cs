using Microsoft.Xna.Framework;
using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Websocket;
using StardewBotFramework.Source;
using StardewBotFramework.Source.ObjectDestruction;
using StardewValley;

namespace NeuroStardewValley.Source;

public class ToolActions
{
	public class RefillWateringCan : NeuroAction
	{
		public override string Name => "refill_watering_can";
		protected override string Description => "This will attempt to refill your watering can in the nearest water.";
		protected override JsonSchema? Schema => new ();
		protected override ExecutionResult Validate(ActionData actionData)
		{
			return ExecutionResult.Success();
		}

		protected override void Execute()
		{
			ModEntry.Bot.Tool.RefillWateringCan();
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
			if (Game1.currentLocation.Map.DisplayWidth / Game1.tileSize < x ||
			    Game1.currentLocation.Map.DisplayHeight / Game1.tileSize < y)
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
			ModEntry.Bot.Tool.RemoveObject(resultData);
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

			if (leftXStr is null || topYStr is null || rightXStr is null || bottomYStr is null)
			{
				resultData = new ();
				return ExecutionResult.Failure("You did not provide a correct schema");
			}

			int leftX = int.Parse(leftXStr);
			int topY = int.Parse(topYStr);
			int rightX = int.Parse(rightXStr);
			int bottomY = int.Parse(bottomYStr);
			if (leftX < 0 || topY < 0 || rightX < 0 || bottomY < 0) 
			{
				resultData = new ();
				return ExecutionResult.Failure("The value you provided is less than 0");
			}
			
			if (leftX > Game1.currentLocation.Map.DisplayWidth / Game1.tileSize ||
			    topY > Game1.currentLocation.Map.DisplayHeight / Game1.tileSize || 
				rightX > Game1.currentLocation.Map.DisplayWidth / Game1.tileSize || 
			    bottomY > Game1.currentLocation.Map.DisplayHeight / Game1.tileSize ) 
			{
				resultData = new ();
				return ExecutionResult.Failure("The value you provided is larger than the map");
			}

			resultData = new () {leftX,topY,rightX,bottomY};
			return ExecutionResult.Success($"You are now watering the farm-land"); // TODO: the action result will get sent twice one successful other false as action could not be unregistered
		}

		protected override void Execute(List<int>? resultData) //TODO: action window is closing and unregistering actions before action is run
		{
			// ModEntry.Bot.Tool.WaterAllPatches();
			// await ModEntry.Bot.Tool.WaterSelectPatches(resultData[0],resultData[1],resultData[2],resultData[3]);
		}
	}
}