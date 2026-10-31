using UnityEngine;

namespace EchoesOfAscent.Gameplay
{
    public interface IMovingPlatform
    {
        Vector3 FrameDelta { get; }
        Vector3 Velocity { get; }
        Transform Transform { get; }
    }
}
