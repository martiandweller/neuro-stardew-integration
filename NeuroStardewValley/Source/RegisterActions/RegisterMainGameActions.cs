using System.Diagnostics;
using NeuroSDKCsharp.Actions;
using NeuroStardewValley.Debug;
using NeuroStardewValley.Source.Actions;
using StardewBotFramework.Source.Events.EventArgs;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.Tools;

namespace NeuroStardewValley.Source.RegisterActions;

public static class RegisterMainGameActions
{
	public static void RegisterActions(ActionWindow window,bool checkCanMove = false)
	{
		if (checkCanMove && !Game1.player.CanMove) {}
		else
		{
			window.AddAction(new MainGameActions.Pathfinding()).AddAction(new MainGameActions.PathFindToExit());
		}
		
		if (Game1.currentLocation.furniture.Count > 0)
		{
			window.AddAction(new MainGameActions.InteractWithFurniture());
		}

		if (Game1.player.CurrentItem is not null)
		{
			window.AddAction(new MainGameActions.UseItem());	
		}

		if (Game1.player.questLog.Count > 0)
		{
			window.AddAction(new QuestLogActions.OpenLog());
		}

		window.AddAction(new InventoryActions.OpenInventory())
			.AddAction(new ToolBarActions.ChangeSelectedToolbarSlot())
			.AddAction(new ToolBarActions.ChangeCurrentToolbar());
	}

	public static void RegisterToolActions(ActionWindow window, BotWarpedEventArgs? e = null,GameLocation? location = null)
	{
		if (e is null) return;
		switch (e.NewLocation)
		{
			case Farm:
			{
				bool madeDestroyAction = false;
				foreach (var item in Main.Bot.PlayerInformation.Inventory) // use this instead of .Any as can't declare wateringCan in any
				{
					switch (item)
					{
						case WateringCan wateringCan:
						{
							if (!wateringCan.isBottomless.Value) window.AddAction(new ToolActions.RefillWateringCan());
							window.AddAction(new ToolActions.WaterFarmLand());
							break;
						}
						case Pickaxe:
						case Axe:
						case MeleeWeapon:
							if (!madeDestroyAction)
							{
								madeDestroyAction = true;
								window.AddAction(new ToolActions.DestroyObject());
							}
							break;
					}
				}

				break;
			}
			case Mine:
			{
				if (Main.Bot.PlayerInformation.Inventory.Any(item => item.GetType() == typeof(Pickaxe)))
				{
					window.AddAction(new ToolActions.DestroyObject());
				}

				break;
			}
		}
	}

	public static void RegisterLocationActions(ActionWindow window,GameLocation location)
	{
		bool madeChestAction = false;
		foreach (var dict in location.Objects)
		{
			foreach (var kvp in dict)
			{
				switch (kvp.Value)
				{
					case Chest:
						if (Game1.activeClickableMenu is null)
						{
							if (!madeChestAction)
							{
								window.AddAction(new WorldObjectActions.OpenChest());
								madeChestAction = true;
							}
						}
						break;
				}
			}
		}
		
		switch (location)
		{
			case Farm farm:
				if (farm.buildings.Any(building => building.GetType() == typeof(ShippingBin)))
				{
					window.AddAction(new ShippingBinActions.GoToNearestShippingBin());
				}

				break;
		}
	}

	public static void RegisterPostAction(BotWarpedEventArgs? e = null)
	{
		Logger.Info($"register actions again.");
		ActionWindow window = ActionWindow.Create(Main.GameInstance);
		RegisterActions(window);
		RegisterToolActions(window,e,Game1.currentLocation);
		RegisterLocationActions(window,Game1.currentLocation);
		window.Register();
	}
	
	public static void LoadGameActions()
	{
		ActionWindow actionWindow = ActionWindow.Create(Main.GameInstance);
		actionWindow.SetForce(0,"","");
		actionWindow.AddAction(new MainGameActions.Pathfinding())
			.AddAction(new MainGameActions.PathFindToExit())
			.AddAction(new MainGameActions.UseItem())
			.AddAction(new InventoryActions.OpenInventory())
			.AddAction(new ToolBarActions.ChangeSelectedToolbarSlot())
			.AddAction(new ToolBarActions.ChangeCurrentToolbar())
			.AddAction(new QuestLogActions.OpenLog());
		actionWindow.Register();
	}
}