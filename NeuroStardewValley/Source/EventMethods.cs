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
	private static Dictionary<SkillType, int> _skillsChangedThisDay = new();

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
			string time = Utilities.StringUtilities.FormatTimeString();
			Context.Send($"Your save has loaded and you are now in the game. You are in your farm-house, " +
			             $"the current day is {SDate.Now().DayOfWeek} {SDate.Now().Day} of {SDate.Now().Season} in year {SDate.Now().Year} at time: {time}.");
			RegisterMainGameActions.LoadGameActions();
		}
		
		public static void OnHUDMessageAdded(object? sender, HUDMessageAddedEventArgs e)
		{
			string context;
			string message = Utilities.StringUtilities.FormatBannerMessage(e.Message);
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
			ActionWindow window = ActionWindow.Create(Main.GameInstance);
			string tilesString = "";
			foreach (var tile in WarpUtilities.GetTilesInLocation(e.NewLocation))
			{
				tilesString += "\n" + tile;
			}
			Context.Send($"You have moved to {e.NewLocation.Name} from {e.OldLocation.Name}.\n These are the tiles that have an object on them around you: {tilesString}");
        
			string warps = WarpUtilities.GetWarpTiles(e.NewLocation);
			string warpsString = WarpUtilities.GetWarpTilesString(warps);
        
			Context.Send(warpsString,true);
			window.SetForce(0, "", "");
			RegisterMainGameActions.RegisterActions(window);
			RegisterMainGameActions.RegisterToolActions(window,e);
			RegisterMainGameActions.RegisterLocationActions(window,Game1.currentLocation);
        
			window.Register();
		}

		public static void OnMenuChanged(object? sender, BotMenuChangedEventArgs e)
		{
			Logger.Info($"current menu: {e.NewMenu}");
			if (e.OldMenu is DialogueBox)
			{
				Logger.Info($"Removing dialogue box");
				Main.Bot.Dialogue.CurrentDialogue = null;
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
			}
		}
		
		public static void OnDayStarted(object? sender, BotDayStartedEventArgs e)
		{
			string time = Utilities.StringUtilities.FormatTimeString();
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
		
		public static void OnDayEnded(object? sender, BotDayEndedEventArgs e) // TODO: check this code works 
		{
			// TODO: register UI actions for end of day (or just automate this and only send context)
			foreach (var skills in _skillsChangedThisDay)
			{
				if (skills.Value is 5 or 10)
				{
					string skillContext = "Skills that have been changed this day: ";
					foreach (var kvp in _skillsChangedThisDay)
					{
						skillContext += $"\n {kvp.Key.ToString()}: new level: {kvp.Value}";
					}
					LevelUpMenu? menu = Game1.activeClickableMenu as LevelUpMenu;
					if (menu is null) return;
					Main.Bot.EndDaySkillMenu.SetMenu(menu);
					while (!menu.isActive)
					{
					}
					if (menu is { isActive: true} && menu.leftProfession.visible || menu.rightProfession.visible)
					{
						ActionWindow window = ActionWindow.Create(Main.GameInstance);
						window.AddAction(new EndDayActions.PickProfession());
						window.SetForce(0, "You have ended the day and have to select a profession for one of your skills", skillContext);
					}

					while (!menu.hasUpdatedProfessions || menu.isActive)
					{
					}
				}
				else
				{
					string skillContext = "Skills that have been changed this day: ";
					foreach (var kvp in _skillsChangedThisDay)
					{
						skillContext += $"\n {kvp.Key.ToString()}: new level: {kvp.Value}";
					}
					Context.Send(skillContext);
					LevelUpMenu? menu = Game1.activeClickableMenu as LevelUpMenu;
					Main.Bot.EndDaySkillMenu.SetMenu(menu!);
					Main.Bot.EndDaySkillMenu.SelectOkButton();
					while (menu!.isActive)
					{
					}
				}
			}

			_ = Delay(); // wait for it to show all results
			string shipString = "These are the items you have shipped today,";
			foreach (var item in Game1.player.displayedShippedItems)
			{
				int sell = item.sellToStorePrice();
				shipString = string.Concat(shipString, $"\n{item.Name}: total sell price: {sell * item.Stack} single sell price: {sell}");
			}
			Context.Send(shipString);
			ShippingMenu? shippingMenu = Game1.activeClickableMenu as ShippingMenu;
			Main.Bot.EndDayShippingMenu.SetMenu(shippingMenu!);
			Main.Bot.EndDayShippingMenu.AdvanceToNextDay();
		}

		static async Task Delay()
		{
			await Task.Delay(5000);
		}
	}

	public static class LessImportantLoop
	{
		public static void OnUiTimeChanged(object? sender, TimeEventArgs e)
		{
			if (Game1.timeOfDay % 100 != 0)
			{
				 return;
			}
			string text = Utilities.StringUtilities.Format24HourString();
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
			_skillsChangedThisDay.Add(e.ChangedSkill,e.NewLevel);
		}
    
		public static void OnDayStartedSkills(object? sender, BotDayStartedEventArgs e)
		{
			_skillsChangedThisDay.Clear();
		}
	}
}