using AssetStudio;

namespace CubismLive2DExtractor.CubismUnityClasses
{
    public sealed class CubismExpressionData : MonoBehaviour
    {
        public string Type { get; set; }
        public float FadeInTime { get; set; }
        public float FadeOutTime { get; set; }
        public SerializableExpressionParameter[] Parameters { get; set; }

        public class SerializableExpressionParameter
        {
            public string Id { get; set; }
            public float Value { get; set; }
            public BlendType Blend { get; set; }
        }
    }
}
