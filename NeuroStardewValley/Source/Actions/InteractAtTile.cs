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
using StardewValley.GameData.Buildings;
using StardewValley.TerrainFeatures;
using Object = StardewValley.Object;

namespace NeuroStardewValley.Source.Actions;

// TODO: This is meant to be a combination of all "interact with" actions 
public class InteractAtTile : NeuroAction<Point>
{
	private static List<Point> ActionTiles => TileContext.ActionableTiles.ToList();
	public override string Name => "interact_at_tile";
	protected override string Description => "Interact with the object at the specified tile";
	protected override JsonSchema Schema => new()
	{
		Type = JsonSchemaType.Object,
		Required = new List<string> { "tile_x", "tile_y" },
		Properties = new Dictionary<string, JsonSchema>
		{
			["tile_x"] = QJS.Type(JsonSchemaType.Integer),
			["tile_y"] = QJS.Type(JsonSchemaType.Integer),
			["use_held_item"] = QJS.Type(JsonSchemaType.Boolean)
		}
	};
	protected override ExecutionResult Validate(ActionData actionData, out Point resultData)
	{
		int tileX = actionData.Data?.Value<int>("tile_x") ?? -1;
		int tileY = actionData.Data?.Value<int>("tile_y") ?? -1;
		bool useHeldItem = actionData.Data?.Value<bool>("use_held_item") ?? false;
		
		resultData = new();
		if (tileX is -1 || tileY is -1)
		{
			return ExecutionResult.Failure($"You have provided a null value for either tileX or TileY.");
		}

		Point point = new Point(tileX, tileY);
		object o = (Main.Bot._currentLocation, point);

		Building building = Main.Bot._currentLocation.buildings
			.FirstOrDefault(b => DoesBuildingContainTile(b,point),new Building());
		if (Main.Bot._currentLocation.buildings.Contains(building))
		{
			o = building;
		}
		bool result;
		string reason;
		
		switch (o)
		{
			case TerrainFeature:
			case Object:
				result = ObjectValidation(tileX,tileY,useHeldItem,out reason);
				break;
			case Building:
				Logger.Info($"interacting with building");
				result = BuildingValidation(tileX,tileY,out reason);
				break;
			default:
				reason = "There is no available object at this tile";
				result = false;
				break;
		}
		
		// need to check if object is within boundary of building and is actionable
		if ((ActionTiles.Any(tile => tile == point) || Main.Bot._currentLocation.isActionableTile(tileX, tileY, Main.Bot._farmer))
		    && o is not Building)
		{
			reason = $"Interacting with the tile at {tileX},{tileY}";
			result = true;
		}

		if (!result)
		{
			return ExecutionResult.Failure(reason);
		}
		
		resultData = point;
		return ExecutionResult.Success(reason);
	}
	private bool _useHeld;
	protected override void Execute(Point resultData)
	{
		object o = (Main.Bot._currentLocation, resultData);
		
		Building build = Main.Bot._currentLocation.buildings.FirstOrDefault(bu => DoesBuildingContainTile(bu,resultData)
			,new Building());
		if (Main.Bot._currentLocation.buildings.Contains(build))
		{
			o = build;
		}
		
		if (ActionTiles.Any(tile => tile != resultData) ||
			(o is Building b && !b.isActionableTile(resultData.X, resultData.Y, Main.Bot._farmer)))
		{
			o = resultData;
		}

		switch (o)
		{
			case TerrainFeature:
			case Object:
				ObjectExecution(resultData);
				break;
			case Building building:
				BuildingExecution(building, resultData);
				break;
			// action tile
			case Point point:
				Logger.Info($"interacting with action tile");
				Main.Bot.ActionTiles.DoActionTile(point);
				if (Game1.activeClickableMenu is null)
				{
					RegisterMainGameActions.RegisterPostAction();
				}
				break;
		}
	}

	private bool ObjectValidation(int tileX, int tileY, bool useHeld, out string reason)
	{
		if (useHeld && Main.Bot._farmer.ActiveItem is null)
		{
			reason = $"You are not holding anything so you cannot use the held item.";
			return false;
		}

		Point point = new Point(tileX, tileY);
		object? obj = TileUtilities.GetTileType(Main.Bot._currentLocation, point);
		if (obj is null or Building)
		{
			reason = $"There is no object valid at the tile you provided.";
			return false;
		}
			
		if (!RangeCheck.InRange(point))
		{
			reason = $"This object is not within range of you.";
			return false;
		}

		_useHeld = useHeld;
		reason = $"Interacting with the object at {point}";
		return true;
	}

	private void ObjectExecution(Point resultData)
	{
		object? o = (Main.Bot._currentLocation, resultData);
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

	private static bool BuildingValidation(int tileX, int tileY, out string reason)
	{
		Point point = new Point(tileX, tileY);
		Building building = Main.Bot._currentLocation.buildings.FirstOrDefault(b => DoesBuildingContainTile(b,point),new Building());
		if (!Main.Bot._currentLocation.buildings.Contains(building))
		{
			reason = $"There is no building at {tileX},{tileY}";
			return false;
		}

		if (building is null)
		{
			reason = $"There is no building at {tileX},{tileY}";
			return false;
		}
		
		BuildingActionTile? tile = building.GetData().ActionTiles.FirstOrDefault(tile => tile.Tile == point);
		if (tile is null)
		{
			reason = $"There is no action for {StringUtilities.TokenizeBuildingName(building)} at {tileX},{tileY}";
			return false;
		}
		
		reason = $"Interacting with {StringUtilities.TokenizeBuildingName(building)}";
		return true;
	}

	private static void BuildingExecution(Building building,Point tile)
	{
		Main.Bot.Building.DoBuildingAction(building, tile.ToVector2());
		if (Game1.activeClickableMenu is null)
		{
			RegisterMainGameActions.RegisterPostAction();
		}
	}

	private static bool DoesBuildingContainTile(Building building,Point tile)
	{
		Rectangle rect = new Rectangle(building.tileX.Value * 64, building.tileY.Value * 64,
			building.tilesWide.Value * 64, building.tilesHigh.Value * 64);
		return rect.Contains(tile.ToVector2() * 64);
	}
}