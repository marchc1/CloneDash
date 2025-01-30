using System.Numerics;
using Nucleus.Types;

namespace Nucleus.Core
{
    public class Model3AnimationCache
    {
        /// <summary>
        /// The animation's name
        /// </summary>
        public string Name { get; internal set; } = "";
        public List<IAnimationChannelData> Channels { get; internal set; } = [];
        public double AnimationLength { get; internal set; } = 0;

        public Dictionary<int, BoneAnimationChannels> BoneIDToChannels { get; private set; } = [];

        public void Build() {
            foreach (IAnimationChannelData channel in Channels) {
                if (!BoneIDToChannels.ContainsKey(channel.Target))
                    BoneIDToChannels[channel.Target] = new();

                switch (channel) {
					case AnimationChannelData<float> ch:
						switch (channel.Path) {
							case AnimationTargetPath.ActiveSlot:
								BoneIDToChannels[channel.Target].ActiveSlot = ch; break;
							case AnimationTargetPath.ActiveSlotAlpha:
								BoneIDToChannels[channel.Target].ActiveSlotAlpha = ch; break;
						}
						break;
					case AnimationChannelData<Vector3> ch:
                        switch (channel.Path) {
                            case AnimationTargetPath.Position: 
								BoneIDToChannels[channel.Target].Position = ch; break;
                            case AnimationTargetPath.Scale: 
								BoneIDToChannels[channel.Target].Scale = ch; break;
                        }
                        break;
                    case AnimationChannelData<Quaternion> ch:
                        BoneIDToChannels[channel.Target].Rotation = ch;
                        break;
                }
            }
        }
    }
}
