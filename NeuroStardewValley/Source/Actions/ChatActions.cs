using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Websocket;
using NeuroStardewValley.Source.RegisterActions;
using StardewValley;
using Farmer = StardewValley.Farmer;

namespace NeuroStardewValley.Source.Actions;

public static class ChatActions
{
    public class SendChatMessage : NeuroAction<List<string>>
    {
        private string[] Action => new[] { "Public", "Private","No reply" };
        
        private string[] GetOtherPlayers()
        {
            string[] names = new string[Game1.otherFarmers.Count];
            foreach (var kvp in Game1.otherFarmers)
            {
                names[kvp.Key] = kvp.Value.Name;
            }

            return names;
        }
        
        public override string Name => "reply_to_chat";
        protected override string Description => "This allows you to reply to the last chat, if you do not want to reply you should send No reply";

        protected override JsonSchema Schema => new()
        {
            Type = JsonSchemaType.Object,
            Required = new List<string> { "action","message" },
            Properties = new Dictionary<string, JsonSchema>
            {
                ["action"] = QJS.Enum(Action),
                ["message"] = QJS.Type(JsonSchemaType.String),
                ["private_player_name"] = QJS.Enum(GetOtherPlayers())
            }
        };
        
        protected override ExecutionResult Validate(ActionData actionData, out List<string>? resultData)
        {
            string? action = actionData.Data?.Value<string>("action");
            string? message = actionData.Data?.Value<string>("message");
            string? playerName = actionData.Data?.Value<string>("private_player_name");
            resultData = new ();

            if (action is null || message is null || playerName is null && action == "Private")
            {
                return ExecutionResult.Failure("A parameter was not set correctly");
            }

            if (action == "No reply")
            {
                resultData[0] = "";
                return ExecutionResult.Success("Nothing was sent in chat.");
            }

            if (!Action.Contains(action))
            {
                return ExecutionResult.Failure($"{action} is not a valid action");
            }

            if (!GetOtherPlayers().Contains(playerName))
            {
                return ExecutionResult.Failure($"{playerName} is not a valid player");
            }

            if (playerName is null)
            {
                return ExecutionResult.Failure($"{playerName} was not valid");
            }
            resultData.Add(action);
            resultData.Add(message);
            resultData.Add(playerName);

            var successString = $"You have sent {message} in {action} chat";
            if (action == "Private") successString += $" to {playerName}";
            return ExecutionResult.Success(successString);
        }

        protected override void Execute(List<string>? resultData)
        {
            if (resultData is null) return;
            
            if (resultData[0] == "Private")
            {
                Main.Bot.Chat.SendPrivateMessage(resultData[2],resultData[1]);
            }
            else if (resultData[0] == "Public")
            {
                Main.Bot.Chat.SendPublicMessage(resultData[1]);
            }
        }
    }

    public class UseEmote : NeuroAction<string>
    {
        public override string Name => "use_emote";
        protected override string Description => "Use an emote";
        protected override JsonSchema Schema => new()
        {
            Type = JsonSchemaType.Object,
            Required = new List<string> { "emote" },
            Properties = new Dictionary<string, JsonSchema>
            {
                ["emote"] = QJS.Enum(Farmer.EMOTES.Where(emoteType =>
                    !emoteType.hidden || Main.Bot._farmer.performedEmotes.ContainsKey(emoteType.emoteString))
                    .Select(emote => emote.displayName))
            }
        };
        protected override ExecutionResult Validate(ActionData actionData, out string? resultData)
        {
            string? emote = actionData.Data?.Value<string>("emote");

            resultData = "";
            if (emote is null)
            {
                return ExecutionResult.Failure($"You cannot provide a null value.");
            }

            if (Farmer.EMOTES.Where(emoteType => emoteType.hidden).Select(type => type.displayName).Contains(emote) 
                || !Farmer.EMOTES.Select(type => type.displayName).Contains(emote))
            {
                return ExecutionResult.Failure($"The value you provided is not a valid emote.");
            }
            
            resultData = emote;
            return ExecutionResult.Success($"Doing {emote}");
        }

        protected override void Execute(string? resultData)
        {
            var emote = Farmer.EMOTES.Where(emote => emote.displayName == resultData).ToArray()[0];
            Main.Bot.Chat.UseEmote(emote.emoteString);
            
            DelayedAction.functionAfterDelay(() => RegisterMainActions.RegisterPostAction(), 2000);
        }
    }
}