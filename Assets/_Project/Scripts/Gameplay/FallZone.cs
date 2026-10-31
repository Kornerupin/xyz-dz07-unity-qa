using UnityEngine;

namespace EchoesOfAscent.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public sealed class FallZone : MonoBehaviour
    {
        void Awake()
        {
            var collider = GetComponent<Collider>();
            collider.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            RecordEcho(other.gameObject);

            var respawnController = other.GetComponent<PlayerRespawnController>();
            respawnController?.RespawnAtLastCheckpoint();
        }

        static void RecordEcho(GameObject player)
        {
            if (EchoesManager.Instance == null)
                return;

            var tracker = player.GetComponent<FallEchoTracker>();
            Vector3 echoPosition = tracker != null ? tracker.LastGroundedPosition : player.transform.position;
            EchoesManager.Instance.RecordFall(echoPosition);
        }
    }
}
