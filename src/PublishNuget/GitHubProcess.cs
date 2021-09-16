using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PublishNuget
{
    internal static class GitHubProcess
    {
        internal static async Task<bool> ExecuteCommandAsync(string command, Action<string?> exceptionCallback, Action<string?>? outputCallback)
        {
            using var process = new Process();

            var isSuccess = true;

            process.StartInfo = new ProcessStartInfo("bash", command);

            process.ErrorDataReceived += (_, args) =>
            {
                isSuccess = false;
                exceptionCallback.Invoke(args.Data);
            };

            process.OutputDataReceived += (_, args) => outputCallback?.Invoke(args?.Data);

            outputCallback?.Invoke(command);

            process.Start();

            await process.WaitForExitAsync();

            return isSuccess;
        }
    }
}
