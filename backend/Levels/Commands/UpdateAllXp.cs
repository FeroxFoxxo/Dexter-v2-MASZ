using Bot.Abstractions;
using Bot.Attributes;
using Bot.Enums;
using Discord;
using Discord.Interactions;
using Levels.Data;
using Levels.Models;

namespace Levels.Commands;

public class UpdateAllXp : Command<UpdateAllXp>
{
    public GuildLevelConfigRepository GuildLevelConfigRepository { get; set; }
    public GuildUserLevelRepository GuildUserLevelRepository { get; set; }

    [SlashCommand("update-all-xp", "Updates all user's experience")]
    [Require(RequireCheck.GuildAdmin)]
    public async Task UpdateAllXpCommand()
    {
        var guild = Context.Guild;
        var userXps = GuildUserLevelRepository.GetAllLevelsInGuild(guild.Id);
        var guildConfig = await GuildLevelConfigRepository!.GetOrCreateConfig(guild.Id);

        var roles = guildConfig.Levels.ToDictionary(
            x => x.Key,
            x => x.Value.Where(x => guild.GetRole(x) != null).ToArray()
        );

        var users = await guild.GetUsersAsync().FlattenAsync();
        var count = 0;
        var totalCount = users.Count();

        await RespondInteraction($"Updating all user roles in {guild.Name}!");

        var infoMsg = await Context.Channel.SendMessageAsync("Running update script...");

        var added = new Dictionary<IGuildUser, ulong[]>();
        var removed = new Dictionary<IGuildUser, ulong[]>();

        foreach (var user in users)
        {
            var xp = userXps.FirstOrDefault(x => x.UserId == user.Id);
            var level = 0;

            if (xp != null)
            {
                var calcLevel = new CalculatedGuildUserLevel(xp, guildConfig);
                level = calcLevel.Total.Level;
            }

            var addedRoles = roles.Where(x => x.Key <= level)
                .SelectMany(x => x.Value)
                .Where(x => !user.RoleIds.Contains(x));

            var removedRoles = roles.Where(x => x.Key > level)
                .SelectMany(x => x.Value)
                .Where(x => user.RoleIds.Contains(x));

            if (addedRoles.Any())
                added.Add(user, addedRoles.ToArray());

            if (removedRoles.Any())
                removed.Add(user, removedRoles.ToArray());

            count++;

            if (count % 100 == 0)
                await infoMsg.ModifyAsync(x =>
                    x.Content = "Calculating... " + GetProgressString(count, totalCount)
                );
        }

        count = 0;
        totalCount = added.Count;

        foreach (var addedUser in added)
        {
            await addedUser.Key.AddRolesAsync(addedUser.Value);

            count++;

            if (count % 5 == 0)
                await infoMsg.ModifyAsync(x =>
                    x.Content = "Adding Roles... " + GetProgressString(count, totalCount)
                );
        }

        count = 0;
        totalCount = removed.Count;

        foreach (var removedUser in removed)
        {
            await removedUser.Key.RemoveRolesAsync(removedUser.Value);

            count++;

            if (count % 5 == 0)
                await infoMsg.ModifyAsync(x =>
                    x.Content = "Removing Roles... " + GetProgressString(count, totalCount)
                );
        }

        await infoMsg.ModifyAsync(x =>
            x.Content = $"Successfully updated all users in {guild.Name}!"
        );
    }

    public static string GetProgressString(int count, int total)
    {
        var progress = Math.Floor((double)count / total * 100);
        return $"progress {count}/{total} ({progress}%)";
    }
}
