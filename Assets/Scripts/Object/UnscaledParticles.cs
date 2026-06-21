using UnityEngine;

// 부착 오브젝트와 모든 자식의 ParticleSystem을 timeScale 무시(실시간)로 전환한다.
// 2배속(#91)에서 촛불 연기 등 환경 파티클이 빨라지지 않게 한다.
// EnvironmentVisual 루트에 부착 → 자식의 연기/불꽃 파티클 일괄 적용.
public class UnscaledParticles : MonoBehaviour
{
    private void Awake()
    {
        foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            main.useUnscaledTime = true;
        }
    }
}
