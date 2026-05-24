using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AIController : MonoBehaviour
{
    [SerializeField] private GameMaster gm;
    [SerializeField] private BoardData boardData;

    // Throw → 이동 루프 → 검은 윷 처리
    public IEnumerator DecideTurn()
    {
        var ai = gm.CurrPlayer;

        ai.Throw();
        while (ai.yutResults.Count > 0 &&
               (ai.yutResults[^1] == YutResult.Yut || ai.yutResults[^1] == YutResult.Mo))
        {
            ai.Throw();
        }
        GameLogUI.Log("<color=#00CFCF>[AI] 윷 결과가 나왔습니다!</color>");
        GameLogUI.UpdateYutResults(ai.yutResults, ai.name);
        yield return new WaitForSeconds(1);

        while (true)
        {
            while (ai.yutResults.Count > 0)
            {
                ai.yutResults.RemoveAll(yr => yr == YutResult.BACKDO && !ai.pieces.Any(p =>
                    !p.hasFinished && p.stackLeader == null && p.currentNode != null &&
                    (p.currentNode.data == boardData.startNode || p.nodeHistory.Count > 0)));  // 못 움직이는 경우 걸러내기
                if (ai.yutResults.Count == 0) break;

                // 즉시 발동 스킬 판단
                if (ai.Skill?.HasImmediateEffect == true && ai.Skill?.CanUseActive(ai) == true && IsNearFinishWithSacrificable(ai))
                {
                    yield return StartCoroutine(ai.Skill.CoOnActiveActivated(ai, reposition: gm.RepositionNode));
                    GameLogUI.Log($"<color=#00CFCF>[AI] {ai.Skill.ActiveSkillName} 발동!</color>");
                }

                var (piece, dest, pushPath, usedYR, isOut, useActiveSkill) = FindBestMove(ai);

                if (isOut)
                    GameLogUI.Log($"<color=#00CFCF>[AI] 완주 선택 ({usedYR})</color>");
                else if (useActiveSkill)
                    GameLogUI.Log($"<color=#00CFCF>[AI] {ai.Skill.ActiveSkillName} 사용! {GameLogUI.GetYutName(usedYR)} > {dest?.nodeName}</color>");
                else
                    GameLogUI.Log($"<color=#00CFCF>[AI] {GameLogUI.GetYutName(usedYR)} > {dest?.nodeName}</color>");

                yield return StartCoroutine(gm.ApplyAIMove(piece, dest, pushPath, usedYR, isOut, useActiveSkill));

                if (ai.AllFinished) yield break;

                yield return new WaitForSeconds(1f);
                GameLogUI.UpdateYutResults(ai.yutResults, ai.name);
            }

            if (!ai.HasBlackYut || !ShouldUseBlackYut()) break;

            ai.Throw(isBlackYut: true);
            GameLogUI.UpdateYutResults(ai.yutResults, ai.name);
            yield return new WaitForSeconds(1f);
        }
    }

    // 전체 말 순회, 완주/이동 후보 점수 매겨서 최고점 반환
    private (Piece piece, BoardNodeData dest, List<BoardNodeData> pushPath, YutResult usedYR, bool isOut, bool useActiveSkill) FindBestMove(Player ai)
    {
        Piece bestPiece = null;
        BoardNodeData bestTarget = null;
        List<BoardNodeData> bestPushPath = null;
        YutResult bestUse = default;
        bool bestIsOut = false;
        bool bestUseActiveSkill = false;

        var personality = ai.characterData != null ? ai.characterData.aiPersonality : new AIPersonality();
        var opponent = gm.GetOpponent(ai);
        bool canUseSkill = ai.Skill?.CanUseActive(ai) == true;

        float bestScore = float.MinValue;
        foreach (var piece in ai.pieces.Where(p => !p.hasFinished && p.stackLeader == null))
        {
            var moves = PieceMoveCalculator.ComputeMoves(piece, ai.yutResults, boardData);

            // 완주 후보
            foreach (var yr in moves.OutResults)
            {
                float score = ScoreMove(piece, null, yr, personality, ai, opponent, isOut: true);
                if (score > bestScore)
                {
                    bestScore = score; bestPiece = piece; bestTarget = null; bestPushPath = null; bestUse = yr; bestIsOut = true; bestUseActiveSkill = false;
                }
            }

            // 이동 후보
            foreach (var kv in moves.Destinations)
            {
                float score = ScoreMove(piece, kv.Key, kv.Value.yr, personality, ai, opponent, isOut: false);
                if (kv.Value.pushPath != null && kv.Value.pushPath.Count > 1 &&
                    kv.Value.pushPath[0].isJunction && kv.Value.pushPath[1] == kv.Value.pushPath[0].shortcutNext)
                    score += personality.progressWeight * 3f;
                bool useSkillForThis = false;
                if (canUseSkill && kv.Value.pushPath != null)
                {
                    float bonus = ai.Skill.EvaluateActiveMoveBonus(ai, kv.Value.pushPath, kv.Key, gm.GetNode, personality);
                    if (bonus > 0f)
                    {
                        score += bonus;
                        useSkillForThis = true;
                    }
                }
                if (score > bestScore)
                {
                    bestScore = score; bestPiece = piece; bestTarget = kv.Key; bestPushPath = kv.Value.pushPath; bestUse = kv.Value.yr; bestIsOut = false; bestUseActiveSkill = useSkillForThis;
                }
            }
        }

        return (bestPiece, bestTarget, bestPushPath, bestUse, bestIsOut, bestUseActiveSkill);
    }

    // 잡기/전진/완주/업기 - 각각 가중치 적용 + random
    private float ScoreMove(Piece piece, BoardNodeData dest, YutResult usedYR, AIPersonality p, Player ai, Player opponent, bool isOut)
    {
        float score = 0f;

        // 1. 완주 가능: p.finishWeight * 10f
        if (isOut) 
            score += p.finishWeight * 10f;

        // 2. 일반적인 상황
        else
        {
            // 2-1. 잡기 가능: p.captureWeight * 5f
            bool canCapture = opponent.pieces.Any(op => !op.hasFinished && op.currentNode != null && op.currentNode.data == dest);
            if (canCapture) score += p.captureWeight * 5f;

            // 2-2. 전진량: p.progressWeight * (int)used
            score += p.progressWeight * (int)usedYR;

            // 2-3. 업기 가능: p.stackWeight * 3f
            bool canStack = ai.pieces.Any(pp => pp != piece && !pp.hasFinished && pp.stackLeader == null
                && pp.currentNode != null && pp.currentNode.data == dest);
            if (canStack) score += p.stackWeight * 3f;
        }

        // 3. 랜덤 노이즈 섞기
        score += p.randomness * Random.value;
        return score;
    }

    private bool IsNearFinishWithSacrificable(Player ai)
    {
        if (boardData.nearFinishNodes == null || boardData.nearFinishNodes.Count == 0) return false;

        var nearFinishPieces = ai.pieces
            .Where(p => !p.hasFinished && p.currentNode != null &&
                        boardData.nearFinishNodes.Contains(p.currentNode.data))
            .ToList();

        if (nearFinishPieces.Count == 0) return false;

        return ai.pieces.Any(p => !p.hasFinished && p.currentNode != null &&
                                  p.stackLeader == null && !nearFinishPieces.Contains(p));
    }

    // 일단 true
    private bool ShouldUseBlackYut() => true;
}