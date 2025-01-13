using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace IMHelper;

public static class GameStateHelper
{
    public enum GameScene
    {
        None = 0,
        Initialize = 1,
        GameSetup = 2,
        MainMenu = 3,
        Chapter0 = 4,
        Chapter1 = 5,
        Chapter2 = 6,
        Chapter3 = 7,
        Chapter4 = 8,
        Chapter5 = 9
    }

    private static readonly Dictionary<GameScene, Action> sceneChangedListeners = new();
    private static readonly List<Action> sceneChangedToInGameListeners = new();

    public static GameScene currentScene { get; private set; }

    public static bool isInGame()
    {
        if (currentScene is GameScene.Chapter0 or GameScene.Chapter1 or GameScene.Chapter2 or GameScene.Chapter3
            or GameScene.Chapter4 or GameScene.Chapter5) return true;
        return false;
    }

    public static void addSceneChangedListener(Action listener, GameScene scene)
    {
        sceneChangedListeners.Add(scene, listener);
    }

    public static void addSceneChangedToInGameListener(Action listener)
    {
        sceneChangedToInGameListeners.Add(listener);
    }

    internal static void sceneChangedListener(Scene current, Scene next)
    {
        Enum.TryParse(SceneManager.GetActiveScene().name, out GameScene newScene);
        currentScene = newScene;
        foreach (var l in sceneChangedListeners)
            if (l.Key == currentScene)
                l.Value();
        if (isInGame())
            foreach (var l in sceneChangedToInGameListeners)
                l();
    }
}