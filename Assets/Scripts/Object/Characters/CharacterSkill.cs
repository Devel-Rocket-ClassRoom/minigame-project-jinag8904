using System.Collections.Generic;
using UnityEngine;

public abstract class CharacterSkill : ScriptableObject
{
    public virtual void OnCapture(Piece piece, List<Piece> captured) {}
    public virtual void OnBeingCaptured(Piece piece) {}
    public virtual void OnFinish(Piece piece) {}
}
