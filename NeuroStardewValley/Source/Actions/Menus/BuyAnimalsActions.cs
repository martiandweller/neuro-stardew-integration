using NeuroSDKCsharp.Actions;
using NeuroSDKCsharp.Json;
using NeuroSDKCsharp.Websocket;
using NeuroStardewValley.Source.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;
using Logger = NeuroStardewValley.Debug.Logger;

namespace NeuroStardewValley.Source.Actions.Menus;

public static class BuyAnimalsActions
{
	public class SelectAnimal : NeuroAction<ClickableTextureComponent>
	{
		public override string Name => "select_animal";
		protected override string Description => "Select the animal you want to buy.";
		protected override JsonSchema Schema => new()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "animal" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["animal"] = QJS.Enum(Main.Bot.AnimalMenu.GetAvailableButtons().Select(cc => cc.hoverText))
			}
		};
		protected override ExecutionResult Validate(ActionData actionData, out ClickableTextureComponent? resultData)
		{
			string? animalString = actionData.Data?.Value<string>("animal");

			resultData = null;
			if (animalString is null)
			{
				return ExecutionResult.Failure($"You gave a null value in animal");
			}

			int index = Main.Bot.AnimalMenu.GetAvailableButtons().Select(cc => cc.hoverText).ToList().IndexOf(animalString);
			if (index == -1)
			{
				return ExecutionResult.Failure($"The animal you gave is not a valid option.");
			}

			ClickableTextureComponent cc = Main.Bot.AnimalMenu.GetAvailableButtons()[index];
			if (cc.item.salePrice() > Main.Bot.PlayerInformation.Money)
			{
				return ExecutionResult.Failure($"You do not have enough money to buy this animal.");
			}

			resultData = cc;
			return ExecutionResult.Success();
		}

		protected override void Execute(ClickableTextureComponent? resultData)
		{
			if (resultData is null) return;
			
			Main.Bot.AnimalMenu.SelectAnimal(resultData);
			RegisterActions(3);
		}
	}

	public class ExitMenu : NeuroAction
	{
		public override string Name => "exit_menu";
		protected override string Description => "Exit the menu";
		protected override JsonSchema Schema => new();
		protected override ExecutionResult Validate(ActionData actionData)
		{
			return ExecutionResult.Success();
		}

		protected override void Execute()
		{
			if (Main.Bot.AnimalMenu.Menu is null) return;
			if (Main.Bot.AnimalMenu.Menu.onFarm)
			{
				Main.Bot.AnimalMenu.ExitFarmMenu();
				return;
			}

			Main.Bot.AnimalMenu.ExitStoreMenu();
		}
	}

	public class SelectBuilding : NeuroAction<Building>
	{
		public override string Name => "select_building";
		protected override string Description => "Select the building to put this animal in.";
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
			string? buildingString = actionData.Data?.Value<string>("building");

			resultData = null;
			if (buildingString is null)
			{
				return ExecutionResult.Failure($"building cannot be null");
			}

			int index = GetSchema().IndexOf(buildingString);
			if (index == -1)
			{
				return ExecutionResult.Failure($"The building you gave was not valid.");
			}

			Building building = Main.Bot.AnimalMenu.GetAvailableBuildings()[index];
			if (!Main.Bot.AnimalMenu.BuildingCheck(building, Main.Bot.AnimalMenu.Menu?.animalBeingPurchased!))
			{
				return ExecutionResult.Failure(string.Format(ResultStrings.ModVarFailure,"BuildingCheck"));
			}
			resultData = building;
			return ExecutionResult.Success($"You have selected a: {StringUtilities.GetBuildingName(building)}");
		}

		protected override void Execute(Building? resultData)
		{
			if (resultData is null) return;
			Main.Bot.AnimalMenu.SelectBuilding(resultData);
			RegisterActions();
		}

		private static List<string> GetSchema()
		{
			var enumerable = Main.Bot.AnimalMenu.GetAvailableBuildings();
			
			List<string> strings = new();
			using var enumerator = enumerable.GetEnumerator();
			while (enumerator.MoveNext())
			{
				strings.Add($"{enumerator.Current.tileX.Value},{enumerator.Current.tileY.Value} {StringUtilities.GetBuildingName(enumerator.Current)}");
			}
			
			return strings;
		}
	}

	public class NameAnimal : NeuroAction<string>
	{
		public override string Name => "name_animal";
		protected override string Description => "Select a name for this animal, you should try to avoid duplicates and long names.";
		protected override JsonSchema Schema => new()
		{
			Type = JsonSchemaType.Object,
			Required = new List<string> { "name" },
			Properties = new Dictionary<string, JsonSchema>
			{
				["name"] = new()
				{
					Type = JsonSchemaType.String,
					MaxLength = 20, // Does not have defined text limit as is based on font size :)
					MinLength = 1
				}
			}
		};
		protected override ExecutionResult Validate(ActionData actionData, out string? resultData)
		{
			string? nameString = actionData.Data?.Value<string>("name");

			resultData = null;
			if (string.IsNullOrEmpty(nameString))
			{
				return ExecutionResult.Failure($"If you want to name an animal you must provide a string value.");
			}

			if (!Main.Bot.AdheresToTextBoxLimit(nameString, Main.Bot.AnimalMenu.Menu?.textBox!))
			{
				return ExecutionResult.Failure($"{nameString} is too long, you should try a different name next time.");
			}

			if (Utility.areThereAnyOtherAnimalsWithThisName(nameString))
			{
				return ExecutionResult.Failure($"Another animal already has that name, you should try another name.");
			}

			resultData = nameString;
			return ExecutionResult.Success();
		}

		protected override void Execute(string? resultData)
		{
			if (string.IsNullOrEmpty(resultData)) return;
			Main.Bot.AnimalMenu.NameAnimal(resultData);
			RegisterActions();
		}
	}

	public class RandomName : NeuroAction
	{
		public override string Name => "randomise_name";
		protected override string Description => "Randomise the name of this animal, If you do not like the random name" +
		                                         " you will be able to change it.";
		protected override JsonSchema Schema => new();
		protected override ExecutionResult Validate(ActionData actionData)
		{
			return ExecutionResult.Success();
		}

		protected override void Execute()
		{
			Main.Bot.AnimalMenu.RandomName();
			RegisterActions();
		}
	}

	public class AcceptName : NeuroAction
	{
		public override string Name => "accept_name";
		protected override string Description => "Accept the currently proposed name of the animal";
		protected override JsonSchema Schema => new();
		protected override ExecutionResult Validate(ActionData actionData)
		{
			return ExecutionResult.Success($"You have selected the name: {Main.Bot.AnimalMenu.Menu?.textBox.Text}, for the {Main.Bot.AnimalMenu.Menu?.animalBeingPurchased.displayType}.");
		}

		protected override void Execute()
		{
			Main.Bot.AnimalMenu.ConfirmName();
		}
	}

	public static void RegisterActions(int waitTime = 0)
	{
		if (Main.Bot.AnimalMenu.Menu is null) return;
		ActionWindow window = ActionWindow.Create(Main.GameInstance);
		string stateString;
		string queryString;
		if (Main.Bot.AnimalMenu.Menu.onFarm && !Main.Bot.AnimalMenu.Menu.namingAnimal)
		{
			queryString = "You are selecting the building for the animal to be in.";
			stateString = $"These are the other animal in the valid buildings: {string.Join("\n",Main.Bot.AnimalMenu.GetAvailableBuildings().Select(FormatBuildingAnimals))}";
			window.AddAction(new SelectBuilding()).AddAction(new ExitMenu());
		}
		else if (Main.Bot.AnimalMenu.Menu.onFarm)
		{
			queryString = "You are selecting a name for the animal.";
			stateString = $"The animal's current name is: {Main.Bot.AnimalMenu.Menu.textBox.Text}, you can either change this or accept it.";
			window.AddAction(new NameAnimal()).AddAction(new RandomName()).AddAction(new AcceptName());
		}
		else
		{
			List<ClickableTextureComponent> cc = Main.Bot.AnimalMenu.GetAvailableButtons(); 
			queryString = "You are selecting an animal to buy";
			foreach (var c in cc)
			{ 
				Logger.Info($"c: {c.hoverText}  {c.item}");
			}
			stateString = $"You have {Main.Bot.PlayerInformation.Money} gold.";
			stateString += $"\n{string.Concat(cc.Select(c => $"{c.hoverText} price: {c.item.salePrice()}"))}";
			window.AddAction(new SelectAnimal()).AddAction(new ExitMenu());
		}

		window.SetForce(waitTime, queryString, stateString);
		window.Register();
	}

	private static string FormatBuildingAnimals(Building building)
	{
		string animals = $"-{building.tileX.Value},{building.tileY.Value} {StringUtilities.GetBuildingName(building)}";
		foreach (var dict in building.GetIndoors().animals)
		{
			animals += string.Join("\n--",dict.Select(kvp => kvp.Value.displayType));
		}

		return animals;
	}
}