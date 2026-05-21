using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "GwishinSkill", menuName = "Yutnori/Skill/Gwishin")]
public class GwishinSkill : CharacterSkill
{
    public override void OnCapture(Piece piece, List<Piece> captured)
    {
        piece.owner.AddBlackYut(1);
        GameLogUI.Log($"<color=purple>[귀신 저주] {piece.owner.name} 검은 윷 +1</color>");
    }

    public override int MaxActiveUses => 1;
    public override string ActiveSkillName => "원혼 강림";

    public override void OnActiveMoveEffect(Player player, Piece mover, List<BoardNode> path, BoardNode dest, Action<BoardNode> reposition)
    {
        foreach (var pathNode in path.Where(n => n != dest))
        {
            var pathEnemyLeaders = pathNode.piecesOnNode
                .Where(p => p.owner != player && p.stackLeader == null)
                .ToList();

            foreach (var enemyLeader in pathEnemyLeaders)
            {
                int totalEnemyCount = 1 + enemyLeader.stackedPieces.Count;
                var outcome = enemyLeader.owner.Skill?.OnCaptureAttempt(enemyLeader, mover, totalEnemyCount)
                              ?? CaptureOutcome.Captured;
                if (outcome == CaptureOutcome.Reversed) continue;

                var capturedPieces = new[] { enemyLeader }.Concat(enemyLeader.stackedPieces).ToList();
                enemyLeader.owner.OnCaught(enemyLeader);
                enemyLeader.owner.AddWonhan(enemyLeader.stackedPieces.Count);
                enemyLeader.owner.Skill?.OnBeingCaptured(enemyLeader, mover);

                foreach (var caught in capturedPieces)
                {
                    pathNode.piecesOnNode.Remove(caught);
                    caught.currentNode = null;
                    caught.nodeHistory.Clear();
                    caught.stackLeader = null;
                    caught.stackedPieces.Clear();
                    caught.pieceObject.transform.position = caught.pieceObject.initPosition;
                }

                GameLogUI.Log($"<color=purple>[{ActiveSkillName}] 경로에서 적 {capturedPieces.Count}개 잡음!</color>");
                player.Skill?.OnCapture(mover, capturedPieces);
            }

            if (pathEnemyLeaders.Count > 0) reposition(pathNode);
        }
    }

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
