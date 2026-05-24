using UnityEngine;

[CreateAssetMenu(fileName = "DokkaebiSkill", menuName = "Yutnori/Skill/Dokkaebi")]
public class DokkaebiSkill : CharacterSkill
{
    public override CaptureOutcome OnCaptureAttempt(Piece target, Piece attacker, int totalDefenderCount = 0)
    {
        int dokkaebiCount = totalDefenderCount > 0 ? totalDefenderCount : 1 + target.stackedPieces.Count;
        int attackerCount = 1 + attacker.stackedPieces.Count;
        float chance = (float)dokkaebiCount / (dokkaebiCount + attackerCount + 1f);
        GameLogUI.Log($"<color=purple>{LocalizationManager.Get("SKILL_DOKKAEBI_SUMO_LOG", dokkaebiCount, attackerCount, (int)(chance * 100))}</color>");
        if (Random.value < chance)
        {
            target.owner.AddBlackYut(1);
            GameLogUI.Log($"<color=green>{LocalizationManager.Get("SKILL_DOKKAEBI_SUMO_WIN", target.owner.name)}</color>");
            return CaptureOutcome.Reversed;
        }
        return CaptureOutcome.Captured;
    }
}
