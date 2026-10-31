using UnityEngine;

namespace EchoesOfAscent.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlatformSpawnPoint))]
    public sealed class Checkpoint : MonoBehaviour
    {
        [SerializeField] int checkpointId;
        [SerializeField] PlatformSpawnPoint platform;

        public int CheckpointId => checkpointId;
        public PlatformSpawnPoint Platform => platform;

        void Awake()
        {
            if (platform == null)
                platform = GetComponent<PlatformSpawnPoint>();
        }

        /// <summary>
        /// Задаёт числовой идентификатор чекпоинта.
        /// </summary>
        /// <param name="id">Уникальный ID чекпоинта в уровне.</param>
        public void Configure(int id)
        {
            checkpointId = id;
        }

        /// <summary>
        /// Активирует чекпоинт и регистрирует его в контроллере респавна игрока.
        /// </summary>
        /// <param name="respawnController">Контроллер респавна игрока.</param>
        public void Activate(PlayerRespawnController respawnController)
        {
            if (respawnController == null)
                return;

            respawnController.RegisterCheckpoint(this);
        }
    }
}
