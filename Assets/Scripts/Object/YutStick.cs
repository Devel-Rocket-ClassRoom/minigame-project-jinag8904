using UnityEngine;

public class YutStick : MonoBehaviour
{
    [SerializeField] private float restThreshold = 0.05f;
    [SerializeField] private float fallGravityMultiplier = 2.0f;

    private Rigidbody rb;
    private int _settleFrames;
    private const int SettleFrameThreshold = 8;

    public event System.Action Landed;  // 바닥에 처음 닿은 순간 (효과음용)
    private bool _hasLanded;

    public bool IsTail => Vector3.Dot(transform.up, Vector3.up) < 0f;

    public bool IsAtRest => rb != null
        && rb.linearVelocity.sqrMagnitude < restThreshold
        && rb.angularVelocity.sqrMagnitude < restThreshold;

    public bool IsAlmostAtRest => rb != null
        && (rb.IsSleeping()
        || (rb.linearVelocity.sqrMagnitude < 0.25f
            && rb.angularVelocity.sqrMagnitude < 0.25f));

    private void Awake() => rb = GetComponent<Rigidbody>();

    private void OnCollisionEnter(Collision collision)
    {
        if (_hasLanded) return;
        _hasLanded = true;
        Landed?.Invoke();
    }

    private void FixedUpdate()
    {
        if (rb == null || rb.isKinematic || rb.IsSleeping()) return;

        // 올라갈 때도 같은 배수 적용해 전체 체공 시간 단축
        // |velocity.y| < 0.3f 구간 제외해 바닥 안착 후 진동 방지
        if (Mathf.Abs(rb.linearVelocity.y) > 0.3f)
            rb.AddForce(Physics.gravity * (fallGravityMultiplier - 1f), ForceMode.Acceleration);

        // 옆으로 서는 것 방지: 앞/뒤 방향으로 쓰러지도록 교정 토크 적용
        float upDot = Vector3.Dot(transform.up, Vector3.up);
        if (Mathf.Abs(upDot) < 0.4f)
        {
            Vector3 targetUp = upDot >= 0f ? Vector3.up : Vector3.down;
            rb.AddTorque(Vector3.Cross(transform.up, targetUp) * 5f, ForceMode.Acceleration);
        }

        // 착지 후 수평 상태에서 미세 흔들림 제거
        bool isSettling = rb.linearVelocity.sqrMagnitude < 0.05f;
        if (isSettling && Mathf.Abs(upDot) > 0.7f)
        {
            _settleFrames++;
            if (_settleFrames >= SettleFrameThreshold)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.angularDamping = 50f;
                rb.Sleep();
            }
        }
        else
        {
            _settleFrames = 0;
            if (!isSettling) rb.angularDamping = 12f;
        }
    }
}
