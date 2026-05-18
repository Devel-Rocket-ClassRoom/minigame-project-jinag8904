using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class DragAndDrop : MonoBehaviour
{
    private Camera cam;
    private GameInput input;

    [SerializeField] private LayerMask pieceLayer;
    [SerializeField] private LayerMask boardNodeLayer;
    [SerializeField] private LayerMask boardSurfaceLayer;
    [SerializeField] private LayerMask outZoneLayer;
    [SerializeField] private BoardData boardData;
    [SerializeField] private Collider outZoneCollider;
    [SerializeField] private GameObject outZoneHighlight;

    // ㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡ

    public bool MoveConfirmed { get; private set; }
    public YutResult UsedYutResult { get; private set; }
    public BoardNode PreviousNodeForSelected { get; private set; }
    private bool needSelection = false;
    public Piece SelectedPiece { get; private set; }
    public BoardNode selectedBoardNode { get; private set; }

    private Player currPlayer;
    private bool isDragging = false;
    private float dragHalfHeight = 0f;
    private Vector3 originPosition;
    private BoardNode[] allBoardNodes;
    private Dictionary<BoardNode, YutResult> validDestinations = new();
    private Dictionary<BoardNode, BoardNode> destToPrevNode = new();
    private bool canOut;
    public List<YutResult> ValidOutResults { get; private set; } = new();
    public bool IsOutConfirmed { get; private set; }

    // ㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡ

    private void Awake()
    {
        input = new GameInput();
        cam = Camera.main;
        allBoardNodes = FindObjectsByType<BoardNode>(FindObjectsSortMode.None);
    }

    private void Update()
    {
        if (!isDragging || SelectedPiece == null) return;

        Vector2 mousePos = input.GamePlay.Point.ReadValue<Vector2>();   // 드래그 하는 동안 말을 마우스 위치로 따라가게 함
        Ray ray = cam.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, boardSurfaceLayer))
        {
            SelectedPiece.pieceObject.transform.position = hit.point + Vector3.up * dragHalfHeight;
            for (int i = 0; i < SelectedPiece.stackedPieces.Count; i++)
                SelectedPiece.stackedPieces[i].pieceObject.transform.position = hit.point + Vector3.up * (dragHalfHeight + 0.1f * (i + 1));
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

    private void OnClickStarted(InputAction.CallbackContext ctx)
    {
        if (!needSelection) return;

        var mousePos = input.GamePlay.Point.ReadValue<Vector2>();
        var ray = cam.ScreenPointToRay(mousePos);
        var hits = Physics.RaycastAll(ray, 100f, pieceLayer);

        foreach (var h in hits)
        {
            var pieceObj = h.collider.GetComponent<PieceObject>();
            var leader = pieceObj.piece.stackLeader ?? pieceObj.piece;  // 업힌 말이면 리더로
            if (leader.owner == currPlayer && !leader.hasFinished)
            {
                SelectedPiece = leader;
                dragHalfHeight = h.collider.bounds.extents.y;
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
            if (node != null && validDestinations.TryGetValue(node, out YutResult usedResult))
            {
                selectedBoardNode = node;
                UsedYutResult = usedResult;
                destToPrevNode.TryGetValue(node, out BoardNode prevNode);
                PreviousNodeForSelected = prevNode;
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
                    selectedBoardNode = null;
                    PreviousNodeForSelected = null;
                    IsOutConfirmed = true;
                    MoveConfirmed = true;
                    needSelection = false;
                    break;
                }
            }
        }

        if (!MoveConfirmed)
        {
            SelectedPiece.pieceObject.transform.position = originPosition;
            for (int i = 0; i < SelectedPiece.stackedPieces.Count; i++)
                SelectedPiece.stackedPieces[i].pieceObject.transform.position = originPosition + Vector3.up * 0.1f * (i + 1);
        }

        ClearHighlights();
        isDragging = false;
    }

    public void BeginSelection(Player player)
    {
        currPlayer = player;

        MoveConfirmed = false;
        IsOutConfirmed = false;
        ValidOutResults.Clear();
        PreviousNodeForSelected = null;
        needSelection = true;

        // 선택 가능한 말(Finished = false인 애들) 시각화 (나중에)
    }

    private void ComputeAndHighlightDestinations()
    {
        var dataMap = Yutnori.GetAllPossibleDestinations(SelectedPiece, currPlayer.yutResults, boardData);
        var penultimateMap = Yutnori.GetPenultimateNodes(SelectedPiece, currPlayer.yutResults, boardData);
        validDestinations.Clear();
        destToPrevNode.Clear();

        foreach (var node in allBoardNodes)
        {
            if (dataMap.TryGetValue(node.data, out YutResult yr))
            {
                validDestinations[node] = yr;
                penultimateMap.TryGetValue(node.data, out BoardNodeData prevData);
                destToPrevNode[node] = prevData != null ? FindBoardNode(prevData) : null;
                node.SetHighlight(true);
            }
        }

        ValidOutResults = Yutnori.GetAllOutResults(SelectedPiece, currPlayer.yutResults, boardData);
        canOut = ValidOutResults.Count > 0;
        if (canOut && outZoneHighlight != null) outZoneHighlight.SetActive(true);
    }

    private BoardNode FindBoardNode(BoardNodeData data)
    {
        foreach (var node in allBoardNodes)
            if (node.data == data) return node;
        return null;
    }

    private void ClearHighlights()
    {
        foreach (var node in validDestinations.Keys)
            node.SetHighlight(false);
        validDestinations.Clear();

        if (canOut)
        {
            if (outZoneHighlight != null) outZoneHighlight.SetActive(false);
            canOut = false;
        }
    }
}