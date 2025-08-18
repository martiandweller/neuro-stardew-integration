namespace NeuroStardewValley;

public class ModConfig
{
    public bool Debug { get; set; } // this allows for many debug features to be used, many triggerable through hotkeys.
    public string WebsocketUri { get; set; }
    public bool AllowCharacterCreation { get; set; } // Allow Neuro to create her own character
    public int SaveSlot { get; set; } // save slot to use
    public Dictionary<string, bool> CharacterCreationOptions { get; set; }
    public Dictionary<string, string> CharacterCreationDefault { get; set; }


    public ModConfig()
    {
        Debug = true; // change to false for proper releases
        WebsocketUri = "ws://localhost:8000/ws/";
        AllowCharacterCreation = false;
        SaveSlot = 0;
        
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