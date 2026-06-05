using TMPro;
using UnityEngine;

// 업힌 말 무더기 위에 떠서 "몇 개인지"를 보여주는 숫자 배지.
// 말의 자식이 아니라 독립 오브젝트로 두고(말의 작은/비균등 스케일 영향 제거),
// 매 프레임 말 위 일정 월드 높이로 따라가며 카메라를 향해 빌보드한다.
// 표시 조건: 스택 리더 && 무더기 2개 이상 && 탑뷰(보드캠) 활성.
public class StackCountBadge : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private float worldHeight = 0.5f;        // 말 위로 띄울 월드 높이(스케일 무관)
    [SerializeField] private float referenceDistance = 6.3f;  // 화면 크기 보정 기준 거리(보드캠 높이≈6.84-0.5)
    [SerializeField] private float referenceFov = 40f;        // 보정 기준 FOV(보드캠 FOV)

    private Piece _piece;
    private Transform _follow;
    private int _shownCount = -1;
    private bool _shown;

    public void Bind(Piece piece)
    {
        _piece = piece;
        _follow = (piece != null && piece.pieceObject != null) ? piece.pieceObject.transform : null;
    }

    public void Refresh(bool viewActive)
    {
        // 리더이고 보드 위에 있으며 완주하지 않은 경우에만 무더기 개수 산정
        int count = (_piece != null && _piece.stackLeader == null
                     && !_piece.hasFinished && _piece.currentNode != null)
            ? 1 + _piece.stackedPieces.Count
            : 0;

        _shown = count >= 2 && viewActive;
        if (label.gameObject.activeSelf != _shown)
            label.gameObject.SetActive(_shown);
        if (_shown && _shownCount != count)
        {
            label.text = count.ToString();
            _shownCount = count;
        }
    }

    private void LateUpdate()
    {
        if (!_shown) return;

        var cam = Camera.main;
        if (cam == null) return;

        if (_follow != null)
            transform.position = _follow.position + Vector3.up * worldHeight;

        // 화면 정렬 빌보드: 카메라 평면과 평행 → 위치/각도에 따른 찌그러짐 없음
        transform.rotation = cam.transform.rotation;

        // 원근 카메라에서 거리/줌이 달라도 화면상 크기가 일정하도록 보정.
        // 기준 거리·FOV(보드캠)에서 k=1 → 프리팹에서 정한 크기 그대로 보임.
        float dist = Vector3.Distance(transform.position, cam.transform.position);
        float refTan = Mathf.Tan(referenceFov * 0.5f * Mathf.Deg2Rad);
        float curTan = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        transform.localScale = Vector3.one * (dist * curTan) / (referenceDistance * refTan);
    }
}
