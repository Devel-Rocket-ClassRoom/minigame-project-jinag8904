using Cysharp.Threading.Tasks;
using UnityEngine;

// 로그인 상태가 되면 패널을 안 열어도 즉시 Firebase 저장소로 전환한다.
// (패널 열 때만 InitAsync 하면, 로그인 직후 첫 읽기가 콜드 커넥션 레이스로 비는 문제 방지)
public class StatsBootstrap : MonoBehaviour
{
    private async UniTaskVoid Start()
    {
        await UniTask.WaitUntil(() => AuthManager.Instance != null && AuthManager.Instance.IsInitialized);

        AuthManager.Instance.LoginStateChanged += OnLoginStateChanged;

        if (AuthManager.Instance.IsLogedIn)
            StatsService.InitAsync().Forget();   // 이미 로그인된 채 시작
    }

    private void OnLoginStateChanged(bool signedIn)
    {
        if (signedIn) StatsService.InitAsync().Forget();
    }

    private void OnDestroy()
    {
        if (AuthManager.Instance != null)
            AuthManager.Instance.LoginStateChanged -= OnLoginStateChanged;
    }
}
