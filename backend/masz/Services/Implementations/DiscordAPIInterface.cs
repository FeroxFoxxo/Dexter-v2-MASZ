﻿using masz.Dtos.DiscordAPIResponses;
using masz.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace masz.Services
{
    public class DiscordAPIInterface : IDiscordAPIInterface
    {        
        private string discordBaseUrl => "https://discord.com/api";
        private readonly ILogger<DiscordAPIInterface> logger;
        private readonly IOptions<InternalConfig> config;
        private readonly string botToken;
        private Dictionary<string, CacheApiResponse> cache = new Dictionary<string, CacheApiResponse>();
        private RestClient restClient;

        public DiscordAPIInterface() {  }
        public DiscordAPIInterface(ILogger<DiscordAPIInterface> logger, IOptions<InternalConfig> config)
        {
            this.logger = logger;
            this.config = config;
            this.botToken = config.Value.DiscordBotToken;

            restClient = new RestClient(discordBaseUrl);
        }

        public async Task<Ban> GetGuildUserBan(string guildId, string userId)
        {
            if (this.cache.ContainsKey($"/guilds/{guildId}/bans/{userId}")) {
                if (this.cache[$"/guilds/{guildId}/bans/{userId}"].ExpiresAt > DateTime.Now) {
                    return new Ban(this.cache[$"/guilds/{guildId}/bans/{userId}"].Content);
                }
                this.cache.Remove($"/guilds/{guildId}/bans/{userId}");
            }
            var request = new RestRequest(Method.GET);
            request.Resource = $"/guilds/{guildId}/bans/{userId}";
            request.AddHeader("Authorization", "Bot " + botToken);

            var response = await restClient.ExecuteAsync<Ban>(request);
            if (response.IsSuccessful)
            {
                this.cache.TryAdd($"/guilds/{guildId}/bans/{userId}", new CacheApiResponse(response.Content, 3));
                return new Ban(response.Content);
            }
            return null;
        }

        public async Task<User> FetchCurrentUserInfo(string token)
        {
            var request = new RestRequest(Method.GET);
            request.Resource = "/users/@me";
            request.AddHeader("Authorization", "Bearer " + token);

            var response = await restClient.ExecuteAsync<User>(request);
            if (response.IsSuccessful)
            {
                return new User(response.Content);
            }
            return null;
        }

        public async Task<User> FetchCurrentBotInfo()
        {
            var request = new RestRequest(Method.GET);
            request.Resource = "/users/@me";
            request.AddHeader("Authorization", "Bot " + botToken);

            var response = await restClient.ExecuteAsync<User>(request);
            if (response.IsSuccessful)
            {
                return new User(response.Content);
            }
            return null;
        }

        public async Task<List<Channel>> FetchGuildChannels(string guildId)
        {
            if (this.cache.ContainsKey($"/guilds/{guildId}/channels")) {
                if (this.cache[$"/guilds/{guildId}/channels"].ExpiresAt > DateTime.Now) {
                    return JsonConvert.DeserializeObject<List<Channel>>(this.cache[$"/guilds/{guildId}/channels"].Content);
                }
                this.cache.Remove($"/guilds/{guildId}/channels");
            }
            var request = new RestRequest(Method.GET);
            request.Resource = $"/guilds/{guildId}/channels";
            request.AddHeader("Authorization", "Bot " + botToken);

            var response = await restClient.ExecuteAsync<List<Guild>>(request);
            if (response.IsSuccessful)
            {
                this.cache.TryAdd($"/guilds/{guildId}/channels", new CacheApiResponse(response.Content));
                return JsonConvert.DeserializeObject<List<Channel>>(response.Content);
            }
            return null;
        }

        public async Task<Guild> FetchGuildInfo(string guildId)
        {
            if (this.cache.ContainsKey($"/guilds/{guildId}")) {
                if (this.cache[$"/guilds/{guildId}"].ExpiresAt > DateTime.Now) {
                    return new Guild(this.cache[$"/guilds/{guildId}"].Content);
                }
                this.cache.Remove($"/guilds/{guildId}");
            }
            var request = new RestRequest(Method.GET);
            request.Resource = $"/guilds/{guildId}";
            request.AddHeader("Authorization", "Bot " + botToken);

            var response = await restClient.ExecuteAsync<Guild>(request);
            if (response.IsSuccessful)
            {
                this.cache.TryAdd($"/guilds/{guildId}", new CacheApiResponse(response.Content));
                return new Guild(response.Content);
            }
            return null;
        }

        public async Task<List<Guild>> FetchGuildsOfCurrentUser(string token)
        {
            var request = new RestRequest(Method.GET);
            request.Resource = "/users/@me/guilds";
            request.AddHeader("Authorization", "Bearer " + token);

            var response = await restClient.ExecuteAsync<List<Guild>>(request);
            if (response.IsSuccessful)
            {
                return JsonConvert.DeserializeObject<List<Guild>>(response.Content);
            }
            return null;
        }

        public async Task<GuildMember> FetchMemberInfo(string guildId, string userId)
        {
            if (this.cache.ContainsKey($"/guilds/{guildId}/members/{userId}")) {
                if (this.cache[$"/guilds/{guildId}/members/{userId}"].ExpiresAt > DateTime.Now) {
                    return new GuildMember(this.cache[$"/guilds/{guildId}/members/{userId}"].Content);
                }
                this.cache.Remove($"/guilds/{guildId}/members/{userId}");
            }
            var request = new RestRequest(Method.GET);
            request.Resource = $"/guilds/{guildId}/members/{userId}";
            request.AddHeader("Authorization", "Bot " + botToken);

            var response = await restClient.ExecuteAsync<GuildMember>(request);
            if (response.IsSuccessful)
            {
                this.cache.TryAdd($"/guilds/{guildId}/members/{userId}", new CacheApiResponse(response.Content));
                return new GuildMember(response.Content);
            }
            return null;
        }

        public async Task<User> FetchUserInfo(string userId)
        {
            if (this.cache.ContainsKey($"/users/{userId}")) {
                if (this.cache[$"/users/{userId}"].ExpiresAt > DateTime.Now) {
                    return new User(this.cache[$"/users/{userId}"].Content);
                }
                this.cache.Remove($"/users/{userId}");
            }
            var request = new RestRequest(Method.GET);
            request.Resource = $"/users/{userId}";
            request.AddHeader("Authorization", "Bot " + botToken);

            var response = await restClient.ExecuteAsync<User>(request);
            if (response.IsSuccessful)
            {
                this.cache.TryAdd($"/users/{userId}", new CacheApiResponse(response.Content));
                return new User(response.Content);
            }
            return null;
        }

        public Task<Message> GetDiscordMessage(string channelId, string messageId)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> ValidateUserToken(string token)
        {
            var request = new RestRequest(Method.GET);
            request.Resource = "/users/@me";
            request.AddHeader("Authorization", "Bearer " + token);

            var response = await restClient.ExecuteAsync<User>(request);
            return response.IsSuccessful;
        }

        public async Task<bool> BanUser(string guildId, string userId)
        {
            var request = new RestRequest(Method.PUT);
            request.Resource = $"/guilds/{guildId}/bans/{userId}";
            request.AddHeader("Authorization", "Bot " + botToken);

            var response = await restClient.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                return true;
            }
            logger.LogError($"{response.StatusCode}: {response.Content}");
            return false;
        }

        public async Task<bool> UnBanUser(string guildId, string userId)
        {
            var request = new RestRequest(Method.DELETE);
            request.Resource = $"/guilds/{guildId}/bans/{userId}";
            request.AddHeader("Authorization", "Bot " + botToken);

            var response = await restClient.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                return true;
            }
            logger.LogError($"{response.StatusCode}: {response.Content}");
            return false;
        }

        public async Task<bool> GrantGuildUserRole(string guildId, string userId, string roleId)
        {
            var request = new RestRequest(Method.PUT);
            request.Resource = $"/guilds/{guildId}/members/{userId}/roles/{roleId}";
            request.AddHeader("Authorization", "Bot " + botToken);

            var response = await restClient.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                return true;
            }
            logger.LogError($"{response.StatusCode}: {response.Content}");
            return false;
        }

        public async Task<bool> RemoveGuildUserRole(string guildId, string userId, string roleId)
        {
            var request = new RestRequest(Method.DELETE);
            request.Resource = $"/guilds/{guildId}/members/{userId}/roles/{roleId}";
            request.AddHeader("Authorization", "Bot " + botToken);

            var response = await restClient.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                return true;
            }
            logger.LogError($"{response.StatusCode}: {response.Content}");
            return false;
        }

        public async Task<bool> KickGuildUser(string guildId, string userId)
        {
            var request = new RestRequest(Method.DELETE);
            request.Resource = $"/guilds/{guildId}/members/{userId}";
            request.AddHeader("Authorization", "Bot " + botToken);

            var response = await restClient.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                return true;
            }
            logger.LogError($"{response.StatusCode}: {response.Content}");
            return false;
        }
    }
}
