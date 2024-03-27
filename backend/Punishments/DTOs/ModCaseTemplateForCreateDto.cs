using Bot.Enums;
using Punishments.Enums;
using System.ComponentModel.DataAnnotations;

namespace Punishments.DTOs;

public class ModCaseTemplateForCreateDto
{
    [Required(ErrorMessage = "TemplateName field is required", AllowEmptyStrings = false)]
    [MaxLength(100)]
    public string TemplateName { get; set; }

    [Required(ErrorMessage = "ViewPermission field is required")]
    public ViewPermission ViewPermission { get; set; }

    [Required(ErrorMessage = "Title field is required")]
    [MaxLength(100)]
    public string Title { get; set; }

    [Required(ErrorMessage = "Description field is required")]
    public string Description { get; set; }

    public string[] Labels { get; set; } = [];

    [Required(ErrorMessage = "PunishmentType field is required")]
    public PunishmentType PunishmentType { get; set; }

    public DateTime? PunishedUntil { get; set; }

    [Required] public bool HandlePunishment { get; set; } = false;

    [Required] public SeverityType SeverityType { get; set; }
}
