using System.Globalization;
using System.Text;
using UnityEngine;

namespace EchoesOfAscent.Editor
{
    public enum ProceduralShaderTemplate
    {
        VertexColor2D,
        FresnelPastel
    }

    public struct VertexColor2DShaderOptions
    {
        public string ShaderName;
        public Color BaseColor;
        public bool UseVertexColors;
    }

    public struct FresnelPastelShaderOptions
    {
        public string ShaderName;
        public Color BaseColor;
        public Color FresnelColor;
        public float FresnelPower;
        public float RimWidth;
        public float FresnelIntensity;
        public float OutlineWidth;
        public float OutlineAlpha;
        public float PulseAmount;
        public float PulseFrequency;
        public float Alpha;
        public bool IncludeOutlinePass;
        public bool IncludePulse;
    }

    public static class ProceduralShaderBuilder
    {
        /// <summary>
        /// Генерирует HLSL-код шейдера по выбранному шаблону.
        /// </summary>
        /// <param name="template">Тип шаблона шейдера.</param>
        /// <param name="options">Параметры шаблона (VertexColor2DShaderOptions или FresnelPastelShaderOptions).</param>
        /// <returns>Исходный код шейдера или пустая строка для неизвестного шаблона.</returns>
        public static string Build(ProceduralShaderTemplate template, object options)
        {
            return template switch
            {
                ProceduralShaderTemplate.VertexColor2D => BuildVertexColor2D((VertexColor2DShaderOptions)options),
                ProceduralShaderTemplate.FresnelPastel => BuildFresnelPastel((FresnelPastelShaderOptions)options),
                _ => string.Empty
            };
        }

        /// <summary>
        /// Генерирует шейдер с поддержкой vertex colors для 2D-платформ.
        /// </summary>
        /// <param name="options">Имя, базовый цвет и флаг использования vertex colors.</param>
        /// <returns>Полный исходный код .shader файла.</returns>
        public static string BuildVertexColor2D(VertexColor2DShaderOptions options)
        {
            string shaderName = SanitizeShaderPath(options.ShaderName);
            string color = FormatColor(options.BaseColor);
            string fragment = options.UseVertexColors
                ? "return half4(_BaseColor.rgb * input.color.rgb, 1.0h);"
                : "return half4(_BaseColor.rgb, _BaseColor.a);";

            string colorAttribute = options.UseVertexColors
                ? "                float4 color : COLOR;"
                : string.Empty;

            string colorVarying = options.UseVertexColors
                ? "                half4 color : COLOR;"
                : string.Empty;

            string colorAssign = options.UseVertexColors
                ? "                output.color = input.color;"
                : string.Empty;

            return $@"Shader ""{shaderName}""
{{
    Properties
    {{
        _BaseColor (""Base Color"", Color) = {color}
    }}

    SubShader
    {{
        Tags
        {{
            ""RenderPipeline"" = ""UniversalPipeline""
            ""RenderType"" = ""Opaque""
            ""Queue"" = ""Geometry""
        }}

        Pass
        {{
            Name ""VertexColor2D""
            Tags {{ ""LightMode"" = ""UniversalForward"" }}

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma prefer_hlslcc gles

            #include ""Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl""

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END

            struct Attributes
            {{
                float3 positionOS : POSITION;
{colorAttribute}
            }};

            struct Varyings
            {{
                float4 positionCS : SV_POSITION;
{colorVarying}
            }};

            Varyings vert(Attributes input)
            {{
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(positionWS);
{colorAssign}
                return output;
            }}

            half4 frag(Varyings input) : SV_Target
            {{
                {fragment}
            }}
            ENDHLSL
        }}
    }}

    FallBack ""Universal Render Pipeline/Unlit""
}}
";
        }

        /// <summary>
        /// Генерирует шейдер с Fresnel-обводкой и пастельным свечением.
        /// </summary>
        /// <param name="options">Параметры цвета, обводки, пульсации и прозрачности.</param>
        /// <returns>Полный исходный код .shader файла.</returns>
        public static string BuildFresnelPastel(FresnelPastelShaderOptions options)
        {
            string shaderName = SanitizeShaderPath(options.ShaderName);
            string baseColor = FormatColor(options.BaseColor);
            string fresnelColor = FormatColor(options.FresnelColor);

            string pulseFunctions = options.IncludePulse
                ? @"
            half GetPulseFactor()
            {
                half pulseWave = sin(_Time.y * _PulseFrequency);
                return pulseWave * 0.5h + 0.5h;
            }"
                : @"
            half GetPulseFactor()
            {
                return 0.5h;
            }";

            string outlinePulseScale = options.IncludePulse
                ? "                half pulseScale = lerp(1.0h - _PulseAmount * 0.3h, 1.0h + _PulseAmount * 0.3h, pulseFactor);"
                : "                half pulseScale = 1.0h;";

            string outlineBrightness = options.IncludePulse
                ? "                half pulseBrightness = lerp(1.0h - _PulseAmount, 1.0h + _PulseAmount * 0.6h, pulseFactor);"
                : "                half pulseBrightness = 1.0h;";

            string bodyPulseIntensity = options.IncludePulse
                ? "                half pulsedIntensity = _FresnelIntensity * lerp(1.0h - _PulseAmount, 1.0h + _PulseAmount * 0.5h, pulseFactor);"
                : "                half pulsedIntensity = _FresnelIntensity;";

            string outlinePass = options.IncludeOutlinePass
                ? $@"        Pass
        {{
            Name ""FresnelOutline""
            Tags {{ ""LightMode"" = ""UniversalForward"" }}

            Cull Front
            ZWrite Off
            Blend One One

            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #pragma target 2.0
            #pragma prefer_hlslcc gles

            #include ""Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl""

            CBUFFER_START(UnityPerMaterial)
                half4 _FresnelColor;
                half _FresnelIntensity;
                half _OutlineWidth;
                half _OutlineAlpha;
                half _PulseAmount;
                half _PulseFrequency;
            CBUFFER_END

            struct OutlineAttributes
            {{
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
            }};

            struct OutlineVaryings
            {{
                float4 positionCS : SV_POSITION;
            }};
{pulseFunctions}

            OutlineVaryings OutlineVert(OutlineAttributes input)
            {{
                OutlineVaryings output;

                half pulseFactor = GetPulseFactor();
{outlinePulseScale}
                half extrude = _OutlineWidth * pulseScale;

                float3 positionOS = input.positionOS + input.normalOS * extrude;
                float3 positionWS = TransformObjectToWorld(positionOS);
                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }}

            half4 OutlineFrag(OutlineVaryings input) : SV_Target
            {{
                half pulseFactor = GetPulseFactor();
{outlineBrightness}
                half3 glow = _FresnelColor.rgb * _FresnelIntensity * pulseBrightness;
                half alpha = _OutlineAlpha * pulseBrightness;
                return half4(glow, alpha);
            }}
            ENDHLSL
        }}

"
                : string.Empty;

            string pulseProperties = options.IncludePulse
                ? $@"        _PulseAmount (""Pulse Amount"", Range(0.0, 1.0)) = {FormatFloat(options.PulseAmount)}
        _PulseFrequency (""Pulse Frequency"", Range(0.2, 6.0)) = {FormatFloat(options.PulseFrequency)}
"
                : string.Empty;

            string pulseCbuffer = options.IncludePulse
                ? @"                half _PulseAmount;
                half _PulseFrequency;
"
                : string.Empty;

            string outlineProperties = options.IncludeOutlinePass
                ? $@"        _OutlineWidth (""Outline Width"", Range(0.01, 0.35)) = {FormatFloat(options.OutlineWidth)}
        _OutlineAlpha (""Outline Alpha"", Range(0.0, 1.0)) = {FormatFloat(options.OutlineAlpha)}
"
                : string.Empty;

            return $@"Shader ""{shaderName}""
{{
    Properties
    {{
        _BaseColor (""Base Pastel Color"", Color) = {baseColor}
        _FresnelColor (""Edge Glow Color"", Color) = {fresnelColor}
        _FresnelPower (""Fresnel Power"", Range(0.2, 4.0)) = {FormatFloat(options.FresnelPower)}
        _RimWidth (""Rim Width"", Range(0.5, 5.0)) = {FormatFloat(options.RimWidth)}
        _FresnelIntensity (""Fresnel Intensity"", Range(0.0, 10.0)) = {FormatFloat(options.FresnelIntensity)}
{outlineProperties}{pulseProperties}        _Alpha (""Alpha"", Range(0.0, 1.0)) = {FormatFloat(options.Alpha)}
    }}

    SubShader
    {{
        Tags
        {{
            ""RenderPipeline"" = ""UniversalPipeline""
            ""RenderType"" = ""Transparent""
            ""Queue"" = ""Transparent""
            ""IgnoreProjector"" = ""True""
        }}

        LOD 200

{outlinePass}        Pass
        {{
            Name ""FresnelPastelForward""
            Tags {{ ""LightMode"" = ""UniversalForward"" }}

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

            #include ""Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl""

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _FresnelColor;
                half _FresnelPower;
                half _RimWidth;
                half _FresnelIntensity;
{pulseCbuffer}                half _Alpha;
            CBUFFER_END

            struct Attributes
            {{
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
            }};

            struct Varyings
            {{
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            }};
{pulseFunctions}

            Varyings vert(Attributes input)
            {{
                Varyings output = (Varyings)0;
                float3 positionWS = TransformObjectToWorld(input.positionOS);
                output.positionWS = positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }}

            half3 GetViewDirectionWS(float3 positionWS)
            {{
                float3 viewDirWS = GetWorldSpaceViewDir(positionWS);
                return normalize((half3)viewDirWS);
            }}

            half4 frag(Varyings input) : SV_Target
            {{
                half3 normalWS = normalize((half3)input.normalWS);
                half3 viewDirWS = GetViewDirectionWS(input.positionWS);

                half ndotv = saturate(dot(normalWS, viewDirWS));
                half fresnelFactor = 1.0h - ndotv;
                fresnelFactor = saturate(fresnelFactor * _RimWidth);
                fresnelFactor = pow(fresnelFactor, _FresnelPower);

                half pulseFactor = GetPulseFactor();
{bodyPulseIntensity}
                fresnelFactor *= pulsedIntensity;

                half3 baseColor = _BaseColor.rgb;
                half3 rimColor = _FresnelColor.rgb * fresnelFactor;
                half3 finalColor = baseColor + rimColor * 1.8h;

                half baseAlpha = _BaseColor.a * _Alpha;
                half rimAlpha = _FresnelColor.a * saturate(fresnelFactor);
                half finalAlpha = saturate(baseAlpha + rimAlpha * 1.2h);

                return half4(finalColor, finalAlpha);
            }}
            ENDHLSL
        }}
    }}

    FallBack ""Universal Render Pipeline/Unlit""
}}
";
        }

        /// <summary>
        /// Нормализует имя шейдера к формату EchoesOfAscent/...
        /// </summary>
        /// <param name="shaderName">Вводимое имя шейдера.</param>
        /// <returns>Канонический путь шейдера в Unity.</returns>
        public static string SanitizeShaderPath(string shaderName)
        {
            if (string.IsNullOrWhiteSpace(shaderName))
                return "EchoesOfAscent/GeneratedShader";

            string trimmed = shaderName.Trim().Replace('\\', '/');
            if (!trimmed.StartsWith("EchoesOfAscent/"))
                trimmed = "EchoesOfAscent/" + trimmed.TrimStart('/');

            return trimmed;
        }

        /// <summary>
        /// Очищает имя файла, оставляя только буквы, цифры, '_' и '-'.
        /// </summary>
        /// <param name="fileName">Исходное имя файла.</param>
        /// <returns>Безопасное имя файла для asset.</returns>
        public static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "GeneratedShader";

            var builder = new StringBuilder(fileName.Length);
            string source = fileName.Trim();
            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
                    builder.Append(c);
            }

            return builder.Length > 0 ? builder.ToString() : "GeneratedShader";
        }

        static string FormatColor(Color color)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "({0:0.###}, {1:0.###}, {2:0.###}, {3:0.###})",
                color.r,
                color.g,
                color.b,
                color.a);
        }

        static string FormatFloat(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
