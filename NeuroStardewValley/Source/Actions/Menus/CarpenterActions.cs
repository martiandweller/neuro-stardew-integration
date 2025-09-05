using Microsoft.Xna.Framework;
using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Websocket;
using NeuroStardewValley.Source.RegisterActions;
using NeuroStardewValley.Source.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.Buildings;
using StardewValley.Menus;
using StardewValley.TokenizableStrings;
using Logger = NeuroStardewValley.Debug.Logger;

namespace NeuroStardewValley.Source.Actions.Menus;

public static class CarpenterActions
{
	public class ChangeBuildingBlueprint : NeuroAction<CarpenterMenu.BlueprintEntry>
	{
		public override string Name => "change_building";
		protected override string Description => "Change the currently selected building";

		protected override JsonSchema Schema => new()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "building" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["building"] = QJS.Enum(GetSchema())
			}
		};
		protected override ExecutionResult Validate(ActionData actionData, out CarpenterMenu.BlueprintEntry? resultData)
		{
			string? blueprintEntry =
				actionData.Data?.Value<string>("building");

			resultData = new CarpenterMenu.BlueprintEntry(0,"",new BuildingData(),"");
			if (blueprintEntry is null)
			{
				return ExecutionResult.Failure($"You provided a null value");
			}

			if (!GetSchema().Contains(blueprintEntry))
			{
				return ExecutionResult.Failure($"You gave an invalid value");
			}

			for (int i = 0; i < GetSchema().Count(); i++)
			{
				if (Main.Bot.FarmBuilding._carpenterMenu!.Blueprints[i].DisplayName == blueprintEntry)
				{
					resultData = Main.Bot.FarmBuilding._carpenterMenu.Blueprints[i];
				}
			}
			return ExecutionResult.Success($"You have selected {resultData.DisplayName}");
		}

		protected override void Execute(CarpenterMenu.BlueprintEntry? resultData)
		{
			Main.Bot.FarmBuilding.ChangeBuilding(resultData!);
			RegisterStoreActions.RegisterCarpenterActions();
		}

		private static IEnumerable<string> GetSchema()
		{
			List<string> nameList = new();
			if (Main.Bot.FarmBuilding._carpenterMenu is null) return nameList;
			foreach (var blueprint in Main.Bot.FarmBuilding._carpenterMenu.Blueprints)
			{
				nameList.Add(blueprint.DisplayName);
			}

			return nameList;
		}
	}
	
	public class CreateBuilding : NeuroAction
	{
		public override string Name => "create_building";
		protected override string Description => "This will move you to creating the building";
		protected override JsonSchema Schema => new();
		protected override ExecutionResult Validate(ActionData actionData)
		{
			if (Main.Bot.FarmBuilding._carpenterMenu is null)
			{
				return ExecutionResult.ModFailure(string.Format(ResultStrings.ModVarFailure,Main.Bot.FarmBuilding._carpenterMenu));
			}
			
			if (Main.Bot.FarmBuilding._carpenterMenu.CanBuildCurrentBlueprint())
			{
				return ExecutionResult.Success($"building {Main.Bot.FarmBuilding.BlueprintEntry.DisplayName}");
			}
			return ExecutionResult.Failure($"You cannot build this blueprint.");
		}

		protected override void Execute()
		{
			Main.Bot.FarmBuilding.InteractWithButton(Main.Bot.FarmBuilding._carpenterMenu!.okButton);
			PlaceBuildingActions.RegisterPlaceBuilding();
		}
	}

	public class DemolishBuilding : NeuroAction
	{
		public override string Name => "demolish_building";
		protected override string Description => "Demolish the building of this type that is on your farm";
		protected override JsonSchema Schema => new();
		protected override ExecutionResult Validate(ActionData actionData)
		{
			if (Main.Bot.FarmBuilding._carpenterMenu is null)
			{
				return ExecutionResult.ModFailure(string.Format(ResultStrings.ModVarFailure,Main.Bot.FarmBuilding._carpenterMenu));
			}
				
			if (Main.Bot.FarmBuilding._carpenterMenu.CanDemolishThis())
			{
				return ExecutionResult.Success();
			}
			return ExecutionResult.Failure($"You cannot destroy this building");
		}

		protected override void Execute()
		{
			Main.Bot.FarmBuilding.InteractWithButton(Main.Bot.FarmBuilding._carpenterMenu!.demolishButton);
		}
	}

	public class UpgradeBuilding : NeuroAction
	{
		public override string Name => "upgrade_building";
		protected override string Description => "Upgrade the currently selected building.";
		protected override JsonSchema Schema => new();
		protected override ExecutionResult Validate(ActionData actionData)
		{
			if (!Main.Bot.FarmBuilding._carpenterMenu!.CanBuildCurrentBlueprint())
			{
				return ExecutionResult.Failure($"You cannot build the: {Main.Bot.FarmBuilding.BlueprintEntry.DisplayName}");
			}
			return ExecutionResult.Success();
		}

		protected override void Execute()
		{
			Main.Bot.FarmBuilding.InteractWithButton(Main.Bot.FarmBuilding._carpenterMenu!.okButton);
			PlaceBuildingActions.RegisterPlaceBuilding(true);
		}
	}

	public class ChangeBuildingSkin : NeuroAction<BuildingSkinMenu.SkinEntry>
	{
		public override string Name => "change_building_look";
		protected override string Description => "Change how this building looks.";
		protected override JsonSchema? Schema => new()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "skin" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["skin"] = QJS.Enum(GetSchema())
			}
		};
		protected override ExecutionResult Validate(ActionData actionData, out BuildingSkinMenu.SkinEntry? resultData)
		{
			string? selectedSkin = actionData.Data?.Value<string>("skin");

			resultData = null;
			if (selectedSkin is null)
			{
				return ExecutionResult.Failure($"You gave a null value for skin");
			}

			if (!GetSchema().Contains(selectedSkin))
			{
				return ExecutionResult.Failure($"The skin you provided is not a valid option");
			}
			
			foreach (var skin in Main.Bot.FarmBuilding.GetBuildingSkins())
			{
				if (skin.Index.ToString() == selectedSkin)
				{
					resultData = skin;
				}
			}
			return ExecutionResult.Success($"using the skin: {selectedSkin}");
		}

		protected override void Execute(BuildingSkinMenu.SkinEntry? resultData)
		{
			Main.Bot.FarmBuilding.InteractWithButton(Main.Bot.FarmBuilding._carpenterMenu!.appearanceButton);
			Main.Bot.FarmBuilding.ChangeSkin(resultData!);
			RegisterStoreActions.RegisterCarpenterActions();
		}

		private static List<string> GetSchema()
		{
			List<string> strings = new();
			foreach (var skin in Main.Bot.FarmBuilding.GetBuildingSkins())
			{
				strings.Add($"{skin.Index}");
			}

			return strings;
		}
	}
}

public static class PlaceBuildingActions
{
	private class PlaceBuilding : NeuroAction<Point>
	{
		public override string Name => "place_building";
		protected override string Description => "Place building at the specified location.";
		protected override JsonSchema Schema => new()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "tile_x", "tile_y" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["tile_x"] = QJS.Type(JsonSchemaType.Integer),
				["tile_y"] = QJS.Type(JsonSchemaType.Integer)
			}
		};
		protected override ExecutionResult Validate(ActionData actionData, out Point resultData)
		{
			int? tileX = actionData.Data?.Value<int>("tile_x");
			int? tileY = actionData.Data?.Value<int>("tile_y");

			if (tileX is null || tileY is null)
			{
				resultData = new();
				return ExecutionResult.Failure($"You have provided a null value in tile_x or tile_y");
			}

			int x = (int)tileX;
			int y = (int)tileY;

			bool canBuild = Main.Bot.FarmBuilding.CreateBuilding(new Point(x,y)); // one day maybe replicate how it checks if you can build so this isn't in validate. A bit lazy for that rn.
			if (!canBuild)
			{
				resultData = new();
				return ExecutionResult.Failure($"You cannot build at {x},{y}.");
			}

			resultData = new (x,y);
			return ExecutionResult.Success();
		}

		protected override void Execute(Point resultData) // This is run in validation
		{
			Logger.Info($"result data: {resultData}");
		}
	}

	private class SelectBuilding : NeuroAction<Building>
	{
		public override string Name => "select_building";
		protected override string Description => "Select a building to either upgrade,destroy or move.";
		protected override JsonSchema Schema => new()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "building" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["building"] = QJS.Enum(GetSchema())
			}
		};
		protected override ExecutionResult Validate(ActionData actionData, out Building? resultData)
		{
			string? buildingStr = actionData.Data?.Value<string>("building");

			if (buildingStr is null)
			{
				resultData = null;
				return ExecutionResult.Failure($"You have provided a null value in building");
			}

			Building? building = GetBuilding(buildingStr);
			if (!GetSchema().Contains(buildingStr) || building is null)
			{
				resultData = null;
				return ExecutionResult.Failure($"You have provided an invalid value in building");
			}
			
			if (building.buildingType.Value == Main.Bot.FarmBuilding.BlueprintEntry.UpgradeFrom)
			{
				resultData = building;
				return ExecutionResult.Success($"selected: {StringUtilities.TokenizeBuildingName(building)}"); 
			}

			resultData = null;
			return ExecutionResult.Failure($"You have provided a tile that does not have a valid building in it.");
		}

		protected override void Execute(Building? resultData)
		{
			if (resultData is null) return;
			Main.Bot.FarmBuilding.SelectBuilding(resultData);
		}

		private static List<string> GetSchema()
		{
			if (Main.Bot.FarmBuilding._carpenterMenu is null) return new();
			List<string> names = new();
			List<Building> buildings;
			if (Main.Bot.FarmBuilding._carpenterMenu.Action == CarpenterMenu.CarpentryAction.Upgrade)
			{
				buildings = Game1.currentLocation.buildings.Where(building =>
					building.buildingType.Value == Main.Bot.FarmBuilding._carpenterMenu.Blueprint.UpgradeFrom).ToList();
			}
			else
			{
				buildings = Game1.currentLocation.buildings.ToList();
			}
			foreach (var building in buildings)
			{
				names.Add($"{StringUtilities.TokenizeBuildingName(building)}  pos: {building.tileX.Value},{building.tileY.Value}");
			}

			return names;
		}

		private static Building? GetBuilding(string str)
		{
			foreach (var building in Game1.currentLocation.buildings)
			{
				if ($"{StringUtilities.TokenizeBuildingName(building)}  pos: {building.tileX.Value},{building.tileY.Value}" == str)
				{
					return building;
				}
			}

			return null;
		}
	}
	
	private class CancelPlacingBuilding : NeuroAction
	{
		public override string Name => "cancel_building";
		protected override string Description => "Cancel placing the current building.";
		protected override JsonSchema Schema => new();
		protected override ExecutionResult Validate(ActionData actionData)
		{
			return ExecutionResult.Success();
		}

		protected override void Execute()
		{
			Main.Bot.FarmBuilding.InteractWithButton(Main.Bot.FarmBuilding._carpenterMenu!.cancelButton);
			RegisterStoreActions.RegisterCarpenterActions();
		}
	}

	public static void RegisterPlaceBuilding(bool upgrade = false)
	{
		// This stops from running SelectBuilding schema too early leading to no buildings
		while (Game1.IsFading() || Main.Bot.FarmBuilding._carpenterMenu?.Action == CarpenterMenu.CarpentryAction.None)
		{
			continue;
		}
		ActionWindow window = ActionWindow.Create(Main.GameInstance);
		if (upgrade)
		{
			window.AddAction(new SelectBuilding());
		}
		else
		{
			window.AddAction(new PlaceBuilding());
		}
		window.AddAction(new CancelPlacingBuilding());
		window.SetForce(5, "", "");
		window.Register();
	}
}