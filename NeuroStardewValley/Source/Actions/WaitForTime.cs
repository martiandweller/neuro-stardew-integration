using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Websocket;
using NeuroStardewValley.Source.EventMethods;
using NeuroStardewValley.Source.Utilities;
using NeuroStardewValley.Debug;
using StardewValley;

namespace NeuroStardewValley.Source.Actions;

public class WaitForTime : NeuroAction<int>
{
	public override string Name => "wait_for_time";
	// TODO: make seconds correct
	protected override string Description =>
		$"Wait until a specified in-game time, each 10 in-game minutes lasts for {Game1.gameTimeInterval} seconds. You should send the time as a string, in the format hour:minute.";
	protected override JsonSchema Schema => new()
	{
		Type = JsonSchemaType.Object,
		Required = new List<string> { "time" },
		Properties = new Dictionary<string, JsonSchema>
		{
			["time"] = QJS.Type(JsonSchemaType.String)
		}
	};
	protected override ExecutionResult Validate(ActionData actionData, out int resultData)
	{
		string? timeStr = actionData.Data?.Value<string>("time");

		resultData = -1;
		if (string.IsNullOrEmpty(timeStr))
		{
			return ExecutionResult.Failure($"The time you provided was not in the correct format.");
		}

		int time = StringUtilities.TimeStringToInt(timeStr);

		Logger.Info($"time: {time}");
		if (time == -1)
		{
			return ExecutionResult.Failure($"The time you provided was not in the correct format.");
		}
		
		if (time < Game1.timeOfDay) return ExecutionResult.Failure($"The time you provided is earlier than the current time.");
		resultData = time;
		return ExecutionResult.Success($"Waiting until {timeStr}");
	}

	protected override void Execute(int resultData)
	{
		LessImportantEvents.WaitingTime = resultData;
	}
}