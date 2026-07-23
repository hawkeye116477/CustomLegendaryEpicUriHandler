using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
using CustomLegendaryEpicUriHandler.Models;
using Newtonsoft.Json;

namespace CustomLegendaryEpicUriHandler
{
    public class LegendarySettings
    {
        public static string PluginPath { get; set; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "Playnite", "ExtensionsData",
                         "ead65c3b-2f8f-4e37-b4e6-b3de6be540c6");

        public static string ClientExecPath
        {
            get
            {
                var path = LauncherPath;
                return string.IsNullOrEmpty(path) ? string.Empty : path;
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
                var originalLegendaryInstallListPath = Path.Combine(legendaryConfigPath, "installed.json");
                var heroicLegendaryInstallListPath = Path.Combine(heroicLegendaryConfigPath, "installed.json");
                if (File.Exists(heroicLegendaryInstallListPath))
                {
                    if (File.Exists(originalLegendaryInstallListPath))
                    {
                        if (File.GetLastWriteTime(heroicLegendaryInstallListPath) >
                            File.GetLastWriteTime(originalLegendaryInstallListPath))
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
                var settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                              "CustomLegendaryEpicUriHandler");
                if (!File.Exists(settingsPath))
                {
                    settingsPath  = Path.Combine(PluginPath, "config.json");
                }
                var legendaryPluginSettings = new LegendaryPluginSettings();
                if (File.Exists(settingsPath))
                {
                    using (var file = File.OpenText(settingsPath))
                    {
                        var serializer = new JsonSerializer();
                        legendaryPluginSettings =
                            (LegendaryPluginSettings)serializer.Deserialize(file, typeof(LegendaryPluginSettings))!;
                    }
                }

                return legendaryPluginSettings;
            }
        }

        public static string HeroicLegendaryPath
        {
            get
            {
                var heroicPath = Path.GetDirectoryName(UninstallProgramList.GetUnistallProgramsList().FirstOrDefault(p => p.DisplayName?.StartsWith("Heroic") == true
                    && p.Publisher == "Heroic Games Launcher")?.DisplayIcon?.Split(',')[0]);
                if (string.IsNullOrEmpty(heroicPath))
                {
                    heroicPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        @"Programs\heroic");
                }
                var heroicResourcesBasePath = Path.Combine(@$"{heroicPath}\resources\app.asar.unpacked\build\bin");
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
                string[] validLegendaryBinaries = ["legendary_windows_x86_64.exe", "legendary.exe"];
                var launcherPath = "";
                string? envPath = Environment.GetEnvironmentVariable("PATH")?
                                             .Split([Path.PathSeparator], StringSplitOptions.RemoveEmptyEntries)
                                             .Where(p => p.IndexOfAny(Path.GetInvalidPathChars()) < 0)
                                             .SelectMany(pathEntry => validLegendaryBinaries.Select(legendaryBinary => Path.Combine(pathEntry.Trim(), legendaryBinary)))
                                             .FirstOrDefault(File.Exists);
                if (!string.IsNullOrWhiteSpace(envPath))
                {
                    launcherPath = envPath;
                }
                else
                {
                    var launcherMatches = validLegendaryBinaries.Select(legendaryBinary => Path.Combine(HeroicLegendaryPath, legendaryBinary)).Where(File.Exists).ToList();
                    if (launcherMatches.Count == 0)
                    {
                        var pf64 = Environment.GetEnvironmentVariable("ProgramW6432");
                        if (string.IsNullOrEmpty(pf64))
                        {
                            pf64 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                        }
                        var launcherBasePath = Path.Combine(pf64, "Legendary");
                        launcherMatches = validLegendaryBinaries.Select(legendaryBinary => Path.Combine(launcherBasePath, legendaryBinary)).Where(File.Exists).ToList();
                    }
                    if (launcherMatches.Count > 0)
                    {
                        launcherPath = launcherMatches.First();
                    }
                }

                var savedSettings = PlaynitePluginSettings;
                if (savedSettings.SelectedFullLauncherPath != "" &&
                    File.Exists(savedSettings.SelectedFullLauncherPath))
                {
                    launcherPath = savedSettings.SelectedFullLauncherPath;
                }

                if (string.IsNullOrEmpty(launcherPath) || !File.Exists(launcherPath))
                {
                    launcherPath = "";
                }

                return launcherPath;
            }
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
                result = await Cli.Wrap(ClientExecPath)
                                  .WithArguments(["info", gameID, "--json"])
                                  .WithEnvironmentVariables(DefaultEnvironmentVariables!)
                                  .AddCommandToLog()
                                  .WithValidation(CommandResultValidation.None)
                                  .ExecuteBufferedAsync();
                if (result.ExitCode != 0)
                {
                    Console.Error.WriteLine("[Legendary]" + result.StandardError);
                    manifest?.errorDisplayed = true;
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