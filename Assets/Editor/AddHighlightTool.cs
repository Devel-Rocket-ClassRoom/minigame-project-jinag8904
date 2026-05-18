using UnityEngine;
using UnityEditor;

public class AddHighlightTool
{
    [MenuItem("Tools/Add Highlight to All BoardNodes")]
    static void Execute()
    {
        const string prefabPath = "Assets/Prefabs/highLight.prefab";
        var highlightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (highlightPrefab == null)
        {
            Debug.LogError($"Prefab not found: {prefabPath}");
            return;
        }

        var nodes = Object.FindObjectsByType<BoardNode>(FindObjectsSortMode.None);
        int added = 0;

        foreach (var node in nodes)
        {
            if (node.highlight != null) continue;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(highlightPrefab, node.transform);
            instance.SetActive(false);
            node.highlight = instance;
            EditorUtility.SetDirty(node);
            added++;
        }

        Debug.Log($"Highlight 추가 완료: {added}개 노드");
    }
}
