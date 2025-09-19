using Microsoft.Xna.Framework;
using NeuroSDKCsharp.Actions;
using NeuroStardewValley.Debug;
using NeuroStardewValley.Source.Actions;
using NeuroStardewValley.Source.Actions.Menus;
using NeuroStardewValley.Source.Actions.ObjectActions;
using NeuroStardewValley.Source.ContextStrings;
using NeuroStardewValley.Source.Utilities;
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
	#region AddActions

	public static void RegisterActions(ActionWindow window)
	{
		window.AddAction(new PathFindingActions.Pathfinding()).AddAction(new PathFindingActions.PathFindToExit())
			.AddAction(new PathFindingActions.GoToCharacter());

		if (Main.Bot._currentLocation.furniture.Count > 0 || WorldObjectActions.InteractWithObject.GetSchema().Count > 0)
		{
			window.AddAction(new WorldObjectActions.InteractWithObject());
		}

		if (WarpUtilities.ActionableTiles.Count > 0)
		{
			window.AddAction(new WorldObjectActions.InteractWithActionTile());
		}

		if (Game1.currentLocation.buildings.Count > 0)
		{
			window.AddAction(new BuildingActions.InteractWithBuilding());
		}

		if (Game1.currentLocation.characters.Any(monster => monster.IsMonster))
		{
			window.AddAction(new PathFindingActions.AttackMonster());
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
			.AddAction(new InventoryActions.ChangeSelectedToolbarSlot());
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
				bool hasItem = false;
				foreach (var item in Main.Bot.PlayerInformation.Inventory) // use this instead of .Any as can't declare wateringCan in any
				{
					if (!hasItem)
					{
						window.AddAction(new ToolActions.UseToolInRect());
						hasItem = true;
					}
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
			case AnimalHouse:
				List<Point> troughTiles = WorldObjectActions.InteractWithTile.GetSchema();
				if (troughTiles.Count < 1) break;
				NeuroSDKCsharp.Messages.Outgoing.Context.Send($"There are the locations of the troughs: {string.Join(" ",troughTiles)}");

				window.AddAction(new WorldObjectActions.InteractWithTile());
				break;
			case MineShaft:
				List<Point> ladderTile = WorldObjectActions.InteractWithTile.GetSchema();
				if (ladderTile.Count < 1) break;
				NeuroSDKCsharp.Messages.Outgoing.Context.Send($"These are the locations of the ladders," +
				                                              $" you may need to destroy the object over them to use them." +
				                                              $" {string.Join(" ",ladderTile)}");

				window.AddAction(new WorldObjectActions.InteractWithTile());
				break;
		}
	}

	#endregion

	public static void RegisterPostAction(BotWarpedEventArgs? e = null,int afterSeconds = 0,string query = "",string state = "",bool? ephemeral = null)
	{
		if (!Context.IsPlayerFree) return;
		
		Logger.Info($"register actions again.");
		ActionWindow window = ActionWindow.Create(Main.GameInstance);
		RegisterActions(window);
		RegisterToolActions(window,e,Main.Bot._currentLocation);
		RegisterLocationActions(window,Main.Bot._currentLocation);
		if (afterSeconds != 0 || query == "" || state == "" || ephemeral != null)
		{
			if (query == "")
			{
				query = $"You are at {Main.Bot._currentLocation.Name}," +
				        $" The current weather is {Main.Bot.WorldState.GetCurrentLocationWeather().Weather}." +
				        $" These are the Items in your inventory: {InventoryContext.GetInventoryString(Main.Bot._farmer.Items, true)}" +
				        $"\nIf you want more information about your items should open your inventory.";
			}
			if (state == "")
			{
				state = string.Join("\n",WarpUtilities.GetTilesInLocation(Main.Bot._currentLocation,Main.Bot._farmer,50));
			}
			window.SetForce(afterSeconds, query, state, ephemeral is null || ephemeral.Value);
		}
		window.Register();
	}
	
	public static void LoadGameActions(string query = "",string state = "",bool ephemeral = false)
	{
		ActionWindow actionWindow = ActionWindow.Create(Main.GameInstance);
		actionWindow.SetForce(0,query,state,ephemeral);
		RegisterActions(actionWindow);
		RegisterToolActions(actionWindow,null,Main.Bot._currentLocation);
		RegisterLocationActions(actionWindow,Main.Bot._currentLocation);
		actionWindow.Register();
	}
}