using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructVisualizer.Nodes
{
    public enum NodesToExcelExportType { SIMPLE, COMPLETE, EXPLICIT }
    public static class NodeAssembler
    {
        #region CONSTANTS

        public static readonly string NODE_NAME = "name";
        public static readonly string NODE_SYNC_BY_NAME = "sync_by_name";
        public static readonly string NODE_DESCR = "descr";
        public static readonly string NODE_UNIT = "unit";
        public static readonly string NODE_DEFAULT_VALUE = "default_value";
        public static readonly string NODE_SOURCE = "source";
        public static readonly string NODE_MANAGER = "manager";
        public static readonly string NODE_GEOMETRY = "geometry";
        public static readonly string NODE_CONTAINED_NODES = "contained";
        public static readonly string NODE_LEVEL = "node_level"; // 'level' is a key word in EXCEL and causes syntactical errors
        public static readonly int NR_FIELDS = 10;
        
        public static readonly char[] NODE_SEPARATORS = new char[] { ',', ';', ' ' };
        public static readonly int NODE_ROOT_LEVEL = 0;

        private static readonly int MAX_NR_ITERATIONS = 10;

        #endregion

        #region NodesToExcelExportType

        public static NodesToExcelExportType StringToType(string _input)
        {
            if (_input == null) return NodesToExcelExportType.SIMPLE;
            string input_str = _input.ToString().Trim();

            switch(input_str)
            {
                case "COMPLETE":
                    return NodesToExcelExportType.COMPLETE;
                case "EXPLICIT":
                    return NodesToExcelExportType.EXPLICIT;
                default:
                    return NodesToExcelExportType.SIMPLE;
            }
        }

        #endregion

        #region IMPORTING: STRINGS -> NODES
        public static List<Node> GetNodeList(List<List<string>> _string_data)
        {
            List<Node> nodes = new List<Node>();
            List<string> nodes_names = new List<string>();
            List<List<string>> nodes_contained_node_names = new List<List<string>>();
            List<int> nodes_levels = new List<int>();
            
            // trim the input
            List<List<string>> string_data_clean = NodeAssembler.CutEmptyColumnsAndRows(_string_data);

            // extract INFO
            NodeAssembler.ExtractInfoFromNodeStrings(string_data_clean, NR_FIELDS - 1, out nodes, out nodes_names,
                                                            out nodes_contained_node_names, out nodes_levels);
            // detect CONTAINMENT relationships
            NodeAssembler.ConvertNodeInfoToNodeList(ref nodes, nodes_names, nodes_contained_node_names);

            return nodes;
        }

        public static List<Node> GetNodeListComplete(List<List<string>> _string_data)
        {
            List<Node> nodes = new List<Node>();
            List<string> nodes_names = new List<string>();
            List<List<string>> nodes_contained_node_names = new List<List<string>>();
            List<int> nodes_levels = new List<int>();
            
            // trim the input
            List<List<string>> string_data_clean = NodeAssembler.CutEmptyColumnsAndRows(_string_data);

            // extract INFO
            NodeAssembler.ExtractInfoFromNodeStrings(string_data_clean, NR_FIELDS, out nodes, out nodes_names,
                                                            out nodes_contained_node_names, out nodes_levels);
            // detect CONTAINMENT relationships
            List<Node> nodes_FINAL = NodeAssembler.ConvertNodeAndLevelInfoToNodeList(nodes, nodes_names, nodes_contained_node_names, nodes_levels);

            return nodes_FINAL;
        }

        public static List<Node> GetNodeListExplicit(List<List<string>> _string_data)
        {
            List<Node> nodes = new List<Node>();
            List<string> nodes_names = new List<string>();
            List<List<string>> nodes_contained_node_names = new List<List<string>>();
            List<int> nodes_levels = new List<int>();

            // trim the input
            List<List<string>> string_data_clean = NodeAssembler.CutEmptyColumnsAndRows(_string_data);

            // extract INFO
            NodeAssembler.ExtractInfoFromNodeStrings(string_data_clean, NR_FIELDS, out nodes, out nodes_names,
                                                            out nodes_contained_node_names, out nodes_levels);
            // detect CONTAINMENT relationships
            List<Node> nodes_FINAL = NodeAssembler.ConvertExplicitNodeAndLevelInfoToNodeList(nodes, nodes_levels);
            return nodes_FINAL;
        }


        private static void ConvertNodeInfoToNodeList(ref List<Node> _nodes_at_level0, List<string> _nodes_names,
                    List<List<string>> _nodes_contained_node_names)
        {
            // ORDER the nodes from the ones w/o contained nodes 
            // to the most complex ones (at the top of the hierarchy)
            List<Node> nodes_remaining = new List<Node>(_nodes_at_level0);
            List<List<string>> nodes_contained_node_names_remaining = new List<List<string>>(_nodes_contained_node_names);
            List<Node> nodes_ordered = new List<Node>();

            int nrR = nodes_remaining.Count;
            int nrTotal = nrR;
            while (nrR > 0)
            {
                for (int i = 0; i < nrTotal; i++)
                {
                    Node n = _nodes_at_level0[i];
                    if (nodes_contained_node_names_remaining[i].Count == 0)
                    {
                        // add the finalized node to the list                        
                        nodes_ordered.Add(n);
                        nodes_remaining.Remove(n);
                    }
                    else
                    {
                        List<string> to_remove = new List<string>();
                        foreach (string name in nodes_contained_node_names_remaining[i])
                        {
                            // the name is not from an explicit node definition -> produce it here
                            if (!_nodes_names.Contains(name))
                            {
                                Node subN = new Node(name, string.Empty, Node.NO_DATA, Node.NO_DATA, Node.NO_DATA, true, false);
                                nodes_ordered.Add(subN);
                                n.AddNode(subN);
                                to_remove.Add(name);
                                continue;
                            }

                            // contained node has not been processed yet -> wait
                            int ind = nodes_ordered.FindIndex(x => x.NodeName == name);
                            if (ind < 0 || ind >= nodes_ordered.Count) continue;

                            // add a DEEP copy of the found node as a subnode to the current one
                            // do not add N to the list of ordered nodes yet
                            Node copy = new Node(nodes_ordered[ind], true);
                            n.AddNode(copy);
                            to_remove.Add(name);
                        }
                        foreach (string name in to_remove)
                            nodes_contained_node_names_remaining[i].Remove(name);
                    }
                }

                nrR = nodes_remaining.Count;
            }
        }

        private static List<Node> ConvertNodeAndLevelInfoToNodeList(List<Node> _nodes_at_allLevels, List<string> _nodes_names,
                    List<List<string>> _nodes_contained_node_names, List<int> _nodes_levels)
        {
            List<Node> nodeTree_FIN = new List<Node>(); // contains all finished nodes of level 0
            List<Node> nodeList_FIN = new List<Node>(); // contains UNIQUE SYNCED FINISHED nodes of various levels

            Stack<Node> nodeStack_OPEN = new Stack<Node>();
            Stack<int> nodeLevelStack_OPEN = new Stack<int>();
            Stack<List<string>> nodeContNamesStack_OPEN = new Stack<List<string>>();

            List<long> ids_remaining = _nodes_at_allLevels.Select(x => x.ID).ToList();
            
            int nrR = ids_remaining.Count;
            int nrTotal = nrR;            
            int nrIterations = 0;
            //string debug = "";
            while (nrR > 0 && nrIterations < NodeAssembler.MAX_NR_ITERATIONS)
            {           
                for (int i = 0; i < nrTotal; i++)
                {
                    // take current node, if not processed yet
                    if (!ids_remaining.Contains(_nodes_at_allLevels[i].ID))
                        continue;
                    Node n = _nodes_at_allLevels[i];
                    int nL = _nodes_levels[i];
                    List<string> nCN = _nodes_contained_node_names[i];
                    // add node to the open stack
                    nodeStack_OPEN.Push(n);
                    nodeLevelStack_OPEN.Push(nL);
                    nodeContNamesStack_OPEN.Push(nCN);

                    #region DEBUG
                    //debug += "i=" + i + " taken: " + _nodes_at_allLevels[i].NodeName + " at L" + _nodes_levels[i] + "\n";
                    //debug += "FINISHED TREE\n";
                    //foreach (Node ntf in nodeTree_FIN)
                    //{
                    //    debug += ntf.ToString() + "\n";
                    //}
                    //debug += "\nFINISHED NODES\n";
                    //foreach (Node ntl in nodeList_FIN)
                    //{
                    //    debug += ntl.ToString() + "\n";
                    //}
                    //debug += "\nOPEN STACK\n";
                    //foreach (Node nso in nodeStack_OPEN)
                    //{
                    //    debug += nso.ToString() + "\n";
                    //}
                    //debug += "\n";
                    #endregion

                    // PROCESS NODE -> try to resolve the contained node names
                    if (nCN.Count != 0)
                    {                        
                        List<string> to_remove = new List<string>();
                        int index_last_following_CN = i + 1;
                        foreach (string name in nCN)
                        {
                            // the name is not from an explicit node definition -> produce it here
                            if (!_nodes_names.Contains(name))
                            {
                                Node subN = new Node(name, string.Empty, Node.NO_DATA, Node.NO_DATA, Node.NO_DATA, true, false);
                                nodeTree_FIN.Add(subN);
                                NodeAssembler.AddNodeToListofFinishedNodes(subN, ref nodeList_FIN);
                                n.AddNode(subN);
                                to_remove.Add(name);
                                continue;
                            }
                            
                            // check if there isn't an unprocessed node FOLLOWING in the input node list                            
                            if (nrIterations == 0 && index_last_following_CN <= nrTotal - 1)
                            {
                                int index;
                                bool found = NodeAssembler.LookAhead(name, nL + 1, _nodes_at_allLevels, _nodes_levels, 
                                                                     index_last_following_CN, out index);
                                if (found)
                                {
                                    index_last_following_CN = index + 1;
                                    continue;
                                    // ... and wait for it
                                }
                            }
                            
                            // contained node has not been processed yet -> wait
                            int ind = nodeList_FIN.FindIndex(x => x.NodeName == name);
                            if (ind < 0 || ind >= nodeList_FIN.Count) continue;

                            // add a DEEP copy of the found node as a subnode to the current one
                            Node copy = new Node(nodeList_FIN[ind], true);
                            copy.NodeSource = "Copy";
                            n.AddNode(copy);
                            to_remove.Add(name);
                            //debug += "made deep copy\n\n";
                            
                        }
                        foreach (string name in to_remove)
                            nCN.Remove(name);
                    }

                    // FINALIZE NODE
                    if (nCN.Count == 0)
                    {                       
                        List<long> ids_finalized = new List<long>();
                        NodeAssembler.FinalizeNode(n, nL, ref nodeTree_FIN, ref nodeList_FIN,
                                    ref nodeStack_OPEN, ref nodeLevelStack_OPEN, ref nodeContNamesStack_OPEN,
                                    ref ids_finalized);
                        foreach (long id in ids_finalized)
                            ids_remaining.Remove(id);
                    }

                }
                nrIterations++;
                nrR = ids_remaining.Count;
                nodeStack_OPEN.Clear();
                nodeLevelStack_OPEN.Clear();
                nodeContNamesStack_OPEN.Clear();
            }

            return nodeTree_FIN;
        }

        private static List<Node> ConvertExplicitNodeAndLevelInfoToNodeList(List<Node> _nodes_at_allLevels, List<int> _nodes_levels)
        {
            List<Node> nodeTree_FIN = new List<Node>(); // contains all finished nodes of level 0
            
            Stack<Node> nodeStack_OPEN = new Stack<Node>();
            Stack<int> nodeLevelStack_OPEN = new Stack<int>();

            int nrTotal = _nodes_at_allLevels.Count;
            //string debug = "";
            for (int i = 0; i <= nrTotal; i++)
            {
                // take the next node
                Node n_next;
                int level_next;
                if (i < nrTotal)
                {
                    n_next = _nodes_at_allLevels[i];
                    level_next = _nodes_levels[i];
                }
                else
                {
                    // last cycle to empty the stacks
                    n_next = null;
                    level_next = 0;
                }

                // peek at the level of the node currently at the top of the stack
                int level_current = -1;
                if (nodeLevelStack_OPEN.Count > 0)
                    level_current = nodeLevelStack_OPEN.Peek();

                #region DEBUG

                //if (i < nrTotal)
                //    debug += "i=" + i + " taken: " + _nodes_at_allLevels[i].NodeName + " at L" + _nodes_levels[i] + "\n";
                
                //debug += "FINISHED TREE\n";
                //foreach (Node ntf in nodeTree_FIN)
                //{
                //    debug += ntf.ToString() + "\n";
                //}
                //debug += "\nOPEN STACK\n";
                //foreach (Node nso in nodeStack_OPEN)
                //{
                //    debug += nso.ToString() + "\n";
                //}
                //debug += "\n";
                //foreach (int nlso in nodeLevelStack_OPEN)
                //{
                //    debug += nlso.ToString() + "\n";
                //}
                //debug += "\n\n";

                #endregion

                if (level_current >= level_next + 1)
                {
                    // e.g. [stack: 0 1 2 3 3] [n:2 or n:1 or n:0]
                    // the next node n is on a higher level -> gather up the nodes already on the stack 
                    // until we reach a node on a higher level ->
                    // then attach them to it
                    while(level_current >= level_next + 1)
                    {
                        int nrN_remaining = nodeStack_OPEN.Count;
                        List<Node> nodes_to_attach = new List<Node>();
                        int level_to_attach = level_current;
                        while (nrN_remaining > 0 && level_current == level_to_attach)
                        {
                            Node n_toAt = nodeStack_OPEN.Pop();
                            level_to_attach = nodeLevelStack_OPEN.Pop();
                            nodes_to_attach.Add(n_toAt);

                            // iterate
                            nrN_remaining = nodeStack_OPEN.Count;
                            level_current = -1;
                            if (nrN_remaining > 0)
                                level_current = nodeLevelStack_OPEN.Peek();
                        }
                        nodes_to_attach.Reverse();

                        if (nrN_remaining > 0)
                        {
                            //attach
                            Node n_current = nodeStack_OPEN.Peek();
                            foreach (Node sN in nodes_to_attach)
                            {
                                n_current.AddNode(sN);
                            }
                            if (level_current == 0)
                            {
                                n_current = nodeStack_OPEN.Pop();
                                level_current = nodeLevelStack_OPEN.Pop();
                                nrN_remaining = nodeStack_OPEN.Count;
                                nodeTree_FIN.Add(n_current);
                            }
                        }
                        else
                        {
                            // we are done
                            nodeTree_FIN.AddRange(nodes_to_attach);
                        }
                    }
                }
                

                // if the node currently on top of the stack is at level 0 
                // and the next is also at level 0 -> extract the current (it is done)
                if (nodeStack_OPEN.Count > 0 && level_current == 0 && level_next == 0)
                {
                    Node nF = nodeStack_OPEN.Pop();
                    level_current = nodeLevelStack_OPEN.Pop();
                    nodeTree_FIN.Add(nF);
                }

                // add node to the open stack (unless last cycle)
                if (i < nrTotal)
                {
                    nodeStack_OPEN.Push(n_next);
                    nodeLevelStack_OPEN.Push(level_next);
                }
                              

            }

            return nodeTree_FIN;
        }

        /// <summary>
        /// For addining finalized nodes to the List of unique synched finalized nodes
        /// </summary>
        /// <param name="_n">node to be added</param>
        /// <param name="_finished">List of unique synched finalized nodes</param>
        private static void AddNodeToListofFinishedNodes(Node _n, ref List<Node> _finished)
        {
            if (_n == null || _finished == null) return;

            int ind = _finished.FindIndex(x => x.NodeName == _n.NodeName);
            if (ind < 0 || ind >= _finished.Count)
            {
                // a node of that name is NOT on the list
                _finished.Add(_n);
            }
            else
            {
                // found a node of the SAME NAME already on the list
                Node found = _finished[ind];
                if ((!found.SyncByName && _n.SyncByName) ||
                    (found.SyncByName && found.HasNonSyncNodes() && _n.SyncByName && !_n.HasNonSyncNodes()))
                {
                    // replace the found with the new one
                    _finished.Remove(found);
                    _finished.Add(_n);
                }
            }
        }

        /// <summary>
        /// For parsing EXCEL Input. Handles the finalization of a processed node.
        /// </summary>
        /// <param name="_n">the node to be finalized (no unresolved subnodes remaining)</param>
        /// <param name="_level">the Level of the node in the _nodeTree_FIN</param>
        /// <param name="_nodeTree_FIN">Tree of finalized nodes</param>
        /// <param name="_nodeList_FIN">List of unique synched finalized nodes</param>
        /// <param name="_nodeStack_OPEN">stack of unfinished nodes</param>
        /// <param name="_nodeLevelStack_OPEN">stackof the levels of the unfinished nodes</param>
        /// <param name="_nodeContNamesStack_OPEN">stack of the not yet preocessed subnode names of the unfinished nodes</param>
        /// <param name="_ids_finalized">the IDs of the finalized nodes (due to the tree structure there can be more than just _n)</param>
        private static void FinalizeNode(Node _n, int _level, ref List<Node> _nodeTree_FIN, ref List<Node> _nodeList_FIN,
            ref Stack<Node> _nodeStack_OPEN, ref Stack<int> _nodeLevelStack_OPEN, ref Stack<List<string>> _nodeContNamesStack_OPEN,
            ref List<long> _ids_finalized)
        {
            if (_n == null || _nodeTree_FIN == null || _nodeList_FIN == null ||
                _nodeStack_OPEN == null || _nodeLevelStack_OPEN == null || _nodeContNamesStack_OPEN == null ||
                _ids_finalized == null)
                return;

            int nrNodesOnStack = _nodeStack_OPEN.Count;
            if (nrNodesOnStack != _nodeLevelStack_OPEN.Count || nrNodesOnStack != _nodeContNamesStack_OPEN.Count)
                return;

            // 0: mark the node as finalized
            _ids_finalized.Add(_n.ID);

            // 1: remove node from the unfinished stacks, if it was on them
            if (nrNodesOnStack > 0)
            {
                Node n_on_top = _nodeStack_OPEN.Peek();
                if (n_on_top.ID == _n.ID)
                {
                    _nodeStack_OPEN.Pop();
                    _nodeLevelStack_OPEN.Pop();
                    _nodeContNamesStack_OPEN.Pop();
                }
            }

            // 2: add node to the list of unique synced finished nodes
            NodeAssembler.AddNodeToListofFinishedNodes(_n, ref _nodeList_FIN);

            // 3: ATTACH node
            if (_level == NodeAssembler.NODE_ROOT_LEVEL)
            {
                // add node to the tree of finalized nodes
                _nodeTree_FIN.Add(_n);
            }
            else
            {
                // attach to a node on the unfinished stacks ONE LEVEL higher
                if (_nodeStack_OPEN.Count > 0)
                {
                    int nL_at_top_of_stack = _nodeLevelStack_OPEN.Peek();
                    List<string> nCN_at_top_of_stack = _nodeContNamesStack_OPEN.Peek();
                    if (_level == nL_at_top_of_stack + 1 && nCN_at_top_of_stack.Contains(_n.NodeName))
                    {
                        // add the finalized LOWER-LEVEL node to the most recent node ONE LEVEL HIGHER
                        Node n_at_top_of_stack = _nodeStack_OPEN.Peek();
                        n_at_top_of_stack.AddNode(_n);
                        nCN_at_top_of_stack.Remove(_n.NodeName);
                        // check to see if the node ONE LEVEL HIGHER has just been finished
                        if (nCN_at_top_of_stack.Count == 0)
                        {
                            NodeAssembler.FinalizeNode(n_at_top_of_stack, nL_at_top_of_stack, 
                                ref _nodeTree_FIN, ref _nodeList_FIN,
                                ref _nodeStack_OPEN, ref _nodeLevelStack_OPEN, ref _nodeContNamesStack_OPEN,
                                ref _ids_finalized);
                        }
                    }
                }
            }

        }

        /// <summary>
        /// Looks in the list _nodes, starting at _start_index, for a particular node name at level _at_level.
        /// </summary>
        /// <param name="_name">the name to look for</param>
        /// <param name="_cutoff_level">the level the node with that name must have</param>
        /// <param name="_nodes">list of nodes</param>
        /// <param name="_levels">list of the nodes' levels</param>
        /// <param name="_start_index">position, from which to start the search</param>
        /// <param name="found_at_index">the index of the found node</param>
        /// <returns>TRUE if such a node is found</returns>
        private static bool LookAhead(string _name, int _at_level, List<Node> _nodes, List<int> _levels, int _start_index,
                                        out int found_at_index)
        {
            found_at_index = -1;
            if (string.IsNullOrEmpty(_name) || _at_level < 0 || _nodes == null || _levels == null)
                return false;

            int nrNodes = _nodes.Count;
            if (nrNodes == 0 || _levels.Count != nrNodes || _start_index < 0 || _start_index >= nrNodes)
                return false;

            for (int i = _start_index; i < nrNodes; i++)
            {
                // if a level closer to 0 comes up first, abandon the search
                if (_levels[i] < _at_level)
                    return false;

                if (string.Equals(_name, _nodes[i].NodeName) && _levels[i] == _at_level)
                {
                    found_at_index = i;
                    return true;
                }
            }

            return false;
        }

        public static void AttachNodesToNodeManager(List<Node> _nodes, ref NodeManager _manager)
        {
            if (_nodes == null || _manager == null) return;

            int nrN = _nodes.Count;
            if (nrN < 1) return;

            foreach(Node node in _nodes)
            {
                _manager.AddNode(node);
            }
        }

        #endregion

        #region IMPORTING UTILS: List<List<string>> operations

        private static List<List<string>> CutEmptyColumnsAndRows(List<List<string>> _input)
        {
            if (_input == null) return null;
            List<List<string>> result = new List<List<string>>();

            int nrRows = _input.Count;
            if (nrRows < 1) return result;
            int nrCols = _input[0].Count;

            // remove the empty rows
            List<int> ind_row_to_remove = new List<int>();
            for (int i = 0; i < nrRows; i++)
            {
                bool row_is_empty = true;
                for (int j = 0; j < nrCols; j++)
                {
                    row_is_empty &= string.IsNullOrEmpty(_input[i][j]);
                }
                if (row_is_empty)
                    ind_row_to_remove.Add(i);
            }
            List<List<string>> input_fullRows = new List<List<string>>();
            for (int i = 0; i < nrRows; i++)
            {
                if (!ind_row_to_remove.Contains(i))
                    input_fullRows.Add(_input[i]);
            }
            nrRows = input_fullRows.Count;
            if (nrRows < 1)
                return result;

            // remove the empty columns (using the first non-empty row (i.e. HEADER))
            List<int> ind_col_to_remove = new List<int>();
            for (int j = 0; j < nrCols; j++)
            {
                if (string.IsNullOrEmpty(input_fullRows[0][j]))
                    ind_col_to_remove.Add(j);
            }
            for (int i = 0; i < nrRows; i++)
            {
                List<string> row = new List<string>();
                for (int j = 0; j < nrCols; j++)
                {
                    if (!ind_col_to_remove.Contains(j))
                        row.Add(input_fullRows[i][j].Trim());
                }
                result.Add(row);
            }

            return result;
        }

        #endregion

        #region IMPORTING UTILS: String -> Node operations

        private static void ExtractInfoFromNodeStrings(List<List<string>> _string_data, int _nr_entries_per_node,
                    out List<Node> nodes_at_level0, out List<string> nodes_names,
                    out List<List<string>> nodes_contained_node_names, out List<int> nodes_levels)
        {
            nodes_at_level0 = new List<Node>();
            nodes_names = new List<string>();
            nodes_contained_node_names = new List<List<string>>();
            nodes_levels = new List<int>();

            // check data consistency
            if (_string_data == null) return;
            if (_nr_entries_per_node < 1) return;

            int nrRows = _string_data.Count;
            if (nrRows < 2) return;

            int nrFields = _string_data[0].Count;
            if (nrFields < _nr_entries_per_node) return;

            if (_nr_entries_per_node > 0 && !string.Equals(_string_data[0][0], NODE_NAME)) return;
            if (_nr_entries_per_node > 1 && !string.Equals(_string_data[0][1], NODE_SYNC_BY_NAME)) return;
            if (_nr_entries_per_node > 2 && !string.Equals(_string_data[0][2], NODE_DESCR)) return;
            if (_nr_entries_per_node > 3 && !string.Equals(_string_data[0][3], NODE_UNIT)) return;
            if (_nr_entries_per_node > 4 && !string.Equals(_string_data[0][4], NODE_DEFAULT_VALUE)) return;
            if (_nr_entries_per_node > 5 && !string.Equals(_string_data[0][5], NODE_SOURCE)) return;
            if (_nr_entries_per_node > 6 && !string.Equals(_string_data[0][6], NODE_MANAGER)) return;
            if (_nr_entries_per_node > 7 && !string.Equals(_string_data[0][7], NODE_GEOMETRY)) return;
            if (_nr_entries_per_node > 8 && !string.Equals(_string_data[0][8], NODE_CONTAINED_NODES)) return;
            if (_nr_entries_per_node > 9 && !string.Equals(_string_data[0][9], NODE_LEVEL)) return;

            // extract data
            for (int i = 1; i < nrRows; i++)
            {
                List<string> current = _string_data[i];

                int nrFields_current = current.Count;
                if (nrFields_current < _nr_entries_per_node)
                    continue;

                // cannot process nodes without a name
                if (string.IsNullOrEmpty(current[0]))
                    continue;

                // 0: extract NODE_NAME
                string name_current = current[0];
                // 1: extract NODE_SYNC_BY_NAME
                bool sync_by_name_current = true;
                if (_nr_entries_per_node > 1)
                {
                    string sync_by_name_current_str = string.IsNullOrEmpty(current[1]) ? "0" : current[1];
                    int sync_by_name_int_val = 0;
                    Int32.TryParse(sync_by_name_current_str, out sync_by_name_int_val);
                    sync_by_name_current = (sync_by_name_int_val == 1);
                }
                // 2: extract NODE_DESCR
                string descr_current = string.Empty;
                if (_nr_entries_per_node > 2)
                    descr_current = current[2];
                // 3: extract NODE_UNIT
                string unit_current = Node.NO_DATA;
                if (_nr_entries_per_node > 3)
                    unit_current = string.IsNullOrEmpty(current[3]) ? Node.NO_DATA : current[3];
                // 4: extract NODE_DEFAULT_VALUE
                string defVal_current = Node.NO_DATA;
                if (_nr_entries_per_node > 4)
                    defVal_current = string.IsNullOrEmpty(current[4]) ? Node.NO_DATA : current[4];
                // 5: extract NODE_SOURCE
                string source_current = Node.NO_DATA;
                if (_nr_entries_per_node > 5)
                    source_current = string.IsNullOrEmpty(current[5]) ? Node.NO_DATA : current[5];
                // 6: extract NODE_MANAGER
                string manager_current = Node.NO_DATA;
                if (_nr_entries_per_node > 6)
                    manager_current = string.IsNullOrEmpty(current[6]) ? Node.NO_DATA : current[6];
                // 7: extract NODE_GEOMETRY
                bool has_geometry_current = false;
                if (_nr_entries_per_node > 7)
                {
                    string geometry_current = string.IsNullOrEmpty(current[7]) ? "0" : current[7];
                    int geometry_int_val = 0;
                    Int32.TryParse(geometry_current, out geometry_int_val);
                    has_geometry_current = (geometry_int_val == 1);
                }
                // 8: extract NODE_CONTAINED_NODES
                List<string> contained_names_current = new List<string>();
                if (_nr_entries_per_node > 8)
                {
                    string contained_current = current[8];
                    string[] contained_single_current = contained_current.Split(NODE_SEPARATORS);
                    contained_names_current = new List<string>(contained_single_current);
                    contained_names_current.RemoveAll(x => x == string.Empty);
                }
                // 9: extract NODE_LEVEL
                int level_current = NODE_ROOT_LEVEL;
                if (_nr_entries_per_node > 9)
                    Int32.TryParse(current[9], out level_current);


                // ... and create NODE
                Node node_current = new Node(name_current, descr_current, unit_current, defVal_current, source_current,
                                             sync_by_name_current, has_geometry_current);
                node_current.NodeManager = NodePropertyValues.StringToNodeManagerType(manager_current);

                // ... add the information to the OUT ARRAYS
                nodes_at_level0.Add(node_current);
                nodes_names.Add(name_current);
                nodes_contained_node_names.Add(contained_names_current);
                nodes_levels.Add(level_current);
            }
        }

        #endregion

        #region EXPORTING: NODES -> STRINGS

        public static List<string> GetNodeListHeader()
        {
            List<string> header = new List<string>();
            header.Add(NODE_NAME);
            header.Add(NODE_SYNC_BY_NAME);
            header.Add(NODE_DESCR);
            header.Add(NODE_UNIT);
            header.Add(NODE_DEFAULT_VALUE);
            header.Add(NODE_SOURCE);
            header.Add(NODE_MANAGER);
            header.Add(NODE_GEOMETRY);
            header.Add(NODE_CONTAINED_NODES);
            return header;
        }

        public static List<string> GetNodeListHeaderComplete()
        {
            List<string> header = new List<string>();
            header.Add(NODE_NAME);
            header.Add(NODE_SYNC_BY_NAME);
            header.Add(NODE_DESCR);
            header.Add(NODE_UNIT);
            header.Add(NODE_DEFAULT_VALUE);
            header.Add(NODE_SOURCE);
            header.Add(NODE_MANAGER);
            header.Add(NODE_GEOMETRY);
            header.Add(NODE_CONTAINED_NODES);
            header.Add(NODE_LEVEL);
            return header;
        }


        public static List<List<string>> NodeListToStrings(List<Node> _nodes)
        {
            List<List<string>> nodes_as_strings = new List<List<string>>();
            if (_nodes == null) return nodes_as_strings;

            foreach(Node n in _nodes)
            {
                List<string> node_record = NodeAssembler.GetNodeRecordSimple(n);
                nodes_as_strings.Add(node_record);
            }

            return nodes_as_strings;
        }

        public static List<List<string>> NodeListToStringsComplete(List<Node> _nodes, bool _explicit = false)
        {
            List<List<string>> nodes_as_strings = new List<List<string>>();
            if (_nodes == null) return nodes_as_strings;

            foreach (Node n in _nodes)
            {
                List<string> node_record = NodeAssembler.GetNodeRecordSimple(n);
                node_record.Add("0"); // level
                nodes_as_strings.Add(node_record);

                List<List<string>> node_contained_records = NodeAssembler.GetSubNodeRecords(n, 0, _explicit);
                if (node_contained_records.Count > 0)
                    nodes_as_strings.AddRange(node_contained_records);                
            }

            return nodes_as_strings;
        }


        private static List<string> GetNodeRecordSimple(Node _node)
        {
            List<string> node_record = new List<string>();
            if (_node == null)
                return node_record;

            node_record.Add(_node.NodeName);
            string sync_by_name = (_node.SyncByName) ? "1" : "0";
            node_record.Add(sync_by_name);
            node_record.Add(_node.NodeDescr);
            node_record.Add(_node.NodeUnit);
            node_record.Add(_node.NodeDefaultVal);
            node_record.Add(_node.NodeSource);
            node_record.Add(NodePropertyValues.NodeManagerTypeToString(_node.NodeManager));

            string has_geom = (_node.HasGeometry) ? "1" : "0";
            node_record.Add(has_geom);

            string contained = "";
            if (_node.ContainedNodes.Count > 0)
            {
                foreach (Node subN in _node.ContainedNodes)
                {
                    contained += subN.NodeName + NODE_SEPARATORS[0] + " ";
                }
                contained = contained.Substring(0, contained.Length - 2);
            }
            node_record.Add(contained);

            return node_record;
        }

        private static List<List<string>> GetSubNodeRecords(Node _node, int _node_level, bool _explicit = false)
        {
            List<List<string>> subnode_records = new List<List<string>>();
            if (_node == null)
                return subnode_records;
            if (_node.ContainedNodes == null || _node.ContainedNodes.Count < 1)
                return subnode_records;

            foreach(Node subNode in _node.ContainedNodes)
            {
                if (!_explicit)
                {
                    if (subNode.SyncByName && !subNode.HasNonSyncNodes())
                        continue;
                }

                List<string> subnode_record = NodeAssembler.GetNodeRecordSimple(subNode);
                int subNode_level = _node_level + 1;
                subnode_record.Add(subNode_level.ToString());
                subnode_records.Add(subnode_record);
                
                List<List<string>> subnode_contained_records = NodeAssembler.GetSubNodeRecords(subNode, subNode_level, _explicit);
                if(subnode_contained_records.Count > 0)
                    subnode_records.AddRange(subnode_contained_records);
                
            }

            return subnode_records;
        }

        #endregion

    }
}
