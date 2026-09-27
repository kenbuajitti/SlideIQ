using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// A cloned project can retain its previous open scene and Play-mode override.
// Always start this project's Editor preview from the SlideIQ menu.
[InitializeOnLoad]
public static class SlideIQStartup
{
    const string MenuPath = "Assets/Scenes/SlideIQMenuScene.unity";
    const string GamePath = "Assets/Scenes/SlideIQGameScene.unity";

    static SlideIQStartup()
    {
        EditorApplication.delayCall += Configure;
    }

    static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var menu = AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuPath);
        if (menu != null) EditorSceneManager.playModeStartScene = menu;
    }

    [MenuItem("Tools/SlideIQ/Repair Startup and Open Menu")]
    public static void Repair()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Stop Play mode before repairing SlideIQ startup.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Configure();
        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene(MenuPath, true),
            new EditorBuildSettingsScene(GamePath, true)
        };
        PlayerSettings.productName = "SlideIQ";
        PlayerSettings.WebGL.template = "PROJECT:SlideIQResponsive";
        EditorSceneManager.OpenScene(MenuPath);
        Debug.Log("SlideIQ startup repaired. Press Play for the tile puzzle and SlideIQ instructions.");
    }
}
