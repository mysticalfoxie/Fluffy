using System.Reflection;
using System.Text;
using Discord;
using Discord.WebSocket;
using Fluffy.DatabaseManagement;
using Fluffy.Handlers;
using Fluffy.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fluffy;

public class Program
{
    public static IServiceProvider Services { get; private set; }
    public static ILogger Logger { get; private set; }
    public static AppConfig AppConfig { get; private set; }
    public static GuildConfig GuildConfig { get; private set; }
    public static DiscordSocketClient Client { get; private set; }
    public static List<IHandler> Handlers { get; } = new();

    private static readonly IServiceCollection Collection = new ServiceCollection();

    public static async Task Main()
    {
        await Initialize();
        await StartClient();
        await InitializeEventHandlers();
        await Task.Delay(-1);
    }

    private static async Task InitializeEventHandlers()
    {
        foreach (var handler in Handlers.OrderBy(x => x.Order))
            handler.Register();

        foreach (var handler in Handlers.OrderBy(x => x.Order))
            await handler.Initialize();
    }

    private static async Task StartClient()
    {
        var dcConfig = new DiscordSocketConfig
        {
            UseSystemClock = true,
            DefaultRetryMode = RetryMode.RetryRatelimit | RetryMode.RetryTimeouts,
            UseInteractionSnowflakeDate = false,
            GatewayIntents = GatewayIntents.AllUnprivileged
                             | GatewayIntents.GuildPresences
                             | GatewayIntents.GuildMembers
                             | GatewayIntents.MessageContent,
            AlwaysDownloadUsers = true
        };

        Client = new DiscordSocketClient(dcConfig);

        // Client.Log += message =>
        // {
        //     var errSeverities = new[] { LogSeverity.Error, LogSeverity.Critical };
        //     if (errSeverities.Contains(message.Severity))
        //         Logger.LogError(message: "An internal discord error occurred.", exception: message.Exception);
        //     else
        //         Logger.LogTrace($"[{message.Severity.ToString()}]: {message.Message}");
        //     return Task.CompletedTask;
        // };

        Logger.LogInformation("Discord client initialized.");

        var tcs = new TaskCompletionSource();
        Client.Connected += () =>
        {
            tcs.SetResult();
            return Task.CompletedTask;
        };

        await Client.LoginAsync(TokenType.Bot, AppConfig.Client.LoginToken);
        await Client.StartAsync();
        await tcs.Task;

        Logger.LogInformation("Discord client logged in.");
    }

    private static async Task Initialize()
    {
        CreateLogger();
        await LoadConfigs();
        AddServices();
        InitializeServices();
        LogAssemblyVersion();
        await AddDatabase();
        Logger.LogInformation("Services initialized.");
    }

    private static async Task LoadConfigs()
    {
        var appConfigContent = await File.ReadAllTextAsync("settings.json");
        AppConfig = JsonConvert.DeserializeObject<AppConfig>(appConfigContent);

        var guildConfigContent = await File.ReadAllTextAsync("guild.json");
        GuildConfig = JsonConvert.DeserializeObject<GuildConfig>(guildConfigContent);
    }

    private static Task AddDatabase()
    {
        return Task.CompletedTask;
        // Storage.ConfigureConnection(
        //     AppConfig.Database.IPAddress,
        //     AppConfig.Database.Username,
        //     AppConfig.Database.Password,
        //     AppConfig.Database.Database
        // );
        //
        // await Storage.EnsureCanConnect();
        // Logger.LogInformation("Database connection established .");
    }

    private static void InitializeServices()
    {
        Services = Collection.BuildServiceProvider();
        Logger = Services.GetRequiredService<ILogger<Program>>();
        Handlers.AddRange(new IHandler[]
        {
            Services.GetRequiredService<FoxTypeMenuHandler>(),
            Services.GetRequiredService<ErrorHandler>(),
            Services.GetRequiredService<NsfwHandler>(),
            Services.GetRequiredService<RuleEmbedHandler>(),
            Services.GetRequiredService<UserLogHandler>(),
            Services.GetRequiredService<PronounsHandler>()
        });
    }

    private static void AddServices()
    {
        Collection.AddSingleton<FoxTypeMenuHandler>();
        Collection.AddSingleton<NsfwHandler>();
        Collection.AddSingleton<ErrorHandler>();
        Collection.AddSingleton<PronounsHandler>();
        Collection.AddSingleton<RuleEmbedHandler>();
        Collection.AddSingleton<UserLogHandler>();
    }

    private static void CreateLogger()
    {
        Console.OutputEncoding = Encoding.Unicode;

#if RELEASE
        Collection.AddLogging(builder => builder
            .SetMinimumLevel(LogLevel.Trace)
            .AddLogFile(new DirectoryInfo("Logs"))
            .AddConsole());
#else
        Collection.AddLogging(builder => builder
            .SetMinimumLevel(LogLevel.Trace)
            .AddConsole());
#endif
    }

    private static void LogAssemblyVersion()
    {
        var botAssembly = Assembly.GetExecutingAssembly().GetName();
        var dcAssembly = Assembly.GetAssembly(typeof(IDiscordClient))!.GetName();
        var databaseAssembly = Assembly.GetAssembly(typeof(Storage))!.GetName();
        Logger.LogInformation($"'{botAssembly.Name}' is running at version '{RemoveDeltaVersion(botAssembly.Version)}'.");
        Logger.LogInformation($"'{databaseAssembly.Name}' is running at version '{RemoveDeltaVersion(databaseAssembly.Version)}'.");
        Logger.LogInformation($"'{dcAssembly.Name}' is running at version '{RemoveDeltaVersion(dcAssembly.Version)}'.");
    }

    private static string RemoveDeltaVersion(Version version)
    {
        var versionNumbers = version.ToString().Split('.');
        var importantNumbers = versionNumbers.Take(3).ToArray();
        return string.Join('.', importantNumbers);
    }
}