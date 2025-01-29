using System.Numerics;

namespace Nucleus.Core
{
    /// <summary>
    /// Internal data structure used for storing potential animation channels
    /// </summary>
    public record BoneAnimationChannels
    {
        public AnimationChannelData<Vector3>? Position;
        public AnimationChannelData<Quaternion>? Rotation;
        public AnimationChannelData<Vector3>? Scale;
        public AnimationChannelData<float>? ActiveSlot;
    }
}
