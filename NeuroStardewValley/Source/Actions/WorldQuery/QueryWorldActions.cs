using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Messages.Outgoing;
using NeuroSDKCsharp.Websocket;
using NeuroStardewValley.Debug;
using NeuroStardewValley.Source.ContextStrings;
using NeuroStardewValley.Source.RegisterActions;
using Newtonsoft.Json.Linq;

namespace NeuroStardewValley.Source.Actions.WorldQuery;

public static class QueryWorldActions
{
	private static readonly int MinRadius = Main.Config.MinQueryRange;
	private static readonly int MaxRadius = Main.Config.MaxQueryRange;
	public class GetObjectsInRadius : NeuroAction<int>
	{
		private string[]? _objectNames;
		public override string Name => "get_objects_in_radius";
		protected override string Description => $"Get all of the objects in a radius between {MinRadius} and {MaxRadius}" +
		                                         $", you also have the option to limit it to objects that have a specified name";
		protected override JsonSchema Schema => new()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "radius" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["radius"] = QJS.Type(JsonSchemaType.Integer),
				["excluded_names"] = new()
				{
					Type = JsonSchemaType.Array,
					Items = new JsonSchema {Type = JsonSchemaType.String}
				}
			}
		};
		protected override ExecutionResult Validate(ActionData actionData, out int resultData)
		{
			_objectNames = null;
			resultData = -1;
			int radius = actionData.Data?.Value<int>("radius") ?? int.MaxValue;
			JArray? jArray = actionData.Data?.Value<JArray>("excluded_names");

			List<string> names = new();
			if (jArray is not null)
			{
				foreach (var token in jArray)
				{
					string? tokenStr = token.Value<string?>();
					if (tokenStr is null) continue;
				
					names.Add(tokenStr);
				}	
			}
			if (radius == int.MaxValue) return ExecutionResult.Failure($"You cannot specify a null radius");

			if (radius < MinRadius || radius > MaxRadius)
			{
				return ExecutionResult.Failure($"The radius you specified does not fall between the min and max radius.");
			}
			
			var tiles = TileContext.GetTilesInLocation(Main.Bot._currentLocation, Main.Bot._farmer.TilePoint,
				radius);
			
			if (tiles.Count < 1) return ExecutionResult.Failure($"There are no objects around you in that radius.");

			if (jArray != null) _objectNames = names.ToArray();
			resultData = radius;
			return ExecutionResult.Success($"");
		}

		protected override void Execute(int resultData)
		{
			string contextString = $"These are the objects in a radius of {resultData} at {Main.Bot._farmer.TilePoint} at {Main.Bot._currentLocation.DisplayName}";
			
			var tiles = TileContext.GetObjectsInLocation(Main.Bot._currentLocation, Main.Bot._farmer.TilePoint,
				resultData);

			foreach (var kvp in tiles)
			{
				Logger.Info($"tile: {kvp.Key.X} {kvp.Key.Y}");
				string? name = TileContext.GetTileContext(Main.Bot._currentLocation, kvp.Key.X, kvp.Key.Y);
				if (name is null) continue;

				if (_objectNames is null)
				{
					contextString += $"\n{name}";
					continue;
				}

				bool contains = false;
				foreach (var nonValidNames in _objectNames)
				{
					if (name.Contains(nonValidNames))
					{
						contains = true;
					}
				}

				if (contains) continue;
				contextString += $"\n{name}";
			}
			// should probably find a better way to do this
			TileContext.SentBuildings.Clear();
			TileContext.SentFurniture.Clear();
			
			Context.Send(contextString,true);
			RegisterMainActions.RegisterPostAction();
		}
	}
 
	public class GetObjectTypeInRadius : NeuroAction<KeyValuePair<string,int>>
	{
		public override string Name => "get_object_type_in_range";
		protected override string Description => $"Get only the specified type of object in a range between {MinRadius} and {MaxRadius}, certain names may not" +
		                                         " be very obvious e.g. HoeDirt being for the dirt you can plant on.";
		protected override JsonSchema Schema => new()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "object_name","radius" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["object_name"] = QJS.Enum(TileContext.GetObjectAmountInLocation(Main.Bot._currentLocation)
					.Select(kvp => kvp.Key).ToList()),
				["radius"] = QJS.Type(JsonSchemaType.Integer),
			}
		};
		protected override ExecutionResult Validate(ActionData actionData, out KeyValuePair<string, int> resultData)
		{
			string? name = actionData.Data?.Value<string>("object_name");
			int? radius = actionData.Data?.Value<int>("radius");

			resultData = new();
			if (string.IsNullOrEmpty(name) || radius is null)
			{
				return ExecutionResult.Failure($"You cannot specify a null value.");
			}

			if (radius < MinRadius || radius > MaxRadius)
			{
				return ExecutionResult.Failure($"The radius should only be between {MinRadius} and {MaxRadius}.");
			}

			if (!TileContext.GetObjectAmountInLocation(Main.Bot._currentLocation)
				    .Select(kvp => kvp.Key).ToList().Contains(name))
			{
				return ExecutionResult.Failure($"The name you specified is not valid.");
			}
			
			if (TileContext.GetSpecifiedObjects(name, Main.Bot._farmer.TilePoint,(int)radius, Main.Bot._currentLocation).Length < 1)
				return ExecutionResult.Failure($"There is no {name} in a radius of {radius}, you can try to either increase the radius or try something else.");

			resultData = new(name, (int)radius);
			return ExecutionResult.Success("");
		}

		protected override void Execute(KeyValuePair<string, int> resultData)
		{
			string contextString = $"These are the {resultData.Key}s in a radius of {resultData.Value} around " +
			 $"{Main.Bot._farmer.TilePoint} at {Main.Bot._currentLocation.DisplayName}:";
			
			contextString += TileContext.GetSpecifiedObjects(resultData.Key, Main.Bot._farmer.TilePoint,
				resultData.Value, Main.Bot._currentLocation);
			Context.Send(contextString,true);
			RegisterMainActions.RegisterPostAction();
		}
	}
}