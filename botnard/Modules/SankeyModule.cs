using botnard.Services;
using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace botnard.Modules
{
    public enum GraphDetail
    {
        [ChoiceDisplay("Normal")] Normal,
        [ChoiceDisplay("Detailed")] ShowAccounts,
        [ChoiceDisplay("Minimal")] NoBudgets
    }
    public enum RelativePeriods
    {
        [ChoiceDisplay("Current Month")] CurrentMonth,
        [ChoiceDisplay("Current Quarter")] CurrentQuarter,
        [ChoiceDisplay("Current Year")] CurrentYear,
        [ChoiceDisplay("Previous Month")] PreviousMonth,
        [ChoiceDisplay("Previous Quarter")] PreviousQuarter,
        [ChoiceDisplay("Previous Year")] PreviousYear,
    }

    [Group("budget-sankey", "Firefly-III Sankey generation tools")]
    public class SankeyModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly OSService _os;
        public SankeyModule(OSService os) => _os = os;

        [SlashCommand("generate", "Generate Sankey Diagram")]
        public async Task SankeyDefault(
            [Summary("period", "Data range")] RelativePeriods period = RelativePeriods.CurrentMonth,
            [Summary("detail", "The level of detail")] GraphDetail detail = GraphDetail.Normal)
        {
            string currentPeriod;
            DateTime now = DateTime.Now;

            switch (period)
            {
                case RelativePeriods.CurrentMonth:
                    currentPeriod = now.ToString("yyyy-MM");
                    break;

                case RelativePeriods.CurrentQuarter:
                    // Format: 2026-q1
                    int currentQ = (now.Month - 1) / 3 + 1;
                    currentPeriod = $"{now.Year}-q{currentQ}";
                    break;

                case RelativePeriods.CurrentYear:
                    // Format: 2026
                    currentPeriod = now.Year.ToString();
                    break;

                case RelativePeriods.PreviousMonth:
                    // Format: 2026-02-01 (Subtracts 1 month from current)
                    currentPeriod = now.AddMonths(-1).ToString("yyyy-MM");
                    break;

                case RelativePeriods.PreviousQuarter:
                    // Format: 2025-q4 (Subtracts 3 months to step back one quarter)
                    DateTime prevQDate = now.AddMonths(-3);
                    int prevQ = (prevQDate.Month - 1) / 3 + 1;
                    currentPeriod = $"{prevQDate.Year}-q{prevQ}";
                    break;

                case RelativePeriods.PreviousYear:
                    // Format: 2025
                    currentPeriod = (now.Year - 1).ToString();
                    break;

                default:
                    currentPeriod = "";
                    break;
            }

            string args = $"-p {currentPeriod} {GetDetailFlags(detail)}";

            await ExecuteSankeyAsync(args, $"Current Month: {currentPeriod} ({detail})");
        }

        [SlashCommand("generate-period", "Generate Sankey Diagram for a specified period")]
        public async Task SankeyPeriod(
            [Summary("period", "Period. YYYY (year), YYYY-MM (month), YYYY-QX (quater), or YYYY-MM-DD (single day)")] string period,
            [Summary("detail", "The level of detail for the graph")] GraphDetail detail = GraphDetail.Normal)
        {
            string args = $"-p {period}{GetDetailFlags(detail)}";

            await ExecuteSankeyAsync(args, $"Period: {period} ({detail})");
        }

        [SlashCommand("generate-range", "Generate Sankey Diagram for a specific date range")]
        public async Task SankeyRange(
            [Summary("start", "Start date (YYYY-MM-DD)")] string start,
            [Summary("end", "End date (YYYY-MM-DD)")] string end,
            [Summary("detail", "The level of detail for the graph")] GraphDetail detail = GraphDetail.Normal)
        {
            string args = $"-s {start} -e {end}{GetDetailFlags(detail)}";

            await ExecuteSankeyAsync(args, $"Range: {start} to {end} ({detail})");
        }

        [SlashCommand("generate-all", "Generate Sankey Diagram for all data")]
        public async Task SankeyAll(
        [Summary("detail", "The level of detail for the graph")] GraphDetail detail = GraphDetail.Normal)
        {
            string start = DotNetEnv.Env.GetString("FIREFLY_DATA_START_DATE");
            string end = DateTime.Now.ToString("yyyy-MM-dd");

            string args = $"-s {start} -e {end}{GetDetailFlags(detail)}";

            await ExecuteSankeyAsync(args, $"All time ({detail})");
        }

        // Private helper to handle the common execution and Regex logic
        private async Task ExecuteSankeyAsync(string commandArgs, string label)
        {
            // 1. Let the user know we're working on it
            await DeferAsync();
            Bot.LogLine($"[Sankey] Starting execution for: {label}");

            string token = DotNetEnv.Env.GetString("FIREFLY_TOKEN");
            string folderPath = DotNetEnv.Env.GetString("FIREFLY_SANKEY_PATH");
            string url = DotNetEnv.Env.GetString("FIREFLY_URL");

            try
            {
                // 2. Run the process and capture output
                var (output, success) = await _os.RunInDirectoryAsync("npx", $"firefly-iii-sankey -t {token} -u {url} {commandArgs} -f sankeymatic", folderPath);

                Bot.LogLine($"[Sankey] Execution {(success ? "Succeeded" : "Failed")}");

                if (!success)
                {
                    // Show the first 500 chars of the error to the user so they can help debug
                    string errorSnippet = output.Length > 500 ? output.Substring(0, 500) + "..." : output;
                    await FollowupAsync($"❌ **Command failed** for {label}.\n```text\n{errorSnippet}\n```");
                    return;
                }

                // 4. Extract URL
                var urlMatch = Regex.Match(output, @"https://sankeymatic\.com/build/[^\s]+", RegexOptions.IgnoreCase);

                if (urlMatch.Success)
                {
                    await FollowupAsync($"✅ **Sankey Generated!** **[Click to view](<{urlMatch.Value.Trim()}>)** ({label}) ");
                }
                else
                {
                    await FollowupAsync($"⚠️ URL not found in output for **{label}**. Check console logs.");
                }
            }
            catch (Exception ex)
            {
                Bot.LogLine($"[Sankey] Critical Exception: {ex.Message}");
                await FollowupAsync($"🔥 A critical error occurred: `{ex.Message}`");
            }
        }

        private string GetDetailFlags(GraphDetail detail) => detail switch
        {
            GraphDetail.ShowAccounts => " --with-accounts",
            GraphDetail.NoBudgets => "--no-budgets",
            _ => "" // Normal/Default
        };
    }
}