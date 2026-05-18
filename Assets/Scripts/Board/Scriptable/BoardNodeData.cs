using UnityEngine;

[CreateAssetMenu(fileName = "BoardNode", menuName = "Yutnori/Board Node")]
public class BoardNodeData : ScriptableObject
{
    [Header("기본 정보")]
    public int nodeId;
    public string nodeName;

    [Header("노드 타입")]
    public NodeType nodeType;

    [Header("연결 (외곽 경로)")]
    public BoardNodeData defaultPrevious;
    public BoardNodeData defaultNext;

    [Header("연결 (지름길)")]
    public BoardNodeData shortcutNext;

    public bool isJunction => nodeType == NodeType.Junction;
    public bool isStart => nodeType == NodeType.Start;
    public bool isEnd => nodeType == NodeType.End;
}

public enum NodeType
{
    Normal,
    Junction,
    Center,
    Start,
    End
}