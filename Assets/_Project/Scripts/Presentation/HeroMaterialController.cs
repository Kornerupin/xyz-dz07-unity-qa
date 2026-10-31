using UnityEngine;

namespace EchoesOfAscent.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Renderer))]
    public sealed class HeroMaterialController : MonoBehaviour
    {
        [SerializeField] Material heroMaterial;

        void Awake()
        {
            ApplyMaterial();
        }

        void OnValidate()
        {
            ApplyMaterial();
        }

        void ApplyMaterial()
        {
            if (heroMaterial == null)
                return;

            Renderer targetRenderer = GetComponent<Renderer>();
            if (targetRenderer == null)
                return;

            targetRenderer.sharedMaterial = heroMaterial;
        }
    }
}
