using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

public class PieceMoveAnimator : MonoBehaviour
{
    [SerializeField] private CinemachineCamera followCam;
    [SerializeField] private CinemachineCamera p2FollowCam;
    [SerializeField] private float cameraBlendTime = 0.85f;
    [SerializeField] private float preBlendWait  = 0.05f;
    [SerializeField] private float postBlendWait = 0.10f;
    [SerializeField] private float hopHeight     = 0.8f;
    [SerializeField] private float hopDuration   = 0.7f;
    [SerializeField] private float settleWait    = 0.35f;
    [SerializeField] private float groundY       = 0.1f;

    private CinemachineBrain _brain;
    private bool _useP2Cam;
    private CinemachineCamera ActiveFollowCam => _useP2Cam && p2FollowCam != null ? p2FollowCam : followCam;

    // 지금 활성 카메라가 보드캠(탑뷰)인지 — 스택 배지 표시 게이팅용
    public bool IsBoardCamActive =>
        _brain != null &&
        (ReferenceEquals(_brain.ActiveVirtualCamera, followCam) ||
         ReferenceEquals(_brain.ActiveVirtualCamera, p2FollowCam));

    public void SetActivePlayer(int playerId) => _useP2Cam = playerId == 1;

    // #91 2배속: 말 이동/정착 속도에만 적용 (튜토리얼은 항상 1배속). timeScale은 건드리지 않음.
    private float Speed => TutorialManager.isTutorial ? 1f : GameSettings.SpeedMultiplier;

    private void Awake()
    {
        _brain = Camera.main.GetComponent<CinemachineBrain>();
    }

    public IEnumerator CoActivateBoardCam(int priority = 20)
    {
        var cam = ActiveFollowCam;
        if (cam == null) yield break;

        cam.Priority = new PrioritySettings { Value = priority };

        // 카메라 블렌드는 2배속 영향에서 제외(브레인 IgnoreTimeScale) → 대기도 실시간으로 맞춤 (#91)
        yield return new WaitForSecondsRealtime(cameraBlendTime);
        if (_brain != null)
            yield return new WaitUntil(() => !_brain.IsBlending);

        yield return new WaitForSecondsRealtime(preBlendWait);
    }

    public IEnumerator CoReleaseFollowCamera()
    {
        var cam = ActiveFollowCam;
        if (cam == null) yield break;

        yield return new WaitForSecondsRealtime(postBlendWait);  // 카메라 블렌드 대기 — 실시간 (#91)

        cam.Priority = new PrioritySettings { Value = 0 };

        yield return null;
        if (_brain != null)
            yield return new WaitUntil(() => !_brain.IsBlending);
    }

    public IEnumerator CoAnimatePieceMove(Piece piece, List<Piece> stackAll, List<BoardNode> pushPathNodes, BoardNode targetNode)
    {
        var hops = BuildHopList(pushPathNodes, targetNode);
        var leaderTf = piece.pieceObject.transform;

        const float stackH = 0.05f;

        foreach (var node in hops)
        {
            var raw = node.transform.position;
            var destPos = new Vector3(raw.x, groundY, raw.z);

            var t = leaderTf.DOJump(destPos, hopHeight, 1, hopDuration / Speed);
            t.OnUpdate(() =>
            {
                for (int i = 0; i < stackAll.Count; i++)
                    stackAll[i].pieceObject.transform.position = leaderTf.position + Vector3.up * stackH * (i + 1);
            });

            yield return t.WaitForCompletion();
        }

        yield return new WaitForSeconds(settleWait / Speed);
    }

    public IEnumerator CoAnimatePieceToPositions(List<Piece> pieces, List<Vector3> destPositions)
    {
        Tween first = null;
        for (int i = 0; i < pieces.Count; i++)
        {
            var tw = pieces[i].pieceObject.transform.DOJump(destPositions[i], hopHeight, 1, hopDuration / Speed);
            if (i == 0) first = tw;
        }
        if (first != null) yield return first.WaitForCompletion();
        yield return new WaitForSeconds(settleWait / Speed);
    }

    private static List<BoardNode> BuildHopList(List<BoardNode> pushPathNodes, BoardNode targetNode)
    {
        if (pushPathNodes == null || pushPathNodes.Count == 0)
            return new List<BoardNode> { targetNode };

        var list = new List<BoardNode>(pushPathNodes);
        if (list[^1] != targetNode) list.Add(targetNode);
        return list;
    }
}
