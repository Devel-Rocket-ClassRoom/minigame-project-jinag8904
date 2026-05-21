using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameMaster : MonoBehaviour
{
    // 모드 선택
    [SerializeField] GameObject modeSelectPanel;
    [SerializeField] Button localModeButton;
    [SerializeField] Button vsAIModeButton;
    private bool isVsAI;
    private bool modeSelected;

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

    // 액티브 스킬
    [SerializeField] private Button p1ActiveSkillButton;
    [SerializeField] private Button p2ActiveSkillButton;
    private bool isActiveSkillOn;

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

    // 캐릭터 HUD
    [SerializeField] private Image p1CharacterIcon;
    [SerializeField] private TextMeshProUGUI p1CharacterNameText;
    [SerializeField] private Image p2CharacterIcon;
    [SerializeField] private TextMeshProUGUI p2CharacterNameText;

    // 캐릭터 선택
    [SerializeField] private CharacterData[] availableCharacters;
    [SerializeField] private GameObject characterSelectPanel;
    [SerializeField] private TextMeshProUGUI characterSelectTitleText;
    [SerializeField] private Button[] characterSelectButtons;
    [SerializeField] private Image[] characterSelectButtonIcons;
    [SerializeField] private Button randomCharacterButton;
    private CharacterData characterDecision;

    // AI
    [SerializeField] private BoardData boardData;
    private AIController aiController;
    private Dictionary<BoardNodeData, BoardNode> boardNodeMap;

    // 마우스 선택
    private DragAndDrop dragAndDrop;

    private void Awake()
    {
        // 1. 플레이어 초기화
        for (int i = 0; i < players.Length; i++)
            players[i] = new Player() { playerId = i, name = $"P{i+1}" };

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
            GameLogUI.Log("검은 윷이 던져졌습니다!");
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

        p1ActiveSkillButton.interactable = false;
        p2ActiveSkillButton.interactable = false;
        foreach (var btn in new[] { p1ActiveSkillButton, p2ActiveSkillButton })
        {
            btn.onClick.AddListener(() => StartCoroutine(CoHandleActiveSkill(currPlayer)));
        }

        modeSelectPanel.SetActive(false);
        localModeButton.onClick.AddListener(() => { isVsAI = false; modeSelected = true; });
        vsAIModeButton.onClick.AddListener(() => { isVsAI = true; players[1].isAI = true; modeSelected = true; });

        characterSelectPanel.SetActive(false);
        for (int i = 0; i < characterSelectButtons.Length; i++)
        {
            var idx = i;
            characterSelectButtons[i].onClick.AddListener(() => characterDecision = availableCharacters[idx]);
            characterSelectButtons[i].gameObject.SetActive(false);
        }
        randomCharacterButton.onClick.AddListener(() => characterDecision = availableCharacters[Random.Range(0, availableCharacters.Length)]);

        dragAndDrop = GetComponent<DragAndDrop>();

        aiController = GetComponent<AIController>();
        boardNodeMap = FindObjectsByType<BoardNode>(FindObjectsSortMode.None).ToDictionary(n => n.data);
        dragAndDrop.boardNodeMap = boardNodeMap;
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
        StartCoroutine(CoRunGame());
    }

    private IEnumerator CoSelectMode()
    {
        isVsAI = false;
        players[1].isAI = false;
        modeSelected = false;
        modeSelectPanel.SetActive(true);

        yield return new WaitUntil(() => modeSelected);

        modeSelectPanel.SetActive(false);
    }

    private IEnumerator CoSelectCharacter()
    {
        yield return StartCoroutine(CoSelectCharacterForPlayer(players[0]));
        yield return StartCoroutine(CoSelectCharacterForPlayer(players[1]));
        UpdateCharacterHUD();
    }

    private void UpdateCharacterHUD()
    {
        p1CharacterIcon.sprite = players[0].characterData?.icon;
        p1CharacterNameText.text = players[0].characterData?.name;
        p2CharacterIcon.sprite = players[1].characterData?.icon;
        p2CharacterNameText.text = players[1].characterData?.name;
    }

    private IEnumerator CoSelectCharacterForPlayer(Player player)
    {
        characterSelectTitleText.text = player.name;
        characterDecision = null;

        for (int i = 0; i < characterSelectButtons.Length; i++)
        {
            bool active = i < availableCharacters.Length;
            characterSelectButtons[i].gameObject.SetActive(active);
            if (active)
            {
                characterSelectButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = availableCharacters[i].name;
                //characterSelectButtonIcons[i].sprite = availableCharacters[i].icon;
            }
        }

        characterSelectPanel.SetActive(true);
        yield return new WaitUntil(() => characterDecision != null);
        characterSelectPanel.SetActive(false);

        player.characterData = characterDecision;
    }

    private IEnumerator CoRunGame()
    {
        GameLogUI.Log("모드 선택");
        yield return StartCoroutine(CoSelectMode());
        yield return StartCoroutine(CoSelectCharacter());
        Init();
        GameLogUI.Log("게임 시작");
        yield return StartCoroutine(CoWhoGoesFirst());
        yield return StartCoroutine(CoPlayGame());
        yield return StartCoroutine(CoEndGame());
    }

    private IEnumerator CoWhoGoesFirst()
    {
        yield return new WaitForSeconds(1);

        GameLogUI.Log("<color=green>순서 정하는 중...</color>");
        currPlayer = players[Random.Range(0, 2)];

        yield return new WaitForSeconds(3);

        GameLogUI.Log($"<color=yellow>{currPlayer.name}부터 시작</color>");
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
        if (player.isAI)
        {
            yield return StartCoroutine(aiController.DecideTurn());
            yield break;
        }

        GameLogUI.Log($"{player.name}의 차례");
        yield return new WaitForSeconds(1);

        GameLogUI.Log("<color=green>윷 던지는 중...</color>");
        player.Throw();
        yield return new WaitForSeconds(3);

        GameLogUI.Log("<color=green>윷 결과가 나왔습니다!</color>");
        LogYutResults(player);

        // 말 옮기기 단계 (검은 윷 추가 사용 포함)
        bool wonThisTurn = false;
        while (true)
        {
            // 결과 다 쓰면 검은 윷 추가 사용 여부 확인
            while (player.yutResults.Count > 0)
            {
                // 실제로 뒷도를 쓸 수 있는 말이 없으면 제거
                player.yutResults.RemoveAll(yr => yr == YutResult.BACKDO && !player.pieces.Any(p =>
                    !p.hasFinished && p.currentNode != null &&
                    (p.nodeHistory.Count > 0 || p.currentNode.data == boardData.startNode)));
                if (player.yutResults.Count == 0) break;

                // 액티브 스킬 버튼 활성화
                GetActiveSkillButton(player).interactable = player.Skill?.CanUseActive(player) == true;

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

                    bool wouldFinishGame = player.FinishedCount + 1 + stackAll.Count >= 4;

                    if (outResults.Count == 1 || wouldFinishGame)
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
                    isActiveSkillOn = false;
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
                var prevNode = piece.currentNode;
                piece.currentNode?.piecesOnNode.Remove(piece);
                foreach (var s in stackAll)
                    s.currentNode?.piecesOnNode.Remove(s);

                var pushPath = dragAndDrop.PushPathOfSelectedMove;
                ApplyNodeHistory(piece, stackAll, pushPath);

                piece.currentNode = targetNode;
                targetNode.piecesOnNode.Add(piece);

                foreach (var s in stackAll)
                {
                    s.currentNode = targetNode;
                    targetNode.piecesOnNode.Add(s);
                }

                // 시각적 이동
                if (prevNode != null)
                    RepositionNode(prevNode);

                RepositionNode(targetNode);

                // 액티브 스킬 - 이동 효과
                if (isActiveSkillOn && pushPath != null)
                {
                    player.Skill.OnActiveMoveEffect(player, piece, pushPath, targetNode, RepositionNode);
                    isActiveSkillOn = false;
                }

                // 잡기 처리
                var enemyLeaders = targetNode.piecesOnNode
                    .Where(p => p.owner != player && p.stackLeader == null)
                    .ToList();

                bool wasReversed = false;
                if (enemyLeaders.Count > 0)
                {
                    // 씨름 판정: 칸 위 적 전체 수 기준으로 1번만
                    int totalEnemyCount = enemyLeaders.Sum(e => 1 + e.stackedPieces.Count);
                    var skilledEnemy = enemyLeaders.FirstOrDefault(e => e.owner.Skill != null);
                    var outcome = skilledEnemy?.owner.Skill.OnCaptureAttempt(skilledEnemy, piece, totalEnemyCount)
                                  ?? CaptureOutcome.Captured;

                    if (outcome == CaptureOutcome.Reversed)
                    {
                        wasReversed = true;
                        var reversedPieces = new[] { piece }.Concat(stackAll).ToList();

                        player.OnCaught(piece);

                        foreach (var r in reversedPieces)
                        {
                            targetNode.piecesOnNode.Remove(r);
                            r.currentNode = null;
                            r.nodeHistory.Clear();
                            r.stackLeader = null;
                            r.stackedPieces.Clear();
                            r.pieceObject.transform.position = r.pieceObject.initPosition;
                        }

                        GameLogUI.Log($"<color=purple>[씨름] {skilledEnemy.owner.name}의 도깨비가 씨름에서 이겼습니다!</color>");
                    }
                    else
                    {
                        foreach (var enemyLeader in enemyLeaders)
                        {
                            var capturedPieces = new[] { enemyLeader }.Concat(enemyLeader.stackedPieces).ToList();

                            enemyLeader.owner.OnCaught(enemyLeader);
                            enemyLeader.owner.AddWonhan(enemyLeader.stackedPieces.Count);
                            bool noBonus = enemyLeader.owner.Skill?.OnBeingCaptured(enemyLeader, piece) ?? false;

                            foreach (var caught in capturedPieces)
                            {
                                targetNode.piecesOnNode.Remove(caught);
                                caught.currentNode = null;
                                caught.nodeHistory.Clear();
                                caught.stackLeader = null;
                                caught.stackedPieces.Clear();
                                caught.pieceObject.transform.position = caught.pieceObject.initPosition;
                            }

                            GameLogUI.Log($"<color=red>{player.name}이(가) 상대 말 {capturedPieces.Count}개를 잡았습니다!</color>");
                            if (!noBonus) player.Throw(isCaptureBonus: true);
                            if (!noBonus) GameLogUI.Log("<color=green>잡기 보너스 윷이 던져졌습니다!</color>");
                            LogYutResults(player);
                            player.Skill?.OnCapture(piece, capturedPieces);
                        }
                    }

                    RepositionNode(targetNode);
                }

                // 사용한 결과 제거 (씨름 패배 시에도 소모)
                player.yutResults.Remove(dragAndDrop.UsedYutResult);
                LogYutResults(player);

                if (wasReversed) continue;

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
                        GameLogUI.Log($"<color=yellow>{player.name}이(가) 말을 업었습니다.</color>");
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
                        GameLogUI.Log($"<color=yellow>{player.name}이(가) 말을 업었습니다.</color>");
                    }
                }

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
        p1ActiveSkillButton.interactable = false;
        p2ActiveSkillButton.interactable = false;
        isActiveSkillOn = false;
    }

    private IEnumerator CoEndGame()
    {
        GameLogUI.Log("게임 끝");
        yield return null;
    }

    private void LogYutResults(Player player)
    {
        GameLogUI.UpdateYutResults(player.yutResults, player.name);
        blackYutButton.gameObject.SetActive(player.HasBlackYut);
    }

    private IEnumerator CoHandleActiveSkill(Player player)
    {
        var skill = player.Skill;
        GetActiveSkillButton(player).interactable = false;
        GameLogUI.Log($"<color=purple>[{skill.ActiveSkillName}] 활성화!</color>");

        if (skill.HasImmediateEffect)
            yield return StartCoroutine(skill.CoOnActiveActivated(player, RequestPiecePickCoroutine, RepositionNode));
        else
        {
            isActiveSkillOn = true;
            skill.OnActiveActivated(player);
        }
    }

    private IEnumerator RequestPiecePickCoroutine(List<Piece> candidates, Action<Piece> onPicked)
    {
        dragAndDrop.BeginSacrificePick(candidates);
        yield return new WaitUntil(() => dragAndDrop.SacrificeConfirmed);
        onPicked(dragAndDrop.SacrificeTarget);
    }

    public Player GetOpponent(Player player) => players[1 - player.playerId];

    private Button GetActiveSkillButton(Player player) => player.playerId == 0 ? p1ActiveSkillButton : p2ActiveSkillButton;

    private void SwitchTurn() => currPlayer = players[1 - currPlayer.playerId];
    
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
            fp.nodeHistory.Clear();
        }

        piece.stackedPieces.Clear();
        foreach (var s in stackedAll) 
        {
            s.stackLeader = null; 
            s.stackedPieces.Clear(); 
        }

        GameLogUI.Log($"<color=green>{player.name} 완주! ({player.FinishedCount}/4)</color>");
        player.Skill?.OnFinish(piece);
    }

    public BoardNode GetNode(BoardNodeData data)
    {
        if (data == null) return null;
        boardNodeMap.TryGetValue(data, out var node);
        return node;
    }

    // pushPath == null: 뒷도(팝), non-null: 전진(목록 순서대로 푸시)
    private void ApplyNodeHistory(Piece leader, List<Piece> stacked, List<BoardNode> pushPath)
    {
        if (pushPath == null)
        {
            if (leader.nodeHistory.Count > 0) leader.nodeHistory.Pop();
            foreach (var s in stacked)
                if (s.nodeHistory.Count > 0) s.nodeHistory.Pop();
        }
        else
        {
            foreach (var n in pushPath)
            {
                leader.nodeHistory.Push(n);
                foreach (var s in stacked)
                    s.nodeHistory.Push(n);
            }
        }
    }

    // 노드 위 말들 재배치
    public void RepositionNode(BoardNode node)
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

    // 플레이어 이동 처리와 동일, 자동화되어 있음.
    public IEnumerator ApplyAIMove(Piece piece, BoardNodeData targetData, List<BoardNodeData> pushPath, YutResult used, bool isOut, bool useActiveSkill = false)
    {
        var player = currPlayer;
        var stackAll = piece.stackedPieces.ToList();

        // 완주 처리
        if (isOut)
        {
            piece.currentNode?.piecesOnNode.Remove(piece);
            foreach (var s in stackAll) s.currentNode?.piecesOnNode.Remove(s);
            var nodeBeforeOut = piece.currentNode;
            if (nodeBeforeOut != null) RepositionNode(nodeBeforeOut);
            HandleFinish(piece, stackAll, player);
            player.yutResults.Remove(used);
            yield break;
        }

        // 이동
        var targetNode = GetNode(targetData);

        var prevNode = piece.currentNode;
        piece.currentNode?.piecesOnNode.Remove(piece);
        foreach (var s in stackAll) s.currentNode?.piecesOnNode.Remove(s);

        var pushPathNodes = pushPath != null
            ? pushPath.ConvertAll(d => GetNode(d))
            : null;
        ApplyNodeHistory(piece, stackAll, pushPathNodes);

        piece.currentNode = targetNode;
        targetNode.piecesOnNode.Add(piece);

        foreach (var s in stackAll)
        {
            s.currentNode = targetNode;
            targetNode.piecesOnNode.Add(s);
        }

        if (prevNode != null) RepositionNode(prevNode);
        RepositionNode(targetNode);

        // 액티브 스킬 - 이동 효과 (AI)
        if (useActiveSkill && pushPathNodes != null)
        {
            player.Skill.OnActiveActivated(player);
            player.Skill.OnActiveMoveEffect(player, piece, pushPathNodes, targetNode, RepositionNode);
        }

        // 잡기 처리
        var enemyLeaders = targetNode.piecesOnNode
            .Where(p => p.owner != player && p.stackLeader == null)
            .ToList();

        if (enemyLeaders.Count > 0)
        {
            int totalEnemyCount = enemyLeaders.Sum(e => 1 + e.stackedPieces.Count);
            var skilledEnemy = enemyLeaders.FirstOrDefault(e => e.owner.Skill != null);
            var outcome = skilledEnemy?.owner.Skill.OnCaptureAttempt(skilledEnemy, piece, totalEnemyCount)
                          ?? CaptureOutcome.Captured;

            if (outcome == CaptureOutcome.Reversed)
            {
                var reversedPieces = new[] { piece }.Concat(stackAll).ToList();

                player.OnCaught(piece);

                foreach (var r in reversedPieces)
                {
                    targetNode.piecesOnNode.Remove(r);
                    r.currentNode = null;
                    r.nodeHistory.Clear();
                    r.stackLeader = null;
                    r.stackedPieces.Clear();
                    r.pieceObject.transform.position = r.pieceObject.initPosition;
                }

                Debug.Log($"<color=purple>[씨름] {skilledEnemy.owner.name}의 도깨비가 씨름에서 이겼습니다!</color>");
            }
            else
            {
                foreach (var enemyLeader in enemyLeaders)
                {
                    var capturedPieces = new[] { enemyLeader }.Concat(enemyLeader.stackedPieces).ToList();

                    enemyLeader.owner.OnCaught(enemyLeader);
                    enemyLeader.owner.AddWonhan(enemyLeader.stackedPieces.Count);
                    bool noBonus = enemyLeader.owner.Skill?.OnBeingCaptured(enemyLeader, piece) ?? false;

                    foreach (var caught in capturedPieces)
                    {
                        targetNode.piecesOnNode.Remove(caught);
                        caught.currentNode = null;
                        caught.nodeHistory.Clear();
                        caught.stackLeader = null;
                        caught.stackedPieces.Clear();
                        caught.pieceObject.transform.position = caught.pieceObject.initPosition;
                    }

                    Debug.Log($"<color=red>{player.name}이(가) 상대 말 {capturedPieces.Count}개를 잡았습니다!</color>");
                    if (!noBonus) player.Throw(isCaptureBonus: true);
                    player.Skill?.OnCapture(piece, capturedPieces);
                }
            }

            RepositionNode(targetNode);
        }

        // 업기 처리 (자동)
        var friendlyLeaders = targetNode.piecesOnNode
            .Where(p => p.owner == player && p != piece && p.stackLeader == null)
            .ToList();

        if (friendlyLeaders.Count > 0)
        {
            var ally = friendlyLeaders[0];
            foreach (var s in ally.stackedPieces)
            {
                piece.stackedPieces.Add(s);
                s.stackLeader = piece;
            }
            ally.stackedPieces.Clear();
            piece.stackedPieces.Add(ally);
            ally.stackLeader = piece;
            RepositionNode(targetNode);
            GameLogUI.Log($"<color=#00CFCF>[AI] 말을 업었습니다.</color>");
        }

        player.yutResults.Remove(used);
    }
}
