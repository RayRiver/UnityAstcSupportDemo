using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssetBundleCache : MonoBehaviour
{
    private static AssetBundleCache s_instance;
    public static AssetBundleCache Instance
    {
        get
        {
            return s_instance;
        }
    }
    
    private Dictionary<string, AssetBundle> m_bundles = new Dictionary<string, AssetBundle>();
    private AssetBundleManifest m_manifest = null;
    private Dictionary<string, BundleMapData> m_bundleMap;
    
    private void Awake()
    {
        s_instance = this;
        DontDestroyOnLoad(gameObject);
        LoadManifest();
        LoadBundleMap();
    }

    private void LoadManifest()
    {
        var bundle = LoadBundleCore(Constants.ManifestBundleName);
        m_manifest = bundle.LoadAsset<AssetBundleManifest>(Constants.ManifestAssetName);
        bundle.Unload(false);
    }

    private void LoadBundleMap()
    {
        var bundle = LoadBundleCore(Constants.BundleMapBundleName);
        var dataList = bundle.LoadAsset<BundleMap>(Constants.BundleMapAssetName);
        bundle.Unload(false);
        
        m_bundleMap = new Dictionary<string, BundleMapData>();
        for (var i = 0; i < dataList.Data.Count; ++i)
        {
            var data = dataList.Data[i];
            m_bundleMap.Add(data.BundleName, data);
        }
    }

    private void CacheBundle(string bundleName, AssetBundle bundle)
    {
        AssetBundle foundBundle = null;
        if (m_bundles.TryGetValue(bundleName, out foundBundle))
        {
            Debug.LogError("bundle already loaded: " + bundleName);
            return;
        }
        m_bundles.Add(bundleName, bundle);
    }
    
    private AssetBundle LoadBundleCore(string bundleName)
    {
        if (Util.UseAstc && m_bundleMap != null)
        {
            BundleMapData bundleMapData = null;
            if (m_bundleMap.TryGetValue(bundleName, out bundleMapData))
            {
                if (bundleMapData.AstcVariant)
                {
                    bundleName = bundleMapData.AstcVariantBundleName;
                }
            }
            else
            {
                Debug.LogError("Cannot find bundle name in bundle map: " + bundleName);
            }
        }

        var bundle = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/" + bundleName);
        Debug.Log("load bundle: " + bundleName);
        return bundle;
    }

    private AssetBundle LoadBundleFromCache(string bundleName)
    {
        AssetBundle bundle = null;
        if (m_bundles.TryGetValue(bundleName, out bundle))
        {
            return bundle;
        }
        return null;
    }

    private void LoadBundleDepend(string bundleName)
    {
        if (m_manifest == null)
        {
            Debug.Log("LoadBundleDepend failed, manifest is null: " + bundleName);
            return;
        }

        var depends = m_manifest.GetAllDependencies(bundleName);
        for (var i = 0; i < depends.Length; ++i)
        {
            var depend = depends[i];
            LoadBundle(depend);
        }
    }

    public AssetBundle LoadBundle(string bundleName)
    {
        var bundle = LoadBundleFromCache(bundleName);
        if (bundle != null) return bundle;
        
        bundle = LoadBundleCore(bundleName);
        CacheBundle(bundleName, bundle);
        
        LoadBundleDepend(bundleName);
        
        return bundle;
    }

    public T LoadAsset<T>(string bundleName, string assetName) where T : UnityEngine.Object
    {
        var bundle = LoadBundle(bundleName);
        return bundle.LoadAsset<T>(assetName);
    }

    public void UnloadAllBundles()
    {
        foreach (var kv in m_bundles)
        {
            var bundleName = kv.Key;
            var bundle = kv.Value;
            bundle.Unload(false);
            Debug.Log("unload bundle: " + bundleName);
        }
        m_bundles.Clear();
    }
}