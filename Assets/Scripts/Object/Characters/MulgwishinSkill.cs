using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    public override int MaxActiveUses => 1;
    public override string ActiveSkillName => "제물";
    public override bool HasImmediateEffect => true;

    public override bool CanUseActive(Player player) =>
        MaxActiveUses > 0 && player.activeSkillUseCount < MaxActiveUses &&
        player.pieces.Any(p => !p.hasFinished && p.currentNode != null && p.stackLeader == null);

    public override IEnumerator CoOnActiveActivated(Player player, PiecePickDelegate requestPick = null, Action<BoardNode> reposition = null)
    {
        player.activeSkillUseCount++;

        var candidates = player.pieces
            .Where(p => !p.hasFinished && p.currentNode != null && p.stackLeader == null)
            .ToList();

        Piece sacrificed = null;
        if (requestPick != null)
            yield return requestPick(candidates, p => sacrificed = p);
        else
        {
            var solos = candidates.Where(p => p.stackedPieces.Count == 0).ToList();
            var pool = solos.Count > 0 ? solos : candidates;
            sacrificed = pool.OrderBy(p => p.nodeHistory.Count).First();
        }

        if (sacrificed == null) yield break;

        var node = sacrificed.currentNode;
        node.piecesOnNode.Remove(sacrificed);

        // 업힌 말이 있으면 첫 번째 말을 새 리더로 지정
        if (sacrificed.stackedPieces.Count > 0)
        {
            var newLeader = sacrificed.stackedPieces[0];
            newLeader.stackLeader = null;
            newLeader.stackedPieces.Clear();
            for (int i = 1; i < sacrificed.stackedPieces.Count; i++)
            {
                newLeader.stackedPieces.Add(sacrificed.stackedPieces[i]);
                sacrificed.stackedPieces[i].stackLeader = newLeader;
            }
            sacrificed.stackedPieces.Clear();
        }

        sacrificed.currentNode = null;
        sacrificed.nodeHistory.Clear();
        sacrificed.pieceObject.transform.position = sacrificed.pieceObject.initPosition;

        reposition?.Invoke(node);

        player.AddBlackYut(2);
        Debug.Log($"<color=cyan>[제물] {player.name} 말 희생 → 검은 윷 +2</color>");
    }
}
