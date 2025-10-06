using Microsoft.Xna.Framework;
using NeuroStardewValley.Debug;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Network;
using StardewValley.TokenizableStrings;
using Object = StardewValley.Object;

namespace NeuroStardewValley.Source.Utilities;

public class StringUtilities
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

	public static int TimeStringToInt(string time)
	{
		var split = time.Split(':');

		Logger.Info($"split length: {split.Length}   game time: {Game1.timeOfDay}  string: {Game1.getTimeOfDayString(Game1.timeOfDay)}");
		if (split.Length != 2) return -1;
		int hour = int.Parse(split[0]) * 100;
		int minute = int.Parse(split[1]);
		Logger.Info($"hour: {hour} minute: {minute}");
		// 2600 as automatically falls asleep at 02:00 am
		if (hour > 2600 || hour < 0 || minute > 60 || minute < 0) return -1;

		return hour + minute;
	}

	public static string FormatBannerMessage(string message)
	{
		string formattedMessage = "";

		formattedMessage = message.Replace("\n", "");

		return formattedMessage;
	}

	public static string FormatDailyQuest(string description)
	{
		string formattedMessage = "";

		formattedMessage = description;
		char lastChar = '#';
		int spaceRepeat = 0;
		foreach (var c in formattedMessage) // we do this to remove the large gaps in text
		{
			if (c == lastChar && c == ' ')
			{
				spaceRepeat++;
			}

			lastChar = c;
		}

		string str = "";
		for (int i = 0; i < spaceRepeat; i++)
		{
			str += " ";
		}

		if (str != "")
		{
			formattedMessage = formattedMessage.Replace(str, "");
		}

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

	public static string FormatItemString(string itemString)
	{
		itemString = itemString.Replace("\n", " ");
		return itemString;
	}
	
	public static string TokenizeBuildingName(Building building)
	{
		return TokenParser.ParseText(building.GetData().Name);
	}
}