using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Victoria;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using CommonDiscordMusicBot.Services;
using CommonDiscordMusicBot.Modules;

namespace CommonDiscordMusicBot
{
    public class Initialize
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commandService;
        private readonly string? _config;
        private IServiceProvider? _services;

        public Initialize()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true,
                MessageCacheSize = 50,
                LogLevel = LogSeverity.Info,
                GatewayIntents = GatewayIntents.AllUnprivileged
            });

            _commandService = new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Info,
                CaseSensitiveCommands = false
            });

            _config = ConfigService.GetToken();
        }

        public async Task InitializeAsync()
        {
            await _client.LoginAsync(TokenType.Bot, _config);
            await _client.StartAsync();

            _client.Log += async (arg) =>
            {
                Log.Information(arg.ToString());
                await Task.CompletedTask;
            };

            Log.Information(ConfigService.GetAuth);
            Log.Information(ConfigService.GetPort.ToString());
            Log.Information(ConfigService.GetHostname);
            Log.Information(ConfigService.GetToken());

            await ServicesSetup();
            await Task.Delay(-1);
        }

        public async Task Logging()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .MinimumLevel.Verbose()
                .WriteTo.File("log")
                .CreateLogger();
            await Task.CompletedTask;
        }

        public async Task ServicesSetup()
        {
            _services = ServiceProvider();

            var commandHandler = new CommandHandler(_client, _commandService, _services);
            await commandHandler.SetupConfigAsync();

            await _services.GetRequiredService<MusicModule>().InitializeAsync();
        }

        private IServiceProvider ServiceProvider() 
            => new ServiceCollection()
            .AddSingleton(_client)
            .AddSingleton(_commandService)
            .AddSingleton<LavaNode>()
            .AddSingleton<LavaConfig>()
            .AddSingleton<MusicModule>()
            .AddSingleton<MusicService>()
            .AddLavaNode( x =>
            {
                x.IsSsl = false;
                x.Authorization = ConfigService.GetAuth;
                x.Port = ConfigService.GetPort;
                x.Hostname = ConfigService.GetHostname;   
            })
            .BuildServiceProvider();
    }
}
