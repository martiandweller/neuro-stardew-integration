
namespace NeuroStardewValley.Source.Utilities;

public static class DialogueUtils
{
	private static readonly string[] EmotionCommandStrings = { "$0", "$1", "$2", "$3", "$4", "$5", "$h", "$s", "$u", "$l", "$a" };
	private static readonly string[] EmotionStrings =
		{ "Neutral", "Happy", "Sad", "Unique", "Love", "Angry", "Happy", "Sad", "Unique", "Love", "Angry" };
	public static string ReplaceEmotionSymbols(string str)
	{
		foreach (var emotion in EmotionCommandStrings)
		{
			if (str.Contains(emotion))
			{
				if (emotion is "$4" or "$u")
				{
					return ""; // wiki doesn't contain unique portraits :(
				}
				return EmotionStrings[EmotionCommandStrings.ToList().IndexOf(emotion)];
			}
		}

		return str;
	}
}