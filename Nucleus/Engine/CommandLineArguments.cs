namespace Nucleus.Engine
{
    public class CommandLineArguments
    {
        private CommandLineArguments() { }

        private HashSet<string> Flags = [];
        private Dictionary<string, string> Vars = [];
        public string[] Raw { get; private set; } = [];

        public bool IsFlagSet(string flag) => Flags.Contains(flag);
        public bool IsVarSet(string var) => Vars.ContainsKey(var);
        public bool TryGetVar(string var, out string? output) => Vars.TryGetValue(var, out output);
        public string TryGetVar(string var, string def) => Vars.TryGetValue(var, out var ret) ? ret : def;

        public static CommandLineArguments FromArgs(string[] args)
        {
            CommandLineArguments ret = new CommandLineArguments();
            ret.Raw = args;

            string? key = null;
            foreach (var part in args)
            {
                if (key != null)
                {
                    ret.Vars[key] = part;
                    key = null;
                }
                else
                {
                    if (part.StartsWith("-"))
                    {
                        ret.Flags.Add(part.Substring(1));
                    }
                    else
                    {
                        key = part;
                    }
                }
            }

            return ret;
        }
    }
}
