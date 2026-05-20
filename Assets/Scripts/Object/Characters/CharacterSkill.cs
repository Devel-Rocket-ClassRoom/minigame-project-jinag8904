using System.Collections.Generic;
using UnityEngine;

public enum CaptureOutcome { Captured, Evaded, Reversed }

public abstract class CharacterSkill : ScriptableObject
{
    public virtual CaptureOutcome OnCaptureAttempt(Piece target, Piece attacker) => CaptureOutcome.Captured;
    public virtual void OnCapture(Piece piece, List<Piece> captured) {}
    public virtual bool OnBeingCaptured(Piece captured, Piece attacker) => false;
    public virtual void OnFinish(Piece piece) {}
}
