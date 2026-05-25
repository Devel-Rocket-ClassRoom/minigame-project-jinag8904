using UnityEngine;

public class YutStick : MonoBehaviour
{
    [SerializeField] private float restThreshold = 0.05f;
    [SerializeField] private float fallGravityMultiplier = 2.0f;

    private Rigidbody rb;

    public bool IsTail => Vector3.Dot(transform.up, Vector3.up) < 0f;

    public bool IsAtRest => rb != null
        && rb.linearVelocity.sqrMagnitude < restThreshold
        && rb.angularVelocity.sqrMagnitude < restThreshold;

    private void Awake() => rb = GetComponent<Rigidbody>();

    private void FixedUpdate()
    {
        // 올라갈 때도 같은 배수 적용해 전체 체공 시간 단축
        // |velocity.y| < 0.3f 구간 제외해 바닥 안착 후 진동 방지
        if (rb != null && !rb.IsSleeping() && Mathf.Abs(rb.linearVelocity.y) > 0.3f)
            rb.AddForce(Physics.gravity * (fallGravityMultiplier - 1f), ForceMode.Acceleration);
    }
}
