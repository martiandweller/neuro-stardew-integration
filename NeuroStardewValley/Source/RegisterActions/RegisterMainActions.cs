using Microsoft.Xna.Framework;
using NeuroSDKCsharp.Actions;
using NeuroStardewValley.Debug;
using NeuroStardewValley.Source.Actions;
using NeuroStardewValley.Source.Actions.Menus;
using NeuroStardewValley.Source.Actions.ObjectActions;
using NeuroStardewValley.Source.Actions.WorldQuery;
using NeuroStardewValley.Source.ContextStrings;
using StardewBotFramework.Source.Events.EventArgs;
using StardewBotFramework.Source.Modules.Pathfinding.Base;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.Tools;

namespace NeuroStardewValley.Source.RegisterActions;

public static class RegisterMainActions
{
	#region AddActions

	private static void RegisterActions(ActionWindow window)
	{
		window.AddAction(new PathFindingActions.Pathfinding()).AddAction(new PathFindingActions.PathFindToExit())
			.AddAction(new InventoryActions.OpenInventory()).AddAction(new InventoryActions.ChangeSelectedToolbarSlot());

		if (Main.Config.WaitTimeAction)
		{
			window.AddAction(new WaitForTime());
		}

		if (Main.Bot._currentLocation.Objects.Length > 0 || TileContext.ActionableTiles.Count > 0 ||
		    Game1.currentLocation.buildings.Count > 0)
		{
			window.AddAction(new QueryWorldActions.GetObjectsInRadius()).AddAction(new QueryWorldActions.GetObjectTypeInRadius());
		}

		if (Main.Bot._farmer.Items.Any(item => item is not null && item.isPlaceable()))
		{
			window.AddAction(new WorldObjectActions.PlaceObjects()).AddAction(new WorldObjectActions.PlaceObject())
				.AddAction(new InteractAtTile());
		}
		
		if (Game1.currentLocation.characters.Any(monster => monster.IsMonster))
		{
			window.AddAction(new PathFindingActions.AttackMonster());
		}
		
		if (Main.Bot._currentLocation.characters.Any(character => !character.IsMonster))
		{
			window.AddAction(new PathFindingActions.InteractCharacter());
		}
		
		if (Game1.player.CurrentItem is not null)
		{
			window.AddAction(new ToolActions.UseItem());
		}

		if (Game1.player.questLog.Count > 0)
		{
			window.AddAction(new QuestLogActions.OpenLog());
		}
	}

	private static void RegisterToolActions(ActionWindow window, BotWarpedEventArgs? e = null,GameLocation? location = null)
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
							if (!wateringCan.isBottomless.Value || wateringCan.WaterLeft < wateringCan.waterCanMax)
								window.AddAction(new ToolActions.RefillWateringCan());
							
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

	private static void RegisterLocationActions(ActionWindow window,GameLocation location)
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
		
		List<Point> propertyTile = WorldObjectActions.InteractWithTileProperty.GetSchema();
		switch (location)
		{
			case Farm farm:
				if (farm.buildings.Any(building => building.GetType() == typeof(ShippingBin)))
				{
					window.AddAction(new ShippingBinActions.GoToNearestShippingBin());
				}

				break;
			case AnimalHouse:
				if (propertyTile.Count < 1) break;
				NeuroSDKCsharp.Messages.Outgoing.Context.Send($"These are the locations of troughs in this area," +
				                                              $" you can should put hay in them to feed your animals: {string.Join(",",propertyTile)}");
				
				window.AddAction(new WorldObjectActions.InteractWithTileProperty("interact_with_trough"));
				break;
			case MineShaft:
				if (propertyTile.Count < 1) break;
				NeuroSDKCsharp.Messages.Outgoing.Context.Send($"These are the locations of the ladders in this cave," +
				                                              $" you may need to destroy the object over them to use them." +
				                                              $" {string.Join(",",propertyTile)}");

				window.AddAction(new WorldObjectActions.InteractWithTileProperty("interact_with_ladder"));
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
				query = $"You are at the tile {Main.Bot.Player.BotTilePosition()}" +
				        $" The current weather is {Main.Bot.WorldState.GetCurrentLocationWeather().Weather}." +
				        $" These are the items in your inventory: {InventoryContext.GetInventoryString(Main.Bot.Inventory.Inventory, true)}" +
				        $"\nIf you want more information about your items you should open your inventory.";
			}
			if (state == "")
			{
				var tiles = TileContext.GetObjectAmountInLocation(Main.Bot._currentLocation);
				state = tiles.Where(kvp => kvp.Key != "Grass").Aggregate($"These are the amount of each object in {Main.Bot._currentLocation.DisplayName}:",
					(current, kvp) => current + $"\n{kvp.Key} amount: {kvp.Value}");
			}
			window.SetForce(afterSeconds, query, state, ephemeral is null || ephemeral.Value);
		}
		window.Register();
	}
}