using System;
using System.IO;
using BulwarkStudios.GameSystems.Ui;
using BulwarkStudios.Utils.UI;
using Cpp2IL.Core.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace IMHelper;

public static class ButtonHelper
{
    public abstract class Button(
        string name,
        Transform original,
        Transform parent,
        Action<Transform> triggerAction,
        string hoverText = null)
    {
        public Transform buttonTransform;
        public bool created;

        public void createButton()
        {
            if (parent.FindChild(name)) return;
            var newButton = Object.Instantiate(original, parent);
            buttonTransform = newButton;
            newButton.name = name;
            newButton.GetComponent<UiButtonTriggerUnityEvent>().enabled = false;
            if (hoverText != null)
            {
                var hoverHelper = newButton.GetComponent<UITooltipHoverHelper>();
                if (hoverHelper != null)
                    hoverHelper.translatedText = hoverText;
            }

            newButton.GetComponent<UiButton>()
                .add_OnTriggered(new Action(delegate { triggerAction(buttonTransform); }));
            createButtonInternal();
            created = true;
            var callingAssembly = Plugin.getCallingAssemblyName();
            if (callingAssembly != null)
                Plugin.Log.LogInfo("Created button \"" + name + "\" from Assembly \"" +
                                   callingAssembly + "\"");
        }

        protected abstract void createButtonInternal();

        public void hide()
        {
            if (created) buttonTransform.gameObject.SetActive(false);
        }

        public void show()
        {
            if (created) buttonTransform.gameObject.SetActive(true);
        }
    }

    public class TextButton(
        string name,
        Transform original,
        Transform parent,
        Action<Transform> triggerAction,
        string text,
        string hoverText = null)
        : Button(name, original, parent, triggerAction, hoverText)
    {
        protected override void createButtonInternal()
        {
            buttonTransform.FindChild("Text (TMP)").GetComponent<TextMeshProUGUI>().text = text;
        }
    }

    public class IconButton(
        string name,
        Transform original,
        Transform parent,
        Action<Transform> triggerAction,
        Stream buttonTexture,
        string hoverText = null)
        : Button(name, original, parent, triggerAction, hoverText)
    {
        protected override void createButtonInternal()
        {
            if (buttonTexture == null) return;
            var oldSprite = buttonTransform.FindChild("Icon").GetComponent<Image>().sprite;
            var texture = new Texture2D(2, 2);
            texture.LoadImage(buttonTexture.ReadBytes());
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                oldSprite.textureRectOffset);
            buttonTransform.FindChild("Icon").GetComponent<Image>().sprite = sprite;
        }
    }
}