using NeuroSDKCsharp.Messages.Outgoing;
using NeuroStardewValley.Debug;
using NeuroStardewValley.Source.RegisterActions;
using NeuroStardewValley.Source.Utilities;
using StardewBotFramework.Source.Events.EventArgs;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace NeuroStardewValley.Source.EventClasses;

public class MainGameLoopEvents
{
	public static readonly Dictionary<SkillType, int> SkillsChangedThisDay = new();

	public static void OnWarped(object? sender, BotWarpedEventArgs e)
	{
		string tilesString = "";
		foreach (var tile in WarpUtilities.GetTilesInLocation(e.NewLocation))
		{
			tilesString += "\n" + tile;
		}
		Context.Send($"You have moved to {e.NewLocation.Name} from {e.OldLocation.Name}.",true);
    
		string warps = WarpUtilities.GetWarpTiles(e.NewLocation);
		string warpsString = WarpUtilities.GetWarpTilesString(warps);
		
		var locationCharacters = Main.Bot.Characters.GetCharactersInCurrentLocation(e.NewLocation);
		string characterContext = "";
		foreach (var kvp in locationCharacters)
		{
			characterContext += $"{kvp.Value.Name} is at {kvp.Key} in this location";
		}
		
		Context.Send(warpsString, true);
		Context.Send(characterContext,true);
		RegisterMainGameActions.RegisterPostAction(e, 0, "Test Query", $"These are the tiles that have an object on them around you: {tilesString}", false);
	}

	public static void OnMenuChanged(object? sender, BotMenuChangedEventArgs e)
	{
		Logger.Info($"current menu: {e.NewMenu}");
		if (e.OldMenu is DialogueBox)
		{
			Logger.Info($"Removing dialogue box");
			Main.Bot.Dialogue.CurrentDialogue = null;
			if (Game1.player.isInBed.Value)
			{
				return; // we don't need to send any actions at this point
			}

			//TODO: remove dialogue actions or set up gameplay actions or shop actions
		}

		Logger.Info($"Menu has been changed to: {e.NewMenu}");

		switch (e.NewMenu)
		{
			case DialogueBox dialogueBox:
				Logger.Info($"add new menu dialogue box");
				Main.Bot.Dialogue.CurrentDialogueBox = dialogueBox;
				RegisterDialogueActions.RegisterActions();
				break;
			case ShopMenu shopMenu:
				Main.Bot.Shop.OpenShop(shopMenu); // this should be handled by OpenShopUi
				RegisterStoreActions.RegisterDefaultShop();
				break;
			case CarpenterMenu carpenterMenu:
				Main.Bot.FarmBuilding.SetCarpenterUI(carpenterMenu);
				RegisterStoreActions.RegisterCarpenterActions();
				break;
			case GeodeMenu geodeMenu:
				Main.Bot.Blacksmith.OpenGeodeMenu(geodeMenu);
				RegisterStoreActions.RegisterBlacksmithActions();
				break;
			case LevelUpMenu levelUpMenu:
				Main.Bot.EndDaySkillMenu.SetMenu(levelUpMenu);
				RegisterLevelUpMenu.GetSkillContext(SkillsChangedThisDay);
				break;
			case ShippingMenu shippingMenu:
				Main.Bot.EndDayShippingMenu.SetMenu(shippingMenu);
				ShippingMenuContext();
				break;
		}
		
		if (e is { NewMenu: null } and {OldMenu:not TitleMenu and not LevelUpMenu}) // ugly but it gets rid of warning and double send at start of game
		{
			Logger.Info($"old menu: {e.OldMenu.GetType()}");
			RegisterMainGameActions.RegisterPostAction();
		}
	}

	public static void OnDayStarted(object? sender, BotDayStartedEventArgs e)
	{
		string time = StringUtilities.FormatTimeString();
		SendQuestContext.SendContext();
		if (Game1.player.passedOut)
		{
			// add items lost or whatever was lost
			Context.Send($"A new day has started. You are in your farm-house after you were knocked out, " +
			             $"the current day is {SDate.Now().DayOfWeek} {SDate.Now().Day} of {SDate.Now().Season} in year {SDate.Now().Year} at time: {time}.");
		}
		else
		{
			Context.Send($"A new day has started. You are in your farm-house, " +
			             $"the current day is {SDate.Now().DayOfWeek} {SDate.Now().Day} of {SDate.Now().Season} in year {SDate.Now().Year} at time: {time}.");
		}
	}

	public static void OnDayEnded(object? sender, BotDayEndedEventArgs e)
	{
		if (!Game1.player.passedOut)
		{
			Context.Send($"You have gone to bed and the day has ended. Good night.");
			return;
		}

		Context.Send($"You have passed out and the day has ended. Maybe you should go back home earlier tomorrow.");
	}
	
	private static void ShippingMenuContext()
	{
		if (Game1.player.displayedShippedItems.Count == 0) return;

		Task.Run(async () =>
			await Task.Delay(Game1.player.displayedShippedItems.Count *
			                 500)); // wait for it to show all results, we multiply as Task.Delay is in milliseconds
		string shipString = "These are the items you have shipped today,";
		foreach (var item in Game1.player.displayedShippedItems)
		{
			int sell = item.sellToStorePrice();
			shipString = string.Concat(shipString,
				$"\n{item.Name}: total sell price: {sell * item.Stack} single sell price: {sell}");
		}

		Context.Send(shipString);
		Main.Bot.EndDayShippingMenu.AdvanceToNextDay();
	}
}