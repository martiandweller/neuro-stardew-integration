using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Messages.Outgoing;
using NeuroSDKCsharp.Websocket;
using NeuroStardewValley.Source.RegisterActions;
using StardewValley;
using StardewValley.Menus;

namespace NeuroStardewValley.Source.Actions.Menus;

public static class BlacksmithActions
{
	public class OpenGeode : NeuroAction<int>
	{
		public override string Name => "open_geode";
		protected override string Description => "Opens the geode that you select from your inventory.";
		protected override JsonSchema Schema => new()
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

			if (Game1.player._money < 25)
			{
				return ExecutionResult.Failure($"You cannot afford to open a geode, you need 25g to open a geode.");
			}
			
			if (Game1.player.freeSpotsInInventory() == 0 && Main.Bot.Inventory.Inventory[index].Stack > 1)
			{
				return ExecutionResult.Failure($"You do not have enough free space in your inventory, so you cannot open this geode. You should try to free some space.");
			}

			resultData = index;
			return ExecutionResult.Success();
		}

		protected override void Execute(int resultData)
		{
			Main.Bot.Blacksmith.OpenGeode(resultData);
			Task.Run(async () => await SendTreasureContext()); // geode item takes time to be added to menu
		}

		private static async Task SendTreasureContext()
		{
			await Task.Delay(3000); // magic number but it looks good so I don't card
			GeodeMenu? menu = Game1.activeClickableMenu as GeodeMenu;
			Context.Send($"You got a {menu?.geodeTreasure.Name} from the geode!");
			RegisterStoreActions.RegisterBlacksmithActions();
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
		protected override string Description => "Exit this menu.";
		protected override JsonSchema Schema => new();
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