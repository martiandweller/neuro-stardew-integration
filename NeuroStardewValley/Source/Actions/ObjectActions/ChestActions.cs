using Microsoft.Xna.Framework;
using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Websocket;
using NeuroStardewValley.Debug;
using NeuroStardewValley.Source.RegisterActions;
using NeuroStardewValley.Source.Utilities;
using StardewBotFramework.Source.Modules.Pathfinding.Base;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Objects;
using Object = StardewValley.Object;

namespace NeuroStardewValley.Source.Actions.ObjectActions;

public static class ChestActions
{
	private static Chest? _chest;

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
				["chest_position"] = QJS.Enum(GetChestsLocations(out var _))
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

		protected override void Execute(Chest? resultData)
		{
			if (resultData is null) return;
			
			foreach (var dict in Game1.currentLocation.Objects)
			{
				foreach (var kvp in dict)
				{
					if (kvp.Value == resultData)
					{
						Task.Run(async () => await ExecuteFunctions(kvp.Key.ToPoint(),resultData));
					}
				}
			}
			
		}

		private static async Task ExecuteFunctions(Point position,Chest chest)
		{
			await Main.Bot.Pathfinding.Goto(new Goal.GetToTile(position.X,position.Y),false);
			Open(chest);
			RegisterChestActions();
		}
		
		private static IInventory Open(Chest chest)
		{
			_chest = chest;
			Dictionary<Point, Object> objects = StringUtilities.GetObjectsInLocation(_chest);

			if (!objects.ContainsValue(_chest)) return new Inventory();

			Main.Bot.Chest.OpenChest(_chest);

			return Main.Bot.Chest.GetItems(_chest);
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
			if (_chest is null) return ExecutionResult.ModFailure($"A chest is not currently opened, that means this action should not have been registered. Sorry.");
			return ExecutionResult.Success();
		}

		protected override void Execute()
		{
			Close();
			RegisterMainGameActions.RegisterPostAction();
		}
		
		private static void Close()
		{
			if (_chest is null) return;
			Main.Bot.Chest.CloseChest();
		}
	}
	
	private class AddItemsToChest : NeuroAction<List<Item>>
	{
		public override string Name => "insert_items";
		protected override string Description => "Insert items in this chest.";
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
			if (_chest is null)
			{
				resultData = new();
				return ExecutionResult.ModFailure($"A chest is not currently opened, that means this action should not have been registered. Sorry.");
			}
			int[]? array = actionData.Data?.Value<int[]>("item_index");

			resultData = new List<Item>();
			if (array is null)
			{
				return ExecutionResult.Failure($"item index is null");
			}
			
			var nullItems = _chest.Items.Where(slot => slot is null);
			if (array.Length > nullItems.ToList().Count)
			{
				return ExecutionResult.Failure($"You have tried to add too many items to this chest.");
			}

			resultData.AddRange(array.Select(item => Main.Bot.Inventory.Inventory[item]));

			List<string> itemNames = new();
			resultData.ToList().ForEach(item => itemNames.Add(item.Name));
			return ExecutionResult.Success($"You have added: {string.Concat(itemNames,"\n")} to the chest");
		}

		protected override void Execute(List<Item>? resultData)
		{
			if (_chest is null || resultData is null) return;
			foreach (var item in resultData)
			{
				Main.Bot.Chest.PutItemInChest(_chest,item,Game1.player);
			}
			RegisterChestActions();
		}
	}
	
	private class TakeItemsFromChest : NeuroAction<List<Item>>
	{
		public override string Name => "take_items";
		protected override string Description => "Take items from this chest.";
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
			if (_chest is null)
			{
				resultData = new();
				return ExecutionResult.ModFailure($"A chest is not currently opened, that means this action should not have been registered. Sorry.");
			}
			int[]? array = actionData.Data?.Value<int[]>("item_index");

			resultData = new();
			if (array is null)
			{
				return ExecutionResult.Failure($"item index is null");
			}

			foreach (var index in array)
			{
				if (_chest.Items[index] is null)
				{
					return ExecutionResult.Failure($"{index} is not a valid item in this chest.");
				}
			}

			resultData.AddRange(array.Select(item => _chest.Items[item]));

			List<string> itemNames = new();
			resultData.ToList().ForEach(item => itemNames.Add(item.Name));
			return ExecutionResult.Success($"You have added: {string.Concat(itemNames,"\n")} to the chest");
		}

		protected override void Execute(List<Item>? resultData)
		{
			if (_chest is null || resultData is null) return;
			foreach (var item in resultData)
			{
				Main.Bot.Chest.TakeItemFromChest(_chest,item,Game1.player);
			}
			RegisterChestActions();
		}
	}
	
	private static void RegisterChestActions()
	{
		Logger.Info($"registering chest actions");
		ActionWindow window = ActionWindow.Create(Main.GameInstance);

		window.AddAction(new CloseChest()).AddAction(new AddItemsToChest()).AddAction(new TakeItemsFromChest());
		string nameList = InventoryContext.GetInventoryString(_chest!.Items, true);
		window.SetForce(0,$"You are now interacting with a chest", 
			$"These are the items in this chest: {nameList}.\n This is your inventory: " +
			$"{InventoryContext.GetInventoryString(Main.Bot.Inventory.Inventory,true)}",true);
		window.Register();
	}
}