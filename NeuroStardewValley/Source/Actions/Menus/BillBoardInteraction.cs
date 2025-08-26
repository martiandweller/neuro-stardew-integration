using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Websocket;
using NeuroStardewValley.Source.Utilities;
using StardewValley.Menus;

namespace NeuroStardewValley.Source.Actions.Menus;

public static class BillBoardInteraction
{
	public class AcceptDailyQuest : NeuroAction
	{
		public override string Name => "accept_quest";
		protected override string Description => "Accept this quest";
		protected override JsonSchema? Schema => new();
		protected override ExecutionResult Validate(ActionData actionData)
		{
			return ExecutionResult.Success();
		}

		protected override void Execute()
		{
			Main.Bot.BillBoard.AcceptDailyQuest();
			Main.Bot.BillBoard.ExitMenu();
		}
	}

	public static void RegisterQuestActions()
	{
		Main.Bot.BillBoard.GetDailyQuest(out string title,out string description,out var objective);
		ActionWindow window = ActionWindow.Create(Main.GameInstance);
		window.AddAction(new AcceptDailyQuest());
		window.SetForce(0, "You have opened the daily quest billboard.",
			$"This is the quest today: {title}.\n{StringUtilities.FormatDailyQuest(description)}\n\n{objective}");
		window.Register();
	}

	public static string GetCalendarContext()
	{
		string context = "";

		foreach (var kvp in Main.Bot.BillBoard.GetCalendar())
		{
			context += $"\nDay: {kvp.Key} event type: {kvp.Value.Type}";
			if (kvp.Value.Type != Billboard.BillboardEventType.None)
			{
				context = $"{context} {kvp.Value.HoverText}";
			}
		}
		
		return context;
	}
}