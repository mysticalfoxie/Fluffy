using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Fluffy.Handlers;

public class NsfwHandler : IHandler
{
    private readonly ILogger<NsfwHandler> _logger;
    private ITextChannel _channel;
    private IMessage _message;

    public NsfwHandler(
        ILogger<NsfwHandler> logger)
    {
        _logger = logger;
    }

    public int Order => 2;

    public void Register()
    {
        Program.Client.ButtonExecuted += OnButtonExecuted;
    }

    private async Task OnButtonExecuted(SocketMessageComponent arguments)
    {
        try
        {
            if (arguments.Message.Id != _message.Id) return;
            if (arguments.Data.CustomId != "nsfw") return;
            
            var user = await _channel.GetUserAsync(arguments.User.Id);
            if (user.RoleIds.Contains(Program.GuildConfig.NaughtyFoxRoleId))
            {
                await user.RemoveRoleAsync(Program.GuildConfig.NaughtyFoxRoleId);
                await arguments.RespondAsync(
                    ephemeral: true,
                    embed: new EmbedBuilder()
                        .WithDescription("✅ ┊ `NSFW Role` has been removed")
                        .WithColor(0x00e05e)
                        .Build());
                _logger.LogInformation($"Removed nsfw role from {user}.");
            }
            else
            {
                await user.AddRoleAsync(Program.GuildConfig.NaughtyFoxRoleId);
                await arguments.RespondAsync(
                    ephemeral: true,
                    embed: new EmbedBuilder()
                        .WithDescription("✅ ┊ `NSFW Role` has been added")
                        .WithColor(0x00e05e)
                        .Build());
                _logger.LogInformation($"Added nsfw role to {user}.");
            }
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleError("An error occurred when trying to handle a select menu response.", ex);
        }
    }
    
    public async Task Initialize()
    {
        _channel = (ITextChannel)await Program.Client.GetChannelAsync(Program.GuildConfig.RoleChannel);
        var messageId = Program.GuildConfig.NsfwMessage;
        if (messageId.HasValue)
        {
            _message = await _channel.GetMessageAsync(messageId.Value);
            if (_message is not null)
            {
                _logger.LogInformation("Nsfw message could be recovered.");
                return;
            }
        }

        _message = await SendNsfwMessage();
        await Program.GuildConfig.Update(x => x.NsfwMessage = _message.Id);
        _logger.LogInformation("Nsfw message has been sent.");
    }

    private async Task<IMessage> SendNsfwMessage()
    {
        return await _channel.SendMessageAsync(
            embed: new EmbedBuilder()
                .WithTitle("NSFW Section")
                .WithDescription(
                    "When you are older then 18 and ok with seeing nsfw (not safe for work)\n" + 
                    "content, you can click the button down below.")
                .WithFooter("You can also click it to remove the section again.")
                .WithColor(0xe00700)
                .Build(),
            components: new ComponentBuilder()
                .WithButton(new ButtonBuilder()
                    .WithLabel("Enable/Disable NSFW Section")
                    .WithCustomId("nsfw")
                    .WithEmote(!Emote.TryParse(Program.GuildConfig.NaughtyFoxRoleEmote, out var naughtyFoxEmote)
                        ? Emoji.Parse(Program.GuildConfig.NaughtyFoxRoleEmote)
                        : naughtyFoxEmote)
                    .WithStyle(ButtonStyle.Danger))
                .Build());
    }
}