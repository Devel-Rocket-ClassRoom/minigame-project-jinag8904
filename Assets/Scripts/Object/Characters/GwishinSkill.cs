using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "GwishinSkill", menuName = "Yutnori/Skill/Gwishin")]
public class GwishinSkill : CharacterSkill
{
    public override void OnCapture(Piece piece, List<Piece> captured)
    {
        piece.owner.AddBlackYut(1);
        Debug.Log($"<color=purple>[귀신 저주] {piece.owner.name} 검은 윷 +1</color>");
    }
}
