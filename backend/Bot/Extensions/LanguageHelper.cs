namespace Bot.Extensions;

public static class LanguageHelper
{
	private static readonly Dictionary<long, string> BasicUnits = new()
	{
		{ 1000000000000, "T" },
		{ 1000000000, "B" },
		{ 1000000, "M" },
		{ 1000, "K" }
	};

	public static readonly Dictionary<long, string> MetricPrefixes = new()
	{
		{ 1000000000000, "T" },
		{ 1000000000, "G" },
		{ 1000000, "M" },
		{ 1000, "K" }
	};

	public static readonly Dictionary<long, string> ByteUnits = new()
	{
		{ 1099511627776, "TB" },
		{ 1073741824, "GB" },
		{ 1048576, "MB" },
		{ 1024, "KB" }
	};

	public static string ToUnit(this long value, Dictionary<long, string> units = null)
	{
		units ??= BasicUnits;

		foreach (var kvp in units)
		{
			if (value >= kvp.Key)
			{
				return $"{(float)value / kvp.Key:G3}{kvp.Value}";
			}
		}
		return value.ToString();
	}

	public static string Ordinal(this int value)
	{
		if (value < 0)
		{
			if (value == -1) return "last";
			else return Ordinal(-value) + " to last";
		}

		var unit = value % 10;
		var tens = value % 100 - unit;
		if (tens == 10)
		{
			return value + "th";
		}

		return value + (unit switch
		{	
			1 => "st",
			2 => "nd",
			3 => "rd",
			_ => "th"
		});
	}
}
