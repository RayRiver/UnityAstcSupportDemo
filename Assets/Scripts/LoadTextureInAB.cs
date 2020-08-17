using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadTextureInAB : MonoBehaviour
{
    [SerializeField] private string m_bundleName;
    [SerializeField] private string m_assetName;
    
    private void Start()
    {
        var spr = AssetBundleCache.Instance.LoadAsset<Sprite>(m_bundleName, m_assetName);
        
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = spr;
        }

        var image = GetComponent<UnityEngine.UI.Image>();
        if (image != null)
        {
            image.sprite = spr;
            image.color = Color.white;
        }
    }
}