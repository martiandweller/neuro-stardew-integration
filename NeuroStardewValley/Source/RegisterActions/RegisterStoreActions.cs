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

		string itemString = "These are the items in the shop and their sale prices:";
		List<ISalable>? items = Main.Bot.Shop.ListAllItems();
		if (items is null)
		{
			Game1.activeClickableMenu = null;
			RegisterMainGameActions.RegisterPostAction();
			return;
		}
		for (int i = 0; i < items.Count - 1; i++)
		{
			ISalable itemISalable = items[i];
			itemString += $"\n{i}: {itemISalable.Name}, description: {StringUtilities.FormatItemString(itemISalable.getDescription())} cost: {itemISalable.salePrice()}";
			Item item = ItemRegistry.Create(itemISalable.QualifiedItemId);
			if (item is Tool)
			{
				Item upgradeItem = ItemRegistry.Create(Main.Bot.Shop.StockInformation[itemISalable].TradeItem);
				itemString += $" items needed for upgrade: {upgradeItem.Name} {Main.Bot.Shop.StockInformation[itemISalable].TradeItemCount}";
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
			if (Main.Bot.FarmBuilding._buildingSkinMenu is null || Main.Bot.FarmBuilding._carpenterMenu.currentBuilding != Main.Bot.FarmBuilding._buildingSkinMenu.Building)
			{
				Logger.Warning($"SETTING BUILDING UI");
				Main.Bot.FarmBuilding.SetBuildingUI(new BuildingSkinMenu(Main.Bot.FarmBuilding.Building, true));
			}
			window.AddAction(new CarpenterActions.ChangeBuildingSkin());	
		}

		string state = "These are the possible buildings that you can either build, upgrade or demolish: ";
		foreach (var entry in Main.Bot.FarmBuilding._carpenterMenu!.Blueprints)
		{
			state += $"\n{entry.DisplayName} time to build: {entry.BuildDays} days  cost to build: {entry.BuildCost}g";
			if (entry.BuildMaterials is null) continue;
			state += $" materials to build: ";
			for (int i = 0; i < entry.BuildMaterials.Count; i++)
			{
				BuildingMaterial material = entry.BuildMaterials[i];
				Item item = ItemRegistry.Create(material.Id);
				state += $" {item.Name} amount: {material.Amount}";
				if (i >= entry.BuildMaterials.Count - 1)
				{
					state += $".";
					continue;
				}

				state += $",";
			}
		}
		window.SetForce(0, $"You are now in the carpenter menu", state);
		
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