using Nucleus.Types;

namespace Nucleus.Core
{
    /// <summary>
    /// Every Model3Animation has a list of AnimationChannelData's which store
    /// <br></br> - The target Model3Component
    /// <br></br> - The path to animate (position, rotation, etc... see <see cref="AnimationTargetPath"/>)
    /// </summary>
    public interface IAnimationChannelData
    {
        public int Target { get; internal set; }
        public AnimationTargetPath Path { get; internal set; }
        public AnimationInterpolation Interpolation { get; internal set; }
    }
}
