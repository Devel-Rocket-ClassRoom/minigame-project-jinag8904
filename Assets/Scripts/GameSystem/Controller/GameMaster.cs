using TMPro;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using DG.Tweening;
using Random = UnityEngine.Random;

public class GameMaster : MonoBehaviour
{
    // 튜토리얼 모드
    [SerializeField] private bool tutorialMode;
    [SerializeField] private CharacterData tutorialCharacter;

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

    private const float StackYOffset = 0.05f;

    // 업힌 말 개수 배지 (런타임에 말마다 1개씩 부착)
    [SerializeField] private StackCountBadge stackBadgePrefab;
    private readonly List<StackCountBadge> _stackBadges = new();

    // 검은 윷 + 턴 종료 UI
    [SerializeField] private Button blackYutButton;
    [SerializeField] private Button endTurnButton;
    private bool endTurnRequested;

    // 윷 던지기 버튼
    [SerializeField] private Button throwYutButton;
    private bool throwRequested;

    // 윷 던지기 컨트롤러
    [SerializeField] private YutThrowController yutThrowController;

    // 윷 결과 가이드 팝업 (H 키) — 플레이 중에만 활성화
    [SerializeField] private YutGuidePopup yutGuidePopup;

    // 액티브 스킬
    [SerializeField] private Button p1ActiveSkillButton;
    [SerializeField] private Button p2ActiveSkillButton;
    private SkillTooltipTrigger p1SkillTrigger;
    private SkillTooltipTrigger p2SkillTrigger;
    private bool isActiveSkillOn;
    [SerializeField] private GameObject p1SkillArmedHighlight;   // 지연형(귀신) 무장 중 버튼 강조
    [SerializeField] private GameObject p2SkillArmedHighlight;

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

    [SerializeField] private WonhanGauge p1WonhanGauge;
    [SerializeField] private WonhanGauge p2WonhanGauge;

    // 캐릭터 테마색 캐시 (미러전 톤업 포함) — 차례 라벨/이름 색에 사용
    private Color _themeColor0 = Color.white;
    private Color _themeColor1 = Color.white;

    [SerializeField] private TMP_Text p1BlackYutCountText;
    [SerializeField] private TMP_Text p2BlackYutCountText;

    [SerializeField] private TMP_Text turnIndicatorText;
    [SerializeField] private TMP_Text p1ActiveCooldownText;
    [SerializeField] private TMP_Text p2ActiveCooldownText;
    [SerializeField] private GameObject p1ActiveCooldownGroup;  // 아이콘 + 숫자 묶음
    [SerializeField] private GameObject p2ActiveCooldownGroup;

    // 캐릭터 선택
    [SerializeField] private CharacterData[] availableCharacters;
    [SerializeField] private GameObject characterSelectPanel;
    [SerializeField] private CanvasGroup characterSelectCanvasGroup;
    [SerializeField] private TextMeshProUGUI characterSelectTitleText;
    [SerializeField] private Button[] characterSelectButtons;
    [SerializeField] private Image[] characterSelectButtonIcons;
    [SerializeField] private Button randomCharacterButton;
    [SerializeField] private AudioClip characterSelectSfx;
    private CharacterData characterDecision;

    // 게임 종료 UI
    [SerializeField] private GameOverUI gameOverUI;

    // 맞은편 캐릭터
    [SerializeField] private OpponentCharacterController[] opponentCharacters;

    // 로컬 2인 모드 시점 전환 카메라
    [SerializeField] private CinemachineCamera p1SideCam;
    [SerializeField] private CinemachineCamera p2SideCam;
    [SerializeField] private float aiTableViewDwell = 0.9f;  // AI 차례 시작 시 테이블뷰에서 머무는 시간
    private CinemachineBrain _brain;

    // 제비뽑기
    [SerializeField] private LotDrawController lotDrawController;

    // AI
    [SerializeField] private BoardData boardData;
    private AIController aiController;
    private Dictionary<BoardNodeData, BoardNode> boardNodeMap;
    private PieceMoveAnimator pieceMoveAnimator;

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

            AttachStackBadge(p1PieceObjects[i], players[0].pieces[i]);
            AttachStackBadge(p2PieceObjects[i], players[1].pieces[i]);
        }

        // 3. UI 비활성화, 연결, 리스너 추가
        blackYutButton.gameObject.SetActive(false);
        blackYutButton.onClick.AddListener(() => StartCoroutine(CoHandleBlackYutThrow()));

        endTurnButton.gameObject.SetActive(false);
        endTurnButton.onClick.AddListener(() => endTurnRequested = true);

        throwYutButton.gameObject.SetActive(false);
        throwYutButton.onClick.AddListener(() => throwRequested = true);

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
        p1SkillTrigger = p1ActiveSkillButton.GetComponent<SkillTooltipTrigger>();
        p2SkillTrigger = p2ActiveSkillButton.GetComponent<SkillTooltipTrigger>();

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

        _brain = Camera.main.GetComponent<CinemachineBrain>();

        dragAndDrop = GetComponent<DragAndDrop>();
        pieceMoveAnimator = GetComponent<PieceMoveAnimator>();

        aiController = GetComponent<AIController>();
        aiController.Init(yutThrowController);
        boardNodeMap = FindObjectsByType<BoardNode>(FindObjectsSortMode.None).ToDictionary(n => n.data);
        dragAndDrop.boardNodeMap = boardNodeMap;

        LocalizationManager.OnLanguageChanged += UpdateCharacterHUD;

        GameEvents.OnBlackYutObtained += HandleBlackYutCountChanged;
        GameEvents.OnBlackYutUsed     += HandleBlackYutCountChanged;
    }

    private void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= UpdateCharacterHUD;

        GameEvents.OnBlackYutObtained -= HandleBlackYutCountChanged;
        GameEvents.OnBlackYutUsed     -= HandleBlackYutCountChanged;
    }

    // 말마다 스택 배지를 자식으로 붙이고 목록에 등록
    private void AttachStackBadge(PieceObject po, Piece piece)
    {
        if (stackBadgePrefab == null || po == null) return;
        var badge = Instantiate(stackBadgePrefab);  // 말의 자식 X → 말 스케일 영향 제거
        badge.Bind(piece);                          // 위치는 배지가 말을 월드 좌표로 따라감
        _stackBadges.Add(badge);
    }

    // 탑뷰(보드캠)일 때만 배지가 보이도록 매 프레임 게이팅 + 개수 갱신
    private void Update()
    {
        bool topView = pieceMoveAnimator != null && pieceMoveAnimator.IsBoardCamActive;
        for (int i = 0; i < _stackBadges.Count; i++)
            _stackBadges[i].Refresh(topView);
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
        ActivateOpponentCharacter();
    }

    private void ActivateOpponentCharacter()
    {
        if (opponentCharacters == null) return;
        foreach (var oc in opponentCharacters)
        {
            bool active = isVsAI && oc.linkedCharacter == players[1].characterData;
            oc.gameObject.SetActive(active);
        }
    }

    private void UpdateCharacterHUD()
    {
        if (players[0].characterData != null)
        {
            p1CharacterIcon.sprite = players[0].characterData.icon;
            p1CharacterNameText.text = LocalizationManager.Get(players[0].characterData.localizationKey);
            p1SkillTrigger?.SetSkill(players[0].Skill, p1CharacterNameText.text);
        }
        if (players[1].characterData != null)
        {
            p2CharacterIcon.sprite = players[1].characterData.icon;
            p2CharacterNameText.text = LocalizationManager.Get(players[1].characterData.localizationKey);
            p2SkillTrigger?.SetSkill(players[1].Skill, p2CharacterNameText.text);
        }

        Color c0 = players[0].characterData != null ? players[0].characterData.themeColor : Color.white;
        Color c1 = players[1].characterData != null ? players[1].characterData.themeColor : Color.white;
        if (players[0].characterData == players[1].characterData)   // 미러전: p2 톤업
            c1 = Color.Lerp(c1, Color.white, 0.4f);

        _themeColor0 = c0;
        _themeColor1 = c1;

        VFXManager.Instance?.SetCaptureColors(c0, c1);
        p1WonhanGauge?.SetColor(c0);
        p2WonhanGauge?.SetColor(c1);

        RefreshActiveCooldowns();
        if (currPlayer != null) UpdateTurnIndicator(currPlayer);
        RefreshTurnNameColors();
    }

    private IEnumerator CoSelectCharacterForPlayer(Player player)
    {
        if (isVsAI)
            characterSelectTitleText.text = LocalizationManager.Get(player.playerId == 0 ? "LABEL_CHARACTER_SELECT" : "LABEL_OPPONENT_SELECT");
        else
            characterSelectTitleText.text = LocalizationManager.Get(player.playerId == 0 ? "LABEL_LOCAL_P1_SELECT" : "LABEL_LOCAL_P2_SELECT");
        characterDecision = null;

        for (int i = 0; i < characterSelectButtons.Length; i++)
        {
            bool active = i < availableCharacters.Length;
            characterSelectButtons[i].gameObject.SetActive(active);
            if (active)
            {
                characterSelectButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = LocalizationManager.Get(availableCharacters[i].localizationKey);
                //characterSelectButtonIcons[i].sprite = availableCharacters[i].icon;
                characterSelectButtons[i].image.color = Color.white;
            }
        }

        characterSelectCanvasGroup.alpha = 0f;
        characterSelectPanel.SetActive(true);
        yield return characterSelectCanvasGroup.DOFade(1f, 0.3f).WaitForCompletion();

        yield return new WaitUntil(() => characterDecision != null);
        var decided = characterDecision; // 0.5s 대기 중 다른 버튼 클릭으로 덮어써지는 것 방지

        SoundManager.Instance?.PlaySFX(characterSelectSfx);

        int selectedIdx = System.Array.IndexOf(availableCharacters, decided);
        for (int i = 0; i < characterSelectButtons.Length; i++)
        {
            if (!characterSelectButtons[i].gameObject.activeSelf) continue;
            if (i == selectedIdx)
            {
                characterSelectButtons[i].image.DOColor(decided.themeColor, 0.1f);
                characterSelectButtons[i].transform.DOScale(1.1f, 0.1f).SetLoops(2, LoopType.Yoyo);
            }
            else
            {
                characterSelectButtons[i].image.DOColor(new Color(1f, 1f, 1f, 0.25f), 0.15f);
            }
        }

        yield return new WaitForSeconds(0.5f);

        yield return characterSelectCanvasGroup.DOFade(0f, 0.3f).WaitForCompletion();
        characterSelectPanel.SetActive(false);

        player.characterData = decided;
    }

    private IEnumerator CoRunGame()
    {
        TutorialManager.isTutorial = tutorialMode;

        // #91 2배속: 선택/도입 연출은 항상 1배속. 배속은 본 게임 루프(CoPlayGame) 진입 시 적용.
        Time.timeScale = 1f;

        if (tutorialMode)
        {
            isVsAI = true;
            players[1].isAI = true;

            players[0].characterData = tutorialCharacter;
            players[1].characterData = tutorialCharacter;
            UpdateCharacterHUD();
            ActivateOpponentCharacter();

            Init();
            yield return StartCoroutine(CoWhoGoesFirst());
            yield return new WaitUntil(() => TutorialManager.readyToPlay);
        }
        else
        {
            yield return StartCoroutine(CoSelectMode());

            bool isKorean = LocalizationManager.CurrentLanguage == Language.Korean;
            if (isVsAI)
            {
                players[0].name = isKorean ? "당신" : "You";
                players[1].name = isKorean ? "상대" : "Opponent";
            }
            else
            {
                players[0].name = isKorean ? "플레이어 1" : "Player 1";
                players[1].name = isKorean ? "플레이어 2" : "Player 2";
            }

            yield return StartCoroutine(CoSelectCharacter());
            Init();
            yield return StartCoroutine(CoWhoGoesFirst());
        }
        yield return StartCoroutine(CoPlayGame());
        yield return StartCoroutine(CoEndGame());
    }

    private IEnumerator CoWhoGoesFirst()
    {
        yield return StartCoroutine(lotDrawController.CoDraw(TutorialManager.isTutorial));
        currPlayer = lotDrawController.LastPickedMarked ? players[0] : players[1];
    }

    // 튜토리얼 모드에서는 (3)단계 윷 가이드 후 TutorialManager가 호출한다.
    public void ArmGuidePopup()
    {
        if (yutGuidePopup != null) yutGuidePopup.Arm();
    }

    private IEnumerator CoPlayGame()
    {
        if (!tutorialMode) ArmGuidePopup();

        // #91 2배속은 timeScale이 아니라 말 이동/턴 대기 속도로 구현(UI·연출 보호). 본게임도 timeScale=1 유지.
        Time.timeScale = 1f;

        while (!players.Any(p => p.AllFinished))
        {
            yield return new WaitForSeconds(0.4f / Speed);
            yield return StartCoroutine(CoHandlePlayerTurn(currPlayer));
            SwitchTurn();
        }

        // 게임 끝
    }

    private IEnumerator CoSwitchSideCam(int playerId)
    {
        if (p1SideCam == null || p2SideCam == null) yield break;

        yutThrowController.SetActivePlayer(playerId);
        pieceMoveAnimator.SetActivePlayer(playerId);

        var incoming = playerId == 0 ? p1SideCam : p2SideCam;
        var outgoing  = playerId == 0 ? p2SideCam : p1SideCam;

        incoming.Priority = new PrioritySettings { Value = 15 };

        yield return null;
        if (_brain != null)
            yield return new WaitUntil(() => !_brain.IsBlending);

        outgoing.Priority = new PrioritySettings { Value = 0 };
    }

    // 던지기 캠(20) → 보드캠(15) 직접 전환.
    // 던지기 캠을 먼저 내려 같은 프레임에 보드캠이 최상위가 되게 함 → 테이블뷰를 거치지 않음.
    private IEnumerator CoEnterBoardCam(int priority = 15)
    {
        if (pieceMoveAnimator == null) yield break;
        yutThrowController?.ReleaseThrowCam();
        yield return StartCoroutine(pieceMoveAnimator.CoActivateBoardCam(priority));
    }

    // AI 던지기 전 카메라 정리: 던지기 캠 내리고 진행 중 블렌드 완료 대기 후 테이블뷰에서 잠시 머무름
    public IEnumerator CoAITableViewDwell()
    {
        yutThrowController?.ReleaseThrowCam();                       // 던지기 캠 잔여분 정리 → 테이블뷰 보장
        if (_brain != null) yield return new WaitUntil(() => !_brain.IsBlending);  // 진행 중 블렌드 마무리 대기
        yield return new WaitForSeconds(aiTableViewDwell / Speed);  // 테이블뷰에서 잠시 머무름
    }

    // AI 이동 볼리가 끝났을 때 보드캠 내려 테이블뷰로 복귀 (AIController가 호출)
    public IEnumerator CoReleaseAICamera()
    {
        if (pieceMoveAnimator != null)
            yield return StartCoroutine(pieceMoveAnimator.CoReleaseFollowCamera());
    }

    // 현재 보드캠(탑뷰)이 떠 있는지
    public bool IsBoardCamActive => pieceMoveAnimator != null && pieceMoveAnimator.IsBoardCamActive;

    // #91 2배속: 턴 진행 대기에만 적용 (튜토리얼은 1배속). timeScale은 건드리지 않음.
    private float Speed => TutorialManager.isTutorial ? 1f : GameSettings.SpeedMultiplier;

    private IEnumerator CoHandlePlayerTurn(Player player)
    {
        if (player.activeSkillCooldown > 0) player.activeSkillCooldown--;

        UpdateTurnIndicator(player);
        RefreshActiveCooldowns();
        RefreshTurnNameColors();

        // 턴 시작 시 양쪽 스킬 버튼 초기화 (AI 턴은 종료 정리를 건너뛰므로 여기서 정리)
        p1ActiveSkillButton.interactable = false;
        p2ActiveSkillButton.interactable = false;
        RefreshAISkillButton();   // AI 버튼 색 갱신 (AI 차례+사용가능이면 빨강, 아니면 흰색)

        if (player.isAI)
        {
            yield return StartCoroutine(CoAITableViewDwell());          // 이전 턴 블렌드 마무리 + 테이블뷰 머무름
            yield return StartCoroutine(aiController.DecideTurn());
            yield break;
        }

        if (!isVsAI)
            yield return StartCoroutine(CoSwitchSideCam(player.playerId));

        yield return new WaitForSeconds(0.4f / Speed);

        yield return StartCoroutine(CoWaitThrowButton(player));
        LogYutResults(player);

        // 윷/모 추가 던지기 (수동)
        while (player.yutResults.Count > 0 &&
               (player.yutResults[^1] == YutResult.Yut || player.yutResults[^1] == YutResult.Mo))
        {
            VFXManager.Instance?.PlayBonusThrow();
            yield return StartCoroutine(CoWaitThrowButton(player));
            LogYutResults(player);
        }

        // 말 옮기기 단계 (검은 윷 추가 사용 포함)
        yield return StartCoroutine(CoEnterBoardCam(15));
        bool wonThisTurn = false;
        while (true)
        {
            // 결과 다 쓰면 검은 윷 추가 사용 여부 확인
            while (player.yutResults.Count > 0)
            {
                if (IsStuckOnBackdo(player))
                {
                    player.yutResults.Clear();
                    LogYutResults(player);
                    break;
                }

                // 액티브 스킬 버튼 활성화
                RefreshActiveSkillButton(player);
                RefreshActiveCooldowns();

                // 선택 시작
                dragAndDrop.BeginSelection(player);
                // 대기 중 물귀신 스킬 등으로 말이 사라져 남은 뒷도를 쓸 수 없게 되면 함께 깨어남
                yield return new WaitUntil(() => dragAndDrop.MoveConfirmed || IsStuckOnBackdo(player));

                // 스킬로 보드 말이 사라져 뒷도가 무효가 된 경우 → 선택 취소 후 루프 상단 정리로 복귀
                if (!dragAndDrop.MoveConfirmed)
                {
                    dragAndDrop.CancelSelection();
                    continue;
                }

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
                    if (isActiveSkillOn) player.Skill?.OnActiveTurnEnd();
                    isActiveSkillOn = false;
                    SetSkillArmedVisual(player, false);
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

                GameEvents.InvokePieceMoved(player.playerId);

                // 사용한 결과 제거 (씨름 패배 시에도 소모) → 잡기 연출 전에 즉시 반영
                player.yutResults.Remove(dragAndDrop.UsedYutResult);
                LogYutResults(player);

                if (targetNode.data.isJunction)
                {
                    GameEvents.InvokeJunctionReached(player.playerId);
                }

                // 귀신 액티브로 이동 중인지 기록 (아래 isActiveSkillOn 리셋 전에) — 도착 칸 잡기에서 물귀신/도깨비 패시브 무효화용
                bool gwishinActiveMove = isActiveSkillOn && pushPath != null && player.Skill is GwishinSkill;

                // 액티브 스킬 - 이동 효과
                if (isActiveSkillOn && pushPath != null)
                {
                    player.Skill.OnActiveMoveEffect(player, piece, pushPath, targetNode, RepositionNode);
                    player.Skill.OnActiveActivated(player);   // 발동 확정 → 쿨타임 차감
                    player.Skill?.OnActiveTurnEnd();
                    isActiveSkillOn = false;
                    SetSkillArmedVisual(player, false);
                    RefreshActiveSkillButton(player);         // 쿨타임 반영 → 즉시 흰색
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
                    var outcome = gwishinActiveMove
                        ? CaptureOutcome.Captured   // 귀신 액티브: 도깨비 씨름(역잡기) 무시
                        : (skilledEnemy?.owner.Skill.OnCaptureAttempt(skilledEnemy, piece, totalEnemyCount)
                           ?? CaptureOutcome.Captured);

                    if (outcome == CaptureOutcome.Reversed)
                    {
                        wasReversed = true;
                        var reversedPieces = new[] { piece }.Concat(stackAll).ToList();

                        VFXManager.Instance?.PlayDokkaebi(targetNode.transform.position);
                        player.OnCaught(piece);
                        GameEvents.InvokeCaptured(player.playerId);

                        foreach (var r in reversedPieces)
                        {
                            targetNode.piecesOnNode.Remove(r);
                            r.currentNode = null;
                            r.nodeHistory.Clear();
                            r.stackLeader = null;
                            r.stackedPieces.Clear();
                            r.pieceObject.transform.position = r.pieceObject.initPosition;
                        }

                    }
                    else
                    {
                        foreach (var enemyLeader in enemyLeaders)
                        {
                            var capturedPieces = new[] { enemyLeader }.Concat(enemyLeader.stackedPieces).ToList();

                            enemyLeader.owner.OnCaught(enemyLeader);
                            enemyLeader.owner.AddWonhan(enemyLeader.stackedPieces.Count);
                            bool noBonus = (gwishinActiveMove && enemyLeader.owner.Skill is MulgwishinSkill)
                                ? false   // 귀신 액티브: 물귀신 윷 결과 삭제 패시브 무효화
                                : (enemyLeader.owner.Skill?.OnBeingCaptured(enemyLeader, piece) ?? false);
                            if (noBonus) VFXManager.Instance?.PlayMulgwishin(targetNode.transform.position);

                            foreach (var caught in capturedPieces)
                            {
                                targetNode.piecesOnNode.Remove(caught);
                                caught.currentNode = null;
                                caught.nodeHistory.Clear();
                                caught.stackLeader = null;
                                caught.stackedPieces.Clear();
                                caught.pieceObject.transform.position = caught.pieceObject.initPosition;
                            }

                            RepositionNode(targetNode);
                            GameEvents.InvokeCaptureSuccess(player.playerId);
                            // 귀신 액티브: 공용 '잡았다!' 배너를 귀신 전용 배너("뒤를 봐.")로 덮어씀
                            if (gwishinActiveMove) VFXManager.Instance?.ShowGwishinActiveBanner();
                            int blackYutBefore = player.BlackYutCount;
                            player.Skill?.OnCapture(piece, capturedPieces);
                            // 귀신: 이 잡기로 원한이 차 검은 윷이 새로 생긴 순간에만 파티클 (매 잡기 X)
                            if (player.Skill is GwishinSkill && player.BlackYutCount > blackYutBefore)
                                VFXManager.Instance?.PlayGwishin(targetNode.transform.position);
                            if (!noBonus)
                            {
                                yield return StartCoroutine(CoWaitThrowButton(player, isCaptureBonus: true));
                            }
                            LogYutResults(player);
                        }
                    }

                    RepositionNode(targetNode);
                }

                if (wasReversed) continue;

                // 업기 처리
                var friendlyLeaders = targetNode.piecesOnNode.Where(p => p.owner == player && p != piece && p.stackLeader == null).ToList();

                if (friendlyLeaders.Count == 1)
                {
                    if (TutorialManager.isTutorial)
                    {
                        stackDecision = true;
                    }
                    else
                    {
                        stackDecision = null;
                        stackDecisionPanel.SetActive(true);

                        yield return new WaitUntil(() => stackDecision.HasValue);

                        stackDecisionPanel.SetActive(false);
                    }

                    if (stackDecision == true)
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
                        GameEvents.InvokePieceStacked(player.playerId);
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
                        GameEvents.InvokePieceStacked(player.playerId);
                    }
                }

            }

            // 결과를 다 썼을 때 남은 검은 윷이 있으면 던지거나 턴 종료 선택
            if (wonThisTurn) break;
            if (!player.HasBlackYut) break;

            endTurnRequested = false;
            endTurnButton.gameObject.SetActive(true);
            blackYutButton.gameObject.SetActive(player.HasBlackYut);  // 결과 소진 후에도 검은 윷 즉시 사용 보장(액티브 잡기로 막 얻은 경우 등)

            yield return new WaitUntil(() => endTurnRequested || player.yutResults.Count > 0);

            endTurnButton.gameObject.SetActive(false);

            if (endTurnRequested) break;
            // 검은 윷을 던진 경우 → inner while 재진입
        }

        // 로컬 모드: 보드캠을 내리기 전에 다음 플레이어(상대) 사이드캠을 먼저 올림
        // → 보드캠이 내려갈 때 테이블캠을 거치지 않고 상대 시점으로 직접 전환
        if (!isVsAI && !wonThisTurn)
        {
            var nextSide = player.playerId == 0 ? p2SideCam : p1SideCam;
            var currSide = player.playerId == 0 ? p1SideCam : p2SideCam;
            if (nextSide != null) nextSide.Priority = new PrioritySettings { Value = 15 };
            if (currSide != null) currSide.Priority = new PrioritySettings { Value = 0 };
        }

        if (pieceMoveAnimator != null) yield return StartCoroutine(pieceMoveAnimator.CoReleaseFollowCamera());
        blackYutButton.gameObject.SetActive(false);
        endTurnButton.gameObject.SetActive(false);
        throwYutButton.gameObject.SetActive(false);
        p1ActiveSkillButton.interactable = false;
        p2ActiveSkillButton.interactable = false;
        if (isActiveSkillOn) player.Skill?.OnActiveTurnEnd();
        isActiveSkillOn = false;
        SetSkillArmedVisual(player, false);
    }

    private IEnumerator CoWaitThrowButton(Player player, bool isCaptureBonus = false)   // 윷 던지기 버튼 입력 대기
    {
        throwRequested = false; // 종료 플래그

        blackYutButton.gameObject.SetActive(false);

        // #119 자동 던지기: 켜져 있으면 버튼 숨기고 잠깐 뒤 자동으로 던진다. (튜토리얼은 제외 — 검은 윷은 항상 수동)
        if (GameSettings.AutoThrow && !TutorialManager.isTutorial)
        {
            throwYutButton.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.5f);  // 던지기 캠을 잠깐 보여준 뒤 자동
        }
        else
        {
            throwYutButton.gameObject.SetActive(true);
            yield return new WaitUntil(() => throwRequested);
            throwYutButton.gameObject.SetActive(false);
        }

        GameEvents.InvokeYutThrown(player.playerId);    // 3D 모델용
        yield return StartCoroutine(yutThrowController.CoThrow());
        player.AddThrowResult(yutThrowController.LastResult);

        // 이동 단계 도중의 잡기 보너스 던지기 → 던지기 캠을 내려 보드캠(15)으로 복귀
        if (isCaptureBonus) yutThrowController.ReleaseThrowCam();
    }

    private IEnumerator CoHandleBlackYutThrow()
    {
        blackYutButton.interactable = false;
        endTurnButton.interactable = false;

        currPlayer.ConsumeBlackYut();   // 클릭 즉시 개수 차감 + UI 갱신
        if (!currPlayer.HasBlackYut) blackYutButton.gameObject.SetActive(false);

        GameEvents.InvokeYutThrown(currPlayer.playerId);
        VFXManager.Instance?.PlayBlackYutThrow();
        yield return StartCoroutine(yutThrowController.CoThrow(isBlackYut: true));
        currPlayer.AddThrowResult(yutThrowController.LastResult);

        // 이동 단계 도중의 검은 윷 던지기 → 던지기 캠을 내려 보드캠(15)으로 복귀
        yutThrowController.ReleaseThrowCam();

        blackYutButton.interactable = true;
        endTurnButton.interactable = true;

        LogYutResults(currPlayer);
    }

    private IEnumerator CoEndGame()
    {
        if (yutGuidePopup != null) yutGuidePopup.Disarm();

        yield return new WaitForSeconds(1f);
        var winner = System.Linq.Enumerable.First(players, p => p.AllFinished);
        gameOverUI.Show(winner.playerId, isVsAI);
    }

    private void UpdateTurnIndicator(Player player)
    {
        if (turnIndicatorText == null || player == null) return;
        string charName = player.characterData != null
            ? LocalizationManager.Get(player.characterData.localizationKey) : player.name;
        turnIndicatorText.text = LocalizationManager.Get("TURN_LABEL", charName);

        turnIndicatorText.color = player.playerId == 0 ? _themeColor0 : _themeColor1;   // 캐릭터 색(미러전 톤업 포함)
    }

    // 현재 차례인 플레이어의 캐릭터 이름만 테마색, 상대는 흰색
    private void RefreshTurnNameColors()
    {
        if (p1CharacterNameText != null)
            p1CharacterNameText.color = (currPlayer == players[0]) ? _themeColor0 : Color.white;
        if (p2CharacterNameText != null)
            p2CharacterNameText.color = (currPlayer == players[1]) ? _themeColor1 : Color.white;
    }

    private void RefreshActiveCooldowns()
    {
        UpdateActiveCooldown(p1ActiveCooldownGroup, p1ActiveCooldownText, players[0]);
        UpdateActiveCooldown(p2ActiveCooldownGroup, p2ActiveCooldownText, players[1]);
    }

    private void UpdateActiveCooldown(GameObject group, TMP_Text txt, Player player)
    {
        if (group == null) return;
        bool hasActive = (player.Skill?.ActiveCooldown ?? 0) > 0;
        group.SetActive(hasActive);                                  // 아이콘+숫자 통째로 (도깨비면 숨김)
        if (hasActive && txt != null)
            txt.text = Mathf.Max(0, player.activeSkillCooldown).ToString();
    }

    private void LogYutResults(Player player)
    {
        GameLogUI.UpdateYutResults(player.yutResults, player.name);
        blackYutButton.gameObject.SetActive(player.HasBlackYut);

        RefreshBlackYutCounts();
    }

    // 검은 윷 획득/사용 이벤트 시 즉시 호출 → 게이지와 동기화 (LogYutResults를 기다리지 않음)
    private void HandleBlackYutCountChanged(int _) => RefreshBlackYutCounts();

    private void RefreshBlackYutCounts()
    {
        UpdateBlackYutCount(p1BlackYutCountText, players[0].BlackYutCount);
        UpdateBlackYutCount(p2BlackYutCountText, players[1].BlackYutCount);
    }

    private void UpdateBlackYutCount(TMP_Text txt, int count)
    {
        if (txt == null) return;
        txt.text = "x" + count;   // 0이어도 x0 표시
    }

    private IEnumerator CoHandleActiveSkill(Player player)
    {
        if (player.isAI) yield break;   // AI 버튼은 사용 가능 여부 표시용으로만 활성 — 클릭은 무시
        var skill = player.Skill;

        if (skill.HasImmediateEffect)
        {
            if (dragAndDrop.IsPickingSacrifice)          // 희생 선택 중 재클릭 → 취소
            {
                if (TutorialManager.isTutorial) yield break;   // 튜토리얼 시연 중엔 취소 불가
                dragAndDrop.CancelSacrificePick();
            }
            else if (skill.CanUseActive(player))         // 쿨타임 등 확인 후 발동
            {
                yield return StartCoroutine(skill.CoOnActiveActivated(player, RequestPiecePickCoroutine, RepositionNode));
                RefreshActiveSkillButton(player);        // 발동(흰색)/취소(빨강) 후 즉시 반영
            }
        }
        else if (isActiveSkillOn)                        // 무장 중 재클릭 → 해제 (차감 없음)
        {
            if (TutorialManager.isTutorial) yield break;   // 튜토리얼 시연 중엔 무장 해제 불가
            isActiveSkillOn = false;
            skill.OnActiveTurnEnd();
            SetSkillArmedVisual(player, false);
        }
        else if (skill.CanUseActive(player))             // 무장 (쿨타임은 실제 발동 시에만 차감)
        {
            isActiveSkillOn = true;
            skill.OnActiveTurnStart();
            SetSkillArmedVisual(player, true);
        }
    }

    private IEnumerator RequestPiecePickCoroutine(List<Piece> candidates, Action<Piece> onPicked)
    {
        dragAndDrop.BeginSacrificePick(candidates);
        SetSkillArmedVisual(currPlayer, true);   // 선택 중 버튼 강조 (귀신 무장과 동일)
        VFXManager.Instance?.BannerHoldOn(LocalizationManager.Get("BANNER_SACRIFICE_PICK"),
                                          new Color(0.043f, 0.482f, 0.541f));  // 청록 #0B7B8A
        GameEvents.InvokeSacrificePickStart();
        yield return new WaitUntil(() => dragAndDrop.SacrificeConfirmed || dragAndDrop.SacrificeCancelled);
        GameEvents.InvokeSacrificePickEnd();
        VFXManager.Instance?.BannerHoldOff();
        SetSkillArmedVisual(currPlayer, false);  // 확정·취소 모두 OFF
        onPicked(dragAndDrop.SacrificeConfirmed ? dragAndDrop.SacrificeTarget : null);
    }

    public Player GetOpponent(Player player) => players[1 - player.playerId];

    public bool IsActiveSkillOn => isActiveSkillOn;

    private Button GetActiveSkillButton(Player player) => player.playerId == 0 ? p1ActiveSkillButton : p2ActiveSkillButton;

    // 현재 플레이어 액티브 버튼 상태 갱신 (사용 가능 → interactable=true(빨강) / 불가 → 흰색). 발동 직후 즉시 호출.
    private void RefreshActiveSkillButton(Player player)
    {
        GetActiveSkillButton(player).interactable =
            (!TutorialManager.isTutorial || TutorialManager.allowSkillDemo)
            && player.Skill?.CanUseActive(player) == true;
    }

    // 지연형 액티브 무장 상태를 버튼에 시각 표시 (테두리/글로우 등)
    private void SetSkillArmedVisual(Player player, bool armed)
    {
        var hl = player.playerId == 0 ? p1SkillArmedHighlight : p2SkillArmedHighlight;
        if (hl != null) hl.SetActive(armed);
    }

    // AI 스킬 버튼: 'AI 차례 + 사용 가능'일 때만 Normal색(빨강), 그 외엔 Disabled색(흰색). 클릭/눌림은 막음(interactable=false).
    public void RefreshAISkillButton()
    {
        Player aiPlayer = players[0].isAI ? players[0] : (players[1].isAI ? players[1] : null);
        if (aiPlayer == null) return;                      // AI 없는 모드(로컬 2인)면 건너뜀
        var btn = GetActiveSkillButton(aiPlayer);
        var cb = btn.colors;                               // 에디터에 설정한 Normal(빨강)/Disabled(흰색) 색
        btn.interactable = false;                          // 눌림/클릭 차단 (시각 표시 전용)
        btn.transition = Selectable.Transition.None;       // 자동 틴트 끄고 색을 직접 지정
        bool active = currPlayer == aiPlayer               // AI 차례일 때만
                      && (!TutorialManager.isTutorial || TutorialManager.allowSkillDemo)
                      && aiPlayer.Skill?.CanUseActive(aiPlayer) == true;
        if (btn.targetGraphic != null)
            btn.targetGraphic.color = active ? cb.normalColor : cb.disabledColor;
    }

    // 남은 결과가 뒷도뿐이고 뒷걸음할 말이 없어 진행이 불가능한 상태
    private bool IsStuckOnBackdo(Player player) =>
        player.yutResults.Count > 0 &&
        player.yutResults.All(yr => yr == YutResult.BACKDO) &&
        !player.pieces.Any(p => !p.hasFinished && p.currentNode != null &&
            (p.nodeHistory.Count > 0 || p.currentNode.data == boardData.startNode));

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

        player.Skill?.OnFinish(piece);
        GameEvents.InvokePieceFinished(player.playerId);
    }

    public BoardNode GetNode(BoardNodeData data)
    {
        if (data == null) return null;
        boardNodeMap.TryGetValue(data, out var node);
        return node;
    }

    private void ApplyNodeHistory(Piece leader, List<Piece> stacked, List<BoardNode> pushPath)
    {
        if (pushPath == null)   // 뒷도
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

            for (int j = 0; j < units[i].stackedPieces.Count; j++)
                units[i].stackedPieces[j].pieceObject.transform.position = pos + Vector3.up * StackYOffset * (j + 1);
        }
    }

    // 플레이어 이동 처리와 동일, but 자동화.
    public IEnumerator ApplyAIMove(Piece piece, BoardNodeData targetData, List<BoardNodeData> pushPath, YutResult used, bool isOut, bool useActiveSkill = false)
    {
        var stackAll = piece.stackedPieces.ToList();    // 쌓인 말들 한꺼번에 다루기

        if (isOut)  // 말이 나가는 경우
        {
            // [말 이동과 시각화]
            var nodeBeforeOut = piece.currentNode;

            nodeBeforeOut.piecesOnNode.Remove(piece);   // 현위치에서 말 제거
            foreach (var s in stackAll)
                nodeBeforeOut.piecesOnNode.Remove(s);

            RepositionNode(nodeBeforeOut);

            yield return StartCoroutine(CoEnterBoardCam());    // 보드캠으로 시점 전환

            var endPositions = currPlayer.playerId == 0 ? p1EndPositions : p2EndPositions;
            var allFinishing = new List<Piece> { piece };   // 리더
            allFinishing.AddRange(stackAll);                // + 쌓인 말 모두

            var destPositions = allFinishing.Select((_, i) => endPositions[currPlayer.FinishedCount + i].position).ToList();

            yield return StartCoroutine(pieceMoveAnimator.CoAnimatePieceToPositions(allFinishing, destPositions));
            HandleFinish(piece, stackAll, currPlayer);

            // 보드캠 유지 — 남은 이동을 모두 끝낸 뒤 AIController가 일괄 복귀
            currPlayer.yutResults.Remove(used);
            GameLogUI.UpdateYutResults(currPlayer.yutResults, currPlayer.name);

            yield break;    // 코루틴 종료
        }

        // [말 이동]
        var prevNode = piece.currentNode;

        prevNode?.piecesOnNode.Remove(piece);
        foreach (var s in stackAll)
            prevNode?.piecesOnNode.Remove(s);

        var pushPathNodes = pushPath != null ? pushPath.ConvertAll(d => GetNode(d)) : null; // 노드 데이터 -> 노드(obj)
        ApplyNodeHistory(piece, stackAll, pushPathNodes);

        var targetNode = GetNode(targetData);
        piece.currentNode = targetNode;

        targetNode.piecesOnNode.Add(piece);        
        foreach (var s in stackAll)
        {
            s.currentNode = targetNode;
            targetNode.piecesOnNode.Add(s);
        }

        if (prevNode != null) RepositionNode(prevNode);

        if (useActiveSkill)
        {
            yield return StartCoroutine(CoEnterBoardCam()); // 보드캠 진입(블렌드 완료)까지 기다린 뒤 잡기 연출
            RepositionNode(targetNode);
            if (pushPathNodes != null)
            {
                currPlayer.Skill.OnActiveActivated(currPlayer);
                currPlayer.Skill.OnActiveMoveEffect(currPlayer, piece, pushPathNodes, targetNode, RepositionNode);
            }
        }
        else
        {
            yield return StartCoroutine(CoEnterBoardCam());
            yield return StartCoroutine(pieceMoveAnimator.CoAnimatePieceMove(piece, stackAll, pushPathNodes, targetNode));
            RepositionNode(targetNode);
            // CoReleaseFollowCamera는 잡기·업기 시각화 후 메서드 끝에서 호출
        }

        // 사용한 결과 제거 → 잡기 연출 전에 즉시 반영
        currPlayer.yutResults.Remove(used);
        GameLogUI.UpdateYutResults(currPlayer.yutResults, currPlayer.name);

        // 귀신 액티브로 이동했는지 — 도착 칸 잡기에서 물귀신/도깨비 패시브 무효화용
        bool gwishinActiveMove = useActiveSkill && pushPathNodes != null && currPlayer.Skill is GwishinSkill;

        // [잡기 처리]
        var enemyLeaders = targetNode.piecesOnNode.Where(p => p.owner != currPlayer && p.stackLeader == null).ToList();
        if (enemyLeaders.Count > 0)
        {
            int totalEnemyCount = enemyLeaders.Sum(e => 1 + e.stackedPieces.Count);
            var skilledEnemy = enemyLeaders.FirstOrDefault(e => e.owner.Skill != null);
            var outcome = gwishinActiveMove
                ? CaptureOutcome.Captured   // 귀신 액티브: 도깨비 씨름(역잡기) 무시
                : (skilledEnemy?.owner.Skill.OnCaptureAttempt(skilledEnemy, piece, totalEnemyCount) ?? CaptureOutcome.Captured);

            if (outcome == CaptureOutcome.Reversed) // 반격당함
            {
                var reversedPieces = new[] { piece }.Concat(stackAll).ToList();

                VFXManager.Instance?.PlayDokkaebi(targetNode.transform.position);
                currPlayer.OnCaught(piece);
                currPlayer.AddWonhan(stackAll.Count);
                GameEvents.InvokeCaptured(currPlayer.playerId);

                foreach (var r in reversedPieces)
                {
                    SendHome(r, targetNode);
                }
            }
            else
            {
                foreach (var enemyLeader in enemyLeaders)
                {
                    var capturedPieces = new[] { enemyLeader }.Concat(enemyLeader.stackedPieces).ToList();

                    enemyLeader.owner.OnCaught(enemyLeader);
                    enemyLeader.owner.AddWonhan(enemyLeader.stackedPieces.Count);
                    GameEvents.InvokeCaptured(enemyLeader.owner.playerId);

                    bool noBonus = (gwishinActiveMove && enemyLeader.owner.Skill is MulgwishinSkill)
                        ? false   // 귀신 액티브: 물귀신 윷 결과 삭제 패시브 무효화
                        : (enemyLeader.owner.Skill?.OnBeingCaptured(enemyLeader, piece) ?? false);
                    if (noBonus) VFXManager.Instance?.PlayMulgwishin(targetNode.transform.position);

                    foreach (var caught in capturedPieces)
                    {
                        SendHome(caught, targetNode);
                    }

                    RepositionNode(targetNode);
                    GameEvents.InvokeCaptureSuccess(currPlayer.playerId);
                    // 귀신 액티브: 공용 '잡았다!' 배너를 귀신 전용 배너("뒤를 봐.")로 덮어씀
                    if (gwishinActiveMove) VFXManager.Instance?.ShowGwishinActiveBanner();
                    int blackYutBefore = currPlayer.BlackYutCount;
                    currPlayer.Skill?.OnCapture(piece, capturedPieces);
                    // 귀신: 이 잡기로 원한이 차 검은 윷이 새로 생긴 순간에만 파티클 (매 잡기 X)
                    if (currPlayer.Skill is GwishinSkill && currPlayer.BlackYutCount > blackYutBefore)
                        VFXManager.Instance?.PlayGwishin(targetNode.transform.position);
                    if (!noBonus)
                    {
                        GameEvents.InvokeYutThrown(currPlayer.playerId);
                        yield return StartCoroutine(yutThrowController.CoThrow());
                        currPlayer.AddThrowResult(yutThrowController.LastResult);
                        GameLogUI.UpdateYutResults(currPlayer.yutResults, currPlayer.name);

                        // 잡기 보너스 던지기 → 던지기 캠을 내려 보드캠(15)으로 복귀
                        yutThrowController.ReleaseThrowCam();
                    }
                }
            }

            RepositionNode(targetNode);
        }

        // 업기 처리 (자동)
        var friendlyLeaders = targetNode.piecesOnNode.Where(p => p.owner == currPlayer && p != piece && p.stackLeader == null).ToList();

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
        }

        // 보드캠 유지 — 남은 이동을 모두 끝낸 뒤 AIController가 일괄 복귀
    }

    private void SendHome(Piece p, BoardNode fromNode)
    {
        fromNode.piecesOnNode.Remove(p);
        p.currentNode = null;
        p.nodeHistory.Clear();
        p.stackLeader = null;
        p.stackedPieces.Clear();
        p.pieceObject.transform.position = p.pieceObject.initPosition;
    }

    public Player GetPlayer(int idx) => players[idx];

    public void PlaceTutorialPiece(Piece piece, BoardNodeData targetNodeData) // 튜토 전용
    {
        if (piece.stackLeader != null)
        {
            piece.stackLeader.stackedPieces.Remove(piece);
            piece.stackLeader = null;
        }

        if (piece.currentNode != null)
        {
            piece.currentNode.piecesOnNode.Remove(piece);
            RepositionNode(piece.currentNode);
            piece.currentNode = null;
        }
        piece.stackLeader = null;
        foreach (var s in piece.stackedPieces)
        {
            s.currentNode = null;
            s.stackLeader = null;
            s.nodeHistory.Clear();
            s.pieceObject.transform.position = s.pieceObject.initPosition;
        }
        piece.stackedPieces.Clear();
        piece.nodeHistory.Clear();

        if (targetNodeData == null)
        {
            piece.pieceObject.transform.position = piece.pieceObject.initPosition;
            return;
        }
        var node = GetNode(targetNodeData);
        if (node == null) return;
        piece.currentNode = node;
        node.piecesOnNode.Add(piece);
        RepositionNode(node);
    }
}
