using UnityEngine;

public class MouseInput : MonoBehaviour
{
    // 선택
    private Camera cam;
    [SerializeField] private LayerMask pieceLayer;
    [SerializeField] private LayerMask boardNodeLayer;

    private Piece selectedPiece;
    private BoardNode selectedBoardNode;
    private bool needSelection;         // 입력을 받아야 할 상태일 때만

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
    }
}