using UnityEngine;

[CreateAssetMenu(fileName = "MulgwishinSkill", menuName = "Yutnori/Skill/Mulgwishin")]
public class MulgwishinSkill : CharacterSkill
{
    public override bool OnBeingCaptured(Piece captured, Piece attacker)
    {
        attacker.owner.yutResults.Clear();
        Debug.Log($"<color=cyan>[물귀신 발목잡기] {attacker.owner.name}의 남은 윷 결과 전부 소모 + 보너스 굴림 없음</color>");
        return true;
    }
}
