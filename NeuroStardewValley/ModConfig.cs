namespace NeuroStardewValley;

public class ModConfig
{
    public bool Debug { get; set; } // this allows for many debug features to be used, many triggerable through hotkeys. Default = false
    public string WebsocketUri { get; set; }
    public bool AllowCharacterCreation { get; set; } // Allow Neuro to create her own character. Default = false
    public int SaveSlot { get; set; } // save slot to use. Default = 0
    public int TileContextRadius { get; set; } // The radius of tiles to send as context. Default = 50
    public int StaminaSendInterval { get; set; } // The amount of in-game hours between each stamina context, sent every hour divisible by four would be 400. Default = 400
    public Dictionary<string, bool> CharacterCreationOptions { get; set; }
    public Dictionary<string, string> CharacterCreationDefault { get; set; }


    public ModConfig()
    {
        Debug = true; // change to false for proper releases
        WebsocketUri = "ws://localhost:8000/ws/";
        AllowCharacterCreation = false;
        SaveSlot = 0;
        TileContextRadius = 50;
        StaminaSendInterval = 400;
        
        CharacterCreationOptions = new()
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

        CharacterCreationDefault = new()
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
}