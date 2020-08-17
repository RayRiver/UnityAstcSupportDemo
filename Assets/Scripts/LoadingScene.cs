using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingScene : MonoBehaviour
{
    private void Start()
    {
        // 卸载所有AB包
        AssetBundleCache.Instance.UnloadAllBundles();
        
        AssetBundleCache.Instance.LoadBundle("scenes_test");
        SceneManager.LoadScene("Test");
    }
}
