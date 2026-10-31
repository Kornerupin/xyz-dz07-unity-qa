using UnityEngine;

namespace EchoesOfAscent.Gameplay
{
    public readonly struct JumpReachMetrics
    {
        public float MaxHorizontal { get; }
        public float MaxRise { get; }
        public float MinHorizontal => MaxHorizontal * 0.2f;

        /// <summary>
        /// Создаёт метрики досягаемости прыжка.
        /// </summary>
        /// <param name="maxHorizontal">Максимальная горизонтальная дистанция.</param>
        /// <param name="maxRise">Максимальный подъём по вертикали.</param>
        public JumpReachMetrics(float maxHorizontal, float maxRise)
        {
            MaxHorizontal = maxHorizontal;
            MaxRise = maxRise;
        }
    }

    public readonly struct PlatformSpec
    {
        public Vector3 Position { get; }
        public Vector3 Scale { get; }

        /// <summary>
        /// Создаёт геометрическую спецификацию платформы.
        /// </summary>
        /// <param name="position">Центр платформы в мировых координатах.</param>
        /// <param name="scale">Размер (ширина, высота, глубина).</param>
        public PlatformSpec(Vector3 position, Vector3 scale)
        {
            Position = position;
            Scale = scale;
        }

        public float CenterX => Position.x;
        public float TopY => Position.y + Scale.y * 0.5f;
        public float HalfWidth => Scale.x * 0.5f;
        public float MinX => Position.x - HalfWidth;
        public float MaxX => Position.x + HalfWidth;
    }

    public static class JumpReachAnalyzer
    {
        const float SafetyMargin = 0.88f;
        const float DefaultPlayerHalfWidth = 0.5f;

        /// <summary>
        /// Вычисляет максимальную дальность прыжка при полном диапазоне импульса.
        /// </summary>
        /// <param name="config">Конфигурация прыжка.</param>
        /// <returns>Метрики горизонтальной и вертикальной досягаемости с запасом безопасности.</returns>
        public static JumpReachMetrics Compute(JumpConfig config)
        {
            return ComputeForImpulseRange(config, 0f, 1f);
        }

        /// <summary>
        /// Вычисляет комфортную досягаемость прыжка в заданном диапазоне импульса.
        /// </summary>
        /// <param name="config">Конфигурация прыжка.</param>
        /// <param name="minImpulseFraction">Минимальная доля импульса (0–1).</param>
        /// <param name="maxImpulseFraction">Максимальная доля импульса (0–1).</param>
        /// <returns>Метрики досягаемости с запасом безопасности.</returns>
        public static JumpReachMetrics ComputeComfortableReach(JumpConfig config, float minImpulseFraction = 0.45f, float maxImpulseFraction = 0.82f)
        {
            return ComputeForImpulseRange(config, minImpulseFraction, maxImpulseFraction);
        }

        /// <summary>
        /// Симулирует траектории и проверяет, может ли игрок допрыгнуть с одной платформы на другую.
        /// </summary>
        /// <param name="from">Исходная платформа.</param>
        /// <param name="to">Целевая платформа.</param>
        /// <param name="config">Конфигурация прыжка.</param>
        /// <param name="playerHalfWidth">Половина ширины коллайдера игрока.</param>
        /// <param name="minImpulseFraction">Минимальная доля импульса для перебора.</param>
        /// <param name="maxImpulseFraction">Максимальная доля импульса для перебора.</param>
        /// <returns>true, если найдена достижимая траектория приземления.</returns>
        public static bool CanJumpBetween(
            PlatformSpec from,
            PlatformSpec to,
            JumpConfig config,
            float playerHalfWidth = DefaultPlayerHalfWidth,
            float minImpulseFraction = 0f,
            float maxImpulseFraction = 1f)
        {
            if (config == null)
                return false;

            float launchY = from.TopY + playerHalfWidth;
            float[] launchXs =
            {
                from.CenterX,
                from.MinX + playerHalfWidth + 0.05f,
                from.MaxX - playerHalfWidth - 0.05f
            };

            const int impulseSteps = 8;
            const int angleSteps = 12;

            foreach (float launchX in launchXs)
            {
                for (int impulseIndex = 0; impulseIndex <= impulseSteps; impulseIndex++)
                {
                    float impulseT = impulseIndex / (float)impulseSteps;
                    float impulse = Mathf.Lerp(
                        Mathf.Lerp(config.minImpulse, config.maxImpulse, minImpulseFraction),
                        Mathf.Lerp(config.minImpulse, config.maxImpulse, maxImpulseFraction),
                        impulseT);

                    for (int angleIndex = 0; angleIndex <= angleSteps; angleIndex++)
                    {
                        float elevation = Mathf.Lerp(config.minLaunchElevation, config.maxLaunchAngle, angleIndex / (float)angleSteps);
                        float elevationRad = elevation * Mathf.Deg2Rad;

                        foreach (float sign in new[] { -1f, 1f })
                        {
                            var velocity = new Vector3(sign * Mathf.Cos(elevationRad) * impulse, Mathf.Sin(elevationRad) * impulse, 0f);
                            if (TrajectoryLandsOnPlatform(launchX, launchY, velocity, to, playerHalfWidth, config))
                                return true;
                        }
                    }
                }
            }

            return false;
        }

        static JumpReachMetrics ComputeForImpulseRange(JumpConfig config, float minImpulseFraction, float maxImpulseFraction)
        {
            if (config == null)
                return new JumpReachMetrics(4f, 2f);

            float maxHorizontal = 0f;
            float maxRise = 0f;
            const int impulseSteps = 12;
            const int angleSteps = 16;

            for (int impulseIndex = 0; impulseIndex <= impulseSteps; impulseIndex++)
            {
                float impulseT = impulseIndex / (float)impulseSteps;
                float impulse = Mathf.Lerp(
                    Mathf.Lerp(config.minImpulse, config.maxImpulse, minImpulseFraction),
                    Mathf.Lerp(config.minImpulse, config.maxImpulse, maxImpulseFraction),
                    impulseT);

                for (int angleIndex = 0; angleIndex <= angleSteps; angleIndex++)
                {
                    float elevation = Mathf.Lerp(config.minLaunchElevation, config.maxLaunchAngle, angleIndex / (float)angleSteps);
                    float elevationRad = elevation * Mathf.Deg2Rad;

                    foreach (float sign in new[] { -1f, 1f })
                    {
                        var velocity = new Vector3(sign * Mathf.Cos(elevationRad) * impulse, Mathf.Sin(elevationRad) * impulse, 0f);
                        var displacement = SimulateDisplacement(velocity, config);
                        maxHorizontal = Mathf.Max(maxHorizontal, displacement.x);
                        maxRise = Mathf.Max(maxRise, displacement.y);
                    }
                }
            }

            return new JumpReachMetrics(maxHorizontal * SafetyMargin, maxRise * SafetyMargin);
        }

        static Vector2 SimulateDisplacement(Vector3 initialVelocity, JumpConfig config)
        {
            Vector3 gravity = Physics.gravity;
            Vector3 position = Vector3.zero;
            Vector3 velocity = initialVelocity;
            float elapsed = 0f;
            float maxY = 0f;

            while (elapsed < config.trajectoryMaxSimTime)
            {
                maxY = Mathf.Max(maxY, position.y);

                Vector3 nextVelocity = velocity + gravity * config.trajectoryTimeStep;
                Vector3 nextPosition = position + velocity * config.trajectoryTimeStep;

                if (position.y >= 0f && nextPosition.y < 0f)
                {
                    float deltaY = nextPosition.y - position.y;
                    float t = Mathf.Abs(deltaY) > 0.0001f ? -position.y / deltaY : 1f;
                    float landX = Mathf.Lerp(position.x, nextPosition.x, Mathf.Clamp01(t));
                    return new Vector2(Mathf.Abs(landX), maxY);
                }

                velocity = nextVelocity;
                position = nextPosition;
                elapsed += config.trajectoryTimeStep;

                if (position.y < -12f)
                    break;
            }

            return new Vector2(Mathf.Abs(position.x), maxY);
        }

        static bool TrajectoryLandsOnPlatform(
            float startX,
            float startY,
            Vector3 initialVelocity,
            PlatformSpec target,
            float playerHalfWidth,
            JumpConfig config)
        {
            Vector3 gravity = Physics.gravity;
            Vector3 position = new Vector3(startX, startY, 0f);
            Vector3 velocity = initialVelocity;
            float elapsed = 0f;

            while (elapsed < config.trajectoryMaxSimTime)
            {
                Vector3 nextVelocity = velocity + gravity * config.trajectoryTimeStep;
                Vector3 nextPosition = position + velocity * config.trajectoryTimeStep;

                bool withinX = nextPosition.x >= target.MinX + playerHalfWidth
                            && nextPosition.x <= target.MaxX - playerHalfWidth;
                bool nearTop = nextPosition.y <= target.TopY + 0.2f && nextPosition.y >= target.TopY - 0.35f;
                bool descending = nextVelocity.y <= 0.5f;

                if (withinX && nearTop && descending)
                    return true;

                position = nextPosition;
                velocity = nextVelocity;
                elapsed += config.trajectoryTimeStep;

                if (position.y < startY - 8f)
                    break;
            }

            return false;
        }
    }
}
