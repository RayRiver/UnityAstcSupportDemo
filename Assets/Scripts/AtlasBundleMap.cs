using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AtlasBundleMapData
{
    public string BundleName;
    public List<string> AssetPaths;
}

public class AtlasBundleMap : ScriptableObject
{
    public List<AtlasBundleMapData> Data = new List<AtlasBundleMapData>();
}