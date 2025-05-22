using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace IMHelper;

internal static class ModsMenuConfig
{
    private static readonly string configFilePath =
        Plugin.config.ConfigFilePath.Insert(Plugin.config.ConfigFilePath.Length - 4, ".mods");

    internal static void saveMod(string guid, bool enabled)
    {
        var allMods = getAllMods() ?? [];
        foreach (var entry in allMods)
        {
            if (!entry.guid.Equals(guid)) continue;
            entry.enabled = enabled;
            goto Write;
        }

        var newModEntry = new modEntry();
        newModEntry.guid = guid;
        newModEntry.enabled = enabled;
        allMods.Add(newModEntry);

        Write:
        {
            File.WriteAllText(configFilePath, JsonSerializer.Serialize(allMods));
        }
    }

    internal static bool getMod(string guid)
    {
        var allMods = getAllMods();
        if (allMods == null) return true;
        foreach (var modEntry in allMods)
            if (modEntry.guid.Equals(guid))
                return modEntry.enabled;
        return true;
    }

    private static List<modEntry> getAllMods()
    {
        if (!File.Exists(configFilePath)) return null;
        List<modEntry> allEntries;
        try
        {
            allEntries = JsonSerializer.Deserialize<List<modEntry>>(File.ReadAllText(configFilePath));
        }
        catch (Exception)
        {
            return null;
        }

        var allMods = new List<modEntry>();
        foreach (var modEntry in allEntries)
            if (modEntry.guid != null)
                allMods.Add(modEntry);
        return allMods;
    }

    private class modEntry
    {
        public string guid { get; set; }
        public bool enabled { get; set; }
    }
}