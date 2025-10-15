using Microsoft.Xna.Framework;
using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Websocket;
using NeuroStardewValley.Debug;
using NeuroStardewValley.Source.ContextStrings;
using NeuroStardewValley.Source.RegisterActions;
using NeuroStardewValley.Source.Utilities;
using Newtonsoft.Json.Linq;
using StardewBotFramework.Source.Modules.Pathfinding.Base;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using Object = StardewValley.Object;

namespace NeuroStardewValley.Source.Actions.ObjectActions;

public static class ChestActions
{
	public static Chest? Chest { get; set; }

	public class OpenChest : NeuroAction<Chest>
	{
		public override string Name => "open_chest";
		protected override string Description => "Open a chest that is in the current location";

		protected override JsonSchema Schema => new()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "chest_position" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["chest_position"] = QJS.Enum(GetChestsLocations(out _))
			}
		};
		protected override ExecutionResult Validate(ActionData actionData, out Chest? resultData)
		{
			string? providedPos = actionData.Data?.Value<string>("chest_position");

			if (providedPos is null)
			{
				resultData = null;
				return ExecutionResult.Failure($"chest_position was null");
			}

			int index = GetChestsLocations(out var list).IndexOf(providedPos);
			Chest chest = list[index];
			// Point point = new Point((int)providedPos, (int)y);
			if (!TileUtilities.IsValidTile(chest.TileLocation.ToPoint(), out var reason, false,false))
			{
				resultData = null;
				return ExecutionResult.Failure(reason);
			}
			
			var objects = StringUtilities.GetObjectsInLocation(new Chest());
			if (!objects.TryGetValue(chest.TileLocation.ToPoint(), out var _))
			{
				resultData = null;
				return ExecutionResult.Failure($"There is not a chest at the provided location");
			}

			resultData = chest;
			return ExecutionResult.Success();
		}

		protected override async void Execute(Chest? resultData)
		{
			try
			{
				if (resultData is null) return;
			
				foreach (var dict in Game1.currentLocation.Objects)
				{
					foreach (var kvp in dict.Where(kvp => kvp.Value == resultData))
					{
						await Main.Bot.Pathfinding.Goto(new Goal.GetToTile(kvp.Key.ToPoint().X,kvp.Key.ToPoint().Y));
						await TaskDispatcher.SwitchToMainThread();
						Open(resultData);
					}
				}
			}
			catch (Exception e)
			{
				Logger.Error($"{e}");
				await TaskDispatcher.SwitchToMainThread();
				if (Game1.activeClickableMenu is ItemGrabMenu)
				{
					RegisterChestActions();
					return;
				}
				RegisterMainActions.RegisterPostAction();
			}
		}
		
		private static void Open(Chest chest)
		{
			Chest = chest;
			Dictionary<Point, Object> objects = StringUtilities.GetObjectsInLocation(Chest);

			if (!objects.ContainsValue(Chest))
			{
				return;
			}

			Main.Bot.Chest.OpenChest(Chest);
		}

		private static List<string> GetChestsLocations(out List<Chest> chests)
		{
			List<string> chestPoints = new();
			chests = new();
			foreach (var dict in Game1.currentLocation.Objects)
			{
				foreach (var kvp in dict)
				{
					if (kvp.Value is Chest chest)
					{
						chests.Add(chest);
						chestPoints.Add(kvp.Key.ToPoint().ToString());
					}
				}
			}

			return chestPoints;
		}
	}
	
	private class CloseChest : NeuroAction
	{
		public override string Name => "close_chest";
		protected override string Description => "Close the currently opened chest.";
		protected override JsonSchema Schema => new ();
		protected override ExecutionResult Validate(ActionData actionData)
		{
			if (Chest is null) return ExecutionResult.ModFailure($"A chest is not currently opened, that means this action should not have been registered. Sorry.");
			return ExecutionResult.Success();
		}

		protected override void Execute()
		{
			Close();
			RegisterMainActions.RegisterPostAction();
		}
		
		private static void Close()
		{
			if (Chest is null) return;
			Main.Bot.Chest.CloseChest();
			Main.Bot.ItemGrabMenu.RemoveUi(); // do this as colour changing is in here
		}
	}
	
	private class AddItemsToChest : NeuroAction<List<Item>>
	{
		public override string Name => "insert_items";
		protected override string Description => $"Insert items in this chest, you should make sure the amount of items" +
		                                         $" you send can be fit in this chest. The max capacity of this chest" +
		                                         $" assuming no items is {Chest?.GetActualCapacity()}";
		protected override JsonSchema Schema => new ()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "item_index" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["item_index"] = new()
				{
					Type = JsonSchemaType.Array,
					Items = new JsonSchema { Type = JsonSchemaType.Integer },
				}
			}
		};
		protected override ExecutionResult Validate(ActionData actionData,out List<Item> resultData)
		{
			if (Chest is null)
			{
				resultData = new();
				return ExecutionResult.ModFailure($"A chest is not currently opened, that means this action should not have been registered. Sorry.");
			}
			var objArray = actionData.Data?.Value<object>("item_index");

			resultData = new();
			if (objArray is null)
			{
				return ExecutionResult.Failure($"item index is null");
			}

			List<int> array = new();
			foreach (var token in (JArray)objArray)
			{
				if (token.Value<int?>() is null) continue;
				
				if (token.Value<int>() < 0 || token.Value<int>() > Main.Bot.Inventory.Inventory.Count - 1 || Main.Bot.Inventory.Inventory[token.Value<int>()] is null)
					return ExecutionResult.Failure($"{token.Value<int>()} cannot be accessed.");
				
				array.Add(token.Value<int>());
			}
			
			if (array.Count > Chest.GetActualCapacity() - Chest.Items.Count(item => item is not null))
			{
				return ExecutionResult.Failure($"You have tried to add too many items to this chest.");
			}

			resultData.AddRange(array.Select(item => Main.Bot.Inventory.Inventory[item]));

			List<string> itemNames = new();
			resultData.ToList().ForEach(item => itemNames.Add(item.DisplayName));
			return ExecutionResult.Success($"You have added: {string.Concat(itemNames,"\n")} to the chest");
		}

		protected override void Execute(List<Item>? resultData)
		{
			if (Chest is null || resultData is null) return;
			foreach (var item in resultData)
			{
				Main.Bot.Chest.PutItemInChest(Chest,item,Game1.player);
			}
			RegisterChestActions();
		}
	}
	
	private class TakeItemsFromChest : NeuroAction<List<Item>>
	{
		public override string Name => "take_items";
		protected override string Description => $"Take items from this chest, indexes are calculated from 0 - amount of items, the amount being in this case {Chest?.Items.Count - 1}.";
		protected override JsonSchema Schema => new()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "item_index" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["item_index"] = new()
				{
					Type = JsonSchemaType.Array,
					Items = new JsonSchema { Type = JsonSchemaType.Integer }
				}
			}
		};
		protected override ExecutionResult Validate(ActionData actionData,out List<Item> resultData)
		{
			if (Chest is null)
			{
				resultData = new();
				return ExecutionResult.ModFailure($"A chest is not currently opened, that means this action should not have been registered. Sorry.");
			}
			var objArray = actionData.Data?.Value<JArray>("item_index");

			resultData = new();
			if (objArray is null)
			{
				return ExecutionResult.Failure($"item index is null");
			}

			List<int> array = new();
			foreach (var token in objArray)
			{
				if (token.Value<int?>() is null) continue;

				
				array.Add(token.Value<int>());
			}

			foreach (var index in array)
			{
				if (index < 0) return ExecutionResult.Failure($"You cannot provide an index less than one.");
				if (index > Chest.Items.Count - 1) return ExecutionResult.Failure($"You cannot provide an index larger than the amount of items in this chest.");
				
				if (Chest.Items[index] is null)
				{
					return ExecutionResult.Failure($"{index} is not a valid item in this chest.");
				}
			}

			resultData.AddRange(array.Select(item => Chest.Items[item]));

			List<string> itemNames = new();
			resultData.ToList().ForEach(item => itemNames.Add(item.DisplayName));
			return ExecutionResult.Success($"You have added: {string.Join("\n",itemNames)} to the chest");
		}

		protected override void Execute(List<Item>? resultData)
		{
			if (Chest is null || resultData is null) return;
			foreach (var item in resultData)
			{
				Main.Bot.Chest.TakeItemFromChest(Chest,item,Game1.player);
			}
			RegisterChestActions();
		}
	}
	
	public static void RegisterChestActions(bool includeColourPicker = false)
	{
		Logger.Info($"registering chest actions");
		ActionWindow window = ActionWindow.Create(Main.GameInstance);

		window.AddAction(new CloseChest()).AddAction(new AddItemsToChest()).AddAction(new TakeItemsFromChest());
		if (includeColourPicker) window.AddAction(new ItemGrabActions.SelectColour());
		
		string nameList = InventoryContext.GetInventoryString(Chest!.Items, true);
		window.SetForce(0,$"You are now interacting with a chest", 
			$"These are the items in this chest: {nameList}.\n This is your inventory: " +
			$"{InventoryContext.GetInventoryString(Main.Bot.Inventory.Inventory,true)}",true);
		window.Register();
	}
}