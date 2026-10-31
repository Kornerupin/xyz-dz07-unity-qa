using UnityEngine;

namespace EchoesOfAscent.Presentation
{
    [CreateAssetMenu(fileName = "VisualLayerConfig", menuName = "Echoes of Ascent/Visual Layer Config")]
    public sealed class VisualLayerConfig : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Visual layer index. Aligns with checkpoint unlock order.")]
        public int layerIndex;

        [Header("Atmosphere")]
        public Color fogColor = new Color(0.55f, 0.48f, 0.42f, 1f);
        public Color ambientColor = new Color(0.65f, 0.58f, 0.5f, 1f);

        [Header("Post Processing")]
        [Range(0f, 1f)]
        public float postProcessWeight = 1f;

        [Header("Background")]
        [Tooltip("Parallax scroll speed multiplier for this layer.")]
        public float parallaxSpeed = 0.5f;
    }
}
