using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Messages.Outgoing;
using NeuroStardewValley.Source.Actions;
using NeuroStardewValley.Source.Utilities;
using StardewBotFramework.Source.Events.EventArgs;
using StardewValley;

namespace NeuroStardewValley.Source.EventMethods;

public class LessImportantEvents
{
	public static void OnBotDeath(object? sender, BotOnDeathEventArgs e)
	{
		Context.Send(
			$"Oh No you died! It was at {e.DeathLocation.Name} {e.DeathPoint}. You lost: {e.ItemLostAmount}");
	}

	public static void OnUiTimeChanged(object? sender, TimeEventArgs e)
	{
		if (Game1.timeOfDay % 100 != 0)
		{
			return;
		}

		string text = StringUtilities.Format24HourString();
		Context.Send($"The current time is {text}, This is sent in the 24 hour notion.", true);
	}

	public static void OnChatMessage(object? sender, ChatMessageReceivedEventArgs e)
	{
		string query;
		switch (e.ChatKind) // magic number are from the game not me :(
		{
			case 0: // normal public message
				query =
					$"In Stardew Valley, {e.PlayerName} has said {e.Message} in public chat. You can use the action to talk back to them if you want";
				break;
			case 1:
				return;
			case 2: // notification
				query =
					$"In Stardew Valley, {e.PlayerName} has said {e.Message} in public chat. You can use the action to talk back to them if you want";
				break;
			case 3: // private
				query =
					$"In Stardew Valley, {e.PlayerName} has said {e.Message} to you in a private message. You can use the action to talk back to them if you want";
				break;
			default:
				return;
		}

		ActionWindow.Create(Main.GameInstance)
			.SetForce(0, query, "")
			.AddAction(new ChatActions.SendChatMessage())
			.Register();
	}

	public static void OnBotSkillChanged(object? sender, BotSkillLevelChangedEventArgs e)
	{
		MainGameLoopEvents.SkillsChangedThisDay.Add(e.ChangedSkill, e.NewLevel);
	}

	public static void OnDayStartedSkills(object? sender, BotDayStartedEventArgs e)
	{
		MainGameLoopEvents.SkillsChangedThisDay.Clear();
	}

	public static void InventoryChanged(object? sender, BotInventoryChangedEventArgs e)
	{
		string contextString = "";
		Item item;
		if (e.Added.Any())
		{
			using var enumerator = e.Added.GetEnumerator();
			while (enumerator.MoveNext())
			{
				item = enumerator.Current;
				contextString += $"Added {item.Name} to your inventory";
				if (item.Stack > 1)
				{
					contextString += $" it's stack size was {item.Stack}";
				}

				contextString += "\n";
			}
		}

		if (e.Removed.Any())
		{
			using var enumerator = e.Removed.GetEnumerator();
			while (enumerator.MoveNext())
			{
				item = enumerator.Current;
				contextString += $"Removed {item.Name} to your inventory";
				if (item.Stack > 1)
				{
					contextString += $" It's stack size was {item.Stack}";
				}

				contextString += "\n";
			}
		}

		if (e.QuantityChanged.Any())
		{
			using var enumerator = e.QuantityChanged.GetEnumerator();
			enumerator.MoveNext();
			while (enumerator.MoveNext())
			{
				contextString +=
					$"Modified {enumerator.Current.Item.Name}'s stack amount from {enumerator.Current.OldSize} to {enumerator.Current.NewSize}.";
				contextString += $"\n";
			}
		}

		Context.Send(contextString, true);
	}
}