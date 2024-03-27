using Bot.Abstractions;
using Bot.Models;
using Bot.Services;
using Discord;
using Punishments.Enums;
using Punishments.Models;

namespace Punishments.Events;

public class PunishmentEventAudit(AuditLogger auditLogger, PunishmentEventHandler punishmentEventHandler) : IEvent
{
    private readonly AuditLogger _auditLogger = auditLogger;
    private readonly PunishmentEventHandler _punishmentEventHandler = punishmentEventHandler;

    public void RegisterEvents()
    {
        _punishmentEventHandler.OnFileUploaded += OnFileUploaded;

        _punishmentEventHandler.OnModCaseCommentDeleted += OnModCaseCommentDeleted;
        _punishmentEventHandler.OnModCaseCommentUpdated += OnModCaseCommentUpdated;
        _punishmentEventHandler.OnModCaseCommentCreated += OnModCaseCommentCreated;

        _punishmentEventHandler.OnModCaseDeleted += OnModCaseDeleted;
        _punishmentEventHandler.OnModCaseUpdated += OnModCaseUpdated;
        _punishmentEventHandler.OnModCaseCreated += OnModCaseCreated;
    }

    private async Task OnFileUploaded(UploadedFile fileInfo, ModCase modCase, IUser actor) =>
        await _auditLogger.QueueLog(
            $"**File** `{fileInfo.Name}` uploaded to case {modCase.GuildId}/{modCase.CaseId} by <@{actor.Id}>.");

    private async Task OnModCaseCommentDeleted(ModCaseComment modCaseComment, IUser actor) =>
        await _auditLogger.QueueLog(
            $"**Comment** `{modCaseComment.ModCase.GuildId}/{modCaseComment.ModCase.CaseId}/{modCaseComment.Id}` by <@{modCaseComment.UserId}> deleted.");

    private async Task OnModCaseCommentUpdated(ModCaseComment modCaseComment, IUser actor) =>
        await _auditLogger.QueueLog(
            $"**Comment** `{modCaseComment.ModCase.GuildId}/{modCaseComment.ModCase.CaseId}/{modCaseComment.Id}` by <@{modCaseComment.UserId}> updated.");

    private async Task OnModCaseCommentCreated(ModCaseComment modCaseComment, IUser actor) =>
        await _auditLogger.QueueLog(
            $"**Comment** `{modCaseComment.ModCase.GuildId}/{modCaseComment.ModCase.CaseId}/{modCaseComment.Id}` by <@{modCaseComment.UserId}> created.");

    private async Task OnModCaseDeleted(ModCase modCase, IUser actor) =>
        await _auditLogger.QueueLog(
            $"**Mod-case** `{modCase.GuildId}/{modCase.CaseId}` for <@{modCase.UserId}> deleted.");

    private async Task OnModCaseUpdated(ModCase modCase, IUser actor) =>
        await _auditLogger.QueueLog(
            $"**Mod-case** `{modCase.GuildId}/{modCase.CaseId}` for <@{modCase.UserId}> by <@{modCase.LastEditedByModId}> updated.");

    private async Task OnModCaseCreated(ModCase modCase, IUser actor, AnnouncementResult result) =>
        await _auditLogger.QueueLog(
            $"**Mod-case** `{modCase.GuildId}/{modCase.CaseId}` for <@{modCase.UserId}> by <@{modCase.ModId}> created.");
}
