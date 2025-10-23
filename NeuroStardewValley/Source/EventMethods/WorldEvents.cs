using System.Collections;
using NeuroSDKCsharp.Messages.Outgoing;
using NeuroStardewValley.Source.ContextStrings;
using StardewBotFramework.Source.Events.World_Events;
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
			string contextString;
			switch (enumerator.Current)
			{
				case KeyValuePair<Vector2,Object> kvp:
					contextString = string.Format(context, kvp.Value.Name);
					break;
				case Furniture furniture:
					contextString = string.Format(context, furniture.DisplayName);
					break;
				case LargeTerrainFeature largeTerrainFeature:
					contextString = string.Format(context, TileContext.SimpleObjectName(largeTerrainFeature));
					break;
				case TerrainFeature terrainFeature:
					contextString = string.Format(context, TileContext.SimpleObjectName(terrainFeature));
					break;
				default:
					continue;
			}
			
			Context.Send(contextString,true);
		}
	}
}