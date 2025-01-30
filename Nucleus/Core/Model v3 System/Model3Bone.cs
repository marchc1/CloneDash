namespace Nucleus.Core
{
    /// <summary>
    /// Bone
    /// </summary>
    public class Model3Bone : Model3Component, IEquatable<Model3Bone>, IComparable<Model3Bone>
    {
		public Bone3Cache Cache { get; internal set; }
        public int ID { get; internal set; }
		public int ActiveSlot { get; internal set; }

		private float __activeSlotAlpha = 1f;
		public float ActiveSlotAlpha {
			get => __activeSlotAlpha;
			set => __activeSlotAlpha = value;
		}
		public int CompareTo(Model3Bone other) {
            return ID.CompareTo(other.ID);
        }

        public bool Equals(Model3Bone other) {
            if (other == null) return false;
            return (this.ID.Equals(other.ID) && this.Root == other.Root);
        }

        public override string ToString() {
            return $"Model V3 Bone [name {Name}, #{ID}, part of {Root}]";
        }
    }
}
