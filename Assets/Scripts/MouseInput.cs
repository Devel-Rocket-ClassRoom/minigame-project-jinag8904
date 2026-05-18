using UnityEngine;
using UnityEngine.InputSystem;

public class MouseInput : MonoBehaviour
{
    private Camera cam;
    private GameInput input = new();

    [SerializeField] private LayerMask pieceLayer;
    [SerializeField] private LayerMask boardNodeLayer;

    // ㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡ

    public bool MoveConfirmed { get; private set; }
    private bool needSelection = false;
    public Piece SelectedPiece { get; private set; }
    public BoardNode selectedBoardNode { get; private set; }

    // ㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡ

    private void Awake()
    {
        cam = Camera.main;
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

        // Raycast 구현부 ↓ (진행 중)
        // 2. 드래그 시작했을 때
        // (유효한 말을 선택했다면)선택한 말 시각화, SelectedPiece에 등록
        // + 해당 말로 갈 수 있는 자리 하이라이트
        Vector2 mousePos = input.GamePlay.Point.ReadValue<Vector2>();
        Ray ray = cam.ScreenPointToRay(mousePos);
        var hits = Physics.RaycastAll(ray, 100f, pieceLayer | boardNodeLayer);
        var pieceHit = new RaycastHit();
        var boardNodeHit = new RaycastHit();

        foreach (var hit in hits)
        {
            if (hit.collider.CompareTag("Piece")) pieceHit = hit;
            else if (hit.collider.CompareTag("BoardNode")) boardNodeHit = hit;
        }

    }

    private void OnClickReleased(InputAction.CallbackContext ctx)
    {
        // 3. 릴리즈 했을 때
        // 놓을 수 있는 자리라면 선택 확정(MoveConfirmed = true), 상태 초기화
        // 아니라면 상태만 초기화
    }

    public void BeginSelection()
    {
        // 1. 선택 시작
        MoveConfirmed = false; 
        needSelection = true;

        // 선택 가능한 말(Finished = false인 애들) 시각화
    }
}