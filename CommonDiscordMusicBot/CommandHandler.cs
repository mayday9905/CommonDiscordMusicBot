using Discord.Commands;
using Discord.WebSocket;
using System.Reflection;

namespace CommonDiscordMusicBot
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commandService;
        private readonly IServiceProvider _services;

        public CommandHandler(DiscordSocketClient client, CommandService commandService, IServiceProvider services)
        {
            _client = client;
            _commandService = commandService;
            _services = services;
        }

        public async Task SetupConfigAsync()
        {
            await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            _client.MessageReceived += HandleMessageAsync;
        }

        private async Task HandleMessageAsync(SocketMessage socketMessage)
        {
            var argPos = 0;
            if (socketMessage.Author.IsBot) return;
            var userMessage = socketMessage as SocketUserMessage;

            if (userMessage is null)
                return;

            if (!userMessage.HasCharPrefix('!', ref argPos))
                return;

            var context = new SocketCommandContext(_client, userMessage);
            await _commandService.ExecuteAsync(context, argPos, _services);
        }
    }
}
