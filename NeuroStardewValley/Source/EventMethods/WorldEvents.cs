using System.Collections;
using NeuroSDKCsharp.Messages.Outgoing;
using StardewBotFramework.Source.Events.World_Events;
using StardewBotFramework.Source.Modules.Pathfinding.Base;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using Object = StardewValley.Object;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace NeuroStardewValley.Source.EventMethods;

public static class WorldEvents
{
	public static void WorldObjectChanged(object? sender, BotObjectListChangedEventArgs e)
	{
		if (e.Added.Any())
		{
			using var enumerator = e.Added.GetEnumerator();
			HandleEnumeratorContext(enumerator,"{0} was added to this location.");
		}

		if (e.Removed.Any())
		{
			using var enumerator = e.Removed.GetEnumerator();
			HandleEnumeratorContext(enumerator,"{0} was removed from this location.");
		}
		
		// if (CharacterController.IsMoving()) CharacterController.ForceStopMoving();
	}

	public static void LocationFurnitureChanged(object? sender, BotFurnitureChangedEventArgs e)
	{
		if (e.Added.Any())
		{
			using var enumerator = e.Added.GetEnumerator();
			HandleEnumeratorContext(enumerator,"{0} was added to this location.");
		}

		if (e.Removed.Any())
		{
			using var enumerator = e.Removed.GetEnumerator();
			HandleEnumeratorContext(enumerator,"{0} was removed from this location.");
		}
	}
	
	public static void TerrainFeatureChanged(object? sender, BotTerrainFeatureChangedEventArgs e)
	{
		if (e.Added.Any())
		{
			using var enumerator = e.Added.GetEnumerator();
			HandleEnumeratorContext(enumerator,"{0} was added to this location.");
		}

		if (e.Removed.Any())
		{
			using var enumerator = e.Removed.GetEnumerator();
			HandleEnumeratorContext(enumerator,"{0} was removed from this location.");
		}
	}
	
	public static void LargeTerrainFeatureChanged(object? sender, BotLargeTerrainFeatureChangedEventArgs e)
	{
		if (e.Added.Any())
		{
			using var enumerator = e.Added.GetEnumerator();
			HandleEnumeratorContext(enumerator,"{0} was added to this location.");
		}

		if (e.Removed.Any())
		{
			using var enumerator = e.Removed.GetEnumerator();
			HandleEnumeratorContext(enumerator,"{0} was removed from this location.");
		}
	}

	private static void HandleEnumeratorContext(IEnumerator enumerator, string context)
	{
		while (enumerator.MoveNext())
		{
			string contextString = "";
			switch (enumerator.Current)
			{
				case KeyValuePair<Vector2,Object> kvp:
					contextString = String.Format(context, kvp.Value.Name);
					break;
				case Furniture furniture:
					contextString = String.Format(context, furniture.Name);
					break;
				case LargeTerrainFeature largeTerrainFeature:
					contextString = String.Format(context, largeTerrainFeature);
					break;
				case TerrainFeature terrainFeature:
					contextString = String.Format(context, terrainFeature);
					break;
			}
			
			Context.Send(contextString);
		}
	}
}