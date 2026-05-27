using DG.Tweening;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class LotDrawController : MonoBehaviour
{
    [Header("씬 오브젝트")]
    [SerializeField] private CinemachineCamera jebiCam;
    [SerializeField] private Transform stick0Root;
    [SerializeField] private Transform stick1Root;

    [Header("머티리얼")]
    [SerializeField] private Material normalMat;
    [SerializeField] private Material markedMat;

    [Header("연출 파라미터")]
    [SerializeField] private float pullOutDistance  = 2f;
    [SerializeField] private float revealTiltDeg    = 180f;
    [SerializeField] private float holdDuration     = 2f;
    [SerializeField] private float camPullBackDist  = 3f;   // 뽑기 시 카메라 후퇴 거리(Z)
    [SerializeField] private float camPullBackDur   = 0.6f; // 카메라 후퇴 시간

    private CinemachineBrain _brain;
    private Vector3 _camInitPos;
    private bool    _waitingForPick;
    private int     _pickedIndex = -1;
    private Vector3 _stick0InitAngles;
    private Vector3 _stick1InitAngles;

    public bool LastPickedMarked { get; private set; }

    private void Awake()
    {
        _brain = Camera.main.GetComponent<CinemachineBrain>();
        _stick0InitAngles = stick0Root.localEulerAngles;
        _stick1InitAngles = stick1Root.localEulerAngles;
        _camInitPos = jebiCam.transform.position;
    }

    private void SetCamActive(bool active)
    {
        jebiCam.Priority = new PrioritySettings { Value = active ? 20 : 0 };
    }

    private void ApplyMat(Transform root, Material mat)
    {
        foreach (var mr in root.GetComponentsInChildren<MeshRenderer>())
        {
            var mats = mr.materials;
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            mr.materials = mats;
        }
    }

    private void Update()
    {
        if (!_waitingForPick || !Input.GetMouseButtonDown(0)) return;

        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out var hit, 100f)) return;

        var t = hit.transform;
        if (t == stick0Root || t.IsChildOf(stick0Root))
            _pickedIndex = 0;
        else if (t == stick1Root || t.IsChildOf(stick1Root))
            _pickedIndex = 1;
    }

    public IEnumerator CoDraw()
    {
        jebiCam.transform.position = _camInitPos;

        // 시작 시 랜덤 배정 — 뒷면이 카메라에 안 보이므로 플레이어는 구분 불가
        bool stick0IsMarked = Random.value < 0.5f;
        ApplyMat(stick0Root, stick0IsMarked ? markedMat : normalMat);
        ApplyMat(stick1Root, stick0IsMarked ? normalMat : markedMat);

        SetCamActive(true);
        yield return null;
        if (_brain != null) yield return new WaitUntil(() => !_brain.IsBlending);

        _pickedIndex = -1;
        _waitingForPick = true;

        yield return new WaitUntil(() => _pickedIndex >= 0);
        _waitingForPick = false;

        bool pickedIsMarked = (_pickedIndex == 0) == stick0IsMarked;
        LastPickedMarked = pickedIsMarked;

        var picked     = _pickedIndex == 0 ? stick0Root : stick1Root;
        var other      = _pickedIndex == 0 ? stick1Root : stick0Root;
        var initAngles = _pickedIndex == 0 ? _stick0InitAngles : _stick1InitAngles;

        // 1단계: 카메라 뒤로
        var camPulledPos = _camInitPos + new Vector3(0, 0, camPullBackDist);
        yield return jebiCam.transform.DOMove(camPulledPos, camPullBackDur)
            .SetEase(Ease.OutCubic).WaitForCompletion();

        yield return new WaitForSeconds(0.3f);

        // 2단계: 세우기 (기울기 제거)
        var uprightAngles = new Vector3(initAngles.x, initAngles.y, 0f);
        yield return picked.DOLocalRotate(uprightAngles, 0.35f)
            .SetEase(Ease.OutCubic).WaitForCompletion();

        // 3단계: 선택 스틱 위로 + 나머지 스틱 아래로 동시에
        other.DOLocalMoveY(other.localPosition.y - 0.5f, 0.8f).SetEase(Ease.InQuad);
        yield return picked.DOLocalMoveY(picked.localPosition.y + pullOutDistance, 0.8f)
            .SetEase(Ease.OutCubic).WaitForCompletion();

        yield return new WaitForSeconds(0.2f);

        // 3단계: 눕혀서 뒷면 공개
        var revealAngles = new Vector3(uprightAngles.x + revealTiltDeg, uprightAngles.y, 0f);
        yield return picked.DOLocalRotate(revealAngles, 0.7f)
            .SetEase(Ease.OutSine).WaitForCompletion();

        yield return new WaitForSeconds(holdDuration);

        SetCamActive(false);
        yield return new WaitForSeconds(0.5f);
    }
}
