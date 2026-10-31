using UnityEngine;

namespace EchoesOfAscent.Gameplay
{
    [CreateAssetMenu(fileName = "JumpConfig", menuName = "Echoes of Ascent/Jump Config")]
    public sealed class JumpConfig : ScriptableObject
    {
        [Header("Impulse")]
        public float minImpulse = 4f;
        public float maxImpulse = 18f;
        [Tooltip("Max elevation from horizontal (degrees). 90 = straight up.")]
        public float maxLaunchAngle = 75f;
        [Tooltip("Min elevation from horizontal (degrees). Blocks downward and flat shots.")]
        public float minLaunchElevation = 8f;

        [Header("Trajectory Preview")]
        public int trajectorySteps = 40;
        public float trajectoryTimeStep = 0.05f;
        public float trajectoryMaxSimTime = 2.5f;

        [Header("Jump Feel")]
        public float coyoteTime = 0.12f;
        public float minSwipePixels = 25f;
    }
}
