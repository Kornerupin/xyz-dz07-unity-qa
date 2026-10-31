Shader "EchoesOfAscent/GeneratedVertexColor_20260619_073725"
{
    Properties
    {
        _BaseColor ("Base Pastel Color", Color) = (1, 0, 0.089, 1)
        _FresnelColor ("Edge Glow Color", Color) = (0.038, 0.176, 0.887, 1)
        _FresnelPower ("Fresnel Power", Range(0.2, 4.0)) = 2.84
        _RimWidth ("Rim Width", Range(0.5, 5.0)) = 3.5
        _FresnelIntensity ("Fresnel Intensity", Range(0.0, 10.0)) = 8.14
        _OutlineWidth ("Outline Width", Range(0.01, 0.35)) = 0.32
        _OutlineAlpha ("Outline Alpha", Range(0.0, 1.0)) = 0.569
        _PulseAmount ("Pulse Amount", Range(0.0, 1.0)) = 0.543
        _PulseFrequency ("Pulse Frequency", Range(0.2, 6.0)) = 0.75
        _Alpha ("Alpha", Range(0.0, 1.0)) = 0.46
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        LOD 200

        Pass
        {
            Name "FresnelOutline"
            Tags { "LightMode" = "UniversalForward" }

            Cull Front
            ZWrite Off
            Blend One One

            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #pragma target 2.0
            #pragma prefer_hlslcc gles

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _FresnelColor;
                half _FresnelIntensity;
                half _OutlineWidth;
                half _OutlineAlpha;
                half _PulseAmount;
                half _PulseFrequency;
            CBUFFER_END

            struct OutlineAttributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct OutlineVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            half GetPulseFactor()
            {
                half pulseWave = sin(_Time.y * _PulseFrequency);
                return pulseWave * 0.5h + 0.5h;
            }

            OutlineVaryings OutlineVert(OutlineAttributes input)
            {
                OutlineVaryings output;

                half pulseFactor = GetPulseFactor();
                half pulseScale = lerp(1.0h - _PulseAmount * 0.3h, 1.0h + _PulseAmount * 0.3h, pulseFactor);
                half extrude = _OutlineWidth * pulseScale;

                float3 positionOS = input.positionOS + input.normalOS * extrude;
                float3 positionWS = TransformObjectToWorld(positionOS);
                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half4 OutlineFrag(OutlineVaryings input) : SV_Target
            {
                half pulseFactor = GetPulseFactor();
                half pulseBrightness = lerp(1.0h - _PulseAmount, 1.0h + _PulseAmount * 0.6h, pulseFactor);
                half3 glow = _FresnelColor.rgb * _FresnelIntensity * pulseBrightness;
                half alpha = _OutlineAlpha * pulseBrightness;
                return half4(glow, alpha);
            }
            ENDHLSL
        }

        Pass
        {
            Name "FresnelPastelForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma prefer_hlslcc gles
            #pragma skip_variants LIGHTMAP_ON DYNAMICLIGHTMAP_ON DIRLIGHTMAP_COMBINED
            #pragma skip_variants DECALS_SCREEN_SPACE DECALS_GBUFFER DECALS_OFF
            #pragma skip_variants _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma skip_variants _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _FresnelColor;
                half _FresnelPower;
                half _RimWidth;
                half _FresnelIntensity;
                half _PulseAmount;
                half _PulseFrequency;
                half _Alpha;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            half GetPulseFactor()
            {
                half pulseWave = sin(_Time.y * _PulseFrequency);
                return pulseWave * 0.5h + 0.5h;
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                float3 positionWS = TransformObjectToWorld(input.positionOS);
                output.positionWS = positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half3 GetViewDirectionWS(float3 positionWS)
            {
                float3 viewDirWS = GetWorldSpaceViewDir(positionWS);
                return normalize((half3)viewDirWS);
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize((half3)input.normalWS);
                half3 viewDirWS = GetViewDirectionWS(input.positionWS);

                half ndotv = saturate(dot(normalWS, viewDirWS));
                half fresnelFactor = 1.0h - ndotv;
                fresnelFactor = saturate(fresnelFactor * _RimWidth);
                fresnelFactor = pow(fresnelFactor, _FresnelPower);

                half pulseFactor = GetPulseFactor();
                half pulsedIntensity = _FresnelIntensity * lerp(1.0h - _PulseAmount, 1.0h + _PulseAmount * 0.5h, pulseFactor);
                fresnelFactor *= pulsedIntensity;

                half3 baseColor = _BaseColor.rgb;
                half3 rimColor = _FresnelColor.rgb * fresnelFactor;
                half3 finalColor = baseColor + rimColor * 1.8h;

                half baseAlpha = _BaseColor.a * _Alpha;
                half rimAlpha = _FresnelColor.a * saturate(fresnelFactor);
                half finalAlpha = saturate(baseAlpha + rimAlpha * 1.2h);

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
