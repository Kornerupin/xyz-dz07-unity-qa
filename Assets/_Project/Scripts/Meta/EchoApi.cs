using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace EchoesOfAscent.Meta
{
    /// <summary>
    /// Shared DTOs and UnityWebRequest helpers for the fall-echo backend.
    /// </summary>
    public static class EchoApi
    {
        /// <summary>
        /// Default LAN backend URL for multi-device play. Override in Inspector if your PC IP differs.
        /// </summary>
        public const string DefaultBaseUrl = "http://192.168.1.11:3000";

        [Serializable]
        public sealed class EchoPosition
        {
            public int seed;
            public float x;
            public float y;
            public float z;

            public EchoPosition()
            {
            }

            public EchoPosition(int levelSeed, Vector3 position)
            {
                seed = levelSeed;
                x = position.x;
                y = position.y;
                z = position.z;
            }

            public Vector3 ToVector3()
            {
                return new Vector3(x, y, z);
            }
        }

        [Serializable]
        public sealed class EchoListResponse
        {
            public EchoPosition[] echoes;
        }

        [Serializable]
        public sealed class SessionResponse
        {
            public int seed;
        }

        /// <summary>
        /// Fetches the shared level seed from the backend session endpoint.
        /// </summary>
        public static IEnumerator GetSharedSeed(string baseUrl, Action<int> onComplete)
        {
            string url = Combine(baseUrl, "/api/session");

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 5;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[EchoApi] GET /api/session failed: {request.error}");
                    onComplete?.Invoke(0);
                    yield break;
                }

                SessionResponse response = JsonUtility.FromJson<SessionResponse>(request.downloadHandler.text);
                int seed = response != null ? response.seed : 0;
                onComplete?.Invoke(seed);
            }
        }

        /// <summary>
        /// POSTs a fall coordinate scoped to a level seed. Failures are logged and ignored.
        /// </summary>
        public static IEnumerator PostFall(string baseUrl, int levelSeed, Vector3 position)
        {
            string url = Combine(baseUrl, "/api/fall");
            string json = JsonUtility.ToJson(new EchoPosition(levelSeed, position));
            byte[] body = Encoding.UTF8.GetBytes(json);

            using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 5;

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                    Debug.LogWarning($"[EchoApi] POST /api/fall failed: {request.error}");
            }
        }

        /// <summary>
        /// GETs fall echoes for a level seed. On failure yields an empty array.
        /// </summary>
        public static IEnumerator GetEchoes(string baseUrl, int levelSeed, Action<EchoPosition[]> onComplete)
        {
            string url = Combine(baseUrl, $"/api/echoes?seed={levelSeed}");

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 5;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[EchoApi] GET /api/echoes failed: {request.error}");
                    onComplete?.Invoke(Array.Empty<EchoPosition>());
                    yield break;
                }

                EchoListResponse response = JsonUtility.FromJson<EchoListResponse>(request.downloadHandler.text);
                EchoPosition[] echoes = response != null && response.echoes != null
                    ? response.echoes
                    : Array.Empty<EchoPosition>();
                onComplete?.Invoke(echoes);
            }
        }

        static string Combine(string baseUrl, string path)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                baseUrl = DefaultBaseUrl;

            return baseUrl.TrimEnd('/') + path;
        }
    }
}
