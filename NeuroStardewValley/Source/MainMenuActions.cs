using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Messages.Outgoing;
using NeuroSDKCsharp.Websocket;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace NeuroStardewValley.Source;

public class MainMenuActions
{
    private bool CharacterCreate => ModEntry.CanCreateCharacter;

    public class CreateCharacter : NeuroAction<Dictionary<string, string?>>
    {
        private static Dictionary<string, bool> EnabledCharacterOptions => ModEntry.EnabledCharacterOptions;
    
        private Dictionary<string, string> DefaultCharacterOptions => ModEntry.DefaultCharacterOptions;
        
        public override string Name => "create_character";

        protected override string Description =>
            "Create a character, this character can be anything or anyone as long you can make it.";

        protected override JsonSchema? Schema => new()
        {
            Type = JsonSchemaType.Object,
            Required = EnabledCharacterOptions.Keys.ToList(),
            Properties = CharacterSchema()
        };
        
        protected override ExecutionResult Validate(ActionData actionData, out Dictionary<string,string?> data)
        {
            data = new();
            foreach (var kvp in EnabledCharacterOptions)
            {
                data[kvp.Key] =  actionData.Data?.Value<string>(kvp.Key);   
            }
            
            return ExecutionResult.Success();
        }

        protected override async Task<Task> Execute(Dictionary<string,string?>? data)
        {
            if (data["gender"] == "Male")
            {
                ModEntry.Bot.CharacterCreation.ChangeGender(true);
            }
            else
            {
                ModEntry.Bot.CharacterCreation.ChangeGender(false);
            }
            
            ModEntry.Bot.CharacterCreation.ChangeAccessory(int.Parse(data["accessories"]));

            ModEntry.Bot.CharacterCreation.SetName(data["name"]);
            ModEntry.Bot.CharacterCreation.SetFarmName(data["farm_name"]);
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
            foreach (var kvp in EnabledCharacterOptions)
            {
                if (kvp.Value)
                {
                    switch (kvp.Key) // No way to get name for a lot of this stuff, and I am a bit too lazy to do it manually
                    {
                        case "skin": // 0-23
                            properties.Add(kvp.Key,QJS.Enum(Enumerable.Range(0,23)));
                            break;
                        case "hair": // 0-73
                            properties.Add(kvp.Key,QJS.Enum(Enumerable.Range(0,73)));
                            break;
                        case "gender":
                            properties.Add(kvp.Key,QJS.Enum(new []{"male","female"}));
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
                        case "eye_colour":
                            properties.Add(kvp.Key + "_hue",QJS.Type(JsonSchemaType.Integer));
                            properties.Add(kvp.Key + "_saturation",QJS.Type(JsonSchemaType.Integer));
                            properties.Add(kvp.Key + "_brightness",QJS.Type(JsonSchemaType.Integer));
                            break;
                        case "hair_colour":
                            properties.Add(kvp.Key + "_hue",QJS.Type(JsonSchemaType.Integer));
                            properties.Add(kvp.Key + "_saturation",QJS.Type(JsonSchemaType.Integer));
                            properties.Add(kvp.Key + "_brightness",QJS.Type(JsonSchemaType.Integer));
                            break;
                        case "pants_colour":
                            properties.Add(kvp.Key + "_hue",QJS.Type(JsonSchemaType.Integer));
                            properties.Add(kvp.Key + "_saturation",QJS.Type(JsonSchemaType.Integer));
                            properties.Add(kvp.Key + "_brightness",QJS.Type(JsonSchemaType.Integer));
                            break;
                        case "farm_types":
                            properties.Add(kvp.Key,QJS.Enum(Enumerable.Range(0,7)));
                            break;
                    }
                    
                }
            }

            return properties;
        }
    }
}