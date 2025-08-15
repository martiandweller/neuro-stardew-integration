using NeuroSDKCsharp.Actions;
using NeuroStardewValley.Source.Actions;
using NeuroStardewValley.Source.Utilities;
using StardewValley.Menus;

namespace NeuroStardewValley.Source.RegisterActions;

public static class RegisterStoreActions
{
	public static void RegisterDefaultShop()
	{
		ActionWindow window = ActionWindow.Create(Main.GameInstance);

		window.AddAction(new ShopActions.CloseShop()).AddAction(new ShopActions.BuyItem());

		string itemString = "These are the items in the shop and their sale prices:\n";
		foreach (var item in Main.Bot.Shop.ListAllItems()!)
		{
			itemString += $"{item.Name}: {StringUtilities.FormatItemString(item.getDescription())}, cost: {item.salePrice()}\n";
		}
		window.SetForce(0, "You are in a shop", itemString);
		
		window.Register();
	}

	public static void RegisterCarpenterActions()
	{
		ActionWindow window = ActionWindow.Create(Main.GameInstance);

		window.AddAction(new CarpenterActions.CreateBuilding()).AddAction(new CarpenterActions.DemolishBuilding())
			.AddAction(new CarpenterActions.UpgradeBuilding()).AddAction(new CarpenterActions.ChangeBuildingBlueprint())
			.AddAction(new CarpenterActions.ChangeBuildingSkin());

		window.SetForce(0, $"You are now in the carpenter menu", "");
		
		window.Register();
	}
}