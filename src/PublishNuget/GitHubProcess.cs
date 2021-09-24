using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace PublishNuget
{
    internal static class GitHubProcess
    {
        internal static async Task<bool> ExecuteCommandAsync(string command, Action<string?> exceptionCallback, Action<string?>? outputCallback = null, string? loggedCommand = null)
        {
            using var process = new Process();

            var isSuccess = true;

            var splitIndex = command.IndexOf(' ');

            process.StartInfo = new ProcessStartInfo(command[..splitIndex], command[(splitIndex + 1)..])
            {
                RedirectStandardInput = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            outputCallback?.Invoke(string.IsNullOrWhiteSpace(loggedCommand) ? command : loggedCommand);

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.ErrorDataReceived += (_, args) =>
            {
                if (string.IsNullOrEmpty(args.Data))
                    return;

                isSuccess = false;

                errorBuilder.AppendLine(args.Data);
            };

            process.OutputDataReceived += (_, args) =>
            {
                if (string.IsNullOrEmpty(args.Data))
                    return;

                outputBuilder.AppendLine(args.Data);
            };

            process.Start();

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            await process.WaitForExitAsync();

            if (outputBuilder.Length != 0)
                outputCallback?.Invoke(outputBuilder.ToString());

            if (errorBuilder.Length != 0)
                exceptionCallback?.Invoke(errorBuilder.ToString());

            return isSuccess && process.ExitCode == 0;
        }
    }
}
