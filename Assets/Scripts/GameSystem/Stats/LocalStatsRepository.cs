using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class LocalStatsRepository : IStatsRepository
{
    private const string Key = "match_records";

    public UniTask RecordMatchAsync(MatchRecord record)
    {
        var list = Load();
        list.Add(record);
        Save(list);
        return UniTask.CompletedTask;
    }

    public UniTask<List<MatchRecord>> LoadMatchesAsync()
    {
        return UniTask.FromResult(Load());
    }

    private List<MatchRecord> Load()
    {
        string json = PlayerPrefs.GetString(Key, "");
        if (string.IsNullOrEmpty(json)) return new List<MatchRecord>();
        var wrapper = JsonUtility.FromJson<MatchRecordList>(json);
        return wrapper?.items ?? new List<MatchRecord>();
    }

    private void Save(List<MatchRecord> list)
    {
        var wrapper = new MatchRecordList { items = list };
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(wrapper));
        PlayerPrefs.Save();
    }

    // JsonUtility는 top-level List를 직렬화 못 함 → 래퍼로 감싼다
    [Serializable]
    private class MatchRecordList
    {
        public List<MatchRecord> items;
    }
}