using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class DragAndDrop : MonoBehaviour
{
    // 선택 입력
    private Camera cam;
    private GameInput input;
    private bool needSelection = false; // true일 때만 입력을 받음
    private bool isDragging = false;

    private Player currPlayer;

    public Piece SelectedPiece { get; private set; }            // 선택 말
    public BoardNode SelectedBoardNode { get; private set; }    // 선택 칸 (확정)
    public bool MoveConfirmed { get; private set; }             // 말 이동 확정 플래그
    public YutResult UsedYutResult { get; private set; }
    public BoardNode PrevOfSelectedPiece { get; private set; }

    // 유효한 목적지 판별(+하이라이트) 등에 사용
    [SerializeField] private BoardData boardData;   // 전체 노드 데이터
    public Dictionary<BoardNodeData, BoardNode> boardNodeMap;
    private Dictionary<BoardNode, YutResult> validDestToYutResult = new();   // <유효 목적지, 사용할 윷 결과> : UsedYutResult 할당
    private Dictionary<BoardNode, BoardNode> destToPrevNode = new();    // <목적지, 그 직전 노드> : PrevOfSelectedPiece 할당
                                                                        // ComputeAndHighlightDestinations() 타이밍에 정보 저장됨

    // 레이어 마스크
    [SerializeField] private LayerMask pieceLayer;
    [SerializeField] private LayerMask boardNodeLayer;
    [SerializeField] private LayerMask boardSurfaceLayer;
    [SerializeField] private LayerMask outZoneLayer;

    // 시각화
    private float halfHeight = 0f;      // 드래그 중인 오브젝트의 키/2 (보드 표면 위를 따라가게 할 때 튀어나오게 할 높이)
    private Vector3 originPosition;     // 이동 전 위치. 유효하지 않은 위치에서 릴리즈 시 복귀할 지점

    // 완주 처리
    private bool canOut;
    public List<YutResult> ValidOutResults { get; private set; } = new();   // 윷 결과들 중, 말을 나가게 할 수 있는 값들
    public bool IsOutConfirmed { get; private set; }

    // 업기 대상 말 선택 모드
    private bool pickingStackTarget = false;
    private List<Piece> stackTargetCandidates = new();
    public Piece PickedStackTarget { get; private set; }
    public bool PickConfirmed { get; private set; }
    public bool PickDeclined { get; private set; }

    // 아웃 존
    [SerializeField] private Collider outZoneCollider;
    [SerializeField] private GameObject outZoneHighlight;

    // ㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡ

    private void Awake()
    {
        input = new GameInput();
        cam = Camera.main;
    }

    private void Update()   // 드래그 - 오브젝트 이동
    {
        if (!isDragging || SelectedPiece == null) return;

        Vector2 mousePos = input.GamePlay.Point.ReadValue<Vector2>();
        Ray ray = cam.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, boardSurfaceLayer))  // 보드 표면 따라가게
        {
            SelectedPiece.pieceObject.transform.position = hit.point + Vector3.up * halfHeight;

            for (int i = 0; i < SelectedPiece.stackedPieces.Count; i++) // 쌓인 말들도 다같이
                SelectedPiece.stackedPieces[i].pieceObject.transform.position = hit.point + Vector3.up * (halfHeight + 0.1f * (i + 1));            
        }
    }

    private void OnEnable()
    {
        input.GamePlay.Enable();
        input.GamePlay.Click.started += OnClickStarted;
        input.GamePlay.Click.canceled += OnClickReleased;
    }

    private void OnDisable()
    {
        input.GamePlay.Click.started -= OnClickStarted;
        input.GamePlay.Click.canceled -= OnClickReleased;
        input.GamePlay.Disable();
    }

    public void BeginSelection(Player player)
    {
        currPlayer = player;

        MoveConfirmed = false;
        IsOutConfirmed = false;
        needSelection = true;

        // PrevOfSelectedPiece = null; (MoveConfirmed와 PrevOfSelectedPiece는 짝지어 써야 함, 현재 코드에서는 초기화 필요 없어서 주석 처리)

        ValidOutResults.Clear();
        
        // 선택 가능한 말(Finished = false인 애들) 시각화 (나중에)
    }

    private void OnClickStarted(InputAction.CallbackContext ctx)
    {
        if (pickingStackTarget) // 쌓을 대상 선택
        {
            var mousePos = input.GamePlay.Point.ReadValue<Vector2>();
            var ray = cam.ScreenPointToRay(mousePos);
            var hits = Physics.RaycastAll(ray, 100f, pieceLayer);
            foreach (var h in hits)
            {
                var pieceObj = h.collider.GetComponent<PieceObject>();
                var leader = pieceObj.piece.stackLeader ?? pieceObj.piece;
                if (stackTargetCandidates.Contains(leader))
                {
                    PickedStackTarget = leader;
                    EndStackTargetPick();
                    PickConfirmed = true;
                    break;
                }
            }
            return;
        }

        if (!needSelection) return;

        var mousePos2 = input.GamePlay.Point.ReadValue<Vector2>();
        var ray2 = cam.ScreenPointToRay(mousePos2);
        var hits2 = Physics.RaycastAll(ray2, 100f, pieceLayer);
        foreach (var h in hits2)
        {
            var pieceObj = h.collider.GetComponent<PieceObject>();
            var leader = pieceObj.piece.stackLeader ?? pieceObj.piece;  // 겹친 말들의 대표 저장
            if (leader.owner == currPlayer && !leader.hasFinished)
            {
                SelectedPiece = leader;
                halfHeight = h.collider.bounds.extents.y;
                originPosition = SelectedPiece.pieceObject.transform.position;
                isDragging = true;
                ComputeAndHighlightDestinations();
                break;
            }
        }
    }

    private void OnClickReleased(InputAction.CallbackContext ctx)
    {
        if (!isDragging) return;

        var mousePos = input.GamePlay.Point.ReadValue<Vector2>();
        var ray = cam.ScreenPointToRay(mousePos);
        var hits = Physics.RaycastAll(ray, 100f, boardNodeLayer);
        foreach (var h in hits)
        {
            var node = h.collider.GetComponent<BoardNode>();
            if (node != null && validDestToYutResult.TryGetValue(node, out YutResult usedResult))  // 놓은 곳이 유효한 자리라면
            {
                SelectedBoardNode = node;
                UsedYutResult = usedResult;

                // 도착지 직전 노드 저장 (이후 백도 처리 용도)
                destToPrevNode.TryGetValue(node, out BoardNode prevNode);
                PrevOfSelectedPiece = prevNode;

                MoveConfirmed = true;
                needSelection = false;
                break;
            }
        }

        if (!MoveConfirmed && canOut)
        {
            var outHits = Physics.RaycastAll(ray, 100f, outZoneLayer);
            foreach (var h in outHits)
            {
                if (h.collider == outZoneCollider)
                {
                    SelectedBoardNode = null;
                    PrevOfSelectedPiece = null;

                    IsOutConfirmed = true;
                    MoveConfirmed = true;
                    needSelection = false;
                    break;
                }
            }
        }

        if (!MoveConfirmed) // 잘못된 위치에서 릴리즈 시 위치 복구
        {
            SelectedPiece.pieceObject.transform.position = originPosition;
            for (int i = 0; i < SelectedPiece.stackedPieces.Count; i++)
                SelectedPiece.stackedPieces[i].pieceObject.transform.position = originPosition + Vector3.up * 0.1f * (i + 1);
        }

        ClearHighlights();
        isDragging = false;
    }

    private void ComputeAndHighlightDestinations()
    {
        var moves = PieceMoveCalculator.ComputeMoves(SelectedPiece, currPlayer.yutResults, boardData);
        validDestToYutResult.Clear();
        destToPrevNode.Clear();

        foreach (var kv in moves.Destinations)
        {
            var node = boardNodeMap[kv.Key];
            validDestToYutResult[node] = kv.Value.yr;
            destToPrevNode[node] = kv.Value.prevNode != null ? boardNodeMap[kv.Value.prevNode] : null;
            node.SetHighlight(true);
        }

        ValidOutResults = moves.OutResults;
        canOut = moves.OutResults.Count > 0;
        if (canOut && outZoneHighlight != null) outZoneHighlight.SetActive(true);
    }

    private BoardNode GetNodeByData(BoardNodeData data)
    {
        boardNodeMap.TryGetValue(data, out var node);
        return node;
    }

    public void BeginStackTargetPick(List<Piece> candidates)
    {
        stackTargetCandidates = candidates;

        PickedStackTarget = null;
        PickConfirmed = false;
        PickDeclined = false;
        pickingStackTarget = true;

        foreach (var c in candidates)
            c.pieceObject.SetHighLight(true);
    }

    public void DeclineStackTargetPick()
    {
        EndStackTargetPick();
        PickDeclined = true;
    }

    public void EndStackTargetPick()
    {
        foreach (var c in stackTargetCandidates)
            c.pieceObject.SetHighLight(false);

        stackTargetCandidates.Clear();
        pickingStackTarget = false;
    }

    private void ClearHighlights()
    {
        foreach (var node in validDestToYutResult.Keys)
            node.SetHighlight(false);

        validDestToYutResult.Clear();

        if (canOut)
        {
            if (outZoneHighlight != null) outZoneHighlight.SetActive(false);
            canOut = false;
        }
    }
}