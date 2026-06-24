using System.Collections.Generic;

public class CharacterStat  // 캐릭터별 전적 데이터
{
    public string character;
    public int wins;
    public int losses;

    public int Total => wins + losses;
    public float WinRate => Total == 0 ? 0f : (float)wins / Total;  // 승률
}

public class MatchStats
{
    public int totalWins;
    public int totalLosses;
    public Dictionary<string, CharacterStat> byCharacter = new();

    public int Total => totalWins + totalLosses;
    public float WinRate => Total == 0 ? 0f : (float)totalWins / Total;

    public static MatchStats From(List<MatchRecord> records)
    {
        var stats = new MatchStats();

        foreach (var r in records)
        {
            if (r.won) stats.totalWins++;
            else stats.totalLosses++;

            // 캐릭터별 집계 (없으면 새로 만들고, 있으면 그걸 씀)
            if (!stats.byCharacter.TryGetValue(r.character, out var cs))
            {
                cs = new CharacterStat { character = r.character };
                stats.byCharacter[r.character] = cs;
            }
            if (r.won) cs.wins++;
            else cs.losses++;
        }

        return stats;
    }
}