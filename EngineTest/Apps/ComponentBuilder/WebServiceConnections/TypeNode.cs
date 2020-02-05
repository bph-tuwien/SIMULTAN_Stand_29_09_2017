using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using System.ComponentModel;

using ParameterStructure.Mapping;

namespace ComponentBuilder.WebServiceConnections
{
    public enum TypeNodeContentBindingType
    {
        NOT_BINDABLE = 0,
        SIMPLE = 1,
        COMPLEX = 2,
        KEY = 3 // for identifying objects
    }

    public class TypeNode : INotifyPropertyChanged
    {
        #region STATIC

        private static List<Type> LEAF_TYPES;
        public static Dictionary<Type, string> TYPE_ALIAS_NONDEVELOPER;

        public const string KEY_SIGNIFIER = "id";

        static TypeNode()
        {
            LEAF_TYPES = new List<Type> { typeof(int), typeof(double), typeof(float), typeof(long), typeof(string), typeof(char), typeof(byte), typeof(bool), typeof(List<>) };
            
            TYPE_ALIAS_NONDEVELOPER = new Dictionary<Type, string>();
            TYPE_ALIAS_NONDEVELOPER.Add(typeof(int), "ganze Zahl");
            TYPE_ALIAS_NONDEVELOPER.Add(typeof(double), "Fließkommazahl");
            TYPE_ALIAS_NONDEVELOPER.Add(typeof(float), "Fließkommazahl");
            TYPE_ALIAS_NONDEVELOPER.Add(typeof(long), "ganze Zahl");
            TYPE_ALIAS_NONDEVELOPER.Add(typeof(string), "Text");
            TYPE_ALIAS_NONDEVELOPER.Add(typeof(char), "alphanumerisches Zeichen");
            TYPE_ALIAS_NONDEVELOPER.Add(typeof(byte), "Byte");
            TYPE_ALIAS_NONDEVELOPER.Add(typeof(bool), "Binary (1 or 0)");
        }

        public static TypeNode CreateFor(string _label, Type _type, bool _is_optional, string _execution_method_name = "")
        {
            if (string.IsNullOrEmpty(_label)) return null;
            if (_type == null) return null;
            return new TypeNode(_label, _type, null, _is_optional, _execution_method_name);
        }

        private static TypeNodeContentBindingType GetBindingType(Type _t)
        {
            if (_t == null) return TypeNodeContentBindingType.NOT_BINDABLE;
            if (_t == typeof(int) || _t == typeof(double) || _t == typeof(float) || _t == typeof(long) ||
                _t == typeof(string) || _t == typeof(char) || _t == typeof(byte) || _t == typeof(bool))
                return TypeNodeContentBindingType.SIMPLE;
            else if (_t.IsGenericType)
                return TypeNodeContentBindingType.NOT_BINDABLE;
            else
                return TypeNodeContentBindingType.COMPLEX;
        }

        public static TypeNodeContentBindingType GetBindingTypeFromString(string _t_as_str)
        {
            if (string.IsNullOrEmpty(_t_as_str)) return TypeNodeContentBindingType.NOT_BINDABLE;
            switch(_t_as_str)
            {
                case "SIMPLE":
                    return TypeNodeContentBindingType.SIMPLE;
                case "COMPLEX":
                    return TypeNodeContentBindingType.COMPLEX;
                case "KEY":
                    return TypeNodeContentBindingType.KEY;
                default:
                    return TypeNodeContentBindingType.NOT_BINDABLE;
            }
        }

        #endregion

        #region PROPERTIES: INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RegisterPropertyChanged(string _propName)
        {
            if (_propName == null)
                return;

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(_propName));
        }

        #endregion

        #region PROPERTIES: Main

        public string Label { get; private set; }
        public Type ContainedType { get; private set; }
        public bool IsOptional { get; private set; }

        private List<TypeNode> ancestors;

        private TypeNode ancestor_of_same_type; // for recursive type defiitions
        public bool TypeCanBeExpandedBySubtypes 
        {
            get
            {
                return (this.ancestor_of_same_type != null && (this.sub_nodes == null || this.sub_nodes.Count == 0));
            }
        }
        public bool TypeCanBeSimplified 
        { 
            get 
            {
                return (this.ancestor_of_same_type != null && (this.sub_nodes != null && this.sub_nodes.Count > 0));
            } 
        }

        #endregion

        #region PROPERTIES: derived from Type

        private List<TypeNode> sub_nodes;
        public ReadOnlyCollection<TypeNode> SubNodes { get { return this.sub_nodes.AsReadOnly(); } }
        public string TypeName { get; private set; }
        public TypeNodeContentBindingType BindingType 
        { 
            get
            {
                if (this.Label == TypeNode.KEY_SIGNIFIER)
                    return TypeNodeContentBindingType.KEY;
                else
                    return TypeNode.GetBindingType(this.ContainedType); 
            }
        }

        public bool IsEnumerable { get; private set; }
        public bool CanHaveMultipleMappings { get; private set; }

        #endregion

        #region PROPERTIES: derived from derived - terms for non-developers

        public string TypeDescription
        {
            get
            {
                if (TypeNode.TYPE_ALIAS_NONDEVELOPER.ContainsKey(this.ContainedType))
                {
                    string type_descr = this.TypeName.Replace(this.ContainedType.Name, TypeNode.TYPE_ALIAS_NONDEVELOPER[this.ContainedType]);
                    return type_descr;
                }
                return this.TypeName;
            }
        }

        #endregion

        #region PROPERTIES: Instantiation based on the mapping

        public object[] InstantiationInput { get; set; }

        private List<object> all_instances;
        public ReadOnlyCollection<object> AllInstances { get { return this.all_instances.AsReadOnly(); } }

        // derived
        public string InstanceString { get; private set; }

        #endregion

        #region PROPERTIES: mapping

        // derived
        private List<MappingObject> all_mappings;
        public ReadOnlyCollection<MappingObject> AllMappings { get { return (this.all_mappings == null) ? null : this.all_mappings.AsReadOnly(); } }

        // MAIN
        private MappingObject most_recent_mapping;
        public MappingObject MostRecentMapping 
        {
            get { return this.most_recent_mapping; }
            internal set
            {
                this.most_recent_mapping = value;

                if (this.most_recent_mapping != null)
                {
                    // add to mapping collection
                    if (this.all_mappings == null)
                        this.all_mappings = new List<MappingObject>();
                    this.all_mappings.Add(this.most_recent_mapping);

                    // check status
                    this.UpdateMappingStatus();
                }
            }
        }

        // MAIN RECORD
        private Dictionary<string, StructureMap> mapping_record;
        public ReadOnlyDictionary<string, StructureMap> MappingRecord { get { return new ReadOnlyDictionary<string, StructureMap>(this.mapping_record); } }

        // derived
        private bool mapping_complete;
        public bool MappingComplete 
        {
            get { return this.mapping_complete; }
            private set
            {
                this.mapping_complete = value;
                if (this.ancestors != null && this.ancestors.Count > 0)
                {
                    this.ancestors[0].UpdateMappingStatus();
                }
                this.RegisterPropertyChanged("MappingComplete");
                this.MappingIsDirect = (this.all_mappings != null && this.all_mappings.Count > 0 && this.mapping_complete);
            }
        }

        // derived
        private bool mapping_is_direct;
        public bool MappingIsDirect 
        {
            get { return this.mapping_is_direct; }
            private set
            {
                this.mapping_is_direct = value;
                this.RegisterPropertyChanged("MappingIsDirect");
            }
        }

        private void UpdateMappingStatus()
        {
            if (this.all_mappings == null || this.all_mappings.Count == 0)
            {
                if (this.SubNodes == null || this.SubNodes.Count == 0)
                    this.MappingComplete = false;
                else
                {
                    bool mostc = true;
                    foreach (TypeNode sN in this.SubNodes)
                    {
                        mostc &= sN.MappingComplete || sN.IsOptional;
                    }
                    this.MappingComplete = mostc;
                }
            }
            else
            {
                if (this.SubNodes == null || this.SubNodes.Count == 0)
                    this.MappingComplete = true;
                else
                {
                    bool mostc = true;
                    foreach (TypeNode sN in this.SubNodes)
                    {
                        mostc &= TypeNode.LEAF_TYPES.Contains(sN.ContainedType) || sN.MappingComplete || sN.IsOptional;
                    }
                    this.MappingComplete = mostc;
                }
            }
        }

        public List<StructureMap> GetMappingRecordsOfTree()
        {
            List<StructureMap> records = new List<StructureMap>();
            if (this.mapping_record != null && this.mapping_record.Count > 0)
                records.AddRange(this.mapping_record.Values);

            if (this.sub_nodes != null && this.sub_nodes.Count > 0)
            {
                foreach(TypeNode tn in this.sub_nodes)
                {
                    records.AddRange(tn.GetMappingRecordsOfTree());
                }
            }

            return records;
        }

        #endregion

        #region PROPERTIES: methods

        /// <summary>
        /// Expected at the root of a type node tree.
        /// </summary>
        public TypeNode ReturnTypeNode { get; private set; }

        public MethodInfo MainServiceMethod { get; private set; }

        #endregion

        #region PROPERTIES: External instance placement


        private List<object> instances_external;
        /// <summary>
        /// Can only be set if the 'AllInstances' collection (from internal initialization) is empty. Changes 'InstanceString'.
        /// </summary>
        public List<object> InstancesExternal
        {
            get { return this.instances_external; }
            set 
            {
                if (this.all_instances == null || this.all_instances.Count == 0)
                {
                    this.instances_external = value;
                    this.SetInstanceStringFromInstancesExternal();
                }
            }
        }

        private void SetInstanceStringFromInstancesExternal()
        {
            if (this.instances_external != null && this.instances_external.Count > 0)
            {
                string representation = string.Empty;
                foreach(object o in this.instances_external)
                {
                    representation += TypeNode.ToShortString(o) + " ";
                }
                this.InstanceString = representation;
                this.RegisterPropertyChanged("InstanceString");
            }
        }


        #endregion

        protected TypeNode(string _label, Type _t, TypeNode _direct_ancestor, bool _is_optional = false, string _execution_method_name = "")
        {
            this.Label = _label;
            this.ContainedType = _t;
            this.IsOptional = _is_optional;

            if (_direct_ancestor != null)
            {
                this.ancestors = new List<TypeNode> { _direct_ancestor };
                if (_direct_ancestor.ancestors != null)
                    this.ancestors.AddRange(_direct_ancestor.ancestors);
            }

            this.MappingComplete = false;
            this.MappingIsDirect = false;

            this.DeriveInfo();

            // get the main calling method, if the node is a root
            if (this.ancestors == null || this.ancestors.Count == 0)
                this.DeriveReturnTreeForMethodContaining(_execution_method_name);
        }

        #region METHODS: derive information form Type

        private int FindAncestorsOfSameType()
        {
            if (this.BindingType == TypeNodeContentBindingType.SIMPLE || this.BindingType == TypeNodeContentBindingType.KEY) return 0;
            if (this.ancestors == null || this.ancestors.Count == 0) return 0;

            List<TypeNode> potential_related_ancestors = this.ancestors.Where(x => x.Label == this.Label && x.ContainedType == this.ContainedType && x.IsOptional == this.IsOptional).ToList();
            if (potential_related_ancestors == null || potential_related_ancestors.Count == 0) return 0;

            int nr_ancestors_of_same_type = 0;
            foreach(TypeNode pot_rel_ancestor in potential_related_ancestors)
            {
                if (pot_rel_ancestor.IsEnumerable)
                {
                    if ((pot_rel_ancestor.SubNodes == null && this.SubNodes == null) ||
                        (pot_rel_ancestor.SubNodes.Count == 0 && this.SubNodes.Count == 0))
                    {
                        if (this.ancestor_of_same_type == null)
                            this.ancestor_of_same_type = pot_rel_ancestor;
                        nr_ancestors_of_same_type++;
                    }
                    else
                    {
                        bool matching_subtypes = true;
                        foreach (TypeNode sN in pot_rel_ancestor.SubNodes)
                        {
                            TypeNode corresponding = this.sub_nodes.FirstOrDefault(x => x.Label == sN.Label && x.ContainedType == sN.ContainedType && x.IsOptional == sN.IsOptional);
                            if (corresponding == null)
                            {
                                matching_subtypes = false;
                                break;
                            }
                        }
                        if (matching_subtypes)
                        {
                            if (this.ancestor_of_same_type == null)
                                this.ancestor_of_same_type = pot_rel_ancestor;
                            nr_ancestors_of_same_type++;
                        }
                    }
                }
                else
                {
                    if (this.ancestor_of_same_type == null)
                        this.ancestor_of_same_type = pot_rel_ancestor;
                    nr_ancestors_of_same_type++;
                }
            }

            return nr_ancestors_of_same_type;
        }

        /// <summary>
        /// Derive info even for recursive types. If an ancestor of the type was already handled, this methos can be called with '_full' = FALSE
        /// to prevent a stack overflow problem.
        /// </summary>
        private void DeriveInfo()
        {
            this.TypeName = this.ContainedType.Name;
            this.sub_nodes = new List<TypeNode>();

            // establish relationship to ancestors of the same type
            int nr_ancestors_of_same_type = this.FindAncestorsOfSameType();
            if (nr_ancestors_of_same_type > 0) return;

            // check if the type is a collection
            this.IsEnumerable = typeof(System.Collections.IEnumerable).IsAssignableFrom(this.ContainedType) && this.ContainedType != typeof(string);

            // handle generics (e.g. List<T>)
            if (this.ContainedType.IsGenericType)
            {
                Type[] generic_args = this.ContainedType.GenericTypeArguments;
                if (generic_args != null && generic_args.Length > 0)
                {
                    int index_of_generic_sep = this.TypeName.IndexOf('`');
                    if (index_of_generic_sep > -1)
                        this.TypeName = this.TypeName.Substring(0, index_of_generic_sep);
                    this.TypeName += " of";
                    
                    for (int i = 0; i < generic_args.Length; i++ )
                    {
                        this.TypeName += " " + generic_args[i].Name + "s";
                        int nr_same_ancestors = (this.ancestors == null) ? 0 : this.ancestors.Count(x => x.Label == i.ToString() && x.ContainedType == generic_args[i] && x.IsOptional == false);
                        if (nr_same_ancestors < 2)
                        {
                            TypeNode created_sub = new TypeNode((i + 1).ToString(), generic_args[i], this, false);
                            created_sub.CanHaveMultipleMappings = this.IsEnumerable;
                            this.sub_nodes.Add(created_sub);
                        }
                    }                
                }                
            }

            // handle leaf types
            if (TypeNode.LEAF_TYPES.Contains(this.ContainedType))
                return;
            if (this.ContainedType.IsGenericType)
                if (TypeNode.LEAF_TYPES.Contains(this.ContainedType.GetGenericTypeDefinition()))
                    return;

            // find the .ctor with most parameters
            List<ParameterInfo> longest_ctor_param_list = new List<ParameterInfo>();
            
            ConstructorInfo[] all_ctors = this.ContainedType.GetConstructors();
            if (all_ctors != null && all_ctors.Length > 0)
            {
                foreach(ConstructorInfo ci in all_ctors)
                {
                    ParameterInfo[] ci_params = ci.GetParameters();
                    if (ci_params != null && ci_params.Length > longest_ctor_param_list.Count)
                    {
                        longest_ctor_param_list = ci_params.ToList();
                    }
                }
            }

            if (longest_ctor_param_list.Count > 0)
            {
                foreach(ParameterInfo pi in longest_ctor_param_list)
                {
                    int nr_same_ancestors = (this.ancestors == null) ? 0 : this.ancestors.Count(x => x.Label == pi.Name && x.ContainedType == pi.ParameterType && x.IsOptional == pi.IsOptional);
                    if (nr_same_ancestors < 2)
                        this.sub_nodes.Add(new TypeNode(pi.Name, pi.ParameterType, this, pi.IsOptional));                   
                }
            }
        }


        private TypeNode DeriveReturnTreeForMethodContaining(string _partial_method_name)
        {
            if (string.IsNullOrEmpty(_partial_method_name)) return null;

            MethodInfo mi = WebServiceReflector.GetMethodByName(this.ContainedType, _partial_method_name);
            if (mi != null)
            {
                if (mi.ReturnType != typeof(void))
                {
                    this.ReturnTypeNode = TypeNode.CreateFor("EXIT", mi.ReturnType, false);
                    this.MainServiceMethod = mi;
                }
            }

            return null;
        }

        #endregion

        #region METHODS: expand a type using an ancestor of the same type

        public void ExpandType(bool _expand)
        {
            if (!_expand) return;
            if (this.ancestor_of_same_type == null) return;
            if (this.ancestor_of_same_type.SubNodes == null || this.ancestor_of_same_type.SubNodes.Count == 0) return;

            Dictionary<TypeNode, bool> new_nodes = new Dictionary<TypeNode, bool>();
            foreach(TypeNode sN in this.ancestor_of_same_type.SubNodes)
            {
                TypeNode new_n = new TypeNode(sN.Label, sN.ContainedType, this, sN.IsOptional);
                this.sub_nodes.Add(new_n);
                if (sN.Label == this.Label && sN.ContainedType == this.ContainedType && sN.IsOptional == this.IsOptional)
                {                    
                    new_nodes.Add(new_n, false); // do not expand any more                    
                }
                else
                {
                    new_nodes.Add(new_n, true); // expand more
                }
            }

            // recursion
            foreach(var entry in new_nodes)
            {
                entry.Key.ExpandType(entry.Value);
            }
        }

        public void CollapseType()
        {
            if (this.ancestor_of_same_type == null) return;

            this.sub_nodes = new List<TypeNode>();
        }

        #endregion

        #region METHODS: create instance of type

        public object CreateInstance()
        {
            // debug
            if (this.ContainedType == typeof(WebServiceConnector.ShadowService.ShadowServ))
            {

            }
            // debug
            try
            {
                if (this.BindingType == TypeNodeContentBindingType.SIMPLE && this.ContainedType != typeof(string))
                {
                    object instance = Activator.CreateInstance(this.ContainedType);
                    if (instance != null && this.InstantiationInput != null && this.InstantiationInput.Length > 0)
                    {
                        object input = this.InstantiationInput[0];
                        if (input != null)
                            instance = Convert.ChangeType(input, this.ContainedType);
                    }
                    return instance;
                }
                else if (this.BindingType == TypeNodeContentBindingType.KEY || this.ContainedType == typeof(string))
                {
                    if (this.InstantiationInput != null && this.InstantiationInput.Length > 0)
                    {
                        string input = (string)this.InstantiationInput[0];
                        object instance = Activator.CreateInstance(this.ContainedType, input.ToCharArray());
                        return instance;
                    }
                    else
                        return null;
                }
                else if (this.BindingType == TypeNodeContentBindingType.COMPLEX)
                {
                    object instance = Activator.CreateInstance(this.ContainedType, this.InstantiationInput);
                    return instance;
                }
                else if (this.BindingType == TypeNodeContentBindingType.NOT_BINDABLE)
                {
                    // collection type
                    if (this.InstantiationInput != null && this.InstantiationInput.Length > 0)
                    {
                        object instance = Activator.CreateInstance(this.ContainedType);
                        if (instance != null && (instance is IList))
                        {
                            IList inst_as_col = (instance as IList);
                            foreach(object obj in this.InstantiationInput)
                            {
                                inst_as_col.Add(obj);
                            }
                        }
                        return instance;
                    }
                    else
                        return null;                    
                }
                else
                {
                    return null;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error creating an instace of " + this.TypeName , MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }


        public void InstantiateNodeTree(bool _go_to_subnodes = true)
        {
            if (!this.MappingComplete) return;

            // create objects from the mapping TOP -> BOTTOM
            if (this.all_mappings != null && this.all_mappings.Count > 0)
            {
                // see if we can instantiate at this node already
                this.all_instances = new List<object>();
                foreach(MappingObject mo in this.all_mappings)
                {
                    if ((this.BindingType == TypeNodeContentBindingType.KEY && (mo is MappingString)) ||
                        (this.BindingType == TypeNodeContentBindingType.SIMPLE && (mo is MappingParameter)) ||
                        (this.BindingType == TypeNodeContentBindingType.COMPLEX && (mo is MappingSinglePoint)))
                    {
                        this.AddInstance(mo.InstantiateMapping());
                    }
                }
            }

            // go down to the sub-nodes, if no instantiation took place
            if (this.all_instances == null || this.all_instances.Count == 0)
            {
                if (this.SubNodes != null && this.SubNodes.Count > 0)
                {
                    foreach (TypeNode sN in this.SubNodes)
                    {
                        sN.InstantiateNodeTree();
                    }

                    // see if instantiation is possible from the instantiated sub-nodes
                    List<object> inst_input_from_subnodes = new List<object>();
                    foreach (TypeNode sN in this.SubNodes)
                    {
                        if (sN.all_instances != null && sN.all_instances.Count > 0)
                        {
                            inst_input_from_subnodes.AddRange(sN.all_instances);
                        }
                        else
                        {
                            inst_input_from_subnodes.Add(null);
                        }
                    }

                    // applying the constructor
                    if (inst_input_from_subnodes.Count > 0)
                        this.InstantiationInput = inst_input_from_subnodes.ToArray();

                    this.AddInstance(this.CreateInstance());                     
                }               
            }

        }

        private void AddInstance(object _instance)
        {
            if (_instance == null) return;

            if (this.all_instances == null)
                this.all_instances = new List<object>();

            this.all_instances.Add(_instance);

            string instance_rep = string.Empty;
            foreach(object o in this.all_instances)
            {
                instance_rep += TypeNode.ToShortString(o) + " ";
            }

            this.InstanceString = instance_rep;
            this.RegisterPropertyChanged("InstanceString");
        }

        public List<object> InstantiateNodeTreeFromExternalInput(Dictionary<TypeNode, List<object>> _input)
        {
            if (_input == null || _input.Count == 0) return null;

            // create objects from the mapping TOP -> BOTTOM
            foreach(var entry in _input)
            {
                if (this == entry.Key)
                    return entry.Value;
            }

            // go down to the sub-nodes, if no instantiation took place           
            if (this.SubNodes != null && this.SubNodes.Count > 0)
            {
                List<object> inst_input_from_subnodes = new List<object>();
                foreach (TypeNode sN in this.SubNodes)
                {
                    List<object> sN_instances = sN.InstantiateNodeTreeFromExternalInput(_input);
                    // allow the adding of NULL and Lists as single objects
                    if (sN_instances == null)
                        inst_input_from_subnodes.Add(null);
                    else
                        inst_input_from_subnodes.AddRange(sN_instances);
                }

                // see if instantiation is possible from the instantiated sub-nodes
                if (inst_input_from_subnodes.Count > 0)
                    this.InstantiationInput = inst_input_from_subnodes.ToArray();

                this.AddInstance(this.CreateInstance());
            }            

            return this.all_instances;
        }

        #endregion

        #region METHODS: save a complete mapping for a node tree

        public StructureNode TranslateToStructure(StructureNode _parent)
        {
            // create the root
            // use the 1st int parameter to indicate if the node is optional
            int optional = (this.IsOptional) ? 1 : 0;
            int depth = (this.ancestors == null) ? 0 : this.ancestors.Count;
            StructureNode struct_node = StructureNode.CreateFrom(this.ContainedType, -1L, optional, depth, this.Label, _parent);
            if (struct_node == null) return null;

            if (this.sub_nodes != null && this.sub_nodes.Count > 0)
            {
                foreach (TypeNode sN in this.sub_nodes)
                {
                    sN.TranslateToStructure(struct_node);
                }
            }       
    
            return struct_node;
        }

        private StructureNode TranslateToLinkedStructure(StructureNode _parent, ref List<StructureNode> _incoming_links)
        {
            // use the 1st int parameter to indicate if the node is optional
            // use the 2nd int parameter to indicate level in the tree (to avoid duplicates)
            int optional = (this.IsOptional) ? 1 : 0;
            int depth = (this.ancestors == null) ? 0 : this.ancestors.Count;
            StructureNode struct_node = StructureNode.CreateFrom(this.ContainedType, -1L, optional, depth, this.Label, _parent);
            if (struct_node == null) return null;

            // look for potential links
            if (this.all_mappings != null && this.all_mappings.Count > 0)
            {
                foreach(MappingObject mo in this.all_mappings)
                {
                    MappingParameter mp = mo as MappingParameter;
                    MappingSinglePoint msp = mo as MappingSinglePoint;
                    MappingString ms = mo as MappingString;
                    
                    foreach(StructureNode in_link in _incoming_links)
                    {
                        StructureNode match = null;
                        if (mp != null)
                            match = in_link.FindMatchFor(mp.MappedParameter.ID, -1, -1, mp.MappedParameter.Name, typeof(ParameterStructure.Parameter.Parameter));                           
                        else if (msp != null)
                            match = in_link.FindMatchFor(msp.MappedPointC.ID_primary, msp.MappedPointC.ID_secondary, -1, string.Empty, typeof(ParameterStructure.Geometry.Point3DContainer));
                        else if (ms != null)
                            match = in_link.FindMatchFor(ms.DirectParent.ID, -1, -1, string.Empty, typeof(ParameterStructure.Component.Component));

                        if (match != null)
                        {
                            match.Marked = true;
                            match.LinkTargetNode = struct_node;
                        }
                    }
                }
            }

            if (this.sub_nodes != null && this.sub_nodes.Count > 0)
            {
                foreach(TypeNode tn in this.sub_nodes)
                {
                    tn.TranslateToLinkedStructure(struct_node, ref _incoming_links);
                }
            }
           
            return struct_node;
        }

        public void TranslateMappingsToLinkedTrees(out List<StructureNode> _mapped_input_roots, out StructureNode _type_root)
        {
            _mapped_input_roots = null;
            _type_root = null;

            // gather all parent components
            List<ParameterStructure.Component.Component> mapped_comps = new List<ParameterStructure.Component.Component>();
            this.GetAllMappedComponents(ref mapped_comps);
            if (mapped_comps.Count == 0) return;

            // find the common root(s)
            List<ParameterStructure.Component.Component> input_roots = mapped_comps[0].FindCommonRootsOf(mapped_comps);

            // extract their structure
            _mapped_input_roots = input_roots.Select(x => StructureNode.CreateFrom(x, null)).ToList();

            // extract the type node structure
            // StructureNode own_structure = this.TranslateToStructure(null);

            // extract the type node structure linked to the input sub-forest containing only mapped elements
            _type_root = this.TranslateToLinkedStructure(null, ref _mapped_input_roots);
        }

        private void GetAllMappedComponents(ref List<ParameterStructure.Component.Component> mapped_comps)
        {
            if (mapped_comps == null)
                mapped_comps = new List<ParameterStructure.Component.Component>();

            if (this.all_mappings != null && this.all_mappings.Count > 0)
            {
                foreach(MappingObject mo in this.all_mappings)
                {
                    if (mo.DirectParent != null && mapped_comps.Where(x => x.ID == mo.DirectParent.ID).Count() == 0)
                        mapped_comps.Add(mo.DirectParent);
                }
            }

            if (this.sub_nodes != null && this.sub_nodes.Count > 0)
            {
                foreach (TypeNode sN in this.sub_nodes)
                {
                    sN.GetAllMappedComponents(ref mapped_comps);
                }
            }  
        }

        public void SaveMappingAsRecord(string _key, out List<StructureNode> mapped_input_roots, out StructureNode type_root)
        {

            if (this.mapping_record == null)
                this.mapping_record = new Dictionary<string, StructureMap>();

            string key = _key;
            if (string.IsNullOrEmpty(_key) || this.mapping_record.ContainsKey(_key))
            {
                key = TypeNode.GenerateUniqueKey(this.mapping_record.Keys.ToList(), _key);
            }

            mapped_input_roots = null;
            type_root = null;
            this.TranslateMappingsToLinkedTrees(out mapped_input_roots, out type_root);
            if (mapped_input_roots == null || type_root == null) return;

            foreach (ParameterStructure.Mapping.StructureNode in_link in mapped_input_roots)
            {
                in_link.PruneUnmarked();
            }

            StructureMap map = new StructureMap(key, mapped_input_roots, type_root);
            this.mapping_record.Add(key, map);


        }

        public List<MappingObject> LoadMappingRecord(string _key, List<ParameterStructure.Component.Component> _source)
        {
            List<MappingObject> restored_mappings = new List<MappingObject>();

            if (string.IsNullOrEmpty(_key)) return restored_mappings;
            if (_source == null || _source.Count == 0) return restored_mappings;

            if (this.mapping_record == null || this.mapping_record.Count == 0 || !this.mapping_record.ContainsKey(_key)) return restored_mappings;            

            // get the record
            StructureMap map_to_load = this.mapping_record[_key];

            // reset the mappings along the tree
            this.ResetMappings();
            
            // traverse the source forest from the roots down and set the mappings
            foreach(StructureNode sN in map_to_load.SourceForest)
            {
                restored_mappings.AddRange(this.RestoreMappingFromRecord(sN, _source));
            }

            return restored_mappings;
        }

        private List<MappingObject> RestoreMappingFromRecord(StructureNode _source_node, List<ParameterStructure.Component.Component> _source)
        {
            List<MappingObject> restored_mappings = new List<MappingObject>();
            if (_source_node == null) return restored_mappings;

            // retrieve the value source
            ParameterStructure.Component.Component c_direct_parent = null;
            ParameterStructure.Component.Component c = null;
            ParameterStructure.Parameter.Parameter p = null;
            ParameterStructure.Geometry.GeometricRelationship gr = null;
            ParameterStructure.Geometry.Point3DContainer sp = null;

            if (_source_node.ContentType != null && _source_node.ContentType == typeof(ParameterStructure.Component.Component))
                c = _source_node.FindComponentMatchIn(_source);
            else if (_source_node.ContentType != null && _source_node.ContentType == typeof(ParameterStructure.Parameter.Parameter))
                p = _source_node.FindParameterMatchIn(_source, out c_direct_parent);           
            else if (_source_node.ContentType != null && _source_node.ContentType == typeof(ParameterStructure.Geometry.GeometricRelationship))
                gr = _source_node.FindGeomRelationshipMatchIn(_source, out c_direct_parent);
            else if (_source_node.ContentType != null && _source_node.ContentType == typeof(ParameterStructure.Geometry.Point3DContainer))
                sp = _source_node.FindSinglePointMatchIn(_source, out c_direct_parent);

            if (c == null && p == null && gr == null && sp == null) return restored_mappings;

            if (_source_node.LinkTargetNode != null)
            {
                // retrieve the value target
                TypeNode match = this.FindMatchTo(_source_node.LinkTargetNode);
                if (match == null) return restored_mappings;

                // restore the mapping
                MappingError err = MappingError.NONE;
                if (c != null)
                {
                    // sets it as MostRecentMapping too
                    MappingString ms = MappingString.Create(c, MappingComponent.PREFIX_ID + c.ID.ToString(), match, false, out err);
                    if (ms != null)
                        restored_mappings.Add(ms);
                }
                else if (p != null && c_direct_parent != null)
                {
                    // sets it as MostRecentMapping too
                    MappingParameter mp = MappingParameter.Create(p, c_direct_parent, match, false, false, out err);
                    if (mp != null)
                        restored_mappings.Add(mp);
                }
                else if (sp != null && c_direct_parent != null)
                {
                    // sets it as MostRecentMapping too
                    MappingSinglePoint msp = MappingSinglePoint.Create(sp, c_direct_parent, match, false, out err);
                    if (msp != null)
                        restored_mappings.Add(msp);
                }
                if (err != MappingError.NONE) return restored_mappings;
            }            

            //RECURSION
            foreach(StructureNode sN in _source_node.ChildrenNodes)
            {
                restored_mappings.AddRange(this.RestoreMappingFromRecord(sN, _source));
            }

            return restored_mappings;
        }

        public TypeNode FindMatchTo(StructureNode _node)
        {
            if (_node == null) return null;

            int optional = (this.IsOptional) ? 1 : 0;
            int depth = (this.ancestors == null) ? 0 : this.ancestors.Count;
            if (_node.IDAsInt_1 == optional && _node.IDAsInt_2 == depth && _node.IDAsString == this.Label && _node.ContentType == this.ContainedType)
            {
                // MATCH
                return this;
            }

            if (this.sub_nodes != null && this.sub_nodes.Count > 0)
            {
                foreach(TypeNode tn in this.sub_nodes)
                {
                    TypeNode match = tn.FindMatchTo(_node);
                    if (match != null)
                        return match;
                }
            }

            return null;
        }

        public void ResetMappings()
        {
            // mapping
            this.all_mappings = new List<MappingObject>();
            this.most_recent_mapping = null;
            this.mapping_complete = false;
            this.mapping_is_direct = false;
            this.RegisterPropertyChanged("MappingComplete");
            this.RegisterPropertyChanged("MappingIsDirect");

            // instancing
            this.all_instances = new List<object>();
            this.InstanceString = string.Empty;
            this.RegisterPropertyChanged("InstanceString");

            if (this.sub_nodes != null && this.sub_nodes.Count > 0)
            {
                foreach(TypeNode tn in this.sub_nodes)
                {
                    tn.ResetMappings();
                }
            }
        }

        #endregion

        #region METHODS: Call the main service method

        public object CallMainServiceMethod(object _instance, string _uri, out object[] method_param_array)
        {
            method_param_array = null;
            if (this.MainServiceMethod == null) return null;
            if (string.IsNullOrEmpty(_uri)) return null;

            // assemble the params
            List<object> method_params = new List<object>();
            ParameterInfo[] p_info = this.MainServiceMethod.GetParameters();
            bool used_input_string = false;

            if (p_info != null && p_info.Length > 0)
            {
                foreach(ParameterInfo pi in p_info)
                {
                    if (pi.IsOut)
                        method_params.Add(null);
                    else if (!pi.IsOut && pi.ParameterType == typeof(string) && !used_input_string)
                    {
                        method_params.Add(_uri);
                        used_input_string = true;
                    }
                    else
                        method_params.Add(null);
                }
            }

            method_param_array = method_params.ToArray();
            object result = this.MainServiceMethod.Invoke(_instance, method_param_array);
            return result;
        }

        #endregion

        #region METHODS: External instance placement

        public void PlaceInstanceInTree(object _instance)
        {
            if (_instance == null) return;
            if (_instance.GetType() != this.ContainedType) return;
            if (this.all_instances != null && this.all_instances.Count > 0) return;

            this.InstancesExternal = new List<object> { _instance };

            if (this.sub_nodes != null && this.sub_nodes.Count > 0)
            {
                // look for the contents of the instance
                List<KeyValuePair<string, object>> instance_contents = new List<KeyValuePair<string, object>>();
                PropertyInfo[] prop_info = this.ContainedType.GetProperties();
                if (prop_info != null && prop_info.Length > 0)
                {
                    foreach(PropertyInfo pi in prop_info)
                    {
                        if (!pi.CanRead) continue;

                        object pi_instance = pi.GetValue(_instance);
                        if (pi_instance != null)
                            instance_contents.Add(new KeyValuePair<string, object>(pi.Name, pi_instance));
                    }
                }

                if (instance_contents.Count > 0)
                {
                    foreach (TypeNode tn in this.sub_nodes)
                    {
                        object tn_instance = instance_contents.FirstOrDefault(x => x.Value.GetType() == tn.ContainedType && MappingObject.StringsCouldMeanTheSame(x.Key, tn.Label)).Value;
                        if (tn_instance != null)
                            tn.PlaceInstanceInTree(tn_instance);
                    }
                }               
            }
            
        }

        #endregion

        #region OVERRIDES

        public override string ToString()
        {
            return this.Label + ": " + this.TypeName + " [" + this.sub_nodes.Count + "]";
        }

        #endregion

        #region UTILS

        private static System.IFormatProvider NR_FORMATTER = new System.Globalization.NumberFormatInfo();

        private static string ToShortString(object _o)
        {
            if (_o == null) return string.Empty;

            if (_o is double)
            {
                return ((double)_o).ToString("F2", TypeNode.NR_FORMATTER);
            }
            else if (_o is System.Windows.Media.Media3D.Point3D)
            {
                System.Windows.Media.Media3D.Point3D p3d = (System.Windows.Media.Media3D.Point3D)_o;
                string p3d_rep = "{" + p3d.X.ToString("F2", TypeNode.NR_FORMATTER) + "," + 
                                       p3d.Y.ToString("F2", TypeNode.NR_FORMATTER) + "," +
                                       p3d.Z.ToString("F2", TypeNode.NR_FORMATTER) + "}";
                return p3d_rep;
            }
            else if (_o is IList)
            {
                string rep = "{";
                foreach (object sO in (IList)_o)
                {
                    rep += TypeNode.ToShortString(sO) + ",";
                }
                if (rep.Length > 1)
                    rep = rep.Substring(0, rep.Length - 1);
                rep += "}";
                return rep;
            }
            else
            {
                return _o.ToString();
            }

        }

        private static string GenerateUniqueKey(List<string> _existing_keys, string _input)
        {
            if (_existing_keys == null || _existing_keys.Count == 0)
            {
                if (string.IsNullOrEmpty(_input))
                    return "Key_1";
                else
                    return _input;
            }

            string new_key = string.Empty;
            if (string.IsNullOrEmpty(_input))
                new_key = "Key_1";
            else
                new_key = _input;

            while(_existing_keys.Contains(new_key))
            {
                new_key += "_1";
            }

            return new_key;
        }

        #endregion

    }

    #region VALUE CONVERTERS

    [ValueConversion(typeof(TypeNodeContentBindingType), typeof(Boolean))]
    public class TypeNodeContentBindingTypeToBooleanConverter : IValueConverter
    {
        // in order to react to more than one type mode at once
        // use + as OR
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            TypeNodeContentBindingType type = TypeNodeContentBindingType.NOT_BINDABLE;
            if (value is TypeNodeContentBindingType)
                type = (TypeNodeContentBindingType)value;

            string str_param = parameter.ToString();
            string[] str_params_OR = str_param.Split(new char[] { '+' });

            if (str_params_OR.Count() < 2)
                return (type == TypeNode.GetBindingTypeFromString(str_param));
            else
            {
                foreach (string p in str_params_OR)
                {
                    if (type == TypeNode.GetBindingTypeFromString(p))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }

    #endregion
}
