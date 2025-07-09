using System.Numerics;
using Raylib_cs;

namespace Nucleus.Types
{
    public enum TransformOrder
    {
        PosRotScale,
        ScaleRotPos,
        RotScalePos,
        RotPosScale
    }
    public struct TransformVQV
    {
        private Vector3 __position = Vector3.Zero;
        private Quaternion __rotation = Quaternion.Identity;
        private Vector3 __scaling = Vector3.Zero;
        private bool __dirty = true;
        private Matrix4x4 __cachedTransformMatrix = Matrix4x4.Identity;

        public Vector3 Position {
            get {
                return __position;
            }
            set {
                __position = value;
                __dirty = true;
            }
        }
        public Quaternion Rotation {
            get {
                return __rotation;
            }
            set {
                __rotation = value;
                __dirty = true;
            }
        }
        public Vector3 Scale {
            get {
                return __scaling;
            }
            set {
                __scaling = value;
                __dirty = true;
            }
        }

        public TransformVQV() {
            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;
            Scale = Vector3.One;
        }

        public TransformVQV(Vector3? pos = null, Quaternion? rot = null, Vector3? scale = null, TransformOrder? transformOrder = null) {
            Position = pos ?? Vector3.Zero;
            Rotation = rot ?? Quaternion.Identity;
            Scale = scale ?? Vector3.One;
            TransformOrder = transformOrder ?? TransformOrder.PosRotScale;
        }

        public static TransformVQV FromTQS(float[] translation, float[] quatrotation, float[] scale) {
            TransformVQV ret = new();

            ret.Position = new(translation[0], translation[1], translation[2]);
            ret.Rotation = new(quatrotation[0], quatrotation[1], quatrotation[2], quatrotation[3]);
            ret.Scale = new(scale[0], scale[1], scale[2]);

            return ret;
        }

        public TransformOrder TransformOrder { get; set; } = TransformOrder.PosRotScale;

        private Matrix4x4 AsMatrix() {
            if (!__dirty)
                return __cachedTransformMatrix;

            Vector3 pos = Position, scale = Scale;
            Quaternion rot = Rotation;

            var finalTranslation = Raymath.MatrixTranslate(pos.X, pos.Y, pos.Z);
            var finalRotation = Raymath.QuaternionToMatrix(rot);
            var finalScaling = Raymath.MatrixScale(scale.X, scale.Z, scale.Y);

            switch (TransformOrder) {
                case TransformOrder.PosRotScale: __cachedTransformMatrix = finalTranslation * finalRotation * finalScaling; break;
                case TransformOrder.ScaleRotPos: __cachedTransformMatrix = finalScaling * finalRotation * finalTranslation; break;
                case TransformOrder.RotScalePos: __cachedTransformMatrix = finalRotation * finalScaling * finalTranslation; break;
                case TransformOrder.RotPosScale: __cachedTransformMatrix = finalRotation * finalTranslation * finalScaling; break;
                default:
                    throw new NotImplementedException();
            }
            __cachedTransformMatrix = __cachedTransformMatrix;

            return __cachedTransformMatrix;
        }

        public Matrix4x4 Matrix => AsMatrix();

        public Vector3 RotationEuler {
            get {
                var ret = Raymath.QuaternionToEuler(Rotation);

                ret *= NMath.RAD2DEG;
                return ret;
            }
        }

        public override string ToString() {
            return $"Transform V,V,V [pos: {Position}, ang: {Rotation} scale: {Scale}]";
        }

        public static TransformVQV DecomposeMatrix(Matrix4x4 value) {
            Vector3 pos;
            Quaternion rot;
            Vector3 scale;

            Matrix4x4.Decompose(value, out scale, out rot, out pos);

            rot *= -1;
            scale = new(scale.X, scale.Z, scale.Y);
            return new TransformVQV(pos, rot, scale);
        }
    }
}
