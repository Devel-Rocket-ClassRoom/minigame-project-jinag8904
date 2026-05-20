using System.Collections.Generic;

public struct MoveResult
{
    // pushPath: null = 뒷도(스택 팝), non-null = 전진(목록 노드들을 히스토리에 순서대로 푸시)
    public Dictionary<BoardNodeData, (YutResult yr, List<BoardNodeData> pushPath)> Destinations;
    public List<YutResult> OutResults;
}

public static class PieceMoveCalculator
{
    public static MoveResult ComputeMoves(Piece piece, List<YutResult> yutResults, BoardData board)
    {
        var destinations = new Dictionary<BoardNodeData, (YutResult, List<BoardNodeData>)>();
        var outResults = new List<YutResult>();

        foreach (var yr in yutResults)
        {
            if (yr == YutResult.BACKDO)
            {
                if (piece.currentNode == null) continue;

                BoardNodeData dest;
                List<BoardNodeData> pushPath;

                if (piece.currentNode.data == board.startNode && piece.nodeHistory.Count == 0)
                {
                    // 시작 노드에서 뒷도: 참먹이로 이동.
                    // startNode를 히스토리에 남겨 참먹이에서 다시 뒷도 시 시작 노드로 복귀 가능하게 함
                    dest = board.goalNode;
                    pushPath = new List<BoardNodeData> { board.startNode };
                }
                else if (piece.nodeHistory.Count > 0)
                {
                    dest = piece.nodeHistory.Peek().data;
                    pushPath = null; // null = 팝 신호
                }
                else
                    continue;

                if (!destinations.ContainsKey(dest))
                    destinations[dest] = (yr, pushPath);
                continue;
            }

            var pathMap = new Dictionary<BoardNodeData, List<BoardNodeData>>();
            bool canOut = false;

            if (piece.currentNode == null)
            {
                TraverseWithPath(board.startNode, (int)yr - 1, false, null,
                    new List<BoardNodeData>(), pathMap, ref canOut);
            }
            else
            {
                var initialPath = new List<BoardNodeData> { piece.currentNode.data };
                TraverseWithPath(piece.currentNode.data, (int)yr, true, null,
                    initialPath, pathMap, ref canOut);
            }

            foreach (var kv in pathMap)
                if (!destinations.ContainsKey(kv.Key))
                    destinations[kv.Key] = (yr, kv.Value);

            if (canOut && !outResults.Contains(yr))
                outResults.Add(yr);
        }

        return new MoveResult { Destinations = destinations, OutResults = outResults };
    }

    // pathSoFar: 현재까지 지나온 노드들(목적지 도착 시 히스토리에 푸시할 경로)
    // isStart=true일 때는 현재 위치 노드가 이미 pathSoFar에 포함되어 있음
    private static void TraverseWithPath(
        BoardNodeData node, int stepsLeft, bool isStart, BoardNodeData cameFrom,
        List<BoardNodeData> pathSoFar,
        Dictionary<BoardNodeData, List<BoardNodeData>> results, ref bool canOut)
    {
        if (stepsLeft == 0)
        {
            results[node] = pathSoFar;
            return;
        }
        if (node.isEnd)
        {
            canOut = true;
            return;
        }

        // 중간 노드는 경로에 추가 (시작 노드는 initialPath에 이미 있으므로 제외)
        var newPath = isStart ? pathSoFar : new List<BoardNodeData>(pathSoFar) { node };

        if (node.isCenter && !isStart)
        {
            var next = cameFrom == node.defaultNextEntry ? node.defaultNext : node.shortcutNext;
            if (next != null)
                TraverseWithPath(next, stepsLeft - 1, false, node, newPath, results, ref canOut);
        }
        else
        {
            if (node.defaultNext != null)
                TraverseWithPath(node.defaultNext, stepsLeft - 1, false, node,
                    new List<BoardNodeData>(newPath), results, ref canOut);

            if (isStart && (node.isJunction || node.isCenter) && node.shortcutNext != null)
                TraverseWithPath(node.shortcutNext, stepsLeft - 1, false, node,
                    new List<BoardNodeData>(newPath), results, ref canOut);
        }
    }
}
