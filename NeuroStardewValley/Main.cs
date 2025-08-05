using Microsoft.Xna.Framework;
using StardewBotFramework.Source;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using Context = NeuroSDKCsharp.Messages.Outgoing.Context;
using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Websocket;
using NeuroStardewValley.Source;
using StardewBotFramework.Source.Events.EventArgs;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;
using Logger = NeuroStardewValley.Debug.Logger;
using Object = StardewValley.Object;

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
    private static string? _uriString;
    
    public static bool CanCreateCharacter;

    private static int _configSaveSlot;

    public static Dictionary<string, bool> EnabledCharacterOptions;
    
    public static Dictionary<string, string> DefaultCharacterOptions;

    private static bool _hasSentCharacter = false;

    public override void Entry(IModHelper helper)
    {
        Bot = new StardewClient(helper, ModManifest,Monitor, helper.Multiplayer);
        
        Config = this.Helper.ReadConfig<ModConfig>();
        _uriString = Config.WebsocketUri;
        CanCreateCharacter = this.Config.AllowCharacterCreation;
        _configSaveSlot = this.Config.SaveSlot;
        
        EnabledCharacterOptions = this.Config.CharacterCreationOptions;
        DefaultCharacterOptions = this.Config.CharacterCreationDefault;
        
        Logger.SetMonitor(Monitor);
        
        helper.Events.GameLoop.GameLaunched += GameLaunched;
        helper.Events.GameLoop.UpdateTicking += UpdateTicking;
        helper.Events.GameLoop.SaveLoaded += GameLoopOnSaveLoaded;
        helper.Events.Display.MenuChanged += MenuChanged;
        Bot.GameEvents.DayStarted += OnDayStartedSkills;
        Bot.GameEvents.DayStarted += OnDayStarted;
        Bot.GameEvents.DayEnded += OnDayEnded;
        Bot.GameEvents.BotSkillChanged += OnBotSkillChanged;
        Bot.GameEvents.UiTimeChanged += OnUiTimeChanged;
        Bot.GameEvents.BotWarped += OnWarped;
        Bot.GameEvents.ChatMessageReceived += OnChatMessage;
        Bot.GameEvents.MenuChanged += OnMenuChanged;
    }

    private void OnMenuChanged(object? sender, BotMenuChangedEventArgs e)
    {
        Logger.Info($"currnet menu: {e.NewMenu}");
        if (e.OldMenu is DialogueBox)
        {
            Logger.Info($"Removing dialogue box");
            //TODO: remove dialogue actions or set up gameplay actions
        }
        if (e.NewMenu is DialogueBox) // if advance does not set up dialogue box twice
        {
            Logger.Info($"add new menu dialogue box");
            
            //TODO: make dialogue actions
        }
    }

    private static Dictionary<SkillType, int> _skillsChangedThisDay = new();
    private void OnBotSkillChanged(object? sender, BotSkillLevelChangedEventArgs e)
    {
        _skillsChangedThisDay.Add(e.ChangedSkill,e.NewLevel);
    }
    
    private void OnDayStartedSkills(object? sender, BotDayStartedEventArgs e)
    {
        _skillsChangedThisDay.Clear();
    }
    
    private void OnDayEnded(object? sender, BotDayEndedEventArgs e)
    {
        // TODO: register UI actions for end of day (or just automate this and only send context)
        if (_skillsChangedThisDay.Count > 0)
        {
            string skillContext = "Skills that have been changed this day: ";
            foreach (var kvp in _skillsChangedThisDay)
            {
                skillContext += $"\n {kvp.Key.ToString()}: new level: {kvp.Value}";
            }
            Context.Send(skillContext);
        }
    }

    private void OnUiTimeChanged(object? sender, TimeEventArgs e)
    {
        Context.Send($"The current time is {e.NewTime}, This is sent in the 24 hour notion, if it is greater than 2400 than you should wrap around e.g. 2600 would be 2:00AM");
    }

    private void OnDayStarted(object? sender, BotDayStartedEventArgs e)
    {
        if (Game1.player.passedOut)
        {
            // add items lost or whatever was lost
            Context.Send($"A new day has started. You are in your farm-house after you were knocked out, the current day is {SDate.Now().DayOfWeek} {SDate.Now().Day} of {SDate.Now().Season} in year {SDate.Now().Year}.");
        }
        else
        {
            Context.Send($"A new day has started. You are in your farm-house, the current day is {SDate.Now().DayOfWeek} {SDate.Now().Day} of {SDate.Now().Season} in year {SDate.Now().Year}.");
        }
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
        string warpsString = GetWarpTilesString(warps);
        
        Context.Send(warpsString,true);
        window.SetForce(0, "", "");
        RegisterMainGameActions.RegisterActions(window);
        RegisterMainGameActions.RegisterToolActions(window,e);
        
        window.Register();
    }

    private void MenuChanged(object? sender, MenuChangedEventArgs e)
    {
        Logger.Info($"Menu has been changed to: {e}");
        if (e.NewMenu is CharacterCustomization)
        {
            ActionWindow window = ActionWindow.Create(GameInstance)
                .SetForce(2, "", "")
                .AddAction(new MainMenuActions.CreateCharacter());
            window.Register();
        }
    }

    private void UpdateTicking(object? sender, UpdateTickingEventArgs e)
    {
        WebsocketHandler.Instance!.Update(); // this is used to send websocket messages

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
                    ActionWindow window = ActionWindow.Create(GameInstance)
                        .SetForce(2, "", "")
                        .AddAction(new MainMenuActions.CreateCharacter());
                    window.Register();
                }
            }
        }
    }

    private void GameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        try
        {
            if (_uriString is null || _uriString == "") throw new Exception($"UriString was not set");
            NeuroSDKCsharp.SdkSetup.Initialize("Stardew Valley",_uriString);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }
    }

    private void GameLoopOnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        Context.Send($"Your save has loaded and you are now in the game. You are in your farm-house, the current day is {SDate.Now().DayOfWeek} {SDate.Now().Day} of {SDate.Now().Season} in year {SDate.Now().Year}.");
        RegisterMainGameActions.LoadGameActions();
    }

    private void OnChatMessage(object? sender, ChatMessageReceivedEventArgs e)
    {
        string query;
        switch (e.ChatKind) // magic number are from the game not me :(
        {
            case 0: // normal public message
                query = $"In Stardew Valley, {e.PlayerName} has said {e.Message} in chat. You can use the action to talk back to them if you want";
                break;
            case 1:
                return;
            case 2: // notification
                query = $"In Stardew Valley, {e.PlayerName} has said {e.Message} in chat. You can use the action to talk back to them if you want";
                break;
            case 3: // private
                query = $"In Stardew Valley, {e.PlayerName} has said {e.Message} to you in a private message. You can use the action to talk back to them if you want";
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
        List<Building> sentBuildings = new();

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

                if (GetTileType(location, new Point(x, y)) is not null)
                {
                    object obj = GetTileType(location, new Point(x, y))!;
                    switch (obj)
                    {
                        case Object objectValue:
                            tileList.Add($"Tile: {objectValue.TileLocation.ToPoint()}, object name: {objectValue.Name}," +
                                         $" object collision: {Game1.currentLocation.isCollidingPosition(new Rectangle(objectValue.TileLocation.ToPoint().X * Game1.tileSize + 1, objectValue.TileLocation.ToPoint().Y * Game1.tileSize + 1, 62, 62), Game1.viewport, isFarmer: true, -1, glider: false, Game1.player)}" +
                                         $" object Type: {objectValue.Type}");
                            break;
                        case Building building:
                            if (sentBuildings.Contains(building)) continue;
                            sentBuildings.Add(building);
                            int buildX = building.tileX.Value;
                            int buildY = building.tileY.Value;
                            int buildWidth = building.tilesWide.Value;
                            int buildHeight = building.tilesHigh.Value;
                            tileList.Add($"The top left tile of the {building.buildingType.Value} is: {buildX},{buildY}. the bottom right is {buildX + buildWidth}, {buildY + buildHeight}");
                            break;
                        case ResourceClump resourceClump:
                            Context.Send($"{resourceClump.modData.Name} is at tile: {resourceClump.Tile.ToPoint()}");
                            break;
                        case TerrainFeature terrainFeature:
                            Context.Send($"{terrainFeature.modData.Name} is at tile: {terrainFeature.Tile.ToPoint()}");
                            break;
                    }
                }
                else
                {
                    tileList.Add($"Tile: {new Point(x, y)}, This is a border of the map.");
                }
            }
        }
        
        return tileList;
    }

    private static object? GetTileType(GameLocation location,Point tile)
    {
        if (location.Objects.ContainsKey(tile.ToVector2()))
        {
            return location.Objects[tile.ToVector2()];
        }

        foreach (var building in location.buildings)
        {
            if (tile.X < building.tileX.Value || tile.X > building.tileX.Value + building.tilesWide.Value)
            {
                if (tile.Y < building.tileY.Value || tile.Y < building.tileY.Value + building.tilesHigh.Value)
                {
                    return building;
                }
            }
        }
        
        foreach (var resourceClump in location.resourceClumps)
        {
            if (resourceClump.getBoundingBox().Contains(tile))
            {
                return resourceClump;
            }
        }

        foreach (var dict in location.terrainFeatures)
        {
            if (!dict.ContainsKey(tile.ToVector2())) continue;
            if (dict[tile.ToVector2()].getBoundingBox().Contains(tile))
            {
                return dict[tile.ToVector2()];
            }
        }

        return null;
    }
    
    private static string GetWarpTiles(GameLocation location)
    {
        location.TryGetMapProperty("Warp", out var warps);
        return warps;
    }

    private static string GetWarpTilesString(string warpTiles)
    {
        string[] warpExtracts = warpTiles.Split(' ');
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
            s +=  "\n" + kvp.Value + ": " + kvp.Key;
        }

        return s;
    }
}