using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 스킬 버튼에 부착. hover 시 자신이 들고 있는 스킬 정보를 SkillTooltipView로 띄운다.
/// 스킬은 GameMaster가 캐릭터 확정 후 SetSkill로 주입한다(버튼은 비활성이어도 hover 이벤트는 발생).
/// </summary>
public class SkillTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private SkillTooltipView view;   // 이 버튼이 쓸 패널(p1→p1패널, p2→p2패널)

    private CharacterSkill skill;
    private string charName;

    public void SetSkill(CharacterSkill skill, string charName)
    {
        this.skill = skill;
        this.charName = charName;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (skill != null && view != null) view.Show(skill, charName);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (view != null) view.Hide();
    }
}