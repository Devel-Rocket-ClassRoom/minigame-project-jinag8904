using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [SerializeField] private AudioClip transitionClip;

    private Image fadeOverlay;
    private const float FADE_DURATION = 0.5f;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        CreateFadeOverlay();
    }

    private void CreateFadeOverlay()
    {
        var canvasGO = new GameObject("SceneLoader_Canvas");
        canvasGO.transform.SetParent(transform);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var overlayGO = new GameObject("FadeOverlay", typeof(RectTransform));
        overlayGO.transform.SetParent(canvasGO.transform, false);

        var rect = (RectTransform)overlayGO.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        fadeOverlay = overlayGO.AddComponent<Image>();
        fadeOverlay.color = new Color(0, 0, 0, 0);
        fadeOverlay.raycastTarget = false;
    }

    public static void LoadScene(string sceneName)
    {
        if (IsTransitioning) return;
        if (Instance == null)
        {
            var go = new GameObject("SceneLoader");
            go.AddComponent<SceneLoader>();
        }
        Instance.StartCoroutine(Instance.CoFadeAndLoad(sceneName));
    }

    public static bool IsTransitioning { get; private set; }

    private IEnumerator CoFadeAndLoad(string sceneName)
    {
        IsTransitioning = true;
        if (transitionClip != null) SoundManager.Instance?.PlaySFX(transitionClip);
        yield return CoFade(0f, 1f, FADE_DURATION);
        SceneManager.LoadScene(sceneName);
        yield return null;
        yield return CoFade(1f, 0f, FADE_DURATION);
        IsTransitioning = false;
    }

    private IEnumerator CoFade(float from, float to, float duration)
    {
        fadeOverlay.raycastTarget = true;
        float elapsed = 0f;
        Color c = fadeOverlay.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, elapsed / duration);
            fadeOverlay.color = c;
            yield return null;
        }
        c.a = to;
        fadeOverlay.color = c;
        if (to == 0f) fadeOverlay.raycastTarget = false;
    }
}
