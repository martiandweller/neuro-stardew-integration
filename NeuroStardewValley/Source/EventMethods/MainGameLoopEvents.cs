using NeuroSDKCsharp.Messages.Outgoing;
using NeuroStardewValley.Debug;
using NeuroStardewValley.Source.Actions;
using NeuroStardewValley.Source.Actions.Menus;
using NeuroStardewValley.Source.Actions.ObjectActions;
using NeuroStardewValley.Source.ContextStrings;
using NeuroStardewValley.Source.RegisterActions;
using NeuroStardewValley.Source.Utilities;
using StardewBotFramework.Source.Events.EventArgs;
using StardewBotFramework.Source.Events.World_Events;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;
using StardewValley.Objects;

namespace NeuroStardewValley.Source.EventMethods;

public static class MainGameLoopEvents
{
	public static readonly Dictionary<SkillType, int> SkillsChangedThisDay = new();

	#region RegisterActions

	public static void OnWarped(object? sender, BotWarpedEventArgs e)
	{
		TileContext.ActionableTiles.Clear();
		if (e.Player.passedOut || Game1.eventUp) return;
		string warps = TileContext.GetWarpTiles(e.NewLocation,true);
		string warpsString = !string.IsNullOrEmpty(warps) ? TileContext.GetWarpTilesString(warps) : "There are no warps in this location";
		
		string characterContext = string.Concat(Main.Bot.Characters.GetCharactersInCurrentLocation(e.NewLocation)
			.Select(kvp => $"{kvp.Value.Name} is at {kvp.Key} in this location").ToList());
		
		Context.Send(warpsString, true);
		Context.Send(characterContext,true);
		RegisterMainGameActions.RegisterPostAction(e, 0,
			$"You are at {e.NewLocation.DisplayName} from {e.OldLocation.DisplayName}, The current weather is {Main.Bot.WorldState.GetCurrentLocationWeather().Weather}." +
			$" These are the items in your inventory: {InventoryContext.GetInventoryString(Main.Bot._farmer.Items, true)} " +
			$"\nIf you want more information about your items should open your inventory.");
	}

	public static void OnMenuChanged(object? sender, BotMenuChangedEventArgs e)
	{
		Logger.Info($"current menu: {e.NewMenu}");
		switch (e.OldMenu)
		{
			// handled by caughtFish event
			case BobberBar:
			// we need to check if old is dialogue box to stop issues with changing menus while standing in bed
			case DialogueBox when Game1.player.isInBed.Value:
				return;
			case DialogueBox when e.NewMenu is not DialogueBox:
				Main.Bot.Dialogue.CurrentDialogueBox = null;
				break;
		}

		switch (e.NewMenu)
		{
			case CharacterCustomization customization:
				Main.Bot.CharacterCreation.SetCreator(customization);
				MainMenuActions.RegisterAction();
				break;
			case DialogueBox dialogueBox:
				Logger.Info($"add new dialogue box");
				Main.Bot.Dialogue.CurrentDialogueBox = dialogueBox;
				RegisterDialogueActions.RegisterActions();
				break;
			case GameMenu menu:
				switch (menu.GetCurrentPage())
				{
					case InventoryPage:
						InventoryActions.RegisterInventoryActions();
						break;
					case CraftingPage:
						CraftingActions.RegisterActions();
						break;
				}
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
				switch (itemGrabMenu.context)
				{
					case Chest chest:
						Main.Bot.ItemGrabMenu.SetUI(itemGrabMenu);
						Main.Bot.Chest.SetChest(chest);
						ChestActions.Chest = chest;
						ChestActions.RegisterChestActions(true);
						return;
					case ShippingBin:
						Main.Bot.ShippingBinInteraction.SetUI(itemGrabMenu);
						ShippingBinActions.RegisterBinActions();
						return;
				}

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
					Context.Send($"These are the event that are happening this season, {BillBoardInteraction.GetCalendarContext()}" +
					             $"\nThere are: {billboard.calendarDays.Count} days in this season. It is currently day {SDate.Now().Day} of {SDate.Now().Season}.");
					Task.Run(async () => // delay for 6.5 seconds then exit menu
					{
						await Task.Delay(6500);
						Main.Bot.BillBoard.ExitMenu();
					});
				}
				break;
			case LetterViewerMenu letterViewerMenu:
				Main.Bot.LetterViewer.SetMenu(letterViewerMenu);
				
				if (letterViewerMenu.HasQuestOrSpecialOrder || letterViewerMenu.itemsLeftToGrab())
				{
					LetterActions.RegisterActions();
				}
				else
				{
					Task.Run(async () =>
					{
						for (int i = 0; i <= Main.Bot.LetterViewer.GetMessage().Count; i++)
						{
							string message = LetterContext.GetStringContext(Main.Bot.LetterViewer.GetMessage()[i],
									i == Main.Bot.LetterViewer.GetMessage().Count - 1,i + 1); // only send extra on last page
							Context.Send($"{message}");
							await Task.Delay(9000); // surely 9 seconds per page is enough :Glueless:
							if (i != Main.Bot.LetterViewer.GetMessage().Count - 1)
							{
								Main.Bot.LetterViewer.NextPage();
							}
						}
						Main.Bot.LetterViewer.ExitMenu();
					});
				}

				break;
			case JunimoNoteMenu junimoNoteMenu:
				Main.Bot.JunimoNote.SetMenu(junimoNoteMenu);
				JunimoNoteActions.RegisterActions();
				break;
			case PurchaseAnimalsMenu animalsMenu:
				Main.Bot.AnimalMenu.SetUI(animalsMenu);
				BuyAnimalsActions.RegisterActions();
				break;
			case ItemListMenu itemListMenu:
				Main.Bot.ItemListMenu.SetMenu(itemListMenu);
				ItemListMenuActions.RegisterActions();
				break;
			case MineElevatorMenu mineElevatorMenu:
				Main.Bot.ElevatorMenu.SetMenu(mineElevatorMenu);
				ElevatorMenuActions.RegisterAction();
				break;
			case NamingMenu namingMenu: // works for naming horses and placing signs
				Main.Bot.NamingMenu.setUI(namingMenu);
				NamingMenuActions.RegisterActions();
				break;
			default:
				if (e.NewMenu is null or QuestLog) break;
				Context.Send(string.Format(ResultStrings.InvalidClickableMenu,$"{e.NewMenu}"));
				e.NewMenu.exitThisMenu(false);
				break;
		}
		
		// ugly but it gets rid of warning and double send at start of game and other double sends
		if (e is { NewMenu: null } and {OldMenu:not TitleMenu or not LevelUpMenu or not MineElevatorMenu})
		{
			Logger.Info($"Re-registering post action: old menu: {e.OldMenu.GetType()}  new: {e.NewMenu}");
			RegisterMainGameActions.RegisterPostAction();
		}
	}
	
	// may need to check what event was ended in the future
	public static void EventFinished(object? sender, EventEndedEventArgs e)
	{
		Logger.Info($"Running event finished: event: {e.Event}  can move after: {e.Event.canMoveAfterDialogue()}");
		if (e.Event.exitLocation is not null)
		{
			Logger.Error($"exit location: {e.Event.exitLocation.Name}  {e.Event.exitLocation.Location}  {e.Event.exitLocation.IsRequestFor(e.Event.exitLocation.Location)}");
			return; // player should get warped after event
		}
		Task.Run(async () =>
		{
			await Task.Delay(15); // this is because sometimes events finish before giving back control
			Logger.Info($"post event task delay");
			RegisterMainGameActions.RegisterPostAction();
		});
	}

	#endregion

	#region ContextEvents

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
	
	public static void OnBotDamaged(object? sender, BotDamagedEventArgs e)
	{
		Context.Send($"You were damaged by {e.Damager.Name}, they did {e.Damaged} damage. You now have {Main.Bot.PlayerInformation.Health} health left.");
	}
	
	public static void LocationNpcChanged(object? sender, BotCharacterListChangedEventArgs e)
	{
		foreach (var npc in e.Added)
		{
			Context.Send($"{npc.Name} has entered this location, they are at {npc.TilePoint}.",true);
		}

		foreach (var npc in e.Removed)
		{
			Context.Send($"{npc.Name} has exited this location.",true);
		}
	}

	#endregion

	#region ContextHelpers

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

	public static string NewDayContext(bool sendQuests = true)
	{
		string time = StringUtilities.FormatTimeString();
		if (sendQuests) Context.Send($"These are the title's of the quests that are available, " +
		                             $"you can see more about them in your quest log.{QuestContext.GetQuestTitles()}");
		Main.Bot.Time.GetTodayFestivalData(out _, out _, out int startTime, out int endTime);
		string passedOut = Game1.player.passedOut
			? "A new day has started, you are in your farm-house after you passed out, "
			: "A new day has started, you are in your farm-house,"; 
		string contextString = $"{passedOut} the current day is {SDate.Now().DayOfWeek} {SDate.Now().Day} of {SDate.Now().Season} in year {SDate.Now().Year} at time: {time}.";
		
		if (Main.Bot.Time.IsFestival())
		{
			contextString += $" There is a festival today! It is located at {Game1.whereIsTodaysFest}, it will start at {startTime} and end at {endTime}," +
			                $" but you should try to go there as soon as possible so you can fully experience it.";
		}
		
		contextString += $" This is the level of your relationship with all the characters you have interacted with: " +
		                 $"{PlayerContext.GetAllCharactersLevel()}.\nAnd these are the levels of all your skills:" +
		                 $" {PlayerContext.GetAllSkillLevel()}.";
		
		return contextString;
	}

	#endregion
}