using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "YutnoriBoard", menuName = "Yutnori/Board Data")]
public class BoardData : ScriptableObject
{
    [Header("모든 노드")]
    public List<BoardNodeData> allNodes;

    [Header("주요 노드 (빠른 접근용)")]
    public BoardNodeData startNode;
    public BoardNodeData goalNode;

    public BoardNodeData GetNodeById(int id)
    {
        return allNodes.FirstOrDefault(n => n.nodeId == id);
    }

    public bool ValidateConnections()   // 디버깅 용도
    {
        foreach (var node in allNodes)
        {
            if (node == null) continue;
            if (!node.isEnd && node.defaultNext == null)
            {
                Debug.LogError($"Node {node.nodeId} ({node.nodeName})에 defaultNext가 비어있음!");
                return false;
            }
        }
        return true;
    }
}