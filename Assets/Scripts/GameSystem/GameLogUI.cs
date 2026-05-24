using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class GameLogUI : MonoBehaviour
{
    public static GameLogUI Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI yutResultText;
    [SerializeField] private TextMeshProUGUI gameLogText;
    [SerializeField] private int maxLogLines = 6;

    private readonly Queue<string> logLines = new Queue<string>();

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

    public static void Log(string message)
    {
        Debug.Log(message);
        if (Instance == null) return;
        Instance.logLines.Enqueue(message);
        if (Instance.logLines.Count > Instance.maxLogLines)
            Instance.logLines.Dequeue();
        Instance.gameLogText.text = string.Join("\n", Instance.logLines);
    }

    public static void UpdateYutResults(List<YutResult> results, string playerName = null)
    {
        if (Instance == null) return;
        Instance.lastYutResults = results;
        Instance.lastYutPlayerName = playerName;
        string header = playerName != null
            ? $"<b>{playerName}</b> {LocalizationManager.Get("YUT_RESULT_HEADER")}"
            : LocalizationManager.Get("YUT_RESULT_HEADER");
        if (results == null || results.Count == 0)
            Instance.yutResultText.text = $"{header}: \n{LocalizationManager.Get("YUT_RESULT_NONE")}";
        else
            Instance.yutResultText.text = $"{header}: \n<b><color=#FFD700>{string.Join("  ", results.Select(GetYutName))}</color></b>";
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
