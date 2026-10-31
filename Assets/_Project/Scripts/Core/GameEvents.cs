using System;

namespace EchoesOfAscent.Core
{
    public static class GameEvents
    {
        public static event Action<int> CheckpointReached;
        public static event Action<int> CheckpointTouched;
        public static event Action LevelCompleted;

        /// <summary>
        /// Вызывает подписчиков на событие достижения чекпоинта.
        /// </summary>
        /// <param name="checkpointId">ID достигнутого чекпоинта.</param>
        public static void RaiseCheckpointReached(int checkpointId)
        {
            CheckpointReached?.Invoke(checkpointId);
        }

        /// <summary>
        /// Вызывает подписчиков на событие касания чекпоинта (без сохранения).
        /// </summary>
        /// <param name="checkpointId">ID затронутого чекпоинта.</param>
        public static void RaiseCheckpointTouched(int checkpointId)
        {
            CheckpointTouched?.Invoke(checkpointId);
        }

        /// <summary>
        /// Вызывает подписчиков на событие завершения уровня.
        /// </summary>
        public static void RaiseLevelCompleted()
        {
            LevelCompleted?.Invoke();
        }
    }
}
