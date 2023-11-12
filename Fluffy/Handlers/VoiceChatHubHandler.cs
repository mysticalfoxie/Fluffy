using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fluffy.Handlers;

public class VoiceChatHubHandler : IHandler
{
    private readonly ILogger<VoiceChatHubHandler> _logger;
    private SocketGuild _guild;
    private IVoiceChannel _hub;
    private SocketCategoryChannel _category;
    private OverwritePermissions _hubPermissions;
    private OverwritePermissions _categoryPermissionOverwrite;
    private OverwritePermissions _tempChannelUserPermission;
    private Overwrite _categoryOverwrite;
    private Regex _pattern;

    public VoiceChatHubHandler(ILogger<VoiceChatHubHandler> logger)
    {
        _logger = logger;
    }
    
    public int Order => 6;

    public void Register()
    {
        Program.Client.UserVoiceStateUpdated += OnUserVoiceStateUpdated;
    }

    private async Task OnUserVoiceStateUpdated(SocketUser user, SocketVoiceState voiceStateO, SocketVoiceState voiceState)
    {
        if (voiceStateO.VoiceChannel.Guild.Id != Program.GuildConfig.Guild) return;
        var countO = voiceStateO.VoiceChannel.ConnectedUsers.Count;
        var count = voiceState.VoiceChannel.ConnectedUsers.Count;
        if (countO < count)
            await OnUserJoinedVoiceChannel(user, voiceState.VoiceChannel);
        else if (countO > count)
            await OnUserLeftVoiceChannel(user, voiceState.VoiceChannel);
        // Update's within the channel are being ignored
    }

    private async Task OnUserLeftVoiceChannel(SocketUser user, SocketVoiceChannel voiceChannel)
    {
        if (!IsTempChannel(voiceChannel)) return;
        if (user.ActiveClients.Count > 0) return;

        await voiceChannel.DeleteAsync();
    }

    private async Task OnUserJoinedVoiceChannel(SocketUser user, SocketVoiceChannel voiceChannel)
    {
        if (voiceChannel.Id != _hub.Id) return;
        var tempChannels = GetTempChannels();
        var targetPosition = _category.Channels.Count;
        var targetName = GetNewTempChannelName(tempChannels);
        var channel = await CreateNewTempChannel(user, targetName, targetPosition);
        var member = _guild.GetUser(user.Id);
        await member.ModifyAsync(props =>
        {
            props.Channel = new Optional<IVoiceChannel>(channel);
        });
    }

    private async Task<IVoiceChannel> CreateNewTempChannel(IUser owner, string name, int position)
    {
        return await _guild.CreateVoiceChannelAsync(name, opts =>
        {
            opts.Position = position;
            opts.CategoryId = _category.Id;
            opts.PermissionOverwrites = new[]
            {
                new Overwrite(owner.Id, PermissionTarget.User, _tempChannelUserPermission)
            };
        });
    }

    private static string GetNewTempChannelName(IEnumerable<IVoiceChannel> tempChannels)
    {
        var tempChannelNameIndex = (tempChannels.Count() + 1).ToString().PadLeft(2, '0');
        var splittedPattern = Program.GuildConfig.TempVoiceNamingPattern.Split("\\d+");
        return splittedPattern[0] + tempChannelNameIndex + splittedPattern[1];
    }

    private bool IsTempChannel(IChannel channel)
    {
        return _pattern.IsMatch(channel.Name);
    }

    // Expensive method!!...
    private IEnumerable<IVoiceChannel> GetTempChannels()
    {
        _category = _guild.GetCategoryChannel(_category.Id); // Re-Download to update cache
        var tempChannels = new List<IVoiceChannel>();
        foreach (var channel in _category.Channels)
        {
            if (channel is not IVoiceChannel voiceChannel) continue;
            if (_pattern.IsMatch(voiceChannel.Name)) continue; 
            tempChannels.Add(voiceChannel);
        }

        return tempChannels.ToArray();
    }

    public async Task Initialize()
    {
        _guild = Program.Client.GetGuild(Program.GuildConfig.Guild);
        InitializeFields();
        await GetOrCreateCategory();
        await EnsureCategoryPermissions();
        await GetOrCreateHub();
        await EnsureHubPermissions();
        await EnsureRightCategory();
    }

    private async Task EnsureCategoryPermissions()
    {
        var memberRole = _guild.Roles.First(x => x.Id == Program.GuildConfig.MemberRoleId);
        var rules = _hub.GetPermissionOverwrite(memberRole);
        
        // This may not work... It's a workaround to not check every property...
        var rulesJson = rules is null ? null : JsonConvert.SerializeObject(rules);
        var targetRulesJson = JsonConvert.SerializeObject(_categoryPermissionOverwrite);
        if (rulesJson is not null && rulesJson == targetRulesJson)
            return;

        await _hub.AddPermissionOverwriteAsync(_guild.EveryoneRole, _hubPermissions);
        _logger.LogInformation("Voice Hub permissions have been adjusted.");
    }

    private void InitializeFields()
    {
        _hubPermissions = new OverwritePermissions(speak: PermValue.Deny, sendMessages: PermValue.Deny);
        _categoryPermissionOverwrite = new OverwritePermissions(speak: PermValue.Allow, sendMessages: PermValue.Allow);
        _categoryOverwrite = new Overwrite(Program.GuildConfig.MemberRoleId, PermissionTarget.Role, _categoryPermissionOverwrite);
        _tempChannelUserPermission = new OverwritePermissions(manageChannel: PermValue.Allow);
        _pattern = new Regex(Program.GuildConfig.TempVoiceNamingPattern);
    }

    private async Task GetOrCreateCategory()
    {
        _category = _guild.GetCategoryChannel(Program.GuildConfig.VoiceHubCategoryId);
        if (_category is not null)
            return;

        var category = await _guild.CreateCategoryChannelAsync(Program.GuildConfig.VoiceHubCategoryName, opts =>
        {
            opts.Position = _guild.CategoryChannels.Count - 1;
            opts.PermissionOverwrites = new[] { _categoryOverwrite };
        });
        
        _category = _guild.GetCategoryChannel(category.Id);
    }

    private async Task EnsureRightCategory()
    {
        if (_hub.CategoryId == Program.GuildConfig.VoiceHubCategoryId) return;

        await _hub.ModifyAsync(opts =>
        {
            opts.CategoryId = Program.GuildConfig.VoiceHubCategoryId;
        });
        
        _logger.LogInformation("Voice Hub has been moved in the right category.");
    }

    private async Task EnsureHubPermissions()
    {
        var rules = _hub.GetPermissionOverwrite(_guild.EveryoneRole);
        
        // This may not work... It's a workaround to not check every property...
        var rulesJson = rules is null ? null : JsonConvert.SerializeObject(rules);
        var targetRulesJson = JsonConvert.SerializeObject(_hubPermissions);
        if (rulesJson is not null && rulesJson == targetRulesJson)
            return;

        await _hub.AddPermissionOverwriteAsync(_guild.EveryoneRole, _hubPermissions);
        _logger.LogInformation("Voice Hub permissions have been adjusted.");
    }

    private async Task GetOrCreateHub()
    {
        if (Program.GuildConfig.VoiceHubId.HasValue)
        {
            _hub = _guild.GetVoiceChannel(Program.GuildConfig.VoiceHubId.Value);
            if (_hub is not null)
            {
                _logger.LogInformation("Voice Hub could be found and recovered.");
                return;
            }
        }

        _hub = await CreateNewHubChannel();
        _logger.LogInformation("Voice Hub has been created.");
    }

    private async Task<IVoiceChannel> CreateNewHubChannel()
    {
        return await _guild.CreateVoiceChannelAsync(Program.GuildConfig.VoiceHubName, opts =>
        {
            opts.CategoryId = _category.Id;
            opts.Position = _category.Channels.Count - 1;
            opts.PermissionOverwrites = new Overwrite[]
            {
                new(_guild.EveryoneRole.Id, PermissionTarget.Role, _hubPermissions)
            };
        });
    }
}