using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class SlideIQBuild
{
    [MenuItem("Tools/SlideIQ/Open Menu")]
    public static void OpenMenu()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene("Assets/Scenes/SlideIQMenuScene.unity");
    }
    [MenuItem("Tools/SlideIQ/Build WebGL and itch ZIP")]
    public static void Build()
    {
        SlideIQValidation.Validate();
        PlayerSettings.productName="SlideIQ";
        PlayerSettings.WebGL.template="PROJECT:SlideIQResponsive";
        PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback=true;
        var root=Path.GetDirectoryName(Application.dataPath);
        string output=Path.Combine(root,"SlideIQWeb"),zip=Path.Combine(root,"SlideIQWeb.zip");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
            scenes=new[]{"Assets/Scenes/SlideIQMenuScene.unity","Assets/Scenes/SlideIQGameScene.unity"},
            locationPathName=output,target=BuildTarget.WebGL,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("SlideIQ build failed: "+report.summary.result);
        if(File.Exists(zip))File.Delete(zip);
        ZipFile.CreateFromDirectory(output,zip,System.IO.Compression.CompressionLevel.Optimal,false);
        Debug.Log("Upload "+zip+" to itch.io as an HTML game.");EditorUtility.RevealInFinder(zip);
    }
}
