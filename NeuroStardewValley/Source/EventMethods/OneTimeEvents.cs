using NeuroSDKCsharp.Messages.Outgoing;
using NeuroStardewValley.Debug;
using NeuroStardewValley.Source.RegisterActions;
using NeuroStardewValley.Source.Utilities;
using StardewBotFramework.Source.Events.EventArgs;
using StardewBotFramework.Source.Modules.Pathfinding.Base;
using StardewModdingAPI.Events;
using StardewValley;

namespace NeuroStardewValley.Source.EventMethods;

public static class OneTimeEvents
{
	public static void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
	{
		RegisterMainActions.RegisterPostAction();
	}

	public static void OnHUDMessageAdded(object? sender, HUDMessageAddedEventArgs e)
	{
		if (e is {MessageSubjectItem: not null}) // remove banners that say they add items (may have other effects no other way though)
		{
			Logger.Info($"e was null");
			return;	
		}
		string context;
		string message = StringUtilities.FormatBannerMessage(e.Message);
		// switch (e.WhatType) // these are the types listed on the stardew wiki lists in the CommonTasks/UserInterface section
		// {
		// 	case 1:
		// 		context = $"You have completed an achievement: {message}";
		// 		break;
		// 	case 2:
		// 		context = $"You have a new quest: {message}";
		// 		break;
		// 	case 3:
		// 		context = $"An error has appeared it says: {message}";
		// 		break;
		// 	case 4:
		// 		context = $"A banner message about stamina has appeared: {message}";
		// 		break;
		// 	case 5:
		// 		context = $"A banner message about health has appeared: {message}";
		// 		break;
		// 	default:
		// 		context = $"A banner message has appeared it says: {message}";
		// 		break;
		// }
		// the game doesn't really stick to what is used in the switch statement so just doing this for now.
		context = $"A banner message has appeared it says: {message}";

		// TODO: causes stutter, I think from async and stuff
		Context.Send(context);
	}
	
	public static void FailedCharacterController(object? sender, CharacterController.FailureReason e)
	{
		if (Main.Config.Debug) Game1.addHUDMessage(new HUDMessage($"Pathfinding failed :( from {e}"));
		if (e == CharacterController.FailureReason.NoCharacter)
		{
			Context.Send($"The character you were either following or attacking left this location.");
			return;
		}
		Context.Send($"The character controller failed due to {e}, you should mention this so it can get fixed." +
		             $" You should also do something different from what you were doing before.");
		// most of the action should re-register on their own
	}
}