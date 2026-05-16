using AssetStudio;

namespace CubismLive2DExtractor.CubismUnityClasses
{
    public sealed class CubismFadeMotionList : MonoBehaviour
    {
        public int[] MotionInstanceIds { get; set; }
        public PPtr<CubismFadeMotionData>[] CubismFadeMotionObjects { get; set; }
    }
}
