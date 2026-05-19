Shader "Custom/Crystal_URP"
{
    Properties
    {
        // ─── Surface ─────────────────────────────────────────────────────────
        [MainTexture]
        _MainTex        ("Surface Texture (RGB=detail, A=opacity mask)", 2D) = "white" {}

        [MainColor]
        _CrystalColor   ("Crystal Color", Color)  = (0.55, 0.85, 1.0, 0.55)

        // ─── Transparency ─────────────────────────────────────────────────────
        _Opacity        ("Base Opacity",        Range(0,1))   = 0.55
        _FresnelPow     ("Fresnel Edge Power",  Range(0.5,8)) = 2.5
        _FresnelBoost   ("Fresnel Edge Boost",  Range(0,2))   = 1.1

        // ─── Specular ─────────────────────────────────────────────────────────
        _SpecColor2     ("Specular Color",      Color)        = (1,1,1,1)
        _Smoothness     ("Smoothness",          Range(0,1))   = 0.88

        // ─── Inner Glow (fake refraction/scatter) ─────────────────────────────
        _InnerGlow      ("Inner Glow Intensity",Range(0,2))   = 0.35
        _InnerGlowColor ("Inner Glow Color",    Color)        = (0.7, 0.95, 1.0, 1.0)

        // ─── Render state (hidden) ────────────────────────────────────────────
        // Hardcoded — avoids SRP Batcher CBUFFER mismatch that causes opacity
        // to silently stop working when _SrcBlend/_DstBlend/_ZWrite are properties
        // but not in the CBUFFER. Do not expose these as material properties.
    }

    // ─── Shared HLSL — avoids duplicating helpers across passes ──────────────
    HLSLINCLUDE
    #pragma target 3.5                   // matches Quest 3 GPU capability floor
    #pragma multi_compile_instancing

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    // ─── SRP Batcher-compatible constant buffer ───────────────────────────────
    // Declared once here, visible to every pass
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
    CBUFFER_END

    // ─── Fresnel (Schlick approx) ─────────────────────────────────────────────
    // Returns 0 at face-on, 1 at grazing angle
    // Cheap: 1 dot + 1 pow — no branching, mobile-safe
    inline half FresnelTerm(half3 normalWS, half3 viewDirWS, half power)
    {
        half NdotV = saturate(dot(normalize(normalWS), normalize(viewDirWS)));
        return pow(1.0h - NdotV, power);
    }

    // ─── Blinn-Phong specular ─────────────────────────────────────────────────
    // ~3 ALU vs GGX's ~15 — indistinguishable at Quest 3 resolution
    inline half3 BlinnPhongSpec(half3 normalWS, half3 viewDirWS,
                                 half3 lightDir, half3 specCol, half smoothness)
    {
        half3 H       = normalize(lightDir + normalize(viewDirWS));
        half  NdotH   = saturate(dot(normalize(normalWS), H));
        half  specPow = exp2(smoothness * 10.0h + 1.0h);   // remap → [2, 1024]
        return specCol * pow(NdotH, specPow);
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

        // ── Forward Lit pass ──────────────────────────────────────────────────
        Pass
        {
            Name "CrystalForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend   SrcAlpha OneMinusSrcAlpha     // hardcoded — see Properties note
            ZWrite  Off
            Cull    Off         // Both faces — hollow crystal meshes work correctly

            HLSLPROGRAM
            // shader_feature_local strips unused variants at build time (Quest shader cache)
            #pragma shader_feature_local _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma shader_feature_local _ _SHADOWS_SOFT

            // Drop heavy variants we don't need — smaller cache, faster load
            #pragma skip_variants LIGHTMAP_ON DYNAMICLIGHTMAP_ON DIRLIGHTMAP_COMBINED
            #pragma skip_variants _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            // Fog intentionally omitted — VR fog breaks stereo depth cues and
            // causes discomfort. If your project needs it, add:
            //   #pragma multi_compile_fog
            // and restore ComputeFogFactor / MixFog in vert/frag.

            #pragma vertex   CrystalVert
            #pragma fragment CrystalFrag

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
                float3 normalWS    : TEXCOORD1;
                float3 viewDirWS   : TEXCOORD2;
                float3 positionWS  : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO          // single-pass stereo (Quest)
            };

            Varyings CrystalVert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrmInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.normalWS    = nrmInputs.normalWS;
                OUT.viewDirWS   = GetWorldSpaceViewDir(posInputs.positionWS);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);

                return OUT;
            }

            half4 CrystalFrag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                // ── 1. Sample surface texture ─────────────────────────────────
                // RGB → surface detail modulator (scratches, facet lines)
                // A   → local opacity mask (chip edges, thin areas)
                half4 texSample    = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half  surfaceGrey  = dot(texSample.rgb, half3(0.299h, 0.587h, 0.114h));
                half  texAlpha     = texSample.a;

                // ── 2. Fresnel ────────────────────────────────────────────────
                half fresnel = FresnelTerm(IN.normalWS, IN.viewDirWS, _FresnelPow);

                // ── 3. Final alpha ────────────────────────────────────────────
                // _Opacity is the master control — texAlpha modulates locally,
                // fresnel adds extra opacity at edges but never exceeds 1.
                // Multiplying fresnel by _Opacity means the slider always works:
                // at 0 the crystal is fully invisible, at 1 fully opaque.
                half baseAlpha  = _Opacity * texAlpha;
                half finalAlpha = saturate(baseAlpha + fresnel * _FresnelBoost * _Opacity);

                // ── 4. Main light ─────────────────────────────────────────────
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                    float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                    Light  mainLight   = GetMainLight(shadowCoord);
                #else
                    Light mainLight = GetMainLight();
                #endif

                half3 lightDir   = normalize(mainLight.direction);
                half3 lightColor = mainLight.color * mainLight.distanceAttenuation;
                half  NdotL      = saturate(dot(normalize(IN.normalWS), lightDir));

                // ── 5. Diffuse ────────────────────────────────────────────────
                // Crystal color × surface detail (scratches darken) × light
                half3 diffuse = _CrystalColor.rgb
                              * lerp(0.6h, 1.0h, surfaceGrey)
                              * NdotL
                              * lightColor;

                // ── 6. Specular ───────────────────────────────────────────────
                half3 spec = BlinnPhongSpec(IN.normalWS, IN.viewDirWS,
                                            lightDir, _SpecColor2.rgb, _Smoothness)
                           * lightColor;

                // ── 7. Inner glow (fake internal scatter / refraction) ─────────
                // Strongest face-on (low fresnel), fades at edges
                half  glowFactor = (1.0h - fresnel) * _InnerGlow;
                half3 glow       = _InnerGlowColor.rgb * _CrystalColor.rgb * glowFactor;

                // ── 8. Ambient (baked SH) ─────────────────────────────────────
                half3 ambient = SampleSH(normalize(IN.normalWS)) * _CrystalColor.rgb * 0.4h;

                // ── 9. Compose ────────────────────────────────────────────────
                half3 color = ambient + diffuse + spec + glow;

                return half4(color, finalAlpha);
            }
            ENDHLSL
        }

        // ── No ShadowCaster pass — intentional ───────────────────────────────
        // Transparent objects casting shadows is expensive and visually wrong
        // for crystal/glass. Omitting saves one draw call per crystal per light.
        // If you need contact shadows, bake them or use a decal projector.
    }

    FallBack "Universal Render Pipeline/Unlit"
}
