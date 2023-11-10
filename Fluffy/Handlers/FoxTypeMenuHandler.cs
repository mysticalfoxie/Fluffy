using Discord;
using Discord.WebSocket;
using Fluffy.Helper;
using Microsoft.Extensions.Logging;

namespace Fluffy.Handlers;

public class FoxTypeMenuHandler : IHandler
{
    private readonly Dictionary<ulong, string> _typeRoles = new();
    private readonly ILogger<FoxTypeMenuHandler> _logger;
    private ITextChannel _channel;
    private IMessage _message;
    private SocketGuild _guild;

    public FoxTypeMenuHandler(
        ILogger<FoxTypeMenuHandler> logger)
    {
        _logger = logger;
    }

    public int Order => 2;

    public void Register()
    {
        Program.Client.SelectMenuExecuted += OnSelectMenuExecuted;
    }

    private async Task OnSelectMenuExecuted(SocketMessageComponent component)
    {
        try
        {
            if (component.Data.CustomId != "fox_type") return;
            if (component.Message.Id != _message.Id) return;
            await HandleSelectMenu(component);
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleError("An error occurred when trying to handle a select menu response.", ex);
            ErrorHandler.HandleInteractionError("An error occurred when trying to handle a select menu response.", component);
        }
    }

    private async Task HandleSelectMenu(SocketMessageComponent component)
    {
        if (component.Data.Values.Count != 1)
            return;
        
        var roleId = ulong.Parse(component.Data.Values.First());
        if (!_typeRoles.TryGetValue(roleId, out var roleName))
            throw new Exception($"Role with id {roleId} is not known.");
        
        var user = await _channel.GetUserAsync(component.User.Id);

        if (user.RoleIds.Contains(Program.GuildConfig.UnknownFoxRoleId))
        {
            await user.RemoveRoleAsync(Program.GuildConfig.UnknownFoxRoleId);
            _logger.LogInformation($"Removed unkown fox type role from {user}.");
        }
        
        await user.AddRoleAsync(roleId);
        _logger.LogInformation($"Added fox type role '{roleName.TrimUnicode()}' to {user}.");
        var toRemove = GetUsersFoxTypeRolesExcept(user, roleId);
        
        if (toRemove.Length > 0)
        {
            foreach (var role in toRemove)
                await user.RemoveRoleAsync(role);
            
            _logger.LogInformation($"Removed {toRemove.Length} fox type role(s) from {user}.");
            
            await component.RespondAsync(
                ephemeral: true,
                embed: new EmbedBuilder()
                    .WithDescription($"✅ ┊ You switch from\n<@&{toRemove.First()}>\nto your new role\n<@&{roleId}>")
                    .WithColor(0x00e05e)
                    .Build());
            return;
        }

        await component.RespondAsync(
            ephemeral: true,
            embed: new EmbedBuilder()
                .WithDescription($"✅ ┊ You have been assigned as a\n<@&{roleId}>")
                .WithColor(0x00e05e)
                .Build());
    }

    private ulong[] GetUsersFoxTypeRolesExcept(IGuildUser user, ulong role)
    {
        var roles = new List<ulong>(_typeRoles.Keys);
        roles.Remove(role);

        var toRemove = roles.Where(i => !user.RoleIds.Contains(i)).ToArray();
        foreach (var i in toRemove)
            roles.Remove(i);

        return roles.ToArray();
    }

    public async Task Initialize()
    {
        _guild = Program.Client.GetGuild(Program.GuildConfig.Guild);
        _typeRoles.Add(Program.GuildConfig.HumanRoleId, _guild.Roles.First(x => x.Id == Program.GuildConfig.HumanRoleId).Name.Trim());
        _typeRoles.Add(Program.GuildConfig.RedFoxRoleId, _guild.Roles.First(x => x.Id == Program.GuildConfig.RedFoxRoleId).Name.Trim());
        _typeRoles.Add(Program.GuildConfig.SilverFoxRoleId, _guild.Roles.First(x => x.Id == Program.GuildConfig.SilverFoxRoleId).Name.Trim());
        _typeRoles.Add(Program.GuildConfig.GrayFoxRoleId, _guild.Roles.First(x => x.Id == Program.GuildConfig.GrayFoxRoleId).Name.Trim());
        _typeRoles.Add(Program.GuildConfig.FennecFoxRoleId, _guild.Roles.First(x => x.Id == Program.GuildConfig.FennecFoxRoleId).Name.Trim());
        _typeRoles.Add(Program.GuildConfig.ArcticFoxRoleId, _guild.Roles.First(x => x.Id == Program.GuildConfig.ArcticFoxRoleId).Name.Trim());
        _channel = (ITextChannel)await Program.Client.GetChannelAsync(Program.GuildConfig.RoleChannel);
        
        var messageId = Program.GuildConfig.FoxTypeMessage;
        if (messageId.HasValue)
        {
            _message = await _channel.GetMessageAsync(messageId.Value);
            if (_message is not null)
            {
                _logger.LogInformation("Fox type message could be recovered.");
                return;
            }
        }

        _message = await SendFoxTypeMessage();
        await Program.GuildConfig.Update(x => x.FoxTypeMessage = _message.Id);
        _logger.LogInformation("Fox type message has been sent.");
    }

    private async Task<IMessage> SendFoxTypeMessage()
    {
        return await _channel.SendMessageAsync(
            embed: new EmbedBuilder()
                .WithTitle("­ ­ ­ ­ ­ ­ - ̗̀  ­ ­ What kind of fox are you? ­ ­ ̖́-")
                .WithDescription(
                    "🦊 Choose your fox type from the dropdown below!\n" + 
                    "✨ By selecting your race, your username's color will be influenced accordingly.\n" + 
                    "👤 If you prefer, you can choose \"Human\" as well, if you don't identify as a fox.\n\n" +
                    "Instructions:\n\n" +
                    "🔹 Click on the dropdown menu below to reveal the available fox races.\n" +
                    "🔹 Select the fox type that best represents you or choose \"Human\" if you prefer.\n" +
                    "🔹 When you made your selection, watch as your username's color transforms to match your chosen fox type.\n\n" +
                    "🎉 Embrace your foxy nature and let your personality stand!\n")
                .WithImageUrl(Program.GuildConfig.FoxTypeBannerImageUrl)
                .WithColor(0xfa0079)
                .Build(),
            components: new ComponentBuilder()
                .WithSelectMenu("fox_type", new List<SelectMenuOptionBuilder>
                {
                    new SelectMenuOptionBuilder()
                        .WithEmote(!Emote.TryParse(Program.GuildConfig.RedFoxRoleEmote, out var redFoxEmote)
                            ? Emoji.Parse(Program.GuildConfig.RedFoxRoleEmote)
                            : redFoxEmote)
                        .WithValue(Program.GuildConfig.RedFoxRoleId.ToString())
                        .WithLabel("Red fox"),
                    new SelectMenuOptionBuilder()
                        .WithEmote(!Emote.TryParse(Program.GuildConfig.GrayFoxRoleEmote, out var grayFoxEmote)
                            ? Emoji.Parse(Program.GuildConfig.GrayFoxRoleEmote)
                            : grayFoxEmote)
                        .WithValue(Program.GuildConfig.GrayFoxRoleId.ToString())
                        .WithLabel("Gray fox"),
                    new SelectMenuOptionBuilder()
                        .WithEmote(!Emote.TryParse(Program.GuildConfig.SilverFoxRoleEmote, out var silverFoxEmote)
                            ? Emoji.Parse(Program.GuildConfig.SilverFoxRoleEmote)
                            : silverFoxEmote)
                        .WithValue(Program.GuildConfig.SilverFoxRoleId.ToString())
                        .WithLabel("Silver fox"),
                    new SelectMenuOptionBuilder()
                        .WithEmote(!Emote.TryParse(Program.GuildConfig.FennecFoxRoleEmote, out var fennecFoxEmote)
                            ? Emoji.Parse(Program.GuildConfig.FennecFoxRoleEmote)
                            : fennecFoxEmote)
                        .WithValue(Program.GuildConfig.FennecFoxRoleId.ToString())
                        .WithLabel("Fennec fox"),
                    new SelectMenuOptionBuilder()
                        .WithEmote(!Emote.TryParse(Program.GuildConfig.ArcticFoxRoleEmote, out var arcticFoxEmote)
                            ? Emoji.Parse(Program.GuildConfig.ArcticFoxRoleEmote)
                            : arcticFoxEmote)
                        .WithValue(Program.GuildConfig.ArcticFoxRoleId.ToString())
                        .WithLabel("Artic fox"),
                    new SelectMenuOptionBuilder()
                        .WithEmote(!Emote.TryParse(Program.GuildConfig.HumanRoleEmote, out var humanEmote)
                            ? Emoji.Parse(Program.GuildConfig.HumanRoleEmote)
                            : humanEmote)
                        .WithValue(Program.GuildConfig.HumanRoleId.ToString())
                        .WithLabel("Human")
                }).Build());
    }
}