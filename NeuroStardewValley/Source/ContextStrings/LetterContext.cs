
namespace NeuroStardewValley.Source.ContextStrings;

public static class LetterContext
{
	public static string GetFullLetterString()
	{
		string letterText = FormatLetterString(string.Concat(Main.Bot.LetterViewer.GetMessage()));
		string context = $"This is what the letter says \"{letterText}\"";
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

	public static string GetStringContext(string message,bool addExtraContext = true, int page = -1)
	{
		string letterText = FormatLetterString(message);
		string context = "This is what {0} says \"{1}\"";
		context = page != -1 ? string.Format(context, $"page {page} of this letter",letterText) : string.Format(context, "the letter",letterText);
		if (!addExtraContext)
		{
			return context;
		}
		
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