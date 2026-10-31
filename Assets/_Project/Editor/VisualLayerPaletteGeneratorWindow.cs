using EchoesOfAscent.Presentation;
using UnityEditor;
using UnityEngine;

namespace EchoesOfAscent.Editor
{
    public sealed class VisualLayerPaletteGeneratorWindow : EditorWindow
    {
        const string ProjectFolder = "Assets/_Project";
        const string DataFolder = "Assets/_Project/Data";
        const int LayerCount = 4;

        [MenuItem("Ascent Tools/Generate visual Layer")]
        static void OpenWindow()
        {
            var window = GetWindow<VisualLayerPaletteGeneratorWindow>("Visual Layer Palettes");
            window.minSize = new Vector2(320f, 140f);
            window.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Visual Layer Palette Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                $"Creates or updates {LayerCount} VisualLayerConfig assets in {DataFolder}.",
                MessageType.Info);

            EditorGUILayout.Space(8f);

            if (GUILayout.Button("Generate Pallets", GUILayout.Height(32f)))
                GeneratePalettes();
        }

        static void GeneratePalettes()
        {
            if (!EnsureFolderExists(ProjectFolder, "Assets"))
            {
                EditorUtility.DisplayDialog("Generate Pallets", "Failed to create project folder.", "OK");
                return;
            }

            if (!EnsureFolderExists(DataFolder, ProjectFolder))
            {
                EditorUtility.DisplayDialog("Generate Pallets", "Failed to create data folder.", "OK");
                return;
            }

            try
            {
                for (int layerIndex = 0; layerIndex < LayerCount; layerIndex++)
                {
                    EditorUtility.DisplayProgressBar(
                        "Generate Pallets",
                        $"Creating layer {layerIndex}...",
                        (layerIndex + 1f) / LayerCount);

                    string assetPath = $"{DataFolder}/VisualLayer_{layerIndex}.asset";
                    var config = AssetDatabase.LoadAssetAtPath<VisualLayerConfig>(assetPath);

                    if (config == null)
                    {
                        config = ScriptableObject.CreateInstance<VisualLayerConfig>();
                        AssetDatabase.CreateAsset(config, assetPath);
                    }

                    ApplyPalette(config, layerIndex);
                    EditorUtility.SetDirty(config);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog(
                    "Generate Pallets",
                    $"Created or updated {LayerCount} visual layer assets in:\n{DataFolder}",
                    "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        static bool EnsureFolderExists(string folderPath, string parentFolder)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return true;

            string folderName = folderPath.Substring(parentFolder.Length + 1);
            return !string.IsNullOrEmpty(AssetDatabase.CreateFolder(parentFolder, folderName));
        }

        static void ApplyPalette(VisualLayerConfig config, int layerIndex)
        {
            Palette palette = GetPalette(layerIndex);

            config.layerIndex = layerIndex;
            config.fogColor = palette.FogColor;
            config.ambientColor = palette.AmbientColor;
            config.postProcessWeight = palette.PostProcessWeight;
            config.parallaxSpeed = palette.ParallaxSpeed;
        }

        static Palette GetPalette(int layerIndex)
        {
            return layerIndex switch
            {
                0 => new Palette(
                    new Color(0.58f, 0.50f, 0.42f),
                    new Color(0.72f, 0.64f, 0.54f),
                    0.82f,
                    0.30f),
                1 => new Palette(
                    new Color(0.54f, 0.50f, 0.44f),
                    new Color(0.70f, 0.66f, 0.58f),
                    0.88f,
                    0.45f),
                2 => new Palette(
                    new Color(0.62f, 0.64f, 0.60f),
                    new Color(0.78f, 0.76f, 0.70f),
                    0.94f,
                    0.60f),
                _ => new Palette(
                    new Color(0.74f, 0.78f, 0.82f),
                    new Color(0.88f, 0.90f, 0.93f),
                    1.00f,
                    0.75f),
            };
        }

        readonly struct Palette
        {
            public Color FogColor { get; }
            public Color AmbientColor { get; }
            public float PostProcessWeight { get; }
            public float ParallaxSpeed { get; }

            public Palette(Color fogColor, Color ambientColor, float postProcessWeight, float parallaxSpeed)
            {
                FogColor = fogColor;
                AmbientColor = ambientColor;
                PostProcessWeight = postProcessWeight;
                ParallaxSpeed = parallaxSpeed;
            }
        }
    }
}
