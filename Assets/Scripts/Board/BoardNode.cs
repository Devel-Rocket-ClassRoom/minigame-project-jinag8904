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
        return count switch
        {
            1 => transform.position,
            2 => transform.position + new Vector3((index == 0 ? -1 : 1) * 0.15f, 0f, 0f),
            3 => transform.position + new Vector3((index - 1) * 0.15f, 0f, index == 2 ? 0.13f : -0.08f),
            _ => transform.position + new Vector3((index % 2 == 0 ? -1 : 1) * 0.13f, 0f, (index < 2 ? 1 : -1) * 0.13f),
        };
    }

    public void SetHighlight(bool active)
    {
        if (highlight != null)
            highlight.SetActive(active);
    }

    //private void OnDrawGizmos()
    //{
    //    if (data == null) return;

    //    if (data.defaultNext != null)
    //    {
    //        var nextNode = FindNodeInScene(data.defaultNext);
    //        if (nextNode != null)
    //        {
    //            Gizmos.color = Color.white;
    //            Gizmos.DrawLine(transform.position, nextNode.transform.position);
    //        }
    //    }

    //    if (data.shortcutNext != null)
    //    {
    //        var shortcutNode = FindNodeInScene(data.shortcutNext);
    //        if (shortcutNode != null)
    //        {
    //            Gizmos.color = Color.red;
    //            Gizmos.DrawLine(transform.position, shortcutNode.transform.position);
    //        }
    //    }
    //}

    //private BoardNode FindNodeInScene(BoardNodeData targetData)
    //{
    //    var allNodes = FindObjectsByType<BoardNode>(FindObjectsSortMode.None);
    //    foreach (var n in allNodes)
    //        if (n.data == targetData) return n;
    //    return null;
    //}
}