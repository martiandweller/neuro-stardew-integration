using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Messages.Outgoing;
using NeuroStardewValley.Debug;
using NeuroStardewValley.Source.Actions.Menus;
using NeuroStardewValley.Source.RegisterActions;
using NeuroStardewValley.Source.Utilities;
using StardewBotFramework.Source.Events.EventArgs;
using StardewModdingAPI.Events;
using StardewValley.Menus;

namespace NeuroStardewValley.Source.EventMethods;

public class OneTimeEvents
{
	public static void CharacterCreatorMenu(object? sender, MenuChangedEventArgs e)
	{
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
		RegisterMainGameActions.LoadGameActions("Your save has loaded and you are in the game.",MainGameLoopEvents.NewDayContext(false));
	}

	public static void OnHUDMessageAdded(object? sender, HUDMessageAddedEventArgs e)
	{
		string context;
		string message = StringUtilities.FormatBannerMessage(e.Message);
		if (e is {MessageSubjectItem: not null}) // remove banners that say they add items (may have other effects no other way though)
		{
			Logger.Info($"e was null");
			return;	
		}
		switch (e.WhatType) // these are the types listed on the stardew wiki lists in the CommonTasks/UserInterface section
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
	
	public static void FailedCharacterController(object? sender, EventArgs e)
	{
		Context.Send($"The character controller failed, you should mention this so it can get fixed." +
		             $" You should also do something different from what you were doing before.");
		RegisterMainGameActions.RegisterPostAction();
	}
}