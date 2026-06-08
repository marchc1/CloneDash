using System;

namespace CubismLive2DExtractor
{
    public class CubismCdi3Json
    {
        public int Version { get; set; }
        public ParamGroupArray[] Parameters { get; set; }
        public ParamGroupArray[] ParameterGroups { get; set; }
        public PartArray[] Parts { get; set; }

        public class ParamGroupArray : IComparable
        {
            public string Id { get; set; }
            public string GroupId { get; set; }
            public string Name { get; set; }

            public int CompareTo(object obj)
            {
                return string.Compare(Id, ((ParamGroupArray)obj).Id, StringComparison.OrdinalIgnoreCase);
            }
        }

        public class PartArray : IComparable
        {
            public string Id { get; set; }
            public string Name { get; set; }

            public int CompareTo(object obj)
            {
                return string.Compare(Id, ((PartArray)obj).Id, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
