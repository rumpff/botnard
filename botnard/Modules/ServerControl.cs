using Discord.Interactions;

namespace botnard.Modules
{
    // These modules must be public and inherit from InteractionModuleBase
    public class ServerControl : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("ping", "Check if the bot is alive")]
        public async Task Ping()
        {
            await RespondAsync("Pong! The server is listening.");
        }

        [SlashCommand("status", "Get server uptime and basic info")]
        public async Task Status()
        {
            var uptime = DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime;
            await RespondAsync($"Bot has been running for {uptime.Days}d {uptime.Hours}h {uptime.Minutes}m.");
        }
    }
}