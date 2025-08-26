
namespace NeuroStardewValley.Source.Utilities;

public static class LetterContext
{
	public static string GetLetterContext()
	{
		string letterText = FormatLetterString(string.Concat(Main.Bot.LetterViewer.GetMessage()));
		string context = $"this is what the letter says \"{letterText}\"";
		if (Main.Bot.LetterViewer.recipeLearned != "")
		{
			context += $" You learned a recipe from this letter! It is for: {Main.Bot.LetterViewer.recipeLearned}";
		}

		if (Main.Bot.LetterViewer.itemsToGrab is not null && Main.Bot.LetterViewer.itemsToGrab.Value)
		{
			context += $" There are items to grab from this letter.";
		}

		return context;
	}

	// this will probably have to be expanded in the future.
	public static string FormatLetterString(string context)
	{
		string formatted = context.Replace("^", "\n"); // game uses carat instead of \n idk why
		return formatted;
	}
}