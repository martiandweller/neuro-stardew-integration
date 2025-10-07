using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Messages.Outgoing;
using NeuroSDKCsharp.Websocket;
using NeuroStardewValley.Debug;
using NeuroStardewValley.Source.ContextStrings;
using NeuroStardewValley.Source.RegisterActions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NeuroStardewValley.Source.Actions.WorldQuery;

public class QueryWorldActions
{
	public class GetObjectsInRadius : NeuroAction<int>
	{
		const int MinRadius = 3;
		const int MaxRadius = 30;
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
			string contextString = $"These are the objects in a radius of {resultData} at {Main.Bot._farmer.TilePoint}";
			
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
}