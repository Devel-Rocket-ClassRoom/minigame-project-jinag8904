using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class OtherworldlyAmbience
{
    const string ProfilePath = "Assets/Settings/SampleSceneProfile.asset";

    [MenuItem("Tools/Scene/Apply Otherworldly Lighting")]
    public static void ApplyLighting()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogError("[OtherworldlyAmbience] No active scene.");
            return;
        }

        RenderSettings.fog = false;
        RenderSettings.fogColor = new Color(0.04f, 0.05f, 0.08f, 1f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.012f;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.22f, 0.20f, 0.30f, 1f);
        RenderSettings.ambientIntensity = 1f;

        EnsureDirectionalLight();
        EnsureCenterCandleLight();
        EnsureCornerCandleLights();

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[OtherworldlyAmbience] Lighting applied to '{scene.name}'. Save scene (Ctrl+S) to persist.");
    }

    const string DirectionalLightName = "Directional Light";

    static void EnsureDirectionalLight()
    {
        var existing = GameObject.Find(DirectionalLightName);
        var go = existing != null ? existing : new GameObject(DirectionalLightName);

        go.transform.position = new Vector3(0f, 100f, 0f);
        // 90도(수직 하향)는 수직 벽에 N·L≈0이라 벽이 까맣게 보임. 50도로 기울여 카메라가 보는 벽(+Z)이 빛을 받게 함.
        go.transform.rotation = Quaternion.Euler(50f, 0f, 0f);

        var light = go.GetComponent<Light>();
        if (light == null) light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = Color.white;
        light.intensity = 1f;
        light.shadows = LightShadows.Hard;
        light.shadowStrength = 0.85f;
        light.bounceIntensity = 0f;
    }

    const string CenterLightName = "CandleLight_Center";

    static void EnsureCenterCandleLight()
    {
        var existing = GameObject.Find(CenterLightName);
        var go = existing != null ? existing : new GameObject(CenterLightName);

        go.transform.position = new Vector3(0f, 6f, 0f);
        go.transform.rotation = Quaternion.identity;

        var light = go.GetComponent<Light>();
        if (light == null) light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1.0f, 0.69f, 0.37f, 1f);
        light.intensity = 12f;
        light.range = 16f;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.6f;
        light.bounceIntensity = 0f;
    }

    const string CornerLightPrefix = "CandleLight_Corner_";
    static readonly Vector3[] CornerPositions = new Vector3[]
    {
        new Vector3( 10f, 3f,   0f),
        new Vector3(  0f, 3f,  10f),
        new Vector3(-10f, 3f,   0f),
        new Vector3(  0f, 3f, -10f),
    };

    static void EnsureCornerCandleLights()
    {
        for (int i = 0; i < CornerPositions.Length; i++)
        {
            string name = CornerLightPrefix + i;
            var existing = GameObject.Find(name);
            var go = existing != null ? existing : new GameObject(name);

            go.transform.position = CornerPositions[i];
            go.transform.rotation = Quaternion.identity;

            var light = go.GetComponent<Light>();
            if (light == null) light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1.0f, 0.69f, 0.37f, 1f);
            light.intensity = 5.5f;
            light.range = 9f;
            light.shadows = LightShadows.None;
            light.bounceIntensity = 0f;
        }
    }

    [MenuItem("Tools/Scene/Apply Otherworldly PostProcessing")]
    public static void ApplyPostProcessing()
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null)
        {
            Debug.LogError($"[OtherworldlyAmbience] VolumeProfile not found at {ProfilePath}");
            return;
        }

        if (!profile.TryGet<Bloom>(out var bloom)) bloom = profile.Add<Bloom>(true);
        bloom.active = true;
        bloom.intensity.overrideState = true;
        bloom.intensity.value = 0.5f;
        bloom.threshold.overrideState = true;
        bloom.threshold.value = 0.85f;
        bloom.scatter.overrideState = true;
        bloom.scatter.value = 0.7f;

        if (!profile.TryGet<Vignette>(out var vignette)) vignette = profile.Add<Vignette>(true);
        vignette.active = true;
        vignette.color.overrideState = true;
        vignette.color.value = new Color(0.02f, 0f, 0.04f, 1f);
        vignette.intensity.overrideState = true;
        vignette.intensity.value = 0.22f;
        vignette.smoothness.overrideState = true;
        vignette.smoothness.value = 0.4f;

        if (!profile.TryGet<Tonemapping>(out var tonemap)) tonemap = profile.Add<Tonemapping>(true);
        tonemap.active = true;
        tonemap.mode.overrideState = true;
        tonemap.mode.value = TonemappingMode.ACES;

        if (!profile.TryGet<ColorAdjustments>(out var color)) color = profile.Add<ColorAdjustments>(true);
        color.active = true;
        color.postExposure.overrideState = true;
        color.postExposure.value = 1.0f;   // 노출 +1스톱 — 전체 밝기 끌어올림
        color.contrast.overrideState = true;
        color.contrast.value = 0f;          // 그림자 짓이김 방지
        color.colorFilter.overrideState = true;
        color.colorFilter.value = new Color(1.0f, 0.96f, 0.9f, 1f);
        color.saturation.overrideState = true;
        color.saturation.value = -5f;

        // 그림자는 차가운 청보라, 하이라이트는 촛불 호박색으로 색 분리 (어둡게 만들지 않도록 약하게)
        if (!profile.TryGet<SplitToning>(out var split)) split = profile.Add<SplitToning>(true);
        split.active = true;
        split.shadows.overrideState = true;
        split.shadows.value = new Color(0.35f, 0.42f, 0.6f, 1f);     // 밝은 청보라
        split.highlights.overrideState = true;
        split.highlights.value = new Color(1.0f, 0.69f, 0.376f, 1f);  // #FFB060
        split.balance.overrideState = true;
        split.balance.value = 0f;

        // 오래된 필름 질감으로 공허한 검정 영역 완화
        if (!profile.TryGet<FilmGrain>(out var grain)) grain = profile.Add<FilmGrain>(true);
        grain.active = true;
        grain.type.overrideState = true;
        grain.type.value = FilmGrainLookup.Medium1;
        grain.intensity.overrideState = true;
        grain.intensity.value = 0.15f;
        grain.response.overrideState = true;
        grain.response.value = 0.8f;

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        Debug.Log($"[OtherworldlyAmbience] PostProcessing applied to {ProfilePath}");
    }

    [MenuItem("Tools/Scene/Apply All M2 Ambience")]
    public static void ApplyAll()
    {
        ApplyLighting();
        ApplyPostProcessing();
    }
}
