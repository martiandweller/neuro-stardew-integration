namespace NeuroStardewValley;

public class ModConfig
{
    public bool AllowCharacterCreation { get; set; }// Allow Neuro to create her own character

    public Dictionary<string,bool> CharacterCreationOptions { get; set; }
    public Dictionary<string,string> CharacterCreationDefault { get; set; }
     public ModConfig()
     { 
         AllowCharacterCreation = false;

         CharacterCreationOptions = new()
         {
             { "skin", true },
             { "gender", true},
             { "hair",true },
             { "shirt", true},
             { "pants", true},
             { "accessories",true},
             { "name", true},
             { "farm_name", true},
             { "favourite_thing", true},
             { "animal_preference" , true},
             { "eye_colour", true},
             { "hair_colour", true},
             { "pants_colour", true},
             { "farm_type", true}
         };
         
         CharacterCreationDefault = new () 
         {
             { "skin", "" },
             { "gender", ""},
             { "hair", "" },
             { "shirt", ""},
             { "pants", ""},
             { "accessories", ""},
             { "name", ""},
             { "farm_name", ""},
             { "favourite_thing", ""},
             { "animal_preference" , ""},
             { "eye_colour", ""},
             { "hair_colour", ""},
             { "pants_colour", ""},
             { "farm_type", ""}
         };
     }
}