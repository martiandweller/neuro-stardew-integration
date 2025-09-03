using StardewValley;
using StardewValley.Menus;

namespace NeuroStardewValley.Source.Utilities;

public class InventoryUtils
{
	public static void ClickFirstEmptySlot(List<ClickableComponent> clickableComponents, IClickableMenu menu)
	{
		for (int i = 0; i < Main.Bot.Inventory.Inventory.Count; i++)
		{
			if (Main.Bot.Inventory.Inventory[i] is not null) continue;

			ClickableComponent cc = clickableComponents[i];
			menu.receiveLeftClick(cc.bounds.X,cc.bounds.Y);
			break;
		}
	}
}