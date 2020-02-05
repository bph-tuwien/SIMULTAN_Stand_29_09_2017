using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Data;

using ParameterStructure.Geometry;
using ParameterStructure.Parameter;

namespace ParameterStructure.Component
{
    public enum ComponentAutoFunction
    {
        SIZE = 0,
        CUMULATION = 1,
    }

    public enum ComponentAction
    {
        NONE = 0,
        DELETE = 1,
        CREATE = 2,
        UPDATE = 3
    }

    public partial class Component : DisplayableProductDefinition
    {
        #region STATIC CONSTANTS

        public const double SCALE_PIXEL_TO_M = 0.05;

        #endregion

        #region STATIC HELPER FUNCTIONS

        public static string ComponentActionToString(ComponentAction _action)
        {
            switch(_action)
            {
                case ComponentAction.CREATE:
                    return "CREATE";
                case ComponentAction.DELETE:
                    return "DELETE";
                case ComponentAction.UPDATE:
                    return "UPDATE";
                default:
                    return "NONE";
            }
        }

        public static ComponentAction StringToComponentAction(string _action)
        {
            if (string.IsNullOrEmpty(_action)) return ComponentAction.NONE;

            switch(_action)
            {
                case "CREATE":
                    return ComponentAction.CREATE;
                case "DELETE":
                    return ComponentAction.DELETE;
                case "UPDATE":
                    return ComponentAction.UPDATE;
                default:
                    return ComponentAction.NONE;
            }
        }

        #endregion

        #region PROPERTIES (derived): Instance Representation

        public CompositeCollection InstancesAndChildren 
        { 
            get
            {
                CompositeCollection instance_children = new CompositeCollection();
                foreach(object entry in this.children)
                {
                    instance_children.Add(entry);
                }
                instance_children.Add(new CollectionContainer { Collection = new List<GeometricRelationship>(this.R2GInstances) });
                return instance_children;
            } 
        }


        #endregion

        #region METHODS: Interaction with a FlowNetwork

        // to be called before deletion
        internal void UnbindFormNWElements()
        {
            if (this.BindingFNE != null)
                this.BindingFNE.Content = null;

            foreach (var entry in this.ContainedComponents)
            {
                if (entry.Value == null) continue;
                entry.Value.UnbindFormNWElements();
            }
        }

        #endregion

        #region METHODS: automatic component generation GENERAL

        public string GenerateSlotFor(Relation2GeomType _type, bool _for_subcomponents)
        {
            int slot_counter = 0;
            int iteration_guard = 100;
            string slot = ComponentUtils.Relation2GeomTypeToSlot(_type) +
                            ComponentUtils.COMP_SLOT_DELIMITER + "AG" + slot_counter.ToString();

            // check if the component already has such a slot
            if (_for_subcomponents)
            {
                while (this.ContainedComponents.ContainsKey(slot) && slot_counter < iteration_guard)
                {
                    slot_counter++;
                    slot = ComponentUtils.Relation2GeomTypeToSlot(_type) +
                                ComponentUtils.COMP_SLOT_DELIMITER + "AG" + slot_counter.ToString();
                }
                if (this.ContainedComponents.ContainsKey(slot))
                    return null; // something went very wrong!
            }
            else
            {
                while (this.ReferencedComponents.ContainsKey(slot) && slot_counter < iteration_guard)
                {
                    slot_counter++;
                    slot = ComponentUtils.Relation2GeomTypeToSlot(_type) +
                                ComponentUtils.COMP_SLOT_DELIMITER + "AG" + slot_counter.ToString();
                }
                if (this.ReferencedComponents.ContainsKey(slot))
                    return null; // something went very wrong!
            }

            return slot;
        }

        /// <summary>
        /// Recursive removal of automatically created components.
        /// </summary>
        /// <param name="_user"></param>
        /// <returns></returns>
        internal bool RemoveAutomaticallyGeneratedSubComponents(ComponentManagerType _user)
        {
            if (this.ContainedComponents == null) return true;

            bool success = true;
            foreach (var entry in this.ContainedComponents)
            {
                Component sComp = entry.Value;
                if (sComp == null) continue;

                if (sComp.IsAutomaticallyGenerated)
                    success &= this.RemoveSubComponent_Level0(sComp, true, _user);
                else
                    success &= sComp.RemoveAutomaticallyGeneratedSubComponents(_user);
            }

            return success;
        }

        #endregion

        #region METHODS: automatic component generation SPECIFIC

        private void AutoAddSubcomponent(string _name, string _description, ComponentAutoFunction _fct)
        {
            if (this.Factory == null) return;

            Component created = new Component();
            created.Factory = this.Factory;
            created.Name = _name;
            created.Description = _description;
            created.SetupForAutomaticallyCreatedComps(this.GetManagerWWritingAccess());
            
            // get the correct parameter set
            switch(_fct)
            {
                case ComponentAutoFunction.CUMULATION:
                    created.AddCumulativeParamsForNWCalculations();
                    break;
                default:
                    created.AddSizeParamsForNWCalculations();
                    break;
            }

            // add to the parent
            string slot = this.GenerateSlotFor(Relation2GeomType.CONTAINED_IN, true);
            bool success0 = this.AddSubComponentSlot(slot, ComponentManagerType.ADMINISTRATOR);
            if (success0)
            {
                bool success1 = this.AddSubComponent(slot, created, ComponentManagerType.ADMINISTRATOR);
                if (success1)
                {
                    // update the internal factory containers
                    this.Factory.UpdateFlatComponentList();
                }
            }
        }

        internal void AutoAddSubcomponentForSize()
        {
            this.AutoAddSubcomponent("Size", "DO NOT CHANGE", ComponentAutoFunction.SIZE);
        }

        internal void AutoAddSubcomponentForCumulation()
        {
            this.AutoAddSubcomponent("Cumulative", "DO NOT CHANGE", ComponentAutoFunction.CUMULATION);
        }

        private bool ContainsValidAutoSubcomponentFor(ComponentAutoFunction _fct)
        {
            List<Parameter.Parameter> parameters_for_check = new List<Parameter.Parameter>();           
            switch(_fct)
            {
                case ComponentAutoFunction.CUMULATION:
                    parameters_for_check = Parameter.Parameter.GetCumulativeParametersForInstancing();
                    break;
                default:
                    parameters_for_check = Parameter.Parameter.GetSizeParametersForInstancing();
                    break;
            }

            foreach(var entry in this.ContainedComponents)
            {
                Component c = entry.Value;
                if (c == null) continue;

                if (!c.IsAutomaticallyGenerated) continue;
                // if (c.R2GMainState.Type != Relation2GeomType.CONTAINED_IN && c.R2GMainState.Type != Relation2GeomType.CONNECTS) continue;

                // check the specific parameters               
                bool contains_all_p = false;
                bool missed_at_least_one = true;
                foreach (Parameter.Parameter p in parameters_for_check)
                {
                    missed_at_least_one = false;
                    Parameter.Parameter corresponding = c.ContainedParameters.FirstOrDefault(x => x.Value.Name == p.Name && x.Value.Unit == p.Unit && x.Value.Propagation == p.Propagation).Value;
                    if (corresponding == null)
                    {
                        missed_at_least_one = true;
                        break;
                    }
                }
                contains_all_p = !missed_at_least_one;
                if (contains_all_p)
                {
                    return true;
                }
            }

            return false;
        }

        internal bool ContainsValidAutoSubcomponentForSize()
        {
            return this.ContainsValidAutoSubcomponentFor(ComponentAutoFunction.SIZE);
        }

        internal bool ContainsValidAutoSubcomponentForCumulation()
        {
            return this.ContainsValidAutoSubcomponentFor(ComponentAutoFunction.CUMULATION);
        }

        #endregion

        #region METHODS: instancing INFO

        /// <summary>
        /// <para>Gets the components coupled with the parent id. Returns a tree with "this" as root.</para>
        /// </summary>
        /// <returns></returns>
        public Dictionary<Component, long> GetSubcompsWGeometricRelationships()
        {
            Dictionary<Component, long> found = new Dictionary<Component, long>();

            foreach (var entry in this.ContainedComponents)
            {
                bool connected_to_sub_tree = false;
                if (entry.Value.R2GMainState.Type != Relation2GeomType.NONE)
                {
                    found.Add(entry.Value, this.ID);
                    connected_to_sub_tree = true;
                }                   

                Dictionary<Component, long> sub_found = entry.Value.GetSubcompsWGeometricRelationships();
                if (sub_found.Count > 0)
                {
                    if (!connected_to_sub_tree)
                    {
                        found.Add(entry.Value, this.ID);
                        connected_to_sub_tree = true;
                    }
                    foreach (var subentry in sub_found)
                    {
                        found.Add(subentry.Key, subentry.Value);
                    }
                }
                
            }

            return found;
        }

        /// <summary>
        /// <para>Returns the ids of all referenced componensts ONE level deep - NO referenced by referenced component!</para>
        /// </summary>
        /// <returns></returns>
        public List<long> GetIdsOfAllReferencedComponents()
        {
            List<long> ids = new List<long>();
            foreach (var entry in this.ReferencedComponents)
            {
                if (entry.Value == null) continue;

                ids.Add(entry.Value.ID);
            }

            return ids;
        }

        #endregion

        #region METHODS: instance GENERAL MANAGEMENT

        private void SwitchR2GMainType()
        {
            this.R2GInstances[0].State = Relation2GeomState.Next(this.R2GMainState);
        }

        public bool AddGeometricRelationship(GeometricRelationship _gr, ComponentManagerType _user)
        {
            // check if the user has writing access
            bool success = this.RecordWritingAccess(_user);
            if (!success) return false;

            // perform the operation
            if (_gr == null) return false;
            if (this.R2GInstances == null) return false;

            foreach (GeometricRelationship instance in this.R2GInstances)
            {
                if (GeometricRelationship.ReferenceSameGeometry(instance, _gr))
                    return false;
            }

            this.R2GInstances.Add(_gr);
            return true;
        }

        public void UpdateGeometricRelationships(List<bool> _to_retain, List<GeometricRelationship> _to_add, ComponentManagerType _user)
        {
            // check if the user has writing access
            bool success = this.RecordWritingAccess(_user);
            if (!success) return;

            // 1.mark obsolete relationships for removal
            if (_to_retain == null || _to_retain.Count != this.R2GInstances.Count) return;

            for (int i = 0; i < this.R2GInstances.Count; i++)
            {
                if (_to_retain[i]) continue;

                this.R2GInstances[i].UpdateState = GeometricRelationshipUpdateState.TO_BE_DELETED;
            }

            // 2. add new relationships and mark them
            if (_to_add != null)
            {
                foreach (GeometricRelationship gr in _to_add)
                {
                    gr.UpdateState = GeometricRelationshipUpdateState.NEW;
                    this.R2GInstances.Add(gr);
                }
            }

            // 3. perform the removal
            List<GeometricRelationship> grs_new = new List<GeometricRelationship>();
            foreach (GeometricRelationship gr_old in this.R2GInstances)
            {
                if (gr_old.UpdateState != GeometricRelationshipUpdateState.TO_BE_DELETED)
                {
                    gr_old.UpdateState = GeometricRelationshipUpdateState.NEUTRAL;
                    grs_new.Add(gr_old);
                }
            }

            // mark the new representative relationship
            if (grs_new[0].ID != this.R2GInstances[0].ID)
                grs_new[0].PropertyChanged += main_r2g_PropertyChanged;

            // switch
            this.R2GInstances = new List<GeometricRelationship>(grs_new);
            this.R2GMainState = this.R2GInstances[0].State;
        }

        #endregion

        #region METHODS: instancing in NW elements INFO, SIZE, -SIZE TRANSFER-

        internal GeometricRelationship GetInstanceIn(FlNetElement _container)
        {
            if (_container == null) return null;
            if (this.R2GInstances == null) return null;

            return this.R2GInstances.FirstOrDefault(x => x.InstanceNWElementID == _container.ID);
        }

        internal List<long> GetIdsOfAllInstanceContainers()
        {
            List<long> ids = new List<long>();
            foreach (GeometricRelationship gr in this.R2GInstances)
            {
                if (!(ids.Contains(gr.InstanceNWElementID)))
                    ids.Add(gr.InstanceNWElementID);
            }

            return ids;
        }

        /// <summary>
        /// <para>Transfer ONLY the size of the component instance bound in a network element.</para>
        /// </summary>        
        internal void TransferSize(long _nwe_id, double _min_h, double _min_b, double _min_L, double _max_h, double _max_b, double _max_L)
        {
            if (_nwe_id < 0) return;

            foreach (GeometricRelationship gr in this.R2GInstances)
            {
                if (gr.InstanceNWElementID != _nwe_id)
                    continue;

                gr.InstanceSize = new List<double> { _min_h, _min_b, _min_L, _max_h, _max_b, _max_L }; // 1.
                break;
            }

            // 2. re-calculate the cumulative values
            this.UpdateCumulativeValuesFromInstances();
        }

        /// <summary>
        /// <para>Transfer BOTH the size AND the SIZE TRANSFER SETTINGS of the component instance bound in a network element.</para>
        /// </summary>
        /// <param name="_nwe_id"></param>
        /// <param name="_current_size"></param>
        /// <param name="_size_settings"></param>
        internal void TransferSizeSettings(long _nwe_id, List<double> _current_size, List<GeomSizeTransferDef> _size_settings)
        {
            if (_nwe_id < 0) return;
            if (_current_size == null || _size_settings == null) return;

            int nrEntries = _current_size.Count;
            if (nrEntries < 6 || nrEntries != _size_settings.Count) return;

            foreach(GeometricRelationship gr in this.R2GInstances)
            {
                if (gr.InstanceNWElementID != _nwe_id)
                    continue;

                gr.InstanceSize = new List<double>(_current_size); // 1.
                gr.InstanceSizeTransferSettings = new List<GeomSizeTransferDef>(_size_settings); // 2. (applies the settings as well)
                break;
            }

            // 3. re-calculate the cumulative values
            this.UpdateCumulativeValuesFromInstances();
        }

        internal string GetMinSizeOfInstanceIn(long _nwe_id)
        {
            if (_nwe_id < 0)
                return "[]";

            string size_info = "[]";
            foreach (GeometricRelationship gr in this.R2GInstances)
            {
                if (gr.InstanceNWElementID != _nwe_id)
                    continue;

                return gr.GetSizeInfoString(false);
            }

            return size_info;
        }

        internal string GetMaxSizeOfInstanceIn(long _nwe_id)
        {
            if (_nwe_id < 0)
                return "[]";

            string size_info = "[]";
            foreach (GeometricRelationship gr in this.R2GInstances)
            {
                if (gr.InstanceNWElementID != _nwe_id)
                    continue;

                return gr.GetSizeInfoString(true);
            }

            return size_info;
        }

        protected void UpdateCumulativeValuesFromInstances()
        {
            Parameter.Parameter p_L_min_total = this.GetFirstParamByName(Parameter.Parameter.RP_LENGTH_MIN_TOTAL);
            Parameter.Parameter p_L_max_total = this.GetFirstParamByName(Parameter.Parameter.RP_LENGTH_MAX_TOTAL);
            Parameter.Parameter p_A_min_total = this.GetFirstParamByName(Parameter.Parameter.RP_AREA_MIN_TOTAL);
            Parameter.Parameter p_A_max_total = this.GetFirstParamByName(Parameter.Parameter.RP_AREA_MAX_TOTAL);
            Parameter.Parameter p_V_min_total = this.GetFirstParamByName(Parameter.Parameter.RP_VOLUME_MIN_TOTAL);
            Parameter.Parameter p_V_max_total = this.GetFirstParamByName(Parameter.Parameter.RP_VOLUME_MAX_TOTAL);
            Parameter.Parameter p_Count_total = this.GetFirstParamByName(Parameter.Parameter.RP_COUNT);

            double p_L_min_total_value = 0;
            double p_L_max_total_value = 0;
            double p_A_min_total_value = 0;
            double p_A_max_total_value = 0;
            double p_V_min_total_value = 0;
            double p_V_max_total_value = 0;

            foreach (GeometricRelationship gr in this.R2GInstances)
            {
                if (gr.InstanceSize == null || gr.InstanceSize.Count < 6) continue;
                if (gr.InstanceSize[0] == 0) continue;

                if (p_L_min_total != null)
                    p_L_min_total_value += gr.InstanceSize[2];
                if (p_L_max_total != null)
                    p_L_max_total_value += gr.InstanceSize[5];

                if (p_A_min_total != null)
                    p_A_min_total_value += gr.InstanceSize[0] * gr.InstanceSize[1];
                if (p_A_max_total != null)
                    p_A_max_total_value += gr.InstanceSize[3] * gr.InstanceSize[4];

                if (p_V_min_total != null)
                    p_V_min_total_value += gr.InstanceSize[0] * gr.InstanceSize[1] * gr.InstanceSize[2];
                if (p_V_max_total != null)
                    p_V_max_total_value += gr.InstanceSize[3] * gr.InstanceSize[4] * gr.InstanceSize[5];
            }

            if (p_L_min_total != null)
                p_L_min_total.ValueCurrent = p_L_min_total_value;
            if (p_L_max_total != null)
                p_L_max_total.ValueCurrent = p_L_max_total_value;

            if (p_A_min_total != null)
                p_A_min_total.ValueCurrent = p_A_min_total_value;
            if (p_A_max_total != null)
                p_A_max_total.ValueCurrent = p_A_max_total_value;

            if (p_V_min_total != null)
                p_V_min_total.ValueCurrent = p_V_min_total_value;
            if (p_V_max_total != null)
                p_V_max_total.ValueCurrent = p_V_max_total_value;

            if (p_Count_total != null)
                p_Count_total.ValueCurrent = this.R2GInstances.Count;
            
        }

        #endregion
        
        #region METHODS: instancing in NW elements MANAGEMENT

        /// <summary>
        /// Called when placing a component in a NW element. It creates a new instance of the component.
        /// </summary>
        /// <param name="_container"></param>
        internal void CreateInstance(FlNetElement _container)
        {
            if (_container == null) return;

            // check for the proper type of geometric relationship
            if (this.R2GMainState.Type != Relation2GeomType.CONTAINED_IN && this.R2GMainState.Type != Relation2GeomType.CONNECTS) return;

            // check if the user has writing access
            if (this.Factory != null)
            {
                bool success = this.RecordWritingAccess(this.Factory.Caller);
                if (!success) return;
            }
            else
                return;

            // check for duplicates
            foreach (GeometricRelationship gr in this.R2GInstances)
            {
                if (gr.InstanceNWElementID == _container.ID)
                    return;
            }

            // check for previous instancing:
            bool first_instancing = (this.R2GInstances.Count == 1 && this.R2GInstances[0].InstanceNWElementID < 0);

            // add an automatically generated sub-component containing the size parameters
            // add an automatically generated sub-component containing the cumulative parameters
            if (!this.ContainsValidAutoSubcomponentForSize())
                this.AutoAddSubcomponentForSize();
            if (!this.ContainsValidAutoSubcomponentForCumulation())                
                this.AutoAddSubcomponentForCumulation();

            // assemble parameters to pass to the instance
            Dictionary<string, double> param_slots = this.ExtractParameterValues();

            // if there has not been any assignment so far, use the first and only GeometricRelationship
            if (first_instancing)
                this.R2GInstances[0].PlaceInFlowNetworkElement(_container, new Point(0, 0), Component.SCALE_PIXEL_TO_M, new List<double> { 0, 0, 0, 0, 0, 0 }, param_slots);
            else
                this.R2GInstances.Add(GeometricRelationship.CreateAndPlaceInFlowNetworkElement(_container, new Point(0, 0), Component.SCALE_PIXEL_TO_M, param_slots));

            this.UpdateCumulativeValuesFromInstances(); // for the instance counter
        }

        internal void RemoveInstance(FlNetElement _container)
        {
            if (_container == null) return;

            // check for the proper type of geometric relationship
            if (this.R2GMainState.Type != Relation2GeomType.CONTAINED_IN && this.R2GMainState.Type != Relation2GeomType.CONNECTS) return;

            // check if the user has writing access
            ComponentManagerType user = ComponentManagerType.ADMINISTRATOR;
            if (this.Factory != null)
            {
                bool success = this.RecordWritingAccess(this.Factory.Caller);
                user = this.Factory.Caller;
                if (!success) return;
            }
            else
                return;

            // if there is only one geometric relationship -> reset it
            if (this.R2GInstances.Count == 1 && this.R2GInstances[0].InstanceNWElementID == _container.ID)
            {
                this.R2GInstances[0].Reset();
                return;
            }

            // otherwise, just remove
            List<bool> to_retain = new List<bool>();
            for (int i = 0; i < this.R2GInstances.Count; i++)
            {
                if (this.R2GInstances[i].InstanceNWElementID == _container.ID)
                {
                    to_retain.Add(false);
                    this.RemovePlacementBasedReferencesToInstanceFromOtherComponents(this.R2GInstances[i]);
                }                   
                else
                    to_retain.Add(true);
            }

            // includes handling the case of the first (main) relationship being removed
            this.UpdateGeometricRelationships(to_retain, null, user);
            // re-calculate the cumulative values
            this.UpdateCumulativeValuesFromInstances();
        }

        private void RemovePlacementBasedReferencesToInstanceFromOtherComponents(GeometricRelationship _gr)
        {
            if (this.ReferencedBy == null || this.ReferencedBy.Count == 0) return;

            // check if _gr is the only placement in the geometry with id _gr.GeomIDs.X
            List<GeometricRelationship> other_placements = this.R2GInstances.FindAll(x => x.ID != _gr.ID && x.State.Type == _gr.State.Type && x.GeomIDs.X == _gr.GeomIDs.X).ToList();
            if (other_placements == null && other_placements.Count > 0) return;

            List<Component> holding_placement_based_refs = this.ReferencedBy.Where(x => x.R2GInstances[0].State.Type == Relation2GeomType.DESCRIBES && x.R2GInstances[0].GeomIDs.X == _gr.GeomIDs.X).ToList();
            if (holding_placement_based_refs == null || holding_placement_based_refs.Count == 0) return;

            foreach(Component c in holding_placement_based_refs)
            {
                c.RemoveReferencedComponent_Level0(this, true, ComponentManagerType.ADMINISTRATOR);
            }
        }

        internal GeometricRelationship UpdateInstanceToContainerBinding(long _container_id, long _container_new_id, string _container_new_name)
        {
            GeometricRelationship gr = this.R2GInstances.FirstOrDefault(x => x.InstanceNWElementID == _container_id);
            if (gr == null) return null;

            gr.UpdateContainerInfo(_container_new_id, _container_new_name);
            return gr;
        }

        internal GeometricRelationship UpdateInstanceConnectivityInfo(FlNetEdge _edge)
        {
            if (_edge == null) return null;
            GeometricRelationship gr = this.R2GInstances.FirstOrDefault(x => x.InstanceNWElementID == _edge.ID);
            if (gr == null) return null;

            if (_edge.Start != null && _edge.End != null)
                gr.InstancePath[0] = new System.Windows.Media.Media3D.Point3D(_edge.Start.ID, _edge.End.ID, -1);

            return gr;
        }

        internal GeometricRelationship UpdateInstanceIn(FlNetElement _container, Point _offset, bool _reset)
        {
            if (_container == null) return null;

            GeometricRelationship gr = this.R2GInstances.FirstOrDefault(x => x.InstanceNWElementID == _container.ID);
            if (gr == null) return null;
            if (gr.InstanceNWElementID != _container.ID) return null;

            // assemble parameters to pass to the instance
            Dictionary<string, double> param_slots = this.ExtractParameterValues();
            // size is a special case !!!
            param_slots[Parameter.Parameter.RP_HEIGHT_MIN] = gr.InstanceSize[0];
            param_slots[Parameter.Parameter.RP_WIDTH_MIN] = gr.InstanceSize[1];
            param_slots[Parameter.Parameter.RP_LENGTH_MIN] = gr.InstanceSize[2];
            param_slots[Parameter.Parameter.RP_HEIGHT_MAX] = gr.InstanceSize[3];
            param_slots[Parameter.Parameter.RP_WIDTH_MAX] = gr.InstanceSize[4];
            param_slots[Parameter.Parameter.RP_LENGTH_MAX] = gr.InstanceSize[5];

            // controlled reset
            if (gr.InstanceParamValues == null)
            {
                gr.InstanceParamValues = param_slots;
                return gr;
            }

            if (_reset)
            {
                gr.InstanceParamValues = param_slots;
                gr.UpdatePositionFrom(_container, _offset, Component.SCALE_PIXEL_TO_M);
                return gr;
            }
            else
            {
                foreach (var entry in param_slots)
                {
                    if (!(gr.InstanceParamValues.ContainsKey(entry.Key)))
                        gr.InstanceParamValues.Add(entry.Key, entry.Value);
                }
                gr.ApplySizeTransferSettings();
            }

            return gr;

        }

        internal void UpdateInstancePositionIn(FlNetElement _container, Point _offset)
        {
            if (_container == null) return;

            GeometricRelationship gr = this.R2GInstances.FirstOrDefault(x => x.InstanceNWElementID == _container.ID);
            if (gr == null) return;
            if (gr.InstanceNWElementID != _container.ID) return;

            gr.UpdatePositionFrom(_container, new Point(0,0), Component.SCALE_PIXEL_TO_M);
        }

        #endregion

        #region METHODS: instancing in NE elements PARAMETER and CALCULATION transfer

        /// <summary>
        /// For calculations within a network involving the size of the instance.
        /// </summary>
        private void AddSizeParamsForNWCalculations()
        {
            if (this.ContainedParameters == null) return;

            // create the size parameters
            List<Parameter.Parameter> parameters = Parameter.Parameter.GetSizeParametersForInstancing();
            this.AddParametersFromList(parameters);

            // create the corresponding calculations
            ObservableConcurrentDictionary<string, Parameter.Parameter> p_in;
            ObservableConcurrentDictionary<string, Parameter.Parameter> p_out;
            Calculation calc;
            CalculationState calc_state = CalculationState.VALID;

            p_in = new ObservableConcurrentDictionary<string, Parameter.Parameter> { { "x2", parameters[3] }, { "x3", parameters[4] } };
            p_out = new ObservableConcurrentDictionary<string, Parameter.Parameter> { { "out01", parameters[7] } };
            calc = new Calculation("x2*x3", "Amax-calc", p_in, p_out);
            this.TestAndSaveCalculationInEditMode(ComponentManagerType.ADMINISTRATOR, calc, ref calc_state);
            if (calc_state != CalculationState.VALID)
            {
                MessageBox.Show(CalculationFactory.CalcStateToStringDE(calc_state),
                    "Fehler bei der automatisierten Gleichungserstellung", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            calc_state = CalculationState.VALID;
            p_in = new ObservableConcurrentDictionary<string, Parameter.Parameter> { { "x2", parameters[0] }, { "x3", parameters[1] } };
            p_out = new ObservableConcurrentDictionary<string, Parameter.Parameter> { { "out01", parameters[6] } };
            calc = new Calculation("x2*x3", "Amin-calc", p_in, p_out);
            this.TestAndSaveCalculationInEditMode(ComponentManagerType.ADMINISTRATOR, calc, ref calc_state);
            if (calc_state != CalculationState.VALID)
            {
                MessageBox.Show(CalculationFactory.CalcStateToStringDE(calc_state),
                    "Fehler bei der automatisierten Gleichungserstellung", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            calc_state = CalculationState.VALID;
            p_in = new ObservableConcurrentDictionary<string, Parameter.Parameter> { { "x2", parameters[7] }, { "x3", parameters[5] } };
            p_out = new ObservableConcurrentDictionary<string, Parameter.Parameter> { { "out01", parameters[9] } };
            calc = new Calculation("x2*x3", "Vmax-calc", p_in, p_out);
            this.TestAndSaveCalculationInEditMode(ComponentManagerType.ADMINISTRATOR, calc, ref calc_state);
            if (calc_state != CalculationState.VALID)
            {
                MessageBox.Show(CalculationFactory.CalcStateToStringDE(calc_state),
                    "Fehler bei der automatisierten Gleichungserstellung", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            calc_state = CalculationState.VALID;
            p_in = new ObservableConcurrentDictionary<string, Parameter.Parameter> { { "x2", parameters[6] }, { "x3", parameters[2] } };
            p_out = new ObservableConcurrentDictionary<string, Parameter.Parameter> { { "out01", parameters[8] } };
            calc = new Calculation("x2*x3", "Vmin-calc", p_in, p_out);
            this.TestAndSaveCalculationInEditMode(ComponentManagerType.ADMINISTRATOR, calc, ref calc_state);
            if (calc_state != CalculationState.VALID)
            {
                MessageBox.Show(CalculationFactory.CalcStateToStringDE(calc_state),
                    "Fehler bei der automatisierten Gleichungserstellung", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// For calculations within a network involving the cumulated size of all instances of this component.
        /// </summary>
        private void AddCumulativeParamsForNWCalculations()
        {
            if (this.ContainedParameters == null) return;

            // create the cumulative parameters
            List<Parameter.Parameter> parameters = Parameter.Parameter.GetCumulativeParametersForInstancing();
            this.AddParametersFromList(parameters);
        }

        /// <summary>
        /// Adds copies of the parameters in the list to the parameters contained in this component.
        /// </summary>
        /// <param name="_parameters"></param>
        private void AddParametersFromList(List<Parameter.Parameter> _parameters)
        {
            if (_parameters == null) return;
            if (_parameters.Count == 0) return;

            foreach (Parameter.Parameter p in _parameters)
            {
                if (this.ContainedParameters.ContainsKey(p.ID)) continue;
                Parameter.Parameter p_dupl = this.ContainedParameters.FirstOrDefault(x => x.Value.Name == p.Name && x.Value.Unit == p.Unit && x.Value.Propagation == p.Propagation).Value;
                if (p_dupl != null) continue; // if this method was called at least once before

                p.PropertyChanged += param_PropertyChanged;
                this.ContainedParameters.Add(p.ID, p);
                this.Category |= p.Category;
            }
        }

        /// <summary>
        /// <para>Returns the value, as a formatted string, in the parameter slot of the instance placed in the NW element.</para>
        /// <para>The parameter can be contained in this component or ANY of its sub-components.</para>
        /// </summary>
        /// <param name="_container"></param>
        /// <param name="_param_suffix"></param>
        /// <returns></returns>
        internal string GetParamValueOfInstance(FlNetElement _container, string _param_suffix)
        {
            if (_container == null) return string.Empty;
            if (string.IsNullOrEmpty(_param_suffix)) return string.Empty;

            Parameter.Parameter p = this.GetFirstParamBySuffix(_param_suffix);
            if (p == null) return string.Empty;

            string info = string.Empty;
            foreach (GeometricRelationship gr in this.R2GInstances)
            {
                if (gr.InstanceNWElementID != _container.ID)
                    continue;

                if (gr.InstanceParamValues == null || gr.InstanceParamValues.Count == 0)
                {
                    this.UpdateInstanceIn(_container, new Point(0,0), false);
                }

                if (gr.InstanceParamValues.ContainsKey(p.Name))
                    info = Parameter.Parameter.ValueToString(gr.InstanceParamValues[p.Name], "F2");

                return info;
            }

            return info;
        }

        /// <summary>
        /// <para>Recursive extraction of all parameters, including all sub-components.</para>
        /// <para>Parameters with Propagation CALC_IN (calculated from NW) are not included.</para>
        /// <para>They are used for cumulative values including all instances of the component.</para>
        /// </summary>
        /// <returns></returns>
        protected Dictionary<string, double> ExtractParameterValues()
        {
            Dictionary<string, double> param_slots = new Dictionary<string, double>();
            Dictionary<long, Parameter.Parameter> flat_p_list = this.GetFlatParamsList();
            foreach(var entry in flat_p_list)
            {
                Parameter.Parameter p = entry.Value;
                if (p == null) continue;
                if (p.Propagation == InfoFlow.CALC_IN) continue;

                if (param_slots.ContainsKey(p.Name)) continue;
                param_slots.Add(p.Name, p.ValueCurrent);
            }

            return param_slots;
        }

        /// <summary>
        /// Not implemented yet...
        /// </summary>
        /// <param name="_param_ids"></param>
        /// <returns></returns>
        internal Dictionary<string, double> ExtractParameterValuesOf(List<long> _param_ids)
        {
            return new Dictionary<string, double>();
        }

        /// <summary>
        /// Extracts the setting for each parameter - to be displayed in the instance or not.
        /// </summary>
        /// <returns></returns>
        protected Dictionary<string, bool> ExtractParamDisplayInInstance()
        {
            Dictionary<string, bool> param_disp = new Dictionary<string, bool>();
            Dictionary<long, Parameter.Parameter> flat_p_list = this.GetFlatParamsList();
            foreach (var entry in flat_p_list)
            {
                Parameter.Parameter p = entry.Value;
                if (p == null) continue;
                if (p.Propagation == InfoFlow.CALC_IN) continue;

                if (param_disp.ContainsKey(p.Name)) continue;
                param_disp.Add(p.Name, p.ShowInCompInstDisplay);
            }

            return param_disp;
        }

        /// <summary>
        /// <para>For calculating with the parameter slots in instances w/o affecting the component and its parameters.</para>
        /// <para>Performed recursively in case the sub-components contain calculations themselves.</para>
        /// </summary>
        /// <param name="_container"></param>
        internal void ExecuteCalculationChainWoArtefacts(FlNetElement _container)
        {
            if (_container == null) return;
            GeometricRelationship instance = this.R2GInstances.FirstOrDefault(x => x.InstanceNWElementID == _container.ID);
            if (instance == null) return;
            
            // populate the parameter values
            if (instance.InstanceParamValues == null || instance.InstanceParamValues.Count == 0)
                this.UpdateInstanceIn(_container, new Point(0,0), false);
            
            // recursion
            this.ExecuteCalculationChainForInstance(instance);
            
            // result transfer params -> size, if necessary
            instance.ApplySizeTransferSettings();
        }

        /// <summary>
        /// Executes calculations in all subcomponents and saves the result in the input instance. Uses depth-first sub-component search.
        /// </summary>
        /// <param name="_instance"></param>
        protected void ExecuteCalculationChainForInstance(GeometricRelationship _instance)
        {
            foreach (var entry in this.ContainedComponents)
            {
                Component subC = entry.Value;
                if (subC == null) continue;

                subC.ExecuteCalculationChainForInstance(_instance);
            }

            foreach (Calculation c in this.ContainedCalculations)
            {
                Dictionary<string, double> instance_param_values = _instance.InstanceParamValues;
                c.PerformCalculationWoArtefacts(ref instance_param_values);
                _instance.InstanceParamValues = new Dictionary<string, double>(instance_param_values);
            }
        }

        #endregion

    }
}
