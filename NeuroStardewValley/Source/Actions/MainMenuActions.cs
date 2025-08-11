using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Messages.Outgoing;
using NeuroSDKCsharp.Websocket;
using NeuroStardewValley.Debug;
using StardewValley.GameData.Pets;
using StardewValley.Menus;

namespace NeuroStardewValley.Source.Actions;

public static class MainMenuActions
{
    public class CreateCharacter : NeuroAction<Dictionary<string, string?>>
    {
        private static bool CanCreateCharacter => Main.CanCreateCharacter;

        private static Dictionary<string, bool> EnabledCharacterOptions => Main.EnabledCharacterOptions;
    
        private static Dictionary<string, string> DefaultCharacterOptions => Main.DefaultCharacterOptions;
        
        public override string Name
        {
            get 
            {
                if (CanCreateCharacter)
                {
                    return "create_character";
                }
                return "start_new_game";
            }
        }

        protected override string Description {
            get
            {
                if (CanCreateCharacter)
                {
                    return "Create a character, this character can be anything or anyone as long you can make it. This will not be able to be changed in the future so be careful.";
                }
                return "This will start the game as a character that has already been decided for you.";
            }
        }

        protected override JsonSchema? Schema => new()
        {
            Type = JsonSchemaType.Object,
            Required = RequiredOptions(),
            Properties = CharacterSchema()
        };
        
        protected override ExecutionResult Validate(ActionData actionData, out Dictionary<string,string?> data)
        {
            data = new();
            if (!CanCreateCharacter)
            {
                return ExecutionResult.Success();
            }
            foreach (var kvp in EnabledCharacterOptions)
            {
                data[kvp.Key] = actionData.Data?.Value<string>(kvp.Key);
                if (data[kvp.Key] != "animal_breed") continue;
                if (!Main.Bot.CharacterCreation.ChangePet(data["animal_preference"]!, data["animal_breed"]!))
                {
                    return ExecutionResult.Failure("That is not a valid animal");
                }
            }
            
            return ExecutionResult.Success();
        }

        protected override void Execute(Dictionary<string,string?>? data)
        {
            if (!CanCreateCharacter)
            {
                Main.Bot.CharacterCreation.StartGame();
                return;
            }
            if (data == new Dictionary<string, string>()) return;
            
            SetCharacter(data,true);
        }

        private static readonly List<string> CatBreedStrings = new()
        {
            "It is an orange tabby cat.", "It is a gray British shorthair cat.", "It is a yellow tabby cat.", "It is a white Persian cat.", "It is a black Bombay cat."
        };
        private static readonly List<string> DogBreedStrings = new()
        {
            "It is an a yellow Labrador Retriever.", "It is an orange Vizsla.", "It is a beige Poodle.", "It is a gray Schnauzer.", "It is a brown Doberman Pinscher."
        };

        private static string[] GetValidPetNames()
        {
            IDictionary<string, PetData> petData = Main.Bot.CharacterCreation.GetPetData();

            IEnumerable<KeyValuePair<string, PetData>> petNames =
                petData.Where(kvp => kvp.Value.Breeds.Any(breed => breed.CanBeChosenAtStart));
            string[] names = Array.Empty<string>();
            foreach (var kvp in petNames)
            {
                names = names.Append(kvp.Key).ToArray();
            }

            return names;
        }

        private static string[] GetModdedPetBreeds()
        {
            IDictionary<string, PetData> petData = Main.Bot.CharacterCreation.GetPetData();
            
            IEnumerable<KeyValuePair<string, PetData>> petNames =
                petData.Where(kvp => kvp.Value.Breeds.Any(breed => breed.CanBeChosenAtStart));
            string[] index = Array.Empty<string>();
            foreach (var kvp in petNames)
            {
                for (int i = 0; i < kvp.Value.Breeds.Count; i++)
                {
                    index = kvp.Key switch
                    {
                        "Dog" => index.Append(DogBreedStrings[i]).ToArray(),
                        "Cat" => index.Append(CatBreedStrings[i]).ToArray(),
                        _ => index.Append($"{kvp.Key}: {i}").ToArray()
                    };
                }
            }

            return index;
        }
        
        private static List<string> RequiredOptions()
        {
            List<string> enabled = new();
            foreach (var kvp in EnabledCharacterOptions)
            {
                if (kvp.Value)
                {
                    enabled.Add(kvp.Key);
                }
            }

            return enabled;
        }
        
        private static Dictionary<string, JsonSchema> CharacterSchema()
        {
            Dictionary<string, JsonSchema> properties = new();
            
            Main.Bot.CharacterCreation.SetCreator((CharacterCustomization)TitleMenu.subMenu);
            Main.Bot.CharacterCreation.SkipIntro();
            if (!CanCreateCharacter)
            {
                SetCharacter(DefaultCharacterOptions!,true);
                
                return new();
            }
            
            foreach (var kvp in EnabledCharacterOptions)
            {
                if (kvp.Value)
                {
                    switch (kvp.Key) // No way to get name for a lot of this stuff, and I am a bit too lazy to do it manually
                    {
                        case "gender":
                            properties.Add(kvp.Key,QJS.Enum(new []{"male","female"}));
                            break;
                        case "skin": // 0-23
                            properties.Add(kvp.Key,QJS.Enum(Enumerable.Range(0,23)));
                            break;
                        case "hair": // 0-73
                            properties.Add(kvp.Key,QJS.Enum(Enumerable.Range(0,73)));
                            break;
                        case "shirt": // 0-111
                            List<string> shirtList = new();
                            for (int i = 0; i < Main.Bot.CharacterCreation.GetPossibleShirts().Values.Count; i++)
                            {
                                shirtList.Add($"string id: {Main.Bot.CharacterCreation.GetPossibleShirts().Keys.ToArray()[i]} shirt name: {Main.Bot.CharacterCreation.GetPossibleShirts().Values.ToArray()[i]}");
                            }
                            IEnumerable<string> shirtEnumerable = shirtList;
                            Context.Send($"All possible shirts: {shirtEnumerable}");
                            properties.Add("Shirt",QJS.Enum(Enumerable.Range(0,Main.Bot.CharacterCreation.GetPossibleShirts().Values.Count)));
                            break;
                        case "pants": // 0-3
                            List<string> pantsList = new();
                            for (int i = 0; i < Main.Bot.CharacterCreation.GetPossiblePants().Values.Count; i++)
                            {
                                pantsList.Add($"pants id: {Main.Bot.CharacterCreation.GetPossiblePants().Keys.ToArray()[i]} pants name: {Main.Bot.CharacterCreation.GetPossiblePants().Values.ToArray()[i]}");
                            }
                            IEnumerable<string> pantsEnumerable = pantsList;
                            Context.Send($"All possible pants: {pantsEnumerable}");
                            properties.Add("Pants",QJS.Enum(Enumerable.Range(0,Main.Bot.CharacterCreation.GetPossiblePants().Values.Count)));
                            break;
                        case "accessories": // 0-30
                            properties.Add(kvp.Key,QJS.Enum(Enumerable.Range(0,30)));
                            break;
                        case "name":
                            properties.Add(kvp.Key,QJS.Type(JsonSchemaType.String));
                            break;
                        case "farm_name":
                            properties.Add(kvp.Key,QJS.Type(JsonSchemaType.String));
                            break;
                        case "favourite_thing":
                            properties.Add(kvp.Key,QJS.Type(JsonSchemaType.String));
                            break;
                        case "animal_preference": // 2 animal types 0-3 options for each
                            properties.Add(kvp.Key + "",QJS.Enum(GetValidPetNames()));
                            properties.Add(kvp.Key + "",QJS.Enum(GetModdedPetBreeds()));// find better way to do this
                            break;
                        case "eye_hue":
                            properties.Add("eye_hue",QJS.Type(JsonSchemaType.Integer));
                            properties.Add("eye_saturation",QJS.Type(JsonSchemaType.Integer));
                            properties.Add("eye_brightness",QJS.Type(JsonSchemaType.Integer));
                            break;
                        case "hair_hue":
                            properties.Add("hair_hue",QJS.Type(JsonSchemaType.Integer));
                            properties.Add("hair_saturation",QJS.Type(JsonSchemaType.Integer));
                            properties.Add("hair_brightness",QJS.Type(JsonSchemaType.Integer));
                            break;
                        case "pants_hue":
                            properties.Add(kvp.Key + "_hue",QJS.Type(JsonSchemaType.Integer));
                            properties.Add(kvp.Key + "_saturation",QJS.Type(JsonSchemaType.Integer));
                            properties.Add(kvp.Key + "_brightness",QJS.Type(JsonSchemaType.Integer));
                            break;
                        case "farm_types":
                            properties.Add(kvp.Key,QJS.Enum(Enumerable.Range(0,7)));
                            break;
                    }
                    
                }
                else
                {
                    Dictionary<string, string?> option = new();
                    switch (kvp.Key) // this is bad but better than what was here before
                    {
                        case "eye_hue":
                            option = ColourKeys("eye");
                            break;
                        case "hair_hue":
                            option = ColourKeys("hair");
                            break;
                        case "pants_hue":
                            option = ColourKeys("pants");
                            break;
                        default:
                            option.Add(kvp.Key,DefaultCharacterOptions[kvp.Key]);
                            break;
                    }
                    SetCharacter(option!);
                    option.Clear();
                }
            }
            
            return properties;
        }

        private static readonly List<string> Keys = new() { "_hue", "_saturation", "_brightness" };
        private static Dictionary<string, string?> ColourKeys(string key)
        {
            Dictionary<string, string?> dict = new();
            foreach (var keyString in Keys)
            {
                var finalString = key + keyString;
                dict.Add(finalString,DefaultCharacterOptions[finalString]);
            }

            return dict;
        }
        
        private static void SetCharacter(Dictionary<string, string?> choice,bool enabled = false)
        {
            Logger.Info($"Running set character");
            foreach (var kvp in EnabledCharacterOptions)
            {
                Logger.Info($"kvp: {kvp.Key}  value: {kvp.Value}");
                if (enabled)
                {
                    if (!kvp.Value)
                    {
                        continue;
                    }
                }
                else
                {
                    if (kvp.Value)
                    {
                        continue;
                    }
                }
                if (!choice.ContainsKey(kvp.Key)) continue;
                
                switch (kvp.Key)
                {
                    case "gender":
                        Main.Bot.CharacterCreation.ChangeGender(choice["gender"] == "male");
                        break;
                    case "skin":
                        Main.Bot.CharacterCreation.ChangeSkinColour(int.Parse(choice["skin"]!));
                        break;
                    case "hair":
                        Main.Bot.CharacterCreation.ChangeHair(int.Parse(choice["hair"]!));
                        break;
                    case "shirt":
                        Main.Bot.CharacterCreation.ChangeShirt(int.Parse(choice["shirt"]!));
                        break;
                    case "pants":
                        Main.Bot.CharacterCreation.ChangePants(int.Parse(choice["pants"]!));
                        break;
                    case "accessories":
                        Main.Bot.CharacterCreation.ChangeAccessory(int.Parse(choice["accessories"]!));
                        break;
                    case "name":
                        Main.Bot.CharacterCreation.SetName(choice["name"]!);
                        break;
                    case "farm_name":
                        Main.Bot.CharacterCreation.SetFarmName(choice["farm_name"]!);
                        break;
                    case "favourite_thing":
                        Main.Bot.CharacterCreation.SetFavThing(choice["favourite_thing"]!);
                        break;
                    case "animal_preference":
                        Logger.Info($"animal: {choice["animal_preference"]} int: {CatBreedStrings.IndexOf(choice["animal_breed"]!).ToString()}  string: {choice["animal_breed"]}");
                        if (DogBreedStrings.IndexOf(choice["animal_breed"]!) == -1)
                        {
                            Main.Bot.CharacterCreation.ChangePet(choice["animal_preference"]!, CatBreedStrings.IndexOf(choice["animal_breed"]!).ToString());
                        }
                        else
                        {
                            Main.Bot.CharacterCreation.ChangePet(choice["animal_preference"]!, DogBreedStrings.IndexOf(choice["animal_breed"]!).ToString());
                        }
                        break;
                    case "eye_hue":
                        Main.Bot.CharacterCreation.ChangeColour(0,int.Parse(choice["eye_hue"]!),int.Parse(choice["eye_saturation"]!),int.Parse(choice["eye_brightness"]!));
                        break;
                    case "hair_hue":
                        Main.Bot.CharacterCreation.ChangeColour(1,int.Parse(choice["hair_hue"]!),int.Parse(choice["hair_saturation"]!),int.Parse(choice["hair_brightness"]!));
                        break;
                    case "pants_hue":
                        Main.Bot.CharacterCreation.ChangeColour(2,int.Parse(choice["pants_hue"]!),int.Parse(choice["pants_saturation"]!),int.Parse(choice["pants_brightness"]!));
                        break;
                    case "farm_type":
                        Main.Bot.CharacterCreation.ChangeFarmTypes(int.Parse(choice["farm_type"]!));
                        break;
                }    
            }
            
        }
    }
}