using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Messages.Outgoing;
using NeuroStardewValley.Debug;
using NeuroStardewValley.Source.Actions;
using NeuroStardewValley.Source.RegisterActions;
using NeuroStardewValley.Source.Utilities;
using StardewBotFramework.Source.Events.EventArgs;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace NeuroStardewValley.Source;

public static class EventMethods
{
	private static readonly Dictionary<SkillType, int> SkillsChangedThisDay = new();

	public static class SingleEvents
	{
		public static void CharacterCreatorMenu(object? sender, MenuChangedEventArgs e)
		{
			Logger.Info($"Menu has been changed to: {e}");
			if (e.NewMenu is CharacterCustomization)
			{
				ActionWindow window = ActionWindow.Create(Main.GameInstance)
					.SetForce(2, "", "")
					.AddAction(new MainMenuActions.CreateCharacter());
				window.Register();
			}
		}
		public static void GameLoopOnSaveLoaded(object? sender, SaveLoadedEventArgs e)
		{
			string time = StringUtilities.FormatTimeString();
			RegisterMainGameActions.LoadGameActions("Your save has loaded and you are in the game. You have started in your farm-house.",$"the current day is {SDate.Now().DayOfWeek} {SDate.Now().Day} of {SDate.Now().Season} in year {SDate.Now().Year} at time: {time}.");
		}
		
		public static void OnHUDMessageAdded(object? sender, HUDMessageAddedEventArgs e)
		{
			string context;
			string message = StringUtilities.FormatBannerMessage(e.Message);
			switch (e.WhatType) // these are the ones one the stardew wiki lists in the CommonTasks/UserInterface section
			{
				case 1:
					context = $"You have completed an achievement: {message}";
					break;
				case 2:
					context = $"You have a new quest: {message}";
					break;
				case 3:
					context = $"An error has appeared it says: {message}";
					break;
				case 4:
					context = $"A banner message about stamina has appeared: {message}";
					break;
				case 5:
					context = $"A banner message about health has appeared: {message}";
					break;
				default:
					context = $"A banner message has appeared it says: {message}";
					break;
			}
			Context.Send(context);
		}
	}
	public static class MainGameLoop
	{
		public static void OnWarped(object? sender, BotWarpedEventArgs e)
		{
			string tilesString = "";
			foreach (var tile in WarpUtilities.GetTilesInLocation(e.NewLocation))
			{
				tilesString += "\n" + tile;
			}
			Context.Send($"You have moved to {e.NewLocation.Name} from {e.OldLocation.Name}.\n These are the tiles that have an object on them around you: {tilesString}");
        
			string warps = WarpUtilities.GetWarpTiles(e.NewLocation);
			string warpsString = WarpUtilities.GetWarpTilesString(warps);
        
			Context.Send(warpsString,true);
			RegisterMainGameActions.RegisterPostAction(e,0,"Test Query","Test State",true);
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
	}

	public static class LessImportantLoop
	{
		public static void OnBotDeath(object? sender, BotOnDeathEventArgs e)
		{
			Context.Send($"Oh No you died! It was at {e.DeathLocation.Name} {e.DeathPoint}. You lost: {e.ItemLostAmount}");
		}
		
		public static void OnUiTimeChanged(object? sender, TimeEventArgs e)
		{
			if (Game1.timeOfDay % 100 != 0)
			{
				 return;
			}
			string text = StringUtilities.Format24HourString();
			Context.Send($"The current time is {text}, This is sent in the 24 hour notion.",true);
		}
		
		public static void OnChatMessage(object? sender, ChatMessageReceivedEventArgs e)
		{
			string query;
			switch (e.ChatKind) // magic number are from the game not me :(
			{
				case 0: // normal public message
					query = $"In Stardew Valley, {e.PlayerName} has said {e.Message} in public chat. You can use the action to talk back to them if you want";
					break;
				case 1:
					return;
				case 2: // notification
					query = $"In Stardew Valley, {e.PlayerName} has said {e.Message} in public chat. You can use the action to talk back to them if you want";
					break;
				case 3: // private
					query = $"In Stardew Valley, {e.PlayerName} has said {e.Message} to you in a private message. You can use the action to talk back to them if you want";
					break;
				default:
					return;
			}
        
			ActionWindow.Create(Main.GameInstance)
				.SetForce(0,query,"")
				.AddAction(new ChatActions.SendChatMessage())
				.Register();
		}
		
		public static void OnBotSkillChanged(object? sender, BotSkillLevelChangedEventArgs e)
		{
			SkillsChangedThisDay.Add(e.ChangedSkill,e.NewLevel);
		}
    
		public static void OnDayStartedSkills(object? sender, BotDayStartedEventArgs e)
		{
			SkillsChangedThisDay.Clear();
		}
	}

	private static void ShippingMenuContext()
	{
		if (Game1.player.displayedShippedItems.Count == 0) return;

		Task.Run(async () => await Task.Delay(Game1.player.displayedShippedItems.Count * 500)); // wait for it to show all results, we multiply as Task.Delay is in milliseconds
		string shipString = "These are the items you have shipped today,";
		foreach (var item in Game1.player.displayedShippedItems)
		{
			int sell = item.sellToStorePrice();
			shipString = string.Concat(shipString, $"\n{item.Name}: total sell price: {sell * item.Stack} single sell price: {sell}");
		}
		Context.Send(shipString);
		Main.Bot.EndDayShippingMenu.AdvanceToNextDay();
	}
}