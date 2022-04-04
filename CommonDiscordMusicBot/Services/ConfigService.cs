namespace CommonDiscordMusicBot.Services
{
    public static class ConfigService
    {
        public static string? GetToken()
        { 
            return Environment.GetEnvironmentVariable("TOKEN");
        }

        public static string? GetAuth => Environment.GetEnvironmentVariable("AUTHENTICATION");
        public static string? GetHostname => Environment.GetEnvironmentVariable("LAVAHOSTNAME");
        public static ushort GetPort => Convert.ToUInt16(Environment.GetEnvironmentVariable("PORT"));
    }
}
