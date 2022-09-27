using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Social.Models;
public class DstRules
{
	/// <summary>
	/// Gets an integer that uniquely identifies the DayLightShiftRule or sets the values of the instance based on it.
	/// </summary>

	public int RawValue
	{
		get
		{
			return (IsAbsolute ? (1 << 24) : 0)
				+ (Starts.RawValue << 12)
				+ Ends.RawValue;
		}
		set
		{
			IsAbsolute = (value >> 24) > 0;
			Starts.RawValue = (short)((value & 0x0000fff000) >> 12);
			Ends.RawValue = (short)(value & 0x0fff);
		}
	}

	/// <summary>
	/// Creates an instance of DaylightShift rules from a unique raw integer value.
	/// </summary>
	/// <param name="rawValue">The rawvalue of an instance of <see cref="DaylightShiftRules"/>. Given by 12 bits for Ends, 12 bits for Starts, and 1 bit for IsAbsolute from least to most significant.</param>

	public DstRules(int rawValue)
	{
		Starts = new DayInYear();
		Ends = new DayInYear();
		RawValue = rawValue;
	}

	/// <summary>
	/// Default constructor for Daylight Shift rules. Doesn't initialize values for Starts or Ends.
	/// </summary>

	public DstRules() { }

	/// <summary>
	/// Refers to a shift that happens in specific days if <see langword="true"/>. Otherwise uses the rules for nth instance of a weekday in a month.
	/// </summary>

	public bool IsAbsolute { get; set; }

	/// <summary>
	/// The absolute day in a year where DST starts.
	/// Otherwise, the "day" represents the weekday in modulo 7 and the ordinal position of the weekday by the quotient of the division by 7.
	/// </summary>

	public DayInYear Starts { get; set; }

	/// <summary>
	/// The absolute day in a year where DST ends. 
	/// Otherwise, the "day" represents the weekday in modulo 7 and the ordinal position of the weekday by the quotient of the division by 7.
	/// </summary>

	public DayInYear Ends { get; set; }

	/// <summary>
	/// Expresses the value of this object in a human-readable manner.
	/// </summary>
	/// <returns>A string detailing the meaning of the values of this object.</returns>

	public override string ToString()
	{
		if (Starts is null || Ends is null) return $"Undefined";
		return $"From the {Starts.ToString(IsAbsolute)} to the {Ends.ToString(IsAbsolute)}.";
	}

	/// <summary>
	/// Checks whether a certain day is supposed to be interpreted using Daylight Saving time for a given user.
	/// </summary>
	/// <param name="day">The Date Time containing the day to check for.</param>
	/// <returns><see langword="true"/> if the given <paramref name="day"/> is comprised in the DST range for this user, otherwise <see langword="false"/>.</returns>

	public bool IsDST(DateTimeOffset day)
	{
		return IsDST(DayInYear.FromDateTime(day), day.Year);
	}

	/// <summary>
	/// Checks whether a certain day is supposed to be interpreted using Daylight Saving time for a given user.
	/// </summary>
	/// <param name="day">The day of the year to check for.</param>
	/// <param name="year">The year to consider this measure for, used for weekday calculations.</param>
	/// <returns><see langword="true"/> if the given <paramref name="day"/> is comprised in the DST range for this user, otherwise <see langword="false"/>.</returns>

	public bool IsDST(DayInYear day, int year)
	{
		int monthmin = (int)Starts.Month;
		int monthmax = (int)Ends.Month;

		int month = (int)day.Month;

		if (monthmax < monthmin)
		{
			monthmax += 12;
			month += month < monthmin ? 12 : 0;
		}

		if (month > monthmin && month < monthmax)
		{
			return true;
		}
		else if (month == monthmin)
		{
			if (IsAbsolute)
				return day.Day > Starts.Day;

			return day.Day > Starts.GetThresholdDay(year);
		}
		else if (month == monthmax)
		{
			if (IsAbsolute)
				return day.Day <= Ends.Day;

			return day.Day <= Ends.GetThresholdDay(year);
		}

		return false;
	}
}
