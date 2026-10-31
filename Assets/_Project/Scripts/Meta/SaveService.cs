using UnityEngine;

namespace EchoesOfAscent.Meta
{
    public static class SaveService
    {
        const string CheckpointKey = "echoes_checkpoint_id";
        const string LevelSeedKey = "echoes_level_seed";

        /// <summary>
        /// Сохраняет ID последнего достигнутого чекпоинта в PlayerPrefs.
        /// </summary>
        /// <param name="checkpointId">ID чекпоинта для сохранения.</param>
        public static void SaveCheckpoint(int checkpointId)
        {
            PlayerPrefs.SetInt(CheckpointKey, checkpointId);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Загружает сохранённый ID чекпоинта из PlayerPrefs.
        /// </summary>
        /// <returns>ID чекпоинта; 0, если сохранение отсутствует.</returns>
        public static int LoadCheckpoint()
        {
            return PlayerPrefs.GetInt(CheckpointKey, 0);
        }

        /// <summary>
        /// Удаляет сохранённый прогресс чекпоинта.
        /// </summary>
        public static void ResetProgress()
        {
            PlayerPrefs.DeleteKey(CheckpointKey);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Сохраняет seed процедурного уровня для повторной генерации и эхо.
        /// </summary>
        public static void SaveLevelSeed(int seed)
        {
            PlayerPrefs.SetInt(LevelSeedKey, seed);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Пытается загрузить сохранённый seed уровня.
        /// </summary>
        public static bool TryLoadLevelSeed(out int seed)
        {
            if (!PlayerPrefs.HasKey(LevelSeedKey))
            {
                seed = 0;
                return false;
            }

            seed = PlayerPrefs.GetInt(LevelSeedKey, 0);
            return seed != 0;
        }

        /// <summary>
        /// Удаляет сохранённый seed уровня (следующий запуск сгенерирует новый layout).
        /// </summary>
        public static void ClearLevelSeed()
        {
            PlayerPrefs.DeleteKey(LevelSeedKey);
            PlayerPrefs.Save();
        }
    }
}
