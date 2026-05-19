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

    // 말 오브젝트 + 초기 위치 + 완주 위치
    [SerializeField] private PieceObject[] p1PieceObjects = new PieceObject[4];
    [SerializeField] private PieceObject[] p2PieceObjects = new PieceObject[4];

    [SerializeField] private Transform[] p1StartPositions = new Transform[4];
    [SerializeField] private Transform[] p2StartPositions = new Transform[4];

    [SerializeField] private Transform[] p1EndPositions = new Transform[4];
    [SerializeField] private Transform[] p2EndPositions = new Transform[4];

    private const float StackYOffset = 0.1f;

    // 검은 윷 + 턴 종료 UI
    [SerializeField] private Button blackYutButton;
    [SerializeField] private Button endTurnButton;
    private bool endTurnRequested;

    // 쌓기(업기) UI
    [SerializeField] private GameObject stackDecisionPanel;
    [SerializeField] private Button stackYesButton;
    [SerializeField] private Button stackNoButton;
    [SerializeField] private Button declineStackButton;
    private bool? stackDecision;

    // 완주 - 사용할 윷 결과 선택
    [SerializeField] private GameObject outResultPanel;
    [SerializeField] private Button[] outResultButtons = new Button[5];
    private static readonly YutResult[] OutResultOrder = { YutResult.Do, YutResult.Gae, YutResult.Geol, YutResult.Yut, YutResult.Mo };
    private Dictionary<YutResult, Button> outResultToButton;
    private YutResult? outResultDecision;

    // 마우스 선택
    private DragAndDrop dragAndDrop;

    private void Awake()
    {
        // 1. 플레이어 초기화
        for (int i = 0; i < players.Length; i++)
            players[i] = new Player() { playerId = i, name = $"플레이어{i+1}" };

        // 2. 말 초기화
        for (int i = 0; i < 4; i++)
        {
            p1PieceObjects[i].Bind(players[0].pieces[i]);
            players[0].pieces[i].pieceObject = p1PieceObjects[i];
            p2PieceObjects[i].Bind(players[1].pieces[i]);
            players[1].pieces[i].pieceObject = p2PieceObjects[i];
        }

        // 3. UI 비활성화, 연결, 리스너 추가
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

        declineStackButton.gameObject.SetActive(false);
        declineStackButton.onClick.AddListener(() => dragAndDrop.DeclineStackTargetPick());

        outResultPanel.SetActive(false);
        outResultToButton = new Dictionary<YutResult, Button>();
        for (int i = 0; i < outResultButtons.Length; i++)
        {
            var yr = OutResultOrder[i];
            outResultToButton[yr] = outResultButtons[i];
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
            p1PieceObjects[i].transform.position = p1StartPositions[i].position;
            p1PieceObjects[i].initPosition = p1StartPositions[i].position;
            p2PieceObjects[i].transform.position = p2StartPositions[i].position;
            p2PieceObjects[i].initPosition = p2StartPositions[i].position;
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
        bool wonThisTurn = false;
        while (true)
        {
            // 결과 다 쓰면 검은 윷 추가 사용 여부 확인
            while (player.yutResults.Count > 0)
            {
                // 뒷도로 옮길 수 있는 말이 없으면 건너뛰기 ('완주한 말' 또는 '보드 위에 안 올라간 말'밖에 없는 경우)
                player.yutResults.RemoveAll(yr => yr == YutResult.BACKDO && !player.pieces.Any(p => !p.hasFinished && p.currentNode != null));
                if (player.yutResults.Count == 0) break;

                // 선택 시작
                dragAndDrop.BeginSelection(player);
                yield return new WaitUntil(() => dragAndDrop.MoveConfirmed);

                // 결정 사항 받아오기
                var piece = dragAndDrop.SelectedPiece;
                var targetNode = dragAndDrop.SelectedBoardNode;
                var stackAll = piece.stackedPieces.ToList();

                // 완주 처리
                if (dragAndDrop.IsOutConfirmed)
                {
                    var outResults = dragAndDrop.ValidOutResults;
                    YutResult chosenOutResult;

                    if (outResults.Count == 1)
                        chosenOutResult = outResults[0];
                    
                    else // 나갈 수 있는 윷 결과가 2개 이상
                    {
                        outResultDecision = null;
                        ShowOutResultPanel(outResults);

                        yield return new WaitUntil(() => outResultDecision.HasValue);

                        HideOutResultPanel();
                        chosenOutResult = outResultDecision.Value;
                    }

                    var nodeBeforeOut = piece.currentNode;

                    piece.currentNode?.piecesOnNode.Remove(piece);
                    foreach (var s in stackAll) 
                        s.currentNode?.piecesOnNode.Remove(s);

                    if (nodeBeforeOut != null) 
                        RepositionNode(nodeBeforeOut);

                    HandleFinish(piece, stackAll, player);
                    player.yutResults.Remove(chosenOutResult);
                    LogYutResults(player);

                    if (player.AllFinished)
                    {
                        wonThisTurn = true;
                        player.yutResults.Clear();
                        break;
                    }
                    continue;
                }

                // 데이터 업데이트 (말 + 업힌 말들 함께 이동)
                piece.currentNode?.piecesOnNode.Remove(piece);
                foreach (var s in stackAll) 
                    s.currentNode?.piecesOnNode.Remove(s);

                var prev = dragAndDrop.PrevOfSelectedPiece;

                piece.previousNode = prev;
                piece.currentNode = targetNode;
                targetNode.piecesOnNode.Add(piece);

                foreach (var s in stackAll)
                {
                    s.previousNode = prev;
                    s.currentNode = targetNode;
                    targetNode.piecesOnNode.Add(s);
                }

                // 시각적 이동
                if (piece.previousNode != null) 
                    RepositionNode(piece.previousNode);

                RepositionNode(targetNode);

                // 잡기 처리
                var capturedPieces = targetNode.piecesOnNode
                    .Where(p => p.owner != player)
                    .ToList();

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
                        caught.stackLeader = null;
                        caught.stackedPieces.Clear();
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

                if (friendlyLeaders.Count == 1)
                {
                    stackDecision = null;
                    stackDecisionPanel.SetActive(true);

                    yield return new WaitUntil(() => stackDecision.HasValue);

                    stackDecisionPanel.SetActive(false);

                    if (stackDecision == true)
                    {
                        var ally = friendlyLeaders[0];
                        foreach (var s in ally.stackedPieces)
                        {
                            piece.stackedPieces.Add(s);
                            s.stackLeader = piece;
                        }
                        ally.stackedPieces.Clear(); // 리더 교체
                        piece.stackedPieces.Add(ally);
                        ally.stackLeader = piece;

                        RepositionNode(targetNode);
                        Debug.Log($"<color=yellow>{player.name}이(가) 말을 업었습니다.</color>");
                    }
                }
                else if (friendlyLeaders.Count >= 2)
                {
                    dragAndDrop.BeginStackTargetPick(friendlyLeaders);
                    declineStackButton.gameObject.SetActive(true);

                    yield return new WaitUntil(() => dragAndDrop.PickConfirmed || dragAndDrop.PickDeclined);

                    declineStackButton.gameObject.SetActive(false);

                    if (dragAndDrop.PickConfirmed)
                    {
                        var ally = dragAndDrop.PickedStackTarget;
                        foreach (var s in ally.stackedPieces)
                        {
                            piece.stackedPieces.Add(s);
                            s.stackLeader = piece;
                        }
                        ally.stackedPieces.Clear();
                        piece.stackedPieces.Add(ally);
                        ally.stackLeader = piece;
                        RepositionNode(targetNode);
                        Debug.Log($"<color=yellow>{player.name}이(가) 말을 업었습니다.</color>");
                    }
                }

                // 사용한 결과 제거
                player.yutResults.Remove(dragAndDrop.UsedYutResult);
                LogYutResults(player);
            }

            // 결과를 다 썼을 때 남은 검은 윷이 있으면 던지거나 턴 종료 선택
            if (wonThisTurn) break;
            if (!player.HasBlackYut) break;

            endTurnRequested = false;
            endTurnButton.gameObject.SetActive(true);

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
        foreach (var kv in outResultToButton)
            kv.Value.gameObject.SetActive(options.Contains(kv.Key));

        outResultPanel.SetActive(true);
    }

    private void HideOutResultPanel()
    {
        outResultPanel.SetActive(false);

        foreach (var kv in outResultToButton)
            kv.Value.gameObject.SetActive(false);
    }

    private void HandleFinish(Piece piece, List<Piece> stackedAll, Player player)
    {
        var endPositions = player.playerId == 0 ? p1EndPositions : p2EndPositions;
        var finishingPieces = new List<Piece> { piece };
        finishingPieces.AddRange(stackedAll);

        foreach (var fp in finishingPieces)
        {
            fp.pieceObject.transform.position = endPositions[player.FinishedCount].position;
            player.FinishPiece(fp);
            fp.currentNode = null;
            fp.previousNode = null;
        }

        piece.stackedPieces.Clear();
        foreach (var s in stackedAll) 
        {
            s.stackLeader = null; 
            s.stackedPieces.Clear(); 
        }

        Debug.Log($"<color=green>{player.name}의 말이 완주! ({player.FinishedCount}/4)</color>");
    }

    // 노드 위 말들 재배치
    private void RepositionNode(BoardNode node)
    {
        var units = node.piecesOnNode.Where(p => p.stackLeader == null).ToList();

        for (int i = 0; i < units.Count; i++)
        {
            var pos = node.GetPiecePosition(i, units.Count);
            units[i].pieceObject.transform.position = pos;

            var renderer = units[i].pieceObject.GetComponentInChildren<Renderer>();
            float stackHeight = renderer != null ? renderer.bounds.size.y : StackYOffset;

            for (int j = 0; j < units[i].stackedPieces.Count; j++)
                units[i].stackedPieces[j].pieceObject.transform.position = pos + Vector3.up * stackHeight * (j + 1);
        }
    }
}
