using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Messages.Outgoing;
using NeuroSDKCsharp.Websocket;
using StardewValley;

namespace NeuroStardewValley.Source.Actions;

public static class BlacksmithActions
{
	public class OpenGeode : NeuroAction<int>
	{
		public override string Name => "open_geode";
		protected override string Description => "Will open the geode that you select from your inventory, The schema will only include indexes that contains a geode";
		protected override JsonSchema? Schema => new()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "item_index" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["item_index"] = QJS.Enum(GetSchema())
			}
		};
		protected override ExecutionResult Validate(ActionData actionData, out int resultData)
		{
			string? stringIndex= actionData.Data?.Value<string>("item_index");
			
			resultData = -1;
			if (string.IsNullOrEmpty(stringIndex) || !GetSchema().Contains(stringIndex))
			{
				return ExecutionResult.Failure($"{stringIndex} is not valid. This is either because it is null or not a valid item in the schema");	
			}
			
			int index = int.Parse(stringIndex);

			if (!Utility.IsGeode(Main.Bot.Inventory.Inventory[index]))
			{
				return ExecutionResult.Failure($"{index} is not a geode");
			}

			resultData = index;
			return ExecutionResult.Success();
		}

		protected override void Execute(int resultData)
		{
			Item? geodeTreasure = Main.Bot.Blacksmith.OpenGeode(resultData);
			if (geodeTreasure is null) return;
			Context.Send($"You got a {geodeTreasure.Name} from the geode!");
		}

		private static string[] GetSchema()
		{
			List<string> strings = new();
			foreach (var item in Main.Bot.Inventory.Inventory)
			{
				if (!Utility.IsGeode(item))
				{
					continue;
				}
				
				strings.Add(Main.Bot.Inventory.Inventory.IndexOf(item).ToString());
			}

			return strings.ToArray();
		} 
	}

	public class CloseMenu : NeuroAction
	{
		public override string Name => "close_menu";
		protected override string Description => "Close the geode menu";
		protected override JsonSchema? Schema => new();
		protected override ExecutionResult Validate(ActionData actionData)
		{
			return ExecutionResult.Success();
		}

		protected override void Execute()
		{
			Main.Bot.Blacksmith.CloseGeodeMenu();
		}
	}
}