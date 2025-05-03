using System.Diagnostics;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace IMHelper;

[BepInPlugin(Guid, Name, Version)]
[BepInProcess("IXION.exe")]
public class Plugin : BasePlugin
{
    private const string Guid = "captnced.IMHelper";
    private const string Name = "IMHelper";
    private const string Version = "1.1.0";
    internal new static ManualLogSource Log;
    internal static ConfigFile config;

    public override void Load()
    {
        Log = base.Log;
        config = Config;
        SceneManager.activeSceneChanged += (UnityAction<Scene, Scene>)GameStateHelper.sceneChangedListener;
        AddComponent<SettingsHelper.keyListener>();
        Log.LogInfo("Loaded \"" + Name + "\" version " + Version + "!");
    }

    internal static string getCallingAssemblyName()
    {
        foreach (var frame in new StackTrace(false).GetFrames())
        {
            var method = frame.GetMethod();
            if (method != null && method.DeclaringType != null &&
                method.DeclaringType.Assembly != Assembly.GetExecutingAssembly())
                return method.DeclaringType.Assembly.GetName().Name;
        }

        return null;
    }
}