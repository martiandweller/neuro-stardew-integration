using Microsoft.Xna.Framework;
using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Websocket;
using NeuroStardewValley.Source.RegisterActions;
using NeuroStardewValley.Source.Utilities;
using StardewValley;
using Object = StardewValley.Object;

namespace NeuroStardewValley.Source.Actions.ObjectActions;

public static class WorldObjectActions
{
	public class InteractWithObject : NeuroAction<Object>
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
            
			resultData = null;
			if (objectTileX is null || objectTileY is null)
			{
				return ExecutionResult.Failure($"You have provided a null value.");
			}
			if (Main.Bot.ObjectInteraction.GetObjectAtTile((int)objectTileX, (int)objectTileY) is null)
			{
				return ExecutionResult.Failure($"There is no object at the provided tile.");
			}

			if (!RangeCheck.InRange(new Point(objectTileX.Value, objectTileY.Value)))
			{
				return ExecutionResult.Failure($"This object is not within range of you.");
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

	public class InteractWithActionTile : NeuroAction<Point>
	{
		private static List<Point> Tiles => WarpUtilities.ActionableTiles.ToList();
		public override string Name => "interact_with_tile";
		protected override string Description => "Interact with a tile that has an action on it.";
		protected override JsonSchema Schema => new()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "tile" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["tile"] = QJS.Enum(Tiles.Select(tile => tile.ToString()).ToList())
			}
		};
		protected override ExecutionResult Validate(ActionData actionData, out Point resultData)
		{
			string? tileString = actionData.Data?.Value<string>("tile");

			resultData = new();
			if (string.IsNullOrEmpty(tileString))
			{
				return ExecutionResult.Failure($"You provided a null or empty value to tile");
			}

			Point tile = Tiles[Tiles.Select(tile => tile.ToString()).ToList().IndexOf(tileString)];

			if (!Tiles.Contains(tile) || !Main.Bot._currentLocation.isActionableTile(tile.X, tile.Y, Main.Bot._farmer))
			{
				return ExecutionResult.Failure($"The tile you provided is not a valid tile, you should try another."); 
			}

			if (!Utility.tileWithinRadiusOfPlayer(tile.X, tile.Y, 1, Main.Bot._farmer))
			{
				return ExecutionResult.Failure($"You are not neighbouring the tile you selected, you should try to get closer.");
			}
			
			resultData = tile;
			return ExecutionResult.Success($"You are interacting with {tile}");
		}

		protected override void Execute(Point resultData)
		{
			Main.Bot.ObjectInteraction.DoActionTile(resultData);
			RegisterMainGameActions.RegisterPostAction();
		}
	}
}