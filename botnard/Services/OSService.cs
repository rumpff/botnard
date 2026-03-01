using System.Diagnostics;
using System.Text;

namespace botnard.Services
{
    public class OSService
    {

        public async Task<(string Output, bool Success)> RunInDirectoryAsync(string command, string args, string workingDir)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command} {args}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDir // This targets your specific folder
            };

            using var process = new Process { StartInfo = startInfo };
            StringBuilder outputBuilder = new();

            process.OutputDataReceived += (s, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
            process.ErrorDataReceived += (s, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            return (outputBuilder.ToString(), process.ExitCode == 0);
        }

        public void KillProcessByName(string name)
        {
            var processes = Process.GetProcessesByName(name);
            foreach (var p in processes) p.Kill();
        }
    }
}