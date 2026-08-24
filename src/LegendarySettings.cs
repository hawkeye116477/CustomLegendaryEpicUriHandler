using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
using CustomLegendaryEpicUriHandler.Models;

namespace CustomLegendaryEpicUriHandler
{
    public class LegendarySettings
    {
        private static string PluginPath { get; set; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Playnite", "ExtensionsData",
                "ead65c3b-2f8f-4e37-b4e6-b3de6be540c6");

        private static string ClientExecPath
        {
            get
            {
                var path = LauncherPath;
                return string.IsNullOrEmpty(path) ? string.Empty : path;
            }
        }

        private static string ConfigPath
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

        private static LegendaryPluginSettings PlaynitePluginSettings
        {
            get
            {
                var settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "CustomLegendaryEpicUriHandler");
                if (!File.Exists(settingsPath))
                {
                    settingsPath = Path.Combine(PluginPath, "config.json");
                }

                var legendaryPluginSettings = new LegendaryPluginSettings();
                if (!File.Exists(settingsPath))
                {
                    return legendaryPluginSettings;
                }

                var content = File.ReadAllText(settingsPath);
                if (string.IsNullOrEmpty(content) || !Serialization.TryFromJson<LegendaryPluginSettings>(content, out var newLegendaryPluginSettings))
                {
                    return legendaryPluginSettings;
                }

                if (newLegendaryPluginSettings != null)
                {
                    legendaryPluginSettings = newLegendaryPluginSettings;
                }

                return legendaryPluginSettings;
            }
        }

        private static string HeroicLegendaryPath
        {
            get
            {
                var heroicPath = Path.GetDirectoryName(UninstallProgramList.GetUnistallProgramsList()
                                                                           .FirstOrDefault(p => p.DisplayName?.StartsWith("Heroic") == true
                                                                                && p.Publisher == "Heroic Games Launcher")
                                                                          ?.DisplayIcon?.Split(',')[0]);
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
                string[] validLegendaryBinaries = ["legendary_windows_x86_64.exe", "legendary_windows_x64.exe", "legendary.exe"];
                var launcherPath = "";
                string? envPath = Environment.GetEnvironmentVariable("PATH")?
                                             .Split([Path.PathSeparator], StringSplitOptions.RemoveEmptyEntries)
                                             .Where(p => p.IndexOfAny(Path.GetInvalidPathChars()) < 0)
                                             .SelectMany(pathEntry => validLegendaryBinaries.Select(legendaryBinary =>
                                                  Path.Combine(pathEntry.Trim(), legendaryBinary)))
                                             .FirstOrDefault(File.Exists);
                if (!string.IsNullOrWhiteSpace(envPath))
                {
                    launcherPath = envPath;
                }
                else
                {
                    var launcherMatches = validLegendaryBinaries
                                         .Select(legendaryBinary => Path.Combine(HeroicLegendaryPath, legendaryBinary))
                                         .Where(File.Exists)
                                         .ToList();
                    if (launcherMatches.Count == 0)
                    {
                        var pf64 = Environment.GetEnvironmentVariable("ProgramW6432");
                        if (string.IsNullOrEmpty(pf64))
                        {
                            pf64 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                        }

                        var launcherBasePath = Path.Combine(pf64, "Legendary");
                        launcherMatches = validLegendaryBinaries.Select(legendaryBinary => Path.Combine(launcherBasePath, legendaryBinary))
                                                                .Where(File.Exists)
                                                                .ToList();
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

        public static async Task<LegendaryGameInfo.Rootobject?> GetGameInfo(LegendaryGameInfo.Game installData)
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
                var content = await File.ReadAllTextAsync(cacheInfoFile);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    if (Serialization.TryFromJson<LegendaryGameInfo.Rootobject>(content, out var newManifest))
                    {
                        if (newManifest is { Game: not null })
                        {
                            manifest = newManifest;
                            correctJson = true;
                        }
                    }
                }
            }

            if (gameID == null)
            {
                return null;
            }

            if (correctJson)
            {
                return manifest;
            }

            var result = await Cli.Wrap(ClientExecPath)
                                  .WithArguments(["info", gameID, "--json"])
                                  .WithEnvironmentVariables(DefaultEnvironmentVariables!)
                                  .AddCommandToLog()
                                  .WithValidation(CommandResultValidation.None)
                                  .ExecuteBufferedAsync();
            if (result.ExitCode != 0)
            {
                await Console.Error.WriteLineAsync("[Legendary]" + result.StandardError);
                manifest.ErrorDisplayed = true;
            }
            else
            {
                await File.WriteAllTextAsync(cacheInfoFile, result.StandardOutput);
                if (Serialization.TryFromJson<LegendaryGameInfo.Rootobject>(result.StandardOutput, out var newManifest))
                {
                    manifest = newManifest;
                }
            }

            return manifest;
        }
    }
}