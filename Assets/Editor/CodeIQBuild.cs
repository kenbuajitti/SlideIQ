using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CodeIQBuild
{
    static void ValidateSceneBindings()
    {
        const string scriptPath = "Assets/Scripts/CodeIQApp.cs";
        var script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
        if (script == null || script.GetClass() != typeof(CodeIQApp))
            throw new Exception("CodeIQ script is not correctly imported. Reimport " + scriptPath);
        string guid = AssetDatabase.AssetPathToGUID(scriptPath);
        foreach (string name in new[] { "CodeIQMenuScene", "CodeIQGameScene" })
        {
            string scenePath = "Assets/Scenes/" + name + ".unity";
            if (!File.ReadAllText(scenePath).Contains("guid: " + guid + ","))
                throw new Exception(scenePath + " is not linked to CodeIQApp. Re-extract the corrected project.");
        }
    }

    [MenuItem("Tools/CodeIQ/Open Menu")]
    public static void OpenMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene("Assets/Scenes/CodeIQMenuScene.unity");
    }

    [MenuItem("Tools/CodeIQ/Build WebGL and itch ZIP")]
    public static void Build()
    {
        ValidateSceneBindings();
        CodeIQValidation.Validate();
        PlayerSettings.productName="CodeIQ";
        PlayerSettings.WebGL.template="PROJECT:CodeIQResponsive";
        PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback=true;
        var root=Path.GetDirectoryName(Application.dataPath);
        string output=Path.Combine(root,"CodeIQWeb"),zip=Path.Combine(root,"CodeIQWeb.zip");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
            scenes=new[]{"Assets/Scenes/CodeIQMenuScene.unity","Assets/Scenes/CodeIQGameScene.unity"},
            locationPathName=output,target=BuildTarget.WebGL,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("CodeIQ WebGL build failed: "+report.summary.result);
        if(File.Exists(zip))File.Delete(zip);
        ZipFile.CreateFromDirectory(output,zip,System.IO.Compression.CompressionLevel.Optimal,false);
        Debug.Log("CodeIQ built. Upload "+zip+" to itch.io as an HTML game.");
        EditorUtility.RevealInFinder(zip);
    }
}
