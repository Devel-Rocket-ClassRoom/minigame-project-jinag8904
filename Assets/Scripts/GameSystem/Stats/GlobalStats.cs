using Cysharp.Threading.Tasks;
using Firebase.Database;
using System.Collections.Generic;
using UnityEngine;

public class GlobalStats 
{
    private static DatabaseReference Node
    {
        get
        {
            var fb = FirebaseInitializer.Instance;
            if (fb == null || !fb.IsReady) return null;
            return fb.Database.RootReference.Child("stats").Child("characters");
        }
    }

    public static async UniTask RecordAsync(string charKey, bool won)
    {
        var node = Node?.Child(charKey);
        if (node == null) return;

        await IncrementChild(node.Child("total"));  // 전체 게임 수 +1
        if (won) await IncrementChild(node.Child("wins"));  // 전체 승리 수 +1
    }

    private static async UniTask IncrementChild(DatabaseReference reference)
    {
        await reference.RunTransaction(data =>
        {
            long current = 0;
            if (data.Value != null) long.TryParse(data.Value.ToString(), out current);
            data.Value = current + 1;
            return TransactionResult.Success(data);
        }).AsUniTask();
    }

    public static async UniTask<Dictionary<string, CharacterStat>> LoadAsync()
    {
        var result = new Dictionary<string, CharacterStat>();
        var node = Node;
        if (node == null) return result;

        DataSnapshot snapshot = await node.GetValueAsync().AsUniTask();
        foreach (DataSnapshot child in snapshot.Children)
        {
            long total = ReadLong(child.Child("total").Value);
            long wins = ReadLong(child.Child("wins").Value);
            result[child.Key] = new CharacterStat
            {
                character = child.Key,
                wins = (int)wins,
                losses = (int)(total - wins)
            };
        }
        return result;
    }

    private static long ReadLong(object value) 
        => value != null && long.TryParse(value.ToString(), out var number) ? number : 0;
}
