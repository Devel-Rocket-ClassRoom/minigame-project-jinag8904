using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    // AudioMixer exposed parameter 이름 = PlayerPrefs 키 (매핑 실수 방지)
    private const string BGMKey = "BGMVolume";
    private const string SFXKey = "SFXVolume";

    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioSource sfxSource;       // 단발 SFX, SFX 그룹으로 라우팅
    [SerializeField] private AudioSource sustainedSource; // 지속 재생(loop), SFX 그룹으로 라우팅
    [SerializeField] private AudioClip uiClickClip;

    private Coroutine _fadeCo;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // Awake에서는 AudioMixer.SetFloat이 무시되는 Unity 이슈가 있어 Start에서 적용
        ApplyVolume(BGMKey, GetBGMVolume());
        ApplyVolume(SFXKey, GetSFXVolume());

        BindClickSounds();  // 최초 씬(Title)의 버튼은 sceneLoaded가 안 불리므로 직접 등록
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => BindClickSounds();

    // 씬의 모든 버튼에 클릭음을 자동 연결한다. (비활성 버튼 포함)
    private void BindClickSounds()
    {
        var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var b in buttons)
        {
            b.onClick.RemoveListener(PlayClick);  // 중복 등록 방지
            b.onClick.AddListener(PlayClick);
        }
    }

    public void PlayClick() => PlaySFX(uiClickClip);

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip != null)
            sfxSource.PlayOneShot(clip, volumeScale);
    }

    // 구간 지속 효과음 시작 (loop). 끝낼 때는 StopSustained로 페이드아웃.
    public void PlaySustained(AudioClip clip)
    {
        if (clip == null || sustainedSource == null) return;
        if (_fadeCo != null) { StopCoroutine(_fadeCo); _fadeCo = null; }
        sustainedSource.clip = clip;
        sustainedSource.volume = 1f;
        sustainedSource.Play();
    }

    // 지속 효과음을 페이드아웃 후 정지 (뚝 끊기지 않게).
    public void StopSustained(float fadeSeconds = 0.4f)
    {
        if (sustainedSource == null || !sustainedSource.isPlaying) return;
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(CoFadeOutSustained(fadeSeconds));
    }

    private IEnumerator CoFadeOutSustained(float fadeSeconds)
    {
        float start = sustainedSource.volume;
        float t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;  // 일시정지(timeScale 0) 영향 없이 페이드
            sustainedSource.volume = Mathf.Lerp(start, 0f, t / fadeSeconds);
            yield return null;
        }
        sustainedSource.Stop();
        sustainedSource.volume = 1f;  // 다음 재생을 위해 복원
        _fadeCo = null;
    }

    public float GetBGMVolume() => PlayerPrefs.GetFloat(BGMKey, 1f);
    public float GetSFXVolume() => PlayerPrefs.GetFloat(SFXKey, 1f);

    public void SetBGMVolume(float v01)
    {
        PlayerPrefs.SetFloat(BGMKey, v01);
        ApplyVolume(BGMKey, v01);
    }

    public void SetSFXVolume(float v01)
    {
        PlayerPrefs.SetFloat(SFXKey, v01);
        ApplyVolume(SFXKey, v01);
    }

    private void ApplyVolume(string param, float v01)
    {
        // 선형 0~1 → dB (0은 -80dB로 완전 음소거)
        float dB = v01 <= 0.0001f ? -80f : Mathf.Log10(v01) * 20f;
        mixer.SetFloat(param, dB);
    }
}
