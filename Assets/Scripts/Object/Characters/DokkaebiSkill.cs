using UnityEngine;

[CreateAssetMenu(fileName = "DokkaebiSkill", menuName = "Yutnori/Skill/Dokkaebi")]
public class DokkaebiSkill : CharacterSkill
{
    public override CaptureOutcome OnCaptureAttempt(Piece target, Piece attacker, int totalDefenderCount = 0)
    {
        int dokkaebiCount = totalDefenderCount > 0 ? totalDefenderCount : 1 + target.stackedPieces.Count;
        int attackerCount = 1 + attacker.stackedPieces.Count;
        float chance = (float)dokkaebiCount / (dokkaebiCount + attackerCount + 1f);
        if (Random.value < chance)
        {
            target.owner.AddBlackYut(1);
            return CaptureOutcome.Reversed;
        }
        return CaptureOutcome.Captured;
    }
}
