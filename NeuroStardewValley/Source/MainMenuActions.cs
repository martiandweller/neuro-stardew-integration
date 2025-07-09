using System.Text;
using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Messages.Outgoing;
using NeuroSDKCsharp.Websocket;
using StardewBotFramework.Source;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace NeuroStardewValley.Source;

public class MainMenuActions
{
    public class CreateCharacter : NeuroAction<Dictionary<string, string?>>
    {
        private static bool CanCreateCharacter => ModEntry.CanCreateCharacter;

        private static Dictionary<string, bool> EnabledCharacterOptions => ModEntry.EnabledCharacterOptions;
    
        private static Dictionary<string, string> DefaultCharacterOptions => ModEntry.DefaultCharacterOptions;
        
        public override string Name => "create_character";

        protected override string Description {
            get
            {
                if (CanCreateCharacter)
                {
                    return "Create a character, this character can be anything or anyone as long you can make it. This will not be able to be changed in the future so be careful.";
                }
                else
                {
                    return "This will start the game as a default character that has already been decided.";
                }
            }
        }

        protected override JsonSchema? Schema => new()
        {
            Type = JsonSchemaType.Object,
            Required = EnabledCharacterOptions.Keys.ToList(),
            Properties = CharacterSchema()
        };
        
        protected override ExecutionResult Validate(ActionData actionData, out Dictionary<string,string?> data)
        {
            if (CanCreateCharacter)
            {
                data = null;
                return ExecutionResult.Success();
            }
            data = new();
            foreach (var kvp in EnabledCharacterOptions)
            {
                data[kvp.Key] = actionData.Data?.Value<string>(kvp.Key);   
            }
            
            return ExecutionResult.Success();
        }

        protected override async Task<Task> Execute(Dictionary<string,string?>? data)
        {
            if (CanCreateCharacter) return Task.CompletedTask;
            if (data is null) return Task.FromCanceled(CancellationToken.None);
            
            SetCharacter(data);
            return Task.CompletedTask;
        }

        private static List<string> _catBreedStrings = new()
        {
            "It is an orange tabby cat.", "It is a gray British shorthair cat.", "It is a yellow tabby cat.", "It is a white Persian cat.", "It is a black Bombay cat."
        };
        private static List<string> _dogBreedStrings = new()
        {
            "It is an a yellow Labrador Retriever.", "It is an orange Vizsla.", "It is a beige Poodle.", "It is a gray Schnauzer.", "It is a brown Doberman Pinscher."
        };

        private static List<string> _petBreedStrings = new()
        {
            "It is an orange tabby cat.", "It is a gray British shorthair cat.", "It is a yellow tabby cat.",
            "It is a white Persian cat.", "It is a black Bombay cat.", "It is an a yellow Labrador Retriever.",
            "It is an orange Vizsla.", "It is a beige Poodle.", "It is a gray Schnauzer.",
            "It is a brown Doberman Pinscher."
        };
        
        private static Dictionary<string, JsonSchema> CharacterSchema()
        {
            Dictionary<string, JsonSchema> properties = new();
            
            ModEntry.Bot.CharacterCreation.SetCreator((CharacterCustomization)TitleMenu.subMenu);
            ModEntry.Bot.CharacterCreation.SkipIntro();
            if (!CanCreateCharacter)
            {
                SetCharacter(DefaultCharacterOptions!);
                
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
                            for (int i = 0; i < ModEntry.Bot.CharacterCreation.GetPossibleShirts().Values.Count; i++)
                            {
                                shirtList.Add($"string id: {ModEntry.Bot.CharacterCreation.GetPossibleShirts().Keys.ToArray()[i]} shirt name: {ModEntry.Bot.CharacterCreation.GetPossibleShirts().Values.ToArray()[i]}");
                            }
                            IEnumerable<string> shirtEnumerable = shirtList;
                            Context.Send($"All possible shirts: {shirtEnumerable}");
                            properties.Add("Shirt",QJS.Enum(Enumerable.Range(0,ModEntry.Bot.CharacterCreation.GetPossibleShirts().Values.Count)));
                            break;
                        case "pants": // 0-3
                            List<string> pantsList = new();
                            for (int i = 0; i < ModEntry.Bot.CharacterCreation.GetPossiblePants().Values.Count; i++)
                            {
                                pantsList.Add($"pants id: {ModEntry.Bot.CharacterCreation.GetPossiblePants().Keys.ToArray()[i]} pants name: {ModEntry.Bot.CharacterCreation.GetPossiblePants().Values.ToArray()[i]}");
                            }
                            IEnumerable<string> pantsEnumerable = pantsList;
                            Context.Send($"All possible pants: {pantsEnumerable}");
                            properties.Add("Pants",QJS.Enum(Enumerable.Range(0,ModEntry.Bot.CharacterCreation.GetPossiblePants().Values.Count)));
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
                            properties.Add(kvp.Key + "_animal_type",QJS.Enum(new []{"Cat","Dog"}));
                            properties.Add(kvp.Key + "_dog_breed",QJS.Enum(_dogBreedStrings));// find better way to do this
                            properties.Add(kvp.Key + "_cat_breed",QJS.Enum(_catBreedStrings));
                            break;
                        case "eye_hue":
                            properties.Add(kvp.Key + "_hue",QJS.Type(JsonSchemaType.Integer));
                            properties.Add(kvp.Key + "_saturation",QJS.Type(JsonSchemaType.Integer));
                            properties.Add(kvp.Key + "_brightness",QJS.Type(JsonSchemaType.Integer));
                            break;
                        case "hair_hue":
                            properties.Add(kvp.Key + "_hue",QJS.Type(JsonSchemaType.Integer));
                            properties.Add(kvp.Key + "_saturation",QJS.Type(JsonSchemaType.Integer));
                            properties.Add(kvp.Key + "_brightness",QJS.Type(JsonSchemaType.Integer));
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
                    switch (kvp.Key)
                    {
                        case "gender":
                            ModEntry.Bot.CharacterCreation.ChangeGender(DefaultCharacterOptions["gender"] == "male");
                            break;
                        case "skin":
                            ModEntry.Bot.CharacterCreation.ChangeSkinColour(int.Parse(DefaultCharacterOptions["skin"]));
                            break;
                        case "hair":
                            ModEntry.Bot.CharacterCreation.ChangeHair(int.Parse(DefaultCharacterOptions["hair"]));
                            break;
                        case "shirt":
                            ModEntry.Bot.CharacterCreation.ChangeShirt(int.Parse(DefaultCharacterOptions["shirt"]));
                            break;
                        case "pants":
                            ModEntry.Bot.CharacterCreation.ChangePants(int.Parse(DefaultCharacterOptions["pants"]));
                            break;
                        case "accessories":
                            ModEntry.Bot.CharacterCreation.ChangeAccessory(int.Parse(DefaultCharacterOptions["accessories"]));
                            break;
                        case "name":
                            ModEntry.Bot.CharacterCreation.SetName(DefaultCharacterOptions["name"]);
                            break;
                        case "farm_name":
                            ModEntry.Bot.CharacterCreation.SetFarmName(DefaultCharacterOptions["farm_name"]);
                            break;
                        case "favourite_thing":
                            ModEntry.Bot.CharacterCreation.SetFavThing(DefaultCharacterOptions["favourite_thing"]);
                            break;
                        case "animal_preference":
                            ModEntry.Bot.CharacterCreation.ChangePet(DefaultCharacterOptions["animal_preference"], DefaultCharacterOptions["animal_breed"]);
                            break;
                        case "eye_hue":
                            ModEntry.Bot.CharacterCreation.ChangeColour(0,int.Parse(DefaultCharacterOptions["eye_hue"]),int.Parse(DefaultCharacterOptions["eye_saturation"]),int.Parse(DefaultCharacterOptions["eye_brightness"]));
                            break;
                        case "hair_hue":
                            ModEntry.Bot.CharacterCreation.ChangeColour(1,int.Parse(DefaultCharacterOptions["hair_hue"]),int.Parse(DefaultCharacterOptions["hair_saturation"]),int.Parse(DefaultCharacterOptions["hair_brightness"]));
                            break;
                        case "pants_hue":
                            ModEntry.Bot.CharacterCreation.ChangeColour(2,int.Parse(DefaultCharacterOptions["pants_hue"]),int.Parse(DefaultCharacterOptions["pants_saturation"]),int.Parse(DefaultCharacterOptions["pants_brightness"]));
                            break;
                        case "farm_type":
                            ModEntry.Bot.CharacterCreation.ChangeFarmTypes(int.Parse(DefaultCharacterOptions["farm_type"]));
                            break;
                    }
                }
            }
            

            return properties;
        }

        private static void SetCharacter(Dictionary<string, string?> choice)
        {
            foreach (var kvp in EnabledCharacterOptions)
            {
                if (!kvp.Value)
                {
                    continue;
                }
                
                switch (kvp.Key)
                {
                    case "gender":
                        ModEntry.Bot.CharacterCreation.ChangeGender(choice["gender"] == "male");
                        break;
                    case "skin":
                        ModEntry.Bot.CharacterCreation.ChangeSkinColour(int.Parse(choice["skin"]));
                        break;
                    case "hair":
                        ModEntry.Bot.CharacterCreation.ChangeHair(int.Parse(choice["hair"]));
                        break;
                    case "shirt":
                        ModEntry.Bot.CharacterCreation.ChangeShirt(int.Parse(choice["shirt"]));
                        break;
                    case "pants":
                        ModEntry.Bot.CharacterCreation.ChangePants(int.Parse(choice["pants"]));
                        break;
                    case "accessories":
                        ModEntry.Bot.CharacterCreation.ChangeAccessory(int.Parse(choice["accessories"]));
                        break;
                    case "name":
                        ModEntry.Bot.CharacterCreation.SetName(choice["name"]);
                        break;
                    case "farm_name":
                        ModEntry.Bot.CharacterCreation.SetFarmName(choice["farm_name"]);
                        break;
                    case "favourite_thing":
                        ModEntry.Bot.CharacterCreation.SetFavThing(choice["favourite_thing"]);
                        break;
                    case "animal_preference":
                        ModEntry.Bot.CharacterCreation.ChangePet(choice["animal_preference"], choice["animal_breed"]);
                        break;
                    case "eye_hue":
                        ModEntry.Bot.CharacterCreation.ChangeColour(0,int.Parse(choice["eye_hue"]),int.Parse(choice["eye_saturation"]),int.Parse(choice["eye_brightness"]));
                        break;
                    case "hair_hue":
                        ModEntry.Bot.CharacterCreation.ChangeColour(1,int.Parse(choice["hair_hue"]),int.Parse(choice["hair_saturation"]),int.Parse(choice["hair_brightness"]));
                        break;
                    case "pants_hue":
                        ModEntry.Bot.CharacterCreation.ChangeColour(2,int.Parse(choice["pants_hue"]),int.Parse(choice["pants_saturation"]),int.Parse(choice["pants_brightness"]));
                        break;
                    case "farm_type":
                        ModEntry.Bot.CharacterCreation.ChangeFarmTypes(int.Parse(choice["farm_type"]));
                        break;
                }    
            }
            
        }
    }
}