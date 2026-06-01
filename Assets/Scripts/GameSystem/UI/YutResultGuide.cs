using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 윷 결과(뒷도~모) 가이드 표를 그리는 재사용 컴포넌트.
/// 튜토리얼, 일시정지 도움말 등 어디에 두어도 동일하게 동작한다.
/// 셀(행)은 미리 배치해 두고, 이 스크립트가 스프라이트/색/텍스트만 채운다.
/// </summary>
public class YutResultGuide : MonoBehaviour
{
    [Serializable]
    public struct ResultRow
    {
        public Image[] yutIcons;            // 길이 4 (왼쪽부터 채움)
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI effectText;
    }

    [Header("Sprites")]
    [SerializeField] private Sprite flatSprite;     // Yut_Plane (평평면)
    [SerializeField] private Sprite markedSprite;   // Yut_Marked (표시 윷, 뒷도용)

    [Header("Tints")]
    [SerializeField] private Color frontColor = Color.white;                      // 앞면(밝게 = 원본)
    [SerializeField] private Color backColor = new Color(0.30f, 0.22f, 0.15f);    // 뒷면(어둡게)

    // 셀(자식)에서 런타임에 자동 수집한다. 자식 순서 = 뒷도→모.
    private ResultRow[] rows;

    // (앞면 개수, 표시윷 여부, 결과명 키, 효과 키)
    private static readonly (int flat, bool marked, string nameKey, string effKey)[] Data =
    {
        (1, true,  "YUT_BACKDO", "YUT_EFFECT_BACKDO"),
        (1, false, "YUT_DO",     "YUT_EFFECT_DO"),
        (2, false, "YUT_GAE",    "YUT_EFFECT_GAE"),
        (3, false, "YUT_GEOL",   "YUT_EFFECT_GEOL"),
        (4, false, "YUT_YUT",    "YUT_EFFECT_YUT"),
        (0, false, "YUT_MO",     "YUT_EFFECT_MO"),
    };

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += RefreshText;
        CollectRows();
        BuildIcons();
        RefreshText();
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= RefreshText;
    }

    private void CollectRows()
    {
        int n = transform.childCount;
        rows = new ResultRow[n];
        for (int r = 0; r < n; r++)
        {
            var cell = transform.GetChild(r);
            var icons = cell.Find("Icons");
            var labels = cell.Find("Labels");

            if (icons != null)
                rows[r].yutIcons = icons.GetComponentsInChildren<Image>(true);

            if (labels != null)
            {
                var texts = labels.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (texts.Length > 0) rows[r].nameText = texts[0];
                if (texts.Length > 1) rows[r].effectText = texts[1];
            }
        }
    }

    private void BuildIcons()
    {
        for (int r = 0; r < rows.Length && r < Data.Length; r++)
        {
            var d = Data[r];
            var icons = rows[r].yutIcons;
            if (icons == null) continue;

            for (int i = 0; i < icons.Length; i++)
            {
                if (icons[i] == null) continue;

                bool isFront = i < d.flat;                       // 앞면을 왼쪽부터 채움
                bool marked = isFront && d.marked && i == 0;     // 뒷도: 첫 칸만 표시 윷
                icons[i].sprite = marked ? markedSprite : flatSprite;
                icons[i].color = isFront ? frontColor : backColor;
            }
        }
    }

    private void RefreshText()
    {
        for (int r = 0; r < rows.Length && r < Data.Length; r++)
        {
            if (rows[r].nameText != null)
                rows[r].nameText.text = LocalizationManager.Get(Data[r].nameKey);
            if (rows[r].effectText != null)
                rows[r].effectText.text = LocalizationManager.Get(Data[r].effKey);
        }
    }
}
