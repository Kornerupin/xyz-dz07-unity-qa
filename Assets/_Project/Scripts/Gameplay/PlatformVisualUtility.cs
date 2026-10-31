using UnityEngine;

namespace EchoesOfAscent.Gameplay
{
    public static class PlatformVisualUtility
    {
        public static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        /// <summary>
        /// Применяет материал к платформе, перезапекает vertex colors и настраивает финишный оттенок.
        /// </summary>
        /// <param name="platform">Объект платформы с MeshFilter и MeshRenderer.</param>
        /// <param name="baseMaterial">Базовый материал для применения.</param>
        /// <param name="isFinish">true для окраски финишной платформы.</param>
        /// <param name="finishColor">Цвет финишной платформы.</param>
        public static void ApplyMaterial(
            GameObject platform,
            Material baseMaterial,
            bool isFinish,
            Color finishColor)
        {
            if (platform == null)
                return;

            platform.isStatic = false;

            Material materialToApply = PlatformMaterialResolver.IsUsableMaterial(baseMaterial)
                ? baseMaterial
                : PlatformMaterialResolver.Resolve();

            MeshFilter meshFilter = platform.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                Mesh mesh = Application.isPlaying ? meshFilter.mesh : meshFilter.sharedMesh;
                if (mesh != null)
                    RebakeVertexColors(mesh, materialToApply);
            }

            if (!platform.TryGetComponent(out MeshRenderer renderer))
                return;

            if (materialToApply == null)
                return;

            if (isFinish)
            {
                Material finishMaterial = new Material(materialToApply);
                finishMaterial.SetColor(BaseColorId, finishColor);
                renderer.sharedMaterial = finishMaterial;
                return;
            }

            renderer.sharedMaterial = materialToApply;
        }

        /// <summary>
        /// Перезапекает vertex colors меша в зависимости от типа шейдера материала.
        /// </summary>
        /// <param name="mesh">Целевой меш платформы.</param>
        /// <param name="material">Материал, определяющий режим запекания.</param>
        public static void RebakeVertexColors(Mesh mesh, Material material)
        {
            if (PlatformMaterialResolver.UsesStoneVertexTint(material))
                RebakeStoneVertexColors(mesh);
            else
                RebakeNeutralVertexColors(mesh);
        }

        /// <summary>
        /// Устанавливает нейтральные (белые) vertex colors для всех вершин меша.
        /// </summary>
        /// <param name="mesh">Целевой меш; игнорируется, если null или пуст.</param>
        public static void RebakeNeutralVertexColors(Mesh mesh)
        {
            if (mesh == null)
                return;

            Vector3[] vertices = mesh.vertices;
            if (vertices == null || vertices.Length == 0)
                return;

            var colors = new Color[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
                colors[i] = Color.white;

            mesh.SetColors(colors);
        }

        /// <summary>
        /// Запекает каменные vertex colors: боковые грани тёмные, верх — с шумовыми сколами.
        /// </summary>
        /// <param name="mesh">Целевой меш платформы; игнорируется, если null или пуст.</param>
        public static void RebakeStoneVertexColors(Mesh mesh)
        {
            if (mesh == null)
                return;

            Vector3[] vertices = mesh.vertices;
            if (vertices == null || vertices.Length == 0)
                return;

            Bounds bounds = mesh.bounds;
            float minY = bounds.min.y;
            float maxY = bounds.max.y;
            float height = Mathf.Max(0.001f, maxY - minY);
            float minX = bounds.min.x;
            float maxX = bounds.max.x;
            float width = Mathf.Max(0.001f, maxX - minX);
            var sideColor = new Color(0.62f, 0.56f, 0.48f, 1f);

            var colors = new Color[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                float heightT = Mathf.Clamp01((vertex.y - minY) / height);
                if (heightT < 0.72f)
                {
                    colors[i] = sideColor;
                    continue;
                }

                float topT = Mathf.Clamp01((heightT - 0.72f) / 0.28f);
                float chipWave = Mathf.PerlinNoise(vertex.x * 2.4f + 17.3f, topT * 9.1f + 3.7f);
                float chip = Mathf.Lerp(0.42f, 1f, chipWave);
                float edgeWear = 1f - Mathf.Abs((vertex.x - bounds.center.x) / (width * 0.5f));
                chip *= Mathf.Lerp(0.82f, 1f, edgeWear);

                colors[i] = new Color(chip, chip * 0.94f, chip * 0.86f, 1f);
            }

            mesh.SetColors(colors);
        }
    }
}
