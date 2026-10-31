using System;
using UnityEngine;

namespace EchoesOfAscent.Core
{
    public readonly struct SwipeData
    {
        public Vector2 StartScreen { get; }
        public Vector2 CurrentScreen { get; }
        public Vector2 DeltaScreen { get; }
        public float Magnitude { get; }
        public float NormalizedMagnitude { get; }
        public float Duration { get; }

        /// <summary>
        /// Создаёт неизменяемую запись данных свайпа с вычисленными производными.
        /// </summary>
        /// <param name="start">Начальная экранная позиция касания.</param>
        /// <param name="current">Текущая экранная позиция.</param>
        /// <param name="maxReferencePixels">Эталонная длина свайпа для нормализации (0–1).</param>
        /// <param name="duration">Длительность свайпа в секундах.</param>
        public SwipeData(Vector2 start, Vector2 current, float maxReferencePixels, float duration)
        {
            StartScreen = start;
            CurrentScreen = current;
            DeltaScreen = current - start;
            Magnitude = DeltaScreen.magnitude;
            NormalizedMagnitude = maxReferencePixels > 0f
                ? Mathf.Clamp01(Magnitude / maxReferencePixels)
                : 0f;
            Duration = duration;
        }

        /// <summary>
        /// Проверяет, превышает ли длина свайпа минимальный порог.
        /// </summary>
        /// <param name="minSwipePixels">Минимальная длина свайпа в пикселях.</param>
        /// <returns>true, если свайп достаточно длинный для распознавания.</returns>
        public bool IsValid(float minSwipePixels)
        {
            return Magnitude >= minSwipePixels;
        }
    }

    public interface ISwipeInputProvider
    {
        event Action<SwipeData> OnSwipeUpdated;
        event Action<SwipeData> OnSwipeReleased;
        event Action OnSwipeCancelled;

        bool IsDragging { get; }
    }
}
