using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

public static class ConfigureBuildSettings
{
    [MenuItem("Tools/Configure Build Settings")]
    public static void Configure()
    {
        var scenes = new[]
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/MainGame.unity"
        };

        var buildScenes = new EditorBuildSettingsScene[scenes.Length];
        for (int i = 0; i < scenes.Length; i++)
        {
            buildScenes[i] = new EditorBuildSettingsScene(scenes[i], true);
        }

        EditorBuildSettings.scenes = buildScenes;

        Debug.Log("Build settings configured with MainMenu and MainGame scenes.");
    }
}
#endif
