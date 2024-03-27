using Bot.Abstractions;
using Bot.Dynamics;
using Bot.Exceptions;
using Bot.Extensions;
using Bot.Services;
using Punishments.Events;
using Punishments.Exceptions;
using Punishments.Models;

namespace Punishments.Data;

public class ModCaseCommentRepository : Repository, IAddGuildStats
{
    private readonly PunishmentEventHandler _eventHandler;
    private readonly ModCaseRepository _modCaseRepository;
    private readonly PunishmentDatabase _punishmentDatabase;

    public ModCaseCommentRepository(PunishmentDatabase punishmentDatabase, PunishmentEventHandler eventHandler,
        ModCaseRepository modCaseRepository, DiscordRest discordRest) : base(discordRest)
    {
        _punishmentDatabase = punishmentDatabase;
        _eventHandler = eventHandler;
        _modCaseRepository = modCaseRepository;

        _modCaseRepository.AsUser(Identity);
    }

    public async Task AddGuildStatistics(dynamic stats, ulong guildId) =>
        stats.comments = await CountCommentsByGuild(guildId);

    public async Task<int> CountCommentsByGuild(ulong guildId) =>
        await _punishmentDatabase.CountCommentsForGuild(guildId);

    public async Task<List<ModCaseComment>> GetLastCommentsByGuild(ulong guildId) =>
        await _punishmentDatabase.SelectLastModCaseCommentsByGuild(guildId);

    public async Task<ModCaseComment> GetSpecificComment(int commentId)
    {
        var comment = await _punishmentDatabase.SelectSpecificModCaseComment(commentId);

        return comment ?? throw new ResourceNotFoundException();
    }

    public async Task<ModCaseComment> CreateComment(ulong guildId, int caseId, string comment)
    {
        var modCase = await _modCaseRepository.GetModCase(guildId, caseId);

        if (!modCase.AllowComments)
            throw new CaseIsLockedException();

        if (modCase.MarkedToDeleteAt.HasValue)
            throw new CaseMarkedToBeDeletedException();

        ModCaseComment newComment = new()
        {
            CreatedAt = DateTime.UtcNow,
            UserId = Identity.Id,
            Message = comment,
            ModCase = modCase
        };

        await _punishmentDatabase.SaveModCaseComment(newComment);

        _eventHandler.ModCaseCommentCreatedEvent.Invoke(newComment, Identity);

        return newComment;
    }

    public async Task<ModCaseComment> UpdateComment(ulong guildId, int caseId, int commentId, string newMessage)
    {
        var modCase = await _modCaseRepository.GetModCase(guildId, caseId);

        if (!modCase.AllowComments)
            throw new CaseIsLockedException();

        if (modCase.MarkedToDeleteAt.HasValue)
            throw new CaseMarkedToBeDeletedException();

        var newComment = modCase.Comments.FirstOrDefault(c => c.Id == commentId) ?? throw new ResourceNotFoundException();
        newComment.Message = newMessage;

        await _punishmentDatabase.UpdateModCaseComment(newComment);

        _eventHandler.ModCaseCommentUpdatedEvent.Invoke(newComment, Identity);

        return newComment;
    }

    public async Task<ModCaseComment> DeleteComment(ulong guildId, int caseId, int commentId)
    {
        var modCase = await _modCaseRepository.GetModCase(guildId, caseId);

        if (!modCase.AllowComments)
            throw new CaseIsLockedException();

        if (modCase.MarkedToDeleteAt.HasValue)
            throw new CaseMarkedToBeDeletedException();

        var deleteComment = modCase.Comments.FirstOrDefault(c => c.Id == commentId) ?? throw new ResourceNotFoundException();
        await _punishmentDatabase.DeleteSpecificModCaseComment(deleteComment);

        _eventHandler.ModCaseCommentDeletedEvent.Invoke(deleteComment, Identity);

        return deleteComment;
    }
}
