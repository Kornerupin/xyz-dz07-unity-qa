using UnityEngine;

namespace EchoesOfAscent.Gameplay
{
    public static class PlatformColliderUtility
    {
        /// <summary>
        /// Настраивает BoxCollider по bounds меша и удаляет MeshCollider для геймплея.
        /// </summary>
        /// <param name="platform">Объект платформы с MeshFilter.</param>
        /// <returns>Настроенный BoxCollider или существующий Collider, если меш отсутствует.</returns>
        public static Collider ConfigureGameplayCollider(GameObject platform)
        {
            if (platform == null)
                return null;

            MeshFilter meshFilter = platform.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
                return platform.GetComponent<Collider>();

            Bounds meshBounds = meshFilter.sharedMesh.bounds;

            BoxCollider boxCollider = platform.GetComponent<BoxCollider>();
            if (boxCollider == null)
                boxCollider = platform.AddComponent<BoxCollider>();

            boxCollider.center = meshBounds.center;
            boxCollider.size = meshBounds.size;
            boxCollider.isTrigger = false;
            boxCollider.enabled = true;

            if (platform.TryGetComponent(out PlatformSpawnPoint spawnPoint))
                spawnPoint.BindPlatformCollider(boxCollider);

            MeshCollider meshCollider = platform.GetComponent<MeshCollider>();
            if (meshCollider != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(meshCollider);
                else
                    Object.DestroyImmediate(meshCollider);
            }

            Physics.SyncTransforms();
            return boxCollider;
        }
    }
}
