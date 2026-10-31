using UnityEngine;

namespace EchoesOfAscent.Gameplay
{
    public sealed class PlatformSpawnPoint : MonoBehaviour
    {
        [SerializeField] Collider platformCollider;
        [SerializeField] float fallMarginBelow = 1.5f;
        [SerializeField] float edgeMargin = 0.35f;

        public Collider PlatformCollider => platformCollider;

        void Awake()
        {
            if (platformCollider == null)
                platformCollider = GetComponent<Collider>();
        }

        /// <summary>
        /// Привязывает коллайдер платформы к точке спавна.
        /// </summary>
        /// <param name="collider">Коллайдер платформы; при null используется компонент на объекте.</param>
        public void BindPlatformCollider(Collider collider)
        {
            platformCollider = collider != null ? collider : GetComponent<Collider>();
        }

        /// <summary>
        /// Вычисляет позицию спавна игрока над верхней поверхностью платформы.
        /// </summary>
        /// <param name="halfHeight">Половина высоты коллайдера игрока.</param>
        /// <returns>Мировая позиция для размещения игрока.</returns>
        public Vector3 GetSpawnPosition(float halfHeight)
        {
            Physics.SyncTransforms();
            Bounds bounds = platformCollider.bounds;
            float topY = bounds.max.y;

            if (topY <= bounds.min.y + 0.001f)
                topY = transform.position.y + platformCollider.bounds.extents.y;

            return new Vector3(bounds.center.x, topY + halfHeight + 0.02f, 0f);
        }

        /// <summary>
        /// Определяет, нужен ли респавн: игрок упал ниже платформы или соскользнул с края.
        /// </summary>
        /// <param name="playerPosition">Текущая позиция игрока.</param>
        /// <param name="velocity">Текущая скорость игрока.</param>
        /// <param name="playerHalfWidth">Половина ширины коллайдера игрока.</param>
        /// <returns>true, если игрок должен быть возвращён на точку спавна.</returns>
        public bool ShouldRespawn(Vector3 playerPosition, Vector3 velocity, float playerHalfWidth)
        {
            Physics.SyncTransforms();
            Bounds bounds = platformCollider.bounds;
            float killY = bounds.min.y - fallMarginBelow;

            if (playerPosition.y < killY)
                return true;

            if (velocity.y >= -0.1f)
                return false;

            bool outsideX = playerPosition.x < bounds.min.x - edgeMargin - playerHalfWidth
                         || playerPosition.x > bounds.max.x + edgeMargin + playerHalfWidth;
            bool belowTop = playerPosition.y <= bounds.max.y + 0.15f;

            return outsideX && belowTop;
        }
    }
}
