using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Messages.Outgoing;
using NeuroStardewValley.Debug;
using NeuroStardewValley.Source.Actions;
using StardewModdingAPI.Enums;
using StardewValley;
using StardewValley.Menus;

namespace NeuroStardewValley.Source.RegisterActions;

public class RegisterLevelUpMenu
{
	public static void GetSkillContext(Dictionary<SkillType,int> skillsChangedThisDay)
	{
		LevelUpMenu? menu = Game1.activeClickableMenu as LevelUpMenu;
		if (menu is null) return;
		Logger.Info($"menu is not null");
		if (menu.leftProfession is not null || menu.rightProfession is not null)
		{
			string skillContext = "Skills that have been changed this day: ";
			// foreach (var kvp in skillsChangedThisDay)
			// {
			// 	skillContext += $"\n {kvp.Key.ToString()}: new level: {kvp.Value}";
			// }
			// while (!menu.isActive)
			// {
			// }
			foreach (var profession in Main.Bot.EndDaySkillMenu.ProfessionsToChoose)
			{
				Logger.Info($"profession: {profession}");
				skillContext += LevelUpMenu.getProfessionTitleFromNumber(profession);
				foreach (string desc in LevelUpMenu.getProfessionDescription(profession))
				{
					skillContext += desc;
				}
			}
			
			Logger.Info($"registering actions");
			ActionWindow window = ActionWindow.Create(Main.GameInstance);
			window.AddAction(new EndDayActions.PickProfession());
			window.SetForce(1, "You have ended the day and have to select a profession for one of your skills", skillContext);
			window.Register();

			// while (!menu.hasUpdatedProfessions || menu.isActive)
			// {
			// }
		}
		else
		{
			// string skillContext = $"The skills that was level up: {skills.Key} level: {skills.Value}";
			// Context.Send(skillContext);
			// while (menu!.isActive)
			// {}
			Logger.Info($"press ok button");
			// while (!menu.CanReceiveInput()) {}
			Main.Bot.EndDaySkillMenu.SelectOkButton();
		}
	}
	
	public static void RegisterActions()
	{
		
	}
}