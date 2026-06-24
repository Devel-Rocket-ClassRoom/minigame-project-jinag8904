using Cysharp.Threading.Tasks;
using UnityEngine;

public static class StatsService
{
    private static IStatsRepository _repo;

    public static IStatsRepository Repo => _repo ??= new LocalStatsRepository(); // 미설정 시 로컬 저장소

    public static void Use(IStatsRepository repo) => _repo = repo; // 사용할 저장소 주입 (InitAsync에서)

    public static async UniTask InitAsync()
    {
        bool ready = await FirebaseInitializer.Instance.WaitForInitializationAsync();
        if (!ready)
        {
            Debug.LogError("[Stats] Firebase 미초기화 -> 로컬 저장소 유지");
            return;
        }

        if (!AuthManager.Instance.IsLogedIn)
        {
            Debug.LogWarning("[Stats] 비로그인 상태 -> InitAsync 무시");
            return;
        }

        string uid = AuthManager.Instance.UserId;
        var root = FirebaseInitializer.Instance.Database.RootReference;
        Use(new FirebaseStatsRepository(uid, root));
        Debug.Log($"[Stats] Firebase 저장소 사용 (uid = {uid})");
    }
}