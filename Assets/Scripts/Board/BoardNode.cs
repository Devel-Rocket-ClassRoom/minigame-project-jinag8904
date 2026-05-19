using UnityEngine;
using System.Collections.Generic;

public class BoardNode : MonoBehaviour
{
    [Header("데이터 참조")]
    public BoardNodeData data;

    [HideInInspector]
    public List<Piece> piecesOnNode = new List<Piece>();

    public GameObject highlight;

    public Vector3 GetPiecePosition(int index, int total = 0)
    {
        int count = total > 0 ? total : piecesOnNode.Count;
        var base_ = transform.position + new Vector3(0f, 0.1f, 0f);
        return count switch
        {
            1 => base_,
            2 => base_ + new Vector3((index == 0 ? -1 : 1) * 0.55f, 0f, 0f),
            3 => index switch { 0 => base_ + new Vector3(-0.42f, 0f, -0.35f), 1 => base_ + new Vector3(0.42f, 0f, -0.35f), _ => base_ + new Vector3(0f, 0f, 0.52f) },
            _ => base_ + new Vector3((index % 2 == 0 ? -1 : 1) * 0.55f, 0f, (index < 2 ? 1 : -1) * 0.55f),
        };
    }

    public void SetHighlight(bool active)
    {
        if (highlight != null)
            highlight.SetActive(active);
    }
}