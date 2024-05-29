using Nucleus.Types;
using System.Numerics;

namespace Nucleus.Core
{
    public class Bone3Cache
    {
        public int ID { get; set; }
        /// <summary>
        /// A -1 points to being parented to the Model
        /// </summary>
        public int Parent { get; set; } = -1;

        public TransformVQV BindPose { get; set; } = new();
        public string Name { get; internal set; }

        public List<Bone3Cache> Children { get; set; } = [];
        public Matrix4x4 InverseBindMatrix { get; internal set; }
    }
}
