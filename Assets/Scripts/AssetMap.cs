using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AssetMapData
{
    public string Path;
    public string BundleName;
    public string AssetName;
}

public class AssetMap : ScriptableObject
{
    public List<AssetMapData> Data = new List<AssetMapData>();
}
