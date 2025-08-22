using StardewValley;
using StardewValley.Inventories;

namespace NeuroStardewValley.Source.Utilities;

public static class InventoryContext
{
	public static string GetItemString(IInventory inventory,Item item,bool includeIndex = false)
	{
		string contextString = "";
		if (includeIndex)
		{
			contextString += $"Index: {inventory.IndexOf(item)} ";
		}

		contextString += $"Name: {item.Name}";
		contextString = string.Concat(contextString, $" Amount: {item.Stack}");
		if (item.Quality > 0)
		{
			contextString = string.Concat(contextString, $" Quality: {item.Quality}");
		}

		return contextString;
	}

	public static string GetInventoryString(IInventory inventory,bool includeIndex = false)
	{
		string contextString = "";
		foreach (var item in inventory)
		{
			if (item is null) continue;
			contextString = string.Concat(contextString, $"\n{GetItemString(inventory,item,includeIndex)}");
		}

		return contextString;
	}
}