using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using BulwarkStudios.GameSystems.Ui;
using BulwarkStudios.Stanford.Core.UI;
using BulwarkStudios.Stanford.Utils.Extensions;
using BulwarkStudios.Utils.UI;
using Stanford.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace IMHelper;

#nullable enable
public static class SettingsHelper
{
    public enum SettingType
    {
        Key,
        Bool,
        Slider,
        NumberInput
    }

    private static readonly List<SettingsSection> topSections = [];
    private static readonly List<KeySetting> allKeySettings = [];
    private static Canvas keyBindingCanvas = null!;
    private static GameObject keyAlreadyUsed = null!;
    private static TextMeshProUGUI keyAlreadyUsedText = null!;
    private static KeySetting? keySettingListening;
    private static KeyCode keyDoubleBindListening;
    private static bool initialized;
    private static bool initializedInGame;
    private static Transform modsMenuTab = null!;
    private static readonly List<Action<KeyCode>> keyPressedListeners = new();
    private static string settingsMenuRoot = "";
    private static string settingsButton = "";
    private static Transform separatorTemplate = null!;
    private static Transform keyTemplate = null!;
    private static Transform boolTemplate = null!;
    private static Transform sliderTemplate = null!;
    private static Transform numberInputTemplate = null!;
    private static bool firstLoad = true;

    internal static void mainMenuListener()
    {
        initialized = false;
        foreach (var section in topSections)
            section.reset();
        settingsMenuRoot = "Canvas/WindowManager/1920x1080/UI Window Settings";
        settingsButton = "Canvas/1920x1080/Canvas Group/Default/Menu/Button_Settings";
        setupTab();
        loadAllSettings();
    }

    internal static void inGameMenuListener()
    {
        initialized = false;
        initializedInGame = false;
        foreach (var section in topSections)
            section.reset();
        settingsMenuRoot = "Canvas/WindowManagerCenterOption/UI Window Settings";
        settingsButton = "Canvas/WindowManagerCenterOption/UI Window Option/Container/Content/Settings";
        GameObject.Find(settingsButton).GetComponent<UiButton>()
            .add_OnTriggered(new Action(settingsWindowTriggeredInGame));
        loadAllSettings();
    }

    private static void setupTab()
    {
        var menu = GameObject.Find(settingsMenuRoot + "/Container/UI Window Header/Menu");

        for (var i = 0; i < menu.transform.childCount; i++)
        {
            var child = menu.transform.GetChild(i);
            child.GetComponent<UiButton>().add_OnTriggered(new Action(delegate { otherMenuTabTriggered(child); }));
        }

        modsMenuTab = Object.Instantiate(menu.transform.FindChild("Controls"), menu.transform);
        modsMenuTab.name = "Mods";
        modsMenuTab.FindChild("Text (TMP)").GetComponent<TextMeshProUGUI>().text = "Mods";
        modsMenuTab.GetComponent<UiButton>().add_OnTriggered(new Action(modsMenuTabTriggered));
        GameObject.Find(settingsButton).GetComponent<UiButton>().add_OnTriggered(new Action(settingsWindowTriggered));
        keyBindingCanvas = GameObject.Find(settingsMenuRoot + "/KeyBindingDialog").GetComponent<Canvas>();
        keyAlreadyUsed = GameObject.Find(settingsMenuRoot + "/AlreadyUsedKeyDialog");
        keyAlreadyUsed.transform.FindChild("Navigation").FindChild("Confirm").GetComponent<UiButton>()
            .add_OnTriggered(new Action(alreadyUsedConfirmButtonTriggered));
        keyAlreadyUsed.transform.FindChild("Navigation").FindChild("Cancel").GetComponent<UiButton>()
            .add_OnTriggered(new Action(alreadyUsedCancelButtonTriggered));
        keyAlreadyUsedText = keyAlreadyUsed.transform.FindChild("Text (TMP)").GetComponent<TextMeshProUGUI>();
    }

    private static void modsMenuTabTriggered()
    {
        try
        {
            if (!initialized)
            {
                setupTemplates();
                foreach (var section in topSections)
                    section.create(GameStateHelper.isInGame());
                initialized = true;
            }

            GameObject.Find(settingsMenuRoot + "/Container/Navigation").active = false;
            var scrollViewSettings =
                GameObject.Find(
                    settingsMenuRoot + "/Container/Scroll View/Viewport/Content/Settings");
            scrollViewSettings.transform.DetachChildren();
            if (topSections.Count == 0) return;
            foreach (var section in topSections)
                section.setParent(scrollViewSettings.transform);
            scrollViewSettings.gameObject.SetActive(false);
            scrollViewSettings.gameObject.SetActive(true);
            firstLoad = false;
        }
        catch (Exception e)
        { 
            GameObject.Find(settingsMenuRoot + "/Container/UI Window Header/Menu/Game").GetComponent<UiButtonTriggerUnityEvent>().OnEventTriggered();
            Plugin.Log.LogError("Could not open Mods Settings Tab:\n" + e);
        }
    }

    private static void setupTemplates()
    {
        separatorTemplate = GameObject.Find("UIGameSettingsSeparator(Clone)").transform;
        var inputSetting =
            GameObject.Find(
                "Canvas/WindowManager/1920x1080/UI Window Settings/Container/Scroll View/Viewport/Content/Settings/UIGameInputSetting(Clone)");
        keyTemplate = inputSetting == null
            ? GameObject
                .Find(
                    "Canvas/WindowManagerCenterOption/UI Window Settings/Container/Scroll View/Viewport/Content/Settings/UIGameInputSetting(Clone)")
                .transform
            : GameObject
                .Find(
                    "Canvas/WindowManager/1920x1080/UI Window Settings/Container/Scroll View/Viewport/Content/Settings/UIGameInputSetting(Clone)")
                .transform;
        boolTemplate =
            Object.Instantiate(GameObject.Find("GameSettingsManager/UIGameBoolSetting").transform.GetChild(0));
        boolTemplate.gameObject.active = true;
        boolTemplate.FindChild("Text (TMP)").GetComponent<TextMeshProUGUI>().color = Color.white;
        sliderTemplate =
            Object.Instantiate(GameObject.Find("GameSettingsManager/UIGameRangeSetting").transform.GetChild(0));
        sliderTemplate.gameObject.active = true;
        sliderTemplate.FindChild("Text (TMP)").GetComponent<TextMeshProUGUI>().color = Color.white;
        numberInputTemplate = Object.Instantiate(keyTemplate);
        numberInputTemplate.gameObject.active = true;
        numberInputTemplate.FindChild("Text (TMP)").GetComponent<TextMeshProUGUI>().color = Color.white;
        numberInputTemplate.GetComponent<UIGameInputSetting>().Destroy();
        numberInputTemplate.FindChild("UI Binding Press").gameObject.Destroy();
        var inputField = Object.Instantiate(GameObject.Find("Canvas/Report/Container/Content/InputField (TMP)"),
            numberInputTemplate);
        inputField.transform.FindChild("Text Area/Placeholder").gameObject.SetActive(false);
        var inputLayout = inputField.gameObject.AddComponent<LayoutElement>();
        inputLayout.preferredWidth = 250;
        var inputText = inputField.transform.FindChild("Text Area/Text").GetComponent<TextMeshProUGUI>();
        inputText.alignment = TextAlignmentOptions.Center;
        inputText.color = Color.white;
        inputText.font = numberInputTemplate.FindChild("Text (TMP)").GetComponent<TextMeshProUGUI>().font;
        inputText.fontSize = 15;
    }

    private static void settingsWindowTriggered()
    {
        GameObject.Find(settingsMenuRoot + "/Container/Navigation").active = true;
        detachAllCustomSettings();
        setSettingsTabVisibility();
    }

    private static void settingsWindowTriggeredInGame()
    {
        if (!initializedInGame)
        {
            setupTab();
            initializedInGame = true;
        }

        setSettingsTabVisibility();
    }

    private static void setSettingsTabVisibility()
    {
        modsMenuTab.SetParent(topSections.Count == 0
            ? null
            : GameObject.Find(settingsMenuRoot + "/Container/UI Window Header/Menu").transform);
    }

    private static void otherMenuTabTriggered(Transform tab)
    {
        if (tab.name.Equals("Controls"))
        {
            tab.parent.transform.FindChild("Game").GetComponent<UiButtonTriggerUnityEvent>().OnEventTriggered();
            tab.GetComponent<UiButtonTriggerUnityEvent>().OnEventTriggered();
        }

        GameObject.Find(settingsMenuRoot + "/Container/Navigation").active = true;
        detachAllCustomSettings();
    }

    private static void detachAllCustomSettings()
    {
        foreach (var section in topSections)
            section.setParent(null);
    }

    private static void alreadyUsedCancelButtonTriggered()
    {
        keyAlreadyUsed.active = false;
        keySettingListening = null;
        keyDoubleBindListening = KeyCode.None;
    }

    private static void alreadyUsedConfirmButtonTriggered()
    {
        keySettingListening!.changeKey(keyDoubleBindListening);
        alreadyUsedCancelButtonTriggered();
    }

    private static bool doubleBind()
    {
        foreach (var setting in Resources.FindObjectsOfTypeAll<UIGameInputSetting>())
            if (setting.state != null && setting.state.Value == Event.current.keyCode)
            {
                keyAlreadyUsedText.text = "The <style=\"Important\">" + Event.current.keyCode +
                                          "</style> key is already used by the game setting <style=\"Important\">" +
                                          setting.displayNameText.text + "</style>. Do you want to double bind?";
                goto Double;
            }
        
        foreach (var keySetting in allKeySettings)
            if (keySetting.getKey() == Event.current.keyCode)
            {
                keyAlreadyUsedText.text = "The <style=\"Important\">" + Event.current.keyCode +
                                          "</style> key is already used by the mod setting <style=\"Important\">" +
                                          keySetting.name + "</style>. Do you want to double bind?";
                goto Double;
            }

        return false;
        Double:
        {
            keyDoubleBindListening = Event.current.keyCode;
            keyAlreadyUsed.active = true;
            return true;
        }
    }

    internal static class keyListener
    {
        internal static void checkKeyPresses()
        {
            if (keySettingListening != null && keyDoubleBindListening == KeyCode.None)
            {
                keyBindingCanvas.enabled = false;
                if (Event.current.keyCode != KeyCode.Escape &&
                    Event.current.keyCode != keySettingListening.getKey() && !doubleBind())
                {
                    keySettingListening.changeKey(Event.current.keyCode);
                    keySettingListening = null;
                }

                if (keySettingListening != null && Event.current.keyCode == keySettingListening.getKey())
                    keySettingListening = null;
            }
            else if (keySettingListening == null && Event.current.keyCode != KeyCode.None)
            {
                foreach (var listener in keyPressedListeners)
                    listener(Event.current.keyCode);
            }
        }
    }

    public static void addTopSection(SettingsSection section)
    {
        foreach (var topSection in topSections)
            if (topSection.name == section.name)
                throw new ArgumentException("Section already exists!", nameof(section));
        
        topSections.Add(section);
    }

    private static void loadAllSettings()
    {
        foreach (var section in topSections)
        {
            section.loadSetting();
        }
    }

    public class SettingsSection(string sectionName) : ISectionItem
    {
        private bool collapsed;
        private bool created;
        private GameObject image = null!;
        private Transform? separator;
        private Transform? transform;

        private List<ISectionItem> subItems { get; } = [];

        public string name { get; init; } = sectionName;
        public SettingsSection? parent { get; set; } = null;

        public void destroy()
        {
            foreach (var item in subItems)
                item.destroy();
            subItems.Clear();
            if (separator != null)
                separator.gameObject.Destroy();
            if (transform != null)
                transform.gameObject.Destroy();
            if (parent == null)
                topSections.Remove(this);
        }

        public void addItem(ISectionItem item)
        {
            foreach (var subItem in subItems)
                if (subItem.name == item.name)
                    throw new ArgumentException("Setting already exists!", nameof(item));
            subItems.Add(item);
            item.parent = this;
        }

        public void addItem<T>(List<T> items) where T : ISectionItem
        {
            foreach (var i in items)
            {
                addItem(i);
            }
        }

        public void loadSetting()
        {
            foreach (var subItem in subItems)
            {
                subItem.loadSetting();
            }
        }

        public string getPath()
        {
            if (parent != null)
                return parent.getPath() + "/" + name;
            return name;
        }

        public void reset()
        {
            foreach (var item in subItems) item.reset();
            collapsed = false;
            created = false;
        }

        public void setParent(Transform? parentItem)
        {
            if (subItems.Count <= 0) return;
            if (transform == null || separator == null) return;
            transform.SetParent(parentItem);
            separator.SetParent(parentItem);
            transform.localScale = Vector3.one;
            separator.localScale = Vector3.one;
            foreach (var item in subItems)
                item.setParent(parentItem);
        }

        public void create(bool inGame)
        {
            if (subItems.Count <= 0) return;
            if (!created)
            {
                var inputSetting =
                    GameObject.Find(
                        "Canvas/WindowManager/1920x1080/UI Window Settings/Container/Scroll View/Viewport/Content/Settings/UIGameInputSetting(Clone)");
                if (inputSetting == null)
                {
                    transform = Object.Instantiate(GameObject
                        .Find(
                            "Canvas/WindowManagerCenterOption/UI Window Settings/Container/Scroll View/Viewport/Content/Settings/UIGameInputSetting(Clone)")
                        .transform);
                    separator = Object.Instantiate(GameObject
                        .Find(
                            "Canvas/WindowManagerCenterOption/UI Window Settings/Container/Scroll View/Viewport/Content/Settings/UIGameSettingsSeparator(Clone)")
                        .transform);
                }
                else
                {
                    transform = Object.Instantiate(GameObject
                        .Find(
                            "Canvas/WindowManager/1920x1080/UI Window Settings/Container/Scroll View/Viewport/Content/Settings/UIGameInputSetting(Clone)")
                        .transform);
                    separator = Object.Instantiate(GameObject
                        .Find(
                            "Canvas/WindowManager/1920x1080/UI Window Settings/Container/Scroll View/Viewport/Content/Settings/UIGameSettingsSeparator(Clone)")
                        .transform);
                }

                transform.name = "CustomUISettingsSection";
                separator.name = "CustomUISettingsSectionSeparator";
                var text = transform.FindChild("Text (TMP)");
                var textMesh = text.GetComponent<TextMeshProUGUI>();
                var depth = getPath().Split('/').Length - 1;
                var tabs = "";
                for (var i = 0; i < depth; i++) tabs += "   ";
                textMesh.text = tabs + name;
                textMesh.m_fontSize = 20 - depth * 2;
                text.GetComponent<LayoutElement>().preferredWidth = 800;
                var but = text.gameObject.AddComponent<UiButton>();
                but.add_OnTriggered(new Action(triggerCollapseButton));
                text.GetComponent<UITextMeshProSwapAccessibilityFont>().enabled = false;
                var button = transform.FindChild("UI Binding Press");
                button.GetComponent<UiButtonTriggerUnityEvent>().enabled = false;
                button.GetComponent<UiButton>().add_OnTriggered(new Action(triggerCollapseButton));
                button.FindChild("BG").gameObject.Destroy();
                button.FindChild("Border").gameObject.Destroy();
                button.FindChild("Text (TMP)").gameObject.Destroy();
                button.GetComponent<LayoutElement>().minWidth = 30;
                image = new GameObject("Image");
                image.transform.SetParent(button);
                var im = image.gameObject.AddComponent<Image>();
                im.sprite = GameObject
                    .Find("GameSettingsManager/UIGameListSetting/UIGameListSetting(Clone)/Dropdown/Arrow")
                    .GetComponent<Image>().sprite;
                im.preserveAspect = true;
                image.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
                image.transform.localPosition = firstLoad ? new Vector3(-5, 0, 0) : new Vector3(120, 0, 0);
                transform.gameObject.SetActive(true);
                separator.gameObject.SetActive(true);
                created = true;
            }

            foreach (var item in subItems)
                item.create(inGame);
        }

        private void triggerCollapseButton()
        {
            if (collapsed)
            {
                image.transform.eulerAngles = new Vector3(0, 0, 0);
                foreach (var item in subItems)
                    item.setVisible(true);
            }
            else
            {
                image.transform.eulerAngles = new Vector3(0, 0, 90);
                foreach (var item in subItems)
                    item.setVisible(false);
            }

            collapsed = !collapsed;
        }

        public void setVisible(bool visible)
        {
            foreach (var item in subItems)
                if (!visible)
                    item.setVisible(false);
                else
                    if (!collapsed)
                        item.setVisible(true);
            transform?.gameObject.SetActive(visible);
            separator?.gameObject.SetActive(visible);
        }
    }

    public abstract class Setting : ISectionItem
    {
        protected readonly bool changeableInGame;
        protected readonly string description;
        private readonly SettingType type;
        protected bool created;
        private Transform? separator;
        protected Transform? transform;

        protected Setting(string settingName, string settingDescription,
            SettingType settingType,
            bool ChangeableInGame)
        {
            type = settingType;
            name = settingName;
            description = settingDescription;
            changeableInGame = ChangeableInGame;
            var callingAssembly = Plugin.getCallingAssemblyName();
            if (callingAssembly != null)
                Plugin.Log.LogInfo("Added " + GetType().Name + " \"" + name + "\" from Assembly \"" + callingAssembly +
                                   "\"");
            initialized = false;
        }
        
        public string name { get; init; }
        public SettingsSection? parent { get; set; }

        public void destroy()
        {
            if (separator != null)
                separator.gameObject.Destroy();
            if (transform != null)
                transform.gameObject.Destroy();
            Plugin.Log.LogInfo("Removed " + GetType().Name + " \"" + name + "\" from Assembly \"" +
                               Plugin.getCallingAssemblyName() + "\"");
        }

        public void reset()
        {
            created = false;
        }

        public string getPath()
        {
            if (parent == null) return null!;
            return parent.getPath() + "." + name;
        }

        public void loadSetting()
        {
            this.loadSettingFromConfig();
        }

        protected abstract void loadSettingFromConfig();
        internal abstract void saveSettingToConfig();

        public virtual void create(bool inGame)
        {
            transform = type switch
            {
                SettingType.Key => Object.Instantiate(keyTemplate),
                SettingType.Bool => Object.Instantiate(boolTemplate),
                SettingType.Slider => Object.Instantiate(sliderTemplate),
                SettingType.NumberInput => Object.Instantiate(numberInputTemplate),
                _ => transform
            };

            transform!.gameObject.SetActive(true);

            separator = Object.Instantiate(separatorTemplate);
            transform.name = "CustomUISetting";
            separator.name = "CustomUISettingsSeparator";
            transform.GetComponent<HorizontalLayoutGroup>().padding.left = 40;
            var text = transform.FindChild("Text (TMP)");
            text.GetComponent<LayoutElement>().preferredWidth = 500;
            setTooltip(description);
            if (!changeableInGame && inGame)
            {
                var textMesh = text.GetComponent<TextMeshProUGUI>();
                textMesh.text = "<s>" + name + "</s>";
                textMesh.color = Color.gray;
                setTooltip("Setting cannot be changed while in game!");
            }
            else
            {
                text.GetComponent<TextMeshProUGUI>().text = name;
            }

            transform.gameObject.SetActive(true);
            separator.gameObject.SetActive(true);
        }

        internal virtual void setTooltip(string tip)
        {
            transform?.FindChild("Text (TMP)")?.GetComponent<UITooltipHoverHelper>().text = tip;
        }

        public void setVisible(bool visible)
        {
            transform?.gameObject.SetActive(visible);
            separator?.gameObject.SetActive(visible);
        }

        public void setParent(Transform? settingParent)
        {
            transform!.SetParent(settingParent);
            separator!.SetParent(settingParent);
            transform.localScale = Vector3.one;
            separator.localScale = Vector3.one;
        }
    }

    public sealed class BooleanSetting(
        string settingName,
        string settingDescription,
        bool defaultValue,
        bool changeableInGame,
        Action<bool>? callback = null)
        : Setting(settingName, settingDescription, SettingType.Bool, changeableInGame)
    {
        private TMP_Dropdown dropdown = null!;
        private bool value = defaultValue;

        public override void create(bool inGame)
        {
            if (created) return;
            base.create(inGame);
            dropdown = transform!.FindChild("Dropdown").GetComponent<TMP_Dropdown>();
            dropdown.onValueChanged.AddListener((UnityAction<int>)valueChanged);
            dropdown.SetValue(value.ToInt(), false);
            if (!changeableInGame && inGame)
            {
                dropdown.enabled = false;
                dropdown.transform.FindChild("Arrow").gameObject.active = false;
                dropdown.transform.FindChild("Label").GetComponent<TextMeshProUGUI>().color = Color.gray;
                dropdown.transform.GetComponent<ClickHandlerFmod>().enabled = false;
            }

            created = true;
        }

        private void valueChanged(int dropdownValue)
        {
            value = dropdown.value.ToBool();
            saveSettingToConfig();
            callback?.Invoke(value);
        }

        protected override void loadSettingFromConfig()
        {
            var savedValue = SettingsConfig.loadSetting(getPath());
            if (savedValue != null) value = bool.Parse(savedValue);
        }

        internal override void saveSettingToConfig()
        {
            SettingsConfig.saveSetting(getPath(), value.ToString());
        }

        public bool getValue()
        {
            return value;
        }
    }

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public sealed class KeySetting : Setting
    {
        private readonly Action<KeyCode>? keyChanged;
        private readonly Action? keyPressed;
        private readonly bool onlyInGame;
        private KeyCode key;
        private TextMeshProUGUI keyText = null!;

        public KeySetting(string settingName, string settingDescription, KeyCode defaultKey,
            Action<KeyCode>? keyChangedAction,
            Action? keyPressedAction, bool onlyTriggerInGame, bool changeableInGame) : base(settingName,
            settingDescription,
            SettingType.Key, changeableInGame)
        {
            key = defaultKey;
            keyChanged = keyChangedAction;
            if (keyPressedAction != null)
            {
                keyPressed = keyPressedAction;
                onlyInGame = onlyTriggerInGame;
                keyPressedListeners.Add(keyTriggeredAction);
            }
            allKeySettings.Add(this);
        }

        public KeySetting(string settingName, string settingDescription, KeyCode defaultKey,
            Action? keyPressedAction, bool onlyTriggerInGame) : this(settingName, settingDescription,
            defaultKey, null,
            keyPressedAction, onlyTriggerInGame, true)
        {
        }

        public KeySetting(string settingName, string settingDescription, KeyCode defaultKey,
            Action<KeyCode>? keyChangedAction, bool changeableInGame) : this(settingName, settingDescription,
            defaultKey,
            keyChangedAction, null, false, changeableInGame)
        {
        }

        private void keyTriggeredAction(KeyCode keyCode)
        {
            if (onlyInGame && !GameStateHelper.isInGame()) return;
            if (keyCode == key) keyPressed!();
        }

        protected override void loadSettingFromConfig()
        {
            var savedKey = SettingsConfig.loadSetting(getPath());
            if (savedKey != null) Enum.TryParse(savedKey, out key);
        }

        internal override void saveSettingToConfig()
        {
            SettingsConfig.saveSetting(getPath(), key.ToString());
        }

        public override void create(bool inGame)
        {
            if (created) return;
            base.create(inGame);
            var button = transform!.FindChild("UI Binding Press");
            keyText = button.FindChild("Text (TMP)").GetComponent<TextMeshProUGUI>();
            keyText.text = key.ToString();
            button.GetComponent<UiButtonTriggerUnityEvent>().enabled = false;
            button.GetComponent<UiButton>().add_OnTriggered(new Action(buttonTriggered));
            if (!changeableInGame && inGame)
            {
                button.GetComponent<UiButton>().enabled = false;
                button.FindChild("BG").gameObject.active = false;
                keyText.color = Color.gray;
            }

            created = true;
        }

        private void buttonTriggered()
        {
            keyBindingCanvas.enabled = true;
            keySettingListening = this;
        }

        public void changeKey(KeyCode keyCode)
        {
            key = keyCode;
            saveSettingToConfig();
            keyText.text = keyCode.ToString();
            keyChanged?.Invoke(keyCode);
        }

        public KeyCode getKey()
        {
            return key;
        }
    }

    public sealed class SliderSetting(
        string settingName,
        string settingDescription,
        int defaultValue,
        int min,
        int max,
        bool changeableInGame)
        : Setting(settingName, settingDescription, SettingType.Slider, changeableInGame)
    {
        private Slider slider = null!;
        private int value = defaultValue;

        protected override void loadSettingFromConfig()
        {
            var savedValue = SettingsConfig.loadSetting(getPath());
            if (savedValue != null)
            {
                var parsed = int.Parse(savedValue);
                if (parsed >= min && parsed <= max) value = parsed;
            }
        }

        internal override void saveSettingToConfig()
        {
            SettingsConfig.saveSetting(getPath(), value.ToString());
        }

        public override void create(bool inGame)
        {
            if (created) return;
            base.create(inGame);
            slider = transform!.FindChild("Slider").GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.wholeNumbers = true;
            slider.onValueChanged.AddListener((UnityAction<float>)valueChanged);

            if (!changeableInGame && inGame) slider.enabled = false;

            created = true;
        }

        private void valueChanged(float v)
        {
            value = (int)slider.value;
            saveSettingToConfig();
        }

        public int getValue()
        {
            return value;
        }
    }

    public sealed class NumberInputSetting(
        string settingName,
        string settingDescription,
        float defaultValue,
        bool changeableInGame)
        : Setting(settingName, settingDescription, SettingType.NumberInput, changeableInGame)
    {
        private readonly float defaultValue = defaultValue;
        private TMP_InputField inputField = null!;
        private float value = defaultValue;
        private readonly bool intBased;

        public NumberInputSetting(string settingName, string settingDescription,
            int defaultValue, bool changeableInGame) :
            this(settingName, settingDescription, (float) defaultValue, changeableInGame)
        {
            intBased = true;
        }

        public override void create(bool inGame)
        {
            if (created) return;
            base.create(inGame);
            transform!.FindChild("Text (TMP)").GetComponent<LayoutElement>().preferredWidth = 450;
            inputField = transform.FindChild("InputField (TMP)(Clone)").GetComponent<TMP_InputField>();
            inputField.text = value.ToString(CultureInfo.InvariantCulture);
            inputField.onValueChanged.AddListener((UnityAction<string>)valueChanged);
            inputField.onEndEdit.AddListener((UnityAction<string>)submit);
            var textArea = inputField.transform.FindChild("Text Area");
            textArea.GetComponent<RectMask2D>().enabled = false;
            textArea.gameObject.AddComponent<UITooltipHoverHelper>();
            setTooltip(description);
            if (!changeableInGame && inGame)
            {
                transform.FindChild("InputField (TMP)(Clone)/Text Area/Text").GetComponent<TextMeshProUGUI>().color =
                    Color.gray;
                inputField.interactable = false;
                setTooltip("Setting cannot be changed while in game!");
            }

            created = true;
        }

        internal override void setTooltip(string tip)
        {
            base.setTooltip(tip);
            transform?.FindChild("InputField (TMP)(Clone)")?.transform.FindChild("Text Area")?.GetComponent<UITooltipHoverHelper>()?.text = tip;
        }

        private void valueChanged(string s)
        {
            inputField.text = Regex.Replace(inputField.text, intBased ? "[^0-9]" : "[^0-9.]", "");
        }

        private void submit(string s)
        {
            if (intBased)
            {
                var success = int.TryParse(s, out var val);
                if (!success)
                {
                    inputField.text = inputField.m_OriginalText;
                    return;
                }

                value = val;
            }
            else
            {
                var success = float.TryParse(s, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var valF);
                if (!success)
                {
                    inputField.text = inputField.m_OriginalText;
                    return;
                }
                value = valF;
            }
            saveSettingToConfig();
        }

        public int getValue()
        {
            return (int) value;
        }

        public int getDefaultValue()
        {
            return (int) defaultValue;
        }
        
        public float getValueFloat()
        {
            return value;
        }

        public float getDefaultValueFloat()
        {
            return defaultValue;
        }

        protected override void loadSettingFromConfig()
        {
            var savedValue = SettingsConfig.loadSetting(getPath());
            if (savedValue == null) return;
            if (intBased)
                value = int.Parse(savedValue);
            else
                value = float.Parse(savedValue, CultureInfo.InvariantCulture);
        }

        internal override void saveSettingToConfig()
        {
            SettingsConfig.saveSetting(getPath(), value.ToString(CultureInfo.InvariantCulture));
        }
    }

    public interface ISectionItem
    {
        public string name { get; init; }
        public SettingsSection? parent { get; set; }
        public string getPath();
        public void loadSetting();
        public void destroy();
        internal void reset();
        internal void create(bool inGame);
        internal void setVisible(bool visible);
        internal void setParent(Transform? parent);
    }
}