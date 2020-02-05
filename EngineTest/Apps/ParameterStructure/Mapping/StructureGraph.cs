using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ParameterStructure.Component;
using ParameterStructure.Parameter;
using ParameterStructure.Geometry;

namespace ParameterStructure.Mapping
{
    public class StructureNode
    {
        #region STATIC: Operators

        public static bool operator ==(StructureNode _sn1, StructureNode _sn2)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(_sn1, _sn2))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)_sn1 == null) || ((object)_sn2 == null))
            {
                return false;
            }

            // Return true if the fields match:
            return _sn1.ContentMatch(_sn2);
        }

        public static bool operator !=(StructureNode _hc1, StructureNode _hc2)
        {
            return !(_hc1 == _hc2);
        }

        #endregion

        #region STATIC: Instance creation general

        /// <summary>
        /// If any of the numeric values is set to '-1' or if the string value is null or empty, it is interpreted as 'not used'.
        /// </summary>
        public static StructureNode CreateFrom(Type _type, long _id_as_long, int _id_as_int_1, int _id_as_int_2, string _id_as_string, StructureNode _sn_parent)
        {
            if (_type == null) return null;

            // create the node
            StructureNode node = new StructureNode();

            // content
            if (_id_as_long != -1)
            {
                node.IDAsLong = _id_as_long;
                node.IDAsLong_Used = true;
            }
            if (_id_as_int_1 != -1)
            {
                node.IDAsInt_1 = _id_as_int_1;
                node.IDAsInt_1_Used = true;
            }
            if (_id_as_int_2 != -1)
            {
                node.IDAsInt_2 = _id_as_int_2;
                node.IDAsInt_2_Used = true;
            }
            if (!string.IsNullOrEmpty(_id_as_string))
            {
                node.IDAsString = _id_as_string;
                node.IDAsString_Used = true;
            }
            node.ContentType = _type;
            node.ContentType_Used = true;

            // structure
            node.ParentNode = _sn_parent;
            if (_sn_parent != null)
            {
                if (!_sn_parent.children_nodes.Contains(node))
                    _sn_parent.children_nodes.Add(node);
            }

            return node;
        }

        #endregion

        #region STATIC: Instance creation specific

        public static StructureNode CreateFrom(Component.Component _node_source, StructureNode _sn_parent)
        {
            if (_node_source == null) return null;

            // create the node
            StructureNode node = new StructureNode();

            // content
            node.IDAsLong = _node_source.ID;
            node.IDAsLong_Used = true;
            node.ContentType = typeof(Component.Component);
            node.ContentType_Used = true;

            // structure
            if (_sn_parent != null)
            {
                // the parent has to be a component
                if (!_sn_parent.ContentType_Used || _sn_parent.ContentType == null) return null;
                if (_sn_parent.ContentType != typeof(Component.Component)) return null;
                // no self-parenting
                if (_sn_parent.IDAsLong == _node_source.ID) return null;

                node.ParentNode = _sn_parent;
            }

            foreach(var entry in _node_source.ContainedParameters)
            {
                Parameter.Parameter p = entry.Value;
                if (p == null) continue;

                StructureNode p_sn = StructureNode.CreateFrom(p, node);
                if (p_sn != null)
                    node.children_nodes.Add(p_sn);
            }

            foreach(GeometricRelationship gr in _node_source.R2GInstances)
            {
                StructureNode gr_sn = StructureNode.CreateFrom(gr, node);
                if (gr_sn != null)
                    node.children_nodes.Add(gr_sn);
            }

            // recursion
            foreach(var entry in _node_source.ContainedComponents)
            {
                Component.Component sC = entry.Value;
                if (sC == null) continue;

                StructureNode sC_sn = StructureNode.CreateFrom(sC, node);
                if (sC_sn != null)
                    node.children_nodes.Add(sC_sn);
            }

            return node;
        }

        protected static StructureNode CreateFrom(Parameter.Parameter _node_source, StructureNode _sn_parent)
        {
            if (_node_source == null) return null;

            // a parameter node cannot be w/o parent component
            if (_sn_parent == null) return null; 
            if (!_sn_parent.ContentType_Used || _sn_parent.ContentType == null) return null;
            if (_sn_parent.ContentType != typeof(Component.Component)) return null;

            // create the node
            StructureNode node = new StructureNode();

            // content
            node.IDAsLong = _node_source.ID;
            node.IDAsLong_Used = true;
            node.IDAsString = _node_source.Name;
            node.IDAsString_Used = true;
            node.ContentType = typeof(Parameter.Parameter);
            node.ContentType_Used = true;

            // structure
            node.ParentNode = _sn_parent;

            return node;
        }

        protected static StructureNode CreateFrom(GeometricRelationship _node_source, StructureNode _sn_parent)
        {
            if (_node_source == null) return null;

            List<HierarchicalContainer> g_content = _node_source.GeometricContent;
            if (g_content.Count == 0) return null;

            // a geometric relationship node cannot exist w/o parent component
            if (_sn_parent == null) return null;
            if (!_sn_parent.ContentType_Used || _sn_parent.ContentType == null) return null;
            if (_sn_parent.ContentType != typeof(Component.Component)) return null;

            // create the node
            StructureNode node = new StructureNode();

            // content
            node.IDAsLong = _node_source.ID;
            node.IDAsLong_Used = true;
            node.ContentType = typeof(GeometricRelationship);
            node.ContentType_Used = true;

            // structure
            node.ParentNode = _sn_parent;

            foreach(Point3DContainer p3dc in g_content)
            {
                StructureNode p3dc_sn = StructureNode.CreateFrom(p3dc, node);
                if (p3dc_sn != null)
                    node.children_nodes.Add(p3dc_sn);
            }

            return node;
        }

        protected static StructureNode CreateFrom(Point3DContainer _node_source, StructureNode _sn_parent)
        {
            if (_node_source == null) return null;

            // a point 3d container cannot exist w/o a parent geometric relationship
            if (_sn_parent == null) return null;
            if (!_sn_parent.ContentType_Used || _sn_parent.ContentType == null) return null;
            if (_sn_parent.ContentType != typeof(GeometricRelationship)) return null;
            if (_sn_parent.IDAsLong != _node_source.ID_primary) return null;

            // create the node
            StructureNode node = new StructureNode();

            // content
            node.IDAsLong = _node_source.ID_primary;
            node.IDAsLong_Used = true;
            node.IDAsInt_1 = _node_source.ID_secondary;
            node.IDAsInt_1_Used = true;
            node.ContentType = typeof(Point3DContainer);
            node.ContentType_Used = true;

            // structure
            node.ParentNode = _sn_parent;

            return node;
        }

        #endregion

        #region PROPERTIES: Structure

        protected List<StructureNode> children_nodes;
        public ReadOnlyCollection<StructureNode> ChildrenNodes { get { return this.children_nodes.AsReadOnly(); } }
        public StructureNode ParentNode { get; protected set; }

        // links
        public StructureNode LinkTargetNode { get; set; }

        // processing
        protected bool marked_upward_propagating;
        public bool Marked 
        {
            get { return this.marked_upward_propagating; }
            set
            {
                this.marked_upward_propagating = value;
                if (this.ParentNode != null)
                    this.ParentNode.Marked = this.marked_upward_propagating;
            }
        }

        #endregion

        #region PROPERTIES: Content Id

        public long IDAsLong { get; protected set; }
        public bool IDAsLong_Used { get; protected set; }

        public int IDAsInt_1 { get; protected set; }
        public bool IDAsInt_1_Used { get; protected set; }

        public int IDAsInt_2 { get; protected set; }
        public bool IDAsInt_2_Used { get; protected set; }

        public string IDAsString { get; protected set; }
        public bool IDAsString_Used { get; protected set; }

        public Type ContentType { get; protected set; }
        public bool ContentType_Used { get; protected set; }

        #endregion

        #region .CTOR
        protected StructureNode()
        {
            // structure
            this.children_nodes = new List<StructureNode>();
            this.ParentNode = null;
            this.LinkTargetNode = null;
            this.marked_upward_propagating = false;

            // content
            this.IDAsLong = -1L;
            this.IDAsLong_Used = false;

            this.IDAsInt_1 = -1;
            this.IDAsInt_1_Used = false;

            this.IDAsInt_2 = -1;
            this.IDAsInt_2_Used = false;

            this.IDAsString = string.Empty;
            this.IDAsString_Used = false;

            this.ContentType = null;
            this.ContentType_Used = false;
        }
        #endregion

        #region METHODS: Search, Prune

        /// <summary>
        /// If any of the numeric values is set to '-1' or the string value to null or empty or the Typeto null, they are ignored.
        /// </summary>
        public StructureNode FindMatchFor(long _id_as_long, int _id_as_int_1, int _id_as_int_2, string _id_as_string, Type _content_type)
        {
            if (this.IsMatchFor(_id_as_long, _id_as_int_1, _id_as_int_2, _id_as_string, _content_type))
                return this;

            StructureNode found = null;
            foreach(StructureNode sN in this.children_nodes)
            {
                found = sN.FindMatchFor(_id_as_long, _id_as_int_1, _id_as_int_2, _id_as_string, _content_type);
                if (found != null)
                    return found;
            }

            return found;
        }

        private bool IsMatchFor(long _id_as_long, int _id_as_int_1, int _id_as_int_2, string _id_as_string, Type _content_type)
        {
            bool equal = true;

            equal &= ((_id_as_long != -1) ? (this.IDAsLong == _id_as_long) : true);
            equal &= ((_id_as_int_1 != -1) ? (this.IDAsInt_1 == _id_as_int_1) : true);
            equal &= ((_id_as_int_2 != -1) ? (this.IDAsInt_2 == _id_as_int_2) : true);
            equal &= ((!string.IsNullOrEmpty(_id_as_string)) ? (this.IDAsString == _id_as_string) : true);
            equal &= ((_content_type != null) ? (this.ContentType == _content_type) : true);

            return equal;
        }

        public void PruneUnmarked()
        {
            List<StructureNode> children_nodes_new = new List<StructureNode>();
            foreach(StructureNode sN in this.children_nodes)
            {
                if (sN.Marked)
                {
                    sN.PruneUnmarked();
                    children_nodes_new.Add(sN);
                }                   
            }
            this.children_nodes = children_nodes_new;
        }

        #endregion

        #region METHODS: Search Specific

        public Component.Component FindComponentMatchIn(List<Component.Component> _comps)
        {
            if (_comps == null || _comps.Count == 0) return null;

            List<Component.Component> flat_list = Component.Component.GetFlattenedListOf(_comps);
            foreach(Component.Component c in flat_list)
            {
                StructureNode match = this.FindMatchFor(c.ID, -1, -1, string.Empty, typeof(ParameterStructure.Component.Component));
                if (match != null)
                    return c;
            }

            return null;
        }

        public Parameter.Parameter FindParameterMatchIn(List<Component.Component> _comps, out Component.Component _comp_parent)
        {
            _comp_parent = null;
            if (_comps == null || _comps.Count == 0) return null;

            List<Component.Component> flat_list = Component.Component.GetFlattenedListOf(_comps);
            foreach (Component.Component c in flat_list)
            {
                foreach(var entry in c.ContainedParameters)
                {
                    Parameter.Parameter p = entry.Value;
                    if (p == null) continue;

                    StructureNode match = this.FindMatchFor(p.ID, -1, -1, p.Name, typeof(ParameterStructure.Parameter.Parameter));
                    if (match != null)
                    {
                        _comp_parent = c;
                        return p;
                    }
                }              
            }

            return null;
        }

        public GeometricRelationship FindGeomRelationshipMatchIn(List<Component.Component> _comps, out Component.Component _comp_parent)
        {
            _comp_parent = null;
            if (_comps == null || _comps.Count == 0) return null;

            List<Component.Component> flat_list = Component.Component.GetFlattenedListOf(_comps);
            foreach (Component.Component c in flat_list)
            {
                foreach (GeometricRelationship gr in c.R2GInstances)
                {
                    StructureNode match = this.FindMatchFor(gr.ID, -1, -1, string.Empty, typeof(ParameterStructure.Geometry.GeometricRelationship));
                    if (match != null)
                    {
                        _comp_parent = c;
                        return gr;
                    }
                }
            }

            return null;
        }

        public Point3DContainer FindSinglePointMatchIn(List<Component.Component> _comps, out Component.Component _comp_parent)
        {
            _comp_parent = null;
            if (_comps == null || _comps.Count == 0) return null;

            List<Component.Component> flat_list = Component.Component.GetFlattenedListOf(_comps);
            foreach (Component.Component c in flat_list)
            {
                foreach(GeometricRelationship gr in c.R2GInstances)
                {
                    List<HierarchicalContainer> geometry = gr.GeometricContent;
                    if (geometry == null || geometry.Count == 0) continue;

                    foreach(HierarchicalContainer hc in geometry)
                    {
                        Point3DContainer p3dc = hc as Point3DContainer;
                        if (p3dc == null) continue;

                        StructureNode match = this.FindMatchFor(p3dc.ID_primary, p3dc.ID_secondary, -1, string.Empty, typeof(ParameterStructure.Geometry.Point3DContainer));
                        if (match != null)
                        {
                            _comp_parent = c;
                            return p3dc;
                        }
                    }                   
                }
            }

            return null;
        }

        #endregion

        #region UTILS

        protected int GetNrAncestors()
        {
            if (this.ParentNode == null) return 0;

            return this.ParentNode.GetNrAncestors() + 1;
        }

        protected string GetIndent()
        {
            int nr_ancestors = this.GetNrAncestors();
            if (nr_ancestors == 0) return string.Empty;

            string indent = string.Empty;
            for(int i = 0; i < nr_ancestors; i++)
            {
                indent += "\t";
            }

            return indent;
        }

        #endregion

        #region OVERRIDES: Equality

        public override bool Equals(object obj)
        {
            StructureNode sn = obj as StructureNode;
            if (sn == null)
                return false;
            else
                return this.ContentMatch(sn);
        }

        public override int GetHashCode()
        {
            int hash_code = this.IDAsLong.GetHashCode() ^ this.IDAsInt_1.GetHashCode() ^ this.IDAsInt_2.GetHashCode();
            
            if (this.IDAsString_Used) 
                hash_code ^= this.IDAsString.GetHashCode();

            if (this.ContentType_Used && this.ContentType != null) 
                hash_code ^= this.ContentType.GetHashCode();

            return hash_code;
        }

        protected bool ContentMatch(StructureNode _sn)
        {
            if (this.IDAsString_Used != _sn.IDAsString_Used) return false;
            if (this.IDAsInt_1_Used != _sn.IDAsInt_1_Used) return false;
            if (this.IDAsInt_2_Used != _sn.IDAsInt_2_Used) return false;
            if (this.IDAsString_Used != _sn.IDAsString_Used) return false;
            if (this.ContentType_Used != _sn.ContentType_Used) return false;

            bool equal = true;

            equal &= ((this.IDAsLong_Used) ? (this.IDAsLong == _sn.IDAsLong) : true);
            equal &= ((this.IDAsInt_1_Used) ? (this.IDAsInt_1 == _sn.IDAsInt_1) : true);
            equal &= ((this.IDAsInt_2_Used) ? (this.IDAsInt_2 == _sn.IDAsInt_2) : true);
            equal &= ((this.IDAsString_Used) ? (this.IDAsString == _sn.IDAsString) : true);
            equal &= ((this.ContentType_Used) ? (this.ContentType == _sn.ContentType) : true);
            
            return equal;
        }

        #endregion

        #region OVERRIDES: ToString

        public override string ToString()
        {
            string representation = this.GetIndent();
            representation += (this.Marked) ? "oSN[" : "SN[";

            representation += (this.IDAsLong_Used) ? " IDL:" + this.IDAsLong.ToString() : " IDL:-";
            representation += (this.IDAsInt_1_Used) ? " IDI1:" + this.IDAsInt_1.ToString() : " IDI1:-";
            representation += (this.IDAsInt_2_Used) ? " IDI2:" + this.IDAsInt_2.ToString() : " IDI2:-";
            representation += (this.IDAsString_Used && this.IDAsString!= null) ? " IDS:" + this.IDAsString : " IDS:-";
            representation += (this.ContentType_Used && this.ContentType != null) ? " CT:" + this.ContentType.ToString() : " CT:-";
            representation += " ]";

            representation += "(" + this.children_nodes.Count + ")";

            if (this.LinkTargetNode != null)
                representation += "->" + this.LinkTargetNode.ToString();

            if (this.children_nodes.Count > 0)
            {
                representation += "\n";
                foreach(StructureNode child in this.children_nodes)
                {
                    representation += child.ToString() + "\n";
                }
            }

            return representation;
        }

        public string ToSimpleString()
        {
            string representation = "SN[";

            representation += (this.IDAsLong_Used) ? " IDL:" + this.IDAsLong.ToString() : "";
            representation += (this.IDAsInt_1_Used) ? " IDI1:" + this.IDAsInt_1.ToString() : "";
            representation += (this.IDAsInt_2_Used) ? " IDI2:" + this.IDAsInt_2.ToString() : "";
            representation += (this.IDAsString_Used && this.IDAsString != null) ? " IDS:" + this.IDAsString : "";

            string cont_type_string = (this.ContentType_Used && this.ContentType != null) ? this.ContentType.ToString() : "";
            if (!string.IsNullOrEmpty(cont_type_string))
            {
                string[] type_comps = cont_type_string.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (type_comps != null && type_comps.Length > 0)
                {
                    representation += "\nCT:" + type_comps[type_comps.Length - 1];
                }
            }
            
            representation += "]";

            return representation;
        }

        public string ToInfoString()
        {
            string representation = "";

            representation += (this.IDAsLong_Used) ? " IDL:" + this.IDAsLong.ToString() : "";
            representation += (this.IDAsInt_1_Used) ? " IDI1:" + this.IDAsInt_1.ToString() : "";
            representation += (this.IDAsInt_2_Used) ? " IDI2:" + this.IDAsInt_2.ToString() : "";
            representation += (this.IDAsString_Used && this.IDAsString != null) ? " IDS:" + this.IDAsString : "";

            string cont_type_string = (this.ContentType_Used && this.ContentType != null) ? this.ContentType.ToString() : "";
            if (!string.IsNullOrEmpty(cont_type_string))
            {
                string[] type_comps = cont_type_string.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (type_comps != null && type_comps.Length > 0)
                {
                    if (type_comps.Length > 1)
                        representation += " CT:" + type_comps[type_comps.Length - 2] + "." + type_comps[type_comps.Length - 1];
                    else
                        representation += " CT:" + type_comps[type_comps.Length - 1];
                }
            }

            return representation;
        }

        #endregion
    }

    public class StructureMap
    {
        public List<StructureNode> SourceForest { get; private set; }
        public StructureNode TargetTree { get; private set; }

        public string Key { get; private set; }

        // derived
        public Utils.SelectableString Representation { get; private set; }

        public StructureMap(string _key, List<StructureNode> _source_forest, StructureNode _taget_tree)
        {
            this.Key = _key;
            this.SourceForest = _source_forest;
            this.TargetTree = _taget_tree;
            this.Representation = new Utils.SelectableString(this.ToString(), false);
        }

        public override string ToString()
        {
            string output = "";
            for (int i = 0; i < this.SourceForest.Count; i++ )
            {
                output += this.SourceForest[i].ToInfoString();
                if (i < this.SourceForest.Count - 1)
                    output += ", ";
            }
            output += "->" + this.TargetTree.ToInfoString();

            return output;
        }
    }
}
