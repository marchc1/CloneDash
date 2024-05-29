namespace Nucleus.Core
{
    /// <summary>
    /// Bone
    /// </summary>
    public class Model3Bone : Model3Component, IEquatable<Model3Bone>, IComparable<Model3Bone>
    {
        public int ID { get; internal set; }

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
