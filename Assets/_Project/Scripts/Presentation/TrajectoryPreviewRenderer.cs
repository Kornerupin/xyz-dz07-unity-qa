using UnityEngine;

namespace EchoesOfAscent.Presentation
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class TrajectoryPreviewRenderer : MonoBehaviour
    {
        [SerializeField] Color lineColor = new Color(0.85f, 0.75f, 0.55f, 0.9f);
        [SerializeField] float lineWidth = 0.06f;

        LineRenderer lineRenderer;

        void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.loop = false;
            lineRenderer.widthMultiplier = lineWidth;
            lineRenderer.positionCount = 0;
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            Hide();
        }

        /// <summary>
        /// Отображает линию траектории по массиву мировых точек.
        /// </summary>
        /// <param name="points">Точки траектории; минимум 2 для отображения.</param>
        public void Show(Vector3[] points)
        {
            if (points == null || points.Length < 2)
            {
                Hide();
                return;
            }

            lineRenderer.positionCount = points.Length;
            lineRenderer.SetPositions(points);
            lineRenderer.enabled = true;
        }

        /// <summary>
        /// Скрывает превью траектории и очищает LineRenderer.
        /// </summary>
        public void Hide()
        {
            lineRenderer.positionCount = 0;
            lineRenderer.enabled = false;
        }
    }
}
