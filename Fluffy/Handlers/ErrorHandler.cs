using Discord;
using Microsoft.Extensions.Logging;

namespace Fluffy.Handlers;

public class ErrorHandler : IHandler
{
    private static ErrorHandler _instance;
    private readonly ILogger<ErrorHandler> _logger;
    private ITextChannel _channel;
    
    public ErrorHandler(ILogger<ErrorHandler> logger)
    {
        _logger = logger;
        _instance = this;
    }

    public int Order => -1;

    public void Register()
    {
        Program.Client.Disconnected += OnDisconnected;
    }

    private async Task OnDisconnected(Exception exception)
    {
        if (exception is not null)
            await HandleErrorInternal("The discord client disconnected unexpected.", exception);
    }

    public async Task Initialize()
    {
        _channel = (ITextChannel)await Program.Client.GetChannelAsync(Program.GuildConfig.ErrorChannel);
    }

    private async Task HandleErrorInternal(string message, Exception ex)
    {
        await _channel.SendMessageAsync(
            embed: new EmbedBuilder()
                .WithColor(0xff0f0f)
                .WithTitle(message ?? "An error occured")
                .WithDescription(ex.ToString())
                .WithFooter(ex.GetType().Name)
                .Build());
    }

    public static void HandleError(string message, Exception exception)
    {
        _instance._logger.LogError(message, exception);
        Task.Factory.StartNew(async () =>
        {
            try
            {
                await _instance.HandleErrorInternal(message, exception);
            }
            catch (Exception ex)
            {
                _instance._logger.LogError(message: "An error occurred trying to send the error message.", exception: ex);
            }
        });
    }

    public static void HandleInteractionError(string message, IInteractionContext context)
    {
        Task.Factory.StartNew(async () =>
        {
            try
            {
                var description = message is not null
                    ? $"❌ ┊ {message}"
                    : "❌ ┊ An internal error occurred. Please try again later.";
                
                if (context.Interaction.HasResponded)
                    await context.Interaction.FollowupAsync(
                        ephemeral: true,
                        embed: new EmbedBuilder()
                            .WithDescription(description)
                            .WithFooter("Feel free to notify the owner of the server about this incident.\nYou can use the provided ticket system in #help.")
                            .WithColor(0xff0f0f)
                            .Build());
                else 
                    await context.Interaction.RespondAsync(
                        ephemeral: true,
                        embed: new EmbedBuilder()
                            .WithDescription(description)
                            .WithFooter("Feel free to notify the owner of the server about this incident.\nYou can use the provided ticket system in #help.")
                            .WithColor(0xff0f0f)
                            .Build());
            }
            catch (Exception ex)
            {
                _instance._logger.LogError(message: "An error occurred trying to send the error message via interaction.", exception: ex);
            }
        });
    }
}