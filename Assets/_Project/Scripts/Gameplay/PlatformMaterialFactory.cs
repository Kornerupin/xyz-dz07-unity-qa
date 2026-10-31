using UnityEngine;

namespace EchoesOfAscent.Gameplay
{
    public static class PlatformMaterialFactory
    {
        /// <summary>
        /// Создаёт runtime-материал на основе указанного шейдера.
        /// </summary>
        /// <param name="shader">Шейдер для нового материала.</param>
        /// <returns>Новый Material или null, если шейдер недоступен.</returns>
        public static Material CreateRuntimeMaterial(Shader shader)
        {
            if (shader == null || shader.name == "Hidden/InternalErrorShader")
                return null;

            return new Material(shader);
        }

        /// <summary>
        /// Определяет материал для спавна платформы: палитра → fallback → глобальный резолвер.
        /// </summary>
        /// <param name="platformIndex">Индекс платформы для выбора из палитры.</param>
        /// <param name="palette">Палитра шейдеров платформ.</param>
        /// <param name="fallbackMaterial">Резервный материал.</param>
        /// <returns>Готовый к применению Material.</returns>
        public static Material ResolveSpawnMaterial(
            int platformIndex,
            PlatformMaterialPalette palette,
            Material fallbackMaterial)
        {
            if (palette != null
                && palette.HasShaders
                && palette.TryCreateMaterialForPlatform(platformIndex, out Material paletteMaterial))
            {
                return paletteMaterial;
            }

            if (PlatformMaterialResolver.IsUsableMaterial(fallbackMaterial))
                return CreateRuntimeMaterial(fallbackMaterial.shader);

            return PlatformMaterialResolver.Resolve();
        }
    }
}
