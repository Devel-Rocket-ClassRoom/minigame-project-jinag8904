using UnityEngine;
using System.Collections.Generic;

public class BoardNode : MonoBehaviour
{
    [Header("데이터 참조")]
    public BoardNodeData data;

    [Header("말 배치 위치")]
    public Transform[] piecePositions;

    [HideInInspector]
    public List<Piece> piecesOnNode = new List<Piece>();

    public GameObject highlight;

    public void SetHighlight(bool active)
    {
        if (highlight != null)
            highlight.SetActive(active);
    }

    private void OnDrawGizmos()
    {
        if (data == null) return;

        if (data.defaultNext != null)
        {
            var nextNode = FindNodeInScene(data.defaultNext);
            if (nextNode != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(transform.position, nextNode.transform.position);
            }
        }

        if (data.shortcutNext != null)
        {
            var shortcutNode = FindNodeInScene(data.shortcutNext);
            if (shortcutNode != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, shortcutNode.transform.position);
            }
        }
    }

    private BoardNode FindNodeInScene(BoardNodeData targetData)
    {
        var allNodes = FindObjectsByType<BoardNode>(FindObjectsSortMode.None);
        foreach (var n in allNodes)
            if (n.data == targetData) return n;
        return null;
    }
}