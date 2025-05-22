using System.Diagnostics;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace IMHelper;

[BepInPlugin(Guid, Name, Version)]
[BepInProcess("IXION.exe")]
public class Plugin : BasePlugin
{
    internal const string Guid = "captnced.IMHelper";
    private const string Name = "IMHelper";
    private const string Version = "3.0.0";
    internal new static ManualLogSource Log;
    internal static MonoHelper monoHelper;
    internal static ConfigFile config;

    public override void Load()
    {
        Log = base.Log;
        config = Config;
        SceneManager.activeSceneChanged += (UnityAction<Scene, Scene>)GameStateHelper.sceneChangedListener;
        AddComponent<KeyListenerHelper.listener>();
        monoHelper = AddComponent<MonoHelper>();
        var harmony = new Harmony(Guid);
        harmony.PatchAll();
        foreach (var patch in harmony.GetPatchedMethods())
            Log.LogInfo("Patched " + patch.DeclaringType + ":" + patch.Name);
        GameStateHelper.addSceneChangedListener(SettingsHelper.mainMenuListener, GameStateHelper.GameScene.MainMenu);
        GameStateHelper.addSceneChangedToInGameListener(SettingsHelper.inGameMenuListener);
        GameStateHelper.addSceneChangedListener(ModsMenu.mainMenuListener, GameStateHelper.GameScene.MainMenu);
        Log.LogInfo("Loaded \"" + Name + "\" version " + Version + "!");
    }

    internal static string getCallingAssemblyName()
    {
        var assembly = getCallingAssembly();
        if (assembly == null) return null;
        return assembly.GetName().Name.Contains("BepInEx") ? null : assembly.GetName().Name;
    }

    internal static Assembly getCallingAssembly()
    {
        foreach (var frame in new StackTrace(false).GetFrames())
        {
            var method = frame.GetMethod();
            if (method != null && method.DeclaringType != null &&
                method.DeclaringType.Assembly != Assembly.GetExecutingAssembly())
                return method.DeclaringType.Assembly;
        }

        return null;
    }

    internal class MonoHelper : MonoBehaviour
    {
    }
}