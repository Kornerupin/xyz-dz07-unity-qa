using System.Collections;
using EchoesOfAscent.Core;
using EchoesOfAscent.Meta;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EchoesOfAscent.Presentation
{
    [DefaultExecutionOrder(100)]
    public sealed class VisualLayerController : MonoBehaviour
    {
        [SerializeField] VisualLayerConfig[] layerConfigs;
        [SerializeField] float blendDuration = 1.5f;
        [SerializeField] Volume localPostProcessVolume;
        [SerializeField] Camera targetCamera;
        [SerializeField] Light directionalLight;
        [SerializeField] bool enableFogOnAwake = true;
        [SerializeField] bool loadSavedLayerOnStart;
        [SerializeField] int maxCheckpointId = 10;

        Coroutine blendRoutine;
        ColorAdjustments colorAdjustments;
        Bloom bloom;
        Vignette vignette;
        int activeLayerIndex = -1;

        void Awake()
        {
            if (localPostProcessVolume == null)
                localPostProcessVolume = GetComponent<Volume>();

            if (targetCamera == null)
                targetCamera = Camera.main;

            if (directionalLight == null)
                directionalLight = FindFirstObjectByType<Light>();

            if (enableFogOnAwake)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
            }

            EnsureVolumeOverrides();
        }

        void OnEnable()
        {
            GameEvents.CheckpointTouched += HandleCheckpointTouched;
        }

        void Start()
        {
            int layerIndex = loadSavedLayerOnStart ? SaveService.LoadCheckpoint() : 0;
            VisualLayerConfig initialConfig = FindConfigByLayerIndex(layerIndex) ?? FindConfigByLayerIndex(0);
            if (initialConfig != null)
                ApplyImmediately(initialConfig);
        }

        void OnDisable()
        {
            GameEvents.CheckpointTouched -= HandleCheckpointTouched;
        }

        void HandleCheckpointTouched(int checkpointId)
        {
            VisualLayerConfig targetConfig = FindConfigByLayerIndex(MapCheckpointToLayer(checkpointId));
            if (targetConfig == null)
                return;

            if (targetConfig.layerIndex == activeLayerIndex)
                return;

            BlendToConfig(targetConfig);
        }

        void EnsureVolumeOverrides()
        {
            if (localPostProcessVolume == null)
                return;

            VolumeProfile profile = localPostProcessVolume.profile;

            if (!profile.TryGet(out colorAdjustments))
                colorAdjustments = profile.Add<ColorAdjustments>(false);
            colorAdjustments.active = true;
            colorAdjustments.colorFilter.overrideState = true;
            colorAdjustments.saturation.overrideState = true;

            if (!profile.TryGet(out bloom))
                bloom = profile.Add<Bloom>(false);
            bloom.active = true;
            bloom.tint.overrideState = true;

            if (!profile.TryGet(out vignette))
                vignette = profile.Add<Vignette>(false);
            vignette.active = true;
            vignette.color.overrideState = true;
            vignette.intensity.overrideState = true;
        }

        void BlendToConfig(VisualLayerConfig targetConfig)
        {
            if (blendRoutine != null)
                StopCoroutine(blendRoutine);

            blendRoutine = StartCoroutine(BlendRoutine(targetConfig));
        }

        IEnumerator BlendRoutine(VisualLayerConfig targetConfig)
        {
            AtmosphereState start = CaptureCurrentState();
            AtmosphereState target = CreateStateFromConfig(targetConfig);
            float duration = Mathf.Max(0.01f, blendDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                ApplyState(LerpState(start, target, t));
                yield return null;
            }

            ApplyState(target);
            activeLayerIndex = targetConfig.layerIndex;
            blendRoutine = null;
        }

        void ApplyImmediately(VisualLayerConfig config)
        {
            if (blendRoutine != null)
            {
                StopCoroutine(blendRoutine);
                blendRoutine = null;
            }

            ApplyState(CreateStateFromConfig(config));
            activeLayerIndex = config.layerIndex;
        }

        int MapCheckpointToLayer(int checkpointId)
        {
            if (layerConfigs == null || layerConfigs.Length == 0)
                return 0;

            int maxLayer = layerConfigs.Length - 1;
            if (maxCheckpointId <= 0)
                return Mathf.Clamp(checkpointId, 0, maxLayer);

            return Mathf.Clamp(Mathf.RoundToInt((float)checkpointId / maxCheckpointId * maxLayer), 0, maxLayer);
        }

        VisualLayerConfig FindConfigByLayerIndex(int layerIndex)
        {
            if (layerConfigs == null)
                return null;

            for (int i = 0; i < layerConfigs.Length; i++)
            {
                if (layerConfigs[i] != null && layerConfigs[i].layerIndex == layerIndex)
                    return layerConfigs[i];
            }

            if (layerIndex >= 0 && layerIndex < layerConfigs.Length)
                return layerConfigs[layerIndex];

            return null;
        }

        AtmosphereState CaptureCurrentState()
        {
            return new AtmosphereState
            {
                FogColor = RenderSettings.fogColor,
                FogDensity = RenderSettings.fogDensity,
                AmbientSky = RenderSettings.ambientSkyColor,
                AmbientEquator = RenderSettings.ambientEquatorColor,
                AmbientGround = RenderSettings.ambientGroundColor,
                CameraBackground = targetCamera != null ? targetCamera.backgroundColor : Color.black,
                LightColor = directionalLight != null ? directionalLight.color : Color.white,
                PostProcessWeight = localPostProcessVolume != null ? localPostProcessVolume.weight : 0f,
                ColorFilter = colorAdjustments != null ? colorAdjustments.colorFilter.value : Color.white,
                Saturation = colorAdjustments != null ? colorAdjustments.saturation.value : 0f,
                BloomTint = bloom != null ? bloom.tint.value : Color.white,
                VignetteColor = vignette != null ? vignette.color.value : Color.black,
                VignetteIntensity = vignette != null ? vignette.intensity.value : 0f
            };
        }

        static AtmosphereState CreateStateFromConfig(VisualLayerConfig config)
        {
            Color ambient = config.ambientColor;
            Color fog = config.fogColor;
            float layerT = Mathf.Clamp01(config.layerIndex / 3f);

            return new AtmosphereState
            {
                FogColor = fog,
                FogDensity = Mathf.Lerp(0.02f, 0.08f, layerT),
                AmbientSky = ambient,
                AmbientEquator = Color.Lerp(ambient, fog, 0.35f),
                AmbientGround = Color.Lerp(ambient, fog * 0.7f, 0.55f),
                CameraBackground = Color.Lerp(fog, ambient, 0.15f + layerT * 0.2f),
                LightColor = Color.Lerp(new Color(1f, 0.9f, 0.78f), new Color(0.82f, 0.9f, 1f), layerT),
                PostProcessWeight = Mathf.Clamp01(config.postProcessWeight),
                ColorFilter = Color.Lerp(Color.white, ambient, 0.45f + layerT * 0.35f),
                Saturation = Mathf.Lerp(-10f, 18f, layerT),
                BloomTint = Color.Lerp(ambient, fog, 0.5f),
                VignetteColor = Color.Lerp(fog * 0.5f, ambient * 0.35f, layerT),
                VignetteIntensity = Mathf.Lerp(0.18f, 0.32f, layerT)
            };
        }

        static AtmosphereState LerpState(AtmosphereState from, AtmosphereState to, float t)
        {
            return new AtmosphereState
            {
                FogColor = Color.Lerp(from.FogColor, to.FogColor, t),
                FogDensity = Mathf.Lerp(from.FogDensity, to.FogDensity, t),
                AmbientSky = Color.Lerp(from.AmbientSky, to.AmbientSky, t),
                AmbientEquator = Color.Lerp(from.AmbientEquator, to.AmbientEquator, t),
                AmbientGround = Color.Lerp(from.AmbientGround, to.AmbientGround, t),
                CameraBackground = Color.Lerp(from.CameraBackground, to.CameraBackground, t),
                LightColor = Color.Lerp(from.LightColor, to.LightColor, t),
                PostProcessWeight = Mathf.Lerp(from.PostProcessWeight, to.PostProcessWeight, t),
                ColorFilter = Color.Lerp(from.ColorFilter, to.ColorFilter, t),
                Saturation = Mathf.Lerp(from.Saturation, to.Saturation, t),
                BloomTint = Color.Lerp(from.BloomTint, to.BloomTint, t),
                VignetteColor = Color.Lerp(from.VignetteColor, to.VignetteColor, t),
                VignetteIntensity = Mathf.Lerp(from.VignetteIntensity, to.VignetteIntensity, t)
            };
        }

        void ApplyState(AtmosphereState state)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = state.FogColor;
            RenderSettings.fogDensity = state.FogDensity;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = state.AmbientSky;
            RenderSettings.ambientEquatorColor = state.AmbientEquator;
            RenderSettings.ambientGroundColor = state.AmbientGround;

            if (targetCamera != null)
            {
                targetCamera.clearFlags = CameraClearFlags.SolidColor;
                targetCamera.backgroundColor = state.CameraBackground;
            }

            if (directionalLight != null)
                directionalLight.color = state.LightColor;

            if (localPostProcessVolume != null)
                localPostProcessVolume.weight = state.PostProcessWeight;

            if (colorAdjustments != null)
            {
                colorAdjustments.colorFilter.value = state.ColorFilter;
                colorAdjustments.saturation.value = state.Saturation;
            }

            if (bloom != null)
                bloom.tint.value = state.BloomTint;

            if (vignette != null)
            {
                vignette.color.value = state.VignetteColor;
                vignette.intensity.value = state.VignetteIntensity;
            }
        }

        struct AtmosphereState
        {
            public Color FogColor;
            public float FogDensity;
            public Color AmbientSky;
            public Color AmbientEquator;
            public Color AmbientGround;
            public Color CameraBackground;
            public Color LightColor;
            public float PostProcessWeight;
            public Color ColorFilter;
            public float Saturation;
            public Color BloomTint;
            public Color VignetteColor;
            public float VignetteIntensity;
        }
    }
}
