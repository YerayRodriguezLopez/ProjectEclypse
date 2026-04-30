Shader "Custom/Dissolve_URP-csf"
{
    Properties
    {
        // ─── Surface ────────────────────────────────────────────────────
        _MainTex        ("Albedo (RGB)",        2D)           = "white" {}
        _Color          ("Tint Color",          Color)        = (1,1,1,1)

        // ─── Dissolve ───────────────────────────────────────────────────
        _NoiseTex       ("Noise Texture",       2D)           = "white" {}
        _DissolveAmount ("Dissolve Amount",     Range(0,1))   = 0.0
        _DissolveSpeed  ("Dissolve Speed",      Range(0,5))   = 1.0

        // ─── Edge Glow ──────────────────────────────────────────────────
        _EdgeColor      ("Edge Glow Color",     Color)        = (1,0.4,0,1)
        _EdgeWidth      ("Edge Glow Width",     Range(0,0.2)) = 0.05
        _EdgeIntensity  ("Edge Glow Intensity", Range(0,10))  = 3.0
    }

    SubShader
    {
        // URP pipeline tag — without this, the shader turns pink on Quest
        Tags
        {
            "RenderType"            = "Opaque"
            "Queue"                 = "Geometry"
            "RenderPipeline"        = "UniversalPipeline"
            "UniversalMaterialType" = "Unlit"
        }
        LOD 100
        Cull Off   // Double-sided — important in VR for close-up objects

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM

            // ─── VR: Single Pass Instanced ─────────────────────────────
            #pragma multi_compile_instancing

            // Required URP keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #pragma vertex   VertexFunction
            #pragma fragment FragmentFunction

            // URP core includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ─── Textures ───────────────────────────────────────────────
            TEXTURE2D(_MainTex);   SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);  SAMPLER(sampler_NoiseTex);

            // ─── Constant buffer (SRP Batcher compatible) ───────────────
            // All uniforms MUST live inside CBUFFER_START/END for SRP Batcher
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NoiseTex_ST;
                half4  _Color;
                half   _DissolveAmount;
                half   _DissolveSpeed;
                half4  _EdgeColor;
                half   _EdgeWidth;
                half   _EdgeIntensity;
            CBUFFER_END

            // ─── Vertex input ────────────────────────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID   // Required for Single Pass Instanced
            };

            // ─── Vertex output / fragment input ──────────────────────────
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uvMain      : TEXCOORD0;
                float2 uvNoise     : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO   // Routes each eye correctly in VR
            };

            // ─── Vertex function ─────────────────────────────────────────
            Varyings VertexFunction(Attributes IN)
            {
                Varyings OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);   // Critical for VR

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uvMain      = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.uvNoise     = TRANSFORM_TEX(IN.uv, _NoiseTex);

                return OUT;
            }

            // ─── Fragment function ────────────────────────────────────────
            half4 FragmentFunction(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);   // Critical for VR

                // --- Sample noise ----------------------------------------
                half noiseSample = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.uvNoise).r;

                // --- Compute threshold (script-driven + autonomous drift) -
                half threshold = _DissolveAmount + _Time.y * _DissolveSpeed * 0.01;
                threshold      = saturate(threshold);

                // --- Clip dissolved pixels --------------------------------
                clip(noiseSample - threshold);

                // --- Edge glow on surviving pixels near the dissolve border
                half  edgeMask = 1.0 - saturate((noiseSample - threshold) / max(_EdgeWidth, 0.0001));
                half3 edgeGlow = _EdgeColor.rgb * edgeMask * _EdgeIntensity;

                // --- Albedo + glow ----------------------------------------
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvMain) * _Color;

                return half4(albedo.rgb + edgeGlow, 1.0);
            }

            ENDHLSL
        }

        // Shadow caster pass — dissolve cuts shadows too
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

            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NoiseTex_ST;
                half4  _Color;
                half   _DissolveAmount;
                half   _DissolveSpeed;
                half4  _EdgeColor;
                half   _EdgeWidth;
                half   _EdgeIntensity;
            CBUFFER_END

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
                float2 uvNoise     : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            ShadowVaryings ShadowVertexFunction(ShadowAttributes IN)
            {
                ShadowVaryings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uvNoise     = TRANSFORM_TEX(IN.uv, _NoiseTex);
                return OUT;
            }

            half4 ShadowFragmentFunction(ShadowVaryings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                half noiseSample = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.uvNoise).r;
                half threshold   = saturate(_DissolveAmount + _Time.y * _DissolveSpeed * 0.01);
                clip(noiseSample - threshold);

                return 0;
            }

            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}