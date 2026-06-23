using Cysharp.Threading.Tasks;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsPanelView : MonoBehaviour
{
    public TextMeshProUGUI totalText;

    [Serializable]
    public class CharacterRow
    {
        public string localizationKey;
        public TextMeshProUGUI text;
    }

    public CharacterRow[] rows;
    public Button closeButton;

    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        closeButton.onClick.AddListener(Hide);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        Refresh().Forget();
    }

    public void Hide() => gameObject.SetActive(false);

    private async UniTaskVoid Refresh()
    {
        var records = await StatsService.Repo.LoadMatchesAsync();
        var stats = MatchStats.From(records);

        totalText.text = LocalizationManager.Get("STATS_TOTAL", stats.totalWins, stats.totalLosses, stats.WinRate * 100f);

        foreach (var row in rows)
        {
            stats.byCharacter.TryGetValue(row.localizationKey, out var cs);
            int w = cs?.wins ?? 0;
            int l = cs?.losses ?? 0;
            float rate = cs?.WinRate ?? 0f;

            row.text.text = LocalizationManager.Get("STATS_ROW", w, l, rate * 100f);
        }
    }
}
