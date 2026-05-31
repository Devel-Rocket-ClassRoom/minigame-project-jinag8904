using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class BoardCircleLayout
{
    const string PrefabPath = "Assets/Prefabs/Board.prefab";
    const float ROuter = 5f;
    const float ROuterInner = 10f / 3f;
    const float RInnerInner = 5f / 3f;

    [MenuItem("Tools/Board/Apply Circle Layout")]
    public static void Apply()
    {
        var positions = BuildPositions();
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            int applied = 0;
            foreach (Transform child in root.transform)
            {
                if (positions.TryGetValue(child.name, out var pos))
                {
                    child.localPosition = pos;
                    applied++;
                }
            }
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log($"[BoardCircleLayout] Applied positions to {applied} nodes in {PrefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    [MenuItem("Tools/Board/Replace Plane With Cylinder Disk")]
    public static void ReplacePlaneWithCylinder()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var plane = root.transform.Find("Plane");
            if (plane == null)
            {
                Debug.LogError($"[BoardCircleLayout] 'Plane' child not found in {PrefabPath}");
                return;
            }

            var mf = plane.GetComponent<MeshFilter>();
            if (mf == null)
            {
                Debug.LogError("[BoardCircleLayout] 'Plane' has no MeshFilter");
                return;
            }

            var temp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mf.sharedMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(temp);

            var s = plane.localScale;
            plane.localScale = new Vector3(s.x, 0.1f, s.z);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log($"[BoardCircleLayout] Replaced 'Plane' mesh with built-in Cylinder (scale.y=0.1) in {PrefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    [MenuItem("Tools/Board/Apply Korean Horror Materials")]
    public static void ApplyMaterials()
    {
        var lacquer  = CreateOrUpdateMat("Assets/Materials/Board_LacquerWood.mat", new Color(0.165f, 0.094f, 0.063f, 1f), 0.4f);
        var hanji    = CreateOrUpdateMat("Assets/Materials/Node_Hanji.mat",       new Color(0.788f, 0.714f, 0.541f, 1f), 0.1f);
        var junction = CreateOrUpdateMat("Assets/Materials/Node_Junction.mat",    new Color(0.478f, 0.102f, 0.102f, 1f), 0.2f);
        var start    = CreateOrUpdateMat("Assets/Materials/Node_Start.mat",       new Color(0.910f, 0.784f, 0.290f, 1f), 0.3f);

        var junctionNames = new HashSet<string> { "Kan5", "Kan10", "Kan15", "Kan24" };
        var startNames    = new HashSet<string> { "Kan0" };

        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            int assigned = 0;
            foreach (Transform child in root.transform)
            {
                var mr = child.GetComponent<MeshRenderer>();
                if (mr == null) continue;

                if (child.name == "Plane")
                {
                    mr.sharedMaterial = lacquer;
                    assigned++;
                }
                else if (child.name.StartsWith("Kan"))
                {
                    if (junctionNames.Contains(child.name)) mr.sharedMaterial = junction;
                    else if (startNames.Contains(child.name)) mr.sharedMaterial = start;
                    else mr.sharedMaterial = hanji;
                    assigned++;
                }
            }
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log($"[BoardCircleLayout] Applied materials to {assigned} renderers in {PrefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Board/Apply Room Materials")]
    public static void ApplyRoomMaterials()
    {
        var floorMat   = CreateOrUpdateMat("Assets/Materials/Room_Floor.mat",        new Color(0.122f, 0.086f, 0.063f, 1f), 0.2f);
        var wallMat    = CreateOrUpdateMat("Assets/Materials/Room_Wall.mat",         new Color(0.059f, 0.035f, 0.031f, 1f), 0.1f);
        var ceilingMat = CreateOrUpdateMat("Assets/Materials/Room_Ceiling.mat",      new Color(0.031f, 0.020f, 0.016f, 1f), 0.1f);
        var tableMat   = CreateOrUpdateMat("Assets/Materials/Table_Lacquer.mat",     new Color(0.227f, 0.055f, 0.039f, 1f), 0.5f);
        var trayMat    = CreateOrUpdateMat("Assets/Materials/Tray_Wood.mat",         new Color(0.227f, 0.157f, 0.125f, 1f), 0.15f);
        var fabricMat  = CreateOrUpdateMat("Assets/Materials/ThrowZone_Fabric.mat",  new Color(0.239f, 0.078f, 0.094f, 1f), 0.05f);

        var assignments = new (string name, Material mat)[]
        {
            ("Floor",          floorMat),
            ("Wwall",          wallMat),
            ("Ewall",          wallMat),
            ("Nwall",          wallMat),
            ("Swall",          wallMat),
            ("Ceiling",        ceilingMat),
            ("Table",          tableMat),
            ("StartPositions", trayMat),
            ("EndPositions",   trayMat),
            ("ThrowFloor",     fabricMat),
        };

        int assigned = 0, missing = 0;
        foreach (var (name, mat) in assignments)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                Debug.LogWarning($"[BoardCircleLayout] Scene object not found: {name}");
                missing++;
                continue;
            }
            var mr = go.GetComponent<MeshRenderer>();
            if (mr == null)
            {
                Debug.LogWarning($"[BoardCircleLayout] '{name}' has no MeshRenderer");
                missing++;
                continue;
            }
            Undo.RecordObject(mr, "Apply Room Material");
            mr.sharedMaterial = mat;
            EditorUtility.SetDirty(mr);
            assigned++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[BoardCircleLayout] Room materials: {assigned} assigned, {missing} missing. Save the scene with Ctrl+S.");
    }

    static Material CreateOrUpdateMat(string path, Color baseColor, float smoothness)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("[BoardCircleLayout] URP Lit shader not found");
                return null;
            }
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", baseColor);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Dictionary<string, Vector3> BuildPositions()
    {
        var d = new Dictionary<string, Vector3>();
        for (int i = 0; i < 20; i++)
        {
            float theta = (270f + i * 18f) * Mathf.Deg2Rad;
            d[$"Kan{i}"] = new Vector3(ROuter * Mathf.Cos(theta), 0f, ROuter * Mathf.Sin(theta));
        }
        d["Kan20"] = new Vector3( ROuterInner, 0f, 0f);
        d["Kan21"] = new Vector3( RInnerInner, 0f, 0f);
        d["Kan22"] = new Vector3(0f, 0f,  ROuterInner);
        d["Kan23"] = new Vector3(0f, 0f,  RInnerInner);
        d["Kan24"] = Vector3.zero;
        d["Kan25"] = new Vector3(-RInnerInner, 0f, 0f);
        d["Kan26"] = new Vector3(-ROuterInner, 0f, 0f);
        d["Kan27"] = new Vector3(0f, 0f, -RInnerInner);
        d["Kan28"] = new Vector3(0f, 0f, -ROuterInner);
        return d;
    }
}
