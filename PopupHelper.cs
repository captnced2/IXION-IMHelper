using System;
using BulwarkStudios.GameSystems.Ui;
using Stanford.Settings;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace IMHelper;

public class PopupHelper
{
    private static Transform clickBlocker;
    private static GameInputLockHandle lockHandle;

    public static void openTextPopup(string title, string content, Action<string> result, bool selectAll = false)
    {
        textPopup.setup();
        textPopup.open(title, content, result, selectAll);
    }

    private static void setupClickBlocker()
    {
        if (clickBlocker != null && clickBlocker.transform != null) return;
        clickBlocker = GameObject.Find("Canvas/WindowManagerCenterOption/BackgroundUiBlocker").transform;
    }

    private static void blockInputs()
    {
        lockHandle = GameInputLockAll.CreateLock();
        clickBlocker.gameObject.SetActive(true);
    }

    private static void unblockInputs()
    {
        lockHandle?.Stop();
        clickBlocker.gameObject.SetActive(false);
    }

    internal static class textPopup
    {
        private static Transform transform;
        private static TMP_InputField inputField;
        private static TextMeshProUGUI title;
        private static Action<string> currentTextPopupCallback;
        internal static bool isOpened;

        internal static void setup()
        {
            if (transform != null && transform.transform != null) return;
            setupClickBlocker();
            transform =
                Object.Instantiate(
                    GameObject.Find("Canvas/WindowManagerCenterOption/UI Window Save Game/NewSavePopup").transform,
                    GameObject.Find("Canvas").transform);
            transform.gameObject.SetActive(false);
            transform.name = "IMHelperTextPopup";
            Action closePopup = delegate { close(); };
            var closeButton = transform.FindChild("Container/UI Window Header/Close Button");
            closeButton.GetComponent<UiButtonTriggerUnityEvent>().enabled = false;
            closeButton.GetComponent<UiButton>().add_OnTriggered(closePopup);
            var cancelButton = transform.FindChild("Container/ButtonContainer/Cancel");
            cancelButton.GetComponent<UiButtonTriggerUnityEvent>().enabled = false;
            cancelButton.GetComponent<UiButton>().add_OnTriggered(closePopup);
            var okButton = transform.FindChild("Container/ButtonContainer/Validate");
            okButton.GetComponent<UiButtonTriggerUnityEvent>().enabled = false;
            okButton.GetComponent<UiButton>().add_OnTriggered(new Action(delegate { submit(); }));
            inputField = transform.FindChild("Container/Content/InputField").GetComponent<TMP_InputField>();
            inputField.m_TextComponent.m_fontStyle = FontStyles.Normal;
            title = transform.FindChild("Container/UI Window Header/Title").GetComponent<TextMeshProUGUI>();
        }

        internal static void open(string popupTitle, string content, Action<string> result, bool selectAll = false)
        {
            if (isOpened) close();
            blockInputs();
            transform.gameObject.SetActive(true);
            currentTextPopupCallback = result;
            title.text = popupTitle;
            inputField.text = content;
            if (selectAll) inputField.SelectAll();
            isOpened = true;
        }

        private static void submit()
        {
            try
            {
                currentTextPopupCallback?.Invoke(inputField.text);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Error while executing text popup callback: " + e);
            }

            close();
        }

        internal static void close()
        {
            if (!isOpened) return;
            transform.gameObject.SetActive(false);
            unblockInputs();
            isOpened = false;
        }
    }
}