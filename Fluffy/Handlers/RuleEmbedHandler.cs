using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Fluffy.Handlers;

public class RuleEmbedHandler : IHandler
{
    private readonly ILogger _logger;
    private ITextChannel _generalRulesChannel;
    private ITextChannel _nsfwRulesChannel;
    private IMessage _nsfwRulesMessage;
    private IMessage _generalRulesMessage;

    public RuleEmbedHandler(ILogger<RuleEmbedHandler> logger)
    {
        _logger = logger;
    }

    public int Order => 4;

    public void Register()
    {
        Program.Client.ButtonExecuted += OnButtonExecuted;
    }

    public async Task Initialize()
    {
        _generalRulesChannel = (ITextChannel)await Program.Client.GetChannelAsync(Program.GuildConfig.GeneralRulesChannel);
        _nsfwRulesChannel = (ITextChannel)await Program.Client.GetChannelAsync(Program.GuildConfig.NsfwRulesChannel);

        await GetOrSendNsfwRulesMessage();
        await GetOrSendGeneralRulesMessage();
    }

    private async Task GetOrSendGeneralRulesMessage()
    {
        var generalRulesMessageId = Program.GuildConfig.GeneralRulesMessage;
        if (generalRulesMessageId.HasValue)
        {
            _generalRulesMessage = await _generalRulesChannel.GetMessageAsync(generalRulesMessageId.Value);
            if (_generalRulesMessage is not null)
            {
                _logger.LogInformation("General rules message could be recovered.");
                return;
            }
        }

        _generalRulesMessage = await SendGeneralRulesMessage();
        await Program.GuildConfig.Update(x => x.GeneralRulesMessage = _generalRulesMessage.Id);
        _logger.LogInformation("General rules message has been sent.");
    }

    private async Task<bool> GetOrSendNsfwRulesMessage()
    {
        var nsfwRulesMessageId = Program.GuildConfig.NsfwRulesMessage;
        if (nsfwRulesMessageId.HasValue)
        {
            _nsfwRulesMessage = await _nsfwRulesChannel.GetMessageAsync(nsfwRulesMessageId.Value);
            if (_nsfwRulesMessage is not null)
            {
                _logger.LogInformation("Nsfw rules message could be recovered.");
                return true;
            }
        }

        _nsfwRulesMessage = await SendNsfwRulesMessage();
        await Program.GuildConfig.Update(x => x.NsfwRulesMessage = _nsfwRulesMessage.Id);
        _logger.LogInformation("Nsfw rules message has been sent.");
        return false;
    }

    private async Task<IMessage> SendNsfwRulesMessage()
    {
        return await _generalRulesChannel.SendMessageAsync(
            embed: new EmbedBuilder()
                .WithTitle("­ ­ ­ ­ ­ ­ ­ ­ ­ ­ - ̗̀  ­ ­ NSFW Rules ­ ­ ̖́-")
                .WithDescription(
                    "­\n" +
                    "🔞 Since NSFW content is very diverse and the topic is very controversial, rules are important to avoid upsetting other members of the community and to refrain from cultural taboos.\n" +
                    "For this reason, please read the rules before uploading any images:\n" +
                    "\n" +
                    "🔸 Any sexual taboo is strictly not allowed in any of these channels.\n" +
                    "🔸 This include child pornography / loli / shota / incest / beastiality / necrophilia / non-consensual / rape / gore / graphic violence.\n" +
                    "🔸 Furries are allowed -- as long as they do not pass through the border of beastiality.\n" +
                    "🔸 Real life image references (such as pose references) are allowed as long as they are artistically nude or models are covered with underwear.\n" +
                    "🔸 No real-life pornography - that includes: photos, videos, and links.\n" +
                    "\n" +
                    "To summarize:\n" +
                    "🔹 Make sure you don't break any laws with the content you send.\n" +
                    "🔹 NSFW art is welcome here, but real-life pornography is not.\n" +
                    "🔹 Don't overdo it... Everyone should know what oversteps the boundries.\n")
                .WithImageUrl(Program.GuildConfig.NsfwRulesBannerImageUrl)
                .WithFooter("By accepting these rules you gain access to our community.")
                .WithColor(0xa30b00)
                .Build(),
            components: new ComponentBuilder()
                .WithButton(new ButtonBuilder()
                    .WithLabel("Accept NSFW Rules")
                    .WithCustomId("accept_nsfw_rules")
                    .WithStyle(ButtonStyle.Primary)
                    .WithEmote(new Emoji("✅")))
                .Build());
    }

    private async Task<IMessage> SendGeneralRulesMessage()
    {
        return await _generalRulesChannel.SendMessageAsync(
            embed: new EmbedBuilder()
                .WithTitle("­ ­ ­ ­ - ̗̀  ­ ­  Welcome to our wonderful community ­ ­ ̖́-")
                .WithDescription(
                    "­\n" +
                    "To ensure that everyone has a pleasant and enjoyable experience, we kindly ask you to abide by the following guidelines:\n" +
                    "\n" +
                    "🔸 Be mature and respectful.\n" +
                    "🔸 Keep all discussions/media safe for work (except for the NSFW section).\n" +
                    "🔸 Avoid making personal derogatory jokes.\n" +
                    "🔸 Do not discriminate against others.\n" +
                    "🔸 Steer clear of discussing politics, religion, controversial, and toxic topics.\n" +
                    "🔸 Refrain from advertising for streamers, artists, or other content creators.\n" +
                    "🔸 Do not post server invites from any other server.\n" +
                    "🔸 Do not leak any Patreon exclusive content from any creator in the server.\n" +
                    "🔸 If you have a concerns regarding what's right to post, just ask a mod.\n" +
                    "\n" +
                    "Remember, our goal is to foster a friendly, welcoming environment for all.\n" +
                    "Thank you for your cooperation! ❤️")
                .WithImageUrl(Program.GuildConfig.RulesBannerImageUrl)
                .WithFooter("By accepting these rules you gain access to our community.")
                .WithColor(0xd9864c)
                .Build(),
            components: new ComponentBuilder()
                .WithButton(new ButtonBuilder()
                    .WithLabel("Accept Rules")
                    .WithCustomId("accept_general_rules")
                    .WithStyle(ButtonStyle.Primary)
                    .WithEmote(new Emoji("✅")))
                .Build());
    }

    private async Task OnButtonExecuted(SocketMessageComponent arguments)
    {
        try
        {
            if (arguments.Message.Id != _nsfwRulesMessage.Id &&
                arguments.Message.Id != _generalRulesMessage.Id) return;
            switch (arguments.Data.CustomId)
            {
                case "accept_general_rules":
                    await OnGeneralRulesAcceptButtonPress(arguments);
                    break;
                case "accept_nsfw_rules":
                    await OnNSFWRulesAcceptButtonPress(arguments);
                    break;
            }
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleError("An error occurred when trying to handle a rule embed button.", ex);
            ErrorHandler.HandleInteractionError("An error occurred when trying to handle a rule embed button.", arguments);
        }
    }

    private async Task OnGeneralRulesAcceptButtonPress(SocketMessageComponent arguments)
    {
        var user = await _generalRulesChannel.GetUserAsync(arguments.User.Id);
        if (user.RoleIds.Contains(Program.GuildConfig.MemberRoleId))
        {
            await arguments.RespondAsync(
                ephemeral: true,
                embed: new EmbedBuilder()
                    .WithDescription("❌ ┊ You already agreed to the rules.")
                    .WithColor(0xff0f0f)
                    .Build());
            return;
        }

        await user.AddRoleAsync(Program.GuildConfig.MemberRoleId);
        if (!user.RoleIds.Contains(Program.GuildConfig.FoxInfoHeaderRoleId))
            await user.AddRoleAsync(Program.GuildConfig.FoxInfoHeaderRoleId);
        if (!user.RoleIds.Contains(Program.GuildConfig.FoxRoleHeaderRoleId))
            await user.AddRoleAsync(Program.GuildConfig.FoxRoleHeaderRoleId);
        if (!user.RoleIds.Contains(Program.GuildConfig.UnknownFoxRoleId))
            await user.AddRoleAsync(Program.GuildConfig.UnknownFoxRoleId);
        if (!user.RoleIds.Contains(Program.GuildConfig.UnknownPronounsRoleId))
            await user.AddRoleAsync(Program.GuildConfig.UnknownPronounsRoleId);
            
        await arguments.RespondAsync(
            ephemeral: true,
            embed: new EmbedBuilder()
                .WithDescription("✅ ┊ Welcome to the community!")
                .WithColor(0x00e05e)
                .Build());
        
        _logger.LogInformation($"Added member privileges to {user}.");
    }

    private async Task OnNSFWRulesAcceptButtonPress(SocketMessageComponent arguments)
    {
        var user = await _nsfwRulesChannel.GetUserAsync(arguments.User.Id);
        if (user.RoleIds.Contains(Program.GuildConfig.NaughtyFoxRoleId))
        {
            await arguments.RespondAsync(
                ephemeral: true,
                embed: new EmbedBuilder()
                    .WithDescription("❌ ┊ You already agreed to the rules.")
                    .WithColor(0xff0f0f)
                    .Build());
            return;
        }

        await user.AddRoleAsync(Program.GuildConfig.NaughtyFoxRoleId);
        if (user.RoleIds.Contains(Program.GuildConfig.NaughtyFoxUnapprovedRoleId))
            await user.RemoveRoleAsync(Program.GuildConfig.NaughtyFoxUnapprovedRoleId);
            
        await arguments.RespondAsync(
            ephemeral: true,
            embed: new EmbedBuilder()
                .WithDescription("✅ ┊ You agreed to the nsfw rules!")
                .WithColor(0x00e05e)
                .Build());
        
        _logger.LogInformation($"Added member privileges to {user}.");
    }
}