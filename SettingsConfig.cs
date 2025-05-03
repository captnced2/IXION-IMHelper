using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace IMHelper;

internal static class SettingsConfig
{
    private static readonly string configFilePath =
        Plugin.config.ConfigFilePath.Insert(Plugin.config.ConfigFilePath.Length - 4, ".settings");

    internal static void saveSetting(string section, string setting, string value)
    {
        var allSettings = getAllConfigSettings();
        if (allSettings is null) allSettings = new List<configSettingEntry>();
        foreach (var configSetting in allSettings)
            if (configSetting.Section.Equals(section) && configSetting.Setting.Equals(setting))
            {
                configSetting.Value = value;
                goto Write;
            }

        var newConfigSettingEntry = new configSettingEntry();
        newConfigSettingEntry.Section = section;
        newConfigSettingEntry.Setting = setting;
        newConfigSettingEntry.Value = value;
        allSettings.Add(newConfigSettingEntry);

        Write:
        {
            File.WriteAllText(configFilePath, JsonSerializer.Serialize(allSettings));
        }
        Plugin.Log.LogInfo("Setting \"" + section + "." + setting + "\" set to \"" + value + "\"");
    }

    internal static string loadSetting(string section, string setting)
    {
        var allSettings = getAllConfigSettings();
        if (allSettings == null) return null;
        foreach (var configSetting in allSettings)
            if (configSetting.Section.Equals(section) && configSetting.Setting.Equals(setting))
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
            if (!(configEntry.Section == null || configEntry.Setting == null || configEntry.Value == null))
                allSettings.Add(configEntry);
        return allSettings;
    }

    internal class configSettingEntry
    {
        public string Section { get; set; }
        public string Setting { get; set; }
        public string Value { get; set; }
    }
}