Shader "Aethel/Painted URP"
{
    Properties
    {
        [MainTexture] _BaseMap("Painted albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Tint", Color) = (1,1,1,1)
        _ShadowTint("Shadow tint", Color) = (.18,.24,.36,1)
        _LightSteps("Light bands", Range(2,8)) = 4
        [HideInInspector] _Cull("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST, _BaseColor, _ShadowTint;
        float _LightSteps, _Cull;
        CBUFFER_END
        ENDHLSL
        Pass
        {
            Name "PaintedForward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2; float fog:TEXCOORD3; };
            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                half3 n = normalize(i.normalWS);
                Light key = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half diffuse = saturate(dot(n, key.direction)) * key.shadowAttenuation;
                half band = floor(diffuse * (_LightSteps - 1) + .5) / (_LightSteps - 1);
                half3 light = lerp(_ShadowTint.rgb, key.color, band) + SampleSH(n) * .35;
                #if defined(_ADDITIONAL_LIGHTS)
                for (uint index=0; index<GetAdditionalLightsCount(); index++)
                {
                    Light accent = GetAdditionalLight(index, i.positionWS);
                    light += accent.color * saturate(dot(n,accent.direction)) * accent.distanceAttenuation;
                }
                #endif
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb * _BaseColor.rgb;
                return half4(MixFog(albedo * light, i.fog),1);
            }
            ENDHLSL
        }
        // All passes share the same per-material layout for SRP Batcher compatibility.
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            float3 _LightPosition;
            float4 ShadowVert(float4 positionOS:POSITION, float3 normalOS:NORMAL):SV_POSITION
            {
                float3 positionWS = TransformObjectToWorld(positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                float3 direction = normalize(_LightPosition-positionWS);
                #else
                float3 direction = _LightDirection;
                #endif
                float4 clip = TransformWorldToHClip(ApplyShadowBias(positionWS,normalWS,direction));
                #if UNITY_REVERSED_Z
                clip.z = min(clip.z, UNITY_NEAR_CLIP_VALUE);
                #else
                clip.z = max(clip.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return clip;
            }
            half4 ShadowFrag():SV_Target { return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            float4 DepthVert(float4 positionOS:POSITION):SV_POSITION { return TransformObjectToHClip(positionOS.xyz); }
            half4 DepthFrag():SV_Target { return 0; }
            ENDHLSL
        }
    }
}
