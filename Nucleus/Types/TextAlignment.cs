namespace Nucleus.Types
{
    public record TextAlignment(int Alignment)
    {
        public static readonly TextAlignment Left = new(0);
        public static readonly TextAlignment Top = new(0);

        public static readonly TextAlignment Middle = new(1);
        public static readonly TextAlignment Center = new(1);

        public static readonly TextAlignment Right = new(2);
        public static readonly TextAlignment Bottom = new(2);

        public static Anchor FromTextAlignment(TextAlignment horizontal, TextAlignment vertical) {
            return (Anchor)(1 + (vertical.Alignment * 3) + horizontal.Alignment);
        }
    }
}
