using AssetStudio;

namespace CubismLive2DExtractor
{
    public class CubismPhysics3Json
    {
        public int Version;
        public SerializableMeta Meta;
        public SerializablePhysicsSettings[] PhysicsSettings;

        public class SerializableNormalizationValue
        {
            public float Minimum;
            public float Default;
            public float Maximum;
        }

        public class SerializableParameter
        {
            public string Target;
            public string Id;
        }

        public class SerializableInput
        {
            public SerializableParameter Source;
            public float Weight;
            public string Type;
            public bool Reflect;
        }

        public class SerializableOutput
        {
            public SerializableParameter Destination;
            public int VertexIndex;
            public float Scale;
            public float Weight;
            public string Type;
            public bool Reflect;
        }

        public class SerializableVertex
        {
            public Vector2 Position;
            public float Mobility;
            public float Delay;
            public float Acceleration;
            public float Radius;
        }

        public class SerializableNormalization
        {
            public SerializableNormalizationValue Position;
            public SerializableNormalizationValue Angle;
        }

        public class SerializablePhysicsSettings
        {
            public string Id;
            public SerializableInput[] Input;
            public SerializableOutput[] Output;
            public SerializableVertex[] Vertices;
            public SerializableNormalization Normalization;
        }

        public class SerializableMeta
        {
            public int PhysicsSettingCount;
            public int TotalInputCount;
            public int TotalOutputCount;
            public int VertexCount;
            public float Fps;
            public SerializableEffectiveForces EffectiveForces;
            public SerializablePhysicsDictionary[] PhysicsDictionary;
        }

        public class SerializableEffectiveForces
        {
            public Vector2 Gravity;
            public Vector2 Wind;
        }

        public class SerializablePhysicsDictionary
        {
            public string Id;
            public string Name;
        }
    }
}
