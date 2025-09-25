using Microsoft.Xna.Framework;
using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Websocket;
using NeuroStardewValley.Debug;
using NeuroStardewValley.Source.ContextStrings;
using NeuroStardewValley.Source.RegisterActions;
using NeuroStardewValley.Source.Utilities;
using StardewBotFramework.Source.Modules.Pathfinding.Base;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;
using xTile.Dimensions;
using Object = StardewValley.Object;

namespace NeuroStardewValley.Source.Actions.ObjectActions;

public static class WorldObjectActions
{
	public class PlaceObjects : NeuroAction<KeyValuePair<Object, int>>
	{
		public override string Name => "place_objects";
		protected override string Description => "place objects in a radius around yourself, as an example, this can be used to repopulate farmland with seeds.";
		protected override JsonSchema Schema => new()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "object","radius" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["object"] = QJS.Enum(Main.Bot.Inventory.Inventory.Where(item => item is not null && item.isPlaceable()).Select(item => item.Name)),
				["radius"] = QJS.Type(JsonSchemaType.Integer)
			}
		};
		protected override ExecutionResult Validate(ActionData actionData, out KeyValuePair<Object, int> resultData)
		{
			string? objString = actionData.Data?.Value<string>("object");
			int? radius = actionData.Data?.Value<int>("radius");

			resultData = new();
			if (objString is null || radius is null)
			{
				return ExecutionResult.Failure($"");
			}
			
			if (radius > 30 || radius < 2) return ExecutionResult.Failure($"The radius can only be between 30 and 2.");
			
			int index = Main.Bot.Inventory.Inventory.Where(item => item is not null && item.isPlaceable())
				.Select(item => item.Name).ToList().IndexOf(objString);
			if (index == -1) return ExecutionResult.Failure($"The object you specified does not exist in your inventory.");
			
			Object obj = (Object)Main.Bot.Inventory.Inventory.Where(item => item is not null && item.isPlaceable()).ToList()[index];

			if (obj is null) return ExecutionResult.Failure($"The object you specified does not exist.");
			
			if (obj.Name != objString) return ExecutionResult.Failure($"The object you specified does not exist in your inventory.");
			
			// This one should not happen as we only send placeable items
			if (!obj.isPlaceable()) return ExecutionResult.Failure($"The object you specified is not placeable.");
			
			// TODO: check top of later fixes for reason for this
			if (!obj.isPassable()) return ExecutionResult.Failure($"This object cannot be placed as it is not passable.");
			
			resultData = new(obj,(int)radius);
			return ExecutionResult.Success();
		}

		protected override void Execute(KeyValuePair<Object, int> resultData)
		{
			Task.Run(async () =>
			{
				await Main.Bot.Tool.PlaceObjectsInRadius(Main.Bot._farmer.TilePoint, resultData.Key, resultData.Value);
				RegisterMainGameActions.RegisterPostAction();
			});
		}
	}
	
	public class InteractWithObject : NeuroAction<Point>
	{
		private bool _useHeld;
		public override string Name => "interact_object";
		protected override string Description =>
			"Will interact with an object, This should primarily be used with furniture or harvesting plants. This will also allow you to add" +
			" items to the object, this can be used with objects like furnaces and the various types of \"machines\"." +
			" If use_held_item is true it may priorities certain actions e.g. filling a machine or placing a tapper on a tree.";
		protected override JsonSchema Schema => new()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "object_x","object_y" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["object_x"] = QJS.Type(JsonSchemaType.Integer),
				["object_y"] = QJS.Type(JsonSchemaType.Integer),
				["use_held_item"] = QJS.Type(JsonSchemaType.Boolean)
			}
		};
		protected override ExecutionResult Validate(ActionData actionData, out Point resultData)
		{
			int? objX = actionData.Data?.Value<int>("object_x");
			int? objY = actionData.Data?.Value<int>("object_y");
			bool? useHeld = actionData.Data?.Value<bool>("use_held_item");
			
			resultData = new();
			if (objX is null || objY is null || useHeld is null)
			{
				return ExecutionResult.Failure($"You have provided a null value.");
			}

			if ((bool)useHeld && Main.Bot._farmer.ActiveItem is null)
			{
				return ExecutionResult.Failure($"You are not holding anything so you cannot use the held item.");
			}

			Point point = new Point((int)objX, (int)objY);
			object? obj = TileContext.GetTileType(Main.Bot._currentLocation, point);
			if (obj is null or Building)
			{
				return ExecutionResult.Failure($"There is no object valid at the tile you provided.");
			}
			
			if (!RangeCheck.InRange(point))
			{
				return ExecutionResult.Failure($"This object is not within range of you.");
			}

			_useHeld = (bool)useHeld;
			resultData = point;
			return ExecutionResult.Success();
		}

		protected override void Execute(Point resultData)
		{
			object? o = TileContext.GetTileType(Main.Bot._currentLocation, resultData);
			if (o is null or Building)
			{
				return;
			}

			Logger.Info($"interacting with {resultData}   {o}");
			switch (o)
			{
				case Object obj:
					if (obj.GetMachineData() is not null && (obj.heldObject.Value is null || 
						(obj.GetMachineData().AllowLoadWhenFull && obj.heldObject.Value is not null)) && _useHeld)
					{
						Main.Bot.Player.AddItemToObject(obj, Main.Bot._farmer.ActiveItem);
						break;
					}

					Main.Bot.ObjectInteraction.InteractWithObject(obj);
					break;
				case TerrainFeature feature:
					if (_useHeld)
					{
						Graph.IsInNeighbours(Main.Bot._farmer.TilePoint, resultData, out var direction,4);
						Main.Bot.Tool.UseTool(direction);
					}
					Main.Bot.ObjectInteraction.InteractWithTerrainFeature(feature, resultData.ToVector2());
					break;
				default:
					Logger.Error($"InteractWithObject execute result data was not a valid class");
					break;
			}
			
			RegisterMainGameActions.RegisterPostAction();
		}
	}

	public class InteractWithActionTile : NeuroAction<Point>
	{
		private static List<Point> ActionTiles => TileContext.ActionableTiles.ToList();
		public override string Name => "interact_with_tile";
		protected override string Description => "Interact with a tile that has an action on it.";
		protected override JsonSchema Schema => new()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "tile" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["tile"] = QJS.Enum(ActionTiles.Select(tile => tile.ToString()).ToList())
			}
		};
		protected override ExecutionResult Validate(ActionData actionData, out Point resultData)
		{
			string? tileString = actionData.Data?.Value<string>("tile");

			resultData = new();
			if (string.IsNullOrEmpty(tileString))
			{
				return ExecutionResult.Failure($"You provided a null or empty value to tile");
			}

			Point tile = ActionTiles[ActionTiles.Select(tile => tile.ToString()).ToList().IndexOf(tileString)];

			if (!ActionTiles.Contains(tile) || !Main.Bot._currentLocation.isActionableTile(tile.X, tile.Y, Main.Bot._farmer))
			{
				return ExecutionResult.Failure($"The tile you provided is not a valid tile, you should try another."); 
			}

			if (!Utility.tileWithinRadiusOfPlayer(tile.X, tile.Y, 1, Main.Bot._farmer))
			{
				return ExecutionResult.Failure($"You are not neighbouring the tile you selected, you should try to get closer.");
			}
			
			resultData = tile;
			return ExecutionResult.Success($"You are interacting with {tile}");
		}

		protected override void Execute(Point resultData)
		{
			Main.Bot.ActionTiles.DoActionTile(resultData);
			RegisterMainGameActions.RegisterPostAction();
		}
	}

	public class InteractWithTile : NeuroAction<Point>
	{
		public override string Name => "interact_with_tile";
		protected override string Description =>
			"Interact with a specified tile that is different from normal tiles, this would commonly be used for troughs" +
			". in animal houses or interacting with the ladders in the mines";
		protected override JsonSchema Schema => new()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "tile" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["tile"] = QJS.Enum(GetSchema().Select(point => point.ToString()).ToList())
			}
		};
		protected override ExecutionResult Validate(ActionData actionData, out Point resultData)
		{
			string? point = actionData.Data?.Value<string>("tile");

			resultData = new();
			if (string.IsNullOrEmpty(point))
			{
				return ExecutionResult.Failure($"The tile you provided was not valid.");
			}

			int index = GetSchema().Select(p => p.ToString()).ToList().IndexOf(point);

			if (index == -1)
			{
				return ExecutionResult.Failure($"You provided an invalid value.");
			}
			
			if (!RangeCheck.InRange(GetSchema()[index]))
			{
				return ExecutionResult.Failure($"This tile is not within range of you, you should try to walk up to it.");
			}
			resultData = GetSchema()[index];
			return ExecutionResult.Success();
		}

		protected override void Execute(Point resultData)
		{
			// currently just stops from sending if mine ladder
			bool registerAction = Main.Bot._currentLocation.getTileIndexAt(resultData.X, resultData.Y, "Buildings") != 173;
			Main.Bot._currentLocation.checkAction(new Location(resultData.X,resultData.Y),Game1.viewport,Main.Bot._farmer);
			if (registerAction) RegisterMainGameActions.RegisterPostAction();
		}

		public static List<Point> GetSchema()
		{
			List<Point> tiles = new();

			switch (Main.Bot._currentLocation)
			{
				case AnimalHouse animalHouse:
					for (int x = 0; x < Main.Bot._currentLocation.Map.DisplayWidth / 64; x++)
					{
						for (int y = 0; y < Main.Bot._currentLocation.Map.DisplayHeight / 64; y++)
						{
							if (animalHouse.doesTileHaveProperty(x, y, "Trough", "Back") == null ||
							    Main.Bot._currentLocation.Objects.ContainsKey(new Vector2(x, y))) continue;

							tiles.Add(new Point(x, y));
						}
					}

					break;
				case MineShaft mineShaft:
					for (int x = 0; x < mineShaft.Map.DisplayWidth / 64; x++)
					{
						for (int y = 0; y < mineShaft.Map.DisplayHeight / 64; y++)
						{
							if (mineShaft.getTileIndexAt(x,y, "Buildings") != 173)
								continue; // tile index for ladders

							tiles.Add(new Point(x, y));
						}
					}

					break;
			}
			
			return tiles;
		}
	}
}