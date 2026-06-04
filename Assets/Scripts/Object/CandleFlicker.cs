using UnityEngine;

/// <summary>
/// 촛불 라이트의 intensity/range를 Perlin noise로 미세하게 흔들어
/// 살아있는 촛불 느낌을 만든다. CandleLight 오브젝트에 부착.
/// </summary>
[RequireComponent(typeof(Light))]
public class CandleFlicker : MonoBehaviour
{
    [SerializeField, Range(0f, 0.5f)]
    private float flickerAmount = 0.18f;   // 기준 intensity 대비 흔들림 비율

    [SerializeField]
    private float flickerSpeed = 2.5f;     // 일렁임 속도

    [SerializeField, Range(0f, 0.3f)]
    private float rangeFlickerAmount = 0.08f; // range 흔들림 비율 (벽 빛 가장자리 일렁임)

    private Light targetLight;
    private float baseIntensity;
    private float baseRange;
    private float seed;

    private void Awake()
    {
        targetLight = GetComponent<Light>();
        baseIntensity = targetLight.intensity;
        baseRange = targetLight.range;
        seed = Random.value * 100f; // 인스턴스별 비동기화
    }

    private void Update()
    {
        float t = Time.time * flickerSpeed + seed;
        // PerlinNoise는 0~1 반환 → -1~1로 변환
        float noise = Mathf.PerlinNoise(t, seed) * 2f - 1f;

        targetLight.intensity = baseIntensity * (1f + noise * flickerAmount);
        targetLight.range = baseRange * (1f + noise * rangeFlickerAmount);
    }
}