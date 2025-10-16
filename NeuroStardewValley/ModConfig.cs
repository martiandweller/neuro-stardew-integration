namespace NeuroStardewValley;

public class ModConfig
{
    // this allows for many debug features to be used, many triggerable through hotkeys.
    public bool Debug { get; set; } = true; // change to false for proper releases
    public string WebsocketUri { get; set; } = "ws://localhost:8000/ws/";
    public bool AllowCharacterCreation { get; set; } = false; // Allow Neuro to create her own character.
    public bool RegisterIfPausedForLong { get; set; } = true; // re-register main actions if paused for too long
    public int TimeUntilRegisterAgain { get; set; } = 60000; // time until register actions again in milliseconds.
    public int SaveSlot { get; set; } = 0; // save slot to use.
    [Obsolete("The objects in the radius are no longer sent, might make this a config option later so its being kept")]
    public int TileContextRadius { get; set; } = 50; // The radius of tiles to send as context.
    public int StaminaSendInterval { get; set; } = 400; // The amount of in-game hours between each stamina context, sent every hour divisible by four would be 400.
    public bool WaitTimeAction { get; set; } = true; // Allow Neuro to use an action that allows her wait for until a provided time.
    
    // specific actions config

    // When Neuro uses a query action it will ask her for a radius, this limits the size of what she can receive.
    public int MinQueryRange { get; set; } = 3;
    public int MaxQueryRange { get; set; } = 100;
    public Dictionary<string, bool> CharacterCreationOptions { get; set; } = new()
    {
        { "skin", true },
        { "gender", true },
        { "hair", true },
        { "shirt", true },
        { "pants", true },
        { "accessories", true },
        { "name", true },
        { "farm_name", true },
        { "favourite_thing", true },
        { "animal_preference", true },
        { "animal_breed", true },
        { "eye_hue", true },
        { "eye_saturation", true },
        { "eye_brightness", true },
        { "hair_hue", true },
        { "hair_saturation", true },
        { "hair_brightness", true },
        { "pants_hue", true },
        { "pants_saturation", true },
        { "pants_brightness", true },
        { "farm_type", true }
    };

    public Dictionary<string, string> CharacterCreationDefault { get; set; } = new()
    {
        { "skin", "" },
        { "gender", "" },
        { "hair", "" },
        { "shirt", "" },
        { "pants", "" },
        { "accessories", "" },
        { "name", "" },
        { "farm_name", "" },
        { "favourite_thing", "" },
        { "animal_preference", "" },
        { "animal_breed", "" },
        { "eye_hue", "" },
        { "eye_saturation", "" },
        { "eye_brightness", "" },
        { "hair_hue", "" },
        { "hair_saturation", "" },
        { "hair_brightness", "" },
        { "pants_hue", "" },
        { "pants_saturation", "" },
        { "pants_brightness", "" },
        { "farm_type", "" }
    };
}