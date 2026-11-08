using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetStudio
{
    public class TypeTree
    {
        public List<TypeTreeNode> m_Nodes;
        public byte[] m_StringBuffer;
        private SubtreeEnds subtreeEnds;

        internal int[] GetSubtreeEnds()
        {
            var nodes = m_Nodes;
            var cached = subtreeEnds;
            if (cached != null && cached.Nodes == nodes && cached.Ends.Length == nodes.Count)
                return cached.Ends;
            var ends = new int[nodes.Count];
            var open = new Stack<int>();
            for (int i = 0; i < nodes.Count; i++)
            {
                var level = nodes[i].m_Level;
                while (open.Count > 0 && nodes[open.Peek()].m_Level >= level)
                    ends[open.Pop()] = i;
                open.Push(i);
            }
            while (open.Count > 0)
                ends[open.Pop()] = nodes.Count;
            subtreeEnds = new SubtreeEnds(nodes, ends);
            return ends;
        }

        private sealed record SubtreeEnds(List<TypeTreeNode> Nodes, int[] Ends);
    }
}
