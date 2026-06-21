using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "GwishinSkill", menuName = "Yutnori/Skill/Gwishin")]
public class GwishinSkill : CharacterSkill
{
    public override void OnCapture(Piece piece, List<Piece> captured)
    {
        piece.owner.AddWonhan(3);
    }

    public override int ActiveCooldown => 2;
    public override string ActiveSkillName => LocalizationManager.Get("SKILL_GWISHIN_ACTIVE");

    public override void OnActiveMoveEffect(Player player, Piece mover, List<BoardNode> path, BoardNode dest, Action<BoardNode> reposition)
    {
        bool capturedAny = false;
        foreach (var pathNode in path.Where(n => n != dest && n != null))
        {
            var pathEnemyLeaders = pathNode.piecesOnNode
                .Where(p => p.owner != player && p.stackLeader == null)
                .ToList();

            foreach (var enemyLeader in pathEnemyLeaders)
            {
                var capturedPieces = new[] { enemyLeader }.Concat(enemyLeader.stackedPieces).ToList();
                enemyLeader.owner.OnCaught(enemyLeader);
                enemyLeader.owner.AddWonhan(enemyLeader.stackedPieces.Count);
                //enemyLeader.owner.Skill?.OnBeingCaptured(enemyLeader, mover);

                foreach (var caught in capturedPieces)
                {
                    pathNode.piecesOnNode.Remove(caught);
                    caught.currentNode = null;
                    caught.nodeHistory.Clear();
                    caught.stackLeader = null;
                    caught.stackedPieces.Clear();
                    caught.pieceObject.transform.position = caught.pieceObject.initPosition;
                }

                player.Skill?.OnCapture(mover, capturedPieces);
                VFXManager.Instance?.PlayGwishin(pathNode.transform.position);
            }

            if (pathEnemyLeaders.Count > 0)
            {
                reposition(pathNode);
                capturedAny = true;
            }
        }

        if (capturedAny) VFXManager.Instance?.ShowGwishinActiveBanner();
    }

    // 귀신 검붉은 비네트 (#A0121B) — 액티브 스킬 대기 중 유지
    public override void OnActiveTurnStart() =>
        VFXManager.Instance?.VignetteHoldOn(new Color(0.627f, 0.071f, 0.106f), 0.35f);

    public override void OnActiveTurnEnd() =>
        VFXManager.Instance?.VignetteHoldOff();

    public override float EvaluateActiveMoveBonus(Player player, List<BoardNodeData> path, BoardNodeData dest, Func<BoardNodeData, BoardNode> getNode, AIPersonality personality)
    {
        if (path == null) return 0f;
        int pathEnemies = 0;
        foreach (var nodeData in path)
        {
            if (nodeData == dest) continue;
            var node = getNode(nodeData);
            if (node != null)
                pathEnemies += node.piecesOnNode.Count(p => p.owner != player && p.stackLeader == null);
        }
        return personality.captureWeight * 5f * pathEnemies;
    }
}
