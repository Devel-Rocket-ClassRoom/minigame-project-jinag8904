using UnityEngine;

[CreateAssetMenu(fileName = "DokkaebiSkill", menuName = "Yutnori/Skill/Dokkaebi")]
public class DokkaebiSkill : CharacterSkill
{
    public override CaptureOutcome OnCaptureAttempt(Piece target, Piece attacker, int totalDefenderCount = 0)
    {
        int dokkaebiCount = totalDefenderCount > 0 ? totalDefenderCount : 1 + target.stackedPieces.Count;
        int attackerCount = 1 + attacker.stackedPieces.Count;
        float chance = (float)dokkaebiCount / (dokkaebiCount + attackerCount);
        GameLogUI.Log($"<color=purple>[씨름] 승리 확률: {chance * 100:0}% (도깨비 측 {dokkaebiCount}명 vs 공격자 {attackerCount}명)</color>");
        if (Random.value < chance)
        {
            target.owner.AddBlackYut(1);
            GameLogUI.Log($"<color=green>[씨름 승리] {target.owner.name} 검은 윷 +1</color>");
            return CaptureOutcome.Reversed;
        }
        return CaptureOutcome.Captured;
    }
}
