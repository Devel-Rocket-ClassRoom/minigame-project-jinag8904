using System.Collections;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;

public class YutThrowController : MonoBehaviour
{
    [SerializeField] private CinemachineCamera throwZoneCam;
    [SerializeField] private GameObject yutStickPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Vector3 throwForce = new Vector3(0f, 5f, 2f);
    [SerializeField] private float torqueStrength = 8f;
    [SerializeField] private float maxWaitSeconds = 8f;

    public YutResult LastResult { get; private set; }

    private CinemachineBrain _brain;

    private void Awake()
    {
        _brain = Camera.main.GetComponent<CinemachineBrain>();
    }

    private void SetThrowCamActive(bool active)
    {
        throwZoneCam.Priority = new PrioritySettings { Value = active ? 20 : 0 };
    }

    public IEnumerator CoThrow(bool isBlackYut = false)
    {
        SetThrowCamActive(true);
        yield return null; // Cinemachine이 블렌드를 시작할 한 프레임 대기
        if (_brain != null)
            yield return new WaitUntil(() => !_brain.IsBlending);
        yield return new WaitForSeconds(0.15f);

        if (isBlackYut)
        {
            // 검은 윷은 확률 기반 결과 사용, 물리 시뮬레이션 없이 카메라만 전환
            LastResult = ThrowYut.Throw(isBlackYut: true);
            yield return new WaitForSeconds(1.5f);
            SetThrowCamActive(false);
            yield break;
        }

        var sticks = new YutStick[4];
        for (int i = 0; i < 4; i++)
        {
            Vector3 pos = spawnPoints != null && i < spawnPoints.Length
                ? spawnPoints[i].position
                : transform.position + new Vector3(i * 0.2f - 0.3f, 1f, 0f);

            var go = Instantiate(yutStickPrefab, pos, Random.rotation);
            sticks[i] = go.GetComponent<YutStick>();
            var rb = go.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // 수평 이동 없이 위로만, 스핀만 추가
                var force = new Vector3(0f, throwForce.y + Random.Range(-0.5f, 0.5f), 0f);
                rb.AddForce(force, ForceMode.VelocityChange);
                rb.AddTorque(Random.insideUnitSphere * torqueStrength, ForceMode.VelocityChange);
                rb.angularDamping = 5f;  // 착지 후 구름 억제
            }
        }

        yield return new WaitForSeconds(0.5f);

        float elapsed = 0f;
        while (elapsed < maxWaitSeconds)
        {
            yield return new WaitForSeconds(0.2f);
            elapsed += 0.2f;
            if (sticks.All(s => s != null && s.IsAtRest)) break;
        }

        yield return new WaitForSeconds(1f);

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
            if (s != null) Destroy(s.gameObject);

        SetThrowCamActive(false);
    }
}
