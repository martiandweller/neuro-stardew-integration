using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Websocket;
using NeuroStardewValley.Source.RegisterActions;
using NeuroStardewValley.Source.Utilities;
using StardewValley;
using StardewValley.GameData.Buildings;
using StardewValley.Menus;
using Rectangle = xTile.Dimensions.Rectangle;

namespace NeuroStardewValley.Source.Actions;

public static class CarpenterActions
{
	public class ChangeBuildingBlueprint : NeuroAction<CarpenterMenu.BlueprintEntry>
	{
		public override string Name => "change_building";
		protected override string Description => "Change the currently selected building";

		protected override JsonSchema? Schema => new()
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
			return ExecutionResult.Success();
		}

		protected override void Execute()
		{
			Main.Bot.FarmBuilding.InteractWithButton(Main.Bot.FarmBuilding._carpenterMenu!.upgradeIcon);
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
				if (skin.DisplayName == selectedSkin)
				{
					resultData = skin;
				}
			}
			return ExecutionResult.Success($"using the skin: {selectedSkin}");
		}

		protected override void Execute(BuildingSkinMenu.SkinEntry? resultData)
		{
			Main.Bot.FarmBuilding.InteractWithButton(Main.Bot.FarmBuilding._carpenterMenu!.appearanceButton);
			// Main.Bot.FarmBuilding.SetBuildingUI((Game1.activeClickableMenu as BuildingSkinMenu)!);
			Main.Bot.FarmBuilding.ChangeSkin(resultData!);
			RegisterStoreActions.RegisterCarpenterActions();
		}

		private static List<string> GetSchema()
		{
			List<string> strings = new();
			foreach (var skin in Main.Bot.FarmBuilding.GetBuildingSkins())
			{
				strings.Add(skin.DisplayName);
			}

			return strings;
		}
	}
}

public static class PlaceBuildingActions
{
	public class PlaceBuilding : NeuroAction<KeyValuePair<int,int>>
	{
		public override string Name => "place_building";
		protected override string Description => "Place building at the specified location.";
		protected override JsonSchema? Schema => new()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "tile_x", "tile_y" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["tile_x"] = QJS.Type(JsonSchemaType.Integer),
				["tile_y"] = QJS.Type(JsonSchemaType.Integer)
			}
		};
		protected override ExecutionResult Validate(ActionData actionData, out KeyValuePair<int, int> resultData)
		{
			int? tileX = actionData.Data?.Value<int>("tile_x");
			int? tileY = actionData.Data?.Value<int>("tile_y");

			if (tileX is null || tileY is null)
			{
				resultData = new KeyValuePair<int, int>();
				return ExecutionResult.Failure($"You have provided a null value in tile_x or tile_y");
			}

			int x = (int)tileX;
			int y = (int)tileY;

			Game1.oldMouseState = new MouseState((x * Game1.tileSize) - Game1.viewport.X,(y * Game1.tileSize) - Game1.viewport.Y, 0, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
			
			Game1.viewport = new Rectangle(x * Game1.tileSize, y * Game1.tileSize, 1920, 1080);
			bool canBuild = Main.Bot.FarmBuilding._carpenterMenu!.tryToBuild();
			if (!canBuild)
			{
				resultData = new();
				return ExecutionResult.Failure($"You cannot build at {x},{y}.");
			}

			resultData = new KeyValuePair<int, int>(x,y);
			return ExecutionResult.Success();
		}

		protected override void Execute(KeyValuePair<int, int> resultData)
		{
			Main.Bot.FarmBuilding.CreateBuilding(new Point(resultData.Key,resultData.Value));
		}
	}

	public static void RegisterPlaceBuilding()
	{
		ActionWindow window = ActionWindow.Create(Main.GameInstance);
		window.AddAction(new PlaceBuilding());
		window.SetForce(0, "", "");
		window.Register();
	}
}