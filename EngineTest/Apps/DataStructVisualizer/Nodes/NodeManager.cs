using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel;
using System.Windows.Data;

namespace DataStructVisualizer.Nodes
{
    public class NodeManager : INotifyPropertyChanged
    {

        #region STAIC

        private static int LAST_EDIT_TOUR_STEP = 500; // milisecs
        public static readonly Node ROOT;

        static NodeManager()
        {
            ROOT = new Node("ROOT", string.Empty, Node.NO_DATA, Node.NO_DATA, Node.NO_DATA, true, false);
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RegisterPropertyChanged(string _propName)
        {
            if (_propName == null)
                return;

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(_propName));
        }

        #endregion  

        #region INIT

        // managed nodes
        public List<Node> Nodes { get; private set; }
        private List<Node> nodes_flat_list;

        private List<string> node_names;
        public List<string> Node_Names 
        {
            get { return this.node_names; } 
            private set
            {
                this.node_names = value;
                this.RegisterPropertyChanged("Node_Names");
            }
        }
        
        private bool sync_running;

        // search function
        private List<Node> nodes_flat;
        IEnumerator<Node> matchingNodeEnumerator;
        private string prev_search_text;

        // .ctor
        public NodeManager()
        {
            this.Nodes = new List<Node>();
            this.Node_Names = new List<string>();
            this.sync_running = false;
        }

        #endregion

        #region NODE LIST MANAGEMENT: SORTING

        public void MoveNodeInList(Node _n, Node _nParent, bool _up)
        {
            if (_n == null)
                return;

            if (_nParent == null)
            {
                // Node is on Level 0
                int nrNL0 = this.Nodes.Count;
                int ind = this.Nodes.FindIndex(x => x.ID == _n.ID);
                if (ind < 0 || ind >= nrNL0)
                    return;

                if (_up)
                {
                    if (ind == 0) return;
                    this.Nodes.Reverse(ind - 1, 2);
                }
                else
                {
                    if (ind == nrNL0 - 1) return;
                    this.Nodes.Reverse(ind, 2);
                }
            }
            else
            {
                // Node is deeper in the tree
                _nParent.MoveContainedNodeInList(_n, _up);
            }
            this.nodes_flat_list = null;
        }

        public ListCollectionView SortNodeListBy(string _propName, bool _ascending)
        {
            ListCollectionView view = new ListCollectionView(this.Nodes);
            SortDescription sort;
            if (_ascending)
                sort = new SortDescription(_propName, ListSortDirection.Ascending);
            else
                sort = new SortDescription(_propName, ListSortDirection.Descending);
            view.SortDescriptions.Add(sort);

            return view;
        }


        #endregion

        #region NODE MANAGEMENT

        public bool AddNode(Node _node, bool _allowDuplicateNames = true)
        {
            if (_node == null) return false;
            if (this.Nodes.Contains(_node)) return false;

            // no duplicate names
            if (!_allowDuplicateNames)
            {
                List<Node> nodes_w_same_name = this.Nodes.FindAll(x => x.NodeName == _node.NodeName);
                if (nodes_w_same_name != null && nodes_w_same_name.Count > 0)
                    return false;
            }

            _node.AddEventHandler(node_PropertyChanged);
            this.Nodes.Add(_node);
            this.nodes_flat_list = null;
            return true;
        }

        public bool AddNodeToNode(Node _node, Node _parent, bool _add_event_handler)
        {
            if (_node == null || _parent == null) return false;
            List<Node> nodes_flat = this.GetFlatNodeList();
            //if (!nodes_flat.Contains(_parent)) return false;

            if (_add_event_handler)
                _node.AddEventHandler(node_PropertyChanged);

            _parent.AddNode(_node);
            this.nodes_flat_list = null;
            return true;
        }

        public Node GetParentNode(Node _node)
        {
            if (_node == null) return null;

            // top level
            if (this.Nodes.Contains(_node)) return null;

            foreach (Node item in this.Nodes)
            {
                List<Node> itemContent = item.GetFlatSubNodeList();
                if (itemContent.Contains(_node))
                    return item;
            }

            return null;
        }

        // returns the ancestor chain starting at the highest level and ending with the input node
        public List<Node> GetParentNodeChain(Node _node)
        {
            if (_node == null) return null;

            // if at top level
            if (this.Nodes.Contains(_node)) return new List<Node> { _node };

            List<Node> chain = new List<Node>();

            // get top-level parent
            Node topParent = null;
            foreach (Node item in this.Nodes)
            {
                List<Node> itemContent = item.GetFlatSubNodeList();
                if (itemContent.Contains(_node))
                {
                    topParent = item;
                    break;
                }
            }

            if (topParent == null)
                return new List<Node> { _node };

            // proceed from the top parent down
            chain.Add(topParent);
            Node currentParent = topParent;
            while (currentParent.ID != _node.ID)
            {
                foreach (Node e in currentParent.ContainedNodes)
                {
                    Node subNode = e as Node;
                    if (subNode == null)
                        continue;

                    if (subNode.ID == _node.ID)
                    {
                        currentParent = subNode;
                        break;
                    }

                    List<Node> subNodeContent = subNode.GetFlatSubNodeList();
                    if (subNodeContent.Contains(_node))
                    {
                        currentParent = subNode;
                        break;
                    }
                }
                chain.Add(currentParent);
            }
            return chain;
        }

        public bool RemoveNode(Node _node, bool _remove_event_handler = true)
        {
            if (_node == null) return false;

            this.nodes_flat_list = null;
            if (_remove_event_handler)
                _node.RemoveEventHandler(node_PropertyChanged);
            _node.RemoveAllConnections();
            bool success_level0 = this.Nodes.Remove(_node);
            if (success_level0)
                return true;
            else
            {
                foreach (Node subNode in this.Nodes)
                {
                    bool success_levelN = subNode.RemoveNode(_node);
                    if (success_levelN)
                        return true;
                }
                return false;
            }
        }

        public bool MoveNode(Node _node, Node _toNode)
        {
            if (_node == null || _toNode == null) return false;
            if (_node.ID == _toNode.ID) return false;
            if (_toNode.ContainedNodes.Contains(_node)) return false;

            this.nodes_flat_list = null;

            // remove layer from old parent layer or from the top of the hierarchy
            bool success = this.RemoveNode(_node, false);
            if (!success) return false;

            // add to the destination layer
            if (_toNode != ROOT)
                success = _toNode.AddNode(_node);
            else
                success = this.AddNode(_node);
            return success;
        }

        public Node FindNodeByName(string _name)
        {
            if (_name == null || _name == string.Empty)
                return null;

            List<Node> flatLayers = this.GetFlatNodeList();
            return flatLayers.Find(x => x.NodeName == _name);
        }

        public Node FindNodeByNameAndParent(string _name, Node _parent)
        {
            if (_name == null || _name == string.Empty || _parent == null)
                return null;

            List<Node> flatNodes = this.GetFlatNodeList();
            List<Node> found = flatNodes.FindAll(x => x.NodeName == _name);
            if (found == null || found.Count < 1)
                return null;

            foreach (Node node in found)
            {
                Node parent = this.GetParentNode(node);
                if (parent != null && parent.ID == _parent.ID)
                    return node;
            }
            return null;
        }

        #endregion

        #region NODE MANAGEMENT - SYNC OPERATIONS

        // handles synchronization btw nodes with property SynchByName = true
        private void node_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Node node = sender as Node;
            if (!this.sync_running && node != null && node.SyncByName && e != null && !string.IsNullOrEmpty(e.PropertyName))
            {
                this.sync_running = true;
                switch (e.PropertyName)
                {
                    case "NodeName":
                        this.SyncAllNames(node.NodeName_Prev, node.NodeName, node);
                        break;
                    case "NodeDescr":
                        //this.SyncAllDescr(node.NodeName, node.NodeDescr);
                        break;
                    case "NodeUnit":
                        this.SyncAllUnits(node.NodeName, node.NodeUnit);
                        break;
                    case "NodeDefaultVal":
                        //this.SyncAllDefaultVals(node.NodeName, node.NodeDefaultVal);
                        break;
                    case "NodeSource":
                        this.SyncAllSources(node.NodeName, node.NodeSource);
                        break;
                    case "NodeManager":
                        this.SyncAllManagers(node.NodeName, node.NodeManager);
                        break;
                    case "HasGeometry":
                        this.SyncAllGeometry(node.NodeName, node.HasGeometry);
                        break;
                }
                this.sync_running = false;
            }
        }

        private void SyncAllNames(string _searchName, string _newName, Node _sender)
        {
            if (string.IsNullOrEmpty(_newName)) return;
            if (_searchName == _newName) return;

            List<Node> to_sync = this.GetNodesToSync(_searchName);
            Node to_sync_with = this.GetNodeToSyncWith(_newName, _sender);
            if (to_sync.Count > 0)
            {
                // an EXISTING node
                if (to_sync_with == null)
                {
                    // no name overlap: perform simple sync
                    this.Node_Names.Remove(_searchName);
                    this.Node_Names.Add(_newName);
                    foreach (Node n in to_sync)
                        n.NodeName = _newName;
                }
                else
                {
                    // CAUTION!!! name overlap -> merge definitions
                    // the user gives feedback prior to this action: in the event handler in App.xaml.cs                   
                    this.Node_Names.Remove(_searchName);
                    // perform full sync
                    _sender.NodeDescr = to_sync_with.NodeDescr;
                    _sender.NodeUnit = to_sync_with.NodeUnit;
                    _sender.NodeDefaultVal = to_sync_with.NodeDefaultVal;
                    _sender.NodeSource = to_sync_with.NodeSource;
                    _sender.NodeManager = to_sync_with.NodeManager;
                    _sender.HasGeometry = to_sync_with.HasGeometry;
                    foreach (Node n in to_sync)
                    {
                        n.NodeName = _newName;
                        n.NodeDescr = to_sync_with.NodeDescr;
                        n.NodeUnit = to_sync_with.NodeUnit;
                        n.NodeDefaultVal = to_sync_with.NodeDefaultVal;
                        n.NodeSource = to_sync_with.NodeSource;
                        n.NodeManager = to_sync_with.NodeManager;
                        n.HasGeometry = to_sync_with.HasGeometry;
                    }                   
                }
            }
            else
            {
                // a NEW node:
                // see if another node with the new name exists and sync with it (i.e. _newName = 'Liste von')
                if (_sender.SyncByName)
                {                    
                    if (to_sync_with != null)
                    {
                        _sender.NodeDescr = to_sync_with.NodeDescr;
                        _sender.NodeUnit = to_sync_with.NodeUnit;
                        _sender.NodeDefaultVal = to_sync_with.NodeDefaultVal;
                        _sender.NodeSource = to_sync_with.NodeSource;
                        _sender.NodeManager = to_sync_with.NodeManager;
                        _sender.HasGeometry = to_sync_with.HasGeometry;
                    }
                }
            }
            // force update in AppViewModel
            List<string> nn_tmp = this.Node_Names;
            this.Node_Names = new List<string>(nn_tmp);
        }

        private void SyncAllDescr(string _searchName, string _newDescr)
        {
            List<Node> to_sync = this.GetNodesToSync(_searchName);
            foreach (Node n in to_sync)
                n.NodeDescr = _newDescr;
        }

        private void SyncAllUnits(string _searchName, string _newUnit)
        {
            List<Node> to_sync = this.GetNodesToSync(_searchName);            
            foreach (Node n in to_sync)
                n.NodeUnit = _newUnit;
        }

        private void SyncAllDefaultVals(string _searchName, string _newDV)
        {
            List<Node> to_sync = this.GetNodesToSync(_searchName);
            foreach (Node n in to_sync)
                n.NodeDefaultVal = _newDV;
        }

        private void SyncAllSources(string _searchName, string _newSource)
        {
            List<Node> to_sync = this.GetNodesToSync(_searchName);
            foreach (Node n in to_sync)
                n.NodeSource = _newSource;
        }

        private void SyncAllManagers(string _searchName, NodeManagerType _newManager)
        {
            List<Node> to_sync = this.GetNodesToSync(_searchName);
            foreach (Node n in to_sync)
                n.NodeManager = _newManager;
        }

        private void SyncAllGeometry(string _searchName, bool _newHasGeom)
        {
            List<Node> to_sync = this.GetNodesToSync(_searchName);
            foreach (Node n in to_sync)
                n.HasGeometry = _newHasGeom;
        }

        private List<Node> GetNodesToSync(string _searchName)
        {
            if (string.IsNullOrEmpty(_searchName))
                return new List<Node>();

            List<Node> flatLayers = this.GetFlatNodeList();
            return flatLayers.FindAll(x => (x.NodeName == _searchName && x.SyncByName));
        }

        private Node GetNodeToSyncWith(string _searchName, Node _searchingNode)
        {
            if (string.IsNullOrEmpty(_searchName) || _searchingNode == null)
                return null;

            List<Node> flatLayers = this.GetFlatNodeList();
            return flatLayers.Find(x => (x.ID != _searchingNode.ID && x.NodeName == _searchName && x.SyncByName));
        }


        #endregion

        #region NODE MANAGEMENT - LAST EDIT 

        public void SetAllLastEditsToNow()
        {
            if (this.Nodes == null) return;

            DateTime now = DateTime.Now;
            foreach(Node n in this.Nodes)
            {
                n.SetLastEditsTo(now);
            }
        }

        
        public async Task TourNodesAccordingToLastEdit(Node _selected, CancellationToken _ct)
        {
            List<Node> nodes_flat = this.GetFlatNodeList();
            nodes_flat.Sort(new NodeLastEditComparer());
            DateTime oldest = nodes_flat[0].LastEdit;
            if (_selected != null)
                oldest = _selected.LastEdit;
            
            foreach(Node n in nodes_flat)
            {
                if (n.LastEdit < oldest) continue;
                if (_ct != null && _ct.IsCancellationRequested) break;
                await Task.Delay(NodeManager.LAST_EDIT_TOUR_STEP, _ct);
                this.SelectNode(n);
            }
        }

        #endregion

        #region INFO EXTRACTION
        // updates also the search list and the 
        // text completion list of all unique names of nodes
        public List<Node> GetFlatNodeList()
        {
            if (this.nodes_flat_list == null)
            {
                this.nodes_flat = null; // used for searching -> clear it here
                this.nodes_flat_list = new List<Node>();
                this.nodes_flat_list.Add(ROOT);
                foreach (Node node in this.Nodes)
                {
                    this.nodes_flat_list.Add(node);
                    this.nodes_flat_list.AddRange(node.GetFlatSubNodeList());
                }    
                // fill in the list of node names
                var all_node_names = this.nodes_flat_list.Select(x => x.NodeName);
                this.Node_Names = all_node_names.Distinct().ToList();
                this.Node_Names.Remove("ROOT");
            }
            return this.nodes_flat_list;
        }

        #endregion

        #region NODE SEARCH: by Name

        public Node ProcessSearch(string _partName)
        {
            if (_partName == null || _partName == String.Empty)
                return null;

            if (this.nodes_flat == null)
            {
                this.nodes_flat = new List<Node>();
                this.nodes_flat.AddRange(this.GetFlatNodeList());
            }

            if (this.nodes_flat.Count < 1)
                return null;

            if (this.matchingNodeEnumerator == null || !this.matchingNodeEnumerator.MoveNext() || prev_search_text != _partName)
                this.VerifyMatches(_partName);

            Node n = this.matchingNodeEnumerator.Current;
            // update GUI parameters
            if (n != null)
            {
                Node node = n as Node;
                if (node != null)
                    this.SelectNode(node);
            }

            this.prev_search_text = _partName;
            return n;
        }

        private void VerifyMatches(string _text)
        {
            IEnumerable<Node> matchingEntities = this.FindMatches(_text);
            this.matchingNodeEnumerator = matchingEntities.GetEnumerator();

            if (!matchingNodeEnumerator.MoveNext())
                MessageBox.Show("No matching names were found.", "Try Again", MessageBoxButton.OK, MessageBoxImage.Information);
        }


        private IEnumerable<Node> FindMatches(string _text)
        {
            foreach (Node n in this.nodes_flat)
            {
                if (n.NodeName.Contains(_text))
                    yield return n;
            }
        }

        #endregion

        #region SELECTION HANDLING

        public void SelectNode(Node _node)
        {
            List<Node> nList = this.GetFlatNodeList();
            foreach (Node node in nList)
            {
                if (node == null || _node == null || node.ID != _node.ID)
                {
                    node.IsSelected = false;
                    node.IsParentOfSelected = false;
                }
                else
                {
                    node.IsSelected = true;
                    List<Node> parents = this.GetParentNodeChain(node);
                    if (parents != null && parents.Count > 0)
                    {
                        foreach(Node parent in parents)
                        {
                            if (parent.ID == _node.ID)
                                continue;
                            parent.IsParentOfSelected = true;
                            parent.IsExpanded = true;
                        }                       
                    }
                }
            }
        }

        #endregion

        #region GUI

        public void CollapseAll()
        {
            foreach(Node n in this.Nodes)
            {
                n.CollapseNode();
            }
        }

        public void ExpandAll()
        {
            foreach(Node n in this.Nodes)
            {
                n.ExpandNode();
            }
        }

        public void HighlightConnectedNodes(Node _node)
        {
            List<Node> nList = this.GetFlatNodeList();
            List<Node> nToShow = _node.ConnectionTo;
            foreach (Node node in nList)
            {
                if (node != null && nToShow.Contains(node))
                {
                    node.Highlight();
                    List<Node> parents = this.GetParentNodeChain(node);
                    if (parents != null && parents.Count > 0)
                    {
                        foreach (Node parent in parents)
                        {
                            parent.IsExpanded = true;
                        }
                    }
                }
            }
        }

        public void UnHighlightConnectedNodes(Node _node)
        {
            List<Node> nList = this.GetFlatNodeList();
            List<Node> nToShow = _node.ConnectionTo;
            foreach (Node node in nList)
            {
                if (node != null && nToShow.Contains(node))
                    node.UnHighlight();                   
            }
        }

        #endregion
    }
}
