using TMPro;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [SerializeField] private ParticleSystem gwishinFXPrefab;
    [SerializeField] private ParticleSystem dokkaebiFXPrefab;
    [SerializeField] private ParticleSystem mulgwishinFXPrefab;
    [SerializeField] private AudioClip gwishinActiveClip;
    [SerializeField] private AudioClip impactClip;
    [SerializeField] private AudioClip dokkaebiReverseClip;

    [Header("연출 공통")]
    [SerializeField] private Image screenOverlay;
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private CanvasGroup bannerGroup;   // SkillBanner의 CanvasGroup
    [SerializeField] private RectTransform bannerRect;  // SkillBanner의 RectTransform
    [SerializeField] private TMP_Text bannerText;

    [Header("잡기 배너 색")]
    private Color _capColor0 = Color.white;
    private Color _capColor1 = Color.white;

    [Header("물귀신")]
    [SerializeField] private Volume globalVolume;
    private Vignette vignette;
    private Tween vignetteHoldTween;
    private float baseVignetteIntensity;
    private Color baseVignetteColor;

    private CinemachineBrain brain;

    private void OnEnable() => GameEvents.OnCaptureSuccess += HandleCapture;
    private void OnDisable() => GameEvents.OnCaptureSuccess -= HandleCapture;

    private void Awake()
    {
        Instance = this;
        brain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : null;

        if (globalVolume != null && globalVolume.profile.TryGet(out vignette))
        {
            baseVignetteIntensity = vignette.intensity.value;
            baseVignetteColor = vignette.color.value;
        }
    }

    public void PlayGwishin(Vector3 pos)
    {
        PlayAt(gwishinFXPrefab, pos);
        ShakeCamera(0.3f);
        Flash(new Color(1f, 0f, 0f), 0.4f, 0.25f);
        AudioSource.PlayClipAtPoint(gwishinActiveClip, pos);
    }

    public void PlayDokkaebi(Vector3 pos)
    {
        PlayAt(dokkaebiFXPrefab, pos);
        ZoomPunch(-8f, 0.15f);   // FOV 8도 확 당겼다 복귀 (줌 펀치)
        ShowBanner(LocalizationManager.Get("SKILL_DOKKAEBI_REVERSE"),
                   new Color(0.91f, 0.46f, 0.10f));  // #E8751A
        SoundManager.Instance?.PlaySFX(dokkaebiReverseClip);
    }

    public void PlayMulgwishin(Vector3 pos)
    {
        PlayAt(mulgwishinFXPrefab, pos);
        SlowMotion(0.25f, 0.05f, 0.5f, 0.4f);
        VignettePulse(new Color(0.043f, 0.482f, 0.541f), 0.4f); // 청록 #0B7B8A
    }

    public void PlayBonusThrow()
    {
        ShowBanner(LocalizationManager.Get("BANNER_BONUS_THROW"),
                   new Color(1f, 0.78f, 0.24f));  // 금빛 #FFC83D
        ZoomPunch(-6f, 0.15f);   // 도깨비보다 살짝 약한 줌 펀치
        ShakeCamera(0.2f);       // 가벼운 흔들림
        AudioSource.PlayClipAtPoint(impactClip, Camera.main.transform.position);
    }

    public void PlayBlackYutThrow()
    {
        ShowBanner(LocalizationManager.Get("BANNER_BLACK_YUT"),
                   new Color(0.63f, 0.07f, 0.11f));  // 핏빛 크림슨 #A0121B
        VignettePulse(new Color(0.1f, 0f, 0.05f), 0.45f);  // 화면 가장자리 어둡게
        ShakeCamera(0.3f);
        AudioSource.PlayClipAtPoint(impactClip, Camera.main.transform.position);
    }

    // ---- 연출 헬퍼 ----
    private void ShakeCamera(float force)
    {
        if (impulseSource != null)
            impulseSource.GenerateImpulseWithForce(force);
    }

    private void Flash(Color color, float maxAlpha, float duration)
    {
        if (screenOverlay == null) return;
        screenOverlay.DOKill();
        color.a = 0f;
        screenOverlay.color = color;
        DOTween.Sequence()
            .Append(screenOverlay.DOFade(maxAlpha, duration * 0.3f))
            .Append(screenOverlay.DOFade(0f, duration * 0.7f));
    }

    // ---- 도깨비 헬퍼 ----
    private void ZoomPunch(float fovDelta, float duration)
    {
        if (brain == null) return;
        var vcam = brain.ActiveVirtualCamera as CinemachineCamera;
        if (vcam == null) return;
        float baseFov = vcam.Lens.FieldOfView;
        DOTween.To(() => vcam.Lens.FieldOfView,
                   v => { var l = vcam.Lens; l.FieldOfView = v; vcam.Lens = l; },
                   baseFov + fovDelta, duration)
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.OutQuad);
    }

    private void ShowBanner(string text, Color color)
    {
        if (bannerGroup == null || bannerText == null) return;
        bannerGroup.DOKill();
        bannerRect.DOKill();
        bannerText.text = text;
        bannerText.color = color;
        bannerGroup.alpha = 0f;
        bannerRect.localScale = Vector3.one * 0.6f;
        DOTween.Sequence()
            .Append(bannerGroup.DOFade(1f, 0.15f))
            .Join(bannerRect.DOScale(1f, 0.3f).SetEase(Ease.OutBack))  // 통통 튀어나옴
            .AppendInterval(0.8f)
            .Append(bannerGroup.DOFade(0f, 0.3f));
    }

    public void BannerHoldOn(string text, Color color)
    {
        if (bannerGroup == null || bannerText == null) return;
        bannerGroup.DOKill();
        bannerRect.DOKill();
        bannerText.text = text;
        bannerText.color = color;
        bannerGroup.alpha = 0f;
        bannerRect.localScale = Vector3.one * 0.6f;
        // 페이드인 후 그대로 유지 (자동으로 사라지지 않음). timeScale 영향 안 받게 SetUpdate(true)
        DOTween.Sequence().SetUpdate(true)
            .Append(bannerGroup.DOFade(1f, 0.15f).SetUpdate(true))
            .Join(bannerRect.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true));
    }

    public void BannerHoldOff()
    {
        if (bannerGroup == null) return;
        bannerGroup.DOKill();
        bannerGroup.DOFade(0f, 0.3f).SetUpdate(true);
    }

    // 귀신 액티브 잡기 성공 배너 (일회성) — 크림슨 #A0121B
    public void ShowGwishinActiveBanner() =>
        ShowBanner(LocalizationManager.Get("SKILL_GWISHIN_ACTIVE_BANNER"),
                   new Color(0.627f, 0.071f, 0.106f));

    // ---- 물귀신 헬퍼 ----

    public void PlayMulgwishinParticle(Vector3 pos) => PlayAt(mulgwishinFXPrefab, pos);

    // 물귀신 액티브 발동 배너 (제물) — 청록 #0B7B8A. AI가 써도 발동이 명확히 보이도록.
    public void ShowMulgwishinActiveBanner()
    {
        ShowBanner(LocalizationManager.Get("SKILL_MULGWISHIN_ACTIVE_BANNER"),
                   new Color(0.043f, 0.482f, 0.541f));
        ShakeCamera(0.25f);
        AudioSource.PlayClipAtPoint(impactClip, Camera.main.transform.position);
    }

    private void SlowMotion(float scale, float toDur, float hold, float backDur)
    {
        DOTween.Kill("slowmo");
        DOTween.Sequence().SetId("slowmo").SetUpdate(true)
            .Append(DOTween.To(() => Time.timeScale, x => Time.timeScale = x, scale, toDur))
            .AppendInterval(hold)
            .Append(DOTween.To(() => Time.timeScale, x => Time.timeScale = x, 1f, backDur))
            .OnComplete(() => Time.timeScale = 1f);
    }

    private void VignettePulse(Color color, float deltaUp)
    {
        if (vignette == null) return;
        DOTween.Kill("vig");
        vignette.color.value = color;
        float peak = Mathf.Clamp01(baseVignetteIntensity + deltaUp);
        DOTween.Sequence().SetId("vig").SetUpdate(true)
            .Append(DOTween.To(() => vignette.intensity.value, x => vignette.intensity.value = x, peak, 0.15f))
            .AppendInterval(0.2f)
            .Append(DOTween.To(() => vignette.intensity.value, x => vignette.intensity.value = x, baseVignetteIntensity, 0.4f))
            .OnComplete(() => vignette.color.value = baseVignetteColor);
    }

    public void VignetteHoldOn(Color color, float deltaUp, float fadeIn = 0.3f)
    {
        if (vignette == null) return;
        DOTween.Kill("vig");            // 패시브 펄스가 돌고 있으면 정리
        vignetteHoldTween?.Kill();
        vignette.color.value = color;
        vignetteHoldTween = DOTween.To(() => vignette.intensity.value,
            x => vignette.intensity.value = x,
            Mathf.Clamp01(baseVignetteIntensity + deltaUp), fadeIn).SetUpdate(true);
    }

    public void VignetteHoldOff(float fadeOut = 0.4f)
    {
        if (vignette == null) return;
        vignetteHoldTween?.Kill();
        vignetteHoldTween = DOTween.To(() => vignette.intensity.value,
            x => vignette.intensity.value = x, baseVignetteIntensity, fadeOut)
            .SetUpdate(true)
            .OnComplete(() => vignette.color.value = baseVignetteColor);
    }

    private static void PlayAt(ParticleSystem prefab, Vector3 pos)
    {
        if (prefab == null) return;
        var ps = Instantiate(prefab, pos, Quaternion.identity);
        ps.Play();
        Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
    }

    private void HandleCapture(int playerId)
    {
        ShowBanner(LocalizationManager.Get("BANNER_CAPTURE"), playerId == 0 ? _capColor0 : _capColor1);
        ShakeCamera(0.3f);
        AudioSource.PlayClipAtPoint(impactClip, Camera.main.transform.position);
    }

    public void SetCaptureColors(Color p0, Color p1)
    {
        _capColor0 = p0;
        _capColor1 = p1;
    }
}