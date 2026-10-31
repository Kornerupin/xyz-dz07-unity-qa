using EchoesOfAscent.Core;
using UnityEngine;

namespace EchoesOfAscent.Gameplay
{
    public sealed class JumpCalculator
    {
        const float GameplayPlaneZ = 0f;

        /// <summary>
        /// Преобразует свайп на экране в начальную скорость прыжка в мировых координатах.
        /// </summary>
        /// <param name="swipe">Данные свайпа (начало, текущая позиция, величина).</param>
        /// <param name="config">Конфигурация прыжка с диапазоном импульса и углов.</param>
        /// <param name="camera">Камера для проекции экранных координат в мир.</param>
        /// <returns>Вектор начальной скорости; Vector3.zero при невалидном вводе.</returns>
        public Vector3 ScreenDeltaToLaunchVelocity(SwipeData swipe, JumpConfig config, Camera camera)
        {
            if (config == null || camera == null)
                return Vector3.zero;

            float depth = GetScreenDepth(camera);
            Vector3 startWorld = ScreenToGameplayPoint(swipe.StartScreen, camera, depth);
            Vector3 endWorld = ScreenToGameplayPoint(swipe.CurrentScreen, camera, depth);

            Vector3 worldPull = startWorld - endWorld;
            worldPull.z = 0f;
            if (worldPull.sqrMagnitude < 0.001f)
                return Vector3.zero;

            Vector3 direction = ClampLaunchDirection(worldPull.normalized, config);
            if (direction.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            return direction * EvaluateImpulse(swipe, config);
        }

        /// <summary>
        /// Вычисляет величину импульса прыжка по нормализованной длине свайпа.
        /// </summary>
        /// <param name="swipe">Данные свайпа с нормализованной величиной.</param>
        /// <param name="config">Конфигурация с minImpulse и maxImpulse.</param>
        /// <returns>Импульс в диапазоне [minImpulse, maxImpulse].</returns>
        public float EvaluateImpulse(SwipeData swipe, JumpConfig config)
        {
            float t = Mathf.Clamp01(swipe.NormalizedMagnitude);
            return Mathf.Lerp(config.minImpulse, config.maxImpulse, t);
        }

        /// <summary>
        /// Симулирует траекторию полёта с учётом гравитации и возвращает точки для отрисовки.
        /// </summary>
        /// <param name="origin">Начальная позиция в мире.</param>
        /// <param name="initialVelocity">Начальная скорость.</param>
        /// <param name="config">Конфигурация с шагом и лимитом времени симуляции.</param>
        /// <returns>Массив точек траектории; пустой массив при невалидных параметрах.</returns>
        public Vector3[] SampleTrajectory(Vector3 origin, Vector3 initialVelocity, JumpConfig config)
        {
            if (config == null || initialVelocity.y < 0f)
                return System.Array.Empty<Vector3>();

            int maxSteps = Mathf.Max(2, config.trajectorySteps);
            var points = new Vector3[maxSteps];
            Vector3 gravity = Physics.gravity;
            Vector3 position = origin;
            Vector3 velocity = initialVelocity;
            float elapsed = 0f;
            int count = 0;

            for (int i = 0; i < maxSteps && elapsed < config.trajectoryMaxSimTime; i++)
            {
                points[count++] = position;
                velocity += gravity * config.trajectoryTimeStep;
                position += velocity * config.trajectoryTimeStep;
                elapsed += config.trajectoryTimeStep;

                if (position.y < origin.y - 10f)
                    break;
            }

            if (count == points.Length)
                return points;

            var trimmed = new Vector3[count];
            for (int i = 0; i < count; i++)
                trimmed[i] = points[i];

            return trimmed;
        }

        static float GetScreenDepth(Camera camera)
        {
            return Mathf.Abs(camera.transform.position.z - GameplayPlaneZ);
        }

        static Vector3 ScreenToGameplayPoint(Vector2 screenPoint, Camera camera, float depth)
        {
            Vector3 world = camera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, depth));
            world.z = GameplayPlaneZ;
            return world;
        }

        /// <summary>
        /// Ограничивает направление запуска: только влево/вправо с восходящим углом; вниз запрещён.
        /// </summary>
        /// <param name="direction">Ненормализованное направление из свайпа.</param>
        /// <param name="config">Конфигурация с minLaunchElevation и maxLaunchAngle.</param>
        /// <returns>Нормализованное направление в плоскости XY; Vector3.zero, если ввод невалиден.</returns>
        public static Vector3 ClampLaunchDirection(Vector3 direction, JumpConfig config)
        {
            float minElevation = config.minLaunchElevation;
            float maxElevation = config.maxLaunchAngle;

            Vector2 flat = new Vector2(direction.x, direction.y);
            if (flat.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            if (flat.y <= 0f)
                return Vector3.zero;

            float signX = Mathf.Sign(flat.x);
            if (Mathf.Approximately(signX, 0f))
                return Vector3.zero;

            float elevation = Mathf.Atan2(flat.y, Mathf.Abs(flat.x)) * Mathf.Rad2Deg;
            elevation = Mathf.Clamp(elevation, minElevation, maxElevation);

            float elevRad = elevation * Mathf.Deg2Rad;
            return new Vector3(signX * Mathf.Cos(elevRad), Mathf.Sin(elevRad), 0f);
        }
    }
}
