using System;
using System.Collections.Generic;
using UnityEngine;

namespace IMHelper;

public static class KeyListenerHelper
{
    private static readonly List<KeyValuePair<KeyCode, Action>> mainMenuListeners = [];
    private static readonly List<KeyValuePair<KeyCode, Action>> inGameListeners = [];

    public static void addMainMenuKeyListener(KeyCode key, Action listener)
    {
        mainMenuListeners.Add(new KeyValuePair<KeyCode, Action>(key, listener));
        var callingAssembly = Plugin.getCallingAssemblyName();
        if (callingAssembly != null)
            Plugin.Log.LogInfo("Added new KeyListener for key " + key + " for Assembly \"" + callingAssembly + "\"");
    }

    public static void addInGameKeyListener(KeyCode key, Action listener)
    {
        inGameListeners.Add(new KeyValuePair<KeyCode, Action>(key, listener));
        var callingAssembly = Plugin.getCallingAssemblyName();
        if (callingAssembly != null)
            Plugin.Log.LogInfo("Added new KeyListener for key " + key + " for Assembly \"" + callingAssembly + "\"");
    }

    internal class listener : MonoBehaviour
    {
        private void OnGUI()
        {
            if (Event.current.type != EventType.KeyDown || Event.current.keyCode == KeyCode.None) return;
            SettingsHelper.keyListener.checkKeyPresses();
            if (GameStateHelper.isInGame())
            {
                foreach (var keyValuePair in inGameListeners)
                    if (keyValuePair.Key == Event.current.keyCode)
                        keyValuePair.Value();
            }
            else if (GameStateHelper.currentScene == GameStateHelper.GameScene.MainMenu)
            {
                foreach (var keyValuePair in mainMenuListeners)
                    if (keyValuePair.Key == Event.current.keyCode)
                        keyValuePair.Value();
            }
        }
    }
}