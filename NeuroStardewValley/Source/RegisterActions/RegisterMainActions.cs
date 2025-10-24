using Microsoft.Xna.Framework;
using NeuroSDKCsharp.Actions;
using NeuroStardewValley.Debug;
using NeuroStardewValley.Source.Actions;
using NeuroStardewValley.Source.Actions.Menus;
using NeuroStardewValley.Source.Actions.ObjectActions;
using NeuroStardewValley.Source.Actions.WorldQuery;
using NeuroStardewValley.Source.ContextStrings;
using StardewBotFramework.Source.Events.EventArgs;
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

		if (TileContext.GetNameAmountInLocation(Main.Bot._currentLocation).Any())
		{
			window.AddAction(new QueryWorldActions.GetObjectsInRadius())
				.AddAction(new QueryWorldActions.GetObjectTypeInRadius());
		}
		
		if (Main.Bot._currentLocation.Objects.Any() || TileContext.ActionableTiles.Any() ||
		    Main.Bot._currentLocation.buildings.Any() || Main.Bot._currentLocation.furniture.Any())
		{
			window.AddAction(new InteractAtTile());
		}

		if (Main.Bot._farmer.Items.Any(item => item is not null && item.isPlaceable()))
		{
			window.AddAction(new WorldObjectActions.PlaceObjects()).AddAction(new WorldObjectActions.PlaceObject());
		}
		
		if (Main.Bot._currentLocation.characters.Any(monster => monster.IsMonster))
		{
			window.AddAction(new PathFindingActions.AttackMonster());
		}
		
		if (Main.Bot._currentLocation.characters.Any(character => !character.IsMonster))
		{
			window.AddAction(new PathFindingActions.InteractCharacter());
		}
		
		if (Main.Bot._farmer.CurrentItem is not null)
		{
			window.AddAction(new ToolActions.UseItem());
		}

		if (Main.Bot._farmer.questLog.Any())
		{
			window.AddAction(new QuestLogActions.OpenLog());
		}
	}

	private static void RegisterToolActions(ActionWindow window, BotWarpedEventArgs? e = null,GameLocation? location = null)
	{
		GameLocation? newLocation = location ?? e?.NewLocation;
		if (newLocation is null) return;
		
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
		if (Main.Bot._farmer.IsSitting())
		{
			var actionWindow = ActionWindow.Create(Main.GameInstance);
			actionWindow.AddAction(new WorldObjectActions.StopSitting()).Register();
			NeuroSDKCsharp.Messages.Outgoing.Context.Send($"You are currently sitting, if you would like to get up you should use the stop sitting action.");
			return;
		}
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
				query =
					$"You are at the tile {Main.Bot.Player.BotTilePosition()}, if you are unsure about what's around" +
					"you in the world, you should use the query actions to learn more about that." +
					$" The current weather is {Main.Bot.WorldState.GetCurrentLocationWeather().Weather}." +
					$" These are the items in your inventory: {InventoryContext.GetInventoryString(Main.Bot.Inventory.Inventory, true)}";
			}
			if (state == "")
			{
				state = GetSeparatedState();
			}
			window.SetForce(afterSeconds, query, state, ephemeral is null || ephemeral.Value);
		}
		window.Register();
	}

	private static string GetSeparatedState()
	{
		var names = TileContext.GetNameAmountInLocation(Main.Bot._currentLocation);
		string context = "These are the objects around you: ";
		string building = "";
		foreach (var kvp in TileContext.GetObjectsInLocation(Main.Bot._currentLocation))
		{
			string name = TileContext.SimpleObjectName(kvp.Value);
			if (name == "" || context.Contains(name) || building.Contains(name) || name.ToLower().Contains("error")) continue;
			switch (kvp.Value)
			{
				case Building:
					building += $"\n{name} amount: {names[name]}";
					break;
				default:
					context += $"\n{name} amount: {names[name]}";
					break;
			}
		}

		if (building.Length == 0) return context;
		
		building = $"\nThese are the buildings around you: {building}";
		context += $"{building}";
		return context;
	}
}