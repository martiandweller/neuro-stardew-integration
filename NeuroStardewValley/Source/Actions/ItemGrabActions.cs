using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Websocket;
using NeuroStardewValley.Debug;
using NeuroStardewValley.Source.Utilities;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Menus;

namespace NeuroStardewValley.Source.Actions;

public static class ItemGrabActions
{
	public static ItemGrabMenu? Menu { get; set; }
	public class TakeItem : NeuroAction<Dictionary<Item,int>>
	{
		public override string Name => "take_items";
		protected override string Description => "Take Items from this menu.";

		protected override JsonSchema Schema => new()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "item_menu_name","inventory_index" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["item_menu_name"] = QJS.Enum(GetMenuItems()),
				["inventory_index"] = QJS.Enum(Enumerable.Range(0, Main.Bot.Inventory.MaxInventory))
			}
		};
		protected override ExecutionResult Validate(ActionData actionData, out Dictionary<Item,int>? resultData)
		{
			string? menuItems = actionData.Data?.Value<string>("item_menu_name");
			int? invIndex = actionData.Data?.Value<int>("inventory_index");
			
			resultData = null;
			
			if (Menu is null)
			{
				return ExecutionResult.Failure(string.Format(ResultStrings.ModVarFailure,$"Menu")); 
			}
			
			if (menuItems is null || invIndex is null)
			{
				return ExecutionResult.Failure($"You gave a null value, this is not allowed.");
			}

			resultData = new Dictionary<Item, int>();
			if (!GetMenuItems().Contains(menuItems))
			{
				return ExecutionResult.Failure($"{menuItems} is not a valid item in the menu.");
			}

			int itemIndex = GetMenuItems().IndexOf(menuItems);
			resultData.Add(Menu.ItemsToGrabMenu.actualInventory[itemIndex],(int)invIndex);
			
			return ExecutionResult.Success();
		}

		protected override void Execute(Dictionary<Item,int>? resultData)
		{
			if (resultData is null) return;
			foreach (var kvp in resultData)
			{
				Main.Bot.ItemGrabMenu.TakeItem(kvp.Key);
			}
		}
	}

	public class AddItem : NeuroAction<Item>
	{
		public override string Name => "add_items";
		protected override string Description => "Add items to this menu";
		protected override JsonSchema Schema => new()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "item_menu_name" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["item_menu_name"] = QJS.Enum(GetMenuItems(true)),
			}
		};
		protected override ExecutionResult Validate(ActionData actionData, out Item? resultData)
		{
			string? itemString = actionData.Data?.Value<string>("item_menu_name");

			resultData = null;
			
			if (Menu is null)
			{
				return ExecutionResult.Failure(string.Format(ResultStrings.ModVarFailure,$"Menu")); 
			}
			
			if (itemString is null)
			{
				return ExecutionResult.Failure($"You gave item_menu_name as null.");
			}

			if (!GetMenuItems(true).Contains(itemString))
			{
				return ExecutionResult.Failure($"{itemString} is not a valid item in the menu.");
			}
			resultData = Game1.player.Items[GetMenuItems(true).IndexOf(itemString)];

			return ExecutionResult.Success($"Added: {resultData.Name} to the menu");
		}

		protected override void Execute(Item? resultData)
		{
			if (resultData is null) return;
			Logger.Info($"can add item: {Main.Bot.ItemGrabMenu.AddItem(resultData)}");
		}
	}

	public static void RegisterActions(ItemGrabMenu menu)
	{
		Menu = menu;
		ActionWindow window = ActionWindow.Create(Main.GameInstance);

		window.AddAction(new TakeItem()).AddAction(new AddItem());

		Inventory inv = new Inventory();
		inv.AddRange(Menu.ItemsToGrabMenu.actualInventory);
		window.SetForce(0, $"You are in a menu that has items in it.",
			$"Menu items: {InventoryContext.GetInventoryString(inv)}" +
			$".\n\n Inventory items: {InventoryContext.GetInventoryString(Game1.player.Items)}");
		
		window.Register();
	}

	private static List<string> GetMenuItems(bool inventory = false)
	{
		List<string> itemStrings = new();
		List<Item> items = new();
		items.AddRange(inventory ? Game1.player.Items : Menu!.ItemsToGrabMenu.actualInventory);
		foreach (var item in items)
		{
			if (item is null) continue;
			
			itemStrings.Add(item.Name);
		}

		return itemStrings;
	}
}