﻿using System.ComponentModel.DataAnnotations;

namespace Bot.DTOs;

public class EmbedAppSettingsForPutDto
{
	[Required(ErrorMessage = "EmbedTitle field is required")]
	[MaxLength(256)]
	public string EmbedTitle { get; set; }
	[MaxLength(4096)]
	public string EmbedContent { get; set; }
}
