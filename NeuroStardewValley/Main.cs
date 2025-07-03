using StardewBotFramework.Debug;
using StardewBotFramework.Source;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using Context = NeuroSDKCsharp.Messages.Outgoing.Context;
using NeuroSDKCsharp.Actions;
using NeuroStardewValley.Source;
using StardewValley;
using Logger = NeuroStardewValley.Debug.Logger;

namespace NeuroStardewValley;

/// <summary>The mod entry point.</summary>
internal sealed class ModEntry : Mod
{
    public static StardewClient Bot;

    private ModConfig Config;

    public static bool CanCreateCharacter;

    public static Dictionary<string, bool> EnabledCharacterOptions;
    
    public static Dictionary<string, string> DefaultCharacterOptions;
    
    public override void Entry(IModHelper helper)
    {
        Bot = new StardewClient(helper, Monitor, helper.Multiplayer);

        Config = this.Helper.ReadConfig<ModConfig>();
        CanCreateCharacter = this.Config.AllowCharacterCreation;
        
        EnabledCharacterOptions = this.Config.CharacterCreationOptions;
        DefaultCharacterOptions = this.Config.CharacterCreationDefault;


        
        Logger.SetMonitor(Monitor);
        
        helper.Events.GameLoop.GameLaunched += GameLaunched;
        helper.Events.GameLoop.UpdateTicking += UpdateTicking;
        helper.Events.GameLoop.SaveLoaded += GameLoopOnSaveLoaded;
    }

    private void UpdateTicking(object? sender, UpdateTickingEventArgs e)
    {
        NeuroSDKCsharp.Websocket.WebsocketHandler.Instance!.Update(); // this is used to send websocket messages
    }

    private void GameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        NeuroSDKCsharp.SdkSetup.Initialize("Stardew Valley");
    }

    private void GameLoopOnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        Context.Send($"This should send a save is loaded starts :)");
        NeuroActionHandler.RegisterActions(new MainGameActions.Pathfinding());
        NeuroActionHandler.RegisterActions(new MainGameActions.UseItem());
    }
}