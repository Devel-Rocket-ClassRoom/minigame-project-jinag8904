public static class StatsService
{
    private static IStatsRepository _repo;

    public static IStatsRepository Repo => _repo ??= new LocalStatsRepository(); // 미설정 시 로컬 저장소

    public static void Use(IStatsRepository repo) => _repo = repo; // 사용할 저장소 주입 (InitAsync에서)
}