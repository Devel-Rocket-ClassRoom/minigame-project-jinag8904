using System.Collections.Generic;

public struct MoveResult
{
    public Dictionary<BoardNodeData, (YutResult yr, BoardNodeData prevNode)> Destinations;
    public List<YutResult> OutResults;
}

public static class PieceMoveCalculator
{
    public static MoveResult ComputeMoves(Piece piece, List<YutResult> yutResults, BoardData board)
    {
        var destinations = new Dictionary<BoardNodeData, (YutResult, BoardNodeData)>();
        var outResults = new List<YutResult>();

        foreach (var yr in yutResults)
        {
            if (yr == YutResult.BACKDO)
            {
                if (piece.currentNode == null) continue;

                BoardNodeData dest;
                if (piece.currentNode.data == board.startNode)
                    dest = board.goalNode;
                else if (piece.previousNode == null)
                    continue;  // 이전 노드 없으면 백도 불가
                else
                    dest = piece.previousNode.data;

                if (!destinations.ContainsKey(dest))
                    destinations[dest] = (yr, null);
                continue;
            }

            var pathMap = new Dictionary<BoardNodeData, BoardNodeData>();
            bool canOut = false;

            if (piece.currentNode == null)
                TraverseWithPrev(board.startNode, (int)yr - 1, false, null, pathMap, ref canOut);
            else
                TraverseWithPrev(piece.currentNode.data, (int)yr, true, null, pathMap, ref canOut);

            foreach (var kv in pathMap)
                if (!destinations.ContainsKey(kv.Key))
                    destinations[kv.Key] = (yr, kv.Value);

            if (canOut && !outResults.Contains(yr))
                outResults.Add(yr);
        }

        return new MoveResult { Destinations = destinations, OutResults = outResults };
    }

    // cameFrom이 진입 방향 필터 겸 직전 노드 역할. canOut은 완주 도달 여부.
    private static void TraverseWithPrev(BoardNodeData node, int stepsLeft, bool isStart, BoardNodeData cameFrom, Dictionary<BoardNodeData, BoardNodeData> results, ref bool canOut)
    {
        if (stepsLeft == 0)
        {
            results[node] = cameFrom;
            return;
        }
        if (node.isEnd)
        {
            canOut = true;
            return;
        }

        if (node.isCenter && !isStart)
        {
            var next = cameFrom == node.defaultNextEntry ? node.defaultNext : node.shortcutNext;
            if (next != null) TraverseWithPrev(next, stepsLeft - 1, false, node, results, ref canOut);
        }
        else
        {
            if (node.defaultNext != null)
                TraverseWithPrev(node.defaultNext, stepsLeft - 1, false, node, results, ref canOut);

            if (isStart && (node.isJunction || node.isCenter) && node.shortcutNext != null)
                TraverseWithPrev(node.shortcutNext, stepsLeft - 1, false, node, results, ref canOut);
        }
    }
}
