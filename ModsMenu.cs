using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils;
using BulwarkStudios.GameSystems.Ui;
using BulwarkStudios.Stanford.Core.UI;
using BulwarkStudios.Stanford.Utils.Extensions;
using BulwarkStudios.Utils.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace IMHelper;

public static class ModsMenu
{
    private static bool initialized;
    private static bool isOpen;
    private static Transform window;
    private static Transform backgroundBlocker;
    private static readonly List<KeyValuePair<string, PluginInfo>> pluginsList = [];

    internal static void mainMenuListener()
    {
        var menu = GameObject.Find("Canvas/1920x1080/Canvas Group/Default/Menu");
        var modsButton = new ButtonHelper.TextButton("Mods", menu.transform.FindChild("Button_Credits"), menu.transform,
            modsMenuTriggered, "Mods");
        modsButton.createButton();
        modsButton.buttonTransform.SetSiblingIndex(2);
        initialized = false;
        getLoadedPlugins();
    }

    private static void modsMenuTriggered(Transform buttonTransform)
    {
        setupWindow();
        openWindow();
    }

    private static void setupWindow()
    {
        if (initialized) return;
        window = Object.Instantiate(GameObject.Find("Canvas/WindowManager/1920x1080/UI Window Load Game"),
            GameObject.Find("Canvas/WindowManager/1920x1080").transform).transform;
        window.name = "Ui Window Mods";
        window.gameObject.SetActive(false);
        window.GetComponent<Canvas>().enabled = true;
        window.GetComponent<GraphicRaycaster>().enabled = true;
        window.transform.FindChild("Container/UI Window Header/Title").GetComponent<TextMeshProUGUI>().text = "Mods";
        var closeButton = window.transform.FindChild("Container/UI Window Header/Close Button");
        closeButton.GetComponent<UiButtonTriggerUnityEvent>().enabled = false;
        closeButton.GetComponent<UiButton>().add_OnTriggered(new Action(delegate { closeWindow(); }));
        backgroundBlocker = GameObject.Find("Canvas/WindowManager").transform.parent.FindChild("BackgroundUiBlocker");
        KeyListenerHelper.addMainMenuKeyListener(KeyCode.Escape, closeWindow);
        window.FindChild("Container/Scroll View").GetComponent<ScrollRect>().enabled = true;
        window.FindChild("Container/Scroll View/Viewport").GetComponent<RectMask2D>().enabled = true;
        setupPluginsList();
        initialized = true;
    }

    private static void setupPluginsList()
    {
        var viewportContent = window.FindChild("Container/Scroll View/Viewport/Content");
        var pluginElementTemplate =
            Object.Instantiate(
                GameObject.Find("GameSettingsManager/UIGameInputSetting").transform
                    .FindChild("UIGameInputSetting(Clone)"), null);
        pluginElementTemplate.name = "PluginElement";
        pluginElementTemplate.gameObject.SetActive(true);
        var pluginSeparatorTemplate =
            Object.Instantiate(
                GameObject.Find("GameSettingsManager/UIGameSettingsSeparator").transform
                    .FindChild("UIGameSettingsSeparator(Clone)"), null);
        pluginSeparatorTemplate.name = "PluginSeparator";
        pluginSeparatorTemplate.gameObject.SetActive(true);
        var templateButton = pluginElementTemplate.transform.FindChild("UI Binding Press");
        templateButton.GetComponent<UiButtonTriggerUnityEvent>().enabled = false;
        templateButton.FindChild("Text (TMP)").GetComponent<TextMeshProUGUI>().text = "Enable";
        Transform separator = null;
        foreach (var keyValuePair in pluginsList)
        {
            var element = Object.Instantiate(pluginElementTemplate, viewportContent);
            separator = Object.Instantiate(pluginSeparatorTemplate, viewportContent);
            var text = element.transform.FindChild("Text (TMP)").GetComponent<TextMeshProUGUI>();
            text.text = keyValuePair.Key;
            text.fontSize = 16;
            var button = element.transform.FindChild("UI Binding Press").GetComponent<UiButton>();
            button.add_OnTriggered(new Action(delegate { buttonTriggered(keyValuePair.Value, element); }));
            setButtonState(element,
                hasEnableMethod(keyValuePair.Value)
                    ? ModsMenuConfig.getMod(keyValuePair.Value.Metadata.GUID) ? 1 : 0
                    : -1, keyValuePair.Value.Metadata.GUID);
            element.transform.FindChild("UI Binding Press").GetComponent<LayoutElement>().m_MinWidth = 120;
            element.transform.FindChild("Text (TMP)").GetComponent<LayoutElement>().m_PreferredWidth = 670;
        }

        separator?.gameObject.SetActive(false);
    }

    private static void buttonTriggered(PluginInfo plugin, Transform buttonTransform)
    {
        var enabled = !ModsMenuConfig.getMod(plugin.Metadata.GUID);
        var type = plugin.Instance.GetType();
        var method = type.GetMethod("enable");
        if (method != null)
        {
            if (method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(bool))
            {
                try
                {
                    method.Invoke(Activator.CreateInstance(type), [enabled]);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError("Error while " + (enabled ? "enabling" : "disabling") + " plugin \"" +
                                        plugin.Metadata.GUID + "\": " + e);
                }

                ModsMenuConfig.saveMod(plugin.Metadata.GUID, enabled);
                setButtonState(buttonTransform, enabled ? 1 : 0, plugin.Metadata.GUID);
                Plugin.Log.LogInfo(enabled
                    ? "Successfully enabled plugin \"" + plugin.Metadata.GUID + "\""
                    : "Successfully disabled plugin \"" + plugin.Metadata.GUID + "\"");
            }
            else
            {
                Plugin.Log.LogInfo("Plugin " + type.Assembly.GetName().Name +
                                   " has a wrong enable function signature");
            }
        }
        else
        {
            Plugin.Log.LogInfo("Cannot disable Plugin " + plugin.Metadata.Name +
                               " as it has no enable function (this should not happen)");
        }
    }

    private static bool hasEnableMethod(PluginInfo plugin)
    {
        var method = plugin.Instance.GetType().GetMethod("enable");
        return method != null;
    }

    private static void setButtonState(Transform button, int pluginEnabled, string guid)
    {
        var bindingPress = button.FindChild("UI Binding Press");
        var buttonText = bindingPress.FindChild("Text (TMP)").GetComponent<TextMeshProUGUI>();
        var border = bindingPress.FindChild("Border").GetComponent<Image>();
        var hover = button.FindChild("Text (TMP)").GetComponent<UITooltipHoverHelper>();
        var colorText = Color.white;
        var colorButton = new Color(255, 250, 215, 255);
        switch (pluginEnabled)
        {
            case 1:
                buttonText.text = "Disable";
                hover.text = guid + " : Enabled";
                break;
            case 0:
                buttonText.text = "Enable";
                hover.text = guid + " : Disabled";
                colorText = Color.gray;
                colorButton = Color.gray;
                break;
            case -1:
                buttonText.text = "-";
                hover.text = guid + " : Enabled";
                bindingPress.DestroyChildren();
                break;
        }

        buttonText.SetFaceColor(colorButton);
        button.FindChild("Text (TMP)").GetComponent<TextMeshProUGUI>().color = colorText;
        border.color = colorButton;
    }

    private static void getLoadedPlugins()
    {
        pluginsList.Clear();
        foreach (var plugin in IL2CPPChainloader.Instance.Plugins)
        {
            if (plugin.Value.Metadata.GUID == Plugin.Guid) continue;
            pluginsList.Add(new KeyValuePair<string, PluginInfo>(plugin.Value.Metadata.Name, plugin.Value));
        }
    }

    public static bool isSelfEnabled()
    {
        var assembly = Plugin.getCallingAssembly();
        foreach (var type in assembly.GetTypes())
            if (typeof(BasePlugin).IsAssignableFrom(type))
            {
                var meta = MetadataHelper.GetMetadata(type);
                return ModsMenuConfig.getMod(meta.GUID);
            }

        return true;
    }

    private static void openWindow()
    {
        if (isOpen) return;
        window.gameObject.SetActive(true);
        backgroundBlocker.gameObject.SetActive(true);
        backgroundBlocker.GetComponent<UIWindowClickBlocker>().enabled = false;
        Plugin.monoHelper.StartCoroutine(fadeContainer(true, window.transform.FindChild("Container")));
        Plugin.monoHelper.StartCoroutine(fadeBackground(true, backgroundBlocker));
        isOpen = true;
    }

    private static void closeWindow()
    {
        if (!isOpen) return;
        Plugin.monoHelper.StartCoroutine(fadeContainer(false, window.transform.FindChild("Container")));
        backgroundBlocker.GetComponent<UIWindowClickBlocker>().enabled = true;
        isOpen = false;
    }

    private static IEnumerator fadeContainer(bool open, Transform container)
    {
        if (open)
        {
            for (var scale = 0.95f; scale <= 1; scale += 0.005f)
            {
                container.localScale = new Vector3(scale, scale, scale);
                yield return null;
            }
        }
        else
        {
            for (var scale = 1f; scale >= 0.95f; scale -= 0.01f)
            {
                container.localScale = new Vector3(scale, scale, scale);
                yield return null;
            }

            Plugin.monoHelper.StartCoroutine(fadeBackground(false, backgroundBlocker));
            window.gameObject.SetActive(false);
        }
    }

    private static IEnumerator fadeBackground(bool open, Transform background)
    {
        var backgroundImage = background.GetComponent<Image>();
        if (open)
        {
            for (var alpha = 0f; alpha <= 0.99f; alpha += 0.12f)
            {
                backgroundImage.color = new Color(0, 0, 0, alpha);
                yield return null;
            }

            backgroundImage.color = new Color(0, 0, 0, 0.99f);
        }
        else
        {
            for (var alpha = 0.99f; alpha >= 0f; alpha -= 0.10f)
            {
                backgroundImage.color = new Color(0, 0, 0, alpha);
                yield return null;
            }

            backgroundBlocker.gameObject.SetActive(false);
        }

        yield return null;
    }
}