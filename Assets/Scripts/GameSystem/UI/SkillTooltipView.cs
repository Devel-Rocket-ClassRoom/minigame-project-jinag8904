using UnityEngine;
using TMPro;

/// <summary>
/// 스킬 버튼 hover 시 캐릭터의 패시브/액티브 설명을 띄우는 공용 툴팁.
/// 씬에 1개 두고 SkillTooltipTrigger가 Show/Hide를 호출한다.
/// 패시브/액티브 각각 설명 키가 없으면 해당 줄을 통째로 숨긴다.
/// </summary>
public class SkillTooltipView : MonoBehaviour
{
    [SerializeField] private GameObject root;        // 툴팁 루트 (기본 숨김)
    [SerializeField] private TMP_Text titleText;     // 캐릭터 이름

    [SerializeField] private GameObject passiveLine; // 패시브 줄 묶음 (없으면 숨김)
    [SerializeField] private TMP_Text passiveText;   // 패시브 설명

    [SerializeField] private GameObject activeLine;  // 액티브 줄 묶음 (없으면 숨김)
    [SerializeField] private TMP_Text activeText;    // 액티브 설명

    private void Awake()
    {
        if (root == null) root = gameObject;
        root.SetActive(false);
    }

    public void Show(CharacterSkill skill, string charName)
    {
        if (skill == null) return;

        titleText.text = charName;

        SetLine(passiveLine, passiveText, skill.HasPassiveDesc,
            () => $"[{LocalizationManager.Get("LABEL_PASSIVE")}] {LocalizationManager.Get(skill.PassiveDescKey)}");

        SetLine(activeLine, activeText, skill.HasActiveDesc,
            () => $"[{LocalizationManager.Get("LABEL_ACTIVE")} · {skill.ActiveSkillName}] {LocalizationManager.Get(skill.ActiveDescKey)}");

        root.SetActive(true);
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
    }

    // 설명 키가 있으면 줄을 켜고 텍스트 채우고, 없으면 줄 묶음을 숨긴다.
    private static void SetLine(GameObject line, TMP_Text text, bool has, System.Func<string> build)
    {
        if (line != null) line.SetActive(has);
        if (has && text != null) text.text = build();
    }
}