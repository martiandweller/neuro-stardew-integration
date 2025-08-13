using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Network;
using Object = StardewValley.Object;

namespace NeuroStardewValley.Source.Utilities;

public class Utilities
{
	public static string FormatTimeString()
	{
		int intHours;
		if (Game1.timeOfDay >= 1200 && Game1.timeOfDay < 1300) // for 12pm
		{
			intHours = 12;
		}
		else
		{
			intHours = Game1.timeOfDay / 100 % 12;
		}
		int minutes = Game1.timeOfDay % 100;
		string padZeros = "";
		string hours = intHours.ToString();
		string text = "";
		if (Game1.timeOfDay % 100 == 0)
		{
			padZeros = padZeros.Insert(0,"0");
		}
		text = text.Insert(text.Length,hours);
		text = text.Insert(text.Length,":");
		text = text.Insert(text.Length, minutes.ToString());
		text = text.Insert(text.Length, padZeros);
		if (Game1.timeOfDay < 1200 || Game1.timeOfDay >= 2400)
		{
			text = text.Insert(text.Length, " AM");
		}
		else
		{
			text = text.Insert(text.Length, " PM");
		}

		return text;
	}

	public static string Format24HourString()
	{
		int intHours;
		if (Game1.timeOfDay >= 1000 && Game1.timeOfDay <= 2400)
		{
			intHours = Game1.timeOfDay / 100;
		}
		else if(Game1.timeOfDay > 2400)
		{
			intHours = Game1.timeOfDay - 2400;
			intHours /= 100;
		}
		else
		{
			intHours = Game1.timeOfDay / 100;
		}
		int minutes = Game1.timeOfDay % 100;
		string padZeros = "";
		string hours = intHours.ToString();
		string text = "";
		if (Game1.timeOfDay % 100 == 0)
		{
			padZeros = padZeros.Insert(0,"0");
		}
		text = text.Insert(text.Length,hours);
		text = text.Insert(text.Length,":");
		text = text.Insert(text.Length, minutes.ToString());
		text = text.Insert(text.Length, padZeros);

		return text;
	}

	public static string FormatBannerMessage(string message)
	{
		string formattedMessage = "";

		formattedMessage = message.Replace("\n", " ");

		return formattedMessage;
	}
	
	public static Dictionary<Point,Object> GetObjectsInLocation(Object obj)
	{
		GameLocation location = Game1.currentLocation;

		OverlaidDictionary objects = location.Objects;

		Dictionary<Point,Object> points = new();
		foreach (var dict in objects)
		{
			foreach (var kvp in dict.Where(kvp => kvp.Value.GetType() == obj.GetType()))
			{
				points.Add(kvp.Key.ToPoint(),kvp.Value);
			}
		}

		return points;
	}
}