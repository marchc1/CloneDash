namespace CubismLive2DExtractor
{
    public class CubismPose3Json
    {
        public string Type;
        public ControlNode[][] Groups;

        public class ControlNode
        {
            public string Id;
            public string[] Link;
        }
    }
}
