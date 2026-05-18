Shader "Custom/Hair_Dissolve_URP"
{
    Properties
    {
        // ─── Surface ─────────────────────────────────────────────────────
        _MainTex        ("Albedo (RGB, A=opacity mask)", 2D)      = "white" {}
        _Color          ("Tint Color",                   Color)   = (1,1,1,1)

        // ─── Normal Map ──────────────────────────────────────────────────
        [Toggle(_NORMALMAP)]
        _UseNormalMap   ("Use Normal Map",          Float)        = 0
        [NoScaleOffset]
        _BumpMap        ("Normal Map",              2D)           = "bump" {}
        _BumpScale      ("Normal Strength",         Range(0,2))   = 1.0

        // ─── Specular ────────────────────────────────────────────────────
        _SpecColor      ("Specular Color",          Color)        = (0.3,0.3,0.3,1)
        _Smoothness     ("Smoothness",              Range(0,1))   = 0.5

        // ─── Emission ────────────────────────────────────────────────────
        [Toggle(_EMISSION)]
        _UseEmission    ("Use Emission",            Float)        = 0
        [NoScaleOffset]
        _EmissionMap    ("Emission Map",            2D)           = "black" {}
        [HDR]
        _EmissionColor  ("Emission Color",          Color)        = (0,0,0,1)

        // ─── Alpha Cutoff ─────────────────────────────────────────────────
        // Raise to clip semi-transparent fringe pixels from the hair texture.
        // 0 = keep everything, 1 = discard everything.
        _AlphaCutoff    ("Alpha Cutoff",            Range(0,1))   = 0.1

        // ─── Voronoi Dissolve ─────────────────────────────────────────────
        _DissolveAmount ("Dissolve Amount",         Range(0,1))   = 0.0
        _CellScale      ("Cell Scale",              Range(1,50))  = 10.0
        _DissolveOffset ("Pattern Offset",          Vector)       = (0,0,0,0)

        // ─── Edge Glow ────────────────────────────────────────────────────
        [HDR]
        _EdgeColor      ("Edge Glow Color",         Color)        = (0,1,0.8,1)
        _EdgeWidth      ("Edge Glow Width",         Range(0,0.3)) = 0.08
        _EdgeIntensity  ("Edge Glow Intensity",     Range(0,10))  = 4.0
    }

    HLSLINCLUDE

    #pragma target 3.0
    #pragma multi_compile_instancing

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

    CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        half4  _Color;

        half   _BumpScale;
        half4  _SpecColor;
        half   _Smoothness;

        half4  _EmissionColor;

        half   _AlphaCutoff;
        half   _DissolveAmount;
        half   _CellScale;
        float3 _DissolveOffset;

        half4  _EdgeColor;
        half   _EdgeWidth;
        half   _EdgeIntensity;
    CBUFFER_END

    // ─── Voronoi (verbatim) ───────────────────────────────────────────────

    float2 RandomCellPoint(float2 cell)
    {
        return frac(sin(float2(dot(cell, float2(127.1, 311.7)),
                               dot(cell, float2(269.5, 183.3)))) * 43758.5453);
    }

    float Voronoi2D(float2 scaled)
    {
        float2 cell      = floor(scaled);
        float2 local     = frac(scaled);
        float  minDistSq = 64.0;

        for (int row = -1; row <= 1; row++)
        for (int col = -1; col <= 1; col++)
        {
            float2 neighbor  = float2(col, row);
            float2 cellPoint = RandomCellPoint(cell + neighbor);
            float2 delta     = neighbor + cellPoint - local;
            minDistSq        = min(minDistSq, dot(delta, delta));
        }

        return sqrt(minDistSq);
    }

    float VoronoiDistance(float3 posOS)
    {
        float3 s  = (posOS + _DissolveOffset) * _CellScale;
        float  xy = Voronoi2D(s.xy);
        float  xz = Voronoi2D(s.xz);
        float  yz = Voronoi2D(s.yz);
        return min(xy, min(xz, yz));
    }

    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderType"            = "Transparent"
            "Queue"                 = "Transparent"
            "RenderPipeline"        = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector"       = "True"
        }
        LOD 200

        // ── Forward Lit pass ──────────────────────────────────────────────
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend   SrcAlpha OneMinusSrcAlpha
            ZWrite  Off
            // Cull Off so both sides of hair cards render;
            // VFACE in the fragment shader corrects normals per side
            Cull    Off

            HLSLPROGRAM

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _EMISSION

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog

            #pragma vertex   VertexFunction
            #pragma fragment FragmentFunction

            TEXTURE2D(_MainTex);     SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap);     SAMPLER(sampler_BumpMap);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);

            struct Attributes
            {
                float4 positionOS  : POSITION;
                float3 normalOS    : NORMAL;
                float4 tangentOS   : TANGENT;
                float2 uv          : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uvMain      : TEXCOORD0;
                float3 positionOS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float3 tangentWS   : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float3 positionWS  : TEXCOORD5;
                float  fogFactor   : TEXCOORD6;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings VertexFunction(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrmInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionHCS  = posInputs.positionCS;
                OUT.positionWS   = posInputs.positionWS;
                OUT.positionOS   = IN.positionOS.xyz;
                OUT.uvMain       = TRANSFORM_TEX(IN.uv, _MainTex);

                OUT.normalWS     = nrmInputs.normalWS;
                OUT.tangentWS    = nrmInputs.tangentWS;
                OUT.bitangentWS  = nrmInputs.bitangentWS;

                OUT.fogFactor    = ComputeFogFactor(posInputs.positionCS.z);

                return OUT;
            }

            half4 FragmentFunction(Varyings IN, half facing : VFACE) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                // ── Texture alpha — transparent pixels fully discarded ─────
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvMain) * _Color;
                clip(albedo.a - _AlphaCutoff);

                // ── Dissolve clip ─────────────────────────────────────────
                float voronoi   = VoronoiDistance(IN.positionOS);
                half  threshold = saturate(_DissolveAmount);
                clip(voronoi - threshold);

                // ── Normal — flip for back faces ──────────────────────────
                half  facingSign = facing > 0.0h ? 1.0h : -1.0h;

                half3 normalWS;
                #if defined(_NORMALMAP)
                    half3 normalTS = UnpackNormalScale(
                        SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uvMain),
                        _BumpScale);
                    float3x3 TBN   = float3x3(
                        normalize(IN.tangentWS),
                        normalize(IN.bitangentWS),
                        normalize(IN.normalWS));
                    normalWS = normalize(mul(normalTS, TBN)) * facingSign;
                #else
                    normalWS = normalize(IN.normalWS) * facingSign;
                #endif

                // ── Lighting ──────────────────────────────────────────────
                Light    mainLight    = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                half3    lightDir     = normalize(mainLight.direction);
                half3    viewDir      = normalize(GetWorldSpaceViewDir(IN.positionWS));
                half3    halfDir      = normalize(lightDir + viewDir);

                half     NdotL        = saturate(dot(normalWS, lightDir));
                half3    diffuse      = albedo.rgb * mainLight.color * mainLight.shadowAttenuation * NdotL;

                half     NdotH        = saturate(dot(normalWS, halfDir));
                half     shininess    = exp2(_Smoothness * 10.0 + 1.0);
                half3    specular     = _SpecColor.rgb * mainLight.color
                                       * mainLight.shadowAttenuation
                                       * pow(NdotH, shininess) * _Smoothness;

                half3    ambient      = SampleSH(normalWS) * albedo.rgb;

                half3    addLights    = half3(0, 0, 0);
                #if defined(_ADDITIONAL_LIGHTS)
                    uint lightCount = GetAdditionalLightsCount();
                    for (uint i = 0u; i < lightCount; ++i)
                    {
                        Light light  = GetAdditionalLight(i, IN.positionWS);
                        half  nDotL  = saturate(dot(normalWS, light.direction));
                        addLights   += albedo.rgb * light.color * light.distanceAttenuation
                                       * light.shadowAttenuation * nDotL;
                    }
                #endif

                half3 finalColor = ambient + diffuse + specular + addLights;

                // ── Emission ──────────────────────────────────────────────
                #if defined(_EMISSION)
                    half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, IN.uvMain).rgb
                                     * _EmissionColor.rgb;
                    finalColor    += emission;
                #endif

                // ── Dissolve edge glow ────────────────────────────────────
                half  edgeMask  = 1.0 - saturate((voronoi - threshold) / max(_EdgeWidth, 0.0001));
                half3 edgeGlow  = _EdgeColor.rgb * edgeMask * _EdgeIntensity;
                finalColor     += edgeGlow;

                // ── Fog ───────────────────────────────────────────────────
                finalColor = MixFog(finalColor, IN.fogFactor);

                return half4(finalColor, albedo.a);
            }

            ENDHLSL
        }

        // ── Shadow caster — respects both alpha cutout and dissolve ───────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM

            #pragma vertex   ShadowVertexFunction
            #pragma fragment ShadowFragmentFunction

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionOS  : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            ShadowVaryings ShadowVertexFunction(ShadowAttributes IN)
            {
                ShadowVaryings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 posWS    = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float4 posCS    = TransformWorldToHClip(ApplyShadowBias(posWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                    posCS.z = min(posCS.z, posCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    posCS.z = max(posCS.z, posCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                OUT.positionHCS = posCS;
                OUT.positionOS  = IN.positionOS.xyz;
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 ShadowFragmentFunction(ShadowVaryings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).a;
                clip(alpha - _AlphaCutoff);
                clip(VoronoiDistance(IN.positionOS) - saturate(_DissolveAmount));
                return 0;
            }

            ENDHLSL
        }

        // ── Depth Normals ─────────────────────────────────────────────────
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM

            #pragma shader_feature_local _NORMALMAP
            #pragma vertex   DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);

            struct DNAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DNVaryings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 positionOS  : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            DNVaryings DepthNormalsVertex(DNAttributes IN)
            {
                DNVaryings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionOS  = IN.positionOS.xyz;
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                VertexNormalInputs nrmInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
                OUT.normalWS    = nrmInputs.normalWS;
                return OUT;
            }

            float4 DepthNormalsFragment(DNVaryings IN, half facing : VFACE) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).a;
                clip(alpha - _AlphaCutoff);
                clip(VoronoiDistance(IN.positionOS) - saturate(_DissolveAmount));
                half3 normalWS = normalize(IN.normalWS) * (facing > 0.0h ? 1.0h : -1.0h);
                return float4(PackNormalOctRectEncode(
                    TransformWorldToViewDir(normalWS, true)), 0, 0);
            }

            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
