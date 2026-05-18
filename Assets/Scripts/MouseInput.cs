using UnityEngine;

public class MouseInput : MonoBehaviour
{
    private Camera cam;
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

    private void Update()
    {
        if (!needSelection) return; // 선택 필요할 때만 입력 받음

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        var hits = Physics.RaycastAll(ray, 100f, pieceLayer | boardNodeLayer);
        var pieceHit = new RaycastHit();
        var boardNodeHit = new RaycastHit();

        foreach (var hit in hits)
        {
            if (hit.collider.CompareTag("Piece")) pieceHit = hit;
            else if (hit.collider.CompareTag("BoardNode")) boardNodeHit = hit;
        }
        // ↑ 마우스에 닿는 것들 구분해둔 상태

        // 1. 드래그 상태가 아닐 때 (선택 가능한 말 시각화)
        // 2. 드래그 시작했을 때 ((유효한 말을 선택했다면)선택한 말 시각화, SelectedPiece에 등록)
        // 3. 드래그 상태일 때 ((들고 있는 게 있다면)놓을 수 있는 자리 하이라이트)
        // 4. 릴리즈 했을 때 (놓을 수 있는 자리라면 선택 확정, 상태 초기화)
    }

    public void BeginSelection() { MoveConfirmed = false; needSelection = true; }
}