using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Web;

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
        public static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                string firstArg = args[0].ToLowerInvariant();
                if (firstArg == "--version" || firstArg == "-v")
                {
                    Console.WriteLine($"\nCustomLegendaryEpicUriHandler {Assembly.GetExecutingAssembly().GetName().Version}");
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
                    Console.WriteLine("Press any key to exit.");
                    Console.ReadKey();
                    return;
                }

                // --- Extract app name from path ---
                string appName = string.Empty;
                string absolutePath = uri.AbsolutePath;
                
                
                if (uri.Host == "apps" && (!string.IsNullOrEmpty(absolutePath)))
                {
                   appName = absolutePath.Trim('/');
                }

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
                    Console.WriteLine("Press any key to exit.");
                    Console.ReadKey();
                    return;
                }

                string launcherArguments = string.Empty;

                // Customize the arguments based on the parsed URL components
                if (!string.IsNullOrEmpty(appName))
                {
                    if (action != null && action.Equals("launch", StringComparison.OrdinalIgnoreCase))
                    {
                        launcherArguments += $"launch {appName} --origin";
                    }
                }
                else
                {
                    Console.WriteLine("No specific app name found in the URL to launch.");
                    Console.WriteLine("Launching Legendary without specific game arguments.");
                }

                Console.WriteLine($"\nLaunching Legendary:");
                Console.WriteLine($"Executable: {legendaryLauncherPath}");
                Console.WriteLine($"Arguments: {launcherArguments}");

                try
                {
                    Process.Start(legendaryLauncherPath, launcherArguments);
                    Console.WriteLine("Legendary launcher started successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error launching Legendary: {ex.Message}");
                }
            }
            catch (UriFormatException ex)
            {
                Console.WriteLine($"Error: Invalid URI format: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
            }

            Console.WriteLine("\nProcessing complete. Press any key to exit.");
            Console.ReadKey();
        }
    }
}