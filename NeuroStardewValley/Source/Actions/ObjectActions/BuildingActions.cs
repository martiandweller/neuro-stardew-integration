using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Websocket;
using NeuroStardewValley.Source.RegisterActions;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.Buildings;
using StardewValley.TokenizableStrings;

namespace NeuroStardewValley.Source.Actions.ObjectActions;

public static class BuildingActions
{
	public class InteractWithBuilding : NeuroAction<Building>
	{
		public override string Name => "interact_with_building";
		protected override string Description => "This will interact with the building specified.";

		protected override JsonSchema Schema => new()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "building" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["building"] = QJS.Enum(GetLocationsBuildings().Select(building => TokenParser.ParseText(building.GetData().Name)))
			}
		};
		protected override ExecutionResult Validate(ActionData actionData, out Building? resultData)
		{
			string? buildingString = actionData.Data?.Value<string>("building");

			resultData = null;
			if (string.IsNullOrEmpty(buildingString))
			{
				return ExecutionResult.Failure($"Building was null or empty");
			}

			if (!GetLocationsBuildings().Select(buildingData => TokenParser.ParseText(buildingData.GetData().Name))
				    .Contains(buildingString))
			{
				return ExecutionResult.Failure($"Building you gave is not valid.");
			}
			
			int index = GetLocationsBuildings().Select(buildingData => TokenParser.ParseText(buildingData.GetData().Name)).ToList().IndexOf(buildingString);
			Building building = GetLocationsBuildings()[index];

			if (GetBuildingsActionTiles(new List<Building> { building })[building].Count == 0)
			{
				return ExecutionResult.Failure($"You cannot select this building, as it has no action tiles.");
			}
				
			resultData = building;
			return ExecutionResult.Success(buildingString);
		}

		protected override void Execute(Building? resultData)
		{
			if (resultData is null) return;
			
			ActionWindow actionWindow = ActionWindow.Create(Main.GameInstance);
			actionWindow.AddAction(new BuildingAction(resultData));
			actionWindow.SetForce(0, $"You are interacting with a building.",
				$"Select an action to do with the selected building");
			actionWindow.Register();
		}
	}

	public class BuildingAction : NeuroAction<BuildingActionTile>
	{
		private readonly Building _building;

		public BuildingAction(Building building)
		{
			_building = building;
		}
		
		public override string Name => "building_action";
		protected override string Description => $"Select an action to use, these are on the building you selected earlier";
		protected override JsonSchema Schema => new()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "action" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["action"] = QJS.Enum(GetBuildingTiles(GetBuildingsActionTiles(new() { _building })))
			}
		};
		protected override ExecutionResult Validate(ActionData actionData, out BuildingActionTile? resultData)
		{
			string? action = actionData.Data?.Value<string>("action");

			resultData = null;
			if (string.IsNullOrEmpty(action))
			{
				return ExecutionResult.Failure($"action was null or empty");
			}

			if (!GetBuildingTiles(GetBuildingsActionTiles(new() { _building })).Contains(action))
			{
				return ExecutionResult.Failure($"You gave an action that is not an option.");
			}

			int index = GetBuildingTiles(GetBuildingsActionTiles(new() { _building })).IndexOf(action);
			BuildingActionTile tile = GetBuildingsActionTiles(new() { _building })[_building][index];
			resultData = tile;
			return ExecutionResult.Success();
		}

		protected override void Execute(BuildingActionTile? resultData)
		{
			if (resultData is null) return;

			Main.Bot.Building.DoBuildingAction(_building, resultData.Tile.ToVector2()); //TODO: this doesn't work might need to change framework
			RegisterMainGameActions.RegisterPostAction();
		}

		private static List<string> GetBuildingTiles(Dictionary<Building, List<BuildingActionTile>> dictionary)
		{
			List<string> buildingsStrings = new();
			foreach (var kvp in dictionary)
			{
				foreach (var tile in kvp.Value)
				{
					string buildingString = "";
					buildingString = string.Concat(buildingString,$"Action: {TokenParser.ParseText(tile.Action)} Tile: {tile.Tile}");
					buildingsStrings.Add(buildingString);
				}
			}

			return buildingsStrings;
		}
	}

	private static List<Building> GetLocationsBuildings()
	{
		return Game1.currentLocation.buildings.ToList();
	}

	private static Dictionary<Building, List<BuildingActionTile>> GetBuildingsActionTiles(List<Building> buildings)
	{
		Dictionary<Building, List<BuildingActionTile>> result = new();
		foreach (var building in buildings)
		{
			result.Add(building,building.GetData().ActionTiles);
		}

		return result;
	}
}