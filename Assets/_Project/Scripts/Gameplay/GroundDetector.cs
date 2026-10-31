using UnityEngine;

namespace EchoesOfAscent.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public sealed class GroundDetector : MonoBehaviour
    {
        [SerializeField] float checkDistance = 0.12f;
        [SerializeField] float skinWidth = 0.02f;
        [SerializeField] LayerMask groundLayers = ~0;

        Collider bodyCollider;

        void Awake()
        {
            bodyCollider = GetComponent<Collider>();
        }

        /// <summary>
        /// Проверяет, стоит ли игрок на земле (sphere cast + raycast вниз).
        /// </summary>
        /// <returns>true, если обнаружена поверхность под ногами.</returns>
        public bool IsGrounded()
        {
            return TryGetGroundHit(out _);
        }

        /// <summary>
        /// Выполняет проверку земли и возвращает данные попадания.
        /// </summary>
        /// <param name="hit">Информация о RaycastHit при успехе.</param>
        /// <returns>true, если обнаружена поверхность (не собственный коллайдер).</returns>
        public bool TryGetGroundHit(out RaycastHit hit)
        {
            var bounds = bodyCollider.bounds;
            var origin = new Vector3(bounds.center.x, bounds.min.y + skinWidth, bounds.center.z);
            float distance = checkDistance + skinWidth;
            float radius = Mathf.Clamp(Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.35f, 0.06f, 0.35f);

            if (Physics.SphereCast(
                    origin + Vector3.up * radius,
                    radius,
                    Vector3.down,
                    out hit,
                    distance + radius,
                    groundLayers,
                    QueryTriggerInteraction.Ignore)
                && hit.collider != bodyCollider)
            {
                return true;
            }

            if (Physics.Raycast(origin, Vector3.down, out hit, distance, groundLayers, QueryTriggerInteraction.Ignore)
                && hit.collider != bodyCollider)
            {
                return true;
            }

            hit = default;
            return false;
        }
    }
}
