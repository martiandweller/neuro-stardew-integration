using Microsoft.Xna.Framework;
using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Websocket;
using StardewBotFramework.Source.Modules.Pathfinding.Base;
using StardewValley;
using StardewValley.Buildings;

namespace NeuroStardewValley.Source.Actions;

public static class ShippingBinActions
{
	public class GoToNearestShippingBin : NeuroAction<ShippingBin>
	{
		public override string Name => "go_to_nearest_shipping";
		protected override string Description => "Walk and open nearest shipping bin";
		protected override JsonSchema? Schema => new JsonSchema();

		protected override ExecutionResult Validate(ActionData actionData, out ShippingBin resultData)
		{
			if (Main.Bot.ShippingBinInteraction.GetShippingBinLocations(Game1.currentLocation).Count == 0)
			{
				resultData = new ShippingBin();
				return ExecutionResult.Failure($"There are no shipping bins in this location");
			}

			List<ShippingBin> bins = Main.Bot.ShippingBinInteraction.GetShippingBinsInLocation(Game1.currentLocation);
			Dictionary<int, Queue<ShippingBin>> dictionary = ClosestShippingBin(Game1.player.TilePoint, bins);
			int lowestIndex = 0;
			foreach (var kvp in dictionary)
			{
				if (kvp.Key < lowestIndex || lowestIndex == 0)
				{
					lowestIndex = kvp.Key;
				}
			}

			resultData = dictionary[lowestIndex].Dequeue();
			return ExecutionResult.Success("Walking to shipping bin now.");
		}

		protected override void Execute(ShippingBin? resultData)
		{
			if (resultData is null) return;

			Main.Bot.Pathfinding.Goto(new Goal.GetToTile(resultData.tileX.Value, resultData.tileY.Value), false);
			// Main.Bot.ShippingBinInteraction.SetUI();
		}

		private static Dictionary<int, Queue<ShippingBin>> ClosestShippingBin(Point place, List<ShippingBin> bins)
		{
			Dictionary<int, Queue<ShippingBin>> points = new();
			foreach (var bin in bins)
			{
				int point = Math.Abs(place.X - bin.tileX.Value + place.Y - bin.tileY.Value);
				if (points.ContainsKey(point))
				{
					points[point].Enqueue(bin);
				}
				else
				{
					Queue<ShippingBin> queue = new();
					queue.Enqueue(bin);
					points.Add(point, queue);
				}
			}

			return points;
		}
	}

	public class SellItems : NeuroAction<List<int>>
	{
		public override string Name => "sell_items";
		protected override string Description => "Add items in your inventory to be sold.";

		protected override JsonSchema Schema => new()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "item_index" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["item_index"] = QJS.Type(JsonSchemaType.Array)
			}
		};

		protected override ExecutionResult Validate(ActionData actionData, out List<int>? resultData)
		{
			Array array = actionData.Data.Value<Array>("item_index");

			resultData = new();
			if (array is null)
			{
				return ExecutionResult.Failure($"item index is null");
			}
			
			foreach (var item in array)
			{
				if (item is not int)
				{
					resultData = new List<int>();
					return ExecutionResult.Failure("You have provided a value that is not an integer");
				}
				
				resultData.Add((int)item);
			}
			
			List<Item> items = new();
			foreach (var index in resultData)
			{
				items.Add(Main.Bot.Inventory.Inventory[index]);	
			}
			List<string> itemNames = new();
			items.ToList().ForEach(item => itemNames.Add(item.Name));
			return ExecutionResult.Success($"You have sold: {string.Concat(itemNames,"\n")}");
		}

		protected override void Execute(List<int>? resultData)
		{
			List<Item> items = new();
			if (resultData is null) return;
			foreach (var index in resultData)
			{
				items.Add(Main.Bot.Inventory.Inventory[index]);	
			}
			Main.Bot.ShippingBinInteraction.ShipMultipleItems(items.ToArray());
		}
	}
}