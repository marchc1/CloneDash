using System.Numerics;
using Raylib_cs;
using Nucleus.Types;

namespace Nucleus.Core
{
    public abstract class Model3Component
    {
        /// <summary>
        /// A static Matrix4x4 used to transform into the engines coordinate system.
        /// </summary>
        public static Matrix4x4 WorldTransform { get; } = new TransformVQV(new(0, 0, 0), Quaternion.CreateFromYawPitchRoll(0, (-90f).ToRadians(), 0), new(1, 1, 1), TransformOrder.RotScalePos).Matrix;

        /// <summary>
        /// Root of the component, will always be a Model. <br></br>
        /// Be wary of stack overflows; the Root of the Root is always valid
        /// </summary>
        public Model3 Root { get; set; }

        /// <summary>
        /// The parent of this component; may either be a Bone or a Model
        /// </summary>
        public Model3Component? Parent { get; set; }
        /// <summary>
        /// All children of this component; will always be a Bone
        /// </summary>
        public List<Model3Bone> Children { get; } = [];
        /// <summary>
        /// Sometimes not applicable; the name of the bone or skeleton.
        /// </summary>
        public string Name { get; set; } = "";

        public Model3Bone[] GetAttachedBones() => Children.ToArray();

        /// <summary>
        /// Is this component the root model?
        /// </summary>
        public bool IsRoot => this.Root == this;
        /// <summary>
        /// Does this component contain child bones?
        /// </summary>
        public bool HasChildren => this.Children.Count > 0;
        /// <summary>
        /// Is this component parented?
        /// </summary>
        public bool IsParented => this.Parent != null;
        /// <summary>
        /// Is the parent a model?
        /// </summary>
        public bool IsParentRoot => IsParented && this.Parent?.IsRoot == true;
        /// <summary>
        /// Calculates a bind * pose matrix transform.
        /// </summary>
        public Matrix4x4 Transform => BindTransform * PoseTransform;

        private TransformVQV __bindPose = new TransformVQV();
        public TransformVQV BindPose {
            get {
                return __bindPose;
            }
            set {
                __bindPose = value;
            }
        }
        public Matrix4x4 InverseBindMatrix { get; internal set; } = Matrix4x4.Identity;
        public Matrix4x4 BindTransform => __bindPose.Matrix;

        private TransformVQV __pose = new TransformVQV();
        public Matrix4x4 PoseTransform {
            get {
                return __pose.Matrix;
            }
            set {
                __pose = TransformVQV.DecomposeMatrix(value);
            }
        }

        public TransformVQV Pose {
            get {
                return __pose;
            }
            set {
                __pose = value;
            }
        }

        public Matrix4x4 CalculateWorldTransform() {
            // calculate matrix
            if (this is Model3) {
                Model3 meM = this as Model3;
                return WorldTransform * meM.BindTransform * meM.PoseTransform;
            }

            Model3Bone me = this as Model3Bone;

            Matrix4x4 parentTransform = Matrix4x4.Identity;
            var component = this;
            while (component.Parent != null) {
                parentTransform = component.Transform * parentTransform;
                component = component.Parent;
            }

            return me.Root.FinalBoneTransforms[me.ID] * me.InverseBindMatrix.Invert();
        }

        public Vector3 LocalToWorld(Vector3? pos = null) {
            Vector3 start = pos ?? new Vector3(0, 0, 0);
            start = Raymath.Vector3Transform(start, CalculateWorldTransform());

            return start;
        }

        /// <summary>
        /// Imagine the same coordinate system you'd use for a 2D environment; where X positive goes rightwards and Y positive goes downwards. Z is used as a depth, since this is still rendered in 3D.
        /// </summary>
        public Vector3 Position {
            get { return __pose.Position; }
            set { __pose.Position = value; }
        }
        /// <summary>
        /// These rotate on the same axises you'd see from Position. So to simply rotate like you'd rotate a 2D image, you'd use the Z axis.
        /// </summary>
        public Vector3 Rotation {
            get { return Raymath.QuaternionToEuler(__pose.Rotation); }
            set {
                __pose.Rotation = Raymath.QuaternionFromEuler(value.X.ToRadians(), value.Y.ToRadians(), value.Z.ToRadians());
            }
        }

        /// <summary>
        /// Quaternion-based rotation.
        /// </summary>
        public Quaternion RotationQuaternion {
            get { return __pose.Rotation; }
            set {
                __pose.Rotation = value;
            }
        }

        /// <summary>
        /// Scales on the same axises that Position scales on.
        /// </summary>
        public Vector3 Scale {
            get { return __pose.Scale; }
            set { __pose.Scale = value; }
        }

        internal void CreateBoneFromCache(Bone3Cache boneCacheStore) {
            Model3Bone bone = new Model3Bone();
			bone.Cache = boneCacheStore;
            bone.Root = this.Root;
            bone.Parent = this;
            bone.Root.Bones.Add(bone);
            bone.ID = boneCacheStore.ID;
            bone.InverseBindMatrix = boneCacheStore.InverseBindMatrix;
            bone.Name = boneCacheStore.Name;
            this.Children.Add(bone);
            bone.BindPose = boneCacheStore.BindPose;


            foreach (var child in boneCacheStore.Children)
                bone.CreateBoneFromCache(child);

            if (this is Model3) {
                // FIX SORTING ISSUES!
                var meAsModel3 = this as Model3;
                meAsModel3.LocalBoneTransforms = new Matrix4x4[Root.Bones.Count];
                meAsModel3.FinalBoneTransforms = new Matrix4x4[Root.Bones.Count];

                meAsModel3.BonesByName = new();
                foreach (var modelBone in meAsModel3.Bones)
                    meAsModel3.BonesByName[modelBone.Name] = modelBone;
            }
        }
    }
}
