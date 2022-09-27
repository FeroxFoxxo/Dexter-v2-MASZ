using AutoMods;
using Bot;
using Bot.Abstractions;
using GuildAudits;
using Invites;
using Levels;
using Messaging;
using MOTDs;
using Punishments;
using Social;
using UserMaps;
using UserNotes;
using Utilities;

namespace Launch;

public static class ImportModules
{
	public static List<Module> GetModules()
	{
		return new List<Module>
		{
			new BotModule(),
			new AutoModModule(),
			new GuildAuditModule(),
			new InviteModule(),
			new MotdModule(),
			new PunishmentModule(),
			new UserMapModule(),
			new UserNoteModule(),
			new UtilityModule(),
			new MessagingModule(),
			new LevelsModule(),
			new SocialModule()
		};
	}
}