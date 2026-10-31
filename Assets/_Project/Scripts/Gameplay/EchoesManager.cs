using System.Collections;
using EchoesOfAscent.Meta;
using UnityEngine;

namespace EchoesOfAscent.Gameplay
{
    /// <summary>
    /// Loads seed-scoped fall echoes and spawns soft-glow markers (including local falls).
    /// Polls the shared backend so other devices' falls appear during the same session.
    /// </summary>
    [DefaultExecutionOrder(-250)]
    public sealed class EchoesManager : MonoBehaviour
    {
        [SerializeField] string echoServerBaseUrl = EchoApi.DefaultBaseUrl;
        [SerializeField] GameObject echoGlowPrefab;
        [SerializeField] Transform echoParent;
        [SerializeField] bool loadOnStart = true;
        [SerializeField] float minEchoSeparation = 0.35f;
        [SerializeField] float recordCooldownSeconds = 0.35f;
        [SerializeField] float pollIntervalSeconds = 2f;

        public static EchoesManager Instance { get; private set; }

        public string ServerBaseUrl =>
            string.IsNullOrWhiteSpace(echoServerBaseUrl) ? EchoApi.DefaultBaseUrl : echoServerBaseUrl;

        float lastRecordTime = float.NegativeInfinity;
        Coroutine pollRoutine;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Start()
        {
            if (loadOnStart)
                pollRoutine = StartCoroutine(PollEchoesLoop());
        }

        /// <summary>
        /// Spawns a local glow immediately and POSTs the fall to the backend for this level seed.
        /// </summary>
        public void RecordFall(Vector3 position)
        {
            if (Time.unscaledTime - lastRecordTime < recordCooldownSeconds)
                return;

            lastRecordTime = Time.unscaledTime;

            int seed = ResolveLevelSeed();
            if (seed == 0)
            {
                Debug.LogWarning("[EchoesManager] Level seed is not ready; fall was not posted.");
                SpawnEcho(position);
                return;
            }

            SpawnEcho(position);
            StartCoroutine(EchoApi.PostFall(ServerBaseUrl, seed, position));
        }

        /// <summary>
        /// Fetches echoes for the active level seed and instantiates glow prefabs.
        /// </summary>
        public IEnumerator LoadAndSpawnEchoes()
        {
            if (echoGlowPrefab == null)
            {
                Debug.LogWarning("[EchoesManager] echoGlowPrefab is not assigned.");
                yield break;
            }

            int seed = ResolveLevelSeed();
            if (seed == 0)
                yield break;

            EchoApi.EchoPosition[] echoes = null;
            yield return EchoApi.GetEchoes(ServerBaseUrl, seed, result => echoes = result);

            if (echoes == null || echoes.Length == 0)
                yield break;

            for (int i = 0; i < echoes.Length; i++)
            {
                if (echoes[i] == null)
                    continue;

                SpawnEcho(echoes[i].ToVector3());
            }
        }

        IEnumerator PollEchoesLoop()
        {
            while (ProceduralLevelGenerator.Current == null || !ProceduralLevelGenerator.Current.IsReady)
                yield return null;

            while (true)
            {
                yield return LoadAndSpawnEchoes();
                yield return new WaitForSecondsRealtime(Mathf.Max(0.5f, pollIntervalSeconds));
            }
        }

        void SpawnEcho(Vector3 position)
        {
            if (echoGlowPrefab == null)
                return;

            if (IsTooCloseToExistingEcho(position))
                return;

            Transform parent = echoParent != null ? echoParent : transform;
            GameObject instance = Instantiate(echoGlowPrefab, position, Quaternion.identity, parent);
            instance.name = $"Echo_{parent.childCount}";
        }

        bool IsTooCloseToExistingEcho(Vector3 position)
        {
            Transform parent = echoParent != null ? echoParent : transform;
            float minSqr = minEchoSeparation * minEchoSeparation;

            for (int i = 0; i < parent.childCount; i++)
            {
                if ((parent.GetChild(i).position - position).sqrMagnitude < minSqr)
                    return true;
            }

            return false;
        }

        static int ResolveLevelSeed()
        {
            if (ProceduralLevelGenerator.Current != null && ProceduralLevelGenerator.Current.IsReady)
                return ProceduralLevelGenerator.Current.ActiveSeed;

            if (SaveService.TryLoadLevelSeed(out int savedSeed))
                return savedSeed;

            return 0;
        }
    }
}
