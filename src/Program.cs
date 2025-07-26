using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using CliWrap;
using CliWrap.EventStream;
using CustomLegendaryEpicUriHandler.Models;

namespace CustomLegendaryEpicUriHandler
{
    internal class Program
    {
        public static string ExeName => Path.GetFileName(Assembly.GetEntryAssembly()?.Location);

        public static void DisplayHelp()
        {
            Console.WriteLine($"\nUsage: {ExeName} [options]");
            Console.WriteLine("\nOptions:");
            Console.WriteLine($"-v, --version\tDisplay version information");
            Console.WriteLine($"-h, --help\tDisplay this help message");
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
                string firstArg = args[0].ToLowerInvariant();
                if (firstArg == "--version" || firstArg == "-v")
                {
                    Console.WriteLine(
                        $"\nCustomLegendaryEpicUriHandler {Assembly.GetExecutingAssembly().GetName().Version}");
                    return;
                }

                if (firstArg == "--help" || firstArg == "-h")
                {
                    DisplayHelp();
                    return;
                }
            }

            if (args.Length == 0)
            {
                DisplayHelp();
                return;
            }

            string urlString = args[0];
            Console.WriteLine($"Received URL: {urlString}");
            try
            {
                Uri uri = new Uri(urlString);
                string expectedScheme = "com.epicgames.launcher";
                if (uri.Scheme != expectedScheme)
                {
                    Console.WriteLine($"Error: Unexpected scheme '{uri.Scheme}'. Expected '${expectedScheme}'.");
                    DisplayGoodBye();
                    return;
                }

                // --- Extract app name from path ---
                string absolutePath = uri.AbsolutePath;


                if (uri.Host == "apps" && (!string.IsNullOrEmpty(absolutePath)))
                {
                    string appName = absolutePath.Trim('/');
                    Console.WriteLine($"App name: {appName}");

                    // --- Parse query parameters ---
                    System.Collections.Specialized.NameValueCollection queryParameters =
                        HttpUtility.ParseQueryString(uri.Query);

                    string action = queryParameters["action"];

                    Console.WriteLine($"Action: {action ?? "N/A"}");
                    string legendaryLauncherPath = Path.Combine(LegendarySettings.LauncherPath, "legendary.exe");

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
                            LegendaryGameInfo.Game game = new LegendaryGameInfo.Game
                            {
                                App_name = appName
                            };
                            var manifest = await LegendarySettings.GetGameInfo(game);
                            if (manifest != null && manifest.Game != null)
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