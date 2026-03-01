using botnard.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DotNetEnv; // Add this
using Microsoft.Extensions.DependencyInjection;

namespace botnard
{
    public class Program
    {
        private DiscordSocketClient _client;
        private IServiceProvider _services;

        public static Task Main(string[] args) => new Program().MainAsync();

        public async Task MainAsync()
        {
            Bot.LogLine("Loading Envoriment Variables");

            // 1. Load the .env file
            Env.Load();

            // 2. Pull values into variables
            string token = Env.GetString("DISCORD_TOKEN");
            ulong guildId = ulong.Parse(Env.GetString("GUILD_ID"));

            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged
            });

            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<InteractionHandler>()
                .AddSingleton<OSService>()
                .BuildServiceProvider();

            await _services.GetRequiredService<InteractionHandler>().InitializeAsync();

            _client.Ready += async () =>
            {
                // Use the Guild ID from our .env for instant command updates
                await _services.GetRequiredService<InteractionService>()
                    .RegisterCommandsToGuildAsync(guildId);

                Console.WriteLine($"Connected as {_client.CurrentUser.Username}");
            };

            Bot.LogLine("Logging in");

            // 3. Start using the token from .env
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            await Task.Delay(-1);
        }
    }

    public static class Bot
    {
        public static void LogLine(string message)
        {
            Console.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] {message}");
        }
    }
}