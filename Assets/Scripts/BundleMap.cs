using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BundleMapData
{
    public string BundleName;
    public string RealBundleName;
    public List<string> Groups;
    public bool AstcVariant = false;
    public string AstcVariantBundleName;
}

public class BundleMap : ScriptableObject
{
    public List<BundleMapData> Data = new List<BundleMapData>();
}
