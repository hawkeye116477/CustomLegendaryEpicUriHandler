using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
using CustomLegendaryEpicUriHandler.Models;
using Newtonsoft.Json;

namespace CustomLegendaryEpicUriHandler
{
    public class LegendarySettings
    {
        public static string PluginPath { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Playnite", "ExtensionsData",
            "ead65c3b-2f8f-4e37-b4e6-b3de6be540c6");

        public static string ClientExecPath
        {
            get
            {
                var path = LauncherPath;
                return string.IsNullOrEmpty(path) ? string.Empty : GetExecutablePath(path);
            }
        }

        public static string ConfigPath
        {
            get
            {
                var legendaryConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".config", "legendary");
                var heroicLegendaryConfigPath =
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "heroic",
                        "legendaryConfig", "legendary");
                var originalLegendaryinstallListPath = Path.Combine(legendaryConfigPath, "installed.json");
                var heroicLegendaryInstallListPath = Path.Combine(heroicLegendaryConfigPath, "installed.json");
                if (File.Exists(heroicLegendaryInstallListPath))
                {
                    if (File.Exists(originalLegendaryinstallListPath))
                    {
                        if (File.GetLastWriteTime(heroicLegendaryInstallListPath) >
                            File.GetLastWriteTime(originalLegendaryinstallListPath))
                        {
                            legendaryConfigPath = heroicLegendaryConfigPath;
                        }
                    }
                    else
                    {
                        legendaryConfigPath = heroicLegendaryConfigPath;
                    }
                }

                var envLegendaryConfigPath = Environment.GetEnvironmentVariable("LEGENDARY_CONFIG_PATH");
                if (!string.IsNullOrWhiteSpace(envLegendaryConfigPath) && Directory.Exists(envLegendaryConfigPath))
                {
                    legendaryConfigPath = envLegendaryConfigPath;
                }

                return legendaryConfigPath;
            }
        }

        public static Dictionary<string, string> DefaultEnvironmentVariables
        {
            get
            {
                var envDict = new Dictionary<string, string>();
                var heroicLegendaryConfigPath =
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "heroic",
                        "legendaryConfig", "legendary");
                if (ConfigPath == heroicLegendaryConfigPath)
                {
                    envDict.Add("LEGENDARY_CONFIG_PATH", ConfigPath);
                }

                return envDict;
            }
        }

        public static LegendaryPluginSettings PlaynitePluginSettings
        {
            get
            {
                var pluginSettingsPath = Path.Combine(PluginPath, "config.json");
                var legendaryPluginSettings = new LegendaryPluginSettings();
                using (var file = File.OpenText(pluginSettingsPath))
                {
                    var serializer = new JsonSerializer();
                    legendaryPluginSettings =
                        (LegendaryPluginSettings)serializer.Deserialize(file, typeof(LegendaryPluginSettings));
                }

                return legendaryPluginSettings;
            }
        }

        public static string HeroicLegendaryPath
        {
            get
            {
                var heroicResourcesBasePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Programs\heroic\resources\app.asar.unpacked\build\bin");
                var path = Path.Combine(heroicResourcesBasePath, @"win32\");
                if (!Directory.Exists(path))
                {
                    path = Path.Combine(heroicResourcesBasePath, @"x64\win32\");
                }

                return path;
            }
        }

        public static string LauncherPath
        {
            get
            {
                var launcherPath = "";
                var envPath = Environment.GetEnvironmentVariable("PATH")
                    ?.Split(';')
                    .Select(x => Path.Combine(x))
                    .FirstOrDefault(x => File.Exists(Path.Combine(x, "legendary.exe")));
                if (string.IsNullOrWhiteSpace(envPath) == false)
                {
                    launcherPath = envPath;
                }
                else if (File.Exists(Path.Combine(HeroicLegendaryPath, "legendary.exe")))
                {
                    launcherPath = HeroicLegendaryPath;
                }
                else
                {
                    var pf64 = Environment.GetEnvironmentVariable("ProgramW6432");
                    if (string.IsNullOrEmpty(pf64))
                    {
                        pf64 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                    }

                    launcherPath = Path.Combine(pf64, "Legendary");
                    if (!File.Exists(Path.Combine(launcherPath, "legendary.exe")))
                    {
                        launcherPath = Directory
                            .GetParent(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty)
                            ?.FullName;
                    }
                }

                var savedSettings = PlaynitePluginSettings;
                if (savedSettings != null && savedSettings.SelectedLauncherPath != "" &&
                    File.Exists(savedSettings.SelectedLauncherPath))
                {
                    launcherPath = savedSettings.SelectedLauncherPath;
                }

                if (launcherPath != null && !File.Exists(Path.Combine(launcherPath, "legendary.exe")))
                {
                    launcherPath = "";
                }

                return launcherPath;
            }
        }

        internal static string GetExecutablePath(string rootPath)
        {
            return Path.Combine(rootPath, "legendary.exe");
        }

        public static async Task<LegendaryGameInfo.Rootobject> GetGameInfo(LegendaryGameInfo.Game installData)
        {
            var gameID = installData.App_name;
            var manifest = new LegendaryGameInfo.Rootobject();
            var cacheInfoPath = Path.Combine(PluginPath, "infocache");
            var cacheInfoFile = Path.Combine(cacheInfoPath, gameID + ".json");
            if (!Directory.Exists(cacheInfoPath))
            {
                Directory.CreateDirectory(cacheInfoPath);
            }

            var correctJson = false;
            if (File.Exists(cacheInfoFile))
            {
                var content = File.ReadAllText(cacheInfoFile);

                if (!string.IsNullOrWhiteSpace(content))
                {
                    try
                    {
                        var serializer = new JsonSerializer();
                        manifest = JsonConvert.DeserializeObject<LegendaryGameInfo.Rootobject>(content);
                        if (manifest != null && manifest.Game != null)
                        {
                            correctJson = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex.Message);
                    }
                }
            }

            if (!correctJson)
            {
                BufferedCommandResult result;
                if (gameID == "eos-overlay")
                {
                    result = await Cli.Wrap(ClientExecPath)
                        .WithArguments(new[] { "eos-overlay", "install" })
                        .WithEnvironmentVariables(DefaultEnvironmentVariables)
                        .WithStandardInputPipe(PipeSource.FromString("n"))
                        .AddCommandToLog()
                        .WithValidation(CommandResultValidation.None)
                        .ExecuteBufferedAsync();
                }
                else
                {
                    result = await Cli.Wrap(ClientExecPath)
                        .WithArguments(new[] { "info", gameID, "--json" })
                        .WithEnvironmentVariables(DefaultEnvironmentVariables)
                        .AddCommandToLog()
                        .WithValidation(CommandResultValidation.None)
                        .ExecuteBufferedAsync();
                }

                var errorMessage = result.StandardError;
                if (result.ExitCode != 0)
                {
                    Console.Error.WriteLine("[Legendary]" + result.StandardError);
                    manifest.errorDisplayed = true;
                }
                else
                {
                    File.WriteAllText(cacheInfoFile, result.StandardOutput);
                    manifest = JsonConvert.DeserializeObject<LegendaryGameInfo.Rootobject>(result.StandardOutput);
                }
            }

            return manifest;
        }
    }
}