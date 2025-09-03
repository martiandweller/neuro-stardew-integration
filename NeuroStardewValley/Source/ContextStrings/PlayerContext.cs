
namespace NeuroStardewValley.Source.ContextStrings;

public static class PlayerContext
{
	public static string GetAllCharactersLevel()
	{
		string charString = "";
		foreach (var social in Main.Bot.PlayerInformation.RelationshipLevel())
		{
			charString = string.Concat(charString, $"\n{social.DisplayName} heart level: {social.HeartLevel}");
		}

		return charString;
	}

	public static string GetAllSkillLevel(bool showUi = false)
	{
		Dictionary<string,int> skills = Main.Bot.PlayerInformation.SkillLevel(showUi);

		string contextString = "";
		foreach (var kvp in skills)
		{
			contextString = string.Concat(contextString, $"\n{kvp.Key}: {kvp.Value}");
		}

		return contextString;
	}
}