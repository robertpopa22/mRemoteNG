using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace mRemoteNG.DotNet.Update
{
    public class DotNetRuntimeCheck
    {
        public const string RequiredDotnetVersion = "9.0.8";
        public const string DotnetInstallerUrl = "https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-9.0.8-windows-x64-installer";
        public const string DotnetInstallerFileName = "windowsdesktop-runtime-9.0.8-win-x64.exe";

        public static async Task Main(string[] args)
        {
            if (await IsDotnetRuntimeInstalled(RequiredDotnetVersion))
            {
                Console.WriteLine($".NET Desktop Runtime {RequiredDotnetVersion} is installed. Launching application...");
            }
            else
            {
                Console.WriteLine($".NET Desktop Runtime {RequiredDotnetVersion} is not installed.");
            }
        }
        /// <summary>
        /// Checks if a specific version of the .NET runtime is installed by running `dotnet --list-runtimes`.
        /// </summary>
        public static async Task<bool> IsDotnetRuntimeInstalled(string version)
            {
                try
                {
                    // Set up a process to run the 'dotnet' command.
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "dotnet",
                            Arguments = "--list-runtimes",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                        }
                    };

                    process.Start();

                // Read the output from the command.
                var output = await process.StandardOutput.ReadToEndAsync();
                    process.WaitForExit();

                    // Check if the output contains the required runtime and version.
                    // The format is typically: Microsoft.NETCore.App 9.0.0 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
                    return output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Any(line => line.Trim().StartsWith($"Microsoft.NETCore.App {version}") ||
                                              line.Trim().StartsWith($"Microsoft.WindowsDesktop.App {version}"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not check .NET runtimes. Please ensure 'dotnet' is in your PATH. Error: {ex.Message}");
                    return false;
                }
            }
        } //Check
}
