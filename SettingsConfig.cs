using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace IMHelper;

internal static class SettingsConfig
{
    private static readonly string configFilePath =
        Plugin.config.ConfigFilePath.Insert(Plugin.config.ConfigFilePath.Length - 4, ".settings");

    internal static void saveSetting(string path, string value)
    {
        var allSettings = getAllConfigSettings() ?? [];
        foreach (var configSetting in allSettings)
            if (configSetting.Path == path)
            {
                configSetting.Value = value;
                goto Write;
            }

        var newConfigSettingEntry = new configSettingEntry
        {
            Path = path,
            Value = value
        };
        allSettings.Add(newConfigSettingEntry);

        Write:
        {
            File.WriteAllText(configFilePath, JsonSerializer.Serialize(allSettings));
        }
        Plugin.Log.LogInfo("Setting \"" + path + "\" set to \"" + value + "\"");
    }

    internal static string loadSetting(string path)
    {
        var allSettings = getAllConfigSettings();
        if (allSettings == null) return null;
        foreach (var configSetting in allSettings)
            if (configSetting.Path == path)
                return configSetting.Value;

        return null;
    }

    private static List<configSettingEntry> getAllConfigSettings()
    {
        if (!File.Exists(configFilePath)) return null;
        List<configSettingEntry> allEntries;
        try
        {
            allEntries = JsonSerializer.Deserialize<List<configSettingEntry>>(File.ReadAllText(configFilePath));
        }
        catch (Exception)
        {
            return null;
        }

        var allSettings = new List<configSettingEntry>();
        foreach (var configEntry in allEntries)
            if (!(configEntry.Path == null || configEntry.Value == null))
                allSettings.Add(configEntry);
        return allSettings;
    }

    private class configSettingEntry
    {
        public string Path { get; init; }
        public string Value { get; set; }
    }
}