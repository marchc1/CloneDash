using System.Numerics;
using Nucleus.Types;

namespace Nucleus.Core
{
    public class Model3AnimationChannel
    {
        public static bool GlobalPause { get; set; } = false;

        public Model3 BoundTo { get; internal set; }
        public Model3AnimationCache AnimationData { get; internal set; }

        public double AnimationPlayhead { get; internal set; } = 0;
        private DateTime __lastProcess = DateTime.Now;

        public double Speed { get; set; } = 1;
        public bool Loops { get; set; } = false;
        public bool Playing { get; set; } = false;
        public string? WhenCompletePlay { get; internal set; }

        public void StartPlaying(bool loops, string? fallback) {
            Loops = loops;
            WhenCompletePlay = fallback;
            AnimationPlayhead = 0;
            Playing = true;
        }
        /// <summary>
        /// Stops animation with NO respect to a fallback animation.
        /// </summary>
        public void StopPlaying() {
            Loops = false;
            WhenCompletePlay = null;
            AnimationPlayhead = 0;
            Playing = false;
        }
        public void Process() {
            if (GlobalPause && BoundTo.RespectsGlobalPause)
                return;
            
            if (!Playing) {
                if(WhenCompletePlay != null) {
                    BoundTo.PlayAnimation(WhenCompletePlay, true);
                    WhenCompletePlay = null;
                }
                return;
            }

            if (AnimationPlayhead >= AnimationData.AnimationLength) {
                if(Loops)
                    AnimationPlayhead = 0;
                else {
                    Playing = false;
                    return;
                }
            }

            DateTime now = DateTime.Now;
            double delta = EngineCore.Level.RealtimeDelta;
            delta *= Math.Max(0, Speed);

            AnimationPlayhead += delta;

            // start working
            foreach (Model3Bone bone in BoundTo.Bones) {
                if (bone.Parent == bone.Root)
                    ApplyPoses(bone, BoundTo.Transform);
            }

            __lastProcess = now;
        }


		internal TransformVQV GetCurrentTransform(BoneAnimationChannels animData, Model3Bone bone) {
            TransformVQV transform = new TransformVQV();

            if (animData.Position != null) transform.Position = animData.Position.LinearInterpolation(AnimationPlayhead);
            if (animData.Rotation != null) transform.Rotation = animData.Rotation.LinearInterpolation(AnimationPlayhead);
            if (animData.Scale != null) transform.Scale = animData.Scale.LinearInterpolation(AnimationPlayhead);

            return transform;
        }

        internal void ApplyPoses(Model3Bone bone, Matrix4x4 parentTransform) {
			BoneAnimationChannels animData = AnimationData.BoneIDToChannels[bone.ID];
			TransformVQV current = GetCurrentTransform(animData, bone);

			if (animData.ActiveSlot != null)
				bone.ActiveSlot = (int)animData.ActiveSlot.LinearInterpolation(AnimationPlayhead);

			if (animData.ActiveSlotAlpha != null) {
				bone.ActiveSlotAlpha = animData.ActiveSlotAlpha.LinearInterpolation(AnimationPlayhead);
			}
			current.TransformOrder = TransformOrder.PosRotScale;
            //current.Scale = new Vector3(6.5f, 1f, 2f);
            Matrix4x4 poseTransform = current.Matrix.Transpose();
            //TransformVQV test1 = TransformVQV.DecomposeMatrix(poseTransform);

            Matrix4x4 t = poseTransform * parentTransform;

            foreach (Model3Bone child in bone.Children) {
                //if (child.Name == "Head") {
                    ApplyPoses(child, t);
                //}
            }

            t = bone.Parent.IsRoot ? (poseTransform * bone.InverseBindMatrix.Transpose()) : (poseTransform * bone.BindPose.Matrix.Transpose().Invert());

            bone.PoseTransform = t;
        }
    }
}
