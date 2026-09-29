using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class SlideIQBuild
{
    public static void ValidateSceneBindings()
    {
        const string scriptPath = "Assets/Scripts/SlideIQApp.cs";
        var script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
        if (script == null || script.GetClass() != typeof(SlideIQApp))
            throw new Exception("SlideIQ script is not correctly imported. Reimport " + scriptPath);
        string guid = AssetDatabase.AssetPathToGUID(scriptPath);
        foreach (string name in new[] { "SlideIQMenuScene", "SlideIQGameScene" })
        {
            string scenePath = "Assets/Scenes/" + name + ".unity";
            if (!File.ReadAllText(scenePath).Contains("m_Script: {fileID: 11500000, guid: " + guid + ","))
                throw new Exception(scenePath + " must reference SlideIQApp. Restore the corrected scene and SlideIQApp.cs.meta together.");
        }
    }

    [MenuItem("Tools/SlideIQ/Open Menu")]
    public static void OpenMenu()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene("Assets/Scenes/SlideIQMenuScene.unity");
    }
    [MenuItem("Tools/SlideIQ/Build WebGL and itch ZIP")]
    public static void Build()
    {
        ValidateSceneBindings();
        SlideIQValidation.Validate();
        SlideIQWebBuild.Build();
    }
}
