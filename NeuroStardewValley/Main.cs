using System.Xml;
using Microsoft.Xna.Framework;
using StardewBotFramework.Source;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using Context = NeuroSDKCsharp.Messages.Outgoing.Context;
using NeuroSDKCsharp.Actions;
using NeuroStardewValley.Source;
using StardewBotFramework.Source.Events;
using StardewBotFramework.Source.Events.EventArgs;
using StardewBotFramework.Source.Events.GamePlayEvents;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;
using Logger = NeuroStardewValley.Debug.Logger;
using Object = StardewValley.Object;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace NeuroStardewValley;

/// <summary>The mod entry point.</summary>
internal sealed class ModEntry : Mod
{
    /// <summary>
    /// This should be used for all ActionWindows
    /// </summary>
    public static Game GameInstance => GameRunner.instance;
    
    public static StardewClient Bot;

    private ModConfig Config;

    public static bool CanCreateCharacter;

    private static int _configSaveSlot;

    public static Dictionary<string, bool> EnabledCharacterOptions;
    
    public static Dictionary<string, string> DefaultCharacterOptions;

    private static bool _hasSentCharacter = false;

    public override void Entry(IModHelper helper)
    {
        Bot = new StardewClient(helper, ModManifest,Monitor, helper.Multiplayer);
        
        Config = this.Helper.ReadConfig<ModConfig>();
        CanCreateCharacter = this.Config.AllowCharacterCreation;
        _configSaveSlot = this.Config.SaveSlot;
        
        EnabledCharacterOptions = this.Config.CharacterCreationOptions;
        DefaultCharacterOptions = this.Config.CharacterCreationDefault;
        
        Logger.SetMonitor(Monitor);
        
        helper.Events.GameLoop.GameLaunched += GameLaunched;
        helper.Events.GameLoop.UpdateTicking += UpdateTicking;
        helper.Events.GameLoop.SaveLoaded += GameLoopOnSaveLoaded;
        helper.Events.Display.MenuChanged += MenuChanged;
        Bot.GameEvents.BotWarped += OnWarped;
        Bot.GameEvents.ChatMessageReceived += OnChatMessage;
    }

    private void OnWarped(object? sender, BotWarpedEventArgs e)
    {
        ActionWindow window = ActionWindow.Create(GameInstance);
        string tilesString = "";
        foreach (var tile in GetTilesInLocation(e.NewLocation))
        {
            tilesString += "\n" + tile;
        }
        Context.Send($"You have moved to {e.NewLocation.Name} from {e.OldLocation.Name}.\n These are the tiles that have an object on them around you: {tilesString}");
        
        string warps = GetWarpTiles(e.NewLocation);
        string[] warpExtracts = warps.Split(' ');
        Dictionary<Point, string> warpLocation = new();
        int runs = 0;
        for (int i = 0; i < warpExtracts.Length / 5; i++)
        {
            Logger.Info($"tile: {warpExtracts[runs]} next tile: {warpExtracts[runs + 1]}");
            Point tile = new Point(int.Parse(warpExtracts[runs]), int.Parse(warpExtracts[runs + 1]));
            
            string locationName = warpExtracts[runs + 2];
            // Point LocationTile = new Point(int.Parse(warpExtracts[runs + 3]), int.Parse(warpExtracts[runs + 4]));
            warpLocation.Add(tile,locationName);
            runs += 5;
        }

        string s = "";
        foreach (var kvp in warpLocation)
        {
            Logger.Info(kvp.Key.ToString());
            Logger.Info(kvp.Value);
            s +=  "\n" + kvp.Key + " " + kvp.Value;
        }
        Context.Send(s,true);

        if (e.NewLocation is Farm)
        {
            foreach (var item in Bot.PlayerInformation.Inventory)
            {
                if (item is WateringCan wateringCan)
                {
                    if (!wateringCan.isBottomless.Value) window.AddAction(new ToolActions.RefillWateringCan());
                }
                if (item is Pickaxe || item is Axe || item is MeleeWeapon) window.AddAction(new ToolActions.DestroyObject());
            }
        }
        
        window.Register();
    }

    private void MenuChanged(object? sender, MenuChangedEventArgs e)
    {
        Logger.Info($"Menu has been changed to: {e}");
        if (e.NewMenu is CharacterCustomization)
        {
            NeuroActionHandler.RegisterActions(new MainMenuActions.CreateCharacter());
        }
    }

    private void UpdateTicking(object? sender, UpdateTickingEventArgs e)
    {
        NeuroSDKCsharp.Websocket.WebsocketHandler.Instance!.Update(); // this is used to send websocket messages

        if (Game1.activeClickableMenu is TitleMenu)
        {
            Bot.MainMenuNavigation.SetTitleMenu((TitleMenu)Game1.activeClickableMenu);

            if (!Config.AllowCharacterCreation)
            {
                Bot.MainMenuNavigation.GotoLoad();

                if (TitleMenu.subMenu is LoadGameMenu)
                {
                    Bot.LoadMenu.SetLoadMenu((LoadGameMenu)TitleMenu.subMenu);
                    if (!Bot.LoadMenu.Loading) Bot.LoadMenu.LoadSlot(_configSaveSlot);
                }
            }
        
            if (!_hasSentCharacter && Config.AllowCharacterCreation)
            {
                Bot.MainMenuNavigation.GotoCreateNewCharacter();
            
                if (TitleMenu.subMenu is CharacterCustomization)
                {
                    _hasSentCharacter = true;
                    Bot.CharacterCreation.SetCreator((CharacterCustomization)TitleMenu.subMenu);
                    NeuroActionHandler.RegisterActions(new MainMenuActions.CreateCharacter());
                }
            }
        }
    }

    private void GameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        NeuroSDKCsharp.SdkSetup.Initialize("Stardew Valley");
    }

    private void GameLoopOnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        Context.Send($"Your save has loaded and you are now in the game.");
        NeuroActionHandler.RegisterActions(new MainGameActions.Pathfinding(), new MainGameActions.UseItem(), new MainGameActions.OpenInventory(), new MainGameActions.PathFindToExit());
    }

    private void OnChatMessage(object? sender, ChatMessageReceivedEventArgs e)
    {
        string query;
        switch (e.ChatKind) // magic number are from the game not me :(
        {
            case 0: // normal public message
                query = $"{e.PlayerName} has said {e.Message} in chat. You can use the action to talk back to them if you want";
                break;
            case 1:
                return;
            case 2: // notification
                query = $"THIS IS A NOTIFICATION FROM THE GAME: {e.PlayerName} has said {e.Message} in chat. You can use the action to talk back to them if you want";
                break;
            case 3: // private
                query = $"{e.PlayerName} has said {e.Message} to you in a private message. You can use the action to talk back to them if you want";
                break;
            default:
                return;
        }
        
        ActionWindow.Create(GameInstance)
            .SetForce(0,query,"", false)
            .AddAction(new ChatActions.SendChatMessage())
            .Register();
    }
    
    private static List<string> GetTilesInLocation(GameLocation location)
    {
        List<string> tileList = new();

        int maxX = location.Map.DisplayWidth / Game1.tileSize;
        int maxY = location.Map.DisplayHeight / Game1.tileSize; 
        
        Logger.Info($"look at this: {maxX}  maxY: {maxY}");
        
        for (int x = 0; x < maxX; x++)
        {
            for (int y = 0; y < maxY; y++)
            {
                Rectangle rect = new Rectangle(x * Game1.tileSize + 1, y * Game1.tileSize + 1, 62, 62);
                if (!Game1.currentLocation.isCollidingPosition(rect, Game1.viewport, true, 0, false, Game1.player))
                    continue;

                if (!location.Objects.ContainsKey(new Vector2(x, y)))
                {
                    tileList.Add($"Tile: {new Point(x, y)}, " +
                                 $"object collision: {Game1.currentLocation.isCollidingPosition(new Rectangle(x * Game1.tileSize + 1, y * Game1.tileSize + 1, 62, 62), Game1.viewport, isFarmer: true, -1, glider: false, Game1.player)}" +
                                 $" This is a border of the map.");
                }
                else
                {
                    Object? objectValue = location.Objects[new Vector2(x, y)];
                    tileList.Add($"Tile: {objectValue.TileLocation.ToPoint()}, object name: {objectValue.Name}," +
                                 $" object collision: {Game1.currentLocation.isCollidingPosition(new Rectangle(objectValue.TileLocation.ToPoint().X * Game1.tileSize + 1, objectValue.TileLocation.ToPoint().Y * Game1.tileSize + 1, 62, 62), Game1.viewport, isFarmer: true, -1, glider: false, Game1.player)}" +
                                 $" object Type: {objectValue.Type}");
                }
            }
        }
        
        return tileList;
    }
    
    private static string GetWarpTiles(GameLocation location)
    {
        location.TryGetMapProperty("Warp", out var warps);
        return warps;
    }
}