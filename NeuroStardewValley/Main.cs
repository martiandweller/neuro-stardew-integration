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
internal sealed class Main : Mod
{
    /// <summary>
    /// This should be used for all ActionWindows
    /// </summary>
    public static Game GameInstance => GameRunner.instance;
    
    public static StardewClient Bot;

    private ModConfig? _config;
    private static string? _uriString;
    
    public static bool CanCreateCharacter;

    private static int _configSaveSlot;

    public static Dictionary<string, bool> EnabledCharacterOptions = new();
    
    public static Dictionary<string, string> DefaultCharacterOptions = new();

    private static bool _hasSentCharacter;

    public override void Entry(IModHelper helper)
    {
        Bot = new StardewClient(helper, ModManifest,Monitor, helper.Multiplayer);
        
        _config = Helper.ReadConfig<ModConfig>();
        _uriString = _config.WebsocketUri;
        CanCreateCharacter = _config.AllowCharacterCreation;
        _configSaveSlot = _config.SaveSlot;
        
        EnabledCharacterOptions = this._config.CharacterCreationOptions;
        DefaultCharacterOptions = this._config.CharacterCreationDefault;
        
        Logger.SetMonitor(Monitor);
        
        helper.Events.GameLoop.GameLaunched += GameLaunched;
        helper.Events.GameLoop.UpdateTicking += UpdateTicking;
        helper.Events.Display.MenuChanged += EventMethods.SingleEvents.CharacterCreatorMenu;
        Bot.GameEvents.DayStarted += EventMethods.MainGameLoop.OnDayStarted;
        Bot.GameEvents.DayEnded += EventMethods.MainGameLoop.OnDayEnded;
        Bot.GameEvents.BotWarped += EventMethods.MainGameLoop.OnWarped;
        Bot.GameEvents.MenuChanged += EventMethods.MainGameLoop.OnMenuChanged;

        helper.Events.GameLoop.SaveLoaded += EventMethods.SingleEvents.GameLoopOnSaveLoaded;

        Bot.GameEvents.ChatMessageReceived += EventMethods.LessImportantLoop.OnChatMessage;
        Bot.GameEvents.BotSkillChanged += EventMethods.LessImportantLoop.OnBotSkillChanged;
        Bot.GameEvents.DayStarted += EventMethods.LessImportantLoop.OnDayStartedSkills;
        Bot.GameEvents.UiTimeChanged += EventMethods.LessImportantLoop.OnUiTimeChanged;
        Bot.GameEvents.HUDMessageAdded += EventMethods.SingleEvents.OnHUDMessageAdded;
    }

    private void GameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(_uriString)) throw new Exception($"UriString was not set correctly");
            NeuroSDKCsharp.SdkSetup.Initialize("Stardew Valley",_uriString);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }
    }
    
    private void UpdateTicking(object? sender, UpdateTickingEventArgs e)
    {
        WebsocketHandler.Instance!.Update(); // this is used to send websocket messages, This may be removed later if WebsocketHandler is changed to GameComponent as planned

        if (Game1.activeClickableMenu is TitleMenu)
        {
            Bot.MainMenuNavigation.SetTitleMenu((TitleMenu)Game1.activeClickableMenu);

            if (!_config.AllowCharacterCreation)
            {
                Bot.MainMenuNavigation.GotoLoad();

                if (TitleMenu.subMenu is LoadGameMenu)
                {
                    Bot.LoadMenu.SetLoadMenu((LoadGameMenu)TitleMenu.subMenu);
                    if (!Bot.LoadMenu.Loading) Bot.LoadMenu.LoadSlot(_configSaveSlot);
                }
            }
        
            if (!_hasSentCharacter && _config.AllowCharacterCreation)
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
}