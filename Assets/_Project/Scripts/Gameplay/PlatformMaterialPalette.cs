using System;
using System.Collections.Generic;
using UnityEngine;

namespace EchoesOfAscent.Gameplay
{
    [CreateAssetMenu(
        fileName = "PlatformMaterialPalette",
        menuName = "Echoes Of Ascent/Platform Material Palette")]
    public sealed class PlatformMaterialPalette : ScriptableObject
    {
        [SerializeField] List<Shader> shaders = new List<Shader>();

        public IReadOnlyList<Shader> Shaders => shaders;
        public bool HasShaders => GetValidShaderCount() > 0;

        /// <summary>
        /// Нормализует индекс платформы в допустимый диапазон [0, validCount).
        /// </summary>
        /// <param name="platformIndex">Исходный индекс платформы (может быть отрицательным).</param>
        /// <param name="validCount">Количество валидных шейдеров в палитре.</param>
        /// <returns>Нормализованный индекс или 0, если validCount &lt;= 0.</returns>
        public static int NormalizeIndex(int platformIndex, int validCount)
        {
            if (validCount <= 0)
                return 0;

            int index = platformIndex % validCount;
            return index < 0 ? index + validCount : index;
        }

        /// <summary>
        /// Создаёт runtime-материал для платформы по её индексу с учётом циклического выбора шейдера.
        /// </summary>
        /// <param name="platformIndex">Индекс платформы в уровне.</param>
        /// <param name="material">Созданный материал при успехе; иначе null.</param>
        /// <returns>true, если материал успешно создан.</returns>
        public bool TryCreateMaterialForPlatform(int platformIndex, out Material material)
        {
            int shaderCount = shaders?.Count ?? 0;
            return TryCreateMaterialAtIndex(NormalizeIndex(platformIndex, shaderCount), out material);
        }

        /// <summary>
        /// Создаёт runtime-материал по индексу шейдера в палитре.
        /// </summary>
        /// <param name="shaderIndex">Индекс шейдера в списке палитры.</param>
        /// <param name="material">Созданный материал при успехе; иначе null.</param>
        /// <returns>true, если шейдер найден и материал создан.</returns>
        public bool TryCreateMaterialAtIndex(int shaderIndex, out Material material)
        {
            material = null;
            Shader shader = GetShaderAt(shaderIndex);
            if (shader == null)
                return false;

            material = PlatformMaterialFactory.CreateRuntimeMaterial(shader);
            return material != null;
        }

        /// <summary>
        /// Возвращает шейдер по индексу с циклической нормализацией.
        /// </summary>
        /// <param name="shaderIndex">Индекс шейдера в палитре.</param>
        /// <returns>Валидный шейдер или null, если палитра пуста или шейдер недоступен.</returns>
        public Shader GetShaderAt(int shaderIndex)
        {
            if (shaders == null || shaders.Count == 0)
                return null;

            int index = NormalizeIndex(shaderIndex, shaders.Count);
            Shader shader = shaders[index];
            return IsValidShader(shader) ? shader : null;
        }

        /// <summary>
        /// Проверяет, содержит ли палитра указанный шейдер.
        /// </summary>
        /// <param name="shader">Проверяемый шейдер.</param>
        /// <returns>true, если шейдер присутствует и валиден.</returns>
        public bool ContainsShader(Shader shader)
        {
            if (!IsValidShader(shader) || shaders == null)
                return false;

            for (int i = 0; i < shaders.Count; i++)
            {
                if (shaders[i] == shader)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Добавляет шейдер в палитру, если он валиден и ещё не присутствует.
        /// </summary>
        /// <param name="shader">Шейдер для добавления.</param>
        public void AppendShader(Shader shader)
        {
            if (!IsValidShader(shader) || ContainsShader(shader))
                return;

            shaders.Add(shader);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        int GetValidShaderCount()
        {
            if (shaders == null)
                return 0;

            int count = 0;
            for (int i = 0; i < shaders.Count; i++)
            {
                if (IsValidShader(shaders[i]))
                    count++;
            }

            return count;
        }

        static bool IsValidShader(Shader shader)
        {
            return shader != null && shader.name != "Hidden/InternalErrorShader";
        }

        /// <summary>
        /// Полностью заменяет содержимое палитры новым списком шейдеров.
        /// </summary>
        /// <param name="newShaders">Новый список шейдеров; null очищает палитру.</param>
        public void ReplaceShaders(IReadOnlyList<Shader> newShaders)
        {
            shaders.Clear();
            if (newShaders == null)
                return;

            for (int i = 0; i < newShaders.Count; i++)
                AppendShader(newShaders[i]);
        }
    }
}
