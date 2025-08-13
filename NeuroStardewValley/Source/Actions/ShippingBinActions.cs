using Microsoft.Xna.Framework;
using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Websocket;
using NeuroStardewValley.Source.RegisterActions;
using StardewBotFramework.Source.Modules.Pathfinding.Base;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;

namespace NeuroStardewValley.Source.Actions;

public static class ShippingBinActions
{
	private static void RegisterBinActions()
	{
		ActionWindow window = ActionWindow.Create(Main.GameInstance);
		window.AddAction(new ExitBin())
			.AddAction(new SellItems())
			.SetForce(0,"You have opened the nearest shipping bin, you can use this to sell items. This should be your main source of making money","")
			.Register();
	}
	
	public class GoToNearestShippingBin : NeuroAction<ShippingBin>
	{
		private static ShippingBin Bin;
		public override string Name => "go_to_nearest_shipping";
		protected override string Description => "Walk and open nearest shipping bin";
		protected override JsonSchema? Schema => new();

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
			
			Task.Run(async () => await ExecuteFunctions(resultData));
		}

		private static async Task ExecuteFunctions(ShippingBin resultData)
		{
			await Main.Bot.Pathfinding.Goto(new Goal.GoalNearby(resultData.tileX.Value, resultData.tileY.Value, 0),
				false);
			Bin = resultData;
			HandleShippingBinUI();
			RegisterBinActions();
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

		private static void HandleShippingBinUI()
		{
			ItemGrabMenu itemGrabMenu = new ItemGrabMenu(null, true, false, Utility.highlightShippableObjects, ShipItemReplica, "", null, true, true, false, true, false, 0, null, -1, Bin);
			itemGrabMenu.initializeUpperRightCloseButton();
			itemGrabMenu.setBackgroundTransparency(false);
			itemGrabMenu.setDestroyItemOnClick(true);
			itemGrabMenu.initializeShippingBin();
			Game1.activeClickableMenu = itemGrabMenu;
			Game1.playSound("shwip");
			if (Game1.player.FacingDirection == 1)
			{
				Game1.player.Halt();
			}
			Game1.player.showCarrying();
			Farm farm = (Farm)Game1.currentLocation;
			if (farm.getShippingBin(Game1.player).Count == 0)
			{
				// Bin.showShipment(null);
				return;
			}
			Bin.showShipment(farm.getShippingBin(Game1.player)[farm.getShippingBin(Game1.player).Count - 1]);
		}
		private static void ShipItemReplica(Item i,Farmer farmer)
		{
			Farm? farm = Game1.currentLocation as Farm;
			farmer = Game1.player;
			if (i != null)
			{
				Game1.player.removeItemFromInventory(i);
				Farm obj = farm;
				obj?.getShippingBin(Game1.player).Add(i);
				
				Bin.showShipment(i, false);
				farm.lastItemShipped = i;
				if (Game1.player.ActiveItem == null)
				{
					Game1.player.showNotCarrying();
					Game1.player.Halt();
				}
			}
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
				["item_index"] = new JsonSchema
				{
					Type = JsonSchemaType.Array,
					Items = new JsonSchema { Type = JsonSchemaType.Integer },
				}
			}
		};

		protected override ExecutionResult Validate(ActionData actionData, out List<int>? resultData)
		{
			Array? array = actionData.Data?.Value<Array>("item_index");

			resultData = new();
			if (array is null)
			{
				return ExecutionResult.Failure($"item index is null");
			}
			
			foreach (var item in array)
			{
				if (item is not int i)
				{
					resultData = new List<int>();
					return ExecutionResult.Failure("You have provided a value that is not an integer");
				}
				
				resultData.Add(i);
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
			RegisterBinActions();
		}
	}

	public class ExitBin : NeuroAction
	{
		public override string Name => "exit_bin";
		protected override string Description => "Close the bin and go back to the game";
		protected override JsonSchema? Schema => new();
		protected override ExecutionResult Validate(ActionData actionData)
		{
			return ExecutionResult.Success();
		}
		protected override void Execute()
		{
			RegisterMainGameActions.RegisterPostAction();
		}
	}
}