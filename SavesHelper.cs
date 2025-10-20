using System;
using System.Diagnostics;
using BulwarkStudios.GameSystems.Ui;
using BulwarkStudios.Stanford.Menu.UI;
using BulwarkStudios.Stanford.Saves;
using BulwarkStudios.Utils.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace IMHelper;

internal static class SavesHelper
{
    private static SettingsHelper.BooleanSetting set;

    internal static void init()
    {
        set = new SettingsHelper.BooleanSetting(Plugin.helperSettingsSection, "Save Utilities",
            "Enables additional info in saves window", false, true);
    }

    internal static void setupLoadMenuItem(UIWindowLoadGameItem item)
    {
        setupMenuItem(item.saveState, item.cycleText, item.transform);
    }

    internal static void setupSaveMenuItem(UIWindowSaveGameItem item)
    {
        setupMenuItem(item.saveState, item.cycleText, item.transform);
    }

    private static void setupMenuItem(SaveState saveState, TMP_Text cycleText, Transform transform)
    {
        if (!set.getValue()) return;
        var path = saveState.GetPath();
        cycleText.text = cycleText.text + "    \t\tID: " + path.Split("\\")[2];
        var buttonTemplate = transform.FindChild("ButtonContainer/Load") == null
            ? transform.FindChild("ButtonContainer/Save")
            : transform.FindChild("ButtonContainer/Load");
        var openButton = Object.Instantiate(buttonTemplate, buttonTemplate.transform.parent);
        openButton.name = "Path";
        openButton.FindChild("Text (TMP)").GetComponent<TextMeshProUGUI>().text = "Folder";
        openButton.GetComponent<LayoutElement>().m_PreferredWidth = 100;
        openButton.GetComponent<UiButtonTriggerUnityEvent>().enabled = false;
        openButton.GetComponent<UiButtonEffectFmodTrigger>().enabled = false;
        openButton.GetComponent<UiButton>().add_OnTriggered(new Action(delegate { openPath(saveState.GetPath()); }));
    }

    private static void openPath(string path)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true,
            Verb = "open"
        });
    }
}