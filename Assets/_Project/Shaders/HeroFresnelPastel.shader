// Объявление шейдера и пути в меню материалов Unity.
Shader "EchoesOfAscent/HeroFresnelPastel"
{
    // Блок публичных параметров, доступных в Inspector.
    Properties
    {
        // Базовый пастельный цвет тела героя (RGB + альфа прозрачности).
        _BaseColor ("Base Pastel Color", Color) = (0.78, 0.88, 0.95, 0.65)
        // Цвет свечения по краям (Fresnel rim).
        _FresnelColor ("Edge Glow Color", Color) = (0.55, 0.95, 1.0, 1.0)
        // Степень Fresnel: меньше = шире свечение по поверхности.
        _FresnelPower ("Fresnel Power", Range(0.2, 4.0)) = 0.75
        // Ширина Fresnel-маски на основном меше.
        _RimWidth ("Rim Width", Range(0.5, 5.0)) = 3.5
        // Интенсивность свечения кромки.
        _FresnelIntensity ("Fresnel Intensity", Range(0.0, 10.0)) = 5.0
        // Толщина геометрической обводки (outline pass).
        _OutlineWidth ("Outline Width", Range(0.01, 0.35)) = 0.14
        // Прозрачность геометрической обводки.
        _OutlineAlpha ("Outline Alpha", Range(0.0, 1.0)) = 0.95
        // Амплитуда синусоидальной пульсации (0.85 = хорошо заметная).
        _PulseAmount ("Pulse Amount", Range(0.0, 1.0)) = 0.85
        // Частота пульсации.
        _PulseFrequency ("Pulse Frequency", Range(0.2, 6.0)) = 2.2
        // Глобальный множитель прозрачности материала.
        _Alpha ("Alpha", Range(0.0, 1.0)) = 1.0
    }

    // Основной подшейдер для URP и мобильных GPU.
    SubShader
    {
        // Теги ренд-пайплайна, очереди прозрачности и типа объекта.
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        // LOD для мобильных.
        LOD 200

        // --- PASS 1: геометрическая обводка (всегда видна на силуэте) ---
        Pass
        {
            // Имя прохода обводки.
            Name "HeroOutline"
            // Тот же light mode для URP forward.
            Tags { "LightMode" = "UniversalForward" }

            // Рисуем только задние грани раздутого меша.
            Cull Front
            // Прозрачность без записи глубины.
            ZWrite Off
            // Аддитивное свечение обводки.
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

            // Общая синусоидальная пульсация для outline и fresnel.
            half GetPulseFactor()
            {
                half pulseWave = sin(_Time.y * _PulseFrequency);
                return pulseWave * 0.5h + 0.5h;
            }

            OutlineVaryings OutlineVert(OutlineAttributes input)
            {
                OutlineVaryings output;

                // Пульсация раздувает обводку от 70% до 130%.
                half pulseFactor = GetPulseFactor();
                half pulseScale = lerp(1.0h - _PulseAmount * 0.3h, 1.0h + _PulseAmount * 0.3h, pulseFactor);
                half extrude = _OutlineWidth * pulseScale;

                // Раздуваем вершины по нормали — классический outline trick.
                float3 positionOS = input.positionOS + input.normalOS * extrude;
                float3 positionWS = TransformObjectToWorld(positionOS);
                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half4 OutlineFrag(OutlineVaryings input) : SV_Target
            {
                half pulseFactor = GetPulseFactor();
                // Яркость обводки пульсирует почти в ноль и обратно — хорошо заметно.
                half pulseBrightness = lerp(1.0h - _PulseAmount, 1.0h + _PulseAmount * 0.6h, pulseFactor);
                half3 glow = _FresnelColor.rgb * _FresnelIntensity * pulseBrightness;
                half alpha = _OutlineAlpha * pulseBrightness;
                return half4(glow, alpha);
            }
            ENDHLSL
        }

        // --- PASS 2: основное тело + Fresnel ---
        Pass
        {
            Name "HeroFresnelPastelForward"
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
                half _OutlineWidth;
                half _OutlineAlpha;
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
                // Широкая мягкая зона свечения (на сфере хорошо читается).
                fresnelFactor = saturate(fresnelFactor * _RimWidth);
                fresnelFactor = pow(fresnelFactor, _FresnelPower);

                half pulseFactor = GetPulseFactor();
                half pulsedIntensity = _FresnelIntensity * lerp(1.0h - _PulseAmount, 1.0h + _PulseAmount * 0.5h, pulseFactor);
                fresnelFactor *= pulsedIntensity;

                half3 baseColor = _BaseColor.rgb;
                half3 rimColor = _FresnelColor.rgb * fresnelFactor;
                // Сильное аддитивное свечение поверх пастельной базы.
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
