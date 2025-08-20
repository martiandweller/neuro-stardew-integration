using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Messages.Outgoing;
using NeuroStardewValley.Debug;
using NeuroStardewValley.Source.Actions;
using StardewModdingAPI.Enums;
using StardewValley;
using StardewValley.Menus;

namespace NeuroStardewValley.Source.RegisterActions;

public static class RegisterLevelUpMenu
{
	public static void GetSkillContext(Dictionary<SkillType,int> skillsChangedThisDay)
	{
		LevelUpMenu? menu = Game1.activeClickableMenu as LevelUpMenu;
		if (menu is null) return;
		Logger.Info($"menu is not null");
		if (menu.leftProfession is not null || menu.rightProfession is not null)
		{
			string skillContext = "The professions you can pick, The first sent profession will always be \"Left\" with the other being \"Right\": ";
			foreach (var profession in Main.Bot.EndDaySkillMenu.ProfessionsToChoose)
			{
				skillContext += "\n";
				foreach (string desc in LevelUpMenu.getProfessionDescription(profession))
				{
					skillContext += $"{desc} ";
				}
			}
			
			ActionWindow window = ActionWindow.Create(Main.GameInstance);
			window.AddAction(new EndDayActions.PickProfession());
			window.SetForce(1, "You have ended the day and have to select a profession for one of your skills", skillContext);
			window.Register();
		}
		else // IDK why I just can't trigger this for some reason so it's not been tested. Blame the game not me pls
		{
			string skillContext = "Skills that have been changed this day: ";
			List<string> info = menu.getExtraInfoForLevel(Main.Bot.EndDaySkillMenu.CurrentSkill, Main.Bot.EndDaySkillMenu.CurrentLevel);
			foreach (var str in info)
			{
				skillContext += str;
			}
			Context.Send(skillContext);
			Task.Run(async () => await Task.Delay(5000));
			Main.Bot.EndDaySkillMenu.SelectOkButton();
			Logger.Info($"pressing ok");
		}
	}
	
	public static void RegisterActions()
	{
		
	}
}