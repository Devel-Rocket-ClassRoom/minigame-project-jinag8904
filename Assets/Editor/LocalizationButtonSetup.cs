using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LocalizationButtonSetup
{
    [MenuItem("Tools/Setup Localization Buttons")]
    static void Setup()
    {
        var csvPath = "Assets/Resources/Localization/StringTable.csv";
        var csvAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(csvPath);
        if (csvAsset == null)
        {
            Debug.LogError($"[LocalizationSetup] CSV 파일을 찾을 수 없습니다: {csvPath}");
            return;
        }

        // CSV에서 KO 텍스트 → 키 역방향 맵 구성
        var koToKey = new Dictionary<string, string>();
        using (var reader = new StringReader(csvAsset.text))
        {
            string line;
            bool first = true;
            while ((line = reader.ReadLine()) != null)
            {
                if (first) { first = false; continue; }
                var parts = line.Split(new[] { ',' }, 3);
                if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[1]))
                    koToKey[parts[1].Trim()] = parts[0].Trim();
            }
        }

        // 씬 내 모든 TextMeshProUGUI 스캔 (비활성 포함)
        var allTMPs = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        int added = 0;
        int skipped = 0;
        int updated = 0;

        var keyField = typeof(LocalizedText).GetField("key", BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var tmp in allTMPs)
        {
            // 씬 오브젝트만 처리 (프리팹 에셋 제외)
            if (!tmp.gameObject.scene.IsValid()) continue;

            var text = tmp.text.Trim();
            var existing = tmp.GetComponent<LocalizedText>();

            if (existing != null)
            {
                var currentKey = keyField?.GetValue(existing) as string;
                if (!string.IsNullOrEmpty(currentKey)) { skipped++; continue; }

                if (!koToKey.TryGetValue(text, out var key)) continue;

                Undo.RecordObject(existing, "Set LocalizedText key");
                keyField?.SetValue(existing, key);
                EditorUtility.SetDirty(existing);

                Debug.Log($"[LocalizationSetup] 키 업데이트: '{tmp.gameObject.name}' \"{text}\" → key={key}");
                updated++;
            }
            else
            {
                if (!koToKey.TryGetValue(text, out var key)) continue;

                var lt = Undo.AddComponent<LocalizedText>(tmp.gameObject);
                keyField?.SetValue(lt, key);
                EditorUtility.SetDirty(lt);

                Debug.Log($"[LocalizationSetup] 컴포넌트 추가: '{tmp.gameObject.name}' \"{text}\" → key={key}");
                added++;
            }
        }

        if (added > 0 || updated > 0)
        {
            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log($"[LocalizationSetup] 완료: {added}개 추가, {updated}개 키 업데이트, {skipped}개 스킵. 씬을 저장하세요 (Ctrl+S).");
        }
        else
        {
            Debug.Log($"[LocalizationSetup] 추가할 항목 없음. (이미 적용 {skipped}개)");
        }
    }
}
