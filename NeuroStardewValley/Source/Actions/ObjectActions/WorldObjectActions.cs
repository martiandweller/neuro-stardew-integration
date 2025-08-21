using Microsoft.Xna.Framework;
using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Websocket;
using NeuroStardewValley.Source.RegisterActions;
using NeuroStardewValley.Source.Utilities;
using StardewBotFramework.Source.Modules.Pathfinding.Base;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Objects;
using Object = StardewValley.Object;

namespace NeuroStardewValley.Source.Actions.ObjectActions;

public static class WorldObjectActions
{
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