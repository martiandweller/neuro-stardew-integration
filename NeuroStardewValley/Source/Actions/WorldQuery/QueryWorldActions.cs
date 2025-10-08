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
				["object_names"] = new()
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
			JArray? jArray = actionData.Data?.Value<JArray>("object_names");

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
			RegisterMainGameActions.RegisterPostAction();
		}
	}
 
	public class GetObjectTypeInRadius : NeuroAction<KeyValuePair<string,int>>
	{
		public override string Name => "get_object_type_in_range";
		protected override string Description => "Get only the specified type of object in range, certain names may not" +
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

			resultData = new(name, (int)radius);
			return ExecutionResult.Success("");
		}

		protected override void Execute(KeyValuePair<string, int> resultData)
		{
			var objects = TileContext.GetObjectsInLocation(Main.Bot._currentLocation, Main.Bot._farmer.TilePoint,
				resultData.Value);

			string contextString = $"These are the {resultData.Key}s in a radius of {resultData.Value} around " +
			                       $"{Main.Bot._farmer.TilePoint} at {Main.Bot._currentLocation.DisplayName}:";
			foreach (var kvp in objects)
			{
				string simpleName = TileContext.SimpleObjectName(kvp.Value);
				string? name = TileContext.GetObjectContext(kvp.Value, kvp.Key.X, kvp.Key.Y);
				if (name is null || simpleName != resultData.Key) continue;
				contextString +=$"\n{name}";
			}
			TileContext.SentFurniture.Clear();
			TileContext.SentBuildings.Clear();
			Context.Send(contextString,true);
			RegisterMainGameActions.RegisterPostAction();
		}
	}
}