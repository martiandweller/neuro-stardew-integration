using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Messages.Outgoing;
using NeuroStardewValley.Source.Actions;
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
		RegisterMainGameActions.LoadGameActions("Your save has loaded and you are in the game.",MainGameLoopEvents.NewDayContext());
	}

	public static void OnHUDMessageAdded(object? sender, HUDMessageAddedEventArgs e)
	{
		string context;
		string message = StringUtilities.FormatBannerMessage(e.Message);
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
}