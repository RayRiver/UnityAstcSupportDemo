using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Sprites;
using UnityEngine;

public class AssetBundleBuilder
{
    public class AssetReference
    {
        public string ImportFrom;

        public static AssetReference CreateReference(string importFrom)
        {
            return new AssetReference
            {
                ImportFrom = importFrom,
            };
        }
    }

    public class RecordData
    {
        public string FileName;
        public string ABName;
        public bool Shared;
        public string SharedInfo;
    }

    private Dictionary<string, List<AssetReference>> m_assetGroups = new Dictionary<string, List<AssetReference>>();

    private Dictionary<string, List<AssetReference>> m_sharedAssetGroups =
        new Dictionary<string, List<AssetReference>>();

    private List<RecordData> m_recordDataList = new List<RecordData>();
    private List<BundleMapData> m_bundleMapDataList = new List<BundleMapData>();
    private List<AssetMapData> m_assetMapDataList = new List<AssetMapData>();
    private List<AtlasBundleMapData> m_atlasBundleMapDataList = new List<AtlasBundleMapData>();
    private string m_assetMapPath = string.Empty;
    private string m_bundleMapPath = string.Empty;

    public List<RecordData> RecordDataList
    {
        get { return m_recordDataList; }
    }

    public List<AssetMapData> AssetMapDataList
    {
        get { return m_assetMapDataList; }
    }

    public List<BundleMapData> BundleMapDataList
    {
        get { return m_bundleMapDataList; }
    }

    public void AddSceneBundle(string scenePath, string outputPrefix)
    {
        var fileName = Path.GetFileNameWithoutExtension(scenePath);
        var filePath = scenePath.Replace('\\', '/');
        var bundleName = string.Format("{0}{1}", outputPrefix, fileName).ToLower();
        var assetReferences = new List<AssetReference>();
        assetReferences.Add(new AssetReference()
        {
            ImportFrom = filePath,
        });
        m_assetGroups.Add(bundleName, assetReferences);
    }

    public void AddSceneBundles(string scenePath, string outputPrefix)
    {
        var topPath = scenePath;
        var files = Directory.GetFiles(topPath, "*.unity", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            AddSceneBundle(file, outputPrefix);
        }
    }
    
    public void AddDirBundle(string dir, string inputPostfix, string outputPrefix)
    {
        var topPath = dir;
        var files = Directory.GetFiles(topPath, "*" + inputPostfix, SearchOption.AllDirectories);
        var bundleName = (outputPrefix + Path.GetFileName(topPath)).ToLower();
        var assetReferences = new List<AssetReference>();
        foreach (var file in files)
        {
            var filePath = file.Replace('\\', '/');
            assetReferences.Add(new AssetReference()
            {
                ImportFrom = filePath,
            });
        }
        m_assetGroups.Add(bundleName, assetReferences);
    }

    public void UpdateSharedAssets()
    {
        // 清空已经解析的sharedAssetGroups
        m_sharedAssetGroups.Clear();
        m_recordDataList.Clear();
        m_bundleMapDataList.Clear();
        m_assetMapDataList.Clear();
        m_atlasBundleMapDataList.Clear();
        m_assetMapPath = string.Empty;
        m_bundleMapPath = string.Empty;

        // asset分别被哪些group引用, dependencyAssetPath(string) -> List<groupName(string)>
        var dependencyCollector = new Dictionary<string, List<string>>();

        // 被依赖的共享groups，groupName(string) -> List<assetReference(AssetReference)>
        var sharedDependency = new Dictionary<string, List<AssetReference>>();

        // incomingAssetPath(string) -> true
        var incomingAssetPaths = new Dictionary<string, bool>();

        // 收集依赖关系: dependencyCollector
        foreach (var kv in m_assetGroups)
        {
            var groupName = kv.Key;
            var assets = kv.Value;
            foreach (var asset in assets)
            {
                var assetPath = asset.ImportFrom;
                if (incomingAssetPaths.ContainsKey(assetPath))
                {
                    Debug.Assert(false, "asset in incomingAssetPaths duplicate");
                }

                incomingAssetPaths.Add(assetPath, true);
                CollectDependencies(groupName, assetPath, dependencyCollector);

                var path = assetPath;
                m_assetMapDataList.Add(new AssetMapData
                {
                    Path = path,
                    BundleName = groupName,
                    AssetName = Path.GetFileNameWithoutExtension(path),
                });
            }

            var realBundleName = GetRealBundleName(groupName);
            m_bundleMapDataList.Add(new BundleMapData
            {
                BundleName = groupName,
                RealBundleName = realBundleName,
            });

            m_recordDataList.Add(new RecordData
            {
                FileName = groupName,
                ABName = realBundleName,
                Shared = false,
                SharedInfo = string.Empty,
            });
        }

        // 解析被依赖的groups: sharedDependency
        foreach (var kv in dependencyCollector)
        {
            var assetPath = kv.Key;
            var groupNames = kv.Value;

            // 被依赖asset存在于原始asset中，忽略
            if (incomingAssetPaths.ContainsKey(assetPath))
            {
                continue;
            }

            if (groupNames != null && groupNames.Count > 0)
            {
                var realName = "depend: " + string.Join(",", groupNames.ToArray());
                var sharedGroupName = "depend_" + Util.GetStringMD5(realName);
                if (string.IsNullOrEmpty(sharedGroupName))
                {
                    Debug.Assert(false, "sharedGroupName is null: " + assetPath + ", " + groupNames);
                }
                
                m_assetMapDataList.Add(new AssetMapData
                {
                    Path = assetPath,
                    BundleName = sharedGroupName,
                    AssetName = Path.GetFileNameWithoutExtension(assetPath),
                });

                var isNew = false;
                List<AssetReference> list = null;
                if (!sharedDependency.TryGetValue(sharedGroupName, out list))
                {
                    list = new List<AssetReference>();
                    sharedDependency.Add(sharedGroupName, list);
                    isNew = true;
                }

                list.Add(AssetReference.CreateReference(assetPath));

                if (isNew)
                {
                    var realBundleName = GetRealBundleName(sharedGroupName);
                    m_bundleMapDataList.Add(new BundleMapData
                    {
                        BundleName = sharedGroupName,
                        RealBundleName = realBundleName,
                        Groups = groupNames,
                    });

                    m_recordDataList.Add(new RecordData
                    {
                        FileName = sharedGroupName,
                        ABName = realBundleName,
                        Shared = true,
                        SharedInfo = realName,
                    });
                }
            }
        }

        // 解析需要多打一次ASTC的bundle
        for (var i = 0; i < m_assetMapDataList.Count; ++i)
        {
            var assetMapData = m_assetMapDataList[i];
            var importer = AssetImporter.GetAtPath(assetMapData.Path) as TextureImporter;
            if (importer != null && !string.IsNullOrEmpty(importer.spritePackingTag))
            {
                var foundIndex = -1;
                for (var index = 0; index < m_atlasBundleMapDataList.Count; ++index)
                {
                    if (m_atlasBundleMapDataList[index].BundleName.Equals(assetMapData.BundleName))
                    {
                        foundIndex = index;
                        break;
                    }
                }

                if (foundIndex >= 0)
                {
                    m_atlasBundleMapDataList[foundIndex].AssetPaths.Add(assetMapData.Path);
                }
                else
                {
                    var data = new AtlasBundleMapData
                    {
                        BundleName = assetMapData.BundleName,
                        AssetPaths = new List<string>(),
                    };
                    data.AssetPaths.Add(assetMapData.Path);
                    m_atlasBundleMapDataList.Add(data);
                }
            }
        }
        
        // 更新BundleMap中的astc字段
        for (var i = 0; i < m_bundleMapDataList.Count; ++i)
        {
            var bundleMapData = m_bundleMapDataList[i];
            var found = false;
            for (var j = 0; j < m_atlasBundleMapDataList.Count; ++j)
            {
                if (m_atlasBundleMapDataList[j].BundleName.Equals(bundleMapData.BundleName))
                {
                    found = true;
                    break;
                }
            }

            if (found)
            {
                bundleMapData.AstcVariant = true;
                bundleMapData.AstcVariantBundleName = "astc_" + bundleMapData.BundleName;
            }
        }

        // 加入Manifest, AssetMap, BundleMap
        {
            var manifestBundleName = Constants.ManifestBundleName;
            var assetMapBundleName = Constants.AssetMapBundleName;
            var bundleMapBundleName = Constants.BundleMapBundleName;

            m_bundleMapDataList.Add(new BundleMapData
            {
                BundleName = manifestBundleName,
                RealBundleName = GetRealBundleName(manifestBundleName),
            });
            m_recordDataList.Add(new RecordData
            {
                FileName = manifestBundleName,
                ABName = GetRealBundleName(manifestBundleName),
                Shared = false,
                SharedInfo = string.Empty,
            });

            m_bundleMapDataList.Add(new BundleMapData
            {
                BundleName = assetMapBundleName,
                RealBundleName = GetRealBundleName(assetMapBundleName),
            });
            m_recordDataList.Add(new RecordData
            {
                FileName = assetMapBundleName,
                ABName = GetRealBundleName(assetMapBundleName),
                Shared = false,
                SharedInfo = string.Empty,
            });

            m_bundleMapDataList.Add(new BundleMapData
            {
                BundleName = bundleMapBundleName,
                RealBundleName = GetRealBundleName(bundleMapBundleName),
            });
            m_recordDataList.Add(new RecordData
            {
                FileName = bundleMapBundleName,
                ABName = GetRealBundleName(bundleMapBundleName),
                Shared = false,
                SharedInfo = string.Empty,
            });
        }

        // 生成BundleMap资源
        {
            var path = "Assets/" + Constants.BundleMapAssetName + ".asset";
            AssetDatabase.DeleteAsset(path);
            var bundleMap = ScriptableObject.CreateInstance<BundleMap>();
            bundleMap.Data.Clear();
            foreach (var data in m_bundleMapDataList)
            {
                bundleMap.Data.Add(data);
            }

            AssetDatabase.CreateAsset(bundleMap, path);
            m_bundleMapPath = path;
        }

        // 生成AssetMap资源
        {
            var path = "Assets/" + Constants.AssetMapAssetName + ".asset";
            AssetDatabase.DeleteAsset(path);
            var assetMap = ScriptableObject.CreateInstance<AssetMap>();
            assetMap.Data.Clear();
            foreach (var data in m_assetMapDataList)
            {
                assetMap.Data.Add(data);
            }

            AssetDatabase.CreateAsset(assetMap, path);
            m_assetMapPath = path;
        }
        
        // 生成AtlasBundleMap资源
        {
            var path = "Assets/" + Constants.AtlasBundleMapAssetName + ".asset";
            AssetDatabase.DeleteAsset(path);
            var map = ScriptableObject.CreateInstance<AtlasBundleMap>();
            map.Data.Clear();
            foreach (var data in m_atlasBundleMapDataList)
            {
                map.Data.Add(data);
            }
            AssetDatabase.CreateAsset(map, path);
        }

        // 保存解析结果
        m_sharedAssetGroups = sharedDependency;
    }

    private string GetRealBundleName(string bundleName)
    {
#if OBSCURE_AB_NAME
        return Util.GetStringMD5(bundleName);
#else
	    return bundleName;
#endif
    }

    private void CollectDependencies(string groupName, string assetPath, Dictionary<string, List<string>> collector)
    {
        var dependencies = AssetDatabase.GetDependencies(assetPath);
        foreach (var dependencyAssetPath in dependencies)
        {
            // 排除script
            {
                var t = AssetDatabase.GetMainAssetTypeAtPath(dependencyAssetPath);
                // 5.6.x may return MonoBehaviour as type when main asset is ScriptableObject	
                if (t == typeof(MonoBehaviour))
                {
                    var asset = AssetDatabase.LoadMainAssetAtPath(dependencyAssetPath);
                    t = asset.GetType();
                }

                if (t == typeof(MonoScript))
                {
                    continue;
                }
            }

            List<string> list = null;
            if (!collector.TryGetValue(dependencyAssetPath, out list))
            {
                list = new List<string>();
                collector.Add(dependencyAssetPath, list);
            }

            if (!list.Contains(groupName))
            {
                list.Add(groupName);
                list.Sort();
            }
        }
    }

    public void SetAssetMap(string path)
    {
        m_assetMapPath = path;
    }

    public AssetBundleManifest BuildAssetBundles(string path, BuildAssetBundleOptions options, BuildTarget target)
    {
        var builds = new List<AssetBundleBuild>();

        foreach (var kv in m_assetGroups)
        {
            builds.Add(new AssetBundleBuild
            {
                assetBundleName = kv.Key,
                assetNames = kv.Value.Select(a => a.ImportFrom).ToArray(),
            });
        }

        foreach (var kv in m_sharedAssetGroups)
        {
            builds.Add(new AssetBundleBuild
            {
                assetBundleName = kv.Key,
                assetNames = kv.Value.Select(a => a.ImportFrom).ToArray(),
            });
        }

        builds.Add(new AssetBundleBuild
        {
            assetBundleName = Constants.BundleMapBundleName,
            assetNames = new[] {m_bundleMapPath},
        });

        builds.Add(new AssetBundleBuild
        {
            assetBundleName = Constants.AssetMapBundleName,
            assetNames = new[] {m_assetMapPath},
        });

        AssetBundleManifest manifest = null;
        
        // 开始打AssetBundle
        {
            EditorSettings.spritePackerMode = SpritePackerMode.BuildTimeOnly;
            Packer.SelectedPolicy = typeof(CustomSpritePackerPolicy).Name;
            CustomSpritePackerPolicy.MakeAstc = false;
            Packer.RebuildAtlasCacheIfNeeded(target, true, Packer.Execution.ForceRegroup);
            
            manifest = BuildPipeline.BuildAssetBundles(path, builds.ToArray(), options, target);
        }

        // 改名manifest
        {
            AssetDatabase.Refresh();
            var relativePath = Util.GetRelativePathInProject(path);
            var manifestFileName = Path.GetFileName(relativePath);
            var srcFile = relativePath + "/" + manifestFileName;
            var newName = Constants.ManifestBundleName;
            AssetDatabase.RenameAsset(srcFile, newName);
            AssetDatabase.RenameAsset(srcFile + ".manifest", newName + ".manifest");
        }

        // 打图集的AB包
        {
            var variantName = "variant";
            
            var variantPath = path + "/" + variantName;
            if (Directory.Exists(variantPath))
            {
                Directory.Delete(variantPath, true);
            }
            if (!Directory.Exists(variantPath))
            {
                Directory.CreateDirectory(variantPath);
            }
            
            var variantBuilds = new List<AssetBundleBuild>();
            for (var i = 0; i < m_atlasBundleMapDataList.Count; ++i)
            {
                var data = m_atlasBundleMapDataList[i];
                variantBuilds.Add(new AssetBundleBuild
                {
                    assetBundleName = data.BundleName, // 这里名字不可以变，否则可能依赖找不到
                    assetNames = data.AssetPaths.ToArray(),
                });
            }
            
            EditorSettings.spritePackerMode = SpritePackerMode.BuildTimeOnly;
            Packer.SelectedPolicy = typeof(CustomSpritePackerPolicy).Name;
            CustomSpritePackerPolicy.MakeAstc = true;
            Packer.RebuildAtlasCacheIfNeeded(target, true, Packer.Execution.ForceRegroup);
            
            var variantManifest = BuildPipeline.BuildAssetBundles(variantPath, variantBuilds.ToArray(), options, target);
            var bundles = variantManifest.GetAllAssetBundles();
            for (var i = 0; i < bundles.Length; ++i)
            {
                var bundleName = bundles[i];
                Debug.Log("bundle = " + bundleName);
                var srcPath = variantPath + "/" + bundleName;
                var dstPath = path + "/astc_" + bundleName;
                File.Copy(srcPath, dstPath);
                File.Copy(srcPath + ".manifest", dstPath + ".manifest");
            }
            
            if (Directory.Exists(variantPath))
            {
                Directory.Delete(variantPath, true);
            }
        }
        
        AssetDatabase.Refresh();
        
        return manifest;
    }
}