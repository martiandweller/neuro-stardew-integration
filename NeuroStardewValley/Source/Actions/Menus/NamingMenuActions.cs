using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Websocket;
using StardewValley.Menus;

namespace NeuroStardewValley.Source.Actions.Menus;

public class NamingMenuActions
{
	public class SetName : NeuroAction<string>
	{
		public override string Name => "set_name";
		protected override string Description => $"Set the name for this, you can use a maximum of " +
		                                         $"{Main.Bot.NamingMenu.Menu.textBox.textLimit} characters with a " +
		                                         $"minimum of {Main.Bot.NamingMenu.Menu.minLength} character";
		protected override JsonSchema Schema => new()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "name" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["name"] = QJS.Type(JsonSchemaType.String)
			}
		};
		protected override ExecutionResult Validate(ActionData actionData, out string? resultData)
		{
			string? name = actionData.Data?.Value<string>("name");

			resultData = null;
			if (name is null)
			{
				return ExecutionResult.Failure($"You provided a null value to a string.");
			}

			if (name.Length > Main.Bot.NamingMenu.Menu.textBox.textLimit ||
			    name.Length <= Main.Bot.NamingMenu.Menu.minLength)
			{
				return ExecutionResult.Failure(
					$"The text you added did not adhere to the text box's limit, you can only type a max of" +
					$" {Main.Bot.NamingMenu.Menu.textBox.textLimit} character and a minimum of" +
					$" {Main.Bot.NamingMenu.Menu.minLength} character.");
			}

			resultData = name;
			return ExecutionResult.Success($"You have added {name}");
		}

		protected override void Execute(string? resultData)
		{
			if (resultData is null) return;
			Main.Bot.NamingMenu.ChangeName(resultData);
			Main.Bot.NamingMenu.DoneNaming();
		}
	}
	public class RandomName : NeuroAction
	{
		public override string Name => "randomize_name";
		protected override string Description => "Press the random name button and get a random name from the game.";
		protected override JsonSchema Schema => new();
		protected override ExecutionResult Validate(ActionData actionData)
		{
			return ExecutionResult.Success();
		}

		protected override void Execute()
		{
			Main.Bot.NamingMenu.RandomizeName();
			RegisterActions();
		}
	}

	public static void RegisterActions()
	{
		ActionWindow window = ActionWindow.Create(Main.GameInstance);

		window.AddAction(new SetName());
		if (Main.Bot.NamingMenu.Menu.randomButton.visible) window.AddAction(new RandomName());

		string state = $"{Main.Bot.NamingMenu.Menu.title}";
		
		if (Main.Bot.NamingMenu.Menu is TitleTextInputMenu textInputMenu) state = $"{textInputMenu.title}";

		if (Main.Bot.NamingMenu.Menu.textBox.Text != "") state += $" The current text is {Main.Bot.NamingMenu.Menu.textBox.Text}";
		window.SetForce(0, $"You are now interacting with a menu that has the title " +
		                   $"\"{Main.Bot.NamingMenu.Menu.title}\", you should enter text to fit this.", state);
		window.Register();
	}
}