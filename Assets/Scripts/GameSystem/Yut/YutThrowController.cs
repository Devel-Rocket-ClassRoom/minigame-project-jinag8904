using DG.Tweening;
using System.Collections;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;

public class YutThrowController : MonoBehaviour
{
    [SerializeField] private CinemachineCamera throwZoneCam;
    [SerializeField] private CinemachineCamera p2ThrowZoneCam;

    [SerializeField] private GameObject yutStickPrefab;
    [SerializeField] private GameObject yutStickMarkedPrefab; // 4번째 윷 (뒷도 판별용 표식 있음)
    [SerializeField] private GameObject blackYutStickPrefab;        // null이면 yutStickPrefab 사용
    [SerializeField] private GameObject blackYutStickMarkedPrefab;  // 검은 윷 4번째 스틱 (뒷도 판별용)
    [SerializeField] private AudioClip blackYutWhooshClip;

    [SerializeField] private Transform[] spawnPoints;

    [SerializeField] private Vector3 throwForce = new Vector3(0f, 5f, 2f);
    [SerializeField] private float torqueStrength = 8f;

    [SerializeField] private float maxWaitSeconds = 8f;

    [SerializeField] private float gravityMultiplier = 2.5f;

    [SerializeField] private float worldScale = 0.18f;   // WorldRoot 축소 배율과 일치

    public YutResult LastResult { get; private set; }

    private bool _useP2Cam;

    private CinemachineCamera ActiveThrowCam => _useP2Cam && p2ThrowZoneCam != null ? p2ThrowZoneCam : throwZoneCam;

    public void SetActivePlayer(int playerId) => _useP2Cam = playerId == 1;

    private CinemachineBrain _brain;

    private void Awake()
    {
        _brain = Camera.main.GetComponent<CinemachineBrain>();
    }

    private IEnumerator ApplyExtraGravity(Rigidbody rb)
    {
        while (rb != null && !rb.isKinematic)
        {
            yield return new WaitForFixedUpdate();
            if (rb != null && !rb.isKinematic && !rb.IsSleeping())
                rb.AddForce(Physics.gravity * (gravityMultiplier - 1f), ForceMode.Acceleration);
        }
    }

    private void SetThrowCamActive(bool active)
    {
        ActiveThrowCam.Priority = new PrioritySettings { Value = active ? 20 : 0 };
    }

    public void ReleaseThrowCam()
    {
        if (ActiveThrowCam != null)
            ActiveThrowCam.Priority = new PrioritySettings { Value = 0 };
    }

    public IEnumerator CoThrow(bool isBlackYut = false) // 윷 던지기 코루틴
    {
        if (ThrowYut.ForcedResults.Count > 0)   // 튜토리얼 모드
        {
            LastResult = ThrowYut.ForcedResults.Dequeue();
            yield break;
        }

        // #91 윷 던지기 물리만 2배속 (카메라는 IgnoreTimeScale로 보호). 종료 시 1f로 복원.
        Time.timeScale = GameSettings.SpeedMultiplier;

        SetThrowCamActive(true);    // 던지기 카메라 ON

        yield return null; // Cinemachine이 블렌드를 시작할 한 프레임 대기
        if (_brain != null) yield return new WaitUntil(() => !_brain.IsBlending);

        yield return new WaitForSeconds(0.5f);  // 잠시 대기

        if (isBlackYut)
        {
            LastResult = ThrowYut.Throw(isBlackYut: true);  // <- 던진 결과는 여기서 결정됨

            var bSticks = new YutStick[4];
            var bRbs = new Rigidbody[4];
            bool bLandedSfxPlayed = false;

            for (int i = 0; i < 4; i++)
            {
                Vector3 pos = spawnPoints[i].position;
                var spawnRot = Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), 0f);    // 랜덤으로 회전
                var prefab = (i == 3) ? blackYutStickMarkedPrefab : blackYutStickPrefab;
                var obj = ObjectPool.Instance.Get(prefab, pos, spawnRot);
                obj.transform.localScale = Vector3.one * worldScale * 2;

                bSticks[i] = obj.GetComponent<YutStick>();
                bRbs[i] = obj.GetComponent<Rigidbody>();

                // 첫 윷이 바닥에 닿는 순간 효과음 (4개 중 한 번만)
                if (bSticks[i] != null)
                    bSticks[i].Landed += () =>
                    {
                        if (bLandedSfxPlayed) return;
                        bLandedSfxPlayed = true;
                        GameEvents.InvokeYutLanded(_useP2Cam ? 1 : 0);
                    };

                if (bRbs[i] != null)
                {
                    var force = new Vector3(0f, (throwForce.y + Random.Range(-0.5f, 0.5f) + 1f) * Mathf.Sqrt(worldScale), 0f);
                    bRbs[i].AddForce(force, ForceMode.VelocityChange);

                    var torqueDir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-0.2f, 0.2f)).normalized;
                    bRbs[i].AddTorque(torqueDir * torqueStrength, ForceMode.VelocityChange);

                    bRbs[i].angularDamping = 12f;    // 계속 구르기 방지
                    StartCoroutine(ApplyExtraGravity(bRbs[i]));
                }
            }

            yield return new WaitForSeconds(0.3f);

            float bElapsed = 0f;
            int bConsistentCount = 0;
            while (bElapsed < maxWaitSeconds)
            {
                yield return new WaitForSeconds(0.2f);
                bElapsed += 0.2f;
                if (bSticks.All(s => s != null && s.IsAtRest)) break;

                if (bSticks.All(s => s != null && s.IsAlmostAtRest)) bConsistentCount++;
                else bConsistentCount = 0;
                if (bConsistentCount >= 3) break;
            }

            // 물리 고정
            foreach (var rb in bRbs)
                if (rb != null) rb.isKinematic = true;

            // 목표 회전 계산
            bool[] shouldBeTail = new bool[4];
            if (LastResult == YutResult.BACKDO) shouldBeTail[3] = true;

            var spinStartRots = bSticks.Select(s => s != null ? s.transform.rotation : Quaternion.identity).ToArray();
            var targetRots = new Quaternion[4];
            for (int i = 0; i < 4; i++)
            {
                if (bSticks[i] == null) continue;
                Vector3 fwd = Vector3.ProjectOnPlane(bSticks[i].transform.forward, Vector3.up);
                if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
                else fwd.Normalize();
                targetRots[i] = Quaternion.LookRotation(fwd, shouldBeTail[i] ? Vector3.down : Vector3.up);
            }

            // 공중으로 띄우기
            var startPositions = bSticks.Select(s => s != null ? s.transform.position : Vector3.zero).ToArray();
            float floatHeight = 2f * worldScale;
            float floatDuration = 0.5f;
            {
                var moveUpTasks = new System.Collections.Generic.List<Tween>();
                for (int i = 0; i < 4; i++)
                    if (bSticks[i] != null)
                        moveUpTasks.Add(
                            bSticks[i].transform.DOMoveY(startPositions[i].y + floatHeight, floatDuration)
                                .SetEase(Ease.OutCubic));
                if (moveUpTasks.Count > 0) yield return moveUpTasks[0].WaitForCompletion();
            }

            // 스핀하면서 목표 회전으로 스냅
            SoundManager.Instance?.PlaySFX(blackYutWhooshClip);
            float spinDuration = 1f;
            Tween lastSpinTween = null;
            for (int i = 0; i < 4; i++)
                if (bSticks[i] != null)
                    lastSpinTween = bSticks[i].transform
                        .DORotateQuaternion(targetRots[i], spinDuration)
                        .SetEase(Ease.InOutCubic);
            if (lastSpinTween != null) yield return lastSpinTween.WaitForCompletion();

            // 바닥으로 내려놓기
            {
                var moveDownTasks = new System.Collections.Generic.List<Tween>();
                for (int i = 0; i < 4; i++)
                    if (bSticks[i] != null)
                        moveDownTasks.Add(
                            bSticks[i].transform.DOMoveY(startPositions[i].y, floatDuration)
                                .SetEase(Ease.InCubic));
                if (moveDownTasks.Count > 0) yield return moveDownTasks[0].WaitForCompletion();
            }

            yield return new WaitForSeconds(0.4f);

            foreach (var s in bSticks)
                if (s != null) ObjectPool.Instance.Return(s.gameObject);

            Time.timeScale = 1f;   // #91 던지기 끝 — 배속 복원
            yield break;
        }

        var sticks = new YutStick[4];
        bool landedSfxPlayed = false;
        for (int i = 0; i < 4; i++)
        {
            Vector3 pos = spawnPoints != null && i < spawnPoints.Length
                ? spawnPoints[i].position
                : transform.position + new Vector3(i * 0.2f - 0.3f, 1f, 0f);

            var spawnRot = Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), 0f);
            var prefab = (i == 3 && yutStickMarkedPrefab != null) ? yutStickMarkedPrefab : yutStickPrefab;
            var go = ObjectPool.Instance.Get(prefab, pos, spawnRot);
            go.transform.localScale = Vector3.one * worldScale * 2;

            sticks[i] = go.GetComponent<YutStick>();

            // 첫 윷이 바닥에 닿는 순간 효과음 (4개 중 한 번만)
            if (sticks[i] != null)
                sticks[i].Landed += () =>
                {
                    if (landedSfxPlayed) return;
                    landedSfxPlayed = true;
                    GameEvents.InvokeYutLanded(_useP2Cam ? 1 : 0);
                };

            var rb = go.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // 수평 이동 없이 위로만, 스핀만 추가
                var force = new Vector3(0f, (throwForce.y + Random.Range(-0.5f, 0.5f)) * Mathf.Sqrt(worldScale), 0f);
                rb.AddForce(force, ForceMode.VelocityChange);
                // Y축 토크 제거: 옆으로 서는 현상 방지 (X축 텀블링만 허용)
                var torqueDir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-0.2f, 0.2f)).normalized;
                rb.AddTorque(torqueDir * torqueStrength, ForceMode.VelocityChange);
                rb.angularDamping = 12f;  // 착지 후 구름 억제
                StartCoroutine(ApplyExtraGravity(rb));
            }
        }

        yield return new WaitForSeconds(0.5f);

        float elapsed = 0f;
        int consistentCount = 0;
        while (elapsed < maxWaitSeconds)
        {
            yield return new WaitForSeconds(0.2f);
            elapsed += 0.2f;
            if (sticks.All(s => s != null && s.IsAtRest)) break;

            if (sticks.All(s => s != null && s.IsAlmostAtRest)) consistentCount++;
            else consistentCount = 0;
            if (consistentCount >= 3) break;
        }

        yield return new WaitForSeconds(0.4f);

        int tailCount = 0;
        bool lastStickIsTail = sticks[3] != null && sticks[3].IsTail;
        foreach (var s in sticks)
            if (s != null && s.IsTail) tailCount++;

        LastResult = tailCount switch
        {
            0 => YutResult.Mo,
            1 => lastStickIsTail ? YutResult.BACKDO : YutResult.Do,
            2 => YutResult.Gae,
            3 => YutResult.Geol,
            4 => YutResult.Yut,
            _ => YutResult.Mo
        };

        foreach (var s in sticks)
            if (s != null) ObjectPool.Instance.Return(s.gameObject);

        Time.timeScale = 1f;   // #91 던지기 끝 — 배속 복원
    }
}