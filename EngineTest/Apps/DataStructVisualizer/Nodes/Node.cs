using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataStructVisualizer.Nodes
{
    public enum NodeState { STANDARD, ISBEING_DELETED, ISBEING_RECONNECTED }

    [DataContract(Name = "Node", Namespace = "http://DataContractSerializer.Node/", IsReference = true)]
    public class Node : INotifyPropertyChanged, IComparable<Node>, IEquatable<Node>
    {
        #region STATIC

        protected static long NR_NODES = 0;
        public static readonly string NAME_DEFAULT = "Node";
        public static readonly string NO_DATA = "-no data-";
        public static readonly string REFERENCE = "VEREINFACHT";

        public static System.Windows.Media.Color COLOR_DEFAULT = System.Windows.Media.Colors.Black;
        public static System.Windows.Media.Color BACKGR_DEFAULT = System.Windows.Media.Colors.White;
        public static System.Windows.Media.Color HIGHLIGHT_DEFAULT = System.Windows.Media.Colors.OrangeRed;
        public static System.Windows.Media.Color MARKED_DEFAULT = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#c83800");

        public static System.Windows.FontWeight FONT_WEIGHT_DEFAULT = System.Windows.FontWeights.Normal;
        public static System.Windows.FontWeight FONT_WEIGHT_CONNECTED = System.Windows.FontWeights.Bold;
        public static System.Windows.FontWeight FONT_WEIGHT_HIGHLIGHT = System.Windows.FontWeights.ExtraBold;

        protected static int CTOR_CODE = 42;

        private static readonly string NODE_SYNCED, NODE_NOT_SYNCED;

        static Node()
        {
            char synced = '\u25c9';
            char not_synced = '\u25ce';
            NODE_SYNCED = synced.ToString();
            NODE_NOT_SYNCED = not_synced.ToString();
        }

        internal static void AdjustNrNodesAfterDeserialization(long _new_nr)
        {
            NR_NODES = Math.Max(NR_NODES, _new_nr);
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

        #region IComparable, IEquatable

        public int CompareTo(Node _n)
        {
            if (_n == null)
                return 1;

            if (this.ID < _n.ID)
                return 1;
            else if (this.ID > _n.ID)
                return -1;
            else
                return 0;
        }

        public bool Equals(Node _n)
        {
            return (this.ID == _n.ID);
        }

        #endregion

        #region PROPERTIES: ID, Name, Sync, Description, Unit, Dafault Value, Source

        [DataMember]
        public long ID { get; private set; }

        private string node_name;
        [DataMember]
        public string NodeName
        {
            get { return this.node_name; }
            set 
            {
                this.NodeName_Prev = this.node_name;
                if (this.was_created_by_ctor == Node.CTOR_CODE && this.node_name != value)
                    this.LastEdit = DateTime.Now;
                this.node_name = value;
                this.RegisterPropertyChanged("NodeName");               
            }
        }
        public string NodeName_Prev { get; private set; }


        private bool sync_by_name;
        [DataMember]
        [System.ComponentModel.DefaultValue(true)]
        public bool SyncByName
        {
            get { return this.sync_by_name; }
            set
            { 
                // not possible to turn it from NOT-SYNCHRONIZED to SYNCHRONIZED
                // (except during Deserialization)
                if (this.was_created_by_ctor == Node.CTOR_CODE && !this.sync_by_name && value == true)
                    return;

                if (this.was_created_by_ctor == Node.CTOR_CODE && this.sync_by_name != value)
                    this.LastEdit = DateTime.Now;
                this.sync_by_name = value;
                this.RegisterPropertyChanged("SyncByName");                
            }
        }

        private string node_descr;
        [DataMember]
        public string NodeDescr
        {
            get { return this.node_descr; }
            set 
            {
                if (this.was_created_by_ctor == Node.CTOR_CODE && this.node_descr != value)
                    this.LastEdit = DateTime.Now;
                this.node_descr = value;
                this.RegisterPropertyChanged("NodeDescr");                
            }
        }

        private string node_unit;
        [DataMember]
        public string NodeUnit
        {
            get { return this.node_unit; }
            set 
            {
                if (this.was_created_by_ctor == Node.CTOR_CODE && this.node_unit != value)
                    this.LastEdit = DateTime.Now;
                this.node_unit = value;
                this.RegisterPropertyChanged("NodeUnit");                
            }
        }
        
        private string node_default_val;
        [DataMember]
        public string NodeDefaultVal
        {
            get { return this.node_default_val; }
            set 
            {
                if (this.was_created_by_ctor == Node.CTOR_CODE && this.node_default_val != value)
                    this.LastEdit = DateTime.Now;
                this.node_default_val = value;
                this.RegisterPropertyChanged("NodeDefaultVal");               
            }
        }
        

        private string node_source;
        [DataMember]
        public string NodeSource
        {
            get { return this.node_source; }
            set 
            {
                if (this.was_created_by_ctor == Node.CTOR_CODE && this.node_source != value)
                    this.LastEdit = DateTime.Now;
                this.node_source = value;
                this.RegisterPropertyChanged("NodeSource");                
            }
        }
        
        #endregion

        #region PROPERTIES: Manager, Geometry, Connections, LastEdit, Mark, Parameter Type

        private NodeManagerType node_manager;
        [DataMember]
        public NodeManagerType NodeManager
        {
            get { return this.node_manager; }
            set 
            {
                if (this.was_created_by_ctor == Node.CTOR_CODE && this.node_manager != value)
                    this.LastEdit = DateTime.Now;
                this.node_manager = value;
                this.RegisterPropertyChanged("NodeManager");                
            }
        }

        private bool has_geometry;
        [DataMember]
        public bool HasGeometry
        {
            get { return this.has_geometry; }
            set 
            {
                if (this.was_created_by_ctor == Node.CTOR_CODE && this.has_geometry != value)
                    this.LastEdit = DateTime.Now;
                this.has_geometry = value;
                this.RegisterPropertyChanged("HasGeometry");               
            }
        }
        

        private List<Node> connection_to;
        [DataMember]
        public List<Node> ConnectionTo
        {
            get { return this.connection_to; }
            set 
            {
                if (this.was_created_by_ctor == Node.CTOR_CODE && this.connection_to != value)
                    this.LastEdit = DateTime.Now;
                this.connection_to = value;
                this.RegisterPropertyChanged("ConnectionTo");               
            }
        }

        private DateTime last_edit;
        [DataMember]
        public DateTime LastEdit
        {
            get { return this.last_edit; }
            private set 
            {
                this.last_edit = value;
                this.RegisterPropertyChanged("LastEdit");
            }
        }

        private bool is_marked;
        [DataMember]
        [System.ComponentModel.DefaultValue(false)]
        public bool IsMarked
        {
            get { return this.is_marked; }
            set 
            { 
                this.is_marked = value;
                this.NodeColor = (this.is_marked) ? Node.MARKED_DEFAULT : Node.COLOR_DEFAULT;
                this.RegisterPropertyChanged("IsMarked");
            }
        }

        private ParameterType node_param_type;
        [DataMember]
        [System.ComponentModel.DefaultValue(ParameterType.NONE)]
        public ParameterType NodeParamType
        {
            get { return this.node_param_type; }
            set 
            {
                if (this.was_created_by_ctor == Node.CTOR_CODE && this.node_param_type != value)
                    this.LastEdit = DateTime.Now;
                this.node_param_type = value;
                this.RegisterPropertyChanged("NodeParamType");
            }
        }

        #endregion

        #region PROPERTIES: Contained nodes

        private List<Node> contained_nodes;
        [DataMember]
        public List<Node> ContainedNodes
        {
            get { return this.contained_nodes; }
            set 
            {
                if (this.was_created_by_ctor == Node.CTOR_CODE && this.contained_nodes != value)
                    this.LastEdit = DateTime.Now;
                this.contained_nodes = value;
                this.RegisterPropertyChanged("ContainedNodes");                
            }
        }

        #endregion

        #region PROPERTIES: GUI Controls General: Color, FontWeight, TreeView: Selected, Expanded

        private System.Windows.Media.Color node_color_prev;
        private System.Windows.Media.Color node_color;
        public System.Windows.Media.Color NodeColor
        {
            get { return this.node_color; }
            set
            {
                this.node_color_prev = this.node_color;
                this.node_color = value;
                this.RegisterPropertyChanged("NodeColor");
            }
        }

        private System.Windows.Media.Color node_backgr;
        public System.Windows.Media.Color NodeBackgr
        {
            get { return this.node_backgr; }
            set 
            { 
                this.node_backgr = value;
                this.RegisterPropertyChanged("NodeBackgr");
            }
        }

        private System.Windows.FontWeight node_weight_prev;
        private System.Windows.FontWeight node_weight;
        public System.Windows.FontWeight NodeWeight
        {
            get { return this.node_weight; }
            set 
            {
                this.node_weight_prev = this.node_weight;
                this.node_weight = value;
                RegisterPropertyChanged("NodeWeight");
            }
        }

        private bool isExpanded;
        public bool IsExpanded
        {
            get { return this.isExpanded; }
            set
            {
                this.isExpanded = value;
                RegisterPropertyChanged("IsExpanded");
            }
        }

        private bool isSelected;
        public bool IsSelected
        {
            get { return this.isSelected; }
            set
            {
                this.isSelected = value;
                RegisterPropertyChanged("IsSelected");
            }
        }

        private bool isParent_ofSelected;
        public bool IsParentOfSelected
        {
            get { return this.isParent_ofSelected; }
            set 
            { 
                isParent_ofSelected = value;
                RegisterPropertyChanged("IsParentOfSelected");
            }
        }

        #endregion

        #region CLASS MEMBERS

        private int was_created_by_ctor;

        #endregion

        #region .CTOR

        public Node()
        {
            this.was_created_by_ctor = Node.CTOR_CODE;
            // general info
            this.ID = (++Node.NR_NODES);
            this.NodeName = Node.NAME_DEFAULT + " " + this.ID.ToString();
            this.sync_by_name = true;
            this.NodeDescr = string.Empty;
            this.NodeUnit = Node.NO_DATA;
            this.NodeDefaultVal = Node.NO_DATA;
            this.NodeSource = Node.NO_DATA;

            // specific SIMULTAN info
            this.NodeManager = NodeManagerType.NONE;
            this.HasGeometry = false;
            this.ConnectionTo = new List<Node>();
            this.ContainedNodes = new List<Node>();
            this.LastEdit = DateTime.Now;
            this.IsMarked = false;
            this.NodeParamType = ParameterType.NONE;

            // GUI
            this.isSelected = false;
            this.IsParentOfSelected = false;
            this.IsExpanded = false;
            this.NodeColor = Node.COLOR_DEFAULT;
            this.NodeBackgr = Node.BACKGR_DEFAULT;
            this.NodeWeight = Node.FONT_WEIGHT_DEFAULT;
        }

        public Node(string _name, string _descr, string _unit, string _defVal, string _source, bool _sync_by_name, bool _has_geom)
        {
            this.was_created_by_ctor = Node.CTOR_CODE;
            // general info
            this.ID = (++Node.NR_NODES);
            this.NodeName = _name;
            this.sync_by_name = _sync_by_name;
            this.NodeDescr = _descr;
            this.NodeUnit = _unit;
            this.NodeDefaultVal = _defVal;
            this.NodeSource = _source;

            // specific SIMULTAN info
            this.NodeManager = NodeManagerType.NONE;
            this.HasGeometry = _has_geom;
            this.ConnectionTo = new List<Node>();
            this.ContainedNodes = new List<Node>();
            this.LastEdit = DateTime.Now;
            this.IsMarked = false;
            this.NodeParamType = ParameterType.NONE;

            // GUI
            this.isSelected = false;
            this.IsParentOfSelected = false;
            this.IsExpanded = false;
            this.NodeColor = Node.COLOR_DEFAULT;
            this.NodeBackgr = Node.BACKGR_DEFAULT;
            this.NodeWeight = Node.FONT_WEIGHT_DEFAULT;
        }

        // Copy - Constructor
        public Node(Node _orginal, bool _deep_copy = false)
        {
            this.was_created_by_ctor = Node.CTOR_CODE;
            // general info
            this.ID = (++Node.NR_NODES);
            this.NodeName = _orginal.NodeName;
            this.sync_by_name = _orginal.SyncByName;
            this.NodeDescr = _orginal.NodeDescr;
            this.NodeUnit = _orginal.NodeUnit;
            this.NodeDefaultVal = _orginal.NodeDefaultVal;
            this.NodeSource = _orginal.NodeSource;

            // specific SIMULTAN info (connections are not copied)
            this.NodeManager = _orginal.NodeManager;
            this.HasGeometry = _orginal.HasGeometry;
            this.ConnectionTo = new List<Node>();
            if (!_deep_copy)               
                this.ContainedNodes = new List<Node>();
            else
                this.ContainedNodes = WinUtils.GeneralUtils.DeepCopyNodeList(_orginal.ContainedNodes);
            this.LastEdit = DateTime.Now;
            this.IsMarked = false;
            this.NodeParamType = _orginal.NodeParamType;

            // GUI
            this.isSelected = false;
            this.IsParentOfSelected = false;
            this.IsExpanded = false;
            this.NodeColor = Node.COLOR_DEFAULT;
            this.NodeBackgr = Node.BACKGR_DEFAULT;
            this.NodeWeight = Node.FONT_WEIGHT_DEFAULT;

            // event handlers for the Node Manager
            PropertyChangedEventHandler pceh_new = this.PropertyChanged;
            PropertyChangedEventHandler pceh_orig = _orginal.PropertyChanged;
            if (pceh_new == null && pceh_orig != null)
            {
                Delegate[] handlers = pceh_orig.GetInvocationList();
                foreach (Delegate h in handlers)
                {
                    PropertyChangedEventHandler pceh = h as PropertyChangedEventHandler;
                    NodeManager target = h.Target as NodeManager;
                    if (h != null && target != null)
                    {
                        this.PropertyChanged += pceh;
                    }
                }
            }
        }

        #endregion

        #region INIT: After De-Serialization, because the .CTOR does not get called

        // returns the max ID of this node or any contained in it
        public long InitAppearance()
        {
            // GUI
            this.isSelected = false;
            this.IsParentOfSelected = false;
            this.IsExpanded = false;
            this.NodeColor = (this.IsMarked) ? Node.MARKED_DEFAULT : Node.COLOR_DEFAULT;
            this.NodeBackgr = Node.BACKGR_DEFAULT;
            this.NodeWeight = (this.ConnectionTo.Count > 0) ? Node.FONT_WEIGHT_CONNECTED : Node.FONT_WEIGHT_DEFAULT;

            // declare 'normal'
            this.was_created_by_ctor = Node.CTOR_CODE;
            // correct timestamp
            if (this.LastEdit == DateTime.MinValue)
                this.LastEdit = DateTime.Now;

            // recursion for ID retrieval
            long maxID = this.ID;
            if (this.ContainedNodes.Count > 0)
            {
                foreach(Node sN in this.ContainedNodes)
                {
                    maxID = Math.Max(maxID, sN.InitAppearance());
                }
            }
            return maxID;
        }

        #endregion

        #region METHODS: Info

        public bool HasNonSyncNodes()
        {
            if (this.ContainedNodes.Count > 0)
            {
                foreach (Node subNode in this.ContainedNodes)
                {
                    if (!subNode.SyncByName)
                        return true;
                }
                foreach(Node subNode in this.ContainedNodes)
                {
                    return subNode.HasNonSyncNodes();
                }
            }
            return false;
        }

        public override string ToString()
        {
            string node_str = "Node ";
            node_str += this.NodeName + "[";
            node_str += (this.SyncByName) ? Node.NODE_SYNCED + " " : Node.NODE_NOT_SYNCED + " ";
            node_str += NodePropertyValues.NodeManagerTypeToString(this.NodeManager) + " ";
            node_str += "[";
            if (this.ContainedNodes.Count > 0)
            {
                foreach(Node sN in this.ContainedNodes)
                {
                    node_str += sN.NodeName;
                    node_str += (sN.SyncByName) ? Node.NODE_SYNCED : Node.NODE_NOT_SYNCED;
                    node_str += " ";
                }
            }
            node_str += "]]";

            return node_str;
        }

        #endregion

        #region METHODS: GUI

        public void ExpandNode()
        {
            this.IsExpanded = true;
            if (this.ContainedNodes.Count > 0)
            {
                foreach (Node n in this.ContainedNodes)
                {
                    n.ExpandNode();
                }
            }
        }

        public void CollapseNode()
        {
            this.IsExpanded = false;
            if (this.ContainedNodes.Count > 0)
            {
                foreach (Node n in this.ContainedNodes)
                {
                    n.CollapseNode();
                }
            }
        }

        public void RestorePrevColor()
        {
            this.node_color = this.node_color_prev;
            this.NodeColor = (this.IsMarked) ? Node.MARKED_DEFAULT : Node.COLOR_DEFAULT;
        }

        public void RestoreFontWeight()
        {
            this.node_weight = this.node_weight_prev;
            this.NodeWeight = (this.ConnectionTo.Count > 0) ? Node.FONT_WEIGHT_CONNECTED : Node.FONT_WEIGHT_DEFAULT;
        }

        public void Highlight()
        {
            this.NodeColor = Node.HIGHLIGHT_DEFAULT;
            this.NodeWeight = Node.FONT_WEIGHT_HIGHLIGHT;
            this.IsExpanded = true;
        }

        public void UnHighlight()
        {
            this.RestorePrevColor();
            this.RestoreFontWeight();
        }

        #endregion

        #region METHODS: List Management (contained nodes)

        public bool AddNode(Node _n, bool _pass_on_own_event_handler = false)
        {
            // cannot assign self as SubEntity 
            if (Object.ReferenceEquals(this, _n)) return false;

            // cannot create a NULL SubEntity 
            if (Object.ReferenceEquals(_n, null)) return false;

            // cannot take same Subentity twice
            if (this.ContainedNodes.Contains(_n)) return false;

            // add event handlers for the NodeManager 
            // (for generating new nodes lower than the ROOT of the tree)
            if (_pass_on_own_event_handler)
            {
                PropertyChangedEventHandler pceh_n = _n.PropertyChanged;
                PropertyChangedEventHandler pceh_this = this.PropertyChanged;
                if (pceh_n == null && pceh_this != null)
                {
                    Delegate[] handlers = pceh_this.GetInvocationList();
                    foreach (Delegate h in handlers)
                    {
                        PropertyChangedEventHandler pceh = h as PropertyChangedEventHandler;
                        NodeManager target = h.Target as NodeManager;
                        if (h != null && target != null)
                        {
                            _n.PropertyChanged += pceh;
                        }
                    }
                }
            }

            // add node to SELF
            this.ContainedNodes.Add(_n);
            this.LastEdit = DateTime.Now;
            return true;
        }

        public void AddEventHandler(PropertyChangedEventHandler _handler)
        {
            this.PropertyChanged += _handler;
            foreach(Node n in this.ContainedNodes)
            {
                n.AddEventHandler(_handler);
            }
        }

        public bool RemoveNode(Node _n)
        {
            if (_n == null) return false;
            bool succes_level0 = this.ContainedNodes.Remove(_n);
            if (succes_level0)
            {
                this.LastEdit = DateTime.Now;
                return true;
            }
            else
            {
                foreach (Node subN in this.ContainedNodes)
                {
                    bool succes_levelN = subN.RemoveNode(_n);
                    if (succes_levelN)
                        return true;
                }
                return false;
            }
        }

        public void RemoveEventHandler(PropertyChangedEventHandler _handler)
        {
            this.PropertyChanged -= _handler;
            foreach (Node n in this.ContainedNodes)
            {
                n.RemoveEventHandler(_handler);
            }
        }

        public bool MoveContainedNodeInList(Node _n, bool _up)
        {
            if (_n == null) return false;

            int nrNL1 = this.ContainedNodes.Count;
            int ind = this.ContainedNodes.FindIndex(x => x.ID == _n.ID);
            if (ind < 0 || ind >= nrNL1)
            {
                return false;
            }
            else
            {
                if (_up)
                {
                    if (ind == 0) return false;
                    this.ContainedNodes.Reverse(ind - 1, 2);
                }
                else
                {
                    if (ind == nrNL1 - 1) return false;
                    this.ContainedNodes.Reverse(ind, 2);
                }
                this.LastEdit = DateTime.Now;
                return true;
            }
        }

        public void SetLastEditsTo(DateTime _reset_val)
        {
            this.LastEdit = _reset_val;
            foreach (Node n in this.ContainedNodes)
            {
                n.SetLastEditsTo(_reset_val);
            }
        }

        public List<Node> GetFlatSubNodeList()
        {
            if (this.ContainedNodes == null || this.ContainedNodes.Count < 1)
                return new List<Node>();

            List<Node> list = new List<Node>();
            foreach (Node n in this.ContainedNodes)
            {              
                list.Add(n);
                if (n.ContainedNodes.Count > 0)
                    list.AddRange(n.GetFlatSubNodeList());
            }
            return list;
        }

        #endregion

        #region METHODS: List management (Connections)

        public bool AddConnectionToNode(Node _n)
        {
            // cannot connect to self 
            if (Object.ReferenceEquals(this, _n)) return false;

            // cannot connect to NULL 
            if (Object.ReferenceEquals(_n, null)) return false;

            // cannot connect to the same Node twice
            if (this.ConnectionTo.Contains(_n)) return false;

            // perform CONNECT
            //_n.PropertyChanged += Node_PropertyChanged;
            this.ConnectionTo.Add(_n);           
            this.NodeWeight = System.Windows.FontWeights.Bold;
            _n.ConnectionTo.Add(this);
            _n.NodeWeight = System.Windows.FontWeights.Bold;
            this.LastEdit = DateTime.Now;
            return true;
        }

        public bool RemoveConnectionToNode(Node _n)
        {
            if (_n == null) return false;
            //_n.PropertyChanged -= Node_PropertyChanged;
            
            bool success1 =  this.ConnectionTo.Remove(_n);
            if (this.ConnectionTo.Count < 1)
                this.NodeWeight = System.Windows.FontWeights.Normal;
            bool success2 = _n.ConnectionTo.Remove(this);
            if (_n.ConnectionTo.Count < 1)
                _n.NodeWeight = System.Windows.FontWeights.Normal;

            this.LastEdit = DateTime.Now;
            return success1 && success2;
        }

        public void RemoveAllConnections()
        {
            if (this.ConnectionTo.Count > 0)
            {
                foreach(Node n in this.ConnectionTo)
                {
                    n.ConnectionTo.Remove(this);
                    if (n.ConnectionTo.Count < 1)
                        n.NodeWeight = System.Windows.FontWeights.Normal;
                }
            }
            this.ConnectionTo.Clear();
            this.NodeWeight = System.Windows.FontWeights.Normal;
            this.LastEdit = DateTime.Now;
        }

        #endregion

        #region EVENT HANDLERS

        // EVENT HANDLERS HAVE TO BE RE-ADDED AFTER DE-SERIALIZATION !!!

        //protected void Node_PropertyChanged(object sender, PropertyChangedEventArgs e)
        //{
        //    Node n = sender as Node;
        //    if (n == null || e == null)
        //        return;

        //    if (e.PropertyName == "State")
        //    {
        //        if (n.State == NodeState.ISBEING_DELETED || n.State == NodeState.ISBEING_RECONNECTED)
        //        {
        //            n.PropertyChanged -= Node_PropertyChanged;
        //        }
        //    }
        //}

        #endregion
    }

    public class NodeComparer : IEqualityComparer<Node>
    {
        public bool Equals(Node _n1, Node _n2)
        {
            //Check whether the compared objects reference the same data. 
            if (Object.ReferenceEquals(_n1, _n2)) return true;

            //Check whether any of the compared objects is null. 
            if (Object.ReferenceEquals(_n1, null) || Object.ReferenceEquals(_n2, null))
                return false;

            return (_n1.ID == _n2.ID);
        }

        // If Equals() returns true for a pair of objects  
        // then GetHashCode() must return the same value for these objects. 
        public int GetHashCode(Node _n)
        {
            //Check whether the object is null 
            if (Object.ReferenceEquals(_n, null)) return 0;

            //Get hash code for the ID field
            return _n.ID.GetHashCode();
        }
    }

    public class NodeLastEditComparer : IComparer<Node>
    {
        public int Compare(Node _n1, Node _n2)
        {
            if (_n1 == null && _n2 == null) return 0;
            if (_n1 != null && _n2 == null) return 1;
            if (_n1 == null && _n2 != null) return -1;

            return (_n1.LastEdit.CompareTo(_n2.LastEdit));
        }
    }
}
