namespace Nucleus.Types
{
    public record MouseButton(int Button)
    {
        public static MouseButton Mouse1 { get; } = new(1);
        public static MouseButton MouseLeft { get; } = new(1);

        public static MouseButton Mouse2 { get; } = new(2);
        public static MouseButton MouseRight { get; } = new(2);

        public static MouseButton Mouse3 { get; } = new(3);
        public static MouseButton MouseMiddle { get; } = new(3);

        public static MouseButton Mouse4 { get; } = new(4);
        public static MouseButton MouseBack { get; } = new(4);

        public static MouseButton Mouse5 { get; } = new(5);
        public static MouseButton MouseForward { get; } = new(5);
    }
}
