using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AIController : MonoBehaviour
{
    [SerializeField] private GameMaster gm;
    [SerializeField] private BoardData boardData;

    private YutThrowController yutThrowController;

    public void Init(YutThrowController controller)
    {
        yutThrowController = controller;
    }

    // Throw → 이동 루프 → 검은 윷 처리
    public IEnumerator DecideTurn()
    {
        if (TutorialManager.isTutorial)
        {
            yield return new WaitForSeconds(1f);
            yield break;
        }

        var ai = gm.CurrPlayer;

        GameEvents.InvokeYutThrown(ai.playerId);
        yield return StartCoroutine(yutThrowController.CoThrow());
        ai.AddThrowResult(yutThrowController.LastResult);
        while (ai.yutResults.Count > 0 &&
               (ai.yutResults[^1] == YutResult.Yut || ai.yutResults[^1] == YutResult.Mo))
        {
            VFXManager.Instance?.PlayBonusThrow();
            GameEvents.InvokeYutThrown(ai.playerId);
            yield return StartCoroutine(yutThrowController.CoThrow());
            ai.AddThrowResult(yutThrowController.LastResult);
        }
        GameLogUI.UpdateYutResults(ai.yutResults, ai.name);
        yield return new WaitForSeconds(0.4f);

        while (true)
        {
            while (ai.yutResults.Count > 0)
            {
                gm.RefreshAISkillButton();   // 현재 사용 가능 여부를 버튼 색에 반영 (사용 후 흐려짐)

                // 뒷도만 남았고 뒷걸음할 말이 없을 때만 정리 (윷 등 다른 결과로 말을 먼저 올리면 뒷도 보존)
                bool noPieceForBackdo = !ai.pieces.Any(p =>
                    !p.hasFinished && p.stackLeader == null && p.currentNode != null &&
                    (p.currentNode.data == boardData.startNode || p.nodeHistory.Count > 0));
                if (noPieceForBackdo && ai.yutResults.All(yr => yr == YutResult.BACKDO))
                {
                    ai.yutResults.Clear();
                    GameLogUI.UpdateYutResults(ai.yutResults, ai.name);
                    break;
                }

                // 즉시 발동 스킬 판단
                if (ai.Skill?.HasImmediateEffect == true && ai.Skill?.CanUseActive(ai) == true && IsNearFinishWithSacrificable(ai))
                {
                    yield return StartCoroutine(ai.Skill.CoOnActiveActivated(ai, reposition: gm.RepositionNode));
                }

                var (piece, dest, pushPath, usedYR, isOut, useActiveSkill) = FindBestMove(ai);

                // 스킬로 말이 사라지는 등으로 둘 수 있는 수가 없으면(뒷도만 남음) 결과 비우고 종료
                if (piece == null)
                {
                    ai.yutResults.Clear();
                    GameLogUI.UpdateYutResults(ai.yutResults, ai.name);
                    break;
                }

                yield return StartCoroutine(gm.ApplyAIMove(piece, dest, pushPath, usedYR, isOut, useActiveSkill));

                if (ai.AllFinished)
                {
                    if (gm.IsBoardCamActive)
                        yield return StartCoroutine(gm.CoReleaseAICamera());
                    yield break;
                }

                yield return new WaitForSeconds(0.3f);
                GameLogUI.UpdateYutResults(ai.yutResults, ai.name);
            }

            // 이번에 던진 윷 결과로 둘 이동을 모두 끝냄 → 보드캠 내려 테이블뷰 복귀
            if (gm.IsBoardCamActive)
                yield return StartCoroutine(gm.CoReleaseAICamera());

            if (!ai.HasBlackYut || !ShouldUseBlackYut()) break;

            yield return StartCoroutine(gm.CoAITableViewDwell());   // 이동 카메라 복귀 대기 + 테이블뷰 머무름

            ai.ConsumeBlackYut();   // 던지기 전 즉시 개수 차감 + UI 갱신
            GameEvents.InvokeYutThrown(ai.playerId);
            VFXManager.Instance?.PlayBlackYutThrow();
            yield return StartCoroutine(yutThrowController.CoThrow(isBlackYut: true));
            ai.AddThrowResult(yutThrowController.LastResult);
            GameLogUI.UpdateYutResults(ai.yutResults, ai.name);
            yield return new WaitForSeconds(0.4f);
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