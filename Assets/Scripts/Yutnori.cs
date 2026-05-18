using System.Collections.Generic;

public static class Yutnori
{
    // 해당 말이 현재 플레이어의 모든 윷 결과로 갈 수 있는 목적지를 반환
    // key: 목적지 노드 데이터, value: 사용된 윷 결과
    public static Dictionary<BoardNodeData, YutResult> GetAllPossibleDestinations(Piece piece, List<YutResult> yutResults, BoardData board)
    {
        var result = new Dictionary<BoardNodeData, YutResult>();
        foreach (var yr in yutResults)
        {
            foreach (var dest in GetDestinations(piece, yr, board))
            {
                if (dest == null) continue; // out은 GetAllOutResults에서 별도 처리
                if (!result.ContainsKey(dest))
                    result[dest] = yr;
            }
        }
        return result;
    }

    private static List<BoardNodeData> GetDestinations(Piece piece, YutResult yutResult, BoardData board)
    {
        if (yutResult == YutResult.BACKDO)
        {
            if (piece.currentNode == null) return new List<BoardNodeData>();
            if (piece.currentNode.data == board.startNode) return new List<BoardNodeData> { board.goalNode };
            if (piece.previousNode == null) return new List<BoardNodeData>();
            return new List<BoardNodeData> { piece.previousNode.data };
        }

        var results = new HashSet<BoardNodeData>();
        if (piece.currentNode == null)
            Traverse(board.startNode, (int)yutResult - 1, false, null, results);
        else
            Traverse(piece.currentNode.data, (int)yutResult, true, null, results);
        return new List<BoardNodeData>(results);
    }

    private static void Traverse(BoardNodeData node, int stepsLeft, bool isStart, BoardNodeData cameFrom, HashSet<BoardNodeData> results)
    {
        if (stepsLeft == 0)
        {
            results.Add(node);
            return;
        }
        if (node.isEnd)
        {
            results.Add(null); // null = out (초과 이동)
            return;
        }

        if (node.isCenter && !isStart)
        {
            // cameFrom이 defaultNextEntry와 일치하면 defaultNext, 아니면 shortcutNext로 진행
            var next = cameFrom == node.defaultNextEntry ? node.defaultNext : node.shortcutNext;
            if (next != null) Traverse(next, stepsLeft - 1, false, node, results);
        }
        else
        {
            if (node.defaultNext != null)
                Traverse(node.defaultNext, stepsLeft - 1, false, node, results);

            if (isStart && (node.isJunction || node.isCenter) && node.shortcutNext != null)
                Traverse(node.shortcutNext, stepsLeft - 1, false, node, results);
        }
    }

    // out을 유발하는 윷 결과 전체 반환 (중복 제거)
    public static List<YutResult> GetAllOutResults(Piece piece, List<YutResult> yutResults, BoardData board)
    {
        var result = new List<YutResult>();
        foreach (var yr in yutResults)
        {
            if (yr == YutResult.BACKDO || result.Contains(yr)) continue;
            if (GetDestinations(piece, yr, board).Contains(null))
                result.Add(yr);
        }
        return result;
    }

    // 목적지의 경로상 직전 노드를 반환 — key: 목적지, value: 직전 노드(null이면 없음)
    public static Dictionary<BoardNodeData, BoardNodeData> GetPenultimateNodes(Piece piece, List<YutResult> yutResults, BoardData board)
    {
        var result = new Dictionary<BoardNodeData, BoardNodeData>();
        foreach (var yr in yutResults)
        {
            if (yr == YutResult.BACKDO) continue;

            var pathMap = new Dictionary<BoardNodeData, BoardNodeData>();
            if (piece.currentNode == null)
                TraverseWithPrev(board.startNode, (int)yr - 1, false, null, pathMap);
            else
                TraverseWithPrev(piece.currentNode.data, (int)yr, true, null, pathMap);

            foreach (var kv in pathMap)
                if (kv.Key != null && !result.ContainsKey(kv.Key))
                    result[kv.Key] = kv.Value;
        }
        return result;
    }

    // cameFrom이 진입 방향 필터 겸 직전 노드 역할을 함
    private static void TraverseWithPrev(BoardNodeData node, int stepsLeft, bool isStart, BoardNodeData cameFrom, Dictionary<BoardNodeData, BoardNodeData> results)
    {
        if (stepsLeft == 0)
        {
            results[node] = cameFrom;
            return;
        }
        if (node.isEnd)
            return;

        if (node.isCenter && !isStart)
        {
            var next = cameFrom == node.defaultNextEntry ? node.defaultNext : node.shortcutNext;
            if (next != null) TraverseWithPrev(next, stepsLeft - 1, false, node, results);
        }
        else
        {
            if (node.defaultNext != null)
                TraverseWithPrev(node.defaultNext, stepsLeft - 1, false, node, results);

            if (isStart && (node.isJunction || node.isCenter) && node.shortcutNext != null)
                TraverseWithPrev(node.shortcutNext, stepsLeft - 1, false, node, results);
        }
    }
}