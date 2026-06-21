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
        return true;
    }

    public override int ActiveCooldown => 2;
    public override string ActiveSkillName => LocalizationManager.Get("SKILL_MULGWISHIN_ACTIVE");
    public override bool HasImmediateEffect => true;

    public override bool CanUseActive(Player player) => base.CanUseActive(player) && player.pieces.Any(p => !p.hasFinished && p.currentNode != null && p.stackLeader == null);

    public override IEnumerator CoOnActiveActivated(Player player, PiecePickDelegate requestPick = null, Action<BoardNode> reposition = null, List<BoardNodeData> protectedNodes = null)
    {
        VFXManager.Instance?.VignetteHoldOn(new Color(0.043f, 0.482f, 0.541f), 0.35f);  // 청록 ON
        try
        {
            var candidates = player.pieces
                .Where(p => !p.hasFinished && p.currentNode != null && p.stackLeader == null)
                .ToList();

            Piece sacrificed = null;
            if (requestPick != null)
                yield return requestPick(candidates, p => sacrificed = p);
            else
            {
                var pool = candidates;
                // 완주 임박 말은 희생 후보에서 제외 (전멸/임박말 손실 방지)
                if (protectedNodes != null)
                {
                    var safe = candidates.Where(p => !protectedNodes.Contains(p.currentNode.data)).ToList();
                    if (safe.Count > 0) pool = safe;
                }
                var solos = pool.Where(p => p.stackedPieces.Count == 0).ToList();
                pool = solos.Count > 0 ? solos : pool;
                sacrificed = pool.OrderBy(p => p.nodeHistory.Count).First();
            }

            if (sacrificed == null) yield break;   // 취소 시 여기서 종료 → 쿨타임 미차감

            OnActiveActivated(player);              // 희생 확정 후에만 쿨타임 차감

            var node = sacrificed.currentNode;
            VFXManager.Instance?.PlayMulgwishinParticle(node.transform.position);
            VFXManager.Instance?.ShowMulgwishinActiveBanner();   // 발동 명시 (특히 AI 차례에 잘 보이도록)

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
        }
        finally
        {
            VFXManager.Instance?.VignetteHoldOff();   // 스킬 끝나면 청록 OFF
        }
    }
}
