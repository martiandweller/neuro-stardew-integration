using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Websocket;
using StardewValley;
using StardewValley.Objects;

namespace NeuroStardewValley.Source.Actions;

public class ShopActions
{
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

			if (itemIndex is null || itemAmount is null)
			{
				resultData = new ();
				return ExecutionResult.Failure($"A value you provided was null.");
			}

			int index = (int)itemIndex;
			int amount = (int)itemAmount;

			if (!Enumerable.Range(0, Main.Bot.Shop.ListAllItems()!.Count).Contains(index))
			{
				resultData = new ();
				return ExecutionResult.Failure($"{index} is not a valid index.");
			}

			ISalable sellItem = Main.Bot.Shop.ListAllItems()![index].GetSalableInstance();
			Main.Bot.Shop.ForSaleStats(out List<ISalable> list, out var currency);
			switch (currency)
			{
				case 0:
					if (sellItem.salePrice() * amount > Game1.player.Money)
					{
						resultData = new ();
						return ExecutionResult.Failure($"You cannot buy this item or the amount of this item, as you do not have enough money for this.");
					}
					break;
				case 1: // this is star gems idk how to get this.
					if (true)
					{
						resultData = new ();
						return ExecutionResult.Failure(
							$"You cannot buy this item or the amount of this item, as you do not have enough money for this.");
					}
					break;
				case 2:
					if (sellItem.salePrice() * amount > Game1.player.QiGems)
					{
						resultData = new ();
						return ExecutionResult.Failure($"You cannot buy this item or the amount of this item, as you do not have enough money for this.");
					}
					break;
				case 3:
					if (sellItem.salePrice() * amount > Game1.player.QiGems)
					{
						resultData = new ();
						return ExecutionResult.Failure($"You cannot buy this item or the amount of this item, as you do not have enough money for this.");
					}
					break;
			}

			resultData = new KeyValuePair<ISalable, int>(sellItem, amount);
			return ExecutionResult.Success();
		}

		protected override void Execute(KeyValuePair<ISalable,int> resultData)
		{
			int index = Main.Bot.Shop.ListAllItems()!.IndexOf(resultData.Key);
			Main.Bot.Shop.BuyItem(index,resultData.Value);
		}
	}

	public class CloseShop : NeuroAction
	{
		public override string Name => "close_shop";
		protected override string Description => "Close the currently open shop menu.";
		protected override JsonSchema? Schema => new JsonSchema();
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