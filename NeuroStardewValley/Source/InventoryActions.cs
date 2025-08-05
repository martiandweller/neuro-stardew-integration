using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Websocket;
using NeuroStardewValley.Debug;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;
using StardewValley;
using StardewValley.Enchantments;
using StardewValley.Inventories;
using StardewValley.Objects;
using StardewValley.Objects.Trinkets;

namespace NeuroStardewValley.Source;

public class InventoryActions
{
    public class MoveItem : NeuroAction<Item>
    {
        private int _position = 0;
        
        public override string Name => "move_item";
        protected override string Description => $"Move an item in inventory, you have {Main.Bot.Inventory.MaxInventory} places in your inventory";

        protected override JsonSchema? Schema => new ()
        {
            Type = JsonSchemaType.Object,
            Required = new List<string> { "item", "position" },
            Properties = new Dictionary<string, JsonSchema>
            {
                ["item"] = QJS.Enum(GetItemNames(Main.Bot.Inventory.GetInventory())),
                ["position"] = QJS.Enum(Enumerable.Range(0,Main.Bot.Inventory.MaxInventory)) // reduce one so we get 0-11
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
            
            if (itemPosition > Main.Bot.Inventory.MaxInventory)
            {
                resultData = null;
                return ExecutionResult.Failure($"You have given a position that is larger than the size of your inventory");
            }

            if (!GetItemNames(Main.Bot.Inventory.GetInventory()).Contains(itemName) || GetItemInInventory(Main.Bot.Inventory.GetInventory(), itemName) is null)
            {
                resultData = null;
                return ExecutionResult.Failure($"You have given an item that is not in your inventory");
            }

            resultData = GetItemInInventory(Main.Bot.Inventory.GetInventory(), itemName)!;
            _position = (int)itemPosition;
            
            return ExecutionResult.Success();
        }

        protected override void Execute(Item? resultData)
        {
            Main.Bot.Inventory.MoveItem(resultData!, _position);

            // stop issue with unexpected action result
            NeuroActionHandler.UnregisterActions(this);
            NeuroActionHandler.RegisterActions(this);
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
    public class InteractWithTrinkets : NeuroAction<Dictionary<string,string>>
    {
        private string[] TrinketAction()
        {
            return new[] { "Equip", "Unequip" };
        }

        private IEnumerable<string> TrinketSlots()
        {
            Logger.Info($"amount: {Farmer.MaximumTrinkets}    {Game1.player.stats.Get("trinketSlots")}     {Game1.player.trinketItems.Count}");
            string[] trinketSlots = new string[Game1.player.stats.Get("trinketSlots")];
            for (int i = 0; i < Game1.player.stats.Get("trinketSlots"); i++)
            {
                Logger.Info($"i: {i}");
                trinketSlots = (string[])trinketSlots.Append(i.ToString());
            }

            return trinketSlots;
        } 
        
        public override string Name => "interact_with_trinkets";

        protected override string Description =>
            "This will allow you to interact with trinkets, you can either remove or equip trinkets";
        protected override JsonSchema? Schema => new ()
        {
            Type = JsonSchemaType.Object,
            Required = new List<string> { "slot", "action" },
            Properties = new Dictionary<string, JsonSchema>
            {
                ["slot"] = QJS.Enum(TrinketSlots()), // explain what these are as I don't even know
                ["action"] = QJS.Enum(TrinketAction()),
                ["inventory_slot"] = QJS.Enum(Enumerable.Range(0,Main.Bot.Inventory.MaxInventory - 1))
            }
        };
        protected override ExecutionResult Validate(ActionData actionData, out Dictionary<string,string>? resultData)
        {
            string? slot = actionData.Data?.Value<string>("slot");
            string? action = actionData.Data?.Value<string>("action");
            int? inventoryInt = actionData.Data?.Value<int>("inventory_slot");

            resultData = new();
            if (slot is null || action is null || inventoryInt is null)
            {
                resultData = null;
                return ExecutionResult.Failure("Can not be null");
            }
            string inventory = inventoryInt.ToString()!;

            if (!TrinketSlots().Contains(slot))
            {
                resultData = null;
                return ExecutionResult.Failure($"{slot} is not a valid slot");
            }

            if (!TrinketAction().Contains(action))
            {
                resultData = null;
                return ExecutionResult.Failure($"{action} is not a valid action");
            }

            if (Enumerable.Range(0, Main.Bot.Inventory.MaxInventory - 1).Contains(int.Parse(inventory)))
            {
                resultData = null;
                return ExecutionResult.Failure($"{inventory} is not a valid inventory slot");
            }

            IEnumerable<string> trinketSlots = TrinketSlots();
            int index = trinketSlots.ToList().IndexOf(slot);
            
            resultData.Add("TrinketSlot", index.ToString());
            resultData.Add("Action", action);
            resultData.Add("Inventory", inventory);
            return ExecutionResult.Success();
        }

        protected override void Execute(Dictionary<string,string>? resultData)
        {
            if (resultData["Action"] == "Equip")
            {
                Trinket? trinket = Game1.player.trinketItems[int.Parse(resultData["Inventory"])];
                Main.Bot.Inventory.EquipTrinket(trinket,int.Parse(resultData["TrinketSlot"]));
            }
            else
            {
                Trinket? trinket = Game1.player.trinketItems[int.Parse(resultData["TrinketSlot"])];
                Main.Bot.Inventory.RemoveTrinket(trinket);
            }
        }
    }
    public class ChangeClothing : NeuroAction<Dictionary<string,string>>
    {
        private string[] Actions()
        {
            return new[] { "Equip", "Unequip" };
        }

        private string[] Slots() // I'm pretty sure they don't add any during the game
        {
            return new[] { "hat", "shirt", "pants", "top_ring", "bottom_ring", "boots" };
        }

        public override string Name => "change_equipped";
        protected override string Description => "This allows you to change equipped clothing and items";

        protected override JsonSchema? Schema => new JsonSchema()
        {
            Type = JsonSchemaType.Object,
            Required = new List<string> { "slot", "action" },
            Properties = new Dictionary<string, JsonSchema>
            {
                ["slot"] = QJS.Enum(Slots()),
                ["action"] = QJS.Enum(Actions()),
                ["inventory_slot"] = QJS.Enum(Enumerable.Range(0, Main.Bot.Inventory.MaxInventory))
            }
        };
        protected override ExecutionResult Validate(ActionData actionData, out Dictionary<string,string>? resultData)
        {
            string slot = actionData.Data?.Value<string>("slot")!;
            string action = actionData.Data?.Value<string>("action")!;
            string inventory = actionData.Data?.Value<string>("inventory_slot")!;

            if (slot is null || action is null || inventory is null && action == "equip")
            {
                resultData = new();
                return ExecutionResult.Failure("should not be null");
            }

            if (!Slots().Contains(slot))
            {
                resultData = new();
                return ExecutionResult.Failure("slot was not set correctly");
            }

            if (!Actions().Contains(action))
            {
                resultData = new();
                return ExecutionResult.Failure("action was not set correctly");
            }

            if (inventory is not null && Enumerable.Range(0, Main.Bot.Inventory.MaxInventory).Contains(int.Parse(inventory)) && action == "Equip")
            {
                resultData = new();
                return ExecutionResult.Failure("inventory was not set correctly");
            }
            
            Logger.Info($"slot keys: {slot}   action keys: {action}   inventory keys: {inventory}");
            resultData = new();
            resultData.Add("slot",slot);
            resultData.Add("action",action);
            resultData.Add("inventory_slot",inventory);
            return ExecutionResult.Success();
        }

        protected override void Execute(Dictionary<string,string>? resultData)
        {
            switch (resultData["slot"])
            {
                case "hat":
                    if (resultData["action"] == "Unequip") Main.Bot.Inventory.ChangeHat(null);
                    else Main.Bot.Inventory.ChangeHat((Hat)Game1.player.Items[int.Parse(resultData["inventory_slot"])]);
                    break;
                case "shirt":
                    if (resultData["action"] == "Unequip") Main.Bot.Inventory.ChangeClothing(true, null);
                    else Main.Bot.Inventory.ChangeClothing(true, (Clothing)Game1.player.Items[int.Parse(resultData["inventory_slot"])]);
                    break;
                case "pants":
                    if (resultData["action"] == "Unequip") Main.Bot.Inventory.ChangeClothing(false, null);
                    else Main.Bot.Inventory.ChangeClothing(false, (Clothing)Game1.player.Items[int.Parse(resultData["inventory_slot"])]);
                    break;
                case "top_ring":
                    if (resultData["action"] == "Unequip") Main.Bot.Inventory.ChangeRings(null,true);
                    else Main.Bot.Inventory.ChangeRings((Ring)Game1.player.Items[int.Parse(resultData["inventory_slot"])], true);
                    break;
                case "bottom_ring":
                    if (resultData["action"] == "Unequip") Main.Bot.Inventory.ChangeRings(null,false);
                    else Main.Bot.Inventory.ChangeRings((Ring)Game1.player.Items[int.Parse(resultData["inventory_slot"])], false);
                    break;
                case "boots":
                    if (resultData["action"] == "Unequip") Main.Bot.Inventory.ChangeBoots(null);
                    else Main.Bot.Inventory.ChangeBoots((Boots)Game1.player.Items[int.Parse(resultData["inventory_slot"])]);
                    break;
            }
            
        }
    }
    public class OpenInventory : NeuroAction
    {
        public override string Name => "open_inventory";
        protected override string Description => "Open your inventory and allow altering the placement of items, this will also stop time.";
        protected override JsonSchema? Schema => null;
        protected override ExecutionResult Validate(ActionData actionData)
        {
            return ExecutionResult.Success();
        }

        protected override void Execute()
        {
            Main.Bot.PlayerInformation.OpenInventory();

            ActionWindow actionWindow = ActionWindow.Create(Main.GameInstance);
            RegisterInventoryActions(actionWindow);
            actionWindow.SetForce(0, "", "You have opened your inventory.");
            actionWindow.Register();
        }
    }
    public class ExitInventory : NeuroAction
    {
        public override string Name => "close_inventory";
        protected override string Description => "Close your inventory and go back to playing the game, this will make time tick again.";
        protected override JsonSchema? Schema => null;
        protected override ExecutionResult Validate(ActionData actionData)
        {
            return ExecutionResult.Success();
        }

        protected override void Execute()
        {
            Main.Bot.PlayerInformation.ExitMenu();

            ActionWindow actionWindow = ActionWindow.Create(Main.GameInstance);
            RegisterMainGameActions.RegisterActions(actionWindow);
            RegisterMainGameActions.RegisterToolActions(actionWindow);
            actionWindow.SetForce(0, "", "You have opened your inventory.");
            actionWindow.Register();
        }
    }
    
    private static void RegisterInventoryActions(ActionWindow actionWindow)
    {
        actionWindow.AddAction(new MoveItem()).AddAction(new InteractWithTrinkets()).AddAction(new ChangeClothing()).AddAction(new ExitInventory());
    }
}