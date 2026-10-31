using UnityEngine;

namespace EchoesOfAscent.Presentation
{
    public sealed class CameraFollowController : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector3 offset = new Vector3(0f, 2f, -10f);
        [SerializeField] float smoothTime = 0.25f;
        [SerializeField] float minX = -10f;
        [SerializeField] float maxX = 10f;
        [SerializeField] float minY = 0f;
        [SerializeField] float maxY = 22f;

        Vector3 smoothVelocity;

        void LateUpdate()
        {
            if (target == null)
                return;

            Vector3 goal = target.position + offset;
            goal.x = Mathf.Clamp(goal.x, minX, maxX);
            goal.y = Mathf.Clamp(goal.y, minY + offset.y, maxY + offset.y);
            goal.z = offset.z;

            transform.position = Vector3.SmoothDamp(transform.position, goal, ref smoothVelocity, smoothTime);
        }

        /// <summary>
        /// Устанавливает цель слежения камеры.
        /// </summary>
        /// <param name="newTarget">Transform объекта, за которым следует камера.</param>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        /// <summary>
        /// Задаёт границы перемещения камеры по осям X и Y.
        /// </summary>
        /// <param name="newMinX">Минимальная координата X.</param>
        /// <param name="newMaxX">Максимальная координата X.</param>
        /// <param name="newMinY">Минимальная координата Y.</param>
        /// <param name="newMaxY">Максимальная координата Y.</param>
        public void SetBounds(float newMinX, float newMaxX, float newMinY, float newMaxY)
        {
            minX = newMinX;
            maxX = newMaxX;
            minY = newMinY;
            maxY = newMaxY;
        }
    }
}
