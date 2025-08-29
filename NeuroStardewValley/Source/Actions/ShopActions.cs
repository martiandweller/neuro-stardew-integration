using Microsoft.Xna.Framework;
using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Websocket;
using NeuroStardewValley.Source.RegisterActions;
using NeuroStardewValley.Source.Utilities;
using StardewBotFramework.Debug;
using StardewValley;
using Logger = NeuroStardewValley.Debug.Logger;

namespace NeuroStardewValley.Source.Actions;

public class ShopActions
{
	public class OpenShop : NeuroAction<KeyValuePair<int,int>>
	{
		public override string Name => "open_shop";
		protected override string Description => "This will open the shop that is at the provided x,y coordinate.";
		protected override JsonSchema? Schema => new()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "tile_x","tile_y" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["tile_x"] = QJS.Type(JsonSchemaType.Integer),
				["tile_y"] = QJS.Type(JsonSchemaType.Integer)
			}
		};
		protected override ExecutionResult Validate(ActionData actionData, out KeyValuePair<int, int> resultData)
		{
			int? nullX = actionData.Data?.Value<int>("tile_x");
			int? mullY = actionData.Data?.Value<int>("tile_y");

			if (nullX is null || mullY is null)
			{
				resultData = new KeyValuePair<int, int>(-1, -1);				
				return ExecutionResult.Failure($"A value you provided was null");
			}

			int x = (int)nullX;
			int y = (int)mullY;
			resultData = new KeyValuePair<int, int>(x, y);
			if (!TileUtilities.IsValidTile(new Point(x, y), out var reason, false, false))
			{
				return ExecutionResult.Failure(reason);
			}

			if (!Main.Bot.Shop.OpenShopUi(x, y))
			{
				return ExecutionResult.Failure($"There is not a shop at the value you provided");
			}
			
			return ExecutionResult.Success($"opening shop");
		}

		protected override void Execute(KeyValuePair<int, int> resultData)
		{
			return; // we set it in validation this is not good practice, but we kinda need to do it.
		}
	}
	
	public class BuyItem : NeuroAction<KeyValuePair<ISalable,int>>
	{
		public override string Name => "buy_item";
		protected override string Description => "Buy an item from the shop";
		protected override JsonSchema Schema => new JsonSchema()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "item_index" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["item_index"] = QJS.Enum(Enumerable.Range(0,Main.Bot.Shop.ListAllItems()!.Count)), // get shop menu items
				["amount"] = QJS.Type(JsonSchemaType.Integer)
			}
		};
		protected override ExecutionResult Validate(ActionData actionData, out KeyValuePair<ISalable,int> resultData)
		{
			int? itemIndex = actionData.Data?.Value<int>("item_index");
			int? itemAmount = actionData.Data?.Value<int>("amount");

			resultData = new();
			if (itemIndex is null || itemAmount is null)
			{
				return ExecutionResult.Failure($"A value you provided was null.");
			}

			int index = (int)itemIndex;
			int amount = (int)itemAmount;

			if (!Enumerable.Range(0, Main.Bot.Shop.ListAllItems()!.Count).Contains(index))
			{
				return ExecutionResult.Failure($"{index} is not a valid index.");
			}

			ISalable sellItem = Main.Bot.Shop.ListAllItems()![index];
			Main.Bot.Shop.ForSaleStats(out List<ISalable> list, out var currency);
			switch (currency)
			{
				case 0:
					if (sellItem.salePrice() * amount > Game1.player.Money)
					{
						return ExecutionResult.Failure($"You cannot buy this item or the amount of this item, as you do not have enough money for this.");
					}
					break;
				case 1: // this is star gems idk how to get this.
					if (true)
					{
						return ExecutionResult.Failure(
							$"You cannot buy this item or the amount of this item, as you do not have enough money for this.");
					}
				case 2:
					if (sellItem.salePrice() * amount > Game1.player.QiGems)
					{
						return ExecutionResult.Failure($"You cannot buy this item or the amount of this item, as you do not have enough money for this.");
					}
					break;
				case 3:
					if (sellItem.salePrice() * amount > Game1.player.QiGems)
					{
						return ExecutionResult.Failure($"You cannot buy this item or the amount of this item, as you do not have enough money for this.");
					}
					break;
			}

			if (!sellItem.CanBuyItem(Game1.player))
			{
				return ExecutionResult.Failure($"You cannot buy this item, this is most likely because you do not have either the items or money necessary.");
			}

			if (amount == 0) amount = 1;
			resultData = new KeyValuePair<ISalable, int>(sellItem, amount);
			return ExecutionResult.Success();
		}

		protected override void Execute(KeyValuePair<ISalable,int> resultData)
		{
			Logger.Info($"isalable: {resultData.Key.Name}  amount: {resultData.Value}");
			int index = Main.Bot.Shop.ListAllItems()!.IndexOf(resultData.Key);
			Logger.Info($"index: {index}");
			Main.Bot.Shop.BuyItem(index,resultData.Value);
			RegisterStoreActions.RegisterDefaultShop();
		}
	}

	public class CloseShop : NeuroAction
	{
		public override string Name => "close_shop";
		protected override string Description => "Close the currently open shop menu.";
		protected override JsonSchema Schema => new();
		protected override ExecutionResult Validate(ActionData actionData)
		{
			return ExecutionResult.Success($"Exiting shop");
		}

		protected override void Execute()
		{
			Main.Bot.Shop.CloseShop();
		}
	}
}