Shader "Custom/Crystal_Dissolve_URP_Lit"
{
    Properties
    {
        // ─── Surface ──────────────────────────────────────────────────────────
        [MainTexture]
        _MainTex        ("Surface Texture (RGB=detail, A=opacity mask)", 2D) = "white" {}

        [MainColor]
        _CrystalColor   ("Crystal Color",           Color)        = (0.55, 0.85, 1.0, 0.55)

        // ─── Transparency ─────────────────────────────────────────────────────
        _Opacity        ("Base Opacity",             Range(0,1))   = 0.55
        _FresnelPow     ("Fresnel Edge Power",       Range(0.5,8)) = 2.5
        _FresnelBoost   ("Fresnel Edge Boost",       Range(0,2))   = 1.1

        // ─── Specular ─────────────────────────────────────────────────────────
        _SpecColor2     ("Specular Color",           Color)        = (1,1,1,1)
        _Smoothness     ("Smoothness",               Range(0,1))   = 0.88

        // ─── Inner Glow (fake refraction / scatter) ───────────────────────────
        _InnerGlow      ("Inner Glow Intensity",     Range(0,2))   = 0.35
        _InnerGlowColor ("Inner Glow Color",         Color)        = (0.7, 0.95, 1.0, 1.0)

        // ─── Voronoi Dissolve ─────────────────────────────────────────────────
        _DissolveAmount ("Dissolve Amount",          Range(0,1))   = 0.0
        _CellScale      ("Cell Scale",               Range(1,50))  = 10.0
        _DissolveOffset ("Pattern Offset",           Vector)       = (0,0,0,0)

        // ─── Dissolve Edge Glow ───────────────────────────────────────────────
        _EdgeColor      ("Edge Glow Color",          Color)        = (0,1,0.8,1)
        _EdgeWidth      ("Edge Glow Width",          Range(0,0.3)) = 0.08
        _EdgeIntensity  ("Edge Glow Intensity",      Range(0,10))  = 4.0
    }

    // ─── Shared HLSL — declared once, visible to all passes ──────────────────
    HLSLINCLUDE
    #pragma target 3.5                  // Quest 3 GPU capability floor
    #pragma multi_compile_instancing

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    // ─── SRP Batcher-compatible CBUFFER ──────────────────────────────────────
    // Every property must live here — missing entries break the batcher and
    // can silently prevent blend state from applying (opacity stops working).
    CBUFFER_START(UnityPerMaterial)
        float4  _MainTex_ST;
        half4   _CrystalColor;
        half    _Opacity;
        half    _FresnelPow;
        half    _FresnelBoost;
        half4   _SpecColor2;
        half    _Smoothness;
        half    _InnerGlow;
        half4   _InnerGlowColor;
        half    _DissolveAmount;
        half    _CellScale;
        float3  _DissolveOffset;
        half4   _EdgeColor;
        half    _EdgeWidth;
        half    _EdgeIntensity;
    CBUFFER_END

    // ─── Voronoi helpers (unchanged from Dissolve_Voronoi_URP_V2) ────────────

    // sin() hash — chaotic distribution avoids grid-alignment artefacts
    float2 RandomCellPoint(float2 cell)
    {
        return frac(sin(float2(dot(cell, float2(127.1,  311.7)),
                               dot(cell, float2(269.5,  183.3)))) * 43758.5453);
    }

    // 2D Voronoi slice — 9-iteration neighbourhood search
    // Used three times (xy/xz/yz) instead of a full 3D loop (27 iterations)
    float Voronoi2D(float2 scaled)
    {
        float2 cell      = floor(scaled);
        float2 local     = frac(scaled);
        float  minDistSq = 64.0;

        for (int row = -1; row <= 1; row++)
        for (int col = -1; col <= 1; col++)
        {
            float2 neighbor   = float2(col, row);
            float2 cellPoint  = RandomCellPoint(cell + neighbor);
            float2 delta      = neighbor + cellPoint - local;
            minDistSq = min(minDistSq, dot(delta, delta));
        }
        return sqrt(minDistSq);
    }

    // Object-space evaluation — seam-free on any mesh regardless of UV layout
    float VoronoiDistance(float3 posOS)
    {
        float3 s  = (posOS + _DissolveOffset) * _CellScale;
        float  xy = Voronoi2D(s.xy);
        float  xz = Voronoi2D(s.xz);
        float  yz = Voronoi2D(s.yz);
        return min(xy, min(xz, yz));
    }

    // ─── Fresnel (Schlick approx) ─────────────────────────────────────────────
    // 0 = face-on, 1 = grazing — 1 dot + 1 pow, no branching
    inline half FresnelTerm(half3 normalWS, half3 viewDirWS, half power)
    {
        half NdotV = saturate(dot(normalize(normalWS), normalize(viewDirWS)));
        return pow(1.0h - NdotV, power);
    }

    // ─── Blinn-Phong specular ─────────────────────────────────────────────────
    // ~3 ALU vs GGX ~15 — indistinguishable at Quest 3 resolution/FOV
    inline half3 BlinnPhongSpec(half3 normalWS, half3 viewDirWS,
                                 half3 lightDir, half3 specCol, half smoothness)
    {
        half3 H      = normalize(lightDir + normalize(viewDirWS));
        half  NdotH  = saturate(dot(normalize(normalWS), H));
        half  power  = exp2(smoothness * 10.0h + 1.0h);    // remap → [2, 1024]
        return specCol * pow(NdotH, power);
    }

    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderPipeline"  = "UniversalPipeline"
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent"
            "IgnoreProjector" = "True"
        }
        LOD 100

        // ── Forward pass ──────────────────────────────────────────────────────
        Pass
        {
            Name "CrystalDissolveForward"
            Tags { "LightMode" = "UniversalForward" }

            // Hardcoded blend — avoids SRP Batcher CBUFFER mismatch that breaks
            // opacity when _SrcBlend/_DstBlend are properties but not in CBUFFER
            Blend   SrcAlpha OneMinusSrcAlpha
            ZWrite  Off
            Cull    Off     // both faces — works on hollow crystal meshes

            HLSLPROGRAM
            #pragma shader_feature_local _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma shader_feature_local _ _SHADOWS_SOFT

            // Strip variants unused by crystals — smaller Quest shader cache
            #pragma skip_variants LIGHTMAP_ON DYNAMICLIGHTMAP_ON DIRLIGHTMAP_COMBINED
            #pragma skip_variants _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            // Fog omitted — breaks stereo depth cues in VR, wastes 3 variants
            // Drive distance fading via _Opacity from C# if needed instead

            #pragma vertex   CrystalDissolveVert
            #pragma fragment CrystalDissolveFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionOS  : TEXCOORD1;     // object space for Voronoi
                float3 normalWS    : TEXCOORD2;
                float3 viewDirWS   : TEXCOORD3;
                float3 positionWS  : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings CrystalDissolveVert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrmInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.positionOS  = IN.positionOS.xyz;    // raw OS — Voronoi stays seam-free
                OUT.normalWS    = nrmInputs.normalWS;
                OUT.viewDirWS   = GetWorldSpaceViewDir(posInputs.positionWS);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);

                return OUT;
            }

            half4 CrystalDissolveFrag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                // ── 1. Voronoi dissolve ───────────────────────────────────────
                // clip() discards dissolved fragments before any lighting runs —
                // dissolve progress actually makes the shader cheaper over time
                float voronoi    = VoronoiDistance(IN.positionOS);
                half  threshold  = saturate(_DissolveAmount);
                clip(voronoi - threshold);

                // ── 2. Dissolve edge glow ─────────────────────────────────────
                // Mask is 1 right at the clip boundary, 0 further inside
                half  edgeMask = 1.0h - saturate((voronoi - threshold) / max(_EdgeWidth, 0.0001h));
                half3 edgeGlow = _EdgeColor.rgb * edgeMask * _EdgeIntensity;

                // ── 3. Surface texture ────────────────────────────────────────
                half4 texSample   = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half  surfaceGrey = dot(texSample.rgb, half3(0.299h, 0.587h, 0.114h));
                half  texAlpha    = texSample.a;

                // ── 4. Fresnel ────────────────────────────────────────────────
                half fresnel = FresnelTerm(IN.normalWS, IN.viewDirWS, _FresnelPow);

                // ── 5. Final alpha ────────────────────────────────────────────
                // _Opacity gates everything — slider always has full range.
                // edgeMask adds extra opacity at the dissolve boundary so the
                // glowing rim stays visible even on a very transparent crystal.
                half finalAlpha = saturate(
                    _Opacity * texAlpha
                    + fresnel * _FresnelBoost * _Opacity
                    + edgeMask * 0.5h          // dissolve edge stays readable
                );

                // ── 6. Main light ─────────────────────────────────────────────
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                    float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                    Light  mainLight   = GetMainLight(shadowCoord);
                #else
                    Light mainLight = GetMainLight();
                #endif

                half3 lightDir   = normalize(mainLight.direction);
                half3 lightColor = mainLight.color * mainLight.distanceAttenuation;
                half  NdotL      = saturate(dot(normalize(IN.normalWS), lightDir));

                // ── 7. Diffuse ────────────────────────────────────────────────
                half3 diffuse = _CrystalColor.rgb
                              * lerp(0.6h, 1.0h, surfaceGrey)   // scratch darkening
                              * NdotL
                              * lightColor;

                // ── 8. Specular ───────────────────────────────────────────────
                half3 spec = BlinnPhongSpec(IN.normalWS, IN.viewDirWS,
                                            lightDir, _SpecColor2.rgb, _Smoothness)
                           * lightColor;

                // ── 9. Inner glow (fake refraction / scatter) ─────────────────
                half  glowFactor = (1.0h - fresnel) * _InnerGlow;
                half3 glow       = _InnerGlowColor.rgb * _CrystalColor.rgb * glowFactor;

                // ── 10. Ambient (baked SH) ────────────────────────────────────
                half3 ambient = SampleSH(normalize(IN.normalWS)) * _CrystalColor.rgb * 0.4h;

                // ── 11. Compose ───────────────────────────────────────────────
                // Edge glow added last — sits on top of crystal lighting,
                // colour-independent so it always reads against the surface
                half3 color = ambient + diffuse + spec + glow + edgeGlow;

                return half4(color, finalAlpha);
            }
            ENDHLSL
        }

        // ── No ShadowCaster pass — intentional ───────────────────────────────
        // Transparent objects casting shadows is expensive and visually wrong
        // for crystal/glass. The dissolve shader's ShadowCaster is only needed
        // on opaque meshes where clip() punches real holes in shadow maps.
        // Crystals don't cast shadows — use baked or decal contact shadows.
    }

    FallBack "Universal Render Pipeline/Unlit"
}
