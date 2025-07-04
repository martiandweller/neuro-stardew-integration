using Microsoft.Xna.Framework;
using StardewBotFramework.Source;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using Context = NeuroSDKCsharp.Messages.Outgoing.Context;
using NeuroSDKCsharp.Actions;
using NeuroStardewValley.Source;
using StardewValley;
using StardewValley.Menus;
using Logger = NeuroStardewValley.Debug.Logger;
using Object = StardewValley.Object;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace NeuroStardewValley;

/// <summary>The mod entry point.</summary>
internal sealed class ModEntry : Mod
{
    public static StardewClient Bot;

    private ModConfig Config;

    public static bool CanCreateCharacter;

    public static int ConfigSaveSlot;

    public static Dictionary<string, bool> EnabledCharacterOptions;
    
    public static Dictionary<string, string> DefaultCharacterOptions;

    private static bool _hasSentCharacter = false;

    public override void Entry(IModHelper helper)
    {
        Bot = new StardewClient(helper, Monitor, helper.Multiplayer);

        Config = this.Helper.ReadConfig<ModConfig>();
        CanCreateCharacter = this.Config.AllowCharacterCreation;
        ConfigSaveSlot = this.Config.SaveSlot;
        
        EnabledCharacterOptions = this.Config.CharacterCreationOptions;
        DefaultCharacterOptions = this.Config.CharacterCreationDefault;
        
        Logger.SetMonitor(Monitor);
        
        helper.Events.GameLoop.GameLaunched += GameLaunched;
        helper.Events.GameLoop.UpdateTicking += UpdateTicking;
        helper.Events.GameLoop.SaveLoaded += GameLoopOnSaveLoaded;
        helper.Events.Display.MenuChanged += MenuChanged;
        helper.Events.Player.Warped += OnWarped;
    }

    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (!e.IsLocalPlayer)
        {
            Context.Send($"Player: {e.Player.Name} has moved to: {e.NewLocation.Name} from: {e.OldLocation.Name}");
            return;
        }
        Context.Send($"You have moved to {e.NewLocation.Name} from {e.OldLocation.Name}\n These are the tiles around you: {GetTilesInLocation(e.NewLocation)}");
        foreach (var tileString in GetTilesInLocation(e.NewLocation))
        {
            Context.Send(tileString,true);
        }

        string warps = GetWarpTiles(e.NewLocation);
        string[] warpExtracts = warps.Split(' ');
        Point warpTile = new Point(int.Parse(warpExtracts[0]), int.Parse(warpExtracts[1]));
        Logger.Info($"warps: {warpExtracts.Length}");
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
                    if (!Bot.LoadMenu.Loading) Bot.LoadMenu.LoadSlot(ConfigSaveSlot);
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
        Context.Send($"This should send a save is loaded starts :)");
        NeuroActionHandler.RegisterActions(new MainGameActions.Pathfinding(), new MainGameActions.UseItem(), new MainGameActions.OpenInventory());
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