using UnityEngine;

[CreateAssetMenu(fileName = "DokkaebiSkill", menuName = "Yutnori/Skill/Dokkaebi")]
public class DokkaebiSkill : CharacterSkill
{
    public override CaptureOutcome OnCaptureAttempt(Piece target, Piece attacker)
    {
        int dokkaebiCount = 1 + target.stackedPieces.Count;
        int attackerCount = 1 + attacker.stackedPieces.Count;
        float chance = (float)dokkaebiCount / (dokkaebiCount + attackerCount);
        Debug.Log($"<color=purple>[씨름] 승리 확률: {chance * 100:0}% (도깨비 {dokkaebiCount}명 vs 공격자 {attackerCount}명)</color>");
        return Random.value < chance ? CaptureOutcome.Reversed : CaptureOutcome.Captured;
    }
}
