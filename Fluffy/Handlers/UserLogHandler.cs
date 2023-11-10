using Discord;
using Discord.WebSocket;

namespace Fluffy.Handlers;

public class UserLogHandler : IHandler
{
    private ITextChannel _channel;
    public int Order => 5;
    
    public void Register()
    {
        Program.Client.UserJoined += OnUserJoined;
        Program.Client.UserLeft += OnUserLeft;
    }

    private async Task OnUserLeft(SocketGuild guild, SocketUser user)
    {
        await _channel.SendMessageAsync(
            embed: new EmbedBuilder()
                .WithDescription($"The user {user.Mention} has left the server. New member count: `{guild.MemberCount}`")
                .WithColor(0xff0f0f)
                .Build());
    }

    private async Task OnUserJoined(SocketGuildUser user)
    {
        var guild = Program.Client.GetGuild(Program.GuildConfig.Guild);
        await _channel.SendMessageAsync(
            embed: new EmbedBuilder()
                .WithDescription($"The user {user.Mention} joined the server. New member count: `{guild.MemberCount}`")
                .WithColor(0x00e05e)
                .Build());
    }

    public async Task Initialize()
    {
        _channel = (ITextChannel)await Program.Client.GetChannelAsync(Program.GuildConfig.MemberLogChannel);
    }
}