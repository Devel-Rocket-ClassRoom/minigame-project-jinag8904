using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class GameLogUI : MonoBehaviour
{
    public static GameLogUI Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI yutResultText;

    private List<YutResult> lastYutResults;
    private string lastYutPlayerName;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
    }

    private void OnDestroy() => LocalizationManager.OnLanguageChanged -= OnLanguageChanged;

    private void OnLanguageChanged() => UpdateYutResults(lastYutResults, lastYutPlayerName);

    public static void UpdateYutResults(List<YutResult> results, string playerName = null)
    {
        if (Instance == null) return;
        Instance.lastYutResults = results;
        Instance.lastYutPlayerName = playerName;
        if (results == null || results.Count == 0)
            Instance.yutResultText.text = "";
        else
            Instance.yutResultText.text = $"<b><color=#FFD700>{string.Join("  ", results.Select(GetYutName))}</color></b>";
    }

    public static string GetYutName(YutResult yr) => yr switch
    {
        YutResult.BACKDO => LocalizationManager.Get("YUT_BACKDO"),
        YutResult.Do     => LocalizationManager.Get("YUT_DO"),
        YutResult.Gae    => LocalizationManager.Get("YUT_GAE"),
        YutResult.Geol   => LocalizationManager.Get("YUT_GEOL"),
        YutResult.Yut    => LocalizationManager.Get("YUT_YUT"),
        YutResult.Mo     => LocalizationManager.Get("YUT_MO"),
        _                => yr.ToString()
    };
}
