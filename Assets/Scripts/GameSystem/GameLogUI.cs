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

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

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
        string header = playerName != null ? $"<b>{playerName}</b> 남은 결과" : "남은 결과";
        if (results == null || results.Count == 0)
            Instance.yutResultText.text = $"{header}: \n없음";
        else
            Instance.yutResultText.text = $"{header}: \n<b><color=#FFD700>{string.Join("  ", results.Select(ToKorean))}</color></b>";
    }

    public static string ToKorean(YutResult yr) => yr switch
    {
        YutResult.BACKDO => "뒷도",
        YutResult.Do     => "도",
        YutResult.Gae    => "개",
        YutResult.Geol   => "걸",
        YutResult.Yut    => "윷",
        YutResult.Mo     => "모",
        _                => yr.ToString()
    };
}
