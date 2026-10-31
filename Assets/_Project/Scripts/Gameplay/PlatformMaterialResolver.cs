using UnityEngine;

namespace EchoesOfAscent.Gameplay
{
    public static class PlatformMaterialResolver
    {
        public const string PreferredShaderName = "EchoesOfAscent/GeneratedVertexColor";
        public const string FallbackShaderName = "EchoesOfAscent/PlatformStone2D";
        public const string PlatformMaterialName = "PlatformStone2D";

        /// <summary>
        /// Возвращает назначенный материал, если он пригоден, иначе ищет подходящий глобально.
        /// </summary>
        /// <param name="assignedMaterial">Предпочтительный материал (опционально).</param>
        /// <returns>Готовый Material или null, если шейдер не найден.</returns>
        public static Material Resolve(Material assignedMaterial = null)
        {
            if (IsUsableMaterial(assignedMaterial))
                return assignedMaterial;

            return Resolve();
        }

        /// <summary>
        /// Ищет именованный или канонический шейдер платформы и создаёт runtime-материал.
        /// </summary>
        /// <returns>Material платформы или null, если шейдер недоступен.</returns>
        public static Material Resolve()
        {
            Material namedMaterial = FindMaterialByName(PlatformMaterialName);
            if (IsUsableMaterial(namedMaterial))
                return namedMaterial;

            Shader shader = FindPlatformShader();
            if (shader == null)
                return null;

            return new Material(shader) { name = "PlatformRuntimeMaterial" };
        }

        /// <summary>
        /// Находит предпочтительный или резервный шейдер платформы EchoesOfAscent.
        /// </summary>
        /// <returns>Shader платформы или null, если ни один не найден.</returns>
        public static Shader FindPlatformShader()
        {
            Shader[] shaders = Resources.FindObjectsOfTypeAll<Shader>();
            Shader canonical = null;
            Shader fallback = null;

            for (int i = 0; i < shaders.Length; i++)
            {
                Shader shader = shaders[i];
                string shaderName = shader.name;

                if (shaderName == PreferredShaderName)
                    canonical = shader;

                if (shaderName == FallbackShaderName)
                    fallback = shader;
            }

            if (canonical != null)
                return canonical;

            return fallback;
        }

        /// <summary>
        /// Проверяет, использует ли материал каменный vertex tint (PlatformStone2D).
        /// </summary>
        /// <param name="material">Проверяемый материал.</param>
        /// <returns>true, если шейдер — PlatformStone2D.</returns>
        public static bool UsesStoneVertexTint(Material material)
        {
            if (material == null || material.shader == null)
                return false;

            return material.shader.name == FallbackShaderName;
        }

        /// <summary>
        /// Проверяет, пригоден ли материал для платформ проекта.
        /// </summary>
        /// <param name="material">Проверяемый материал.</param>
        /// <returns>true, если материал имеет валидный шейдер EchoesOfAscent.</returns>
        public static bool IsUsableMaterial(Material material)
        {
            if (material == null || material.shader == null)
                return false;

            string shaderName = material.shader.name;
            if (shaderName == "Hidden/InternalErrorShader")
                return false;

            return shaderName.StartsWith("EchoesOfAscent/")
                || shaderName.Contains("GeneratedVertexColor")
                || shaderName.Contains("PlatformStone2D");
        }

        static Material FindMaterialByName(string materialName)
        {
            Material[] materials = Resources.FindObjectsOfTypeAll<Material>();
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i].name == materialName)
                    return materials[i];
            }

            return null;
        }
    }
}
