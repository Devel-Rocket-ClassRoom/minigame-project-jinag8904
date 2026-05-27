using UnityEditor;
using UnityEngine;

public class BlueOrcMaterialConverter
{
    [MenuItem("Tools/Convert BlueOrc Materials to URP")]
    static void Convert()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Imported/WiseAlienGames/BlueOrcs/Materials" });
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");

        if (urpLit == null)
        {
            Debug.LogError("URP/Lit 셰이더를 찾을 수 없습니다. URP 패키지가 설치돼 있는지 확인하세요.");
            return;
        }

        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            Texture albedo   = mat.HasProperty("_MainTex")          ? mat.GetTexture("_MainTex")          : null;
            Texture normal   = mat.HasProperty("_BumpMap")           ? mat.GetTexture("_BumpMap")           : null;
            Texture metallic = mat.HasProperty("_MetallicGlossMap")  ? mat.GetTexture("_MetallicGlossMap")  : null;
            Texture occlusion= mat.HasProperty("_OcclusionMap")      ? mat.GetTexture("_OcclusionMap")      : null;
            Texture height   = mat.HasProperty("_ParallaxMap")       ? mat.GetTexture("_ParallaxMap")       : null;
            Color   color    = mat.HasProperty("_Color")             ? mat.GetColor("_Color")               : Color.white;

            mat.shader = urpLit;

            if (albedo)    mat.SetTexture("_BaseMap",         albedo);
            if (normal)  { mat.SetTexture("_BumpMap",         normal);   mat.EnableKeyword("_NORMALMAP"); }
            if (metallic){ mat.SetTexture("_MetallicGlossMap",metallic); mat.EnableKeyword("_METALLICSPECGLOSSMAP"); }
            if (occlusion) mat.SetTexture("_OcclusionMap",    occlusion);
            if (height)    mat.SetTexture("_ParallaxMap",     height);
            mat.SetColor("_BaseColor", color);

            EditorUtility.SetDirty(mat);
            count++;
            Debug.Log($"변환 완료: {mat.name}");
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"총 {count}개 머티리얼 변환 완료.");
    }
}
