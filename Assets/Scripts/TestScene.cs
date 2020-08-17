using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TestScene : MonoBehaviour
{
    [SerializeField] private SpriteRenderer m_dynamicSpriteRenderer;
    [SerializeField] private SpriteRenderer m_dependencySpriteRenderer;
    [SerializeField] private Button m_buttonShowSpriteTextureFormat;
    [SerializeField] private Button m_buttonChangeToAnotherFormat;
    [SerializeField] private Text m_textUseAstc;
    [SerializeField] private Text m_textDeviceSupportAstc;
    [SerializeField] private Text m_textDynamicFormat;
    [SerializeField] private Text m_textDependencyFormat;
    
    private void Start()
    {
        m_buttonShowSpriteTextureFormat.onClick.RemoveAllListeners();
        m_buttonShowSpriteTextureFormat.onClick.AddListener(OnShowSpriteTextureFormat);
        m_buttonChangeToAnotherFormat.onClick.RemoveAllListeners();
        m_buttonChangeToAnotherFormat.onClick.AddListener(OnChangeToAnotherFormat);
        
        m_textUseAstc.text = Util.UseAstc.ToString();
        m_textDeviceSupportAstc.text = Util.SupportAstc().ToString();
    }

    private void OnShowSpriteTextureFormat()
    {
        m_textUseAstc.text = Util.UseAstc.ToString();
        m_textDeviceSupportAstc.text = Util.SupportAstc().ToString();
        m_textDynamicFormat.text = m_dynamicSpriteRenderer.sprite.texture.format.ToString();
        m_textDependencyFormat.text = m_dependencySpriteRenderer.sprite.texture.format.ToString();
    }

    private void OnChangeToAnotherFormat()
    {
        Util.UseAstc = !Util.UseAstc;
        AssetBundleCache.Instance.LoadBundle("scenes_loading");
        SceneManager.LoadScene("Loading");
    }
}