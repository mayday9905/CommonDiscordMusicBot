using CommonDiscordMusicBot;
using Serilog;

namespace Program
{
    public static class Program
    {
        public static async Task Main()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .CreateLogger();

            await new Initialize().InitializeAsync();
        }
    }
}