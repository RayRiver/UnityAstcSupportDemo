using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class Util
{
    public const string PlatformAndroid = "Android";
    public const string PlatformIos = "iOS";
    public const string PlatformWindows = "Windows";
    public const string PlatformOSX = "OSX";
    public const string PlatformLinux = "Linux";

    public const TextureFormat ASTC_RGB_FORMAT = TextureFormat.ASTC_RGB_6x6;
    public const TextureFormat ASTC_RGBA_FORMAT = TextureFormat.ASTC_RGBA_6x6;

    public static bool UseAstc = true;

    public static bool SupportAstc()
    {
        var support = true;
        support = support && SystemInfo.SupportsTextureFormat(ASTC_RGB_FORMAT);
        support = support && SystemInfo.SupportsTextureFormat(ASTC_RGBA_FORMAT);
        return support;
    }

    public static string GetStringMD5(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return GetBytesMD5(bytes);
    }

    public static string GetBytesMD5(byte[] bytes)
    {
        var md5 = MD5.Create();
        var result = md5.ComputeHash(bytes, 0, bytes.Length);
        var sb = new StringBuilder();
        for (int i = 0; i < result.Length; ++i)
        {
            sb.Append(result[i].ToString("x2"));
        }

        return sb.ToString();
    }

    public static string GetAssetBundlePath()
    {
        return Application.streamingAssetsPath + "/data";
    }

    public static string GetRelativePathInProject(string path)
    {
        var index = path.IndexOf("Assets/", StringComparison.Ordinal);
        if (index >= 0)
        {
            return path.Substring(index);
        }
        return path;
    }

    public static string GetPlatformString()
    {
        var platform = string.Empty;
#if UNITY_ANDROID
            platform = PlatformIos;
#elif UNITY_IOS
            platform = PlatformIos;
#elif UNITY_STANDALONE_WIN
        platform = PlatformWindows;
#elif UNITY_EDITOR_WIN
            platform = PlatformWindows;
#elif UNITY_STANDALONE_OSX
            platform = PlatformOSX;
#elif UNITY_EDITOR_OSX
            platform = PlatformOSX;
#elif UNITY_STANDALONE_LINUX
            platform = PlatformLinux;
#else
            platform = PlatformWindows;
            UnityEngine.Debug.Assert(false, "Not supported platform");
#endif
        return platform;
    }

#if UNITY_EDITOR
    public static string GetPlatformStringInEditor(BuildTarget buildTarget = BuildTarget.NoTarget)
    {
        var platform = string.Empty;
        var target = EditorUserBuildSettings.activeBuildTarget;
        if (buildTarget != BuildTarget.NoTarget)
        {
            target = buildTarget;
        }

        switch (target)
        {
            case BuildTarget.Android:
                platform = PlatformAndroid;
                break;
            case BuildTarget.iOS:
                platform = PlatformIos;
                break;
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                platform = PlatformWindows;
                break;
            case BuildTarget.StandaloneOSX:
                platform = PlatformOSX;
                break;
            case BuildTarget.StandaloneLinux:
            case BuildTarget.StandaloneLinux64:
            case BuildTarget.StandaloneLinuxUniversal:
                platform = PlatformLinux;
                break;
            default:
                platform = PlatformWindows;
                UnityEngine.Debug.Assert(false, "Not supported platform: " + target);
                break;
        }

        return platform;
    }
#endif

    public static LayerMask GetLayerMask(string layerName)
    {
        return 1 << LayerMask.NameToLayer(layerName);
    }
}