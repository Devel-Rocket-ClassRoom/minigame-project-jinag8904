using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "GwishinSkill", menuName = "Yutnori/Skill/Gwishin")]
public class GwishinSkill : CharacterSkill
{
    public override void OnCapture(Piece piece, List<Piece> captured)
    {
        foreach (var leader in captured.Where(p => p.stackLeader == null))
        {
            leader.owner.AddWonhan(3);
            Debug.Log($"<color=purple>[귀신 저주] {leader.owner.name} 추가 원한 +3</color>");
        }
    }
}
