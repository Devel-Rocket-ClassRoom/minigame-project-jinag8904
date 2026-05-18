using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class GameMaster : MonoBehaviour
{
    // 플레이어
    private Player[] players = new Player[2];
    private Player currPlayer;
    public Player CurrPlayer => currPlayer;

    // 말 오브젝트 & 초기 위치 (인스펙터에서 플레이어 순서대로 4개씩 할당)
    [SerializeField] private PieceObject[] p1PieceObjects = new PieceObject[4];
    [SerializeField] private PieceObject[] p2PieceObjects = new PieceObject[4];
    [SerializeField] private Transform[] p1InitPositions = new Transform[4];
    [SerializeField] private Transform[] p2InitPositions = new Transform[4];
    [SerializeField] private Transform[] p1EndPositions = new Transform[4];
    [SerializeField] private Transform[] p2EndPositions = new Transform[4];
    private const float StackYOffset = 0.1f;

    // UI
    [SerializeField] private Button blackYutButton;
    [SerializeField] private Button endTurnButton;
    [SerializeField] private GameObject stackDecisionPanel;
    [SerializeField] private Button stackYesButton;
    [SerializeField] private Button stackNoButton;
    private bool? stackDecision;
    private bool endTurnRequested;
    [SerializeField] private GameObject outResultPanel;
    [SerializeField] private Button[] outResultButtons = new Button[5]; // 인스펙터에서 Do/Gae/Geol/Yut/Mo 순서로 할당
    private static readonly YutResult[] OutResultOrder = { YutResult.Do, YutResult.Gae, YutResult.Geol, YutResult.Yut, YutResult.Mo };
    private Dictionary<YutResult, Button> outResultButtonMap;
    private YutResult? outResultDecision;

    // 마우스 선택
    private DragAndDrop dragAndDrop;

    private void Awake()
    {
        for (int i = 0; i < players.Length; i++)
            players[i] = new Player() { playerId = i, name = $"플레이어{i+1}" };

        for (int i = 0; i < 4; i++)
        {
            p1PieceObjects[i].Bind(players[0].pieces[i]);
            players[0].pieces[i].pieceObject = p1PieceObjects[i];
            p2PieceObjects[i].Bind(players[1].pieces[i]);
            players[1].pieces[i].pieceObject = p2PieceObjects[i];
        }

        blackYutButton.gameObject.SetActive(false);
        blackYutButton.onClick.AddListener(() =>
        {
            currPlayer.Throw(isBlackYut: true);
            if (!currPlayer.HasBlackYut) blackYutButton.gameObject.SetActive(false);
            LogYutResults(currPlayer);
        });

        endTurnButton.gameObject.SetActive(false);
        endTurnButton.onClick.AddListener(() => endTurnRequested = true);

        stackDecisionPanel.SetActive(false);
        stackYesButton.onClick.AddListener(() => stackDecision = true);
        stackNoButton.onClick.AddListener(() => stackDecision = false);

        outResultPanel.SetActive(false);
        outResultButtonMap = new Dictionary<YutResult, Button>();
        for (int i = 0; i < outResultButtons.Length; i++)
        {
            var yr = OutResultOrder[i];
            outResultButtonMap[yr] = outResultButtons[i];
            var captured = yr;
            outResultButtons[i].onClick.AddListener(() => outResultDecision = captured);
            outResultButtons[i].gameObject.SetActive(false);
        }

        dragAndDrop = GetComponent<DragAndDrop>();
    }

    private void Init()
    {
        foreach (var player in players)
            player.Init();

        for (int i = 0; i < 4; i++)
        {
            p1PieceObjects[i].transform.position = p1InitPositions[i].position;
            p1PieceObjects[i].initPosition = p1InitPositions[i].position;
            p2PieceObjects[i].transform.position = p2InitPositions[i].position;
            p2PieceObjects[i].initPosition = p2InitPositions[i].position;
        }
    }

    private void Start()
    {
        Debug.Log("게임 시작");
        StartCoroutine(CoRunGame());
    }

    private IEnumerator CoRunGame()
    {
        Init();
        yield return StartCoroutine(CoWhoGoesFirst());
        yield return StartCoroutine(CoPlayGame());
        yield return StartCoroutine(CoEndGame());
    }

    private IEnumerator CoWhoGoesFirst()
    {
        yield return new WaitForSeconds(1);

        Debug.Log("<color=green>순서 정하는 중...</color>");
        currPlayer = players[Random.Range(0, 2)];

        yield return new WaitForSeconds(3);

        Debug.Log($"<color=yellow>{currPlayer.name}부터 시작</color>");
    }

    private IEnumerator CoPlayGame()
    {
        while (!players.Any(p => p.AllFinished))
        {
            yield return new WaitForSeconds(1);
            yield return StartCoroutine(CoHandlePlayerTurn(currPlayer));
            SwitchTurn();
        }
    }

    private IEnumerator CoHandlePlayerTurn(Player player)
    {
        yield return new WaitForSeconds(1);

        Debug.Log($"{player.name}의 차례");
        yield return new WaitForSeconds(1);

        Debug.Log("<color=green>윷 던지는 중...</color>");
        player.Throw();
        yield return new WaitForSeconds(3);

        LogYutResults(player);

        if (player.HasBlackYut) blackYutButton.gameObject.SetActive(true);

        // 말 옮기기 단계 (검은 윷 추가 사용 포함)
        while (true)
        {
            // ↓결과 다 쓰면 검은 윷 추가 사용 여부 확인
            while (player.yutResults.Count > 0)
            {
                // ↓뒷도로 옮길 수 있는 말이 없으면 건너뛰기 (판 위에 있는 말이면 가능 — startNode는 previousNode 없어도 goalNode로 이동 가능)
                player.yutResults.RemoveAll(yr => yr == YutResult.BACKDO && !player.pieces.Any(p => !p.hasFinished && p.currentNode != null && (p.previousNode != null || p.currentNode.data.isStart)));
                if (player.yutResults.Count == 0) break;

                // 선택 시작
                dragAndDrop.BeginSelection(player);
                yield return new WaitUntil(() => dragAndDrop.MoveConfirmed);

                // 결정 사항 받아오기
                var piece = dragAndDrop.SelectedPiece;
                var targetNode = dragAndDrop.selectedBoardNode;
                var stackAll = piece.stackedPieces.ToList();

                // 완주 처리
                if (dragAndDrop.IsOutConfirmed)
                {
                    var outResults = dragAndDrop.ValidOutResults;
                    YutResult chosenOutResult;
                    if (outResults.Count == 1)
                    {
                        chosenOutResult = outResults[0];
                    }
                    else
                    {
                        outResultDecision = null;
                        ShowOutResultPanel(outResults);
                        yield return new WaitUntil(() => outResultDecision.HasValue);
                        HideOutResultPanel();
                        chosenOutResult = outResultDecision.Value;
                    }

                    var nodeBeforeOut = piece.currentNode;
                    piece.currentNode?.piecesOnNode.Remove(piece);
                    foreach (var s in stackAll) s.currentNode?.piecesOnNode.Remove(s);
                    if (nodeBeforeOut != null) RepositionNode(nodeBeforeOut);

                    HandleFinish(piece, stackAll, player);
                    player.yutResults.Remove(chosenOutResult);
                    LogYutResults(player);
                    continue;
                }

                // 데이터 업데이트 (말 + 업힌 말들 함께 이동)
                piece.currentNode?.piecesOnNode.Remove(piece);
                foreach (var s in stackAll) s.currentNode?.piecesOnNode.Remove(s);

                var newPreviousNode = dragAndDrop.PreviousNodeForSelected;
                piece.previousNode = newPreviousNode;
                piece.currentNode = targetNode;
                targetNode.piecesOnNode.Add(piece);

                foreach (var s in stackAll)
                {
                    s.previousNode = newPreviousNode;
                    s.currentNode = targetNode;
                    targetNode.piecesOnNode.Add(s);
                }

                // 시각적 이동
                if (piece.previousNode != null) RepositionNode(piece.previousNode);
                RepositionNode(targetNode);

                // 잡기 처리
                var capturedPieces = targetNode.piecesOnNode.Where(p => p.owner != player).ToList();
                if (capturedPieces.Count > 0)
                {
                    // 리더(또는 독립 말)에만 OnCaught + 업힌 말 수만큼 보너스 원한
                    foreach (var leader in capturedPieces.Where(p => p.stackLeader == null))
                    {
                        leader.owner.OnCaught(leader);
                        leader.owner.AddWonhan(leader.stackedPieces.Count);
                    }

                    foreach (var caught in capturedPieces)
                    {
                        targetNode.piecesOnNode.Remove(caught);
                        caught.currentNode = null;
                        caught.previousNode = null;
                        caught.stackedPieces.Clear();
                        caught.stackLeader = null;
                        caught.pieceObject.transform.position = caught.pieceObject.initPosition;
                    }
                    RepositionNode(targetNode);

                    Debug.Log($"<color=red>{player.name}이(가) 상대 말 {capturedPieces.Count}개를 잡았습니다!</color>");
                    player.Throw(isCaptureBonus: true);
                    LogYutResults(player);
                }

                // 업기 처리
                var friendlyLeaders = targetNode.piecesOnNode
                    .Where(p => p.owner == player && p != piece && p.stackLeader == null)
                    .ToList();

                if (friendlyLeaders.Count > 0)
                {
                    stackDecision = null;
                    stackDecisionPanel.SetActive(true);
                    yield return new WaitUntil(() => stackDecision.HasValue);
                    stackDecisionPanel.SetActive(false);

                    if (stackDecision == true)
                    {
                        foreach (var ally in friendlyLeaders)
                        {
                            foreach (var s in ally.stackedPieces)
                            {
                                piece.stackedPieces.Add(s);
                                s.stackLeader = piece;
                            }
                            ally.stackedPieces.Clear();
                            piece.stackedPieces.Add(ally);
                            ally.stackLeader = piece;
                        }
                        RepositionNode(targetNode);
                        Debug.Log($"<color=yellow>{player.name}이(가) 말을 업었습니다.</color>");
                    }
                }

                // 사용한 결과 제거
                player.yutResults.Remove(dragAndDrop.UsedYutResult);
                LogYutResults(player);
            }

            // 결과를 다 썼을 때 남은 검은 윷이 있으면 던지거나 턴 종료 선택
            if (!player.HasBlackYut) break;

            endTurnRequested = false;
            endTurnButton.gameObject.SetActive(true);
            // blackYutButton은 이미 활성화 상태 — 클릭 시 결과 추가됨
            yield return new WaitUntil(() => endTurnRequested || player.yutResults.Count > 0);
            endTurnButton.gameObject.SetActive(false);

            if (endTurnRequested) break;
            // 검은 윷을 던진 경우 → inner while 재진입
        }

        blackYutButton.gameObject.SetActive(false);
        endTurnButton.gameObject.SetActive(false);
    }

    private IEnumerator CoEndGame()
    {
        Debug.Log("게임 끝");
        yield return null;
    }

    private void LogYutResults(Player player)
    {
        var yrs = player.yutResults;
        if (yrs.Count == 0) { Debug.Log("남은 결과 없음"); return; }

        var sb = new StringBuilder("결과: ");
        for (int i = 0; i < yrs.Count; i++)
        {
            if (i != 0) sb.Append(" / ");
            sb.Append(yrs[i]);
        }
        Debug.Log(sb.ToString());
    }

    private void SwitchTurn()
    {
        currPlayer = players[1 - currPlayer.playerId];
    }

    private void ShowOutResultPanel(List<YutResult> options)
    {
        foreach (var kv in outResultButtonMap)
            kv.Value.gameObject.SetActive(options.Contains(kv.Key));
        outResultPanel.SetActive(true);
    }

    private void HideOutResultPanel()
    {
        outResultPanel.SetActive(false);
        foreach (var kv in outResultButtonMap)
            kv.Value.gameObject.SetActive(false);
    }

    private void HandleFinish(Piece piece, List<Piece> stackedWithIt, Player player)
    {
        var endPositions = player.playerId == 0 ? p1EndPositions : p2EndPositions;
        var allFinishing = new List<Piece> { piece };
        allFinishing.AddRange(stackedWithIt);

        foreach (var fp in allFinishing)
        {
            fp.pieceObject.transform.position = endPositions[player.FinishedCount].position;
            player.FinishPiece(fp);
            fp.currentNode = null;
            fp.previousNode = null;
        }

        piece.stackedPieces.Clear();
        foreach (var s in stackedWithIt) { s.stackLeader = null; s.stackedPieces.Clear(); }

        Debug.Log($"<color=green>{player.name}의 말이 완주! ({player.FinishedCount}/4)</color>");
    }

    // 노드 위 말들 재배치. 업기 그룹은 같은 위치에 y축으로 쌓기.
    private void RepositionNode(BoardNode node)
    {
        var units = node.piecesOnNode.Where(p => p.stackLeader == null).ToList();
        
        for (int i = 0; i < units.Count; i++)
        {
            var pos = node.GetPiecePosition(i, units.Count);
            units[i].pieceObject.transform.position = pos;
            for (int j = 0; j < units[i].stackedPieces.Count; j++)
                units[i].stackedPieces[j].pieceObject.transform.position = pos + Vector3.up * StackYOffset * (j + 1);
        }
    }
}
