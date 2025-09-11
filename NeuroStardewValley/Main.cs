using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewBotFramework.Source;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using NeuroSDKCsharp.Actions;
using NeuroStardewValley.Source.Actions.Menus;
using NeuroStardewValley.Source.ContextStrings;
using NeuroStardewValley.Source.EventMethods;
using StardewBotFramework.Source.Modules.Pathfinding.Base;
using StardewValley;
using StardewValley.Menus;
using Logger = NeuroStardewValley.Debug.Logger;

namespace NeuroStardewValley;

/// <summary>The mod entry point.</summary>
internal sealed class Main : Mod
{
    /// <summary>
    /// This should be used for all ActionWindows
    /// </summary>
    public static Game GameInstance => GameRunner.instance;
    
    public static StardewClient Bot = null!;

    public static ModConfig Config = null!;
    private static string? _uriString;
    
    public static bool CanCreateCharacter;

    private static int _configSaveSlot;

    public static Dictionary<string, bool> EnabledCharacterOptions = new();
    
    public static Dictionary<string, string> DefaultCharacterOptions = new();

    private static bool _hasSentCharacter;
    
    public override void Entry(IModHelper helper)
    {
        Bot = new StardewClient(helper, ModManifest, Monitor, helper.Multiplayer);

        Config = Helper.ReadConfig<ModConfig>();
        _uriString = Config.WebsocketUri;
        CanCreateCharacter = Config.AllowCharacterCreation;
        _configSaveSlot = Config.SaveSlot;
        
        EnabledCharacterOptions = Config.CharacterCreationOptions;
        DefaultCharacterOptions = Config.CharacterCreationDefault;

        Logger.SetMonitor(Monitor);

        helper.Events.GameLoop.GameLaunched += GameLaunched;
        helper.Events.GameLoop.UpdateTicking += UpdateTicking;
        helper.Events.Display.MenuChanged += OneTimeEvents.CharacterCreatorMenu;
        Bot.GameEvents.DayStarted += MainGameLoopEvents.OnDayStarted;
        Bot.GameEvents.DayEnded += MainGameLoopEvents.OnDayEnded;
        Bot.GameEvents.BotWarped += MainGameLoopEvents.OnWarped;
        Bot.GameEvents.MenuChanged += MainGameLoopEvents.OnMenuChanged;
        Bot.GameEvents.BotLocationNpcChanged += MainGameLoopEvents.LocationNpcChanged;
        Bot.GameEvents.OnBotDamaged += MainGameLoopEvents.OnBotDamaged;
        Bot.GameEvents.EventFinished += MainGameLoopEvents.EventFinished;

        helper.Events.GameLoop.SaveLoaded += OneTimeEvents.OnSaveLoaded;

        Bot.GameEvents.ChatMessageReceived += LessImportantEvents.OnChatMessage;
        Bot.GameEvents.BotSkillChanged += LessImportantEvents.OnBotSkillChanged;
        Bot.GameEvents.DayStarted += LessImportantEvents.OnDayStartedSkills;
        Bot.GameEvents.UiTimeChanged += LessImportantEvents.OnUiTimeChanged;
        Bot.GameEvents.HUDMessageAdded += OneTimeEvents.OnHUDMessageAdded;
        Bot.GameEvents.OnBotDeath += LessImportantEvents.OnBotDeath;
        Bot.GameEvents.BotInventoryChanged += LessImportantEvents.InventoryChanged;
        Bot.GameEvents.CaughtFish += LessImportantEvents.CaughtFish;

        Bot.GameEvents.BotObjectChanged += WorldEvents.WorldObjectChanged;
        Bot.GameEvents.BotTerrainFeatureChanged += WorldEvents.TerrainFeatureChanged;
        Bot.GameEvents.BotLargeTerrainFeatureChanged += WorldEvents.LargeTerrainFeatureChanged;
        Bot.GameEvents.BotLocationFurnitureChanged += WorldEvents.LocationFurnitureChanged;

        CharacterController.FailedPathFinding += OneTimeEvents.FailedCharacterController;
        
        if (Config.Debug)
        {
            helper.Events.Display.Rendered += StardewBotFramework.Debug.DrawFoundTiles.OnRenderPathNode;
            helper.Events.Input.ButtonPressed += InputOnButtonPressed;
        }
    }

    private void InputOnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (e.Button == SButton.B)
        {
            Game1.activeClickableMenu = new CarpenterMenu("Robin");
        }

        if (e.Button == SButton.U)
        {
            Logger.Info($"tile: {Game1.currentCursorTile}");
        }

        if (e.Button == SButton.I)
        {
            Logger.Info($"pixel tile: {(Game1.currentCursorTile.X * Game1.tileSize)}  {(Game1.currentCursorTile.Y * Game1.tileSize)}");
        }

        if (e.Button == SButton.G)
        {
            Game1.player.setSkillLevel("Farming", 10);
        }

        if (e.Button == SButton.Y)
        {
            foreach (var building in Game1.getFarm().buildings)
            {
                Logger.Info($"building: {building.humanDoor.Value}");
            }
        }

        if (e.Button == SButton.G)
        {
            Game1.player.Position = Game1.currentCursorTile * 64;
        }

        if (e.Button == SButton.X)
        {
            Bot.FishingBar.Fish(100);
        }

        if (e.Button == SButton.H)
        {
            MouseState mouseState = Game1.input.GetMouseState();
            Logger.Info($"mouse state: {mouseState}");
            Logger.Info($"{new Vector2((int)((Utility.ModifyCoordinateFromUIScale(mouseState.X) + (float)Game1.viewport.X) / 64f), (int)((Utility.ModifyCoordinateFromUIScale(mouseState.Y) + (float)Game1.viewport.Y) / 64f))}");
        }

        if (e.Button == SButton.U)
        {
            NeuroSDKCsharp.Messages.Outgoing.Context.Send(PlayerContext.GetAllCharactersLevel());
        }

        if (e.Button == SButton.R)
        {
            foreach (var building in Game1.currentLocation.buildings)
            {
                building.FinishConstruction();
            }
        }
    }

    private void GameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(_uriString)) throw new Exception($"UriString was not set correctly");
            NeuroSDKCsharp.SdkSetup.Initialize(GameInstance,"Stardew Valley",_uriString);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }
    }
    
    private void UpdateTicking(object? sender, UpdateTickingEventArgs e)
    {
        if (Game1.activeClickableMenu is TitleMenu)
        {
            Bot.MainMenuNavigation.SetTitleMenu((TitleMenu)Game1.activeClickableMenu);

            if (!CanCreateCharacter)
            {
                Bot.MainMenuNavigation.GotoLoad();

                if (TitleMenu.subMenu is LoadGameMenu)
                {
                    Bot.LoadMenu.SetLoadMenu((LoadGameMenu)TitleMenu.subMenu);
                    if (!Bot.LoadMenu.Loading) Bot.LoadMenu.LoadSlot(_configSaveSlot);
                }
            }
        
            if (!_hasSentCharacter && CanCreateCharacter)
            {
                Bot.MainMenuNavigation.GotoCreateNewCharacter();
            
                if (TitleMenu.subMenu is CharacterCustomization)
                {
                    _hasSentCharacter = true;
                    Bot.CharacterCreation.SetCreator((CharacterCustomization)TitleMenu.subMenu);
                    ActionWindow window = ActionWindow.Create(GameInstance)
                        .SetForce(2, "You should create your character, you will not be able to change this later.", "You are now in the character creator menu.")
                        .AddAction(new MainMenuActions.CreateCharacter());
                    window.Register();
                }
            }
        }
    }

}