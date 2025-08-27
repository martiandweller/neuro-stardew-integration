using NeuroSDKCsharp.Messages.Outgoing;
using NeuroStardewValley.Debug;
using NeuroStardewValley.Source.Actions;
using NeuroStardewValley.Source.Actions.Menus;
using NeuroStardewValley.Source.RegisterActions;
using NeuroStardewValley.Source.Utilities;
using StardewBotFramework.Source.Events.EventArgs;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace NeuroStardewValley.Source.EventMethods;

public static class MainGameLoopEvents
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
		RegisterMainGameActions.RegisterPostAction(e, 0, $"You are at {e.NewLocation.Name}", 
			$"These are the tiles that have an object on them around you: {tilesString}", false);
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
		}
		
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
				break;
			case ItemGrabMenu itemGrabMenu:
				if (itemGrabMenu.context is Chest) return; // The OpenChest action handles this.
				Main.Bot.ItemGrabMenu.SetUI(itemGrabMenu);
				ItemGrabActions.RegisterActions(itemGrabMenu);
				break;
			case Billboard billboard:
				Main.Bot.BillBoard.SetMenu(billboard);
				if (billboard.acceptQuestButton.visible)
				{
					BillBoardInteraction.RegisterQuestActions();
				}
				else
				{
					Context.Send($"These are the event that are happening this month, {BillBoardInteraction.GetCalendarContext()}\n There are: {billboard.calendarDays.Count} days in this month.");
					Task.Run(async () => // delay for 5 seconds then exit menu
					{
						await Task.Delay(5000);
						Main.Bot.BillBoard.ExitMenu();
					});
				}
				break;
			case LetterViewerMenu letterViewerMenu:
				Main.Bot.LetterViewer.SetMenu(letterViewerMenu);
				if (Main.Bot.LetterViewer.HasQuest is not null && Main.Bot.LetterViewer.HasQuest.Value ||
				    Main.Bot.LetterViewer.itemsToGrab is not null && Main.Bot.LetterViewer.itemsToGrab.Value)
				{
					LetterActions.RegisterActions();
				}
				else
				{
					Task.Run(async () =>
					{
						for (int i = 0; i < Main.Bot.LetterViewer.GetMessage().Count; i++)
						{
							string message = LetterContext.GetStringContext(Main.Bot.LetterViewer.GetMessage()[i],
								i == Main.Bot.LetterViewer.GetMessage().Count - 1); // only send on last page
							Context.Send(message);
							await Task.Delay(7500);
							if (i != Main.Bot.LetterViewer.GetMessage().Count - 1)
							{
								Main.Bot.LetterViewer.NextPage();
							}
						}
						Main.Bot.LetterViewer.ExitMenu();
					});
				}

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
		Context.Send(NewDayContext());
	}

	public static void OnDayEnded(object? sender, BotDayEndedEventArgs e)
	{
		Task.Run(async () => await ShippingMenuContext()); // we cannot send in MenuChanged as it is reset by then.

		if (!Game1.player.passedOut)
		{
			Context.Send($"You have gone to bed and the day has ended. Good night.");
			return;
		}

		Context.Send($"You have passed out and the day has ended. Maybe you should go back home earlier tomorrow.");
	}
	
	private static async Task ShippingMenuContext()
	{
		List<Item> soldItems = new();
		using var enumerator = Game1.getAllFarmers().GetEnumerator();
		while (enumerator.MoveNext())
		{
			if (enumerator.Current is null) continue;
			soldItems.AddRange(Game1.getFarm().getShippingBin(enumerator.Current));
		}
		if (soldItems.Count == 0) return;

		string shipString = "These are the items you have shipped today:";
		foreach (var item in soldItems)
		{
			int sell = item.sellToStorePrice();
			shipString = string.Concat(shipString, $"\n{item.Name}: total sell price: {sell * item.Stack} single sell price: {sell}");
		}
		await Task.Delay(soldItems.Count * 750);
		Context.Send(shipString);
		Main.Bot.EndDayShippingMenu.AdvanceToNextDay();
	}

	public static string NewDayContext()
	{
		string time = StringUtilities.FormatTimeString();
		QuestContext.SendContext();
		Main.Bot.Time.GetTodayFestivalData(out Dictionary<string, string> _, out GameLocation _,
			out int startTime, out var endTime);
		string contextString = $"the current day is {SDate.Now().DayOfWeek} {SDate.Now().Day} of {SDate.Now().Season} in year {SDate.Now().Year} at time: {time}.";
		if (Main.Bot.Time.IsFestival())
		{
			contextString = $"{contextString} There is a festival today! It is located at {Game1.whereIsTodaysFest}, it will start at {startTime} and end at {endTime}," +
			                $" but you should try to go there as soon as possible so you can fully experience it.";
		}
		if (Game1.player.passedOut)
		{
			contextString = $" A new day has started. You are in your farm-house after you were knocked out, {contextString}";
		}
		else
		{
			contextString = $" A new day has started. You are in your farm-house, {contextString}";
		}

		return contextString;
	}
}