using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum Language { Korean, English }

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }
    public static Language CurrentLanguage { get; private set; } = Language.Korean;
    public static event System.Action OnLanguageChanged;

    private static readonly Dictionary<string, string> table = new();
    private static bool tableLoaded;
    private const string PrefKey = "Language";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        var saved = PlayerPrefs.GetString(PrefKey, nameof(Language.Korean));
        CurrentLanguage = System.Enum.TryParse<Language>(saved, out var lang) ? lang : Language.Korean;
        LoadTable();
    }

    public static void SetLanguage(Language lang)
    {
        if (CurrentLanguage == lang) return;
        CurrentLanguage = lang;
        PlayerPrefs.SetString(PrefKey, lang.ToString());
        PlayerPrefs.Save();
        LoadTable();
        OnLanguageChanged?.Invoke();
    }

    private static void LoadTable()
    {
        table.Clear();
        tableLoaded = false;

        var asset = Resources.Load<TextAsset>("Localization/StringTable");
        if (asset == null)
        {
            Debug.LogError("StringTable.csv를 찾을 수 없습니다: Resources/Localization/StringTable");
            return;
        }

        int col = CurrentLanguage == Language.Korean ? 1 : 2;

        using var reader = new StringReader(asset.text);
        reader.ReadLine(); // 헤더 스킵

        string line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split(new[] { ',' }, 3);
            if (parts.Length <= col) continue;
            table[parts[0].Trim()] = parts[col].Trim();
        }

        tableLoaded = true;
    }

    public static string Get(string key)
    {
        if (!tableLoaded) LoadTable();
        if (table.TryGetValue(key, out var val)) return val;
        Debug.LogWarning($"[Loc] 키를 찾을 수 없음: {key}");
        return key;
    }

    public static string Get(string key, params object[] args) => string.Format(Get(key), args);
}
