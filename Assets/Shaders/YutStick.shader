Shader "Custom/YutStick"
{
    Properties
    {
        _FlatColor   ("Flat Face / Back (뒷면)",   Color) = (0.85, 0.75, 0.55, 1)
        _RoundColor  ("Round Face / Front (앞면)", Color) = (0.28, 0.16, 0.07, 1)
        _SideColor   ("Side Color",                Color) = (0.52, 0.33, 0.15, 1)
        _Smoothness  ("Smoothness",  Range(0,1))          = 0.12
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        // ── Forward Lit ────────────────────────────────────────────────────────
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _FlatColor;
                float4 _RoundColor;
                float4 _SideColor;
                float  _Smoothness;
            CBUFFER_END

            struct Attributes
            {
                float4 posOS  : POSITION;
                float3 normOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 posHCS : SV_POSITION;
                float3 normOS : TEXCOORD0;
                float3 normWS : TEXCOORD1;
                float3 posWS  : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes i)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.posHCS = TransformObjectToHClip(i.posOS.xyz);
                o.normOS = i.normOS;
                o.normWS = TransformObjectToWorldNormal(i.normOS);
                o.posWS  = TransformObjectToWorld(i.posOS.xyz);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // ── 오브젝트 공간 Y 법선으로 면 판별 ──────────────────────────
                // 평평한 면(뒷면): 법선 Y ≈ -1  (로컬 아래쪽 = 바닥)
                // 둥근 면(앞면):  법선 Y ≈ +1  (로컬 위쪽 = 볼록한 등)
                float nY = i.normOS.y;
                float flatW  = saturate((-0.55 - nY) * 6.0);   // nY < -0.55 → 1
                float roundW = saturate(( nY - 0.45) * 6.0);   // nY >  0.45 → 1
                float sideW  = 1.0 - saturate(flatW + roundW);

                float4 albedo = _FlatColor  * flatW
                              + _RoundColor * roundW
                              + _SideColor  * sideW;

                // ── URP 라이팅 ────────────────────────────────────────────────
                float3 nWS = normalize(i.normWS);

                #ifdef _MAIN_LIGHT_SHADOWS
                    float4 shadowCoord = TransformWorldToShadowCoord(i.posWS);
                #else
                    float4 shadowCoord = float4(0,0,0,0);
                #endif

                Light mainLight = GetMainLight(shadowCoord);
                float NdotL = saturate(dot(nWS, mainLight.direction));
                float3 diffuse = mainLight.color * mainLight.shadowAttenuation * NdotL;
                float3 ambient = SampleSH(nWS);

                float3 col = albedo.rgb * (diffuse + ambient);
                return half4(col, 1.0);
            }
            ENDHLSL
        }

        // ── Shadow Caster ──────────────────────────────────────────────────────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _FlatColor; float4 _RoundColor; float4 _SideColor; float _Smoothness;
            CBUFFER_END

            float3 _LightDirection;

            struct Attribs { float4 pos : POSITION; float3 norm : NORMAL; };

            float4 vert(Attribs i) : SV_POSITION
            {
                float3 posWS  = TransformObjectToWorld(i.pos.xyz);
                float3 normWS = TransformObjectToWorldNormal(i.norm);
                return TransformWorldToHClip(ApplyShadowBias(posWS, normWS, _LightDirection));
            }

            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
