Shader "Custom/Dissolve_Voronoi_URP"
{
    Properties
    {
        // ─── Surface ────────────────────────────────────────────────────
        _MainTex        ("Albedo (RGB)",        2D)           = "white" {}
        _Color          ("Tint Color",          Color)        = (0,0,0,1)

        // ─── Voronoi Dissolve ────────────────────────────────────────────
        _DissolveAmount ("Dissolve Amount",     Range(0,1))   = 0.0
        _DissolveSpeed  ("Dissolve Speed",      Range(0,1))   = 0.05
        _CellScale      ("Cell Scale",          Range(1,50))  = 10.0

        // ─── Edge Glow ───────────────────────────────────────────────────
        _EdgeColor      ("Edge Glow Color",     Color)        = (0,1,0.8,1)
        _EdgeWidth      ("Edge Glow Width",     Range(0,0.3)) = 0.08
        _EdgeIntensity  ("Edge Glow Intensity", Range(0,10))  = 4.0
    }

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
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM

            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #pragma vertex   VertexFunction
            #pragma fragment FragmentFunction

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ─── Textures ────────────────────────────────────────────────
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            // ─── Constant buffer (SRP Batcher compatible) ────────────────
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Color;
                half   _DissolveAmount;
                half   _DissolveSpeed;
                half   _CellScale;
                half4  _EdgeColor;
                half   _EdgeWidth;
                half   _EdgeIntensity;
            CBUFFER_END

            // ────────────────────────────────────────────────────────────
            // Voronoi helpers
            // ────────────────────────────────────────────────────────────

            // Pseudo-random 2D point inside a cell
            float2 RandomCellPoint(float2 cell)
            {
                float2 v = float2(
                    dot(cell, float2(127.1, 311.7)),
                    dot(cell, float2(269.5, 183.3))
                );
                return frac(sin(v) * 43758.5453);
            }

            // Returns distance to the nearest Voronoi feature point.
            // Result sits in roughly [0, 0.75] — 0 at each cell centre,
            // increasing outward. Circular holes grow as threshold rises.
            float VoronoiDistance(float2 uv)
            {
                float2 scaled = uv * _CellScale;
                float2 cell   = floor(scaled);
                float2 local  = frac(scaled);

                float minDist = 8.0;

                // 3x3 neighbour search — required to avoid seam artefacts
                for (int row = -1; row <= 1; row++)
                {
                    for (int col = -1; col <= 1; col++)
                    {
                        float2 neighbor  = float2(col, row);
                        float2 cellPoint = RandomCellPoint(cell + neighbor);
                        float2 delta     = neighbor + cellPoint - local;
                        float  dist      = length(delta);
                        minDist          = min(minDist, dist);
                    }
                }

                return minDist;
            }

            // ─── Structs ──────────────────────────────────────────────────
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

            // ─── Vertex ───────────────────────────────────────────────────
            Varyings VertexFunction(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uvMain      = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            // ─── Fragment ─────────────────────────────────────────────────
            half4 FragmentFunction(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                // Voronoi distance: 0 at cell centres, grows outward
                float voronoi = VoronoiDistance(IN.uvMain);

                // Threshold grows slowly over time, driven by C# or autonomous
                // Multiplier kept tiny (0.005) so default speed feels natural
                half threshold = _DissolveAmount + _Time.y * _DissolveSpeed * 0.005;
                threshold      = saturate(threshold);

                // Discard pixels INSIDE growing circles (near cell centres)
                clip(voronoi - threshold);

                // Glow ring on the surviving edge of each circle
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
            Cull Off

            HLSLPROGRAM

            #pragma multi_compile_instancing
            #pragma vertex   ShadowVertexFunction
            #pragma fragment ShadowFragmentFunction

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Color;
                half   _DissolveAmount;
                half   _DissolveSpeed;
                half   _CellScale;
                half4  _EdgeColor;
                half   _EdgeWidth;
                half   _EdgeIntensity;
            CBUFFER_END

            float2 RandomCellPoint(float2 cell)
            {
                float2 v = float2(
                    dot(cell, float2(127.1, 311.7)),
                    dot(cell, float2(269.5, 183.3))
                );
                return frac(sin(v) * 43758.5453);
            }

            float VoronoiDistance(float2 uv)
            {
                float2 scaled = uv * _CellScale;
                float2 cell   = floor(scaled);
                float2 local  = frac(scaled);
                float  minDist = 8.0;

                for (int row = -1; row <= 1; row++)
                    for (int col = -1; col <= 1; col++)
                    {
                        float2 neighbor  = float2(col, row);
                        float2 cellPoint = RandomCellPoint(cell + neighbor);
                        float2 delta     = neighbor + cellPoint - local;
                        minDist          = min(minDist, length(delta));
                    }

                return minDist;
            }

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
                float voronoi  = VoronoiDistance(IN.uvMain);
                half  threshold = saturate(_DissolveAmount + _Time.y * _DissolveSpeed * 0.005);
                clip(voronoi - threshold);
                return 0;
            }

            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}