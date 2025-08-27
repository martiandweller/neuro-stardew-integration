using NeuroSDKCsharp.Actions;
using NeuroStardewValley.Debug;
using NeuroStardewValley.Source.Actions;
using NeuroStardewValley.Source.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace NeuroStardewValley.Source.RegisterActions;

public static class RegisterDialogueActions
{
	public static void RegisterActions()
	{
		Logger.Info($"running dialogue register actions");
		ActionWindow window = ActionWindow.Create(Main.GameInstance);
		if (Game1.activeClickableMenu is DialogueBox dialogueBox)
		{
			string stateString;
			Logger.Info($"dialoguebox response length: {dialogueBox.responses.Length}    getCurrentDialogue: {string.Concat(dialogueBox.dialogues)}");
			if (dialogueBox.responses.Length > 0)
			{
				window.AddAction(new DialogueActions.DialogueResponse());				
				stateString = $"The possible responses are \n";
				for (int i = 0; i < dialogueBox.responses.Length; i++)
				{
					stateString += $"{i}: {dialogueBox.responses[i].responseText} \n";	
				}
			}
			else
			{
				window.AddAction(new DialogueActions.AdvanceDialogue());
				if (dialogueBox.characterDialogue is not null)
				{
					Main.Bot.Dialogue.SetCurrentDialogue(dialogueBox.characterDialogue);
					stateString = $"{dialogueBox.characterDialogue.speaker.Name} is talking to you, they said: {dialogueBox.characterDialogue.getCurrentDialogue()}";
					if (DialogueUtils.ReplaceEmotionSymbols(dialogueBox.characterDialogue.CurrentEmotion) != "")
					{
						stateString +=
							$"\n They look {DialogueUtils.ReplaceEmotionSymbols(dialogueBox.characterDialogue.CurrentEmotion)}";
					}
				}
				else
				{
					stateString = $"The current dialogue is {dialogueBox.getCurrentString()}";
				}
			}
			
			window.SetForce(2, "", stateString);
			window.Register();
		}
		else
		{
			Logger.Error($"Current active clickable menu is not a DialogueBox");
		}
	}
}