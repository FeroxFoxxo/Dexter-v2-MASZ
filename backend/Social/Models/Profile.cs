using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Models;
public class Profile
{
	[Key]
	public ulong Id { get; set; }

	public string? Gender { get; set; }
	public string? Sexuality { get; set; }
	public string? SonaInfo { get; set; }
	public string? Nationality { get; set; }
	public string? Languages { get; set; }
	public string? MiscInfo { get; set; }
	public short? BirthdayValue { get; set; }
	public int? BirthYear { get; set; }
	public string? TimeZone { get; set; }
	public string? TimeZoneDST { get; set; }
	public int? DstRulesValue { get; set; }
	public UserPreferences Preferences { get; set; }

	[NotMapped]
	public DayInYear? Birthday
	{
		get
		{
			if (BirthdayValue is null || BirthdayValue == 0) return null;
			try
			{
				return DayInYear.FromRawValue(BirthdayValue ?? 0);
			}
			catch
			{
				return null;
			}
		}
		set
		{
			BirthdayValue = value?.RawValue ?? null;
		}
	}
}

[Flags]
public enum UserPreferences
{

}
