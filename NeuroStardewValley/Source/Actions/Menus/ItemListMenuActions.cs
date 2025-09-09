using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Websocket;
using StardewBotFramework.Debug;

namespace NeuroStardewValley.Source.Actions.Menus;

public class ItemListMenuActions
{
	public class ExitMenu : NeuroAction
	{
		public override string Name => "exit_menu";
		protected override string Description => "Exit this menu.";
		protected override JsonSchema Schema => new();
		protected override ExecutionResult Validate(ActionData actionData)
		{
			return ExecutionResult.Success($"");
		}

		protected override void Execute()
		{
			Main.Bot.ItemListMenu.ClickOk();
			Main.Bot.ItemListMenu.RemoveMenu();
		}
	}

	public static void RegisterActions()
	{
		ActionWindow window = ActionWindow.Create(Main.GameInstance);
		window.AddAction(new ExitMenu());
		var items = Main.Bot.ItemListMenu.GetItems().Select(item => $"\n{item.Name} amount: {item.stack}");
		var enumerable = items.ToList();
		var itemsList = enumerable.ToList();
		Logger.Info($"items: {itemsList.Count}");
		window.SetForce(0, "You are in a menu, that tells you about the items you lost.",
			$"{itemsList}");
		window.Register();
	}
}