using Microsoft.Xna.Framework;
using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Websocket;
using NeuroStardewValley.Source.RegisterActions;
using NeuroStardewValley.Source.Utilities;
using StardewBotFramework.Source.Modules.Pathfinding.Base;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Objects;
using Object = StardewValley.Object;

namespace NeuroStardewValley.Source.Actions;

public static class WorldObjectActions
{
	private static Chest? _chest;

	public class OpenChest : NeuroAction<Chest>
	{
		public override string Name => "open_chest";
		protected override string Description => "Open a chest that is in the current location";

		protected override JsonSchema? Schema => new JsonSchema()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "chest_tile_x", "chest_tile_y" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["chest_tile_x"] = QJS.Type(JsonSchemaType.Integer),
				["chest_tile_y"] = QJS.Type(JsonSchemaType.Integer)
			}
		};
		protected override ExecutionResult Validate(ActionData actionData, out Chest? resultData)
		{
			int? x = actionData.Data?.Value<int>("x_position");
			int? y = actionData.Data?.Value<int>("y_position");

			if (x is null  || y is null)
			{
				resultData = null;
				return ExecutionResult.Failure($"either x or y were not set correctly.");
			}

			Point point = new Point((int)x, (int)y);
			if (!TileUtilities.IsValidTile(point, out var reason, false))
			{
				resultData = null;
				return ExecutionResult.Failure(reason);
			}
			
			var objects = Utilities.StringUtilities.GetObjectsInLocation(new Chest());
			if (!objects.TryGetValue(point, out var obj))
			{
				resultData = null;
				return ExecutionResult.Failure($"There is not a chest at the provided location");
			}

			resultData = (Chest)obj;
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
						Task.Run(async () => await ExecuteFunctions(kvp.Key.ToPoint()));
					}
				}
			}
			Open(resultData);
			RegisterChestActions();
		}

		private static async Task ExecuteFunctions(Point position)
		{
			await Main.Bot.Pathfinding.Goto(new Goal.GetToTile(position.X,position.Y),false,false);
		}
		
		private static IInventory Open(Chest chest)
		{
			_chest = chest;
			Dictionary<Point, Object> objects = Utilities.StringUtilities.GetObjectsInLocation(_chest);

			if (!objects.ContainsValue(_chest)) return new Inventory();

			Main.Bot.Chest.OpenChest(_chest);

			return Main.Bot.Chest.GetItems(_chest);
		}
	}
	
	private class CloseChest : NeuroAction
	{
		public override string Name => "close_chest";
		protected override string Description => "Close the currently opened chest.";
		protected override JsonSchema? Schema => new ();
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
		protected override JsonSchema? Schema => new ()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "item_index" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["item_index"] = new JsonSchema
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
		protected override JsonSchema? Schema => new ()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "item_index" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["item_index"] = new JsonSchema
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
		ActionWindow window = ActionWindow.Create(Main.GameInstance);

		window.AddAction(new CloseChest()).AddAction(new AddItemsToChest()).AddAction(new TakeItemsFromChest());
		window.SetForce(0,$"You are now in a chest's menu", string.Concat(_chest!.Items.Where(item => !string.IsNullOrEmpty(item.Name)))); // send chest items in state
		window.Register();
	}
}