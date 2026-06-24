using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public interface IStatsRepository
{
    UniTask RecordMatchAsync(MatchRecord record);
    UniTask<List<MatchRecord>> LoadMatchesAsync();
}