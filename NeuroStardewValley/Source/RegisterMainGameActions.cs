using NeuroSDKCsharp.Actions;
using NeuroStardewValley.Debug;
using StardewBotFramework.Source;
using StardewBotFramework.Source.Events.EventArgs;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Tools;

namespace NeuroStardewValley.Source;

public class RegisterMainGameActions
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

		window.AddAction(new InventoryActions.OpenInventory())
			.AddAction(new ToolBarActions.ChangeSelectedToolbarSlot())
			.AddAction(new ToolBarActions.ChangeCurrentToolbar());
	}

	public static void RegisterToolActions(ActionWindow window, BotWarpedEventArgs? e = null)
	{
		if (e is not null)
		{
			if (e.NewLocation is Farm)
			{
				foreach (var item in Main.Bot.PlayerInformation.Inventory)
				{
					if (item is WateringCan wateringCan)
					{
						if (!wateringCan.isBottomless.Value) window.AddAction(new ToolActions.RefillWateringCan());
						window.AddAction(new ToolActions.WaterFarmLand());
					}
					if (item is Pickaxe || item is Axe || item is MeleeWeapon) window.AddAction(new ToolActions.DestroyObject()); // can add action twice will not cause crash just a print
				}
			}

			if (e.NewLocation is Mine)
			{
				if (Main.Bot.PlayerInformation.Inventory.Any(item => item.GetType() == typeof(Pickaxe)))
				{
					window.AddAction(new ToolActions.DestroyObject());
				}
			}
		}
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
			.AddAction(new ToolBarActions.ChangeCurrentToolbar());
		actionWindow.Register();
	}
}