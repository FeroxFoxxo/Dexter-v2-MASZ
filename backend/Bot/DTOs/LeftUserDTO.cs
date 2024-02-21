using Bot.Models;
using Discord;
using System.Collections.Immutable;

namespace Bot.DTOs;

public class LeftUserDto : IUser
{
    public LeftUserDto(IUser user)
    {
        IsBot = user.IsBot;
        Username = user.Username;
        DiscriminatorValue = user.DiscriminatorValue;
        AvatarId = user.AvatarId;
        PublicFlags = user.PublicFlags;
        Id = user.Id;
    }

    public LeftUserDto(LeftUser user)
    {
        IsBot = user.IsBot;
        Username = user.Username;
        DiscriminatorValue = user.DiscriminatorValue;
        AvatarId = user.AvatarId;
        PublicFlags = user.PublicFlags;
        Id = user.Id;
    }

    /// <inheritdoc />
    public ulong Id { get; set; }

    /// <inheritdoc />
    public bool IsBot { get; set; }

    /// <inheritdoc />
    public string Username { get; set; }

    /// <inheritdoc />
    public ushort DiscriminatorValue { get; set; }

    /// <inheritdoc />
    public string AvatarId { get; set; }

    /// <inheritdoc />
    public UserProperties? PublicFlags { get; set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt => SnowflakeUtils.FromSnowflake(Id);

    /// <inheritdoc />
    public string Discriminator => DiscriminatorValue.ToString("D4");

    /// <inheritdoc />
    public string Mention => MentionUtils.MentionUser(Id);

    /// <inheritdoc />
    public virtual UserStatus Status => UserStatus.Offline;

    /// <inheritdoc />
    public virtual IReadOnlyCollection<ClientType> ActiveClients => [];

    /// <inheritdoc />
    public virtual IReadOnlyCollection<IActivity> Activities => [];

    /// <inheritdoc />
    public virtual bool IsWebhook => false;

    /// <inheritdoc />
    public string GlobalName => Username;

    public string AvatarDecorationHash => throw new NotImplementedException();

    public ulong? AvatarDecorationSkuId => throw new NotImplementedException();

    public Task<IDMChannel> CreateDMChannelAsync(RequestOptions options = null)
        => throw new NotImplementedException();

    public string GetAvatarUrl(ImageFormat format = ImageFormat.Auto, ushort size = 128)
        => CDN.GetUserAvatarUrl(Id, AvatarId, size, format);

    public string GetDefaultAvatarUrl()
        => CDN.GetDefaultUserAvatarUrl(DiscriminatorValue);

    public LeftUser CreateUserFromDto()
        => new()
        {
            AvatarId = AvatarId,
            DiscriminatorValue = DiscriminatorValue,
            Id = Id,
            IsBot = IsBot,
            PublicFlags = PublicFlags,
            Username = Username
        };

    public string GetDisplayAvatarUrl(ImageFormat format = ImageFormat.Auto, ushort size = 128)
        => CDN.GetUserAvatarUrl(Id, AvatarId, size, format);

    public string GetAvatarDecorationUrl()
        => CDN.GetAvatarDecorationUrl(AvatarDecorationHash);
}
