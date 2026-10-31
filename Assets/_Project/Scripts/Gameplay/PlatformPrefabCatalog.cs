using System;
using System.Collections.Generic;
using UnityEngine;

namespace EchoesOfAscent.Gameplay
{
    [CreateAssetMenu(
        fileName = "PlatformPrefabCatalog",
        menuName = "Echoes Of Ascent/Platform Prefab Catalog")]
    public sealed class PlatformPrefabCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public GameObject prefab;
            public Vector2 baseSize;
            public int paletteShaderIndex;
        }

        [SerializeField] List<Entry> entries = new List<Entry>();

        public IReadOnlyList<Entry> Entries => entries;
        public bool HasPrefabs => GetValidEntryCount() > 0;

        /// <summary>
        /// Выбирает случайную валидную запись каталога с помощью Unity Random.
        /// </summary>
        /// <returns>Случайная запись с непустым префабом.</returns>
        /// <exception cref="InvalidOperationException">Если в каталоге нет валидных префабов.</exception>
        public Entry PickRandomEntry()
        {
            int validCount = GetValidEntryCount();
            if (validCount == 0)
                throw new InvalidOperationException("Platform prefab catalog has no valid prefab references.");

            int pick = UnityEngine.Random.Range(0, validCount);
            return GetValidEntryAt(pick);
        }

        /// <summary>
        /// Выбирает случайную валидную запись каталога с помощью заданного генератора.
        /// </summary>
        /// <param name="rng">Детерминированный генератор случайных чисел.</param>
        /// <returns>Случайная запись с непустым префабом.</returns>
        /// <exception cref="InvalidOperationException">Если в каталоге нет валидных префабов.</exception>
        public Entry PickRandomEntry(System.Random rng)
        {
            int validCount = GetValidEntryCount();
            if (validCount == 0)
                throw new InvalidOperationException("Platform prefab catalog has no valid prefab references.");

            return GetValidEntryAt(rng.Next(0, validCount));
        }

        /// <summary>
        /// Инстанцирует префаб платформы, масштабирует по ширине, выравнивает и назначает материал.
        /// </summary>
        /// <param name="parent">Родительский Transform для нового объекта.</param>
        /// <param name="entry">Запись каталога с префабом и базовым размером.</param>
        /// <param name="targetWidth">Целевая ширина платформы в мировых единицах.</param>
        /// <param name="centerX">Целевая координата X центра платформы.</param>
        /// <param name="topY">Целевая координата Y верхней поверхности.</param>
        /// <param name="platformIndex">Индекс платформы для выбора материала из палитры.</param>
        /// <param name="palette">Палитра материалов платформ (может быть null).</param>
        /// <param name="fallbackMaterial">Резервный материал, если палитра недоступна.</param>
        /// <param name="isFinish">true для финишной платформы с особым цветом.</param>
        /// <param name="finishColor">Цвет финишной платформы.</param>
        /// <param name="spec">Выходная геометрическая спецификация созданной платформы.</param>
        /// <returns>Созданный экземпляр GameObject платформы.</returns>
        public GameObject InstantiateScaled(
            Transform parent,
            Entry entry,
            float targetWidth,
            float centerX,
            float topY,
            int platformIndex,
            PlatformMaterialPalette palette,
            Material fallbackMaterial,
            bool isFinish,
            Color finishColor,
            out PlatformSpec spec)
        {
            if (entry.prefab == null)
            {
                throw new MissingReferenceException(
                    "Platform prefab catalog entry is missing. Run Ascent Tools > Refresh Platform Catalog in the editor.");
            }

            GameObject instance = Instantiate(entry.prefab, parent);
            float scaleX = targetWidth / Mathf.Max(0.01f, entry.baseSize.x);
            instance.transform.localScale = new Vector3(scaleX, 1f, 1f);
            instance.transform.position = Vector3.zero;

            Collider gameplayCollider = PlatformColliderUtility.ConfigureGameplayCollider(instance);
            PlatformSpecUtility.AlignTopSurface(instance, centerX, topY);

            Material spawnMaterial = PlatformMaterialFactory.ResolveSpawnMaterial(
                platformIndex,
                palette,
                fallbackMaterial);

            PlatformVisualUtility.ApplyMaterial(instance, spawnMaterial, isFinish, finishColor);
            spec = PlatformSpecUtility.FromCollider(gameplayCollider);
            return instance;
        }

        int GetValidEntryCount()
        {
            if (entries == null)
                return 0;

            int count = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].prefab != null)
                    count++;
            }

            return count;
        }

        Entry GetValidEntryAt(int validIndex)
        {
            int seen = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].prefab == null)
                    continue;

                if (seen == validIndex)
                    return entries[i];

                seen++;
            }

            throw new InvalidOperationException("Platform prefab catalog index is out of range.");
        }

#if UNITY_EDITOR
        /// <summary>
        /// Заменяет все записи каталога; автоматически вычисляет baseSize при необходимости.
        /// </summary>
        /// <param name="newEntries">Новый список записей каталога.</param>
        public void ReplaceEntries(IReadOnlyList<Entry> newEntries)
        {
            entries.Clear();
            if (newEntries == null)
                return;

            for (int i = 0; i < newEntries.Count; i++)
            {
                Entry entry = newEntries[i];
                if (entry.prefab == null)
                    continue;

                if (entry.baseSize.x <= 0.01f || entry.baseSize.y <= 0.01f)
                    entry.baseSize = PlatformSpecUtility.ReadBaseSize(entry.prefab);

                entries.Add(entry);
            }

            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
