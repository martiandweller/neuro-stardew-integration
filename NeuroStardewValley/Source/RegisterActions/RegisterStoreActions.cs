using NeuroSDKCsharp.Actions;
using NeuroStardewValley.Debug;
using NeuroStardewValley.Source.Actions;
using NeuroStardewValley.Source.Actions.Menus;
using NeuroStardewValley.Source.Utilities;
using StardewValley;
using StardewValley.GameData.Buildings;
using StardewValley.Menus;

namespace NeuroStardewValley.Source.RegisterActions;

public static class RegisterStoreActions
{
	public static void RegisterDefaultShop()
	{
		ActionWindow window = ActionWindow.Create(Main.GameInstance);

		window.AddAction(new ShopActions.CloseShop()).AddAction(new ShopActions.BuyItem());

		// mainly just to stop warnings
		if (Main.Bot.Shop._currentShop is null)
		{
			Game1.activeClickableMenu = null;
			RegisterMainGameActions.RegisterPostAction();
			return;
		}
		// amount of items that can be highlighted
		if (Main.Bot.Shop._currentShop.inventory.actualInventory.Any(item =>
			    item is not null && Main.Bot.Shop._currentShop.inventory.highlightMethod(item)))
		{
			window.AddAction(new ShopActions.SellBackItem());
		}

		string itemString = "These are the items in the shop and their sale prices:";
		List<ISalable> items = Main.Bot.Shop.ListAllItems();
		if (items.Count < 1)
		{
			Game1.activeClickableMenu = null;
			RegisterMainGameActions.RegisterPostAction();
			return;
		}
		for (int i = 0; i < items.Count - 1; i++)
		{
			ISalable itemISalable = items[i];
			ItemStockInformation stockInformation = Main.Bot.Shop.StockInformation[itemISalable];
			itemString += $"\n{i}: {itemISalable.DisplayName}, description: {StringUtilities.FormatItemString(itemISalable.getDescription())} cost: {stockInformation.Price}";
			Item item = ItemRegistry.Create(itemISalable.QualifiedItemId);
			if (item is Tool && !string.IsNullOrEmpty(stockInformation.TradeItem))
			{
				Item upgradeItem = ItemRegistry.Create(stockInformation.TradeItem);
				itemString += $" items needed for upgrade: {upgradeItem.DisplayName} {stockInformation.TradeItemCount}";
			}
		}

		itemString += "\nThese are the items you can sell to the shop: ";

		if (Main.Bot.Shop._currentShop is null) return;
		foreach (var item in Main.Bot.Shop._currentShop.inventory.actualInventory)
		{
			if (item is null) continue;

			if (Main.Bot.Shop._currentShop.inventory.highlightMethod(item))
			{
				itemString += $"\n{item.DisplayName} sell price: {item.sellToStorePrice()}";
			}
		}
		window.SetForce(0, "You are in a shop", itemString);
		
		window.Register();
	}

	public static void RegisterCarpenterActions()
	{
		ActionWindow window = ActionWindow.Create(Main.GameInstance);

		if (Main.Bot.FarmBuilding._carpenterMenu is null)
		{
			Logger.Error($"_carpenter menu is null");
			return;
		}
		
		window.AddAction(new CarpenterActions.DemolishBuilding())
		.AddAction(new CarpenterActions.CreateBuilding()).AddAction(new CarpenterActions.ChangeBuildingBlueprint());

		if (Main.Bot.FarmBuilding._carpenterMenu.Blueprint.IsUpgrade)
		{
			window.AddAction(new CarpenterActions.UpgradeBuilding());
		}
		
		if (Main.Bot.FarmBuilding.Building.CanBeReskinned())
		{
			Main.Bot.FarmBuilding.SetBuildingUI(new BuildingSkinMenu(Main.Bot.FarmBuilding.Building, true));
			window.AddAction(new CarpenterActions.ChangeBuildingSkin());	
		}

		string state = "These are the possible buildings that you can either build, upgrade or demolish: ";
		foreach (var entry in Main.Bot.FarmBuilding._carpenterMenu!.Blueprints)
		{
			state += $"\n-Building name: {entry.DisplayName}\n-- Time to build: {entry.BuildDays} days\n-- Cost to build: {entry.BuildCost}g";
			if (entry.BuildMaterials is null) continue;
			state += $"\n-- Materials to build: ";
			foreach (var material in entry.BuildMaterials)
			{
				Item item = ItemRegistry.Create(material.Id);
				state += $"\n--- {item.DisplayName} amount: {material.Amount}";
			}
		}
		window.SetForce(0, $"You are now in the carpenter menu", state,true);
		
		window.Register();
	}

	public static void RegisterBlacksmithActions()
	{
		ActionWindow window = ActionWindow.Create(Main.GameInstance);
		List<Item> items = new();
		foreach (var item in Main.Bot.Inventory.Inventory)
		{
			if (!Utility.IsGeode(item))
			{
				continue;
			}
			
			items.Add(item);
		}
		if (items.Count > 0)
		{
			window.AddAction(new BlacksmithActions.OpenGeode());
		}

		window.AddAction(new BlacksmithActions.CloseMenu());
		
		window.SetForce(0, $"You are in the blacksmiths, you can select a geode to open here.",
			$"You should either select a geode to open or close the menu.");
		
		window.Register();
	}
}