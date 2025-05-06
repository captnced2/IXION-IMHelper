using System;
using System.IO;
using BulwarkStudios.GameSystems.Ui;
using BulwarkStudios.Utils.UI;
using Cpp2IL.Core.Extensions;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace IMHelper;

public class ButtonHelper
{
    public class Button
    {
        private readonly Stream buttonTexture;
        private readonly string hoverText;
        private readonly string name;
        private readonly Transform original;
        private readonly Transform parent;
        private readonly Action<Transform> triggerAction;
        public Transform buttonTransform;
        public bool created;

        public Button(string name, Transform original, Transform parent, Action<Transform> triggerAction,
            Stream buttonTexture,
            string hoverText = null)
        {
            this.name = name;
            this.hoverText = hoverText;
            this.parent = parent;
            this.original = original;
            this.triggerAction = triggerAction;
            this.buttonTexture = buttonTexture;
        }

        public void createButton()
        {
            if (!parent.FindChild(name))
            {
                var newButton = Object.Instantiate(original, parent);
                buttonTransform = newButton;
                newButton.name = name;
                newButton.SetAsLastSibling();
                newButton.GetComponent<UiButtonTriggerUnityEvent>().enabled = false;
                if (hoverText != null) newButton.GetComponent<UITooltipHoverHelper>().translatedText = hoverText;
                newButton.GetComponent<UiButton>()
                    .add_OnTriggered(new Action(delegate { triggerAction(buttonTransform); }));

                if (buttonTexture != null)
                {
                    var oldSprite = buttonTransform.FindChild("Icon").GetComponent<Image>().sprite;
                    var texture = new Texture2D(2, 2);
                    texture.LoadImage(buttonTexture.ReadBytes());
                    var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                        oldSprite.textureRectOffset);
                    buttonTransform.FindChild("Icon").GetComponent<Image>().sprite = sprite;
                }

                created = true;
                Plugin.Log.LogInfo("Created button \"" + name + "\" from Assembly \"" +
                                   Plugin.getCallingAssemblyName() + "\"");
            }
        }

        public void hide()
        {
            if (created) buttonTransform.gameObject.SetActive(false);
        }

        public void show()
        {
            if (created) buttonTransform.gameObject.SetActive(true);
        }
    }
}