using botnard.Services;
using Discord;
using Discord.Interactions;
using System.Text.RegularExpressions;

namespace botnard.Modules
{
    public class SystemModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly OSService _os;

        // Constructor injection: The DI container provides the OSService automatically
        public SystemModule(OSService os)
        {
            _os = os;
        }

        [SlashCommand("kill-task", "Terminates a process by name")]
        [RequireOwner]
        public async Task KillTask(string processName)
        {
            _os.KillProcessByName(processName);
            await RespondAsync($"Attempted to kill all instances of `{processName}`.");
        }
    }
}