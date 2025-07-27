using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using CliWrap;
using CliWrap.EventStream;
using CustomLegendaryEpicUriHandler.Models;
using Microsoft.Win32;

namespace CustomLegendaryEpicUriHandler
{
    internal class Program
    {
        public static string ExeName => Path.GetFileName(Assembly.GetEntryAssembly()?.Location);

        public static void DisplayHelp()
        {
            Console.WriteLine($"\nUsage: {ExeName} [options]");
            Console.WriteLine("\nOptions:");
            Console.WriteLine("-r, --register\tActivate handler by adding entry to registry");
            Console.WriteLine("-v, --version\tDisplay version information");
            Console.WriteLine("-h, --help\tDisplay this help message");
        }

        public static void DisplayGoodBye()
        {
            Console.WriteLine("Press any key to continue/exit.");
            Console.ReadKey();
        }

        public static async Task Main(string[] args)
        {
            if (args.Length > 0)
            {
                var firstArg = args[0].ToLowerInvariant();
                if (firstArg == "--version" || firstArg == "-v")
                {
                    Console.WriteLine(
                        $"CustomLegendaryEpicUriHandler {Assembly.GetExecutingAssembly().GetName().Version}");
                    return;
                }

                if (firstArg == "--help" || firstArg == "-h")
                {
                    DisplayHelp();
                    return;
                }

                if (firstArg == "--register" || firstArg == "-r")
                {
                    Console.WriteLine("Registering 'com.epicgames.launcher' Uri Handler...");
                    using (var epicKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\com.epicgames.launcher"))
                    {
                        epicKey.SetValue("", "URL:com.epicgames.launcher Protocol");
                        epicKey.SetValue("URL Protocol", "");
                        using (var shellKey = epicKey.CreateSubKey("shell"))
                        using (var openKey = shellKey.CreateSubKey("open"))
                        using (var commandKey = openKey.CreateSubKey("command"))
                        {
                            var handlerExePath = Assembly.GetExecutingAssembly().Location;
                            commandKey.SetValue("", $"\"{handlerExePath}\" \"%1\"");
                            Console.WriteLine("Successfully registered 'com.epicgames.launcher' protocol handler.");
                        }
                    }

                    return;
                }
            }

            if (args.Length == 0)
            {
                DisplayHelp();
                return;
            }

            var urlString = args[0];
            Console.WriteLine($"Received URL: {urlString}");
            try
            {
                var uri = new Uri(urlString);
                var expectedScheme = "com.epicgames.launcher";
                if (uri.Scheme != expectedScheme)
                {
                    Console.WriteLine($"Error: Unexpected scheme '{uri.Scheme}'. Expected '${expectedScheme}'.");
                    DisplayGoodBye();
                    return;
                }

                // --- Extract app name from path ---
                var absolutePath = uri.AbsolutePath;


                if (!string.IsNullOrEmpty(absolutePath))
                {
                    switch (uri.Host)
                    {
                        case "apps":
                        {
                            var appName = absolutePath.Trim('/');
                            Console.WriteLine($"App name: {appName}");

                            // --- Parse query parameters ---
                            var queryParameters =
                                HttpUtility.ParseQueryString(uri.Query);

                            var action = queryParameters["action"];

                            Console.WriteLine($"Action: {action ?? "N/A"}");
                            var legendaryLauncherPath = Path.Combine(LegendarySettings.LauncherPath, "legendary.exe");

                            if (!File.Exists(legendaryLauncherPath))
                            {
                                Console.WriteLine(
                                    $"Error: Legendary launcher not found at '{legendaryLauncherPath}'. Please install or add it to PATH environment variable.");
                                DisplayGoodBye();
                                return;
                            }

                            var launcherArguments = new List<string>();

                            // Customize the arguments based on the parsed URL components
                            if (!string.IsNullOrEmpty(appName))
                            {
                                if (action != null && action.Equals("launch", StringComparison.OrdinalIgnoreCase))
                                {
                                    launcherArguments.AddRange(new[] { "launch", $"{appName}" });
                                    var game = new LegendaryGameInfo.Game
                                    {
                                        App_name = appName
                                    };
                                    var manifest = await LegendarySettings.GetGameInfo(game);
                                    if (manifest?.Game != null)
                                    {
                                        if (!string.IsNullOrEmpty(manifest.Game.External_activation) &&
                                            (manifest.Game.External_activation.ToLower() == "origin" ||
                                             manifest.Game.External_activation.ToLower() == "the ea app"))
                                        {
                                            launcherArguments.Add("--origin");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("No specific app name found in the URL to launch.");
                                DisplayGoodBye();
                            }

                            try
                            {
                                var stdOutBuffer = new StringBuilder();
                                var cmd = Cli.Wrap(legendaryLauncherPath)
                                    .WithArguments(launcherArguments)
                                    .WithEnvironmentVariables(LegendarySettings.DefaultEnvironmentVariables)
                                    .AddCommandToLog()
                                    .WithValidation(CommandResultValidation.None);
                                await foreach (var cmdEvent in cmd.ListenAsync())
                                {
                                    switch (cmdEvent)
                                    {
                                        case StartedCommandEvent started:
                                            Console.WriteLine("Legendary launcher started successfully.");
                                            break;
                                        case StandardErrorCommandEvent stdErr:
                                            stdOutBuffer.AppendLine(stdErr.Text);
                                            break;
                                        case ExitedCommandEvent exited:
                                            if (exited.ExitCode != 0)
                                            {
                                                var errorMessage = stdOutBuffer.ToString();
                                                Console.WriteLine("[Legendary] " + errorMessage);
                                                Console.Error.WriteLine("[Legendary] exit code: " + exited.ExitCode);
                                                DisplayGoodBye();
                                            }

                                            break;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine($"Error launching Legendary: {ex.Message}");
                                DisplayGoodBye();
                            }

                            break;
                        }
                        case "store":
                        {
                            var urlPart = absolutePath.Trim('/');
                            var epicUrl = $"https://store.epicgames.com/{urlPart}";
                            Console.WriteLine($"Final URL: {epicUrl}");
                            Process.Start(epicUrl);
                            break;
                        }
                    }
                }
            }
            catch (UriFormatException ex)
            {
                Console.Error.WriteLine($"Error: Invalid URI format: {ex.Message}");
                DisplayGoodBye();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"An unexpected error occurred: {ex.Message}");
                DisplayGoodBye();
            }
        }
    }
}