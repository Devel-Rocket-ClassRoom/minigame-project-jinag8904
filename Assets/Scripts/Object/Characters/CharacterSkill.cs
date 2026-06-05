using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CaptureOutcome { Captured, Evaded, Reversed }

public abstract class CharacterSkill : ScriptableObject
{
    // 말 선택 UI 콜백: 후보 목록 → 선택 결과를 onPicked로 반환하는 코루틴
    public delegate IEnumerator PiecePickDelegate(List<Piece> candidates, Action<Piece> onPicked);

    public virtual CaptureOutcome OnCaptureAttempt(Piece target, Piece attacker, int totalDefenderCount = 0) => CaptureOutcome.Captured;
    public virtual void OnCapture(Piece piece, List<Piece> captured) {}
    public virtual bool OnBeingCaptured(Piece captured, Piece attacker) => false;
    public virtual void OnFinish(Piece piece) {}

    public virtual int MaxActiveUses => 0;
    public virtual string ActiveSkillName => "";
    public virtual bool CanUseActive(Player player) => MaxActiveUses > 0 && player.activeSkillUseCount < MaxActiveUses;
    public virtual void OnActiveActivated(Player player) => player.activeSkillUseCount++;

    // true면 버튼 클릭 즉시 CoOnActiveActivated 실행 (이동과 무관). false면 기존 방식(isActiveSkillOn).
    public virtual bool HasImmediateEffect => false;
    public virtual IEnumerator CoOnActiveActivated(Player player, PiecePickDelegate requestPick = null, Action<BoardNode> reposition = null, List<BoardNodeData> protectedNodes = null) { yield break; }

    // isActiveSkillOn 방식(HasImmediateEffect=false)에서 활성/비활성 시 비주얼 처리용
    public virtual void OnActiveTurnStart() {}
    public virtual void OnActiveTurnEnd() {}

    // 이동 후 경로/목적지에 적용할 효과. reposition은 노드 시각 갱신 콜백.
    public virtual void OnActiveMoveEffect(Player player, Piece mover, List<BoardNode> path, BoardNode dest, Action<BoardNode> reposition) {}

    // AI가 이동 후보에 액티브 스킬 보너스 점수를 계산. getNode로 BoardNodeData → BoardNode 변환.
    public virtual float EvaluateActiveMoveBonus(Player player, List<BoardNodeData> path, BoardNodeData dest, Func<BoardNodeData, BoardNode> getNode, AIPersonality personality) => 0f;
}
