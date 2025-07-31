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

		protected override Task Execute()
		{
			ModEntry.Bot.Tool.RefillWateringCan();
			return Task.CompletedTask;
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

		protected override Task Execute(Point resultData)
		{
			ModEntry.Bot.Tool.RemoveObject(resultData);
			return Task.CompletedTask;
		}
	}
}