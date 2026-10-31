using EchoesOfAscent.Gameplay;
using UnityEditor;
using UnityEngine;

namespace EchoesOfAscent.Editor
{
    public sealed class ProceduralPlatformGeneratorWindow : EditorWindow
    {
        const string OutputFolder = "Assets/_Project/Generated/Platforms";

        float width = 4f;
        float height = 0.55f;
        int roughness = 14;
        int seed;

        [MenuItem("Ascent Tools/Generate Stone Platform")]
        static void OpenWindow()
        {
            var window = GetWindow<ProceduralPlatformGeneratorWindow>("Stone Platform");
            window.minSize = new Vector2(340f, 220f);
            window.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Procedural Stone Platform", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "2D side-view platform: chips deform the top silhouette (XY profile). " +
                "Camera must look along Z. Mesh is a thin slab with readable edge damage.",
                MessageType.Info);

            EditorGUILayout.Space(6f);
            width = EditorGUILayout.FloatField("Width", width);
            height = EditorGUILayout.FloatField("Height", height);
            roughness = EditorGUILayout.IntField("Roughness (chips)", roughness);
            seed = EditorGUILayout.IntField("Seed (0 = random)", seed);

            EditorGUILayout.Space(10f);

            if (GUILayout.Button("Generate Platform", GUILayout.Height(34f)))
                GeneratePlatform();
        }

        void GeneratePlatform()
        {
            width = Mathf.Max(0.5f, width);
            height = Mathf.Max(0.05f, height);
            roughness = Mathf.Max(0, roughness);

            int bakeSeed = seed != 0 ? seed : Random.Range(1, int.MaxValue);

            if (!EnsureOutputFolder())
            {
                EditorUtility.DisplayDialog("Generate Platform", "Failed to create output folder.", "OK");
                return;
            }

            try
            {
                EditorUtility.DisplayProgressBar("Generate Platform", "Building mesh...", 0.25f);

                Mesh mesh = ProceduralPlatformMeshBuilder.Build(width, height, roughness, bakeSeed);
                string assetId = $"{System.DateTime.Now:yyyyMMdd_HHmmss}_{bakeSeed}";

                string meshPath = $"{OutputFolder}/PlatformMesh_{assetId}.asset";
                string prefabPath = $"{OutputFolder}/Platform_{assetId}.prefab";

                AssetDatabase.CreateAsset(Object.Instantiate(mesh), meshPath);
                AssetDatabase.SaveAssets();

                Mesh savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                GameObject platformObject = BuildPlatformObject(savedMesh, bakeSeed);

                bool savedPrefab;
                PrefabUtility.SaveAsPrefabAsset(platformObject, prefabPath, out savedPrefab);
                Object.DestroyImmediate(platformObject);

                if (!savedPrefab)
                {
                    EditorUtility.DisplayDialog("Generate Platform", "Failed to save prefab.", "OK");
                    return;
                }

                AssetDatabase.SaveAssets();
                PlatformPrefabCatalogBuilder.RefreshCatalog();

                GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Selection.activeObject = prefabAsset;
                EditorGUIUtility.PingObject(prefabAsset);

                EditorUtility.DisplayDialog(
                    "Generate Platform",
                    $"Platform saved as prefab (not added to scene).\n\nMesh:\n{meshPath}\n\nPrefab:\n{prefabPath}",
                    "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        static bool EnsureOutputFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project"))
                AssetDatabase.CreateFolder("Assets", "_Project");

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Generated"))
                AssetDatabase.CreateFolder("Assets/_Project", "Generated");

            if (AssetDatabase.IsValidFolder(OutputFolder))
                return true;

            return !string.IsNullOrEmpty(AssetDatabase.CreateFolder("Assets/_Project/Generated", "Platforms"));
        }

        static GameObject BuildPlatformObject(Mesh mesh, int bakeSeed)
        {
            var platformObject = new GameObject($"Platform_Stone_{bakeSeed}");
            platformObject.tag = "Ground";

            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0)
                platformObject.layer = groundLayer;

            MeshFilter meshFilter = platformObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            MeshRenderer meshRenderer = platformObject.AddComponent<MeshRenderer>();
            Material platformMaterial = PlatformPrefabCatalogBuilder.SyncPlatformMaterialAsset();
            if (platformMaterial != null)
                meshRenderer.sharedMaterial = platformMaterial;

            platformObject.AddComponent<PlatformSpawnPoint>();
            PlatformColliderUtility.ConfigureGameplayCollider(platformObject);

            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(platformObject);
            flags |= StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI;
            GameObjectUtility.SetStaticEditorFlags(platformObject, flags);

            return platformObject;
        }
    }
}
