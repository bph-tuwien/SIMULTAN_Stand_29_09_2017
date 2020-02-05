using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DataStructVisualizer.Nodes;

namespace DataStructVisualizer.WinUtils
{
    public static class GeneralUtils
    {

        public static List<Node> DeepCopyNodeList(List<Node> _original)
        {
            List<Node> copy = new List<Node>();
            if (_original == null) return copy;

            int nr_L0 = _original.Count;
            if (nr_L0 == 0) return copy;

            foreach(Node node in _original)
            {
                // make shallow copy
                Node node_cp = new Node(node, false);
                // recursive step -> add copy of the contained entities
                if(node.ContainedNodes.Count > 0)
                    node_cp.ContainedNodes = new List<Node>(DeepCopyNodeList(node.ContainedNodes));
                copy.Add(node_cp);
            }

            return copy;
        }

    }
}
