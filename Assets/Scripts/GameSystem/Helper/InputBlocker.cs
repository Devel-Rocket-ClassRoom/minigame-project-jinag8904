using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InputBlocker : MonoBehaviour
{
    [SerializeField] private Image blockerImage;

    // 3D 말 차단용 (DragAndDrop에서 체크)
    public static bool BlockingPieces { get; private set; }
    public static Piece AllowedPiece { get; private set; }
    public static BoardNode AllowedDestinationNode { get; private set; }

    // 원래 sibling 인덱스 복원용
    private readonly List<(RectTransform rt, Transform originalParent, int originalSiblingIndex)> _allowed = new();

    public void SetUIBlocked(bool blocked)
    {
        blockerImage.raycastTarget = blocked;
    }

    public void AllowButton(RectTransform btn)
    {
        _allowed.Add((btn, btn.parent, btn.GetSiblingIndex()));
        btn.SetParent(blockerImage.transform.parent, worldPositionStays: true);
        btn.SetAsLastSibling();
    }

    public void AllowPieceOnly(Piece piece)
    {
        BlockingPieces = true;
        AllowedPiece = piece;
    }

    public void AllowDestinationOnly(BoardNode node)
    {
        AllowedDestinationNode = node;
    }

    public void BlockPieces()
    {
        BlockingPieces = true;
        AllowedPiece = null;
    }

    public void Deactivate()
    {
        blockerImage.raycastTarget = false;

        foreach (var (rt, parent, idx) in _allowed)
        {
            rt.SetParent(parent, worldPositionStays: true);
            rt.SetSiblingIndex(idx);
        }
        _allowed.Clear();

        BlockingPieces = false;
        AllowedPiece = null;
        AllowedDestinationNode = null;
    }
}