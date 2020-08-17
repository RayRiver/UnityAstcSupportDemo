using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class BuildAssetBundles 
{
    [MenuItem("Tools/Build Apk")]
    private static void DoBuildApk()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            return;
        }

        if (!EditorUtility.DisplayDialog(
            "Confirm", "Ready to make Apk?", "OK", "Cancel"))
        {
            return;
        }
        
        DoBuildAssetBundles();
        
        var scenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                scenes.Add(scene.path);
            }
        }

        var outputDir = Application.dataPath + "/../Output";
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }
        
		var playerOptions = new BuildPlayerOptions();
        playerOptions.scenes = scenes.ToArray();
        playerOptions.locationPathName = outputDir + "/Game.apk";
		playerOptions.target = EditorUserBuildSettings.activeBuildTarget;
		playerOptions.options = BuildOptions.Development | BuildOptions.ConnectWithProfiler;

        BuildPipeline.BuildPlayer(playerOptions);
    }

    [MenuItem("Tools/Build Asset Bundles")]
    private static void DoBuildAssetBundles()
    {
        var abPath = Application.streamingAssetsPath;
        if (Directory.Exists(abPath))
        {
            Directory.Delete(abPath, true);
        }
        if (!Directory.Exists(abPath))
        {
            Directory.CreateDirectory(abPath);
        }

        var builder = new AssetBundleBuilder();
        builder.AddSceneBundle("Assets/Scenes/Test.unity", "scenes_");
        builder.AddSceneBundle("Assets/Scenes/Loading.unity", "scenes_");
        builder.AddDirBundle("Assets/Textures/Dynamic", "", "textures_");
        builder.UpdateSharedAssets();
        builder.BuildAssetBundles(abPath, BuildAssetBundleOptions.ChunkBasedCompression,
            EditorUserBuildSettings.activeBuildTarget);
    }
}