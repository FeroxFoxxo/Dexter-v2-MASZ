export interface GuildAuditLogConfig {
    id: number;
    guildId: string;
    guildAuditLogEvent: number;
    channelId: string;
    pingRoles: string[];
    ignoreRoles: string[];
    ignoreChannels: string[];
}
