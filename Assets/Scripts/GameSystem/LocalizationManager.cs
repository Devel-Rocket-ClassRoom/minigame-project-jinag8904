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
            var parts = SplitCsv(line, 3);
            if (parts.Length <= col) continue;
            table[parts[0].Trim()] = parts[col].Trim().Replace("\\n", "\n");
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

    private static string[] SplitCsv(string line, int maxParts)
    {
        var result = new List<string>();
        int pos = 0;
        while (pos < line.Length)
        {
            if (result.Count == maxParts - 1)
            {
                result.Add(line.Substring(pos));
                break;
            }
            if (line[pos] == '"')
            {
                pos++;
                var sb = new System.Text.StringBuilder();
                while (pos < line.Length)
                {
                    if (line[pos] == '"' && pos + 1 < line.Length && line[pos + 1] == '"')
                    { sb.Append('"'); pos += 2; }
                    else if (line[pos] == '"')
                    { pos++; break; }
                    else sb.Append(line[pos++]);
                }
                result.Add(sb.ToString());
                if (pos < line.Length && line[pos] == ',') pos++;
            }
            else
            {
                int comma = line.IndexOf(',', pos);
                if (comma < 0) { result.Add(line.Substring(pos)); break; }
                result.Add(line.Substring(pos, comma - pos));
                pos = comma + 1;
            }
        }
        return result.ToArray();
    }
}
