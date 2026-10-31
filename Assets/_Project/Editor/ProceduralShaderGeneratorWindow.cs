using System.IO;
using UnityEditor;
using UnityEngine;

namespace EchoesOfAscent.Editor
{
    public sealed class ProceduralShaderGeneratorWindow : EditorWindow
    {
        const string OutputFolder = "Assets/_Project/Shaders";

        ProceduralShaderTemplate template = ProceduralShaderTemplate.VertexColor2D;
        string shaderName = "EchoesOfAscent/GeneratedVertexColor";
        string fileName = "GeneratedVertexColor";

        Color baseColor = new Color(0.82f, 0.78f, 0.72f, 1f);
        bool useVertexColors = true;

        Color fresnelColor = new Color(0.55f, 0.95f, 1f, 1f);
        float fresnelPower = 0.75f;
        float rimWidth = 3.5f;
        float fresnelIntensity = 5f;
        float outlineWidth = 0.14f;
        float outlineAlpha = 0.95f;
        float pulseAmount = 0.85f;
        float pulseFrequency = 2.2f;
        float alpha = 1f;
        bool includeOutlinePass = true;
        bool includePulse = true;

        Vector2 scroll;

        [MenuItem("Ascent Tools/Generate Shader")]
        static void OpenWindow()
        {
            var window = GetWindow<ProceduralShaderGeneratorWindow>("Shader Generator");
            window.minSize = new Vector2(360f, 420f);
            window.Show();
        }

        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("URP Shader Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Generates HLSL shaders using project templates (vertex-color platforms, Fresnel pastel spirit). " +
                "Files are saved to Assets/_Project/Shaders.",
                MessageType.Info);

            EditorGUILayout.Space(6f);

            EditorGUI.BeginChangeCheck();
            template = (ProceduralShaderTemplate)EditorGUILayout.EnumPopup("Template", template);
            shaderName = EditorGUILayout.TextField("Shader Path", shaderName);
            fileName = EditorGUILayout.TextField("File Name", fileName);
            if (EditorGUI.EndChangeCheck())
                SyncNamesFromShaderPath();

            EditorGUILayout.Space(8f);
            DrawTemplateFields();

            EditorGUILayout.Space(12f);

            if (GUILayout.Button("Generate Shader", GUILayout.Height(34f)))
                GenerateShader();

            EditorGUILayout.EndScrollView();
        }

        void SyncNamesFromShaderPath()
        {
            string sanitized = ProceduralShaderBuilder.SanitizeShaderPath(shaderName);
            shaderName = sanitized;

            int slash = sanitized.LastIndexOf('/');
            string shortName = slash >= 0 ? sanitized.Substring(slash + 1) : sanitized;
            if (!string.IsNullOrWhiteSpace(shortName))
                fileName = ProceduralShaderBuilder.SanitizeFileName(shortName);
        }

        void DrawTemplateFields()
        {
            switch (template)
            {
                case ProceduralShaderTemplate.VertexColor2D:
                    EditorGUILayout.LabelField("Vertex Color 2D", EditorStyles.boldLabel);
                    baseColor = EditorGUILayout.ColorField("Base Color", baseColor);
                    useVertexColors = EditorGUILayout.Toggle("Multiply Vertex Colors", useVertexColors);
                    break;

                case ProceduralShaderTemplate.FresnelPastel:
                    EditorGUILayout.LabelField("Fresnel Pastel", EditorStyles.boldLabel);
                    baseColor = EditorGUILayout.ColorField("Base Pastel Color", baseColor);
                    fresnelColor = EditorGUILayout.ColorField("Edge Glow Color", fresnelColor);
                    fresnelPower = EditorGUILayout.Slider("Fresnel Power", fresnelPower, 0.2f, 4f);
                    rimWidth = EditorGUILayout.Slider("Rim Width", rimWidth, 0.5f, 5f);
                    fresnelIntensity = EditorGUILayout.Slider("Fresnel Intensity", fresnelIntensity, 0f, 10f);
                    alpha = EditorGUILayout.Slider("Alpha", alpha, 0f, 1f);
                    includeOutlinePass = EditorGUILayout.Toggle("Outline Pass", includeOutlinePass);
                    includePulse = EditorGUILayout.Toggle("Pulse Animation", includePulse);

                    if (includeOutlinePass)
                    {
                        outlineWidth = EditorGUILayout.Slider("Outline Width", outlineWidth, 0.01f, 0.35f);
                        outlineAlpha = EditorGUILayout.Slider("Outline Alpha", outlineAlpha, 0f, 1f);
                    }

                    if (includePulse)
                    {
                        pulseAmount = EditorGUILayout.Slider("Pulse Amount", pulseAmount, 0f, 1f);
                        pulseFrequency = EditorGUILayout.Slider("Pulse Frequency", pulseFrequency, 0.2f, 6f);
                    }

                    break;
            }
        }

        void GenerateShader()
        {
            if (!EnsureOutputFolder())
            {
                EditorUtility.DisplayDialog("Generate Shader", "Failed to create Shaders folder.", "OK");
                return;
            }

            SyncNamesFromShaderPath();

            string safeFileName = ProceduralShaderBuilder.SanitizeFileName(fileName);
            string stampedName = $"{safeFileName}_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            string shaderPath = $"{OutputFolder}/{stampedName}.shader";
            safeFileName = stampedName;
            shaderName = $"EchoesOfAscent/{safeFileName}";

            string source = template switch
            {
                ProceduralShaderTemplate.VertexColor2D => ProceduralShaderBuilder.BuildVertexColor2D(
                    new VertexColor2DShaderOptions
                    {
                        ShaderName = shaderName,
                        BaseColor = baseColor,
                        UseVertexColors = useVertexColors
                    }),
                ProceduralShaderTemplate.FresnelPastel => ProceduralShaderBuilder.BuildFresnelPastel(
                    new FresnelPastelShaderOptions
                    {
                        ShaderName = shaderName,
                        BaseColor = baseColor,
                        FresnelColor = fresnelColor,
                        FresnelPower = fresnelPower,
                        RimWidth = rimWidth,
                        FresnelIntensity = fresnelIntensity,
                        OutlineWidth = outlineWidth,
                        OutlineAlpha = outlineAlpha,
                        PulseAmount = pulseAmount,
                        PulseFrequency = pulseFrequency,
                        Alpha = alpha,
                        IncludeOutlinePass = includeOutlinePass,
                        IncludePulse = includePulse
                    }),
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(source))
            {
                EditorUtility.DisplayDialog("Generate Shader", "Unknown shader template.", "OK");
                return;
            }

            File.WriteAllText(shaderPath, source);
            AssetDatabase.ImportAsset(shaderPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            Selection.activeObject = shader;
            EditorGUIUtility.PingObject(shader);

            if (template == ProceduralShaderTemplate.VertexColor2D
                || template == ProceduralShaderTemplate.FresnelPastel)
            {
                PlatformPrefabCatalogBuilder.SyncPlatformMaterialPalette();
                PlatformPrefabCatalogBuilder.RefreshCatalog();
            }

            EditorUtility.DisplayDialog(
                "Generate Shader",
                $"Shader saved to:\n{shaderPath}\n\nMenu path:\n{shaderName}",
                "OK");
        }

        static bool EnsureOutputFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project"))
                AssetDatabase.CreateFolder("Assets", "_Project");

            if (AssetDatabase.IsValidFolder(OutputFolder))
                return true;

            return !string.IsNullOrEmpty(AssetDatabase.CreateFolder("Assets/_Project", "Shaders"));
        }
    }
}
