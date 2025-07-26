using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CustomLegendaryEpicUriHandler.Models;
using Newtonsoft.Json;

namespace CustomLegendaryEpicUriHandler
{
    public class LegendarySettings
    {
        public static LegendaryPluginSettings PlaynitePluginSettings
        {
            get
            {
                var pluginSettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Playnite", "ExtensionsData", "ead65c3b-2f8f-4e37-b4e6-b3de6be540c6", "config.json");
                LegendaryPluginSettings legendaryPluginSettings = new LegendaryPluginSettings();
                using (StreamReader file = File.OpenText(pluginSettingsPath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    legendaryPluginSettings = (LegendaryPluginSettings)serializer.Deserialize(file, typeof(LegendaryPluginSettings));
                }
                return legendaryPluginSettings;
            }
        }
        public static string HeroicLegendaryPath
        {
            get
            {
                var heroicResourcesBasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
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
                                         .Split(';')
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
                        launcherPath = Directory.GetParent(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty)?.FullName;
                    }
                }
                
                var savedSettings = PlaynitePluginSettings;
                if (savedSettings.SelectedLauncherPath != "" && File.Exists(savedSettings.SelectedLauncherPath))
                {
                    launcherPath = savedSettings.SelectedLauncherPath;
                }
                if (!File.Exists(Path.Combine(launcherPath, "legendary.exe")))
                {
                    launcherPath = "";
                }
                return launcherPath;
            }
        }

    }
}