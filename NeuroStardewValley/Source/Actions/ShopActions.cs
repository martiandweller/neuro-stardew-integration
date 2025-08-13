using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Websocket;
using StardewValley;
using StardewValley.Objects;

namespace NeuroStardewValley.Source.Actions;

public class ShopActions
{
	public class BuyItem : NeuroAction<Item>
	{
		public override string Name => "buy_item";
		protected override string Description => "Buy an item from the shop";

		protected override JsonSchema Schema => new JsonSchema()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "item_index" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["item_index"] = QJS.Enum(0,), // get shop menu items
				["amount"] = QJS.Type(JsonSchemaType.Integer)
			}
		};
		protected override ExecutionResult Validate(ActionData actionData, out Item? resultData)
		{
			resultData
			return ExecutionResult.Success();
		}

		protected override void Execute(Item? resultData)
		{
			throw new NotImplementedException();
		}
	}
}