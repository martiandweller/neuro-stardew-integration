using StardewValley;
using StardewValley.Inventories;

namespace NeuroStardewValley.Source.ContextStrings;

public static class InventoryContext
{
	public static readonly string[] QualityStrings = { "Normal", "Silver", "Gold", "Iridium" };
	public static string GetItemString(IInventory inventory,Item item,bool includeIndex = false,bool includeAttachments = false)
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
			contextString = string.Concat(contextString, $" Quality: {QualityStrings[item.Quality]}");
		}

		if (includeIndex && item is Tool tool)
		{
			contextString += $" Allowed attachment amount: {tool.AttachmentSlotsCount} Equipped attachments: ";
			var attachments = tool.attachments.Where(obj => obj is not null && !string.IsNullOrEmpty(obj.Name))
				.Select(obj => $"{obj.Name} amount: {obj.Stack}").ToList();
			if (attachments.Count > 0)
			{
				contextString += string.Concat(attachments);
			}
			else
			{
				contextString += "None";
			}
		}

		return contextString;
	}

	public static string GetInventoryString(IInventory inventory,bool includeIndex = false,bool includeAttachments = false)
	{
		string contextString = "";
		foreach (var item in inventory)
		{
			if (item is null) continue;
			contextString = string.Concat(contextString, $"\n{GetItemString(inventory,item,includeIndex,includeAttachments)}");
		}

		return contextString;
	}

	public static string GetShippableString(IInventory inventory)
	{
		string contextString = "This is the start ";
		foreach (var item in inventory)
		{
			if (item is null || !item.canBeShipped()) continue;
			
			contextString += $"\nIndex: {inventory.IndexOf(item)}";
			contextString += $" Name: {item.Name}";
			contextString +=$" Amount: {item.Stack}";
			if (item.Quality > 0)
			{
				contextString = string.Concat(contextString, $" Quality: {QualityStrings[item.Quality]}");
			}
		}

		return contextString;
	}
}