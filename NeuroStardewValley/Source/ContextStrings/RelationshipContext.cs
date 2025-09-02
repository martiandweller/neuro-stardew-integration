using NeuroStardewValley.Debug;

namespace NeuroStardewValley.Source.ContextStrings;

public static class RelationshipContext
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
}