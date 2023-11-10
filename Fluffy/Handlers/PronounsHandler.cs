using Discord;
using Discord.WebSocket;
using Fluffy.Helper;
using Microsoft.Extensions.Logging;

namespace Fluffy.Handlers;

public class PronounsHandler : IHandler
{
    private readonly ILogger<PronounsHandler> _logger;
    private readonly Dictionary<ulong, string> _typeRoles = new();
    private ITextChannel _channel;
    private IMessage _message;
    private SocketGuild _guild;

    public PronounsHandler(
        ILogger<PronounsHandler> logger)
    {
        _logger = logger;
    }

    public int Order => 0;

    public void Register()
    {
        Program.Client.SelectMenuExecuted += OnSelectMenuExecuted;
    }

    private async Task OnSelectMenuExecuted(SocketMessageComponent component)
    {
        try
        {
            if (component.Data.CustomId != "pronouns") return;
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
        if (user.RoleIds.Contains(Program.GuildConfig.UnknownPronounsRoleId))
        {
            await user.RemoveRoleAsync(Program.GuildConfig.UnknownPronounsRoleId);
            _logger.LogInformation($"Removed unkown pronouns role from {user}.");
        }
        
        await user.AddRoleAsync(roleId);
        _logger.LogInformation($"Added pronouns role '{roleName.TrimUnicode()}' to {user}.");
        var toRemove = GetPronounsRolesExcept(user, roleId);
        
        if (toRemove.Length > 0)
        {
            foreach (var role in toRemove)
                await user.RemoveRoleAsync(role);
            
            _logger.LogInformation($"Removed {toRemove.Length} pronouns role(s) from {user}.");
            
            await component.RespondAsync(
                ephemeral: true,
                embed: new EmbedBuilder()
                    .WithDescription($"✅ ┊ Your role\n<@&{toRemove.First()}>\nhas been replaced with\n<@&{roleId}>")
                    .WithColor(0x00e05e)
                    .Build());
            return;
        }

        await component.RespondAsync(
            ephemeral: true,
            embed: new EmbedBuilder()
                .WithDescription($"✅ ┊ You received the role\n<@&{roleId}>")
                .WithColor(0x00e05e)
                .Build());
    }

    private ulong[] GetPronounsRolesExcept(IGuildUser user, ulong role)
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
        _typeRoles.Add(Program.GuildConfig.FemalePronounsRoleId, _guild.Roles.First(x => x.Id == Program.GuildConfig.FemalePronounsRoleId).Name.Trim());
        _typeRoles.Add(Program.GuildConfig.MalePronounsRoleId, _guild.Roles.First(x => x.Id == Program.GuildConfig.MalePronounsRoleId).Name.Trim());
        _typeRoles.Add(Program.GuildConfig.DiversPronounsRoleId, _guild.Roles.First(x => x.Id == Program.GuildConfig.DiversPronounsRoleId).Name.Trim());
        _channel = (ITextChannel)await Program.Client.GetChannelAsync(Program.GuildConfig.RoleChannel);
        var messageId = Program.GuildConfig.PronounsMessage;
        if (messageId.HasValue)
        {
            _message = await _channel.GetMessageAsync(messageId.Value);
            if (_message is not null)
            {
                _logger.LogInformation("Pronouns message could be recovered.");
                return;
            }
        }

        _message = await SendPronounsMessage();
        await Program.GuildConfig.Update(x => x.PronounsMessage = _message.Id);
        _logger.LogInformation("Pronouns message has been sent.");
    }

    private async Task<IMessage> SendPronounsMessage()
    {
        return await _channel.SendMessageAsync(
            embed: new EmbedBuilder()
                .WithTitle("­ ­ ­ ­ ­ ­ ­ ­ - ̗̀  ­ ­ Select your Pronouns ­ ­ ̖́-")
                .WithDescription(
                    "🌈 ­ Selecting the right pronouns is important for creating an inclusive and respectful community.\n" +
                    "Our Pronoun Picker feature allows you to easily choose the pronouns that best represent your gender identity. Embrace self-expression and let others know how you wish to be referred to!\n\n" +
                    "🔹 Simply select your preferred pronouns from the dropdown menu below.\n" +
                    "🔹 Once you've made your selection, your chosen pronouns will be in your server profile, helping others address you correctly.\n" +
                    "🔹 Feel free to update your pronouns at any time.\n\n" +
                    "❗ Remember, respecting and honoring each other's pronouns is an essential part of creating a safe and inclusive space.")
                .WithImageUrl(Program.GuildConfig.PronounsBannerImageUrl)
                .WithColor(0x9d00e0)
                .Build(),
            components: new ComponentBuilder()
                .WithSelectMenu("pronouns", new List<SelectMenuOptionBuilder>
                {
                    new SelectMenuOptionBuilder()
                        .WithEmote(!Emote.TryParse(Program.GuildConfig.FemalePronounsRoleEmote, out var femaleEmote)
                            ? Emoji.Parse(Program.GuildConfig.FemalePronounsRoleEmote)
                            : femaleEmote)
                        .WithValue(Program.GuildConfig.FemalePronounsRoleId.ToString())
                        .WithLabel("she・her"),
                    new SelectMenuOptionBuilder()
                        .WithEmote(!Emote.TryParse(Program.GuildConfig.MalePronounsRoleEmote, out var maleEmote)
                            ? Emoji.Parse(Program.GuildConfig.MalePronounsRoleEmote)
                            : maleEmote)
                        .WithValue(Program.GuildConfig.MalePronounsRoleId.ToString())
                        .WithLabel("he・him"),
                    new SelectMenuOptionBuilder()
                        .WithEmote(!Emote.TryParse(Program.GuildConfig.DiversPronounsRoleEmote, out var diversEmote)
                            ? Emoji.Parse(Program.GuildConfig.DiversPronounsRoleEmote)
                            : diversEmote)
                        .WithValue(Program.GuildConfig.DiversPronounsRoleId.ToString())
                        .WithLabel("they・them")
                }).Build());
    }
}