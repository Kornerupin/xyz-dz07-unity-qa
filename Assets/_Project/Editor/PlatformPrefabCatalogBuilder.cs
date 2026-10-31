using System.Collections.Generic;
using System.IO;
using EchoesOfAscent.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoesOfAscent.Editor
{
    public static class PlatformPrefabCatalogBuilder
    {
        public const string CatalogPath = "Assets/_Project/Data/PlatformPrefabCatalog.asset";
        public const string PalettePath = "Assets/_Project/Data/PlatformMaterialPalette.asset";
        public const string PrefabFolder = "Assets/_Project/Generated/Platforms";
        const string PlatformMaterialGuid = "c3d4e5f6a7b849012345678901bcdef1";
        const string ShadersFolder = "Assets/_Project/Shaders";
        const string GeneratedMaterialsFolder = "Assets/_Project/Materials/Generated";
        const string CanonicalVertexColorShaderPath = ShadersFolder + "/GeneratedVertexColor.shader";

        /// <summary>
        /// Пункт меню: синхронизирует шейдеры, перезапекает цвета и обновляет префабы платформ.
        /// </summary>
        [MenuItem("Ascent Tools/Apply Platform Shaders")]
        public static void ApplyPlatformShadersFromMenu()
        {
            ApplyPlatformShaderPipeline();
            EditorUtility.DisplayDialog(
                "Platform Shaders",
                "Platform material synced, mesh colors rebaked, prefabs updated.",
                "OK");
        }

        /// <summary>
        /// Выполняет полный пайплайн синхронизации материалов, палитры и префабов платформ.
        /// </summary>
        public static void ApplyPlatformShaderPipeline()
        {
            SyncPlatformMaterialAsset();
            PlatformMaterialPalette palette = SyncPlatformMaterialPalette();

            if (palette != null && palette.HasShaders)
                ApplyPaletteToAllPlatformPrefabs(palette);
            else
            {
                Material material = SyncPlatformMaterialAsset();
                RebakeAllPlatformMeshColors(material);
                ApplyMaterialToAllPlatformPrefabs(material);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Пункт меню: исправляет коллайдеры и материалы на всех префабах платформ.
        /// </summary>
        [MenuItem("Ascent Tools/Fix Platform Prefab Colliders")]
        public static void FixPlatformPrefabColliders()
        {
            string[] prefabGuids = AssetDatabase.FindAssets("Platform_ t:Prefab", new[] { PrefabFolder });
            int fixedCount = 0;
            Material platformMaterial = SyncPlatformMaterialAsset();
            PlatformMaterialPalette palette = SyncPlatformMaterialPalette();

            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
                if (prefabRoot == null)
                    continue;

                PlatformColliderUtility.ConfigureGameplayCollider(prefabRoot);
                ClearStaticFlags(prefabRoot);

                Material prefabMaterial = platformMaterial;
                Shader shader = palette != null ? palette.GetShaderAt(i) : null;
                Material generatedMaterial = LoadGeneratedMaterialAsset(shader);
                if (generatedMaterial != null)
                    prefabMaterial = generatedMaterial;

                RebakePrefabMeshColors(prefabRoot, prefabMaterial);
                ApplyPlatformMaterial(prefabRoot, prefabMaterial);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                fixedCount++;
            }

            AssetDatabase.SaveAssets();
            RefreshCatalog();
            EditorUtility.DisplayDialog(
                "Platform Colliders",
                $"Updated colliders and materials on {fixedCount} prefab(s).",
                "OK");
        }

        /// <summary>
        /// Синхронизирует asset материала PlatformStone2D с указанным или найденным шейдером.
        /// </summary>
        /// <param name="generatedShader">Шейдер для применения; null — автопоиск.</param>
        /// <returns>Обновлённый Material asset.</returns>
        public static Material SyncPlatformMaterialAsset(Shader generatedShader = null)
        {
            const string materialPath = "Assets/_Project/Materials/PlatformStone2D.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Shader shader = generatedShader != null ? generatedShader : LoadGeneratedPlatformShader();
            if (shader == null)
                return material;

            if (material == null)
            {
                material = new Material(shader) { name = PlatformMaterialResolver.PlatformMaterialName };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else
            {
                material.shader = shader;
            }

            material.SetColor(PlatformVisualUtility.BaseColorId, ReadShaderDefaultBaseColor(shader));
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>
        /// Читает значение _BaseColor по умолчанию из временного материала шейдера.
        /// </summary>
        /// <param name="shader">Шейдер для чтения цвета.</param>
        /// <returns>Базовый цвет шейдера или Color.white, если shader null.</returns>
        public static Color ReadShaderDefaultBaseColor(Shader shader)
        {
            if (shader == null)
                return Color.white;

            var tempMaterial = new Material(shader);
            Color baseColor = tempMaterial.GetColor(PlatformVisualUtility.BaseColorId);
            Object.DestroyImmediate(tempMaterial);
            return baseColor;
        }

        /// <summary>
        /// Пункт меню: синхронизирует палитру шейдеров и применяет её к префабам.
        /// </summary>
        [MenuItem("Ascent Tools/Sync Platform Material Palette")]
        public static void SyncPlatformMaterialPaletteFromMenu()
        {
            PlatformMaterialPalette palette = SyncPlatformMaterialPalette();
            if (palette != null && palette.HasShaders)
                ApplyPaletteToAllPlatformPrefabs(palette);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "Platform Material Palette",
                palette != null && palette.HasShaders
                    ? $"Synced {palette.Shaders.Count} generated shader(s)."
                    : "No generated shaders found in Assets/_Project/Shaders.",
                "OK");
        }

        /// <summary>
        /// Собирает все Generated-шейдеры, создаёт material assets и обновляет палитру.
        /// </summary>
        /// <returns>Обновлённый PlatformMaterialPalette asset.</returns>
        public static PlatformMaterialPalette SyncPlatformMaterialPalette()
        {
            EnsureDataFolder();
            EnsureGeneratedMaterialsFolder();

            List<Shader> shaders = FindAllGeneratedShaders();
            SyncGeneratedMaterialAssets(shaders);

            PlatformMaterialPalette palette = AssetDatabase.LoadAssetAtPath<PlatformMaterialPalette>(PalettePath);
            if (palette == null)
            {
                palette = ScriptableObject.CreateInstance<PlatformMaterialPalette>();
                AssetDatabase.CreateAsset(palette, PalettePath);
            }

            palette.ReplaceShaders(shaders);
            EditorUtility.SetDirty(palette);
            return palette;
        }

        static void SyncGeneratedMaterialAssets(IReadOnlyList<Shader> shaders)
        {
            for (int i = 0; i < shaders.Count; i++)
            {
                Shader shader = shaders[i];
                if (shader == null)
                    continue;

                string shaderPath = AssetDatabase.GetAssetPath(shader);
                string fileName = Path.GetFileNameWithoutExtension(shaderPath);
                string materialPath = $"{GeneratedMaterialsFolder}/{fileName}.mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(shader) { name = fileName };
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                else
                {
                    material.shader = shader;
                }

                material.SetColor(PlatformVisualUtility.BaseColorId, ReadShaderDefaultBaseColor(shader));
                EditorUtility.SetDirty(material);
            }
        }

        static List<Shader> FindAllGeneratedShaders()
        {
            string[] shaderGuids = AssetDatabase.FindAssets("t:Shader", new[] { ShadersFolder });
            var shaders = new List<Shader>(shaderGuids.Length);

            for (int i = 0; i < shaderGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(shaderGuids[i]);
                string fileName = Path.GetFileNameWithoutExtension(path);
                if (!fileName.StartsWith("Generated", System.StringComparison.Ordinal))
                    continue;

                Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if (shader != null)
                    shaders.Add(shader);
            }

            shaders.Sort((left, right) => string.Compare(
                Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(left)),
                Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(right)),
                System.StringComparison.Ordinal));

            return shaders;
        }

        /// <summary>
        /// Применяет материалы из палитры ко всем префабам Platform_ в Generated/Platforms.
        /// </summary>
        /// <param name="palette">Палитра шейдеров с material assets.</param>
        /// <returns>Количество обновлённых префабов.</returns>
        public static int ApplyPaletteToAllPlatformPrefabs(PlatformMaterialPalette palette)
        {
            if (palette == null || !palette.HasShaders)
                return 0;

            string[] prefabGuids = AssetDatabase.FindAssets("Platform_ t:Prefab", new[] { PrefabFolder });
            int updated = 0;

            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
                if (prefabRoot == null)
                    continue;

                Material platformMaterial = LoadGeneratedMaterialAsset(palette.GetShaderAt(i));
                if (platformMaterial == null)
                    continue;

                ApplyPlatformMaterial(prefabRoot, platformMaterial);
                ClearStaticFlags(prefabRoot);
                RebakePrefabMeshColors(prefabRoot, platformMaterial);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                updated++;
            }

            return updated;
        }

        static Material LoadGeneratedMaterialAsset(Shader shader)
        {
            if (shader == null)
                return null;

            string shaderPath = AssetDatabase.GetAssetPath(shader);
            string fileName = Path.GetFileNameWithoutExtension(shaderPath);
            return AssetDatabase.LoadAssetAtPath<Material>($"{GeneratedMaterialsFolder}/{fileName}.mat");
        }

        /// <summary>
        /// Применяет один материал ко всем префабам Platform_ в Generated/Platforms.
        /// </summary>
        /// <param name="platformMaterial">Материал для назначения.</param>
        /// <returns>Количество обновлённых префабов.</returns>
        public static int ApplyMaterialToAllPlatformPrefabs(Material platformMaterial)
        {
            string[] prefabGuids = AssetDatabase.FindAssets("Platform_ t:Prefab", new[] { PrefabFolder });
            int updated = 0;

            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
                if (prefabRoot == null)
                    continue;

                ApplyPlatformMaterial(prefabRoot, platformMaterial);
                ClearStaticFlags(prefabRoot);
                RebakePrefabMeshColors(prefabRoot, platformMaterial);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                updated++;
            }

            return updated;
        }

        static Shader LoadGeneratedPlatformShader()
        {
            Shader canonical = AssetDatabase.LoadAssetAtPath<Shader>(CanonicalVertexColorShaderPath);
            if (canonical != null)
                return canonical;

            string[] shaderGuids = AssetDatabase.FindAssets("GeneratedVertexColor t:Shader", new[] { ShadersFolder });
            Shader latestShader = null;
            string latestFileName = string.Empty;

            for (int i = 0; i < shaderGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(shaderGuids[i]);
                Shader candidate = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if (candidate == null)
                    continue;

                string fileName = Path.GetFileNameWithoutExtension(path);
                if (string.Compare(fileName, latestFileName, System.StringComparison.Ordinal) > 0)
                {
                    latestShader = candidate;
                    latestFileName = fileName;
                }
            }

            return latestShader;
        }

        static Material LoadPlatformMaterial()
        {
            Material synced = SyncPlatformMaterialAsset();
            if (synced != null)
                return synced;

            return AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(PlatformMaterialGuid));
        }

        static void ApplyPlatformMaterial(GameObject platform, Material platformMaterial)
        {
            if (platformMaterial == null || !platform.TryGetComponent(out MeshRenderer renderer))
                return;

            renderer.sharedMaterial = platformMaterial;
        }

        /// <summary>
        /// Перезапекает vertex colors на всех mesh-ассетах PlatformMesh_.
        /// </summary>
        /// <param name="platformMaterial">Материал, определяющий режим запекания; null — без учёта шейдера.</param>
        /// <returns>Количество обновлённых мешей.</returns>
        public static int RebakeAllPlatformMeshColors(Material platformMaterial = null)
        {
            string[] meshGuids = AssetDatabase.FindAssets("PlatformMesh_ t:Mesh", new[] { PrefabFolder });
            int updated = 0;

            for (int i = 0; i < meshGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(meshGuids[i]);
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (mesh == null)
                    continue;

                PlatformVisualUtility.RebakeVertexColors(mesh, platformMaterial);
                EditorUtility.SetDirty(mesh);
                updated++;
            }

            return updated;
        }

        static void RebakePrefabMeshColors(GameObject platform, Material platformMaterial)
        {
            if (!platform.TryGetComponent(out MeshFilter meshFilter) || meshFilter.sharedMesh == null)
                return;

            PlatformVisualUtility.RebakeVertexColors(meshFilter.sharedMesh, platformMaterial);
            EditorUtility.SetDirty(meshFilter.sharedMesh);
        }

        static void ClearStaticFlags(GameObject platform)
        {
            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(platform);
            if (flags == 0)
                return;

            GameObjectUtility.SetStaticEditorFlags(platform, 0);
        }

        /// <summary>
        /// Пункт меню: обновляет каталог префабов платформ из Generated/Platforms.
        /// </summary>
        [MenuItem("Ascent Tools/Refresh Platform Catalog")]
        public static void RefreshFromMenu()
        {
            RefreshCatalog();
            EditorUtility.DisplayDialog(
                "Platform Catalog",
                $"Catalog refreshed.\n\n{CatalogPath}",
                "OK");
        }

        /// <summary>
        /// Пункт меню: удаляет каменные платформы из активной сцены.
        /// </summary>
        [MenuItem("Ascent Tools/Clear Stone Platforms From Scene")]
        public static void ClearStonePlatformsFromScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            int removed = 0;

            for (int i = 0; i < roots.Length; i++)
                removed += RemoveStonePlatformsRecursive(roots[i]);

            if (removed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log($"Removed {removed} stone platform object(s) from scene '{scene.name}'.");
            }
            else
            {
                Debug.Log($"No stone platform objects found in scene '{scene.name}'.");
            }
        }

        /// <summary>
        /// Обновляет каталог префабов и синхронизирует связанные материалы.
        /// </summary>
        /// <returns>Обновлённый PlatformPrefabCatalog asset.</returns>
        public static PlatformPrefabCatalog RefreshCatalog()
        {
            PlatformPrefabCatalog catalog = RefreshCatalogEntries();
            EnsureGeneratedPlatformMaterials();
            return catalog;
        }

        static PlatformPrefabCatalog RefreshCatalogEntries()
        {
            EnsureDataFolder();

            PlatformPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<PlatformPrefabCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PlatformPrefabCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            string[] prefabGuids = AssetDatabase.FindAssets("Platform_ t:Prefab", new[] { PrefabFolder });
            var entries = new List<PlatformPrefabCatalog.Entry>(prefabGuids.Length);

            PlatformMaterialPalette palette = AssetDatabase.LoadAssetAtPath<PlatformMaterialPalette>(PalettePath);
            int shaderCount = palette != null && palette.HasShaders ? palette.Shaders.Count : 0;

            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;

                entries.Add(new PlatformPrefabCatalog.Entry
                {
                    prefab = prefab,
                    baseSize = PlatformSpecUtility.ReadBaseSize(prefab),
                    paletteShaderIndex = shaderCount > 0 ? i % shaderCount : -1
                });
            }

            catalog.ReplaceEntries(entries);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return catalog;
        }

        /// <summary>
        /// Синхронизирует материалы и применяет палитру ко всем префабам платформ.
        /// </summary>
        public static void EnsureGeneratedPlatformMaterials()
        {
            SyncPlatformMaterialAsset();
            PlatformMaterialPalette palette = SyncPlatformMaterialPalette();
            if (palette != null && palette.HasShaders)
                ApplyPaletteToAllPlatformPrefabs(palette);
        }

        static void EnsureGeneratedMaterialsFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project"))
                AssetDatabase.CreateFolder("Assets", "_Project");

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Materials"))
                AssetDatabase.CreateFolder("Assets/_Project", "Materials");

            if (!AssetDatabase.IsValidFolder(GeneratedMaterialsFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Materials", "Generated");
        }

        static void EnsureDataFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project"))
                AssetDatabase.CreateFolder("Assets", "_Project");

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Data"))
                AssetDatabase.CreateFolder("Assets/_Project", "Data");
        }

        static int RemoveStonePlatformsRecursive(GameObject gameObject)
        {
            int removed = 0;

            for (int i = gameObject.transform.childCount - 1; i >= 0; i--)
                removed += RemoveStonePlatformsRecursive(gameObject.transform.GetChild(i).gameObject);

            if (IsStonePlatformObject(gameObject))
            {
                Object.DestroyImmediate(gameObject);
                return 1;
            }

            return removed;
        }

        static bool IsStonePlatformObject(GameObject gameObject)
        {
            string name = gameObject.name;
            if (name.StartsWith("Platform_Stone_", System.StringComparison.Ordinal))
                return true;

            if (!name.StartsWith("Platform_", System.StringComparison.Ordinal))
                return false;

            return gameObject.GetComponent<PlatformSpawnPoint>() != null
                && gameObject.GetComponent<MeshFilter>() != null
                && gameObject.GetComponent<Checkpoint>() == null;
        }
    }
}
