Shader "Custom/Dissolve_Voronoi_URP"
{
    Properties
    {
        // ─── Surface ────────────────────────────────────────────────────
        _MainTex        ("Albedo (RGB)",        2D)           = "white" {}
        _Color          ("Tint Color",          Color)        = (0,0,0,1)

        // ─── Voronoi Dissolve ────────────────────────────────────────────
        _DissolveAmount ("Dissolve Amount",     Range(0,1))   = 0.0
        _CellScale      ("Cell Scale",          Range(1,50))  = 10.0

        // ─── Edge Glow ───────────────────────────────────────────────────
        _EdgeColor      ("Edge Glow Color",     Color)        = (0,1,0.8,1)
        _EdgeWidth      ("Edge Glow Width",     Range(0,0.3)) = 0.08
        _EdgeIntensity  ("Edge Glow Intensity", Range(0,10))  = 4.0
    }

    // ─── Shared HLSL include (avoids duplicating helpers across passes) ───
    HLSLINCLUDE

    #pragma target 3.0                          // Fix #4 — explicit SM target
    #pragma multi_compile_instancing

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    // ─── Constant buffer (SRP Batcher compatible) ────────────────────────
    // Fix #2 — declared once here, shared by both passes
    CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        half4  _Color;
        half   _DissolveAmount;
        half   _CellScale;
        half4  _EdgeColor;
        half   _EdgeWidth;
        half   _EdgeIntensity;
    CBUFFER_END

    // ────────────────────────────────────────────────────────────────────
    // Voronoi helpers
    // ────────────────────────────────────────────────────────────────────

    // Fix #7 — integer hash replaces sin(), much cheaper on mobile GPUs
    float2 RandomCellPoint(float2 cell)
    {
        uint2 u  = (uint2)(int2)cell;
        u        = u * uint2(1664525u, 22695477u) + uint2(1013904223u, 2531011u);
        u       ^= (u >> 16u);
        return float2(u & 0xFFFFu) / 65535.0;
    }

    // Fix #8 — single sqrt at the end instead of nine length() calls
    float VoronoiDistance(float2 uv)
    {
        float2 scaled    = uv * _CellScale;
        float2 cell      = floor(scaled);
        float2 local     = frac(scaled);
        float  minDistSq = 64.0;

        // 3×3 neighbour search — required to avoid seam artefacts
        for (int row = -1; row <= 1; row++)
        {
            for (int col = -1; col <= 1; col++)
            {
                float2 neighbor  = float2(col, row);
                float2 cellPoint = RandomCellPoint(cell + neighbor);
                float2 delta     = neighbor + cellPoint - local;
                minDistSq        = min(minDistSq, dot(delta, delta));
            }
        }

        return sqrt(minDistSq); // one sqrt total instead of nine
    }

    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderType"            = "Opaque"
            "Queue"                 = "Geometry"
            "RenderPipeline"        = "UniversalPipeline"
            "UniversalMaterialType" = "Unlit"
        }
        LOD 100
        Cull Back   // Fix #9 — closed character meshes don't need double-sided

        // ── Forward Lit pass ─────────────────────────────────────────────
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM

            // Fix #5 — shader_feature_local strips unused shadow variants from build
            #pragma shader_feature_local _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma shader_feature_local _ _SHADOWS_SOFT

            #pragma vertex   VertexFunction
            #pragma fragment FragmentFunction

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uvMain      : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings VertexFunction(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uvMain      = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 FragmentFunction(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float voronoi  = VoronoiDistance(IN.uvMain);

                // Fix #1 — single driver: _DissolveAmount only (C# owns timing)
                half threshold = saturate(_DissolveAmount);

                clip(voronoi - threshold);

                half  edgeMask = 1.0 - saturate((voronoi - threshold) / max(_EdgeWidth, 0.0001));
                half3 edgeGlow = _EdgeColor.rgb * edgeMask * _EdgeIntensity;

                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvMain) * _Color;
                return half4(albedo.rgb + edgeGlow, 1.0);
            }

            ENDHLSL
        }

        // ── Shadow caster — holes cut through shadows too ────────────────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back   // Fix #9 — matches forward pass

            HLSLPROGRAM

            #pragma vertex   ShadowVertexFunction
            #pragma fragment ShadowFragmentFunction

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionHCS : SV_POSITION;
                float2 uvMain      : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            ShadowVaryings ShadowVertexFunction(ShadowAttributes IN)
            {
                ShadowVaryings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uvMain      = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 ShadowFragmentFunction(ShadowVaryings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                // Fix #1 — single driver here too
                clip(VoronoiDistance(IN.uvMain) - saturate(_DissolveAmount));
                return 0;
            }

            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
