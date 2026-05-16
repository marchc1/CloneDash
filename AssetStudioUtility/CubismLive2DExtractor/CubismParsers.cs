using System;
using System.Collections.Specialized;
using System.Linq;
using AssetStudio;
using CubismLive2DExtractor.CubismUnityClasses;
using Newtonsoft.Json;

namespace CubismLive2DExtractor
{
    public static class CubismParsers
    {
        public enum CubismMonoBehaviourType
        {
            FadeController,
            FadeMotionList,
            FadeMotion,
            ExpressionController,
            ExpressionList,
            Expression,
            Physics,
            DisplayInfo,
            PosePart,
            Model,
            RenderTexture,
        }

        public static string ParsePhysics(OrderedDictionary physicsDict, float motionFps)
        {
            var cubismPhysicsRig = JsonConvert.DeserializeObject<CubismPhysics>(JsonConvert.SerializeObject(physicsDict)).Rig;

            var physicsSettings = new CubismPhysics3Json.SerializablePhysicsSettings[cubismPhysicsRig.SubRigs.Length];
            for (var i = 0; i < physicsSettings.Length; i++)
            {
                var subRigs = cubismPhysicsRig.SubRigs[i];
                physicsSettings[i] = new CubismPhysics3Json.SerializablePhysicsSettings
                {
                    Id = $"PhysicsSetting{i + 1}",
                    Input = new CubismPhysics3Json.SerializableInput[subRigs.Input.Length],
                    Output = new CubismPhysics3Json.SerializableOutput[subRigs.Output.Length],
                    Vertices = new CubismPhysics3Json.SerializableVertex[subRigs.Particles.Length],
                    Normalization = new CubismPhysics3Json.SerializableNormalization
                    {
                        Position = new CubismPhysics3Json.SerializableNormalizationValue
                        {
                            Minimum = subRigs.Normalization.Position.Minimum,
                            Default = subRigs.Normalization.Position.Default,
                            Maximum = subRigs.Normalization.Position.Maximum
                        },
                        Angle = new CubismPhysics3Json.SerializableNormalizationValue
                        {
                            Minimum = subRigs.Normalization.Angle.Minimum,
                            Default = subRigs.Normalization.Angle.Default,
                            Maximum = subRigs.Normalization.Angle.Maximum
                        }
                    }
                };
                for (var j = 0; j < subRigs.Input.Length; j++)
                {
                    var input = subRigs.Input[j];
                    physicsSettings[i].Input[j] = new CubismPhysics3Json.SerializableInput
                    {
                        Source = new CubismPhysics3Json.SerializableParameter
                        {
                            Target = "Parameter", //同名GameObject父节点的名称
                            Id = input.SourceId
                        },
                        Weight = input.Weight,
                        Type = Enum.GetName(typeof(CubismPhysicsSourceComponent), input.SourceComponent),
                        Reflect = input.IsInverted
                    };
                }
                for (var j = 0; j < subRigs.Output.Length; j++)
                {
                    var output = subRigs.Output[j];
                    physicsSettings[i].Output[j] = new CubismPhysics3Json.SerializableOutput
                    {
                        Destination = new CubismPhysics3Json.SerializableParameter
                        {
                            Target = "Parameter", //同名GameObject父节点的名称
                            Id = output.DestinationId
                        },
                        VertexIndex = output.ParticleIndex,
                        Scale = output.AngleScale,
                        Weight = output.Weight,
                        Type = Enum.GetName(typeof(CubismPhysicsSourceComponent), output.SourceComponent),
                        Reflect = output.IsInverted
                    };
                }
                for (var j = 0; j < subRigs.Particles.Length; j++)
                {
                    var particles = subRigs.Particles[j];
                    physicsSettings[i].Vertices[j] = new CubismPhysics3Json.SerializableVertex
                    {
                        Position = particles.InitialPosition,
                        Mobility = particles.Mobility,
                        Delay = particles.Delay,
                        Acceleration = particles.Acceleration,
                        Radius = particles.Radius
                    };
                }
            }
            var physicsDictionary = new CubismPhysics3Json.SerializablePhysicsDictionary[physicsSettings.Length];
            for (var i = 0; i < physicsSettings.Length; i++)
            {
                physicsDictionary[i] = new CubismPhysics3Json.SerializablePhysicsDictionary
                {
                    Id = $"PhysicsSetting{i + 1}",
                    Name = $"Dummy{i + 1}"
                };
            }

            var fps = cubismPhysicsRig.Fps == 0 ? motionFps : cubismPhysicsRig.Fps;
            var physicsJson = new CubismPhysics3Json
            {
                Version = 3,
                Meta = new CubismPhysics3Json.SerializableMeta
                {
                    PhysicsSettingCount = cubismPhysicsRig.SubRigs.Length,
                    TotalInputCount = cubismPhysicsRig.SubRigs.Sum(x => x.Input.Length),
                    TotalOutputCount = cubismPhysicsRig.SubRigs.Sum(x => x.Output.Length),
                    VertexCount = cubismPhysicsRig.SubRigs.Sum(x => x.Particles.Length),
                    Fps = fps == 0 ? 30f : fps,
                    EffectiveForces = new CubismPhysics3Json.SerializableEffectiveForces
                    {
                        Gravity = cubismPhysicsRig.Gravity,
                        Wind = cubismPhysicsRig.Wind
                    },
                    PhysicsDictionary = physicsDictionary
                },
                PhysicsSettings = physicsSettings
            };
            return JsonConvert.SerializeObject(physicsJson, Formatting.Indented, new MyJsonConverter2());
        }

        public static OrderedDictionary ParseMonoBehaviour(MonoBehaviour m_MonoBehaviour, CubismMonoBehaviourType cubismMonoBehaviourType, AssemblyLoader assemblyLoader)
        {
            var orderedDict = m_MonoBehaviour.ToType();
            if (orderedDict != null)
                return orderedDict;

            var fieldName = "";
            var m_Type = m_MonoBehaviour.ConvertToTypeTree(assemblyLoader);
            switch (cubismMonoBehaviourType)
            {
                case CubismMonoBehaviourType.FadeController:
                    fieldName = "cubismfademotionlist";
                    break;
                case CubismMonoBehaviourType.FadeMotionList:
                    fieldName = "cubismfademotionobjects";
                    break;
                case CubismMonoBehaviourType.FadeMotion:
                    fieldName = "parameterids";
                    break;
                case CubismMonoBehaviourType.ExpressionController:
                    fieldName = "expressionslist";
                    break;
                case CubismMonoBehaviourType.ExpressionList:
                    fieldName = "cubismexpressionobjects";
                    break;
                case CubismMonoBehaviourType.Expression:
                    fieldName = "parameters";
                    break;
                case CubismMonoBehaviourType.Physics:
                    fieldName = "_rig";
                    break;
                case CubismMonoBehaviourType.DisplayInfo:
                    fieldName = "name";
                    break;
                case CubismMonoBehaviourType.PosePart:
                    fieldName = "groupindex";
                    break;
                case CubismMonoBehaviourType.Model:
                    fieldName = "_moc";
                    break;
                case CubismMonoBehaviourType.RenderTexture:
                    fieldName = "_maintexture";
                    break;
            }
            if (m_Type.m_Nodes.FindIndex(x => x.m_Name.ToLower() == fieldName) < 0)
            {
                m_MonoBehaviour.m_Script.TryGet(out var m_MonoScript);
                var assetName = m_MonoBehaviour.m_Name != "" ? m_MonoBehaviour.m_Name : m_MonoScript.m_ClassName;
                Logger.Warning($"{cubismMonoBehaviourType} asset \"{assetName}\" is not readable");
                return null;
            }
            orderedDict = m_MonoBehaviour.ToType(m_Type);

            return orderedDict;
        }
    }
}
