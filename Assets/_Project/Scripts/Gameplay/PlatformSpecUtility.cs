using UnityEngine;

namespace EchoesOfAscent.Gameplay
{
    public static class PlatformSpecUtility
    {
        /// <summary>
        /// Создаёт PlatformSpec из bounds коллайдера.
        /// </summary>
        /// <param name="collider">Коллайдер платформы.</param>
        /// <returns>Спецификация с центром и размером; default, если collider null.</returns>
        public static PlatformSpec FromCollider(Collider collider)
        {
            if (collider == null)
                return default;

            Bounds bounds = collider.bounds;
            return new PlatformSpec(bounds.center, bounds.size);
        }

        /// <summary>
        /// Оценивает PlatformSpec по записи каталога без инстанцирования префаба.
        /// </summary>
        /// <param name="entry">Запись каталога с базовым размером.</param>
        /// <param name="targetWidth">Целевая ширина платформы.</param>
        /// <param name="centerX">Целевая координата X центра.</param>
        /// <param name="topY">Целевая координата Y верхней поверхности.</param>
        /// <returns>Оценочная геометрическая спецификация платформы.</returns>
        public static PlatformSpec EstimateFromEntry(
            PlatformPrefabCatalog.Entry entry,
            float targetWidth,
            float centerX,
            float topY)
        {
            float height = entry.baseSize.y;
            float centerY = topY - height * 0.5f;
            return new PlatformSpec(new Vector3(centerX, centerY, 0f), new Vector3(targetWidth, height, 1f));
        }

        /// <summary>
        /// Оценивает PlatformSpec по префабу без инстанцирования.
        /// </summary>
        /// <param name="prefab">Префаб платформы.</param>
        /// <param name="targetWidth">Целевая ширина платформы.</param>
        /// <param name="centerX">Целевая координата X центра.</param>
        /// <param name="topY">Целевая координата Y верхней поверхности.</param>
        /// <returns>Оценочная геометрическая спецификация платформы.</returns>
        public static PlatformSpec EstimateFromPrefab(
            GameObject prefab,
            float targetWidth,
            float centerX,
            float topY)
        {
            Vector2 baseSize = ReadBaseSize(prefab);
            float height = baseSize.y;
            float centerY = topY - height * 0.5f;
            return new PlatformSpec(new Vector3(centerX, centerY, 0f), new Vector3(targetWidth, height, 1f));
        }

        /// <summary>
        /// Читает базовый размер префаба из MeshFilter или Collider.
        /// </summary>
        /// <param name="prefab">Префаб платформы.</param>
        /// <returns>Размер (ширина, высота); Vector2.one, если данные недоступны.</returns>
        public static Vector2 ReadBaseSize(GameObject prefab)
        {
            if (prefab == null)
                return Vector2.one;

            MeshFilter meshFilter = prefab.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                Vector3 size = meshFilter.sharedMesh.bounds.size;
                return new Vector2(size.x, size.y);
            }

            Collider collider = prefab.GetComponent<Collider>();
            if (collider != null)
            {
                Vector3 size = collider.bounds.size;
                return new Vector2(size.x, size.y);
            }

            return Vector2.one;
        }

        /// <summary>
        /// Сдвигает экземпляр платформы так, чтобы верхняя поверхность совпала с topY.
        /// </summary>
        /// <param name="instance">Экземпляр платформы с коллайдером.</param>
        /// <param name="centerX">Целевая координата X центра.</param>
        /// <param name="topY">Целевая координата Y верхней поверхности.</param>
        public static void AlignTopSurface(GameObject instance, float centerX, float topY)
        {
            if (instance == null)
                return;

            Physics.SyncTransforms();
            Collider collider = instance.GetComponent<BoxCollider>();
            if (collider == null)
                collider = instance.GetComponent<Collider>();
            if (collider == null)
                return;

            Bounds bounds = collider.bounds;
            Vector3 delta = new Vector3(centerX - bounds.center.x, topY - bounds.max.y, 0f);
            instance.transform.position += delta;
            Physics.SyncTransforms();
        }
    }
}
