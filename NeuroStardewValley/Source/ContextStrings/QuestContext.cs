using NeuroSDKCsharp.Messages.Outgoing;
using StardewValley.Quests;

namespace NeuroStardewValley.Source.ContextStrings;

public static class QuestContext
{
	public static string GetSingleQuest(Quest quest)
	{
		string questString = "";
		questString = string.Concat(questString, $"\nTitle: {quest.questTitle}. Description: {quest.questDescription} Objective: {quest.currentObjective}");
		if (quest.HasMoneyReward())
		{
			questString = string.Concat(questString, $" Reward: ${quest.moneyReward.Value}");
		}
		else if (quest.HasReward())
		{
			questString = string.Concat(questString, $" Reward: {quest.rewardDescription}");
		}

		if (quest.completed.Value)
		{
			questString = string.Concat(questString, $"This quest has been completed, you should get your reward from it.");
		}
		else
		{
			questString = string.Concat(questString, $"You have not completed this quest yet.");
		}

		return string.Concat(questString, quest.IsTimedQuest() ? $" Time left: {quest.GetDaysLeft()}" : $" There is no time limit on this quest.");
	}
	
	public static string GetQuestsStrings()
	{
		string questString = "These are the current quests that are active.";
		foreach (var quest in Main.Bot.QuestLog.Quests)
		{
			questString = string.Concat(questString,GetSingleQuest(quest));
		}

		return questString;
	}
	
	public static void SendContext()
	{
		string questString = GetQuestsStrings();
		Context.Send(questString);
	}

	public static string GetQuestTitles()
	{
		return Main.Bot.QuestLog.Quests.Aggregate("", (current, quest) => string.Concat(current, $"\n{quest.questTitle}"));
	}
}