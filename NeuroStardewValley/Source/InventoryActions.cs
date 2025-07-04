using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Websocket;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Enchantments;
using StardewValley.Inventories;

namespace NeuroStardewValley.Source;

public class InventoryActions
{
    public class MoveItem : NeuroAction<Item>
    {
        private int _position = 0;
        
        public override string Name => "move_item";
        protected override string Description => $"Move an item in inventory, you have {ModEntry.Bot.Inventory.MaxInventory} places in your inventory";

        protected override JsonSchema? Schema => new ()
        {
            Type = JsonSchemaType.Object,
            Required = new List<string> { "item", "position" },
            Properties = new Dictionary<string, JsonSchema>
            {
                ["item"] = QJS.Enum(GetItemNames(ModEntry.Bot.Inventory.GetInventory())),
                ["position"] = QJS.Enum(Enumerable.Range(0,ModEntry.Bot.Inventory.MaxInventory - 1)) // reduce one so we get 0-11
            }
        };
        
        protected override ExecutionResult Validate(ActionData actionData, out Item? resultData)
        {
            int? itemPosition = actionData.Data?.Value<int>("position");
            string? itemName = actionData.Data?.Value<string>("item");

            if (itemPosition is null || itemName is null)
            {
                resultData = null;
                return ExecutionResult.Failure($"An argument you gave was null");
            }
            
            if (itemPosition > ModEntry.Bot.Inventory.MaxInventory)
            {
                resultData = null;
                return ExecutionResult.Failure($"You have given a position that is larger than the size of your inventory");
            }

            if (!GetItemNames(ModEntry.Bot.Inventory.GetInventory()).Contains(itemName) || GetItemInInventory(ModEntry.Bot.Inventory.GetInventory(), itemName) is null)
            {
                resultData = null;
                return ExecutionResult.Failure($"You have given an item that is not in your inventory");
            }

            resultData = GetItemInInventory(ModEntry.Bot.Inventory.GetInventory(), itemName)!;
            _position = (int)itemPosition;
            
            return ExecutionResult.Success();
        }

        protected override Task Execute(Item? resultData)
        {
            ModEntry.Bot.Inventory.MoveItem(resultData!, _position);

            // stop issue with unexpected action result
            NeuroActionHandler.UnregisterActions(this);
            NeuroActionHandler.RegisterActions(this);
            return Task.CompletedTask;
        }

        private IEnumerable<string> GetItemNames(Inventory inventory)
        {
            List<string> items = new();
            for (int i = 0; i < inventory.Count; i++)
            {
                if (inventory[i] is null)
                {
                    items.Add($"Position: {i} has no item in it");
                    continue;
                }
                items.Add($"Item Name: {inventory[i].Name}, Item Amount: {inventory[i].stack}, Item Position: {i}");
            }

            return items;
        }

        private Item? GetItemInInventory(Inventory inventory,string itemString)
        {
            Item? item = null;
            List<string> items = GetItemNames(inventory).ToList();
            for (int i = 0; i < inventory.Count; i++)
            {
                if (inventory[i] is null)
                {
                    items.Add($"Position: {i} has no item in it");
                    continue;
                }

                if (items[i] == itemString)
                {
                    item = inventory[i];
                    break;
                }
            }

            return item;
        }
    }
    
    
}