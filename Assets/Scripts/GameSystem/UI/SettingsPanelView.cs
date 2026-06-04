using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelView : MonoBehaviour
{
    public Slider bgmSlider;
    public Slider sfxSlider;
    public Button closeButton;

    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        bgmSlider.onValueChanged.AddListener(v => SoundManager.Instance?.SetBGMVolume(v));
        sfxSlider.onValueChanged.AddListener(v => SoundManager.Instance?.SetSFXVolume(v));
        closeButton.onClick.AddListener(Hide);
    }

    public void Show()
    {
        // 저장된 볼륨으로 슬라이더 초기화 (이벤트 발화 없이)
        if (SoundManager.Instance != null)
        {
            bgmSlider.SetValueWithoutNotify(SoundManager.Instance.GetBGMVolume());
            sfxSlider.SetValueWithoutNotify(SoundManager.Instance.GetSFXVolume());
        }
        gameObject.SetActive(true);
    }

    public void Hide() => gameObject.SetActive(false);
}
