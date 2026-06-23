using Cysharp.Threading.Tasks;
using Firebase.Database;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class FirebaseStatsRepository : IStatsRepository
{
    private readonly DatabaseReference _matchesRef; // users/{uid}/matches

    public FirebaseStatsRepository(string uid, DatabaseReference root)
    {
        _matchesRef = root.Child("users").Child(uid).Child("matches");
    }

    public async UniTask RecordMatchAsync(MatchRecord record)
    {
        var data = new Dictionary<string, object>
        {
            ["won"] = record.won,
            ["character"] = record.character
        };
        await _matchesRef.Push().SetValueAsync(data).AsUniTask();
    }

    public async UniTask<List<MatchRecord>> LoadMatchesAsync()
    {
        DataSnapshot snapshot = await _matchesRef.GetValueAsync().AsUniTask();

        var list = new List<MatchRecord>();
        foreach (DataSnapshot child in snapshot.Children)
        {
            bool.TryParse(child.Child("won").Value?.ToString(), out bool won);
            string character = child.Child("character").Value?.ToString() ?? "";
            list.Add(new MatchRecord(won, character));
        }
        return list;
    }
}
