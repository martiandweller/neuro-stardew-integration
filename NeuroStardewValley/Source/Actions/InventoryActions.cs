using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Websocket;
using NeuroStardewValley.Debug;
using NeuroStardewValley.Source.Actions.Menus;
using NeuroStardewValley.Source.ContextStrings;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Objects;
using StardewValley.Objects.Trinkets;
using Object = StardewValley.Object;

namespace NeuroStardewValley.Source.Actions;

 static class InventoryActions
{
    #region BaseUI

    public class OpenInventory : NeuroAction
    {
        public override string Name => "open_inventory";
        protected override string Description => "Open your inventory and allow altering the placement of items, this will also stop time.";
        protected override JsonSchema Schema => new();
        protected override ExecutionResult Validate(ActionData actionData)
        {
            return ExecutionResult.Success();
        }

        protected override void Execute()
        {
            Main.Bot.PlayerInformation.OpenInventory();
            RegisterInventoryActions();
        }
    }
    public class ExitInventory : NeuroAction
    {
        public override string Name => "close_inventory";
        protected override string Description => "Close your inventory and go back to playing the game, this will make time tick again.";
        protected override JsonSchema Schema => new();
        protected override ExecutionResult Validate(ActionData actionData)
        {
            return ExecutionResult.Success();
        }

        protected override void Execute()
        {
            Main.Bot.PlayerInformation.ExitMenu();
        }
    }

    #endregion

    #region ItemInteraction

    private class MoveItem : NeuroAction<Item>
    {
        private int _position;
        public override string Name => "move_item";
        protected override string Description => $"Move an item in inventory, you have {Main.Bot.Inventory.MaxInventory} places in your inventory";

        protected override JsonSchema Schema => new ()
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
            RegisterInventoryActions();
        }

        private IEnumerable<string> GetItemNames(Inventory inventory)
        {
            List<string> items = new();
            for (int i = 0; i < inventory.Count; i++)
            {
                if (inventory[i] is null)
                {
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
    private class InteractWithTrinkets : NeuroAction<Dictionary<string,string>>
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
        protected override JsonSchema Schema => new ()
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
            if (resultData is null) return;
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
            RegisterInventoryActions();
        }
    }
    private class ChangeClothing : NeuroAction<Dictionary<string,string>>
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

        protected override JsonSchema Schema => new()
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
            string? inventory = actionData.Data?.Value<string>("inventory_slot");

            if (action == "equip")
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
            resultData = new()
            {
                { "slot", slot },
                { "action", action }
            };
            if (inventory is not null)
            {
                resultData.Add("inventory_slot",inventory);
            }
            return ExecutionResult.Success();
        }

        protected override void Execute(Dictionary<string,string>? resultData)
        {
            if (resultData is null) return;
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
            RegisterInventoryActions();
        }
    }

    #endregion
    
    #region Attach

    public class AttachItem : NeuroAction<KeyValuePair<Item, Item>>
    {
        public override string Name => "attach_item";

        protected override string Description =>
            "Attach an item to another, this is most commonly used with bait and fishing rods.";
        protected override JsonSchema Schema => new()
        {
            Type = JsonSchemaType.Object,
            Required = new List<string> { "item", "attached_item" },
            Properties = new Dictionary<string, JsonSchema>
            {
                ["item"] = QJS.Enum(Enumerable.Range(0,Main.Bot.Inventory.MaxInventory)), // item to attach
                ["attached_item"] = QJS.Enum(Enumerable.Range(0,Main.Bot.Inventory.MaxInventory)) // item we attach to
            }
        };
        protected override ExecutionResult Validate(ActionData actionData, out KeyValuePair<Item, Item> resultData)
        {
            int? item = actionData.Data?.Value<int>("item");
            int? attachToItem = actionData.Data?.Value<int>("attached_item");

            resultData = new();
            if (item is null || attachToItem is null)
            {
                return ExecutionResult.Failure($"You have not selected both items.");
            }
            
            if (!Enumerable.Range(0, Main.Bot.Inventory.MaxInventory).Contains((int)item) ||
                !Enumerable.Range(0, Main.Bot.Inventory.MaxInventory).Contains((int)attachToItem))
            {
                return ExecutionResult.Failure($"The index you provided was not a valid index");
            }

            Item toolItem = Main.Bot.Inventory.Inventory[(int)item];
            Item attachItem = Main.Bot.Inventory.Inventory[(int)attachToItem];
            if (toolItem is not Tool tool)
            {
                return ExecutionResult.Failure($"The index you provided does not point to an item that has an attachment slot.");
            }
            if (!tool.canThisBeAttached((Object)attachItem))
            {
                return ExecutionResult.Failure($"{attachItem.Name} cannot be attached to {tool.Name}");
            }

            if (tool.attachments.Count(obj => obj is not null) >= tool.AttachmentSlotsCount)
            {
                return ExecutionResult.Failure($"This item already has too many attachments on it.");
            }

            resultData = new KeyValuePair<Item, Item>(tool, attachItem);
            return ExecutionResult.Success();
        }

        protected override void Execute(KeyValuePair<Item, Item> resultData)
        {
            Main.Bot.Inventory.AttachItem(resultData.Value,resultData.Key);
            RegisterInventoryActions();
        }
    }
    
    public class RemoveItem : NeuroAction<Item>
    {
        public override string Name => "remove_attached_item";
        protected override string Description => "Remove the item attached to another item." +
                                                 " The attachment will either, be placed the first empty slot in your inventory or it will be dropped on the floor. This depends on how much free space you have.";
        protected override JsonSchema Schema => new()
        {
            Type = JsonSchemaType.Object,
            Required = new List<string> { "item" },
            Properties = new Dictionary<string, JsonSchema>
            {
                ["item"] = QJS.Enum(Enumerable.Range(0, Main.Bot.Inventory.MaxInventory))
            }
        };
        protected override ExecutionResult Validate(ActionData actionData, out Item? resultData)
        {
            int? index = actionData.Data?.Value<int>("item");

            resultData = null;
            if (index is null)
            {
                return ExecutionResult.Failure($"The index you provided was null.");
            }

            if (!Enumerable.Range(0, Main.Bot.Inventory.MaxInventory).Contains((int)index))
            {
                return ExecutionResult.Failure($"The index you provided was not a valid index");
            }

            Item i = Main.Bot.Inventory.Inventory[(int)index];
            if (i is not Tool tool)
            {
                return ExecutionResult.Failure($"The index you provided does not point to an item that has an attachment slot.");
            }

            if (tool.attachments.Count(obj => obj is not null) == 0)
            {
                return ExecutionResult.Failure($"This item does not have an attachment on it.");
            }

            resultData = tool;
            return ExecutionResult.Success($"Removing the {tool.Name}'s attachment");
        }

        protected override void Execute(Item? resultData)
        {
            if (resultData is not Tool tool)
            {
                return;
            }
            
            Main.Bot.Inventory.RemoveAttached(tool);
            RegisterInventoryActions();
        }
    }
    
    #endregion

    #region ToolBar

    public class ChangeSelectedToolbarSlot : NeuroAction<int>
    	{
    		public override string Name => "change_toolbar_slot";
    		protected override string Description => $"Change currently selected toolbar slot, the slots available are between 0,{Main.Bot._farmer.MaxItems}.";
    		protected override JsonSchema Schema => new()
    		{
    			Type = JsonSchemaType.Object,
    			Required = new List<string> { "slot" },
    			Properties = new Dictionary<string, JsonSchema>
    			{
    				["slot"] = QJS.Enum(Enumerable.Range(0, Main.Bot._farmer.MaxItems))
    			}
    		};
            
    		protected override ExecutionResult Validate(ActionData actionData, out int resultData)
    		{
    			string? slotStr = actionData.Data?.Value<string>("slot");
    
    			if (string.IsNullOrEmpty(slotStr))
    			{
    				resultData = -1;
    				return ExecutionResult.Failure($"slot can not be null");
    			}
                
    			int slot = int.Parse(slotStr);
    
    			if (!Enumerable.Range(0, Main.Bot._farmer.MaxItems).Contains(slot))
    			{
    				resultData = -1;
    				return ExecutionResult.Failure($"{slot} is not a valid slot index");
    			}
    
    			resultData = slot;
    			return ExecutionResult.Success($"Changing to slot: {slot}");
    		}
    
    		protected override void Execute(int resultData)
    		{
    			int? toolbarRotates = resultData / 12;
    			for (int i = 0; i < toolbarRotates; i++)
    			{
    				Main.Bot.Inventory.SelectInventoryRowForToolbar(true);
    				resultData -= 12;
    			}
    			
    			Main.Bot.Inventory.SelectSlot(resultData);
    		}
    	}

    #endregion
    
    private static void RegisterInventoryActions()
    {
        ActionWindow actionWindow = ActionWindow.Create(Main.GameInstance);
        actionWindow.AddAction(new MoveItem()).AddAction(new InteractWithTrinkets()).AddAction(new ChangeClothing())
            .AddAction(new ExitInventory()).AddAction(new AttachItem()).AddAction(new RemoveItem()).AddAction(new CraftingActions.SetCraftingPage());

        string nameList = InventoryContext.GetInventoryString(Main.Bot.Inventory.Inventory, true, true);
        List<string> itemList = PrepareItemStringList(Main.Bot.Inventory.GetEquippedClothing()).ToList();
        List<string> trinkets = Main.Bot.Inventory.GetCurrentEquippedTrinkets(Game1.player)
            .Where(trinket => trinket is not null).Select(trinket => trinket.Name).ToList();
        
        string state = $"These are the items in your inventory: {nameList}." +
                       $"\nThese are the items clothes you have equipped {string.Concat(itemList)}.";
        if (trinkets.Count > 0)
        {
            state += $"\nThis is the trinket you have equipped currently: {string.Concat(trinkets)}";
        }
        actionWindow.SetForce(0, "You are in your inventory.", state);
        actionWindow.Register();
    }

    private static IEnumerable<string> PrepareItemStringList(Dictionary<string,Item> getEquippedClothing)
    {
        Inventory inventory = new Inventory();
        inventory.AddRange(getEquippedClothing.Values);
        IEnumerable<Item> items = PrepareItemStringList(inventory);
        List<string> itemString = new(); 
        using var enumerator = items.GetEnumerator();
        while (enumerator.MoveNext())
        {
            foreach (var kvp in getEquippedClothing)
            {
                if (kvp.Value == enumerator.Current)
                {
                    itemString.Add($"\n{kvp.Key}: {enumerator.Current.Name}");
                }
            }
        }

        return itemString;
    }

    private static IEnumerable<Item> PrepareItemStringList(Inventory items)
    {
        IEnumerable<Item> list = items.Where(item => item is not null).Where(item => !string.IsNullOrEmpty(item.Name));
        return list;
    }
}