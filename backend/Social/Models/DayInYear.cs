using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bot.Extensions;

namespace Social.Models;
public class DayInYear
{
	public short RawValue
	{
		get
		{
			return (short)(Day + ((int)Month << 7));
		}
		set
		{
			Day = (byte)(value & 0x7f);
			Month = (byte)(value >> 7);
		}
	}

	public byte Day;
	public byte Month;

	public int RelativeWeekday { get 
		{
			var d = Day % 7;
			return d < 0 ? d + 7 : d;
		} 
	}

	public static DayInYear FromRawValue(short rawValue)
	{
		DayInYear d = new();
		d.RawValue = rawValue;
		return d;
	}

	public int WeekdayCount => Day / 7;

	public override string ToString()
	{
		return ToString(true);
	}

	public string ToString(bool isAbsolute = true)
	{
		if (isAbsolute) return $"{((int)Day).Ordinal()} of {Month}";
		else return $"{(WeekdayCount <= 0 ? (WeekdayCount-1).Ordinal() : WeekdayCount.Ordinal())} {WeekdayNames[RelativeWeekday]} of {Month}";
	}

	public static readonly string[] WeekdayNames = new string[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

	/// <summary>
	/// Gets the day of the month that this day of year represents in relative mode for a given year.
	/// </summary>
	/// <param name="year">The relevant year for the calculation.</param>
	/// <returns>A number representing the day of the month the nth weekday will fall on.</returns>

	public int GetThresholdDay(int year)
	{
		int monthStartWeekday = (int)(CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(new DateTime(year, Month, 1)) + 6) % 7;
		int weekdayDiff = (monthStartWeekday - RelativeWeekday) % 7;

		int thresholdDay;

		if (Day < 7)
		{
			//Last weekday of the month
			int maxDay = CultureInfo.InvariantCulture.Calendar.GetDaysInMonth(year, (int)Month);
			for (thresholdDay = weekdayDiff + 1; thresholdDay <= maxDay - 7; thresholdDay += 7) { }
			return thresholdDay;
		}
		else
		{
			return weekdayDiff + 7 * WeekdayCount + 1;
		}
	}

	/// <summary>
	/// Converts a given date into a dayInYear format.
	/// </summary>
	/// <param name="day">The Date object to obtain data from.</param>
	/// <returns>A DayInYear object that represents the day in the year held in <paramref name="day"/>.</returns>

	public static DayInYear FromDateTime(DateTimeOffset day)
	{
		return new DayInYear() { Day = (byte)day.Day, Month = (byte)day.Month };
	}
}
