using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BulwarkStudios.GameSystems.Ui;
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
        Bool
    }

    internal static List<SettingsSection> sections = new();
    private static Canvas? keyBindingCanvas;
    private static GameObject? keyAlreadyUsed;
    private static TextMeshProUGUI? keyAlreadyUsedText;
    private static KeySetting? keySettingListening;
    private static KeyCode keyDoubleBindListening;
    private static bool initialized;
    private static bool initializedInGame;
    private static readonly List<Action<KeyCode>> keyPressedListeners = new();
    private static string settingsMenuRoot = "";
    private static string settingsButton = "";
    private static Transform seperatorTemplate;
    private static Transform keyTemplate;
    private static Transform boolTemplate;

    static SettingsHelper()
    {
        GameStateHelper.addSceneChangedListener(mainMenuListener, GameStateHelper.GameScene.MainMenu);
        GameStateHelper.addSceneChangedToInGameListener(inGameMenuListener);
    }

    internal static void mainMenuListener()
    {
        initialized = false;
        settingsMenuRoot = "Canvas/WindowManager/1920x1080/UI Window Settings";
        settingsButton = "Canvas/1920x1080/Canvas Group/Default/Menu/Button_Settings";
        setupTab();
    }

    internal static void inGameMenuListener()
    {
        initialized = false;
        initializedInGame = false;
        settingsMenuRoot = "Canvas/WindowManagerCenterOption/UI Window Settings";
        settingsButton = "Canvas/WindowManagerCenterOption/UI Window Option/Container/Content/Settings";
        Action settingsTriggered = delegate { settingsWindowTriggeredInGame(); };
        GameObject.Find(settingsButton).GetComponent<UiButton>().add_OnTriggered(settingsTriggered);
    }

    private static void setupTab()
    {
        if (sections.Count == 0) return;
        var menu = GameObject.Find(settingsMenuRoot + "/Container/UI Window Header/Menu");

        for (var i = 0; i < menu.transform.childCount; i++)
        {
            var child = menu.transform.GetChild(i);
            Action otherTabTriggered = delegate { otherMenuTabTriggered(child); };
            child.GetComponent<UiButton>().add_OnTriggered(otherTabTriggered);
        }

        var modsMenuTab = Object.Instantiate(menu.transform.FindChild("Controls"), menu.transform);
        modsMenuTab.name = "Mods";

        modsMenuTab.FindChild("Text (TMP)").GetComponent<TextMeshProUGUI>().text = "Mods";
        Action modsTabTriggered = delegate { modsMenuTabTriggered(); };
        modsMenuTab.GetComponent<UiButton>().add_OnTriggered(modsTabTriggered);
        Action settingsTriggered = delegate { settingsWindowTriggered(); };
        GameObject.Find(settingsButton).GetComponent<UiButton>()
            .add_OnTriggered(settingsTriggered);
        keyBindingCanvas = GameObject.Find(settingsMenuRoot + "/KeyBindingDialog").GetComponent<Canvas>();
        keyAlreadyUsed = GameObject.Find(settingsMenuRoot + "/AlreadyUsedKeyDialog");
        Action alreadyUsedConfirmTriggered = delegate { alreadyUsedConfirmButtonTriggered(); };
        keyAlreadyUsed.transform.FindChild("Navigation").FindChild("Confirm").GetComponent<UiButton>()
            .add_OnTriggered(alreadyUsedConfirmTriggered);
        Action alreadyUsedCancelTriggered = delegate { alreadyUsedCancelButtonTriggered(); };
        keyAlreadyUsed.transform.FindChild("Navigation").FindChild("Cancel").GetComponent<UiButton>()
            .add_OnTriggered(alreadyUsedCancelTriggered);
        keyAlreadyUsedText = keyAlreadyUsed.transform.FindChild("Text (TMP)").GetComponent<TextMeshProUGUI>();
    }

    private static void modsMenuTabTriggered()
    {
        if (!initialized)
        {
            seperatorTemplate = GameObject.Find("UIGameSettingsSeparator(Clone)").transform;
            keyTemplate = GameObject.Find("UIGameInputSetting(Clone)").transform;
            boolTemplate = GameObject.Find("GameSettingsManager/UIGameBoolSetting").transform.GetChild(0);
            boolTemplate.gameObject.active = true;
            boolTemplate.FindChild("Text (TMP)").GetComponent<TextMeshProUGUI>().color = Color.white;
            foreach (var section in sections)
                section.createSection(GameStateHelper.isInGame());
            initialized = true;
        }

        GameObject.Find(settingsMenuRoot + "/Container/Navigation").active = false;
        var scrollViewSettings =
            GameObject.Find(
                settingsMenuRoot + "/Container/Scroll View/Viewport/Content/Settings");
        scrollViewSettings.transform.DetachChildren();
        if (sections.Count == 0) return;
        foreach (var section in sections)
            section.setSectionParent(scrollViewSettings.transform);
    }

    private static void settingsWindowTriggered()
    {
        GameObject.Find(settingsMenuRoot + "/Container/Navigation").active = true;
        detachAllCustomSettings();
    }

    private static void settingsWindowTriggeredInGame()
    {
        if (!initializedInGame)
        {
            setupTab();
            initializedInGame = true;
        }
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
        foreach (var section in sections)
            section.detachSection();
    }

    private static void alreadyUsedCancelButtonTriggered()
    {
        keyAlreadyUsed.active = false;
        keySettingListening = null;
        keyDoubleBindListening = KeyCode.None;
    }

    private static void alreadyUsedConfirmButtonTriggered()
    {
        keySettingListening.changeKey(keyDoubleBindListening);
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

        foreach (var section in sections)
        foreach (var keySetting in section.sectionSettings.OfType<KeySetting>())
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

    internal class keyListener : MonoBehaviour
    {
        private void OnGUI()
        {
            if (Event.current.type == EventType.KeyDown)
                if (keySettingListening != null && Event.current.keyCode != KeyCode.None &&
                    keyDoubleBindListening == KeyCode.None)
                {
                    keyBindingCanvas.enabled = false;
                    if (Event.current.keyCode != KeyCode.Escape &&
                        Event.current.keyCode != keySettingListening.getKey() && doubleBind() == false)
                    {
                        keySettingListening.changeKey(Event.current.keyCode);
                        keySettingListening = null;
                    }

                    if (Event.current.keyCode == keySettingListening.getKey())
                    {
                        keySettingListening = null;
                    }
                }
                else if (keySettingListening == null && Event.current.keyCode != KeyCode.None)
                {
                    foreach (var listener in keyPressedListeners)
                        listener(Event.current.keyCode);
                }
        }
    }

    public class SettingsSection
    {
        internal readonly string sectionName;
        private readonly string seperatorName = "UIGameSettingsSeparator(Clone)";
        private readonly string transformName = "UIGameInputSetting(Clone)";
        private Transform seperator;
        private Transform transform;

        public SettingsSection(string SectionName)
        {
            foreach (var section in sections)
                if (section.sectionName.Equals(SectionName))
                    throw new ArgumentException("Section already exists!", nameof(SectionName));

            sectionName = SectionName;
            sections.Add(this);
        }

        internal List<Setting> sectionSettings { get; } = new();

        internal void addSetting(Setting setting)
        {
            sectionSettings.Add(setting);
        }

        internal void detachSection()
        {
            setSectionParent(null);
        }

        internal void setSectionParent(Transform? parent)
        {
            if (sectionSettings.Count > 0)
            {
                if (transform == null) return;
                transform.SetParent(parent);
                seperator.SetParent(parent);
                transform.localScale = Vector3.one;
                seperator.localScale = Vector3.one;
                foreach (var setting in sectionSettings)
                    setting.setParent(parent);
            }
        }

        internal void createSection(bool inGame)
        {
            if (sectionSettings.Count > 0)
            {
                transform = Object.Instantiate(GameObject.Find(transformName).transform);
                seperator = Object.Instantiate(GameObject.Find(seperatorName).transform);
                transform.name = "CustomUISettingsSection";
                seperator.name = "CustomUISettingsSectionSeparator";
                var text = transform.FindChild("Text (TMP)");
                var textMesh = text.GetComponent<TextMeshProUGUI>();
                textMesh.text = sectionName;
                textMesh.m_fontSize = 20;
                text.GetComponent<LayoutElement>().preferredWidth = 800;
                transform.FindChild("UI Binding Press").gameObject.active = false;
                foreach (var setting in sectionSettings)
                    setting.create(inGame);
            }
        }
    }

    public abstract class Setting
    {
        protected readonly bool changeableInGame;
        private readonly string description;
        internal readonly string name;
        private readonly SettingType type;
        protected SettingsSection section;
        protected Transform seperator;
        protected Transform transform;

        public Setting(SettingsSection Section, string settingName, string settingDescription, SettingType settingType,
            bool ChangeableInGame)
        {
            foreach (var setting in Section.sectionSettings)
                if (setting.name.Equals(settingName))
                    throw new ArgumentException("Setting already exists!", nameof(settingName));

            type = settingType;
            name = settingName;
            description = settingDescription;
            changeableInGame = ChangeableInGame;
            section = Section;
            section.addSetting(this);
            var st = new StackTrace();
            Plugin.Log.LogInfo("Added " + GetType().Name + " \"" + name + "\" from Assembly \"" +
                               Plugin.getCallingAssemblyName() + "\"");
        }

        internal abstract void loadSettingFromConfig();
        internal abstract void saveSettingToConfig();

        internal virtual void create(bool inGame)
        {
            switch (type)
            {
                case SettingType.Key:
                {
                    transform = Object.Instantiate(keyTemplate);
                    break;
                }
                case SettingType.Bool:
                {
                    transform = Object.Instantiate(boolTemplate);
                    break;
                }
            }

            seperator = Object.Instantiate(seperatorTemplate);
            transform.name = "CustomUISetting";
            seperator.name = "CustomUISettingsSeparator";
            transform.GetComponent<HorizontalLayoutGroup>().padding.left = 40;
            var text = transform.FindChild("Text (TMP)");
            text.GetComponent<LayoutElement>().preferredWidth = 500;
            var tooltip = text.GetComponent<UITooltipHoverHelper>();
            tooltip.text = description;
            if (!changeableInGame && inGame)
            {
                var textMesh = text.GetComponent<TextMeshProUGUI>();
                textMesh.text = "<s>" + name + "</s>";
                textMesh.color = Color.gray;
                tooltip.text = "Setting cannot be changed while in game!";
            }
            else
            {
                text.GetComponent<TextMeshProUGUI>().text = name;
            }
        }

        internal void detachSeperator()
        {
            seperator.SetParent(null);
        }

        internal void setParent(Transform? parent)
        {
            transform.SetParent(parent);
            seperator.SetParent(parent);
            transform.localScale = Vector3.one;
            seperator.localScale = Vector3.one;
        }
    }

    public class BooleanSetting : Setting
    {
        private TMP_Dropdown dropdown;
        private bool value;

        public BooleanSetting(SettingsSection section, string settingName, string settingDescription, bool defaultValue,
            bool changeableInGame) :
            base(section, settingName, settingDescription, SettingType.Bool, changeableInGame)
        {
            value = defaultValue;
            loadSettingFromConfig();
        }

        internal override void create(bool inGame)
        {
            base.create(inGame);
            dropdown = transform.FindChild("Dropdown").GetComponent<TMP_Dropdown>();
            dropdown.onValueChanged.AddListener((UnityAction<int>)valueChanged);
            dropdown.SetValue(value.ToInt(), false);
            if (!changeableInGame && inGame)
            {
                dropdown.enabled = false;
                dropdown.transform.FindChild("Arrow").gameObject.active = false;
                dropdown.transform.FindChild("Label").GetComponent<TextMeshProUGUI>().color = Color.gray;
                dropdown.transform.GetComponent<ClickHandlerFmod>().enabled = false;
            }
        }

        internal void valueChanged(int dropdownValue)
        {
            value = dropdown.value.ToBool();
            saveSettingToConfig();
        }

        internal override void loadSettingFromConfig()
        {
            var savedValue = SettingsConfig.loadSetting(section.sectionName, name);
            if (savedValue != null) value = bool.Parse(savedValue);
        }

        internal override void saveSettingToConfig()
        {
            SettingsConfig.saveSetting(section.sectionName, name, value.ToString());
        }

        public bool getValue()
        {
            return value;
        }
    }

    public class KeySetting : Setting
    {
        private readonly Action<KeyCode>? keyChanged;
        private readonly Action? keyPressed;
        private readonly bool onlyInGame;
        private KeyCode key;
        private TextMeshProUGUI keyText;

        public KeySetting(SettingsSection section, string settingName, string settingDescription, KeyCode defaultKey,
            Action<KeyCode>? keyChangedAction,
            Action? keyPressedAction, bool onlyTriggerInGame, bool changeableInGame) : base(section, settingName,
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

            loadSettingFromConfig();
        }

        public KeySetting(SettingsSection section, string settingName, string settingDescription, KeyCode defaultKey,
            Action? keyPressedAction, bool onlyTriggerInGame) : this(section, settingName, settingDescription,
            defaultKey, null,
            keyPressedAction, onlyTriggerInGame, true)
        {
        }

        public KeySetting(SettingsSection section, string settingName, string settingDescription, KeyCode defaultKey,
            Action<KeyCode>? keyChangedAction, bool changeableInGame) : this(section, settingName, settingDescription,
            defaultKey,
            keyChangedAction, null, false, changeableInGame)
        {
        }

        internal void keyTriggeredAction(KeyCode keyCode)
        {
            if (onlyInGame && !GameStateHelper.isInGame()) return;
            if (keyCode == key) keyPressed();
        }

        internal override void loadSettingFromConfig()
        {
            var savedKey = SettingsConfig.loadSetting(section.sectionName, name);
            if (savedKey != null) Enum.TryParse(savedKey, out key);
        }

        internal override void saveSettingToConfig()
        {
            SettingsConfig.saveSetting(section.sectionName, name, key.ToString());
        }

        internal override void create(bool inGame)
        {
            base.create(inGame);
            var button = transform.FindChild("UI Binding Press");
            keyText = button.FindChild("Text (TMP)").GetComponent<TextMeshProUGUI>();
            keyText.text = key.ToString();
            button.GetComponent<UiButtonTriggerUnityEvent>().enabled = false;
            Action triggered = delegate { buttonTriggered(); };
            button.GetComponent<UiButton>().add_OnTriggered(triggered);
            if (!changeableInGame && inGame)
            {
                button.GetComponent<UiButton>().enabled = false;
                button.FindChild("BG").gameObject.active = false;
                keyText.color = Color.gray;
            }
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
            if (keyChanged != null)
                keyChanged(keyCode);
        }

        public KeyCode getKey()
        {
            return key;
        }
    }
}