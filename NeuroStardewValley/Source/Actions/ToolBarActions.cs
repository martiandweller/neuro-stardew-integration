using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Websocket;

namespace NeuroStardewValley.Source.Actions;

public static class ToolBarActions
{
	public class ChangeSelectedToolbarSlot : NeuroActionS<int>
	{
		public override string Name => "change_toolbar_slot";
		protected override string Description => "Change currently selected toolbar slot, the slots available are between 0,11, you can select a slot does not mean it has an item in it.";
		protected override JsonSchema? Schema => new()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "slot" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["slot"] = QJS.Enum(Enumerable.Range(0, 12)),
			}
		};
        
		protected override ExecutionResult Validate(ActionData actionData, out int? resultData)
		{
			string? slotStr = actionData.Data?.Value<string>("slot");

			if (string.IsNullOrEmpty(slotStr))
			{
				resultData = -1;
				return ExecutionResult.Failure($"slot can not be null");
			}
            
			int slot = int.Parse(slotStr);

			if (!Enumerable.Range(0, 12).Contains(slot))
			{
				resultData = -1;
				return ExecutionResult.Failure($"{slot} is not a valid slot index");
			}

			resultData = slot;
			return ExecutionResult.Success($"Changing to slot: {slot}");
		}

		protected override void Execute(int? resultData)
		{
			if (resultData is null) return;
			Main.Bot.Inventory.SelectSlot((int)resultData);
		}
	}

	public class ChangeCurrentToolbar : NeuroActionS<int>
	{
		private static int ToolBarAmount => Main.Bot.Inventory.Inventory.Count / 12;
		public override string Name => "change_toolbar_row";
		protected override string Description => $"Change currently selected toolbar row, This will go around like a carousel. You have {ToolBarAmount} toolbars including the currently selected one.";
		protected override JsonSchema? Schema => new()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "row" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["row"] = QJS.Enum(Enumerable.Range(1,ToolBarAmount))
			}
		};
        
		protected override ExecutionResult Validate(ActionData actionData, out int? resultData)
		{
			string? rowStr = actionData.Data?.Value<string>("row");

			if (string.IsNullOrEmpty(rowStr))
			{
				resultData = -1;
				return ExecutionResult.Failure($"slot can not be null");
			}
            
			int row = int.Parse(rowStr);

			if (!Enumerable.Range(0, ToolBarAmount).Contains(row))
			{
				resultData = -1;
				return ExecutionResult.Failure($"{row} is not a valid row index");
			}

			resultData = row;
			return ExecutionResult.Success($"Changing to row: {row}");
		}

		protected override void Execute(int? resultData)
		{
			if (resultData is null) return;
			Main.Bot.Inventory.SelectInventoryRowForToolbar(true,(int)resultData);
		}
	}
}