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
        GameLogUI.Log($"<color=#00CFCF>{LocalizationManager.Get("SKILL_MULGWISHIN_ON_CAPTURED", attacker.owner.name)}</color>");
        return true;
    }

    public override int MaxActiveUses => 1;
    public override string ActiveSkillName => LocalizationManager.Get("SKILL_MULGWISHIN_ACTIVE");
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
        GameLogUI.Log($"<color=#00CFCF>{LocalizationManager.Get("SKILL_MULGWISHIN_SACRIFICE", player.name)}</color>");
    }
}
