using NeuroSDKCsharp.Messages.Outgoing;
using NeuroStardewValley.Debug;
using StardewValley;

namespace NeuroStardewValley.Source.Utilities;

public class SendQuestContext
{
	public static string GetString()
	{
		string questString = "These are the current quests that are active.";
		foreach (var quest in Game1.player.questLog)
		{
			// if (!quest.accepted.Value) continue;
			questString = string.Concat(questString, $"\nTitle: {quest.questTitle}. Description: {quest.questDescription}. Objective: {quest.currentObjective}");
			if (quest.HasMoneyReward())
			{
				questString = string.Concat(questString, $". Reward: ${quest.moneyReward.Value}");
			}
			else if (quest.HasReward())
			{
				questString = string.Concat(questString, $". Reward: {quest.rewardDescription}");
			}

			if (quest.completed.Value)
			{
				questString = string.Concat(questString, $"This quest has been completed, you should get your reward from it.");
			}

			questString = string.Concat(questString, quest.IsTimedQuest() ? $". Time left: {quest.GetDaysLeft()}" : $". This is not a timed quest.");
		}

		return questString;
	}
	
	public static void SendContext()
	{
		string questString = GetString();
		Context.Send(questString);
	}
}