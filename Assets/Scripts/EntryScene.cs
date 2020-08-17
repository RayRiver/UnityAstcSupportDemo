using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EntryScene : MonoBehaviour
{
    private void Start()
    {
        AssetBundleCache.Instance.LoadBundle("scenes_loading");
        SceneManager.LoadScene("Loading");
    }
}