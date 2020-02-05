using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

using ParameterStructure.Parameter;
using ParameterStructure.Mapping;
using ParameterStructure.DXF;

namespace ParameterStructure.Component
{
    #region ENUMS

    public enum ParameterDeleteResult
    {
        SUCCESS = 0,
        ERR_ARG_NULL = 1,
        ERR_NOT_FOUND = 2,
        ERR_BOUND_IN_CALC = 3,
        ERR_UNKNOWN = 4
    }

    #endregion

    #region HELPER CLASSES

    internal class ComponentReferenceRequest
    {
        public Component Seeker { get; private set; }
        public long ID_Reqested { get; private set; }
        public string Slot_Requested { get; private set; }

        public ComponentReferenceRequest(Component _seeker, long _id_requested, string _slot_requested)
        {
            this.Seeker = _seeker;
            this.ID_Reqested = _id_requested;
            this.Slot_Requested = _slot_requested;
        }
    }


    internal class Mapping2ComponentCalculatorRequest
    {
        public Component Seeker { get; private set; }
        public Mapping2Component Map { get; private set; }
        public long ID_Reqested { get; private set; }

        public Mapping2ComponentCalculatorRequest(Component _seeker, Mapping2Component _map, long _id_requested)
        {
            this.Seeker = _seeker;
            this.Map = _map;
            this.ID_Reqested = _id_requested;
        }
    }

    #endregion

    public class ComponentFactory : INotifyPropertyChanged
    {
        #region STATIC

        public const string COMPONENT_RECORD_FILE_NAME = "ComponentRecord";
        public const string COMPONENT_RECORD_VIS_FILE_NAME = "ComponentRecordVis";
        private static long NR_FACTORIES = 0;

        private static List<ComponentFactory> INSTANCES;
        static ComponentFactory()
        {
            ComponentFactory.INSTANCES = new List<ComponentFactory>();
        }

        private static void UpdateComponentCounter()
        {
            List<long> max_ids = new List<long>();
            foreach(ComponentFactory cf in INSTANCES)
            {
                long max_id = 0;
                if (cf.component_record_flat != null && cf.component_record_flat.Count > 0)
                    max_id = cf.component_record_flat.Select(x => x.ID).Max();

                max_ids.Add(max_id);
            }

            Component.NR_COMPONENTS = max_ids.Max();
        }

        private static void UpdateNetworkCounter()
        {
            List<long> max_ids = new List<long>();
            foreach(ComponentFactory cf in INSTANCES)
            {
                long max_id = 0;
                if (cf.network_record != null && cf.network_record.Count > 0)
                    max_id = cf.network_record.Select(x => x.GetAllElementIds().Max()).Max();

                max_ids.Add(max_id);
            }

            FlNetElement.NR_FL_NET_ELEMENTS = max_ids.Max() + 1;
        }

        public static void DiscardFactory(ComponentFactory _f)
        {
            if (INSTANCES.Contains(_f))
                INSTANCES.Remove(_f);
        }

        #endregion

        #region CLASS MEMBERS

        private long id;

        public ComponentManagerType Caller { get; private set; }

        // components
        private Component component_candidate;
        private List<Component> component_record;
        private List<Component> component_record_flat;
        public List<Component> ComponentRecord { get { return this.component_record; } }

        //private List<string> component_names;

        // flow networks
        private FlowNetwork network_candidate;
        private List<FlowNetwork> network_record;
        private List<FlowNetwork> network_record_flat;

        public List<FlowNetwork> NetworkRecord { get { return this.network_record; } }

        private List<FlNetNode> nw_parsed_nodes; // to be reset after parsing the owner network
        private List<FlNetEdge> nw_parsed_edges; // to be reset after parsing the owner network
        private List<FlowNetwork> nw_parsed_nws; // to be reset after parsing the owner network

        // for component parsing
        private List<ComponentReferenceRequest> component_references_to_restore;
        private List<Mapping2ComponentCalculatorRequest> component_mapping_to_restore;
        public bool LockParsedComponents { get; internal set; }

        #endregion

        public ComponentFactory(ComponentManagerType _caller)
        {
            this.id = (++ComponentFactory.NR_FACTORIES);
            this.Caller = _caller;
            this.ParamFilterString = string.Empty;

            this.component_record = new List<Component>();
            this.component_references_to_restore = new List<ComponentReferenceRequest>();
            this.component_mapping_to_restore = new List<Mapping2ComponentCalculatorRequest>();
            this.marked_id = -1;

            this.network_record = new List<FlowNetwork>();
            this.nw_parsed_nodes = new List<FlNetNode>();
            this.nw_parsed_edges = new List<FlNetEdge>();
            this.nw_parsed_nws = new List<FlowNetwork>();

            ComponentFactory.INSTANCES.Add(this); // added 14.09.2016

            // parameter filtering: added 02.11.2016
            this.p_vis_flags_prev = 11111;
            this.p_vis_flags = 11111;
            this.p_vis_all = true;
            this.PVis = new Dictionary<InfoFlow, bool>
            {
                {InfoFlow.INPUT, true},
                {InfoFlow.OUPUT, true},
                {InfoFlow.MIXED, true},
                {InfoFlow.REF_IN, true},
                {InfoFlow.CALC_IN, true}
            };
        }

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

        #region PROPERTIES: Filtering

        private bool refresh_view = false;
        public bool RefreshView
        {
            get { return this.refresh_view; }
            private set
            { 
                this.refresh_view = value;
                this.RegisterPropertyChanged("RefreshView");
            }
        }

        /// <summary>
        /// For filtering parameters in the ParamChildren of the components in this factory
        /// </summary>
        private string param_filter_string;
        public string ParamFilterString
        {
            get { return this.param_filter_string; }
            set 
            { 
                this.param_filter_string = value;
                this.RegisterPropertyChanged("ParamFilterString");
            }
        }


        #endregion

        #region PROPERTIES: Newly Marked Component

        private long marked_id;
        public long MarkedId
        {
            get { return this.marked_id; }
            private set 
            {
                this.marked_id = value;
                this.RegisterPropertyChanged("MarkedId");
            }
        }

        public bool MarkedTrue { get; private set; }

        #endregion

        #region PROPERTIES: Newly Marked Network

        private long marked_nw_id;
        public long MarkedNwId
        {
            get { return this.marked_nw_id; }
            private set 
            { 
                this.marked_nw_id = value;
                this.RegisterPropertyChanged("MarkedNwId");
            }
        }

        public bool MarkedNwTrue { get; private set; }

        #endregion

        #region PROPERTIES: Visibility of Parameters acc to Propagation

        private int p_vis_flags_prev;
        private int p_vis_flags;
        private bool p_vis_all;
        private Dictionary<InfoFlow, bool> p_vis;
        public Dictionary<InfoFlow, bool> PVis
        {
            get { return this.p_vis; }
            set 
            {
                if (value == null) return;
                if (value.Count < Enum.GetNames(typeof(InfoFlow)).Length) return;
                this.p_vis = value;
                
                this.p_vis_all = this.p_vis.Aggregate(true, (total, x) => total & x.Value);
                this.p_vis_flags_prev = this.p_vis_flags;
                this.p_vis_flags = this.p_vis.Aggregate(0, (total, x) => total*10 + (x.Value ? 1 : 0));
            }
        }

        #endregion

        #region METHODS: Factory Methods Component

        public Component CreateEmptyComponent(bool _manually_generated)
        {
            // create component
            this.component_candidate = new Component();
            this.component_candidate.Factory = this;
            this.component_candidate.PropertyChanged += top_level_comp_PropertyChanged;
            this.component_record.Add(this.component_candidate);
            if (!_manually_generated)
                this.component_candidate.SetupForAutomaticallyCreatedComps(this.Caller);
            else
                this.component_candidate.GiveWritingAccessToCurrentUser(this.Caller); // added 08.09.2017

            // update the internal factory containers
            this.UpdateFlatComponentList();

            // return
            return this.component_candidate;
        }

        internal Component ReconstructComponent(long _id, string _name, string _description, bool _is_automatically_generated,
                                                Category _category, ComponentAccessProfile _local_access, List<string> _fits_in_slots, string _current_slot,
                                                IDictionary<string, Component> _contained_components, IDictionary<string, long> _ref_components,
                                                IList<Parameter.Parameter> _contained_parameters, IList<CalculationPreview> _contained_calculations_preview,
                                                IList<Geometry.GeometricRelationship> _geometry, IList<Mapping.Mapping2Component> _maps,
                                                DateTime _time_stamp, long _symbol_id, bool _add_to_record)
        {
            if (_id < 0 || string.IsNullOrEmpty(_name)) return null;
            if (_fits_in_slots == null || string.IsNullOrEmpty(_current_slot)) return null;
            if (_contained_components == null || _ref_components == null ||
                _contained_parameters == null || _contained_calculations_preview == null) 
                return null;

            Component created = new Component(_id, _name, _description, _is_automatically_generated,
                                              _category, _local_access, _fits_in_slots, _current_slot,
                                              _contained_components, _ref_components, 
                                              _contained_parameters, _contained_calculations_preview,
                                              _geometry, _maps, _time_stamp, _symbol_id);
            created.Factory = this;           

            if (_add_to_record)
            {
                created.PropertyChanged += top_level_comp_PropertyChanged;
                this.component_record.Add(created);
                this.UpdateFlatComponentList();
            }

            // adjust type counter
            Component.NR_COMPONENTS = Math.Max(Component.NR_COMPONENTS, created.ID);

            // try to restore references TO other components
            // IF NOT SUCCESSFUL: record references that need to be restored for later
            foreach(var entry in _ref_components)
            {
                string slot = entry.Key;
                if (string.IsNullOrEmpty(slot)) continue;

                long id = entry.Value;
                if (id < 0)
                {
                    // empty slot that can be added now
                    if (!(created.ReferencedComponents.ContainsKey(entry.Key)))
                        created.AddReferencedComponentSlot(entry.Key);
                }
                else
                {
                    Component ref_candidate = this.component_record_flat.Find(x => x.ID == id);
                    if (ref_candidate != null)
                    {
                        ComponentFactory.AddReferenceComponent(ref_candidate, created, slot);
                    }
                    else
                    {
                        // save a request to be handled later
                        this.component_references_to_restore.Add(new ComponentReferenceRequest(created, id, slot));
                    }
                }
            }

            // try to restore mappings TO other components (added 28.08.2017)
            // IF NOT SUCCESSFUL: record references that need to be restored for later
            foreach(Mapping2Component map in _maps)
            {
                Component map_calc_candidate = this.component_record_flat.FirstOrDefault(x => x.ID == map.ParsingCalculatorID);
                if (map_calc_candidate != null)
                {
                    map.Calculator = map_calc_candidate;
                    map.Calculator.MappedToBy.Add(created);
                }
                else
                {
                    // save a request to be handled later
                    this.component_mapping_to_restore.Add(new Mapping2ComponentCalculatorRequest(created, map, map.ParsingCalculatorID));
                }
            }

            // try to restore references FROM other components
            ComponentReferenceRequest request = this.component_references_to_restore.Find(x => x.ID_Reqested == _id);
            if (request != null)
            {
                ComponentFactory.AddReferenceComponent(created, request.Seeker, request.Slot_Requested);
                this.component_references_to_restore.Remove(request);
            }
            // added 28.08.2017
            Mapping2ComponentCalculatorRequest mapping_request = this.component_mapping_to_restore.FirstOrDefault(x => x.ID_Reqested == _id);
            if (mapping_request != null)
            {
                mapping_request.Map.Calculator = created;
                created.MappedToBy.Add(mapping_request.Seeker);
                this.component_mapping_to_restore.Remove(mapping_request);
            }

            // set viewing properties
            if (created != null)
            {
                // check for reading rights (no rights >> hiding AND locking)
                if (created.HasReadingAccess(this.Caller) || this.Caller == ComponentManagerType.ADMINISTRATOR)
                    created.HideDetails = false;
                else
                    created.HideDetails = true;
                
                // check for writing rights
                // IsLocked is handled ADDITIONALLY by the DXFDistributedDecoder AFTER PARSING
                // (locking may result not from lack of access but because another user is editing the component)
                if (created.HasWritingAccess(this.Caller))
                    created.IsLocked = false;
                else
                    created.IsLocked = true;
            }
            // moved 03.02.2017, modified 27.04.2017
            created.IsLocked |= this.LockParsedComponents;

            // DONE
            return created;
        }

        #endregion

        #region METHODS: Factory Methods Networks

        public FlowNetwork CreateEmptyNetwork(ComponentManagerType _user)
        {
            // create component
            this.network_candidate = new FlowNetwork(new Point(0, 0), null, "- - -", _user);
            this.network_candidate.PropertyChanged += top_level_network_PropertyChanged;
            this.network_record.Add(this.network_candidate);

            // update the internal factory containers
            this.UpdateFlatNetworkList();

            // return
            return this.network_candidate;
        }

        internal FlowNetwork ReconstructNetwork(long _id, string _name, string _description, Component _content, bool _is_valid, Point _position,
                                                ComponentManagerType _manager, DateTime _time_stamp,
                                                IList<FlNetNode> _nodes, IList<FlNetEdge> _edges, IList<FlowNetwork> _subnetworks,
                                                long _node_start_id, long _node_end_id, List<FlowNetworkCalcRule> _calc_rules,
                                                bool _add_to_record)
        {
            // create
            FlowNetwork created = new FlowNetwork(_id, _name, _description, _content, _is_valid, _position, 
                                                  _manager, _time_stamp, _nodes, _edges, _subnetworks, 
                                                  _node_start_id, _node_end_id, _calc_rules);

            // adjusting the type counter takes place in the parsing .ctor above

            created.IsLocked = this.LockParsedComponents || (this.Caller != ComponentManagerType.ADMINISTRATOR && created.Manager != this.Caller);

            // add to record 
            if (_add_to_record)
            {
                created.PropertyChanged += top_level_network_PropertyChanged;
                this.network_record.Add(created);
                this.UpdateFlatNetworkList();
            }
            
            // DONE
            return created;
        }

        #endregion

        #region METHODS: Info Extraction

        // updates the component list and the display state (i.e. marked property)
        public void UpdateFlatComponentList(bool _update_even_if_populated = true, bool _include_invisible = true)
        {
            if (_update_even_if_populated || this.component_record_flat == null)
            {
                this.component_record_flat = DisplayableProductDefinition.FlattenHierarchicalRecord(this.component_record, _include_invisible);

                //// fill in the list of component names
                //var all_comp_names = this.component_record_flat.Select(x => x.TypeName);
                //this.component_names = all_comp_names.Distinct().ToList();
            }
        }

        // returns the top-level ancestor
        public Component GetParentComponent(Component _comp)
        {
            return DisplayableProductDefinition.GetParent(_comp, this.component_record);
        }

        // returns the ancestor chain starting at the highest level and ending with the input component
        public List<Component> GetParentComponentChain(Component _comp)
        {
            return DisplayableProductDefinition.GetParentChain(_comp, this.component_record);
        }

        public void UpdateFlatNetworkList(bool _update_even_if_populated = true )
        {
            if (_update_even_if_populated || this.network_record_flat == null)
            {
                this.network_record_flat = DisplayableProductDefinition.FlattenHierarchicalRecord(this.network_record);
            }
        }

        public FlowNetwork GetParentNetwork(FlowNetwork _nw)
        {
            return DisplayableProductDefinition.GetParent(_nw, this.network_record);
        }

        public List<FlowNetwork> GetParentNetworkChain(FlowNetwork _nw)
        {
            return DisplayableProductDefinition.GetParentChain(_nw, this.network_record);
        }

        public Component GetByID(long _id)
        {
            if (this.component_record_flat == null)
                this.UpdateFlatComponentList();
            return this.component_record_flat.FirstOrDefault(x => x.ID == _id);
        }

        public FlNetElement GetNWByID(long _id)
        {
            foreach(FlowNetwork fnw in this.network_record)
            {
                if (fnw.ID == _id)
                    return fnw;

                FlNetElement found = fnw.GetById(_id);
                if (found != null)
                    return found;
            }
            return null;
        }

        internal List<Component> FindAllReferencesOnAllLevels(Component _comp)
        {
            List<Component> found = new List<Component>();
            if (_comp == null) return found;
            if (_comp.IsMarkable)
                found.Add(_comp);

            List<Component> subComps = _comp.GetFlatSubCompList();
            subComps.Add(_comp);

            foreach (Component c in subComps)
            {
                // modified 12.09.2016 to include references of references (causes stackoverflow when circular references present)
                // List<Component> rrComps = c.GetAllRefCompsOfRefComps();
                // replaced 07.09.2017
                List<Component> rrComps = null;
                c.GetAllRefCompsOfRefComps(ref rrComps);
                foreach (Component rComp in rrComps)
                {
                    Component rComp_parent = this.GetParentComponent(rComp); // top level parent
                    if (rComp_parent != null)
                    {
                        if (!found.Contains(rComp_parent))
                            found.Add(rComp_parent);
                    }
                    else
                    {
                        if (!found.Contains(rComp))
                            found.Add(rComp);
                    }
                }
            }

            return found;
        }

        public Dictionary<long, string> GetComponentInfo(List<long> _ids)
        {
            Dictionary<long, string> info = new Dictionary<long, string>();
            if (_ids == null || _ids.Count == 0) return info;

            foreach(long id in _ids)
            {
                if (id < 0) continue;
                info.Add(id, null);
            }

            int nr_entries = info.Count();
            int nr_found = 0;

            foreach(Component c in this.component_record_flat)
            {
                if (info.ContainsKey(c.ID))
                {
                    info[c.ID] = c.ToInfoString();
                    nr_found++;
                    if (nr_found == nr_entries)
                        break;
                }
            }

            return info;
        }

        public List<FlNetElement> GetFlNwElementsContainingInstancesOf(FlowNetwork _nw, Component _comp)
        {
            List<FlNetElement> containers = new List<FlNetElement>();
            if (_nw == null || _comp == null)
                return containers;

            return _nw.GetAllContainersOf(_comp);
        }


        public List<Component> FindCommonRootsOf(List<Component> _comps)
        {
            if (_comps == null || _comps.Count == 0) return null;

            List<List<Component>> parent_chains = new List<List<Component>>();
            foreach(Component c in _comps)
            {
                List<Component> pc = this.GetParentComponentChain(c);
                if (pc == null || pc.Count == 0) continue;

                parent_chains.Add(pc);
            }

            // the parent chains start with the parent on the highest level and end with the child
            // e.g. the following parent chains
            // A B C D
            // A B C H
            // A B F
            // X Y Z W
            // should result in:
            // B
            // W
            int nr_chains = parent_chains.Count;
            int max_chain_len = parent_chains.Select(x => x.Count).Max();

            // find the top-level roots
            // A
            // X
            List<Component> roots = new List<Component>();
            List<bool> roots_finalized = new List<bool>();
            foreach (List<Component> chain in parent_chains)
            {
                if (roots.Where(x => x.ID == chain[0].ID).Count() == 0)
                {
                    roots.Add(chain[0]);
                    roots_finalized.Add(false);
                }
            }

            // attempt to bring the roots closer to the end of the chains
            for (int i = 0; i < roots.Count; i++ )
            {
                for(int c = 1; c < max_chain_len; c++)
                {
                    List<Component> nodes_after_the_root = new List<Component>();

                    foreach (List<Component> chain in parent_chains)
                    {
                        if (chain[c - 1].ID != roots[i].ID)
                            continue;

                        if (chain.Count > c)
                        {
                            // test the next node in the chain
                            if (nodes_after_the_root.Where(x => x.ID == chain[c].ID).Count() == 0)
                                nodes_after_the_root.Add(chain[c]);
                        }
                        else
                        {
                            // reached the end of a chain
                            roots_finalized[i] = true;
                            break;
                        }
                    }
                    if (nodes_after_the_root.Count != 1)
                    {
                        // no further movement to the end of the chains possible
                        roots_finalized[i] = true;
                        break;
                    }
                    else
                    {
                        roots[i] = nodes_after_the_root[0];
                    }
                }

            }

            return roots;
        }

        #endregion

        #region METHODS: Component Record View

        public void CreateView(List<Category> _cats, bool _or = true)
        {
            foreach(Component comp in this.component_record)
            {
                comp.HideAccTo(_cats, _or);
            }
            this.RefreshView = !(this.RefreshView);
        }

        public void CreateView(List<ComponentManagerType> _users, bool _or = true)
        {
            foreach (Component comp in this.component_record)
            {
                comp.HideAccTo(_users, _or);
            }
            this.RefreshView = !(this.RefreshView);
        }

        #endregion

        #region METHODS: Management (Remove, Copy Components)

        public bool RemoveComponent(Component _comp, bool _remove_slot, bool _remove_references = true)
        {
            if (_comp == null) return false;

            if (_remove_references)
                _comp.RemoveReferenceFromAllComponents(this.Caller);

            // remove connection to networks
            _comp.UnbindFormNWElements();

            _comp.PropertyChanged -= top_level_comp_PropertyChanged;
            bool success_level0 = this.ComponentRecord.Remove(_comp);
            if (success_level0)
            {
                this.UpdateFlatComponentList();
                return true;
            }
            else
            {
                foreach (Component comp in this.ComponentRecord)
                {
                    bool success_levelN = comp.RemoveSubComponent(_comp, _remove_slot, this.Caller);
                    if (success_levelN)
                    {
                        this.UpdateFlatComponentList();
                        return true;
                    }
                }
                return false;
            }
        }

        // does not remove locked components
        public bool RemoveMarkedComponents()
        {
            bool success = true;
            List<Component> to_remove = new List<Component>();
            foreach(Component c in this.component_record)
            {
                if (c.IsMarked && !c.IsLocked)
                    to_remove.Add(c);               
            }
            foreach(Component c in to_remove)
            {
                success &= c.RemoveReferenceFromAllComponents(this.Caller);
                c.UnbindFormNWElements();
                c.PropertyChanged -= top_level_comp_PropertyChanged;
                success &= this.ComponentRecord.Remove(c);
            }
            this.UpdateFlatComponentList();
            ComponentFactory.UpdateComponentCounter(); // added 14.09.2016
            return success;
        }

        public bool ClearRecord()
        {
            bool success = true;

            foreach (Component c in this.component_record)
            {
                c.UnbindFormNWElements();
                c.PropertyChanged -= top_level_comp_PropertyChanged;   
            }
            this.component_record.Clear();
            this.UpdateFlatComponentList();
            ComponentFactory.UpdateComponentCounter(); // added 14.09.2016

            foreach (FlowNetwork nw in this.network_record)
            {
                nw.PropertyChanged -= top_level_network_PropertyChanged;
            }
            this.network_record.Clear();
            this.UpdateFlatNetworkList();

            return success;
        }

        /// <summary>
        /// Was meant to copy only top-level components. Calling method was chnaged to copy any component to the top level (27.09.2017).
        /// </summary>
        public Component CopyUnassignedComponent(Component _comp_to_copy, bool _add_to_record = true)
        {
            if (_comp_to_copy == null) return null;

            bool has_access = _comp_to_copy.HasWritingAccess(this.Caller);
            if (!has_access) return null;

            // COPY
            Component comp_copy = new Component(_comp_to_copy);

            // add to the record
            if (_add_to_record)
            {
                _comp_to_copy.PropertyChanged += top_level_comp_PropertyChanged;
                this.ComponentRecord.Add(comp_copy);
                // update the internal factory containers
                this.UpdateFlatComponentList();
            }

            return comp_copy;
        }

        public bool CopyComponent(Component _comp_to_copy, Component _comp_parent, string _slot_in_parent)
        {
            if (_comp_to_copy == null) return false;

            bool has_access = _comp_to_copy.HasWritingAccess(this.Caller);
            if (!has_access) return false;

            if (_comp_parent == null)
            {
                // add to the record
                Component comp_copy = new Component(_comp_to_copy);
                comp_copy.PropertyChanged += top_level_comp_PropertyChanged;
                this.ComponentRecord.Add(comp_copy);
                // update the internal factory containers
                this.UpdateFlatComponentList();
                return true;
            }
            else
            {
                // add to the parent component as a SubComponent
                bool has_access_to_parent = _comp_parent.HasWritingAccess(this.Caller);
                if (!has_access_to_parent) return false;
                if (string.IsNullOrEmpty(_slot_in_parent)) return false;

                // added 07.09.20147: check if the slot is occupied by a component that cannot be deleted
                if ( _comp_parent.ContainedComponents.ContainsKey(_slot_in_parent) &&
                    _comp_parent.ContainedComponents[_slot_in_parent] != null)
                {
                    if (!(_comp_parent.ContainedComponents[_slot_in_parent].RemovalOfCompAdmissible()))
                        return false;
                }

                Component comp_copy = new Component(_comp_to_copy);

                bool success0 = _comp_parent.ContainedComponents.ContainsKey(_slot_in_parent);
                if (!success0)
                    success0 = _comp_parent.AddSubComponentSlot(_slot_in_parent, this.Caller);

                if (success0)
                {
                    bool success1 = _comp_parent.AddSubComponent(_slot_in_parent, comp_copy, this.Caller);
                    if (success1)
                    {
                        // update the internal factory containers
                        this.UpdateFlatComponentList();
                        return true;
                    }
                    else
                        return false;
                }
                else
                    return false;
            }

        }

        private bool InsertAutomaticallyCreatedSubComponent(Component _parent, Component _child)
        {
            if (_parent == null || _child == null) return false;
            if (!(_child.IsAutomaticallyGenerated)) return false;

            // check the writing access
            bool has_access_to_parent = _parent.HasWritingAccess(this.Caller);
            if (!has_access_to_parent) return false;
            bool has_access_to_child = _child.HasWritingAccess(this.Caller);
            if (!has_access_to_child) return false;

            // 1. generate the appropriate slot
            string slot = _parent.GenerateSlotFor(_child.R2GMainState.Type, true);
            if (slot == null) return false; // something went very wrong

            // 2. add the child component
            _child.SetCurrentSlotWoSideEffects(ComponentUtils.Relation2GeomTypeToSlot(_child.R2GMainState.Type));
            bool success0 = _parent.AddSubComponentSlot(slot, this.Caller);
            if (success0)
            {
                bool success1 = _parent.AddSubComponent(slot, _child, this.Caller);
                if (success1)
                {
                    // update the internal factory containers
                    this.UpdateFlatComponentList();
                    return true;
                }
            }

            return false;
        }

        private bool RemoveAutomaticallyCreatedSubComponent(Component _parent, Component _child)
        {
            if (_parent == null || _child == null) return false;
            if (!(_child.IsAutomaticallyGenerated)) return false;

            // perform remove
            return _parent.RemoveSubComponent(_child, true, this.Caller);
        }

        public static bool AddReferenceComponent(Component _comp_ref, Component _comp_parent, string _slot_in_parent, 
                                          ComponentManagerType _user)
        {
            if (_comp_ref == null || _comp_parent == null) return false;
            if (string.IsNullOrEmpty(_slot_in_parent)) return false;

            bool has_access = _comp_parent.HasWritingAccess(_user);
            if (!has_access) return false;

            // add to the parent component as a Reference
            bool success0 = _comp_parent.ReferencedComponents.ContainsKey(_slot_in_parent);
            if (!success0)
                success0 = _comp_parent.AddReferencedComponentSlot(_slot_in_parent, _user);

            if (success0)
            {
                bool success1 = _comp_parent.AddReferencedComponent(_slot_in_parent, _comp_ref, _user);
                return success1;
            }
            else
                return false;

        }

        // to use ONLY while parsing (this method does not timestamp the WRITE activity in the parent component)
        private static bool AddReferenceComponent(Component _comp_ref, Component _comp_parent, string _slot_in_parent)
        {
            if (_comp_ref == null || _comp_parent == null) return false;
            if (string.IsNullOrEmpty(_slot_in_parent)) return false;

            // add to the parent component as a Reference
            bool success0 = _comp_parent.ReferencedComponents.ContainsKey(_slot_in_parent);
            if (!success0)
                success0 = _comp_parent.AddReferencedComponentSlot(_slot_in_parent);

            if (success0)
            {
                bool success1 = _comp_parent.AddReferencedComponent(_slot_in_parent, _comp_ref);
                return success1;
            }
            else
                return false;
        }


        #endregion

        #region METHODS: Management (Parameters)

        /// <summary>
        /// <para>To use before deleting a subcomponent.</para>
        /// <para>Tests if its parameters are involved in calculations in some of the parent components.</para>
        /// <para>Calculations in the component itself are disregarded.</para>
        /// </summary>
        internal ParameterDeleteResult RemoveParameterFromComponentTest(Parameter.Parameter _p_to_remove, Component _comp, 
            Calculation _calc_to_disregard = null, bool _disregard_owner_calcs = true)
        {
            if (_p_to_remove == null || _comp == null) return ParameterDeleteResult.ERR_ARG_NULL;

            // added 02.09.2016
            // find the component to which the parameter ACTUALLY belongs
            // because the method can be called from ANY of its parent components
            Component owner = null;
            if (_comp.ContainedParameters.ContainsKey(_p_to_remove.ID))
            {
                owner = _comp;
            }
            else
            {
                List<Component> sComps = _comp.GetFlatSubCompList();
                foreach (Component sC in sComps)
                {
                    if (sC.ContainedParameters.ContainsKey(_p_to_remove.ID))
                    {
                        owner = sC;
                        break;
                    }
                }
            }

            if (owner == null)
                return ParameterDeleteResult.ERR_NOT_FOUND;

            // get the parent chain of the component
            List<Component> comp_chain = this.GetParentComponentChain(owner);
            int index_caller = comp_chain.FindIndex(x => x.ID == _comp.ID);
            int index_owner = comp_chain.FindIndex(x => x.ID == owner.ID);
            for (int i = 0; i < comp_chain.Count; i++ )
            {
                Component pComp = comp_chain[i];

                if (!pComp.HasCalculations) continue;
                if (_disregard_owner_calcs && index_caller <= i && i <= index_owner) continue; // disregard owner calculations

                bool involved = pComp.ParamIsInvolvedInCalcs(_p_to_remove, _calc_to_disregard);
                if (involved)
                    return ParameterDeleteResult.ERR_BOUND_IN_CALC;
            }

            //foreach (Component pComp in comp_chain)
            //{
            //    if (!pComp.HasCalculations) continue;
            //    if (_disregard_owner_calcs && pComp.ID == owner.ID) continue; // disregard owner calculations

            //    bool involved = pComp.ParamIsInvolvedInCalcs(_p_to_remove, _calc_to_disregard);
            //    if (involved)
            //        return ParameterDeleteResult.ERR_BOUND_IN_CALC;
            //}

            return ParameterDeleteResult.SUCCESS;
        }

        public ParameterDeleteResult RemoveParameterFromComponent(Parameter.Parameter _p_to_remove, Component _comp)
        {
            if (_p_to_remove == null || _comp == null) return ParameterDeleteResult.ERR_ARG_NULL;   
        
            // added 02.09.2016
            // find the component to which the parameter ACTUALLY belongs
            // because the method can be called from ANY of its parent components
            Component caller = null;
            if (_comp.ContainedParameters.ContainsKey(_p_to_remove.ID))
            {
                caller = _comp;
            }
            else
            {               
                List<Component> sComps = _comp.GetFlatSubCompList();
                foreach (Component sC in sComps)
                {
                    if (sC.ContainedParameters.ContainsKey(_p_to_remove.ID))
                    {
                        caller = sC;
                        break;
                    }
                }
            }
            
            if (caller == null)
                return ParameterDeleteResult.ERR_NOT_FOUND;

            // get the parent chain of the component
            List<Component> comp_chain = this.GetParentComponentChain(caller);
            foreach(Component pComp in comp_chain)
            {
                if (!pComp.HasCalculations) continue;

                bool involved = pComp.ParamIsInvolvedInCalcs(_p_to_remove);
                if (involved)
                    return ParameterDeleteResult.ERR_BOUND_IN_CALC;
            }

            // if all is well, we can delete the parameter
            bool deleted = caller.RemoveParameter(_p_to_remove, this.Caller);
            if (!deleted)
                return ParameterDeleteResult.ERR_NOT_FOUND;
            else
                return ParameterDeleteResult.SUCCESS;
        }

        internal bool PassParameterChangeToCalculations(Parameter.Parameter _p_changed, Component _comp, bool _needs_recalculation = false)
        {
            if (_p_changed == null || _comp == null) return false;

            // get the parent chain of the component
            List<Component> comp_chain = this.GetParentComponentChain(_comp);
            foreach (Component pComp in comp_chain)
            {
                if (!pComp.HasCalculations) continue;

                List<Calculation> calcs_involving_p = pComp.GetCalculationsInvolving(_p_changed);
                if (calcs_involving_p.Count > 0)
                {
                    foreach (Calculation c in calcs_involving_p)
                    {
                        c.GatherInputParamNames(true);
                        c.GatherReturnParamNames(true);
                        if (_needs_recalculation)
                            c.UpToDate = false;
                    }
                }
            }

            return true;
        }

        internal bool AdoptRefParamValueFromParentChain(ref Parameter.Parameter _p_to_change, Component _comp)
        {
            if (_p_to_change == null || _comp == null) return false;

            // get the parent chain of the component
            List<Component> comp_chain = this.GetParentComponentChain(_comp);
            foreach (Component pComp in comp_chain)
            {
                pComp.AdoptRefParamValue(ref _p_to_change);
            }
            return true;
        }

        internal bool AdoptLimitingRefParamValueFromParentChain(ref Parameter.Parameter _p_to_change, Component _comp)
        {
            if (_p_to_change == null || _comp == null) return false;

            // get the parent chain of the component
            List<Component> comp_chain = this.GetParentComponentChain(_comp);
            foreach (Component pComp in comp_chain)
            {
                pComp.AdoptLimitingRefParamValue(ref _p_to_change);
            }
            return true;
        }

        // If a parameter was added to a component that was used as a template to create many others ->
        // and ONE new parameter was added -> propagate the change to all others
        public int PropagateOneAddedParameter(Component _c_template)
        {
            if (_c_template == null) return 0;

            int nr_changed = 0;
            foreach(Component c in this.component_record_flat)
            {
                if (c.ID == _c_template.ID) continue;
                bool changed = c.SynchOneParameter(_c_template);
                if (changed)
                    nr_changed++;
            }

            return nr_changed;
        }

        #endregion

        #region METHODS: Management (Calculations)

        // added 02.09.2016
        public bool TestAndSaveCalculationInEditMode(Component _comp, Calculation _calc, ComponentManagerType _user, ref CalculationState calc_state)
        {
            if (_comp == null || _calc == null) return false;

            // find the component to which the calculation ACTUALLY belongs
            // because the method can be called from ANY of its parent components when EDITING
            Component caller = _comp;
            if (!_comp.ContainedCalculations.Contains(_calc))
            {
                List<Component> sComps = _comp.GetFlatSubCompList();
                foreach (Component sC in sComps)
                {
                    if (sC.ContainedCalculations.Contains(_calc))
                    {
                        caller = sC;
                        break;
                    }
                }
            }

            return caller.TestAndSaveCalculationInEditMode(_user, _calc, ref calc_state);
        }

        #endregion

        #region METHODS: Management (Merging Component Records)

        public void AddToRecord(List<Component> _components_to_add)
        {
            if (_components_to_add == null) return;
            if (_components_to_add.Count < 1) return;

            // 0. check the state of Type Component
            ComponentFactory.UpdateComponentCounter();
            // 1. check the ids of the new components to avoid duplicates in the record
            // 2. make sure they refer to this factory
            List<Component> to_change_id = new List<Component>();
            foreach(Component c in _components_to_add)
            {
                if (c.ID < Component.NR_COMPONENTS)
                    to_change_id.Add(c);
                c.Factory = this;
                
                List<Component> c_subC = c.GetFlatSubCompList();
                foreach(Component sC in c_subC)
                {
                    if (sC.ID < Component.NR_COMPONENTS)
                        to_change_id.Add(sC);
                    sC.Factory = this;
                }
            }

            // adjust the ids of the new components
            foreach(Component c in to_change_id)
            {
                c.ID = (++Component.NR_COMPONENTS);
            }

            // 3. add to the record
            foreach(Component c in _components_to_add)
            {
                c.PropertyChanged += top_level_comp_PropertyChanged;
                this.component_record.Add(c);   
            }
            this.UpdateFlatComponentList();
            ComponentFactory.UpdateComponentCounter(); // added 01.12.2016

            List<Component> added_flat = Component.GetFlattenedListOf(_components_to_add);

            // 4. adjust the IDs of the contained Geometric Relationships, added 25.08.2017
            // 5. adjust the parameter IDs inside the newly components, added 25.08.2017
            // 6. adjust the calculation IDs inside the newly components, added 25.08.2017
            List<Geometry.GeometricRelationship> all_added_gr = new List<Geometry.GeometricRelationship>();
            List<Parameter.Parameter> all_added_params = new List<Parameter.Parameter>();
            List<Calculation> all_added_calculations = new List<Calculation>();
            foreach (Component c in added_flat)
            {
                all_added_gr.AddRange(c.R2GInstances);
                all_added_params.AddRange(c.ContainedParameters.Values);
                all_added_calculations.AddRange(c.ContainedCalculations);
            }
            Geometry.GeometricRelationship.AdjustInstanceIds(ref all_added_gr);
            Parameter.Parameter.AdjustInstanceIds(ref all_added_params);
            Calculation.AdjustInstanceIds(ref all_added_calculations);
        }

        public void AddToRecord(List<FlowNetwork> _nws_to_add)
        {
            if (_nws_to_add == null) return;
            if (_nws_to_add.Count < 1) return;

            // 0. check the state of Type FlNetElement
            ComponentFactory.UpdateNetworkCounter();
            // 1. change the ids of the new networks to avoid duplicates in the record
            long max_current_id = FlNetElement.NR_FL_NET_ELEMENTS - 1;
            foreach(FlowNetwork nw in _nws_to_add)
            {
                nw.UpdateAllElementIds(ref max_current_id);
            }
            FlNetElement.NR_FL_NET_ELEMENTS = max_current_id + 1;

            // add to the record
            foreach(FlowNetwork nw in _nws_to_add)
            {
                nw.PropertyChanged += top_level_network_PropertyChanged;
                this.network_record.Add(nw);                
            }
            this.UpdateFlatNetworkList();
        }

        // copies the components and restores the references btw components
        public static List<Component> CopyComponents(List<Component> _to_copy)
        {
            List<Component> copied = new List<Component>();
            if (_to_copy == null) return copied;
            if (_to_copy.Count < 1) return copied;

            //// check the state of Type Component (has no effect, because method does not change any of the involved factories)
            //ComponentFactory.UpdateComponentCounter();

            // perform the copying W/O THE REFERENCED COMPONENTS
            // while saving a request for the referenced component to be fulfilled after copying is complete
            Dictionary<long, Component> id_old_comp_new = new Dictionary<long, Component>();
            List<ComponentReferenceRequest> references_to_restore = new List<ComponentReferenceRequest>();
            foreach(Component c in _to_copy)
            {
                // deep copy w/o referenced components
                Component c_copy = new Component(c, false);
                copied.Add(c_copy);
                // maintain the copy record
                Dictionary<long, Component> c_id_old_comp_new = Component.GetCopyRecord(c, c_copy);
                //// adapt counter (added 01.12.2016)
                //Component.NR_COMPONENTS = Math.Max(Component.NR_COMPONENTS, c_id_old_comp_new.Values.Max(x => x.ID));
                foreach(var entry in c_id_old_comp_new)
                {
                    try
                    {
                        id_old_comp_new.Add(entry.Key, entry.Value);
                    }
                    catch
                    {

                    }
                }
                // record the reference requests along the entire hierarchy
                List<ComponentReferenceRequest> c_refs_to_restore = Component.GetReferenceRequests(c, c_copy);
                references_to_restore.AddRange(c_refs_to_restore);
            }
            
            // restore references
            foreach(ComponentReferenceRequest request in references_to_restore)
            {
                if (request.ID_Reqested < 0)
                {
                    // empty slot -> can be added immediately
                    if (!(request.Seeker.ReferencedComponents.ContainsKey(request.Slot_Requested)))
                        request.Seeker.AddReferencedComponentSlot(request.Slot_Requested);
                }
                else
                {
                    if (id_old_comp_new.ContainsKey(request.ID_Reqested))
                    {
                        ComponentFactory.AddReferenceComponent(id_old_comp_new[request.ID_Reqested], request.Seeker, request.Slot_Requested);
                    }
                    else
                    {
                        // dead reference -> add only the slot
                        if (!(request.Seeker.ReferencedComponents.ContainsKey(request.Slot_Requested)))
                            request.Seeker.AddReferencedComponentSlot(request.Slot_Requested);
                    }
                }
            }

            return copied;
        }

        #endregion

        #region METHODS: Management (Add, Copy, Remove Networks, Convert Node <-> Network )

        public long AddNetworkToNetwork(FlowNetwork _parent, Point _pos, string _name, string _description)
        {
            if (_parent == null) return -1;
            if (!this.network_record_flat.Contains(_parent)) return -1;

            long id_created = _parent.AddFlowNetwork(_pos, _name, _description);

            this.UpdateFlatNetworkList();

            return id_created;  
        }

        public long CopyNetwork(FlowNetwork _nw_to_copy, bool _copy_to_top)
        {
            if (_nw_to_copy == null) return -1;
            if (_nw_to_copy.IsLocked) return -1;

            // look for parents
            FlowNetwork nw_parent = null;
            List<FlowNetwork> parent_chain = this.GetParentNetworkChain(_nw_to_copy);
            if (parent_chain.Count > 1)
                nw_parent = parent_chain[parent_chain.Count - 2];
            
            if (nw_parent == null || _copy_to_top)
            {
                // add to the record
                FlowNetwork nw_copy = new FlowNetwork(_nw_to_copy);
                nw_copy.PropertyChanged += top_level_network_PropertyChanged;
                this.NetworkRecord.Add(nw_copy);
                // update the internal factory containers
                this.UpdateFlatNetworkList();
                return nw_copy.ID;
            }
            else
            {
                if (nw_parent.IsLocked) return -1;

                // add to parent network as sub-network 
                // (with an OFFSET so original and copy do not overlap completely)
                FlowNetwork nw_copy = new FlowNetwork(_nw_to_copy);
                Point pos_new = nw_copy.Position + FlowNetwork.COPY_OFFSET;
                nw_copy.Position = pos_new;

                bool success = nw_parent.AddFlowNetwork(nw_copy);
                if (success)
                {
                    // update internal factory containers
                    this.UpdateFlatNetworkList();
                    return nw_copy.ID;
                }
                else
                {
                    return -1;
                }
            }

        }

        public bool RemoveNetwork(FlowNetwork _netw, bool _inform_content = true)
        {
            if (_netw == null) return false;
            if (_netw.IsLocked) return false;

            return this.RemoveNetworkRegardlessOfLocking(_netw, _inform_content);
        }

        internal bool RemoveNetworkRegardlessOfLocking(FlowNetwork _netw, bool _inform_content = true)
        {
            // alert contained component(s)
            if (_inform_content)
            {
                if (_netw.Content != null)
                    _netw.Content.BindingFNE = null;
                List<Component> netw_contents = _netw.GetAllContent();
                foreach (Component c in netw_contents)
                {
                    c.BindingFNE = null;
                }
            }

            _netw.PropertyChanged -= top_level_network_PropertyChanged;
            bool success_level0 = this.NetworkRecord.Remove(_netw);
            if (success_level0)
            {
                this.UpdateFlatNetworkList();
                return true;
            }
            else
            {
                foreach (FlowNetwork nw in this.NetworkRecord)
                {
                    bool success_levelN = nw.RemoveNetwork(_netw);
                    if (success_levelN)
                    {
                        this.UpdateFlatNetworkList();
                        return true;
                    }
                }
                return false;
            }
        }

        public FlowNetwork ConvertNodeToNetwork(FlowNetwork _parent, FlNetNode _node)
        {
            if (_parent == null) return null;

            FlowNetwork converted = _parent.NodeToNetwork(_node);
            if (converted != null)
            {
                this.UpdateFlatNetworkList();
            }

            return converted;
        }

        public FlNetNode ConvertNetworkToNode(FlowNetwork _parent, FlowNetwork _nw)
        {
            if (_parent == null) return null;

            FlNetNode converted = _parent.NetworkToNode(_nw);
            if (converted != null)
            {
                this.UpdateFlatNetworkList();
            }

            return converted;
        }


        // does not remove locked networks
        public bool RemoveMarkedNetworks()
        {
            bool success = true;
            List<FlowNetwork> to_remove = new List<FlowNetwork>();
            foreach (FlowNetwork nw in this.network_record)
            {
                if (nw.IsMarked && !nw.IsLocked)
                    to_remove.Add(nw);
            }
            foreach (FlowNetwork nw in to_remove)
            {
                nw.PropertyChanged -= top_level_network_PropertyChanged;
                success &= this.NetworkRecord.Remove(nw);
            }
            this.UpdateFlatNetworkList();
            return success;
        }

        internal bool ClearNetworkRecord()
        {
            foreach (FlowNetwork nw in this.network_record)
            {
                nw.PropertyChanged -= top_level_network_PropertyChanged;
            }
            this.network_record.Clear();
            this.UpdateFlatNetworkList();
            return true;
        }

        public void SwitchNetworkManager(FlowNetwork _nw, ComponentManagerType _manager_new)
        {
            if (_nw == null) return;
            if (this.Caller != ComponentManagerType.ADMINISTRATOR) return;

            _nw.ChangeManager(_manager_new);
        }

        #endregion

        #region METHODS: Selection Handling

        public void SelectComponent(Component _comp)
        {
            DisplayableProductDefinition.SelectElement(_comp, this.component_record, ref this.component_record_flat);
        }

        public Component SelectComponent(long _id)
        {
            return DisplayableProductDefinition.SelectElement(_id, this.component_record, ref this.component_record_flat);
        }

        public void SelectNetwork(FlowNetwork _nw)
        {
            DisplayableProductDefinition.SelectElement(_nw, this.network_record, ref this.network_record_flat);
        }

        public FlowNetwork SelectNetwork(long _id)
        {
            return DisplayableProductDefinition.SelectElement(_id, this.network_record, ref this.network_record_flat);
        }


        #endregion

        #region CLASS MEMBERS, METHODS: ---Search---

        private IEnumerator<Component> matchingCompEnumerator;
        private string prev_search_text;

        public Component ProcessSearch(string _partString)
        {
            if (_partString == null || _partString == String.Empty)
                return null;

            this.UpdateFlatComponentList(true, false);

            if (this.component_record_flat.Count < 1)
                return null;

            if (this.matchingCompEnumerator == null || !this.matchingCompEnumerator.MoveNext() || prev_search_text != _partString)
                this.VerifyMatches(_partString);

            Component n = this.matchingCompEnumerator.Current;
            // update display parameters
            if (n != null)
            {
                Component comp = n as Component;
                if (comp != null)
                    this.SelectComponent(comp);
            }

            this.prev_search_text = _partString;
            return n;
        }

        private void VerifyMatches(string _text)
        {
            IEnumerable<Component> matchingEntities = this.FindMatches(_text);
            this.matchingCompEnumerator = matchingEntities.GetEnumerator();

            if (!matchingCompEnumerator.MoveNext())
                MessageBox.Show("No matching components were found.", "Try Again", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private IEnumerable<Component> FindMatches(string _text)
        {
            foreach (Component c in this.component_record_flat)
            {
                if (c.Name.Contains(_text) || c.Description.Contains(_text) || c.CurrentSlot.Contains(_text))
                    yield return c;
            }
        }

        #endregion

        #region METHODS: DXF Export To Single File

        public StringBuilder ExportRecord(bool _export_marked_only, bool _finalize)
        {
            StringBuilder sb = new StringBuilder();
            // COMPONENTS
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_START);
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_NAME).ToString());
            sb.AppendLine(ParamStructTypes.ENTITY_SECTION);

            if (this.component_record.Count > 0)
            {
                foreach (Component record in this.component_record)
                {
                    if (_export_marked_only)
                    {
                        if (record.IsMarkable && record.IsMarked)
                            record.AddToExport(ref sb);
                    }
                    else
                    {
                        record.AddToExport(ref sb);
                    }
                }
            }

            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_END);

            // NETWORKS
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_START);
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_NAME).ToString());
            sb.AppendLine(ParamStructTypes.NETWORK_SECTION);

            if (this.network_record.Count > 0)
            {
                foreach(FlowNetwork nw in this.network_record)
                {
                    if (_export_marked_only)
                    {
                        if (nw.IsMarkable && nw.IsMarked)
                            nw.AddToExport(ref sb);
                    }
                    else
                    {
                        nw.AddToExport(ref sb);
                    }
                }
            }

            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_END);

            // FINALIZE FILE
            if (_finalize)
            {
                sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
                sb.AppendLine(ParamStructTypes.EOF);
            }

            return sb;
        }

        #endregion

        #region METHODS: DXF Export To Multiple Files

        private static StringBuilder ExportPartialRecord(List<Component> _c_to_export, List<FlowNetwork> _nw_to_export,
                                                         bool _export_marked_only, bool _finalize)
        {
            if (_c_to_export == null && _nw_to_export == null) return null;
            if (_c_to_export.Count == 0 && _nw_to_export.Count == 0) return new StringBuilder();

            StringBuilder sb = new StringBuilder();
            
            // COMPONENTS
            if (_c_to_export.Count > 0)
            {
                sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
                sb.AppendLine(ParamStructTypes.SECTION_START);
                sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_NAME).ToString());
                sb.AppendLine(ParamStructTypes.ENTITY_SECTION);

                foreach (Component record in _c_to_export)
                {
                    if (_export_marked_only)
                    {
                        if (record.IsMarkable && record.IsMarked)
                            record.AddToExport(ref sb);
                    }
                    else
                    {
                        record.AddToExport(ref sb);
                    }
                }

                sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
                sb.AppendLine(ParamStructTypes.SECTION_END);
            }

            // FLOW NETWORKS (have to be after the Components, because the NW Elements reference Components)
            if (_nw_to_export.Count > 0)
            {
                sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
                sb.AppendLine(ParamStructTypes.SECTION_START);
                sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_NAME).ToString());
                sb.AppendLine(ParamStructTypes.NETWORK_SECTION);

                foreach (FlowNetwork record in _nw_to_export)
                {
                    if (_export_marked_only)
                    {
                        if (record.IsMarkable && record.IsMarked)
                            record.AddToExport(ref sb);
                    }
                    else
                    {
                        record.AddToExport(ref sb);
                    }
                }

                sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
                sb.AppendLine(ParamStructTypes.SECTION_END);
            }

            // EOF
            if (_finalize)
            {
                sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
                sb.AppendLine(ParamStructTypes.EOF);
            }

            return sb;
        }

        public Dictionary<ComponentManagerType, StringBuilder> ExportRecordDistributed()
        {
            // separate the component record into subrecords for export acc to manager type with writing access
            // only components AT THE HIGHEST HIERARCHY LEVEL are separated (subcomponents cannot be separated form the parent)
            Dictionary<ComponentManagerType, List<Component>> separated_C = ComponentUtils.SplitAccToProfile(this.component_record);

            // separate the network record into subrecords for export acc to manager type
            Dictionary<ComponentManagerType, List<FlowNetwork>> separated_NW = this.SplitNWRecordAccToManager();

            // perform the actual export
            Dictionary<ComponentManagerType, StringBuilder> export_strings = new Dictionary<ComponentManagerType, StringBuilder>();
            ////// OLD
            ////foreach(var entry in separated_C)
            ////{
            ////    List<Component> sub_record = entry.Value;
            ////    if (sub_record == null) continue;
            ////    if (sub_record.Count == 0) continue;

            ////    export_strings.Add(entry.Key, ComponentFactory.ExportPartialRecord(sub_record, false, true));
            ////}

            int nr_cmt = Enum.GetNames(typeof(ComponentManagerType)).Length;
            for (int i = 0; i < nr_cmt; i++)
            {
                ComponentManagerType manager = (ComponentManagerType)i;
                List<Component> sub_record_C = separated_C[manager];
                List<FlowNetwork> sub_record_NW = separated_NW[manager];

                if (sub_record_C.Count > 0 || sub_record_NW.Count > 0)
                {
                    export_strings.Add(manager, ComponentFactory.ExportPartialRecord(sub_record_C, sub_record_NW, false, true));
                }
            }

            return export_strings;
        }

        private Dictionary<ComponentManagerType, List<FlowNetwork>> SplitNWRecordAccToManager()
        {
            Dictionary<ComponentManagerType, List<FlowNetwork>> separated = new Dictionary<ComponentManagerType, List<FlowNetwork>>();
            
            int nr_cmt = Enum.GetNames(typeof(ComponentManagerType)).Length;
            for (int i = 0; i < nr_cmt; i++)
            {
                separated.Add((ComponentManagerType)i, new List<FlowNetwork>());
            }
            if (this.network_record.Count == 0) return separated;

            foreach(FlowNetwork nw in this.network_record)
            {
                for (int i = 0; i < nr_cmt; i++)
                {
                    ComponentManagerType manager = (ComponentManagerType)i;
                    if (manager == nw.Manager)
                    {
                        separated[manager].Add(nw);
                        break;
                    }
                }
            }

            return separated;
        }

        #endregion

        #region METHODS: Post-Parsing

        /// <summary>
        /// Assumes that all references and mappings to other components are within this factory record.
        /// Update: Method handles multiple factory records and cross-references btw them
        /// </summary>
        /// <returns></returns>
        public bool RestoreReferencesWithinRecord()
        {
            bool success = true;
            foreach(ComponentReferenceRequest request in this.component_references_to_restore)
            {
                Component ref_candidate = this.component_record_flat.Find(x => x.ID == request.ID_Reqested);

                if (ref_candidate == null)
                {
                    // just add the slot
                    if (!(request.Seeker.ReferencedComponents.ContainsKey(request.Slot_Requested)))
                        request.Seeker.AddReferencedComponentSlot(request.Slot_Requested);
                    success &= false;
                }
                else
                {
                    success &= ComponentFactory.AddReferenceComponent(ref_candidate, request.Seeker, request.Slot_Requested);
                }
            }
            // added 28.08.2017
            foreach(Mapping2ComponentCalculatorRequest map_request in this.component_mapping_to_restore)
            {
                Component mapping_candidate = this.component_record_flat.FirstOrDefault(x => x.ID == map_request.ID_Reqested);

                if (mapping_candidate == null)
                {
                    // remove the mapping from the component
                    map_request.Seeker.RemoveMapping(map_request.Map);
                    success &= false;
                }
                else
                {
                    map_request.Map.Calculator = mapping_candidate;
                    mapping_candidate.MappedToBy.Add(map_request.Seeker);
                    success &= true;
                }
            }

            // DONE
            this.component_references_to_restore = new List<ComponentReferenceRequest>();
            this.component_mapping_to_restore = new List<Mapping2ComponentCalculatorRequest>();
            return success;
        }

        public bool RestoreReferencesRecord2Network()
        {
            // TODO ...
            return true;
        }

        public void MakeParameterOutsideBoundsVisible()
        {
            foreach(Component c in this.component_record)
            {
                c.CheckParamBounds();
            }
        }

        #endregion

        #region METHODS: Post-Automatic Component Generation

        /// <summary>
        /// <para>Assumes the same sequence in all 3 input containers.</para>
        /// </summary>
        /// <param name="_affected_components">is couples each affected components with the appropriate action to take</param>
        /// <param name="_parent_ids"></param>
        /// <param name="_ref_comp_ids"></param>
        public void AdjustSubAndRefComponentDependenciesAfterAutomaticGeneration(Dictionary<Component, ComponentAction> _affected_components, 
                                                                                 List<long> _parent_ids, List<List<long>> _ref_comp_ids)
        {
            if (_affected_components == null || _parent_ids == null || _ref_comp_ids == null)
            {
                this.RefreshView = !(this.RefreshView);
                return; 
            }       

            int nr_affected = _affected_components.Count;
            if (nr_affected == 0 || nr_affected != _parent_ids.Count || nr_affected != _ref_comp_ids.Count) return;

            // 1. Find all existing parents of NEW automatically generated subcomponents and
            // DELETE all current automatically generated subcomponents together with their slots
            Dictionary<long, Component> parents_of_new_AG = new Dictionary<long, Component>();
            for (int i = 0; i < nr_affected; i++)
            {
                Component affected = _affected_components.ElementAt(i).Key;
                ComponentAction action_for_affected = _affected_components.ElementAt(i).Value;
                long parent_id = _parent_ids[i];
                if (action_for_affected == ComponentAction.CREATE && affected.IsAutomaticallyGenerated && 
                    parent_id > -1 && !parents_of_new_AG.ContainsKey(parent_id))
                {
                    Component parent = this.GetByID(parent_id);
                    if (parent != null)
                        parents_of_new_AG.Add(parent_id, parent);
                }
            }

            foreach (var entry in parents_of_new_AG)
            {
                Component parent = entry.Value;
                parent.RemoveAutomaticallyGeneratedSubComponents(this.Caller);
                // including NW bindings and references from other components
            }

            // 2. SUBcomponents
            for(int i = 0; i < nr_affected; i++)
            {
                Component affected = _affected_components.ElementAt(i).Key;
                ComponentAction action_for_affected = _affected_components.ElementAt(i).Value;
                
                // work with the component ids
                if (affected.IsAutomaticallyGenerated && _parent_ids[i] > -1)
                {
                    Component parent_of_affected = this.GetByID(_parent_ids[i]);
                    bool action_success = false;
                    if (action_for_affected == ComponentAction.CREATE)
                    {
                        // attach to parent (null reference handled in the method call below)                     
                        action_success = this.InsertAutomaticallyCreatedSubComponent(parent_of_affected, affected);                       
                    }
                    else if (action_for_affected == ComponentAction.DELETE)
                    {
                        // remove from parent (null reference handled in the method call below)
                        action_success = this.RemoveAutomaticallyCreatedSubComponent(parent_of_affected, affected);
                    }
                    if (action_success)
                    {
                        // remove the affected component from top level of record
                        affected.PropertyChanged -= top_level_comp_PropertyChanged;
                        bool success_level0 = this.ComponentRecord.Remove(affected);
                    }
                }                
                
            }

            // 3. Clean-up (errors adding automatically generated components)
            List<Component> to_remove = new List<Component>();
            foreach (Component c in this.component_record)
            {
                if (c.IsAutomaticallyGenerated)
                    to_remove.Add(c);
            }
            foreach (Component c in to_remove)
            {
                this.component_record.Remove(c);
            }

            this.UpdateFlatComponentList();

            // 4. REFERENCED Components
            for (int i = 0; i < nr_affected; i++)
            {
                Component affected = _affected_components.ElementAt(i).Key;
                this.UpdateReferencedComponentsFrom(affected, _ref_comp_ids[i]);
            }

            this.RefreshView = !(this.RefreshView);   
        }

        public void AdjustRefComponentDependencies(List<long> _comp_ids, List<List<long>> _ref_comp_ids)
        {
            if (_comp_ids == null || _ref_comp_ids == null) return;

            int nrComps = _comp_ids.Count;
            if (nrComps == 0 || nrComps != _ref_comp_ids.Count) return;

            for(int i = 0; i < nrComps; i++)
            {
                Component affected = this.GetByID(_comp_ids[i]);
                this.UpdateReferencedComponentsFrom(affected, _ref_comp_ids[i]);
            }

            this.RefreshView = !(this.RefreshView);
        }

        private void UpdateReferencedComponentsFrom(Component _comp, List<long> _ref_comp_ids)
        {
            if (_comp == null) return;
            if (_ref_comp_ids == null) return;
            // if _ref_comp_ids.Count == 0 -> delete all references

            // remove obsolete references
            foreach (var entry in _comp.ReferencedComponents)
            {
                Component refC = entry.Value;
                if (refC == null) continue;

                if (!(_ref_comp_ids.Contains(refC.ID)))
                {
                    _comp.RemoveReferencedComponent_Level0(refC, true, this.Caller);
                }
            }
            // add missing references
            foreach (long ref_id in _ref_comp_ids)
            {
                // if it already has the reference -> SKIP
                if (_comp.HasReferencedComp_Level0(ref_id)) continue;

                Component referenced_by_comp = this.GetByID(ref_id);
                if (referenced_by_comp == null) continue;

                string reference_slot = _comp.GenerateSlotFor(referenced_by_comp.R2GMainState.Type, false);
                if (reference_slot == null) continue; // something went very wrong here

                ComponentFactory.AddReferenceComponent(referenced_by_comp, _comp, reference_slot, this.Caller);
            }
        }

        #endregion

        #region METHODS: Post-Placement in Geometry

        public void UpdateConnectivity(Component _comp)
        {
            if (_comp.R2GMainState.Type != Geometry.Relation2GeomType.CONTAINED_IN) return;
            foreach(Geometry.GeometricRelationship gr in _comp.R2GInstances)
            {
                if (gr.InstanceNWElementID < 0) continue;
                FlNetElement nwe = this.GetNWByID(gr.InstanceNWElementID);
                if (nwe == null) continue;
                FlNetNode n = nwe as FlNetNode;
                if (n == null) continue;
                n.UpdateAdjacentEdgeRealization();
            } 
        }

        #endregion

        #region METHODS: Display

        public void ExpandAll()
        {
            foreach(Component c in this.component_record)
            {
                c.ExpandComp();
            }
        }

        public void ExpandComp(Component _c)
        {
            if (_c != null)
                _c.ExpandComp();
        }

        public void CollapseAll()
        {
            foreach (Component c in this.component_record)
            {
                c.CollapseComp();
            }
        }

        public void MarkAll(bool _mark)
        {
            foreach(Component c in this.component_record)
            {
                c.IsMarked = _mark;
            }
        }

        public void MarkAllNW(bool _mark)
        {
            foreach(FlowNetwork nw in this.network_record)
            {
                nw.IsMarked = _mark;
            }
        }

        public int GetNrMarkedRecords()
        {
            int nr = 0;
            foreach(Component c in this.component_record)
            {
                if (c.IsMarked)
                    nr++;
            }
            return nr;
        }

        public int GetNrMarkedNWRecords()
        {
            int nr = 0;
            foreach(FlowNetwork nw in this.network_record)
            {
                if (nw.IsMarked)
                    nr++;
            }
            return nr;
        }

        public int GetNrMarkedUnlockedRecords()
        {
            int nr = 0;
            foreach (Component c in this.component_record)
            {
                if (c.IsMarked && !c.IsLocked)
                    nr++;
            }
            return nr;
        }

        public int GerNrMarkedUnlockedNetworks()
        {
            int nr = 0;
            foreach (FlowNetwork nw in this.network_record)
            {
                if (nw.IsMarked && !nw.IsLocked)
                    nr++;
            }
            return nr;
        }

        
        // mark the component along with all components it references
        public void MarkWReferences(Component _comp)
        {
            List<Component> to_mark = this.FindAllReferencesOnAllLevels(_comp);
            foreach(Component c in to_mark)
            {
                c.IsMarked = true;
            }
        }

        public List<Component> ExtractVisibleComponents(bool _apply_user_marking)
        {
            List<Component> extracted = new List<Component>();
            if (_apply_user_marking)
            {
                foreach (Component c in this.component_record)
                {
                    c.ApplyIsExcludedFromDisplay = _apply_user_marking; // added 21.08.2017
                    if (!c.IsExcludedFromDisplay)
                        extracted.Add(c);
                }
            }
            else
            {
                foreach (Component c in this.component_record)
                {
                    c.ApplyIsExcludedFromDisplay = _apply_user_marking; // added 21.08.2017
                }
                extracted = new List<Component>(this.component_record);
            }

            // added 02.11.2016 filter parameters according to propagation
            if (this.p_vis_flags != this.p_vis_flags_prev)
            {
                // remove the parameters of the respecive propagation type of the children collection of each component
                foreach(Component c in extracted)
                {
                    c.AdjustParametersInChildrenContainer(this.PVis, this.p_vis_all);
                }
                this.p_vis_flags_prev = this.p_vis_flags;
            }

            // 24.08.2017
            // call the setter of ApplyIsExcludedFromDisplay to propagate the Children filtering from bottom to top
            foreach(Component c in extracted)
            {
                c.UpdateChildrenContainersBottomToTop();
            }

            // 02.11.2016: do we need this?!
            CollectionViewSource.GetDefaultView(extracted).GroupDescriptions.Add(new PropertyGroupDescription("CurrentSlot"));
            return extracted;
        }

        // 'lightbulb'
        public void UserSwitchAll(bool _on)
        {
            foreach (Component c in this.component_record)
            {
                c.IsExcludedFromDisplay = !_on;
                c.SetLightBulbSettingsForSubComps(_on); // added 30.08.2017
            }
        }

        // 'lightbulb'
        // display content of network including references of references
        public void TurnAllContentOn(FlowNetwork _nw)
        {
            if (_nw == null) return;
            List<Component> content = _nw.GetAllContent();
            Dictionary<long, Component> content_w_refs = new Dictionary<long, Component>();           
            foreach(Component c in content)
            {
                // add the top-level parent
                Component c_tL_parent= this.GetParentComponent(c);
                if (c_tL_parent != null && !content_w_refs.ContainsKey(c_tL_parent.ID))
                    content_w_refs.Add(c_tL_parent.ID, c_tL_parent);

                // add the top-level referenced components
                List<Component> c_w_refs = this.FindAllReferencesOnAllLevels(c);
                foreach (Component c1 in c_w_refs)
                {
                    if (c1.IsMarkable && !content_w_refs.ContainsKey(c1.ID))
                        content_w_refs.Add(c1.ID, c1);
                }
                
            }

            foreach(var entry in content_w_refs)
            {
                entry.Value.IsExcludedFromDisplay = false;
            }
        }

        // 'highlight'
        // highlight content of network
        public void HighlightAllContent(FlowNetwork _nw, bool _on)
        {
            if (_nw == null) return;
            if (_on)
            {
                List<Component> content = _nw.GetAllContent();
                foreach (Component c in content)
                {
                    c.IsBoundInSelNW = true;
                }
            }
            else
            {
                foreach(Component c in this.component_record_flat)
                {
                    c.IsBoundInSelNW = false;
                }
            }
        }

        protected void UpdateCompChildrenAccToParamPropagation()
        {

        }

        #endregion

        #region METHODS: Export / Import Visibility

        public StringBuilder ExportLightbulbSettings()
        {
            StringBuilder sb = new StringBuilder();

            foreach (Component record in this.component_record)
            {
                if (record == null) continue;
                string on = (record.IsExcludedFromDisplay) ? "0" : "1";
                sb.AppendLine(record.ID + ":" + on);
                record.ExportLightBulbSettings(ref sb); // added 21.08.2017
            }

            return sb;
        }

        public void ImportLightbulbSettings(string _file_name)
        {
            if (this.component_record_flat == null)
                this.UpdateFlatComponentList();

            using(System.IO.StreamReader reader = new System.IO.StreamReader(_file_name))
            {
                while(reader.Peek() >= 0)
                {
                    string entry = reader.ReadLine();
                    if (!string.IsNullOrEmpty(entry))
                    {
                        string[] comps = entry.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                        if (comps.Length == 2)
                        {
                            long id = -1;
                            long.TryParse(comps[0], out id);

                            int setting = -1;
                            int.TryParse(comps[1], out setting);

                            if (id > - 1 && setting > -1)
                            {
                                Component c = this.component_record_flat.Find(x => x.ID == id);
                                if (c != null)
                                {
                                    c.IsExcludedFromDisplay = (setting == 0) ? true : false;
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region EVENT HANDLERS : Marked

        private void top_level_comp_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Component c = sender as Component;
            if (c == null || e == null) return;

            if (e.PropertyName == "IsMarked")
            {
                this.MarkedTrue = c.IsMarked;
                this.MarkedId = c.ID;
            }
        }

        private void top_level_network_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            FlowNetwork nw = sender as FlowNetwork;
            if (nw == null || e == null) return;

            if (e.PropertyName == "IsMarked")
            {
                this.MarkedNwTrue = nw.IsMarked;
                this.MarkedNwId = nw.ID;
            }
        }

        #endregion
    }
}
