using NeuroSDKCsharp.Actions;
using NeuroStardewValley.Debug;
using NeuroStardewValley.Source.Actions;
using NeuroStardewValley.Source.Actions.Menus;
using NeuroStardewValley.Source.Actions.ObjectActions;
using StardewBotFramework.Source.Events.EventArgs;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.Tools;

namespace NeuroStardewValley.Source.RegisterActions;

public static class RegisterMainGameActions
{
	public static void RegisterActions(ActionWindow window,bool checkCanMove = true)
	{
		if (!Context.IsPlayerFree) return;

		window.AddAction(new MainGameActions.Pathfinding()).AddAction(new MainGameActions.PathFindToExit());

		// if (Context.CanPlayerMove)
		// {
		// }
		
		if (Game1.currentLocation.furniture.Count > 0 || Game1.currentLocation.Objects.Length > 0)
		{
			window.AddAction(new WorldObjectActions.InteractWithObject());
		}

		if (Game1.currentLocation.buildings.Count > 0)
		{
			window.AddAction(new BuildingActions.InteractWithBuilding());
		}

		if (Game1.player.CurrentItem is not null)
		{
			window.AddAction(new ToolActions.UseItem());	
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
		GameLocation newLocation;
		if (e is not null)
		{
			newLocation = e.NewLocation;
		}
		else if (location is not null)
		{
			newLocation = location;
		}
		else
		{
			return;
		}
		switch (newLocation)
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
						case FishingRod:
							if (Game1.currentLocation.canFishHere())
							{
								window.AddAction(new ToolActions.Fishing());
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
								window.AddAction(new ChestActions.OpenChest());
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

	public static void RegisterPostAction(BotWarpedEventArgs? e = null,int afterSeconds = 0,string query = "",string state = "",bool? ephemeral = null)
	{
		Logger.Info($"register actions again.");
		ActionWindow window = ActionWindow.Create(Main.GameInstance);
		RegisterActions(window,false);
		RegisterToolActions(window,e,Game1.currentLocation);
		RegisterLocationActions(window,Game1.currentLocation);
		if (afterSeconds != 0 || query == "" || state == "" || ephemeral != null)
		{
			Logger.Info($"ephemeral: {ephemeral is not null && ephemeral.Value}  {ephemeral}");
			window.SetForce(afterSeconds, query, state, ephemeral is not null && ephemeral.Value);
		}
		window.Register();
	}
	
	public static void LoadGameActions(string query = "",string state = "",bool ephemeral = false)
	{
		ActionWindow actionWindow = ActionWindow.Create(Main.GameInstance);
		actionWindow.SetForce(0,query,state,ephemeral);
		RegisterActions(actionWindow,false);
		RegisterToolActions(actionWindow,null,Game1.currentLocation);
		RegisterLocationActions(actionWindow,Game1.currentLocation);
		actionWindow.Register();
	}
}