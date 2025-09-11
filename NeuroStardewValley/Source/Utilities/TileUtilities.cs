using Microsoft.Xna.Framework;
using NeuroStardewValley.Debug;
using StardewBotFramework.Source.Modules.Pathfinding.Base;
using StardewValley;

namespace NeuroStardewValley.Source.Utilities;

public static class TileUtilities
{
	public static bool IsValidTile(Point tile, out string reason,bool destructive = false,bool collisionMap = true)
	{
		if (tile.X > Game1.currentLocation.Map.DisplayWidth / Game1.tileSize || tile.X < 0 ||
		    tile.Y > Game1.currentLocation.Map.DisplayWidth / Game1.tileSize || tile.Y < 0)
		{
			reason = $"The value was either less than 0 or greater than the size of the map";
			return false;
		}

		if (collisionMap)
		{
			Main.Bot.Pathfinding.BuildCollisionMap();
			if (Main.Bot.Pathfinding.IsBlocked(tile.X, tile.Y) && !destructive)
			{
				reason = "You gave a position that is blocked.";
				return false;
			}
		}
		reason = "";
		return true;
	}
	
	/// <summary>
	/// <see cref="Graph.IsInNeighbours"/>
	/// </summary>
	public static bool IsNeighbour(Point tile, Point neighbour,out int direction,int directions = 8)
	{
		bool result = Graph.IsInNeighbours(tile, neighbour, out var d,directions);
		direction = d;
		return result;
	}
}