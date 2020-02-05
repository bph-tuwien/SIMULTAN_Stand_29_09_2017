using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Input;

using Sprache;
using Sprache.Calc;

using ParameterStructure.Parameter;
using ParameterStructure.DXF;
using ParameterStructure.Geometry;
using ParameterStructure.Utils;

namespace ParameterStructure.Component
{
    #region ENUMS

    // [0    - 1000]: DXF Specs and general custom codes
    // [1001 - 1100]: Parameter
    // [1101 - 1300]: MultiValue
    // [1301 - 1400]: Calculation
    // [1401 - 1500]: Component
    //      [[1421 - 1430]]: Component -> AccessTracker
    //      [[1431 - 1440]]: Component -> AccessProfile
    // [1501 - 1600]: FlowNetwork
    // [1601 - 1700]: GeometicRelationship
    // [1701 - 1800]: Mapping2Component
    public enum ComponentSaveCode
    {
        NAME = 1401,
        DESCRIPTION = 1402,
        CATEGORY = 1403,                    // as string
        ACCESS_RECORD = 1404,               // NOT USED -> saved as DXF Entities within a DXF entitiy
        FUNCTION_SLOTS_ALL = 1405,          // as a sequence of strings
        FUNCTION_SLOT_CURRENT = 1406,

        CONTAINED_COMPONENTS = 1407,        // saved as DXF Entities
        CONTAINED_COMPONENT_SLOTS = 1408,   // saved as a sequence of strings
        REFERENCED_COMPONENTS = 1409,       // saved as pairs of STRING and LONG
        CONTAINED_PARAMETERS = 1410,        // saved as DXF Entities
        CONTAINED_CALCULATIONS = 1411,      // saved as DXF Entities

        TIME_STAMP = 1412,
        SYMBOL_ID = 1413,
        RELATIONSHIPS_TO_GEOMETRY = 1414,   // saved as DXF Entities
        GENERATED_AUTOMATICALLY = 1415,     // from exchange with the GeometryViewer
        MAPPINGS_TO_COMPONENTS = 1416,      // for deferred calculations
    }

    #endregion
    public partial class Component : DisplayableProductDefinition
    {
        #region STATIC

        internal static long NR_COMPONENTS = 0;
        protected static XtensibleCalculator CALCULATOR = new XtensibleCalculator();
        private const string CALC_CANDIDATE_NAME = "Gleichung";
        private const string CALC_CANDIDATE_EXPR = "x";

        internal const string FLNET_COMP_PARAM_NAME_MS = "MStrom";
        internal const string FLNET_COMP_PARAM_NAME_ES = "EStrom";
        internal const string FLNET_COMP_PARAM_NAME_IS = "IStrom";

        protected static int MAX_DISPLAYABLE_NR_PARAMS = 64;

        protected static string ERR_MESSAGES = string.Empty;

        private static int CompareBySlotName(Component _c1, Component _c2)
        {
            if (_c1 == null && _c2 == null) return 0;
            if (_c1 != null && _c2 == null) return 1;
            if (_c1 == null && _c2 != null) return -1;

            string slot_name_1 = _c1.CurrentSlot;
            string slot_name_2 = _c2.CurrentSlot;

            if (string.IsNullOrEmpty(slot_name_1) && string.IsNullOrEmpty(slot_name_2)) return 0;
            if (!string.IsNullOrEmpty(slot_name_1) && string.IsNullOrEmpty(slot_name_2)) return 1;
            if (string.IsNullOrEmpty(slot_name_1) && !string.IsNullOrEmpty(slot_name_2)) return -1;

            return slot_name_1.CompareTo(slot_name_2);
        }

        #endregion

        #region PROPERTIES: Management (Category, AccessLocal, Slots)

        protected Category category;
        public Category Category
        {
            get { return this.category; }
            protected set
            {
                this.category = value;
                this.RegisterPropertyChanged("Category");
            }
        }
        
        private ComponentAccessProfile access_local;
        public ComponentAccessProfile AccessLocal
        {
            get { return this.access_local; }
            protected set 
            {
                if (this.access_local != null)
                    this.access_local.PropertyChanged -= access_local_PropertyChanged;
                this.access_local = value;
                if (this.access_local != null)
                    this.access_local.PropertyChanged += access_local_PropertyChanged;
                this.RegisterPropertyChanged("AccessLocal");
            }
        }

        protected void access_local_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ComponentAccessProfile cap = sender as ComponentAccessProfile;
            if (cap == null || e == null) return;

            if (e.PropertyName == "ProfileState")
                this.RegisterPropertyChanged("AccessLocal");

        }

        // sets the default access only after adding the FIRST SLOT
        private List<string> fits_in_slots;
        public List<string> FitsInSlots
        {
            get { return this.fits_in_slots; }
            set 
            { 
                this.fits_in_slots = value;
                if (this.fits_in_slots != null && this.fits_in_slots.Count > 0)
                {
                    if (this.IsMarkable) // added 31.08.2016
                        this.CurrentSlot = this.fits_in_slots[0];
                    Dictionary<ComponentManagerType, ComponentAccessTracker> profile = ComponentUtils.GetStandardProfile(this.fits_in_slots[0]);
                    ComponentManagerType current_caller = (this.Factory == null) ? this.GetManagerWWritingAccess() : this.Factory.Caller;
                    this.AccessLocal = new ComponentAccessProfile(profile, current_caller);
                }
                this.RegisterPropertyChanged("FitsInSlots");
            }
        }

        // only for SUBCOMPONENTS, because referenced components can be in multiple slots
        private string current_slot;
        public string CurrentSlot
        {
            get { return this.current_slot; }
            set
            { 
                this.current_slot = value;
                if (this.R2GInstances != null && this.R2GInstances.Count > 0 && this.R2GMainState.Type == Relation2GeomType.NONE)
                {
                    this.R2GInstances[0].State = new Relation2GeomState { IsRealized = false, Type = ComponentUtils.SlotToRelation2GeomType(this.current_slot) };
                }
                this.RegisterPropertyChanged("CurrentSlot");
            }
        }

        internal void SetCurrentSlotWoSideEffects(string _slot)
        {
            this.current_slot = _slot;
        }

        #endregion

        #region PROPERTIES: Contained Elements (ContainedComponents, ReferencedComponents, ReferencedBy)

        // SUB-COMPONENTS
        protected ObservableConcurrentDictionary<string, Component> contained_components;
        public ObservableConcurrentDictionary<string, Component> ContainedComponents
        {
            get { return this.contained_components; }
            protected set 
            {
                if (this.contained_components != null)
                    this.contained_components.CollectionChanged -= contained_components_CC;
                this.contained_components = value;
                if (this.contained_components != null)
                    this.contained_components.CollectionChanged += contained_components_CC;
                this.RegisterPropertyChanged("ContainedComponents");
            }
        }

        private void contained_components_CC(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.RegisterPropertyChanged("ContainedComponents");
            this.UpdateChildrenContainer();
            this.UpdateParamChildrenContainer();
            this.UpdateCalcChildrenContainer();
        }

        // REFERENCED COMPONENTS
        private ObservableConcurrentDictionary<string, Component> referenced_components;
        public ObservableConcurrentDictionary<string, Component> ReferencedComponents
        {
            get { return this.referenced_components; }
            protected set 
            {
                if (this.referenced_components != null)
                {
                    this.referenced_components.CollectionChanged -= referenced_components_CC;
                    foreach(var entry in this.referenced_components)
                    {
                        Component c = entry.Value;
                        if (c == null) continue;
                        c.ReferencedBy.Remove(this);
                    }
                }

                this.referenced_components = value;

                if (this.referenced_components != null)
                {
                    this.referenced_components.CollectionChanged += referenced_components_CC;
                    foreach (var entry in this.referenced_components)
                    {
                        Component c = entry.Value;
                        if (c == null) continue;
                        c.ReferencedBy.Add(this);
                    }
                }

                this.RegisterPropertyChanged("ReferencedComponents");
            }
        }

        private void referenced_components_CC(object sender, NotifyCollectionChangedEventArgs e)
        {
            IDictionary<string, Component> collection = sender as IDictionary<string, Component>;
            if (collection == null) return;

            if (e != null)
            {
                switch(e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach(var element in e.NewItems)
                        {
                            if (element is KeyValuePair<string, Component>)
                            {
                                KeyValuePair<string, Component> entry = (KeyValuePair<string, Component>)element;
                                Component comp = entry.Value;
                                if (comp != null)
                                    comp.ReferencedBy.Add(this);
                            }
                        }
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        foreach (var element in e.OldItems)
                        {
                            if (element is KeyValuePair<string, Component>)
                            {
                                KeyValuePair<string, Component> entry = (KeyValuePair<string, Component>)element;
                                Component comp = entry.Value;
                                if (comp != null)
                                    comp.ReferencedBy.Remove(this);
                            }
                        }
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        break;
                }
            }
            this.RegisterPropertyChanged("ReferencedComponents");
            this.UpdateChildrenContainer();
        }

        // derived: list of Components that REFERENCE THIS ONE
        private List<Component> referenced_by;
        public List<Component> ReferencedBy
        {
            get { return this.referenced_by; }
            protected set 
            { 
                this.referenced_by = value;
                this.RegisterPropertyChanged("ReferencedBy");
            }
        }

        #endregion

        #region PROPERTIES: Contained Elements (ContainedParameters, ContainedCalculations)

        // PARAMETERS
        protected ObservableConcurrentDictionary<long, Parameter.Parameter> contained_parameters;
        public ObservableConcurrentDictionary<long, Parameter.Parameter> ContainedParameters
        {
            get { return this.contained_parameters; }
            protected set 
            {
                if (this.contained_parameters != null)
                    this.contained_parameters.CollectionChanged -= contained_parameters_CC;
                this.contained_parameters = value;
                if (this.contained_parameters != null)
                    this.contained_parameters.CollectionChanged += contained_parameters_CC;
                this.RegisterPropertyChanged("ContainedParameters");
            }
        }

        private void contained_parameters_CC(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.RegisterPropertyChanged("ContainedParameters");
            this.RegisterPropertyChanged("NrParameters");
            this.UpdateChildrenContainer();
            this.UpdateParamChildrenContainer();
            this.UpdateCalcChildrenContainer();
        }

        // derived
        public int NrParameters { get { return this.contained_parameters.Count(); } }

        // helper (only when creating a component by copying another)
        // a record of all parameters in all subcomponents and in this: id of original, id of copied parameter
        private Dictionary<long, Parameter.Parameter> contained_parameters_copy_record;

        // CALCULATIONS
        // the order of calculations corresponds to the sequence in which they have to be performed
        private List<Calculation> contained_calculations;
        public List<Calculation> ContainedCalculations
        {
            get { return this.contained_calculations; }
            protected set 
            {
                this.contained_calculations = value;
                if (this.contained_calculations != null && this.contained_calculations.Count > 0)
                    this.HasCalculations = true;
                else
                    this.HasCalculations = false;
                this.RegisterPropertyChanged("ContainedCalculations");
                this.UpdateChildrenContainer();
                this.UpdateCalcChildrenContainer();
            }
        }

        // holds a calculation that is currently being edited
        private Calculation calc_in_edit_mode;
        public Calculation CalcInEditMode
        {
            get { return this.calc_in_edit_mode; }
            protected set 
            {
                this.calc_in_edit_mode = value;
                this.RegisterPropertyChanged("CalcInEditMode");
            }
        }

        // derived
        private bool has_calculations;
        public bool HasCalculations
        {
            get { return this.has_calculations; }
            protected set
            { 
                this.has_calculations = value;
                this.RegisterPropertyChanged("HasCalculations");
            }
        }

        // derived
        protected List<long> params_involved_in_calculations;

        #endregion

        #region PROPERTIES (derived): ALL Contained Elements as Children

        // ALL CONTAINED ELEMENTS AS CHILDREN
        private CompositeCollection children;
        public CompositeCollection Children { get { return this.children; } }

        public void UpdateChildrenContainer()
        {
            // mark the change
            this.TimeStamp = DateTime.Now;
            // extract the sub-component slots
            if (!this.ApplyIsExcludedFromDisplay || !this.IsExcludedFromDisplay) // added 21.08.2017
            {
                List<Component> slots_COPY_full = new List<Component>();
                List<EmptyComponentSlot> slots_COPY_empty = new List<EmptyComponentSlot>();
                foreach (var entry in this.ContainedComponents)
                {
                    if (entry.Value == null)
                        slots_COPY_empty.Add(new EmptyComponentSlot { SlotType = 0, SlotName = entry.Key, IsLarge = false });
                    else
                    {
                        Component comp = entry.Value;
                        comp.CurrentSlot = entry.Key;
                        if (!comp.ApplyIsExcludedFromDisplay || !comp.IsExcludedFromDisplay)
                            slots_COPY_full.Add(comp);
                    }
                }
                slots_COPY_full.Sort(Component.CompareBySlotName);
                slots_COPY_empty.Sort(EmptyComponentSlot.CompareByName);

                // extract the slots of the referenced components
                List<EmptyComponentSlot> slot_REF_empty = new List<EmptyComponentSlot>();
                foreach (var entry in this.ReferencedComponents)
                {
                    string descr = (entry.Value == null) ? string.Empty : entry.Value.Name + ": " + entry.Value.Description;
                    slot_REF_empty.Add(new EmptyComponentSlot { SlotType = 1, SlotName = entry.Key, SlotDescr = descr, IsLarge = false });
                }
                slot_REF_empty.Sort(EmptyComponentSlot.CompareByName);

                if (this.HideDetails || this.IsHidden)
                {
                    this.children = new CompositeCollection
                    {
                        new CollectionContainer { Collection = slots_COPY_full },
                        new CollectionContainer { Collection = slots_COPY_empty },
                        new CollectionContainer { Collection = slot_REF_empty }
                    };
                }
                else
                {
                    List<Parameter.Parameter> params_sorted = new List<Parameter.Parameter>(this.ContainedParameters.Values);
                    params_sorted.Sort(Parameter.Parameter.CompareByName);

                    this.children = new CompositeCollection
                    {
                        new CollectionContainer { Collection = params_sorted },
                        new CollectionContainer { Collection = this.ContainedCalculations},
                        new CollectionContainer { Collection = slots_COPY_full },
                        new CollectionContainer { Collection = slots_COPY_empty },
                        new CollectionContainer { Collection = slot_REF_empty }
                    };
                }
                this.NrActiveChildren = this.ContainedParameters.Count() + this.ContainedCalculations.Count + slots_COPY_full.Count;
            }
            else
            {
                this.children = new CompositeCollection();
                this.NrActiveChildren = 0;
            }
            
            this.RegisterPropertyChanged("Children");
            this.RegisterPropertyChanged("NrActiveChildren");
        }

        public int NrActiveChildren { get; private set; }

        // to be called for filtering of parameters
        // it filters the calculations out in any case
        internal void AdjustParametersInChildrenContainer(Dictionary<InfoFlow, bool> _criteria, bool _include_calcs)
        {
            // recursion
            foreach (var entry in this.ContainedComponents)
            {
                if (entry.Value == null) continue;
                entry.Value.AdjustParametersInChildrenContainer(_criteria, _include_calcs);
            }

            if (this.HideDetails || this.IsHidden) return;
            if (this.Children == null || this.Children.Count == 0) return;

            // parameter filter
            CollectionContainer paramCol = this.Children[0] as CollectionContainer;
            if (paramCol != null)
            {
                List<Parameter.Parameter> params_filtered_sorted = new List<Parameter.Parameter>();
                foreach (var entry in this.ContainedParameters)
                {
                    if (_criteria[entry.Value.Propagation])
                        params_filtered_sorted.Add(entry.Value);
                }
                params_filtered_sorted.Sort(Parameter.Parameter.CompareByName);
                paramCol.Collection = params_filtered_sorted;
            }

            // calculation filter
            if (this.Children.Count > 1)
            {
                CollectionContainer calcCol = this.Children[1] as CollectionContainer;
                if (calcCol != null)
                {
                    if (_include_calcs)
                        calcCol.Collection = this.ContainedCalculations;
                    else
                        calcCol.Collection = new List<Calculation>();
                }
            } 
        }

        #endregion

        #region PROPERTIES (derived): ALL Contained Parameters as ParamChildren

        private CompositeCollection param_children;
        public CompositeCollection ParamChildren { get { return this.param_children; } }

        public void UpdateParamChildrenContainer()
        {
            // check, if we can still display the parameters : added 18.10.2016, updated 21.08.2017
            if ((this.GetNrOfFlatParams() > Component.MAX_DISPLAYABLE_NR_PARAMS) ||
                (this.ApplyIsExcludedFromDisplay && this.IsExcludedFromDisplay))
            {
                this.param_children = new CompositeCollection();
                this.RegisterPropertyChanged("ParamChildren");
                return;
            }

            // add own parameters, changed 01.09.2016
            List<Parameter.Parameter> params_own = this.GetOwnParamsFilteredByFactory(); // updated 31.08.2017
            params_own.Sort(Parameter.Parameter.CompareByName);
            this.param_children = new CompositeCollection
            {
                new CollectionContainer { Collection = params_own }
            };

            // extract the parameters from the sub-component slots
            // replaced 31.08.2016
            List<KeyValuePair<string, Component>> sorted_flat_sComps = this.GetSortedFlatSubCompWSlotNameList(string.Empty);
            foreach(var entry in sorted_flat_sComps)
            {
                if (entry.Value.ContainedParameters.Count() < 1) continue;

                List<EmptyComponentSlot> slot = new List<EmptyComponentSlot> { new EmptyComponentSlot { SlotType = 0, SlotName = entry.Key, IsLarge = true } };
                this.param_children.Add(new CollectionContainer { Collection = slot });
                List<Parameter.Parameter> params_sorted = entry.Value.GetOwnParamsFilteredByFactory(); // updated 31.08.2017
                params_sorted.Sort(Parameter.Parameter.CompareByName);
                this.param_children.Add(new CollectionContainer { Collection = params_sorted });
            }

            this.RegisterPropertyChanged("ParamChildren");
        }

        // DERIVED: for parameter selection in drop-down boxes etc.
        public CompositeCollection ParamChildrenNonAutomatic
        {
            get
            {
                if (this.IsAutomaticallyGenerated)
                    return new CompositeCollection();

                // own params
                List<Parameter.Parameter> params_own = new List<Parameter.Parameter>(this.ContainedParameters.Values);
                params_own.Sort(Parameter.Parameter.CompareByName);
                CompositeCollection p_children = new CompositeCollection
                {
                    new CollectionContainer { Collection = params_own }
                };

                // extract the parameters from the NON-AUTOMATICALLY-GENERATED sub-component slots
                List<KeyValuePair<string, Component>> sorted_flat_sComps = this.GetSortedFlatSubCompWSlotNameList(string.Empty);
                foreach (var entry in sorted_flat_sComps)
                {
                    if (entry.Value.IsAutomaticallyGenerated) continue;
                    if (entry.Value.ContainedParameters.Count() < 1) continue;

                    List<EmptyComponentSlot> slot = new List<EmptyComponentSlot> { new EmptyComponentSlot { SlotType = 0, SlotName = entry.Key, IsLarge = true } };
                    p_children.Add(new CollectionContainer { Collection = slot });
                    List<Parameter.Parameter> params_sorted = new List<Parameter.Parameter>(entry.Value.ContainedParameters.Values);
                    params_sorted.Sort(Parameter.Parameter.CompareByName);
                    p_children.Add(new CollectionContainer { Collection = params_sorted });
                }

                return p_children;
            }
        }

        public static List<string> GetUniqueParameterNamesFor(List<Component> _comps)
        {
            List<string> names = new List<string>();
            if (_comps == null || _comps.Count == 0)
                return names;

            foreach(Component c in _comps)
            {
                Dictionary<long, Parameter.Parameter> ps = c.GetFlatParamsList();
                foreach(var entry in ps)
                {
                    Parameter.Parameter p = entry.Value;
                    if (p != null && !names.Contains(p.Name))
                        names.Add(p.Name);
                }
            }
            return names;
        }

        #endregion

        #region PROPERTIES: (derived) ALL Contained Calculations as CalcChildren

        private CompositeCollection calc_children;
        public CompositeCollection CalcChildren { get { return this.calc_children; } }

        public void UpdateCalcChildrenContainer()
        {
            // check, if we can still display the parameters : added 18.10.2016, updated 21.08.2017
            if ((this.GetNrOfFlatParams() > Component.MAX_DISPLAYABLE_NR_PARAMS) ||
                (this.ApplyIsExcludedFromDisplay && this.IsExcludedFromDisplay))
            {
                this.calc_children = new CompositeCollection();
                this.RegisterPropertyChanged("CalcChildren");
                return;
            }

            // add own calculations
            this.calc_children = new CompositeCollection
            {
                new CollectionContainer { Collection = this.ContainedCalculations }
            };

            // extract the calculations from the sub-component slots
            // replaced 31.08.2016
            List<KeyValuePair<string, Component>> sorted_flat_sComps = this.GetSortedFlatSubCompWSlotNameList(string.Empty);
            foreach(var entry in sorted_flat_sComps)
            {
                if (entry.Value.ContainedCalculations.Count() < 1) continue;

                List<EmptyComponentSlot> slot = new List<EmptyComponentSlot> { new EmptyComponentSlot { SlotType = 0, SlotName = entry.Key, IsLarge = true } };
                this.calc_children.Add(new CollectionContainer { Collection = slot });
                this.calc_children.Add(new CollectionContainer { Collection = entry.Value.ContainedCalculations });

            }

            this.RegisterPropertyChanged("CalcChildren");
        }

        #endregion

        #region PROPERTIES (DO NOT SAVE): (overrides)

        public override bool ApplyIsExcludedFromDisplay
        {
            get{ return base.ApplyIsExcludedFromDisplay; }
            internal set
            {
                base.ApplyIsExcludedFromDisplay = value;
                // propagate
                foreach(var entry in this.ContainedComponents)
                {
                    if (entry.Value != null)
                        entry.Value.ApplyIsExcludedFromDisplay = this.ApplyIsExcludedFromDisplay;
                }               
            }
        }

        internal void UpdateChildrenContainersBottomToTop()
        {
            List<Component> list = this.GetFlatSubCompList();
            list.Reverse();

            foreach(Component sC in list)
            {
                sC.UpdateChildrenContainer();
                sC.UpdateParamChildrenContainer();
                sC.UpdateCalcChildrenContainer(); 
            }

            this.UpdateChildrenContainer();
            this.UpdateParamChildrenContainer();
            this.UpdateCalcChildrenContainer(); 
        }
        
        #endregion

        #region PROPERTIES (derived, implementation): Contained Elements of SAME TYPE

        internal override IReadOnlyList<DisplayableProductDefinition> ContainedOfSameType
        {
            get { return this.ContainedComponents.Values.Cast<DisplayableProductDefinition>().ToList().AsReadOnly(); }
        }

        #endregion

        #region PROPERTIES: Contained Geometric Relationships
        // added 15.11.2016

        // only geometry property that is displayed in the tree view
        private Relation2GeomState r2g_main_state;
        public Relation2GeomState R2GMainState
        {
            get { return this.r2g_main_state; }
            private set 
            {
                this.r2g_main_state = value;
                this.RegisterPropertyChanged("R2GMainState");
            }
        }

        // the relationship at index 0 gives the main type (see above)
        // DEFAULT: no GeometricRelationship (list with one entry of type Relation2GeomType.NONE)
        private List<GeometricRelationship> r2g_instances;
        public List<GeometricRelationship> R2GInstances
        {
            get { return this.r2g_instances; }
            private set 
            { 
                this.r2g_instances = value;
            }
        }

        public ICommand RotateR2GMainState { get; protected set; }

        #endregion

        #region PROPERTIES: Time Stamp

        private DateTime time_stamp;
        public DateTime TimeStamp
        {
            get { return this.time_stamp; }
            protected set
            {
                this.time_stamp = value;
                this.RegisterPropertyChanged("TimeStamp");
            }
        }

        #endregion

        #region PROPERTIES for Display (Symbol ID, Symbol Image)

        private long symbol_id;
        public long SymbolId
        {
            get { return this.symbol_id; }
            set
            { 
                this.symbol_id = value;
                this.RegisterPropertyChanged("SymbolId");
            }
        }

        // derived only for display : do not save
        private BitmapImage symbol_image;
        public BitmapImage SymbolImage
        {
            get { return this.symbol_image; }
            set 
            { 
                this.symbol_image = value;
                this.RegisterPropertyChanged("SymbolImage");
            }
        }

        #endregion

        #region PROPERTIES (DO NOT SAVE) for Display (has parameters outside of limits)

        protected bool has_all_params_within_limits;
        public bool HasAllParamsWithinLimits
        {
            get { return this.has_all_params_within_limits; }
            protected set
            {
                this.has_all_params_within_limits = value;
                this.RegisterPropertyChanged("HasAllParamsWithinLimits");
            }
        }

        #endregion

        #region PROPERTIES(DO NOT SAVE) for Display (IsSelected override)

        public override bool IsSelected
        {
            get { return this.isSelected; }
            set
            {
                this.isSelected = value;
                this.ParentIsSelected = this.isSelected;
                // communicate to subcomponents
                List<Component> sC = this.GetFlatSubCompList();
                foreach(Component c in sC)
                {
                    c.ParentIsSelected = this.isSelected;
                }
                foreach(var entry in this.ContainedParameters)
                {
                    Parameter.Parameter p = entry.Value;
                    if (p != null)
                        p.OwnerIsSelected = this.isSelected;
                }
                foreach(Calculation calc in this.ContainedCalculations)
                {
                    calc.OwnerIsSelected = this.isSelected;
                }
                // remove, if too slow ...
                foreach(var item in this.Children)
                {
                    CollectionContainer container = item as CollectionContainer;
                    if (container == null) continue;
                    foreach (var entry in container.Collection)
                    {
                        if (entry is EmptyComponentSlot)
                        {
                            EmptyComponentSlot slot = entry as EmptyComponentSlot;
                            slot.ParentIsSelected = this.isSelected;
                        }
                        else
                            break;
                    }
                }
                this.RegisterPropertyChanged("IsSelected");
            }
        }

        #endregion

        #region PROPERTIES(DO NOT SAVE): For Management Across All Components
        internal ComponentFactory Factory { get; set; }

        #endregion

        #region PROPERTIES (DO NOT SAVE): For interop w FlowNetworks

        protected FlNetElement binding_fne;
        public FlNetElement BindingFNE
        {
            get { return this.binding_fne; }
            internal set 
            {
                this.binding_fne = value;
                if (this.binding_fne == null)
                    this.IsBoundInNW = false;
                else
                    this.IsBoundInNW = true;
            }
        }

        protected bool is_bound_in_nw;
        public bool IsBoundInNW
        {
            get { return this.is_bound_in_nw; }
            protected set 
            { 
                this.is_bound_in_nw = value;
                this.RegisterPropertyChanged("IsBoundInNW");
            }
        }

        #endregion

        #region .CTOR

        internal Component()
        {
            // general
            this.ID = (++Component.NR_COMPONENTS);
            this.Name = "Component_" + this.ID.ToString();
            this.Description = "description...";
            
            // management
            this.Category = ParameterStructure.Component.Category.NoNe;
            this.InitAccess();
            this.FitsInSlots = new List<string>();
            this.CurrentSlot = ComponentUtils.COMP_SLOT_UNDEFINED;

            // contained entities
            this.ContainedComponents = new ObservableConcurrentDictionary<string, Component>();
            this.ReferencedComponents = new ObservableConcurrentDictionary<string, Component>();
            this.ReferencedBy = new List<Component>();
            this.ContainedParameters = new ObservableConcurrentDictionary<long, Parameter.Parameter>();
            this.ContainedCalculations = new List<Calculation>();

            // timestamp
            this.TimeStamp = DateTime.Now;

            // symbol
            this.SymbolId = -1;

            // display properties
            this.IsExpanded = false;
            this.IsSelected = false;
            this.IsMarkable = false;
            this.IsMarked = false;
            this.IsLocked = false;
            this.HideDetails = false;
            this.IsHidden = false;
            this.IsExcludedFromDisplay = false;
            this.HasAllParamsWithinLimits = true;

            // factory properties
            this.Factory = null;

            // interaction with NW
            this.BindingFNE = null;

            // geometry (default state: NONE)
            this.R2GInstances = new List<GeometricRelationship>();
            GeometricRelationship main_r2g = new GeometricRelationship(); // NONE
            main_r2g.PropertyChanged += main_r2g_PropertyChanged;
            this.R2GInstances.Add(main_r2g);
            this.R2GMainState = new Relation2GeomState();
            this.RotateR2GMainState = new PS_RelayCommand((x) => this.SwitchR2GMainType());

            // mapping for calculations
            this.mappings_to_comps = new List<Mapping.Mapping2Component>();
            this.MappedToBy = new List<Component>();
        }


        #endregion

        #region COPY .CTOR

        // assumes the _original is not NULL
        internal Component(Component _original, bool _keep_referenced_components = true)
        {
            // general
            this.ID = (++Component.NR_COMPONENTS);
            this.Name = _original.Name;
            this.Description = _original.Description;
            this.IsAutomaticallyGenerated = _original.IsAutomaticallyGenerated;

            // MANAGEMENT 1 - TMP (for usage during the filling in of parameters and subcomponents)
            this.Category = ParameterStructure.Component.Category.NoNe;
            this.InitAccess();
            this.FitsInSlots = new List<string>();
            this.CurrentSlot = ComponentUtils.COMP_SLOT_UNDEFINED;
            
            // contained entities
            // -- COPY the SubComponents
            this.ContainedComponents = new ObservableConcurrentDictionary<string, Component>();
            foreach(var entry in _original.ContainedComponents)
            {
                Component c = entry.Value;
                if (c != null)
                    this.ContainedComponents.Add(entry.Key, new Component(c, _keep_referenced_components));
                else
                    this.ContainedComponents.Add(entry.Key, null);
            }
            // -- REFERENCE the Referenced Components
            this.ReferencedComponents = new ObservableConcurrentDictionary<string, Component>();
            if (_keep_referenced_components)
            {
                foreach (var entry in _original.ReferencedComponents)
                {
                    this.ReferencedComponents.Add(entry.Key, entry.Value);
                }
            }
            this.ReferencedBy = new List<Component>();
            // -- MAKE RECORD of all Parameters in the original (incl. SubComponents)
            // -- necessary for the copying of the Calculations!!!
            this.contained_parameters_copy_record = this.GetFlatParameterCopyRecord();
            // -- COPY the Parameters
            this.ContainedParameters = new ObservableConcurrentDictionary<long, Parameter.Parameter>();
            foreach(var entry in _original.ContainedParameters)
            {
                Parameter.Parameter p_orig = entry.Value;
                if (p_orig == null) continue;

                Parameter.Parameter p_copy = p_orig.Clone();
                p_copy.PropertyChanged += param_PropertyChanged;
                this.ContainedParameters.Add(p_copy.ID, p_copy);
                this.contained_parameters_copy_record.Add(p_orig.ID, p_copy);
            }
            // -- COPY the Calculations
            this.ContainedCalculations = new List<Calculation>();
            if (this.contained_parameters_copy_record != null)
            {
                foreach (Calculation calc in _original.ContainedCalculations)
                {
                    // construct the new parameter lists with the parameters of the copied component
                    ObservableConcurrentDictionary<string, Parameter.Parameter> params_in_new = new ObservableConcurrentDictionary<string, Parameter.Parameter>();
                    ObservableConcurrentDictionary<string, Parameter.Parameter> params_out_new = new ObservableConcurrentDictionary<string, Parameter.Parameter>();
                    foreach (var entry in calc.InputParams)
                    {
                        params_in_new.Add(entry.Key, contained_parameters_copy_record[entry.Value.ID]);
                    }
                    foreach (var entry in calc.ReturnParams)
                    {
                        params_out_new.Add(entry.Key, contained_parameters_copy_record[entry.Value.ID]);
                    }

                    // create the new calculation
                    Calculation calc_copy = new Calculation(calc.Expression, calc.Name, params_in_new, params_out_new);
                    this.ContainedCalculations.Add(calc_copy);
                    this.HasCalculations = true;
                }
            }
            // MANAGEMENT 2 (overrides the changes made during the filling in of parameters and subcomponents)
            this.Category = _original.Category;
            this.FitsInSlots = new List<string>(_original.FitsInSlots);
            this.AccessLocal = new ComponentAccessProfile(_original.AccessLocal); // AFTER 'FitsInSlots' in order to adopt user changes made after the slot selection
            this.AccessLocal.UpdateProfileState();
            this.CurrentSlot = _original.CurrentSlot;

            // timestamp
            this.TimeStamp = DateTime.Now;

            // symbol
            this.SymbolId = _original.SymbolId;
                
            // display properties
            this.IsExpanded = false;
            this.IsSelected = false;
            this.IsMarkable = false;
            this.IsMarked = false;
            this.IsLocked = false;
            this.HideDetails = false;
            this.IsHidden = false;
            this.IsExcludedFromDisplay = false;
            this.HasAllParamsWithinLimits = _original.HasAllParamsWithinLimits;

            // factory properties
            this.Factory = _original.Factory;

            // interaction with NW
            this.BindingFNE = null;

            // geometry relationships ARE NOT COPIED (revert to default state: NONE)
            this.R2GInstances = new List<GeometricRelationship>();
            if (!this.IsAutomaticallyGenerated)
            {
                GeometricRelationship main_r2g = new GeometricRelationship(); // No copying
                main_r2g.PropertyChanged += main_r2g_PropertyChanged;
                this.R2GInstances.Add(main_r2g);
                this.R2GMainState = new Relation2GeomState();
            }
            else
            {
                // modified 27.09.2017: to avoid passing a REFERENCE! instead of a true copy
                foreach (GeometricRelationship gr in _original.R2GInstances)
                {
                    if (gr == null) continue;

                    if (this.R2GInstances.Count == 0)
                    {
                        this.R2GMainState = new Relation2GeomState { IsRealized = false, Type = gr.State.Type };
                        gr.PropertyChanged += main_r2g_PropertyChanged;
                    }
                    Relation2GeomState gr_state = new Relation2GeomState { IsRealized = false, Type = gr.State.Type };
                    this.R2GInstances.Add(new GeometricRelationship("Geometry", gr_state, new System.Windows.Media.Media3D.Point4D(-1, -1, -1, -1),
                                                                     System.Windows.Media.Media3D.Matrix3D.Identity, 
                                                                     System.Windows.Media.Media3D.Matrix3D.Identity, 
                                                                     System.Windows.Media.Media3D.Matrix3D.Identity));
                }
            }
            this.RotateR2GMainState = new PS_RelayCommand((x) => this.SwitchR2GMainType());

            // mapping for calculations -> copy them
            this.mappings_to_comps = new List<Mapping.Mapping2Component>();
            foreach(Mapping.Mapping2Component m in _original.mappings_to_comps)
            {
                Dictionary<long, long> contained_parameters_id_copy_record = contained_parameters_copy_record.Select(x => new KeyValuePair<long, long>(x.Key, x.Value.ID)).ToDictionary(x => x.Key, x => x.Value);
                Mapping.Mapping2Component m_copy = m.CopyForDataCarrier(contained_parameters_id_copy_record);
                if (m_copy != null)
                {
                    this.mappings_to_comps.Add(m_copy);
                    m_copy.Calculator.MappedToBy.Add(this);
                }
            }
            this.MappedToBy = new List<Component>();
        }

        #endregion

        #region PARSING .CTOR

        // does NOT handle referenced components (they may not be loaded when this ctor is called)
        // the component factory attempts to restore the references ONCE ALL COMPONENTS HAVE BEEN PARSED
        internal Component(long _id, string _name, string _description, bool _is_automatically_generated,
                           Category _category, ComponentAccessProfile _local_access, List<string> _fits_in_slots, string _current_slot,
                           IDictionary<string, Component> _contained_components, IDictionary<string, long> _ref_components,
                           IList<Parameter.Parameter> _contained_parameters, IList<CalculationPreview> _contained_calculations_preview,
                           IList<GeometricRelationship> _geometry, IList<Mapping.Mapping2Component> _maps, DateTime _time_stamp, long _symbol_id)
        {
            // general
            this.ID = _id;
            this.Name = _name;
            this.Description = _description;
            this.IsAutomaticallyGenerated = _is_automatically_generated;

            // MANAGEMENT 1 - TMP (for usage during the filling in of parameters and subcomponents)
            this.Category = ParameterStructure.Component.Category.NoNe;
            this.InitAccess();
            this.FitsInSlots = new List<string>();
            this.CurrentSlot = ComponentUtils.COMP_SLOT_UNDEFINED;

            // CONTAINED ENTITIES
            // // -- SubComponents (do NOT copy!)
            this.ContainedComponents = new ObservableConcurrentDictionary<string, Component>();
            foreach (var entry in _contained_components)
            {
                Component c = entry.Value;
                if (c != null)
                {
                    c.PropertyChanged += subComp_PropertyChanged;
                    this.ContainedComponents.Add(entry.Key, c);
                }
                else
                    this.ContainedComponents.Add(entry.Key, null);
            }
            // // -- referenced Components (just prepare containers)
            this.ReferencedComponents = new ObservableConcurrentDictionary<string, Component>();
            this.ReferencedBy = new List<Component>();
            // // -- Contained Parameters (do NOT copy!)
            this.ContainedParameters = new ObservableConcurrentDictionary<long, Parameter.Parameter>();
            foreach (Parameter.Parameter p in _contained_parameters)
            {
                if (p == null) continue;
                p.PropertyChanged += param_PropertyChanged;
                this.ContainedParameters.Add(p.ID, p);
            }
            // // -- Contained Calculations in the correct order (do NOT copy!)
            // // -- assumes that the calculation was correctly saved!
            this.ContainedCalculations = new List<Calculation>();
            Dictionary<long, Parameter.Parameter> all_params = this.GetFlatParamsList();
            foreach (CalculationPreview calcP in _contained_calculations_preview)
            {
                bool exceptio_caught = false;
                // compose the actual parameter lists
                ObservableConcurrentDictionary<string, Parameter.Parameter> parameters_in = new ObservableConcurrentDictionary<string, Parameter.Parameter>();
                foreach(var entry in calcP.InputParamsPreview)
                {
                    try
                    {
                        parameters_in.Add(entry.Key, all_params[entry.Value]);
                    }
                    catch(Exception e)
                    {
                        // should not happen!!!
                        Component.ERR_MESSAGES += e.Message + "\n";
                        exceptio_caught = true;
                        break;
                    }
                    
                }
                if (exceptio_caught)
                    continue;

                ObservableConcurrentDictionary<string, Parameter.Parameter> parameters_out = new ObservableConcurrentDictionary<string, Parameter.Parameter>();
                foreach(var entry in calcP.ReturnParamsPreview)
                {
                    try
                    {
                        parameters_out.Add(entry.Key, all_params[entry.Value]);
                    }
                    catch (Exception e)
                    {
                        // should not happen!!!
                        Component.ERR_MESSAGES += e.Message + "\n";
                        exceptio_caught = true;
                        break;
                    }
                }
                // construct the calculation
                Calculation calc = new Calculation(calcP.Expression, calcP.Name, parameters_in, parameters_out);
                calc.PropertyChanged += this.calculation_PropertyChanged;
                this.ContainedCalculations.Add(calc);
                this.HasCalculations = true;
            }

            // MANAGEMENT 2 (overrides the changes made during the filling in of parameters and subcomponents)
            this.Category = _category;
            this.FitsInSlots = new List<string>(_fits_in_slots);
            this.AccessLocal = _local_access; // AFTER 'FitsInSlots' in order to adopt user changes made after the slot selection
            this.AccessLocal.UpdateProfileState();
            this.CurrentSlot = _current_slot;

            // timestamp
            this.TimeStamp = _time_stamp;

            // symbol
            this.SymbolId = _symbol_id;

            // display properties
            this.IsExpanded = false;
            this.IsSelected = false;
            this.IsMarkable = false;
            this.IsMarked = false;
            this.IsLocked = false;
            this.HideDetails = false;
            this.IsHidden = false;
            this.IsExcludedFromDisplay = false;
            this.HasAllParamsWithinLimits = true;

            // interaction with NW
            this.BindingFNE = null;

            // geometry
            this.R2GInstances = new List<GeometricRelationship>();
            if (_geometry == null || _geometry.Count == 0)
            {
                GeometricRelationship main_r2g = new GeometricRelationship(); // NONE
                main_r2g.PropertyChanged += main_r2g_PropertyChanged;
                this.R2GInstances.Add(main_r2g);
                this.R2GMainState = new Relation2GeomState();
            }
            else
            {
                foreach(GeometricRelationship gr in _geometry)
                {
                    if (gr == null) continue;

                    if (this.R2GInstances.Count == 0)
                    {
                        this.R2GMainState = new Relation2GeomState { IsRealized = gr.State.IsRealized, Type = gr.State.Type };
                        gr.PropertyChanged += main_r2g_PropertyChanged;
                    }
                    this.R2GInstances.Add(gr);
                }
            }
            this.RotateR2GMainState = new PS_RelayCommand((x) => this.SwitchR2GMainType());

            // mapping to calculations
            this.mappings_to_comps = new List<Mapping.Mapping2Component>(_maps);
            this.MappedToBy = new List<Component>();
        }


        #endregion

        #region METHODS: Info (Reference requests, Copy records)

        // assumes that _original and _copy are actually the original and its copy w/o the references
        internal static List<ComponentReferenceRequest> GetReferenceRequests(Component _original, Component _copy)
        {
            List<ComponentReferenceRequest> requests = new List<ComponentReferenceRequest>();
            if (_original == null || _copy == null) return requests;

            // record the top-level requests (including empty slots)
            foreach (var entry in _original.ReferencedComponents)
            {
                string rSlot = entry.Key;
                Component rComp_orig = entry.Value;
                if (rComp_orig == null)
                    requests.Add(new ComponentReferenceRequest(_copy, -1, rSlot));
                else
                    requests.Add(new ComponentReferenceRequest(_copy, rComp_orig.ID, rSlot));
            }

            // recursively record the requests of all subcomponents
            int nr_subComp = _original.ContainedComponents.Count();
            if (nr_subComp != _copy.ContainedComponents.Count()) return requests;
            for (int i = 0; i < nr_subComp; i++)
            {
                var entry_orig = _original.ContainedComponents.ElementAt(i);
                var entry_copy = _copy.ContainedComponents.ElementAt(i);
                Component sComp_orig = entry_orig.Value;
                Component sComp_copy = entry_copy.Value;
                if (sComp_copy == null || sComp_orig == null) continue;

                requests.AddRange(Component.GetReferenceRequests(sComp_orig, sComp_copy));
            }

            return requests;
        }

        internal static Dictionary<long, Component> GetCopyRecord(Component _original, Component _copy)
        {
            Dictionary<long, Component> id_old_comp_new = new Dictionary<long, Component>();
            if (_original == null || _copy == null) return id_old_comp_new;

            // top level
            id_old_comp_new.Add(_original.ID, _copy);

            // recursively generate the copy record
            int nr_subComp = _original.ContainedComponents.Count();
            if (nr_subComp != _copy.ContainedComponents.Count()) return id_old_comp_new;
            for (int i = 0; i < nr_subComp; i++)
            {
                var entry_orig = _original.ContainedComponents.ElementAt(i);
                var entry_copy = _copy.ContainedComponents.ElementAt(i);
                Component sComp_orig = entry_orig.Value;
                Component sComp_copy = entry_copy.Value;
                if (sComp_copy == null || sComp_orig == null) continue;

                Dictionary<long, Component> sC_id_old_comp_new = Component.GetCopyRecord(sComp_orig, sComp_copy);
                foreach(var entry in sC_id_old_comp_new)
                {
                    id_old_comp_new.Add(entry.Key, entry.Value);
                }
            }

            return id_old_comp_new;
        }

        internal bool CanParticipateInFlNet()
        {
            foreach(var entry in this.ContainedParameters)
            {
                string name = entry.Value.Name;
                if (name.Contains(Component.FLNET_COMP_PARAM_NAME_MS) ||
                    name.Contains(Component.FLNET_COMP_PARAM_NAME_ES) ||
                    name.Contains(Component.FLNET_COMP_PARAM_NAME_IS))
                    return true;
            }
            return false;
        }

        #endregion

        #region METHODS: Filtering, Visibility

        internal void HideAccTo(List<Category> _cats, bool _or = true)
        {
            // handle this components
            bool old_is_hidden = this.is_hidden;
            
            if (_cats == null || _cats.Count < 1)
            {
                this.IsHidden = false;
            }
            else
            {
                bool take_this = !_or;
                foreach (Category cat in _cats)
                {
                    if (_or)
                        take_this |= this.Category.HasFlag(cat);
                    else
                        take_this &= this.Category.HasFlag(cat);
                }
                this.IsHidden = !(take_this);
            }

            if (old_is_hidden != is_hidden)
                this.UpdateChildrenContainer();

            // handle the subcomponents
            foreach (var entry in this.ContainedComponents)
            {
                Component comp = entry.Value;
                if (comp == null) continue;

                comp.HideAccTo(_cats, _or);
            }
        }

        internal void HideAccTo(List<ComponentManagerType> _users, bool _or = true)
        {
            // handle this components
            bool old_is_hidden = this.is_hidden;

            if (_users == null || _users.Count < 1)
            {
                this.IsHidden = false;
            }
            else
            {
                bool take_this = !_or;
                foreach (ComponentManagerType user in _users)
                {
                    if (_or)
                        take_this |= this.AccessLocal[user].AccessTypeFlags.HasFlag(ComponentAccessType.WRITE);
                    else
                        take_this &= this.AccessLocal[user].AccessTypeFlags.HasFlag(ComponentAccessType.WRITE);
                }
                this.IsHidden = !(take_this);
            }

            if (old_is_hidden != is_hidden)
                this.UpdateChildrenContainer();

            // handle the subcomponents
            foreach (var entry in this.ContainedComponents)
            {
                Component comp = entry.Value;
                if (comp == null) continue;

                comp.HideAccTo(_users, _or);
            }
        }

        internal void ExportLightBulbSettings(ref StringBuilder _sb)
        {
            if (_sb == null) return;

            foreach(var entry in this.ContainedComponents)
            {
                Component c = entry.Value;
                if (c == null) continue;

                string on = (c.IsExcludedFromDisplay) ? "0" : "1";
                _sb.AppendLine(c.ID + ":" + on);
                c.ExportLightBulbSettings(ref _sb);
            }
        }

        /// <summary>
        /// To be used when turning ALL components on or off.
        /// </summary>
        /// <param name="_on"></param>
        internal void SetLightBulbSettingsForSubComps(bool _on)
        {
            foreach(var entry in this.ContainedComponents)
            {
                Component c = entry.Value;
                if (c == null) continue;

                c.IsExcludedFromDisplay = !_on;
                c.SetLightBulbSettingsForSubComps(_on);
            }
        }

        #endregion

        #region METHODS: Access Management

        protected void InitAccess()
        {
            Dictionary<ComponentManagerType, ComponentAccessTracker> prof = new Dictionary<ComponentManagerType, ComponentAccessTracker>();
            int nrRoles = Enum.GetNames(typeof(ComponentManagerType)).Length;
            for (int i = 0; i < nrRoles; i++)
            {
                ComponentAccessTracker tracker = new ComponentAccessTracker();
                tracker.AccessTypeFlags = ComponentAccessType.NO_ACCESS;
                prof.Add((ComponentManagerType)i, tracker);
            }
            this.AccessLocal = new ComponentAccessProfile(prof, ComponentManagerType.ADMINISTRATOR);
        }

        internal void GiveWritingAccessToCurrentUser(ComponentManagerType _user)
        {
            ComponentAccessTracker tracker = this.AccessLocal[_user];
            tracker.AccessTypeFlags |= ComponentAccessType.READ;
            tracker.AccessTypeFlags |= ComponentAccessType.WRITE;
            this.AccessLocal[_user] = tracker;
        }

        internal void SetupForAutomaticallyCreatedComps(ComponentManagerType _manager)
        {
            this.IsAutomaticallyGenerated = true;

            int nrRoles = Enum.GetNames(typeof(ComponentManagerType)).Length;
            for (int i = 0; i < nrRoles; i++)
            {
                ComponentAccessTracker tracker = this.AccessLocal[(ComponentManagerType)i];

                tracker.AccessTypeFlags |= ComponentAccessType.READ;
                if (_manager == (ComponentManagerType)i)
                {
                    tracker.AccessTypeFlags |= ComponentAccessType.WRITE;
                    tracker.AccessTypeFlags |= ComponentAccessType.SUPERVIZE;
                }

                if ((ComponentManagerType)i == ComponentManagerType.ADMINISTRATOR)
                    tracker.AccessTypeFlags |= ComponentAccessType.WRITE;
                if ((ComponentManagerType)i == ComponentManagerType.BUILDING_DEVELOPER)
                    tracker.AccessTypeFlags |= ComponentAccessType.RELEASE;

                this.AccessLocal[(ComponentManagerType)i] = tracker;
            }
        }

        protected bool RecordWritingAccess(ComponentManagerType _user)
        {
            // check if the user has writing access
            ComponentAccessTracker tracker = this.AccessLocal[_user];
            if (!(tracker.AccessTypeFlags.HasFlag(ComponentAccessType.WRITE))) return false;
            // record the access
            tracker.LastAccess_Write = DateTime.Now;
            this.AccessLocal[_user] = tracker;
            return true;
        }

        internal bool HasWritingAccess(ComponentManagerType _user)
        {
            // check if the user has writing access
            ComponentAccessTracker tracker = this.AccessLocal[_user];
            if ((tracker.AccessTypeFlags.HasFlag(ComponentAccessType.WRITE)))
                return true;
            else
                return false;
        }

        protected ComponentManagerType GetManagerWWritingAccess()
        {
            int nrRoles = Enum.GetNames(typeof(ComponentManagerType)).Length;
            // try other users before the administrator
            for(int i = 1; i < nrRoles; i++)
            {
                if (this.AccessLocal[(ComponentManagerType)i].AccessTypeFlags.HasFlag(ComponentAccessType.WRITE))
                    return (ComponentManagerType)i;
            }

            return ComponentManagerType.ADMINISTRATOR;
        }

        internal bool HasReadingAccess(ComponentManagerType _user)
        {
            // check if the user has reading access
            ComponentAccessTracker tracker = this.AccessLocal[_user];
            if ((tracker.AccessTypeFlags.HasFlag(ComponentAccessType.READ)))
                return true;
            else
                return false;
        }

        public bool HasReadWriteAccess(ComponentManagerType _user)
        {
            return this.HasReadingAccess(_user) && this.HasWritingAccess(_user);
        }

        #endregion

        #region METHODS: Parameters (Management)

        public bool AddParameter(Parameter.Parameter _param, ComponentManagerType _user)
        {
            // check if the user has writing access
            bool success = this.RecordWritingAccess(_user);
            if (!success) return false;

            // perform the operation
            if (_param == null) return false;
            if (this.ContainedParameters == null) return false;
            if (this.ContainedParameters.ContainsKey(_param.ID)) return false;

            _param.PropertyChanged += param_PropertyChanged;
            this.ContainedParameters.Add(_param.ID, _param);
            this.Category |= _param.Category;
            return true;
        }

        public bool AddCopyOfParameter(Parameter.Parameter _param, ComponentManagerType _user)
        {
            if (_param == null) return false;
            Parameter.Parameter copy = _param.Clone();

            return this.AddParameter(copy, _user);
        }

        public bool RemoveParameter(Parameter.Parameter _param, ComponentManagerType _user)
        {
            Component owner = this.GetParameterOwner(_param);
            if (owner == null) return false;

            // check if the user has writing access
            bool success = owner.RecordWritingAccess(_user);
            if (!success) return false;

            Parameter.Parameter p = owner.ContainedParameters[_param.ID];
            if (p != null)
                p.PropertyChanged -= owner.param_PropertyChanged;

            owner.ContainedParameters.Remove(_param.ID);
            owner.GatherCategoryInfo();
            return true;           
        }

        public void CopyParameterWithinComponent(Parameter.Parameter _param, ComponentManagerType _user)
        {
            Component owner = this.GetParameterOwner(_param);
            if (owner == null) return;

            // check if the user has writing access
            bool success = owner.RecordWritingAccess(_user);
            if (!success) return;

            // copy
            Parameter.Parameter copy = _param.Clone();
            copy.PropertyChanged += owner.param_PropertyChanged;
            owner.ContainedParameters.Add(copy.ID, copy);
        }

        /// <summary>
        /// Called when communicating w external apps to transfer parameter values
        /// </summary>
        /// <param name="_name"></param>
        /// <param name="_value"></param>
        /// <param name="_user"></param>
        /// <param name="_add_if_name_not_found"></param>
        public void SetParameterValue(string _name, double _value, ComponentManagerType _user, bool _add_if_name_not_found)
        {
            // check if the user has writing access
            bool success = this.RecordWritingAccess(_user);
            if (!success) return;

            // perform the operation
            if (this.ContainedParameters == null) return;

            // look for a parameter by this name
            Parameter.Parameter p = this.ContainedParameters.Values.FirstOrDefault(x => x.Name == _name && x.ValueField == null); // canged 18.08.2017 // && x.Propagation == InfoFlow.INPUT
            if (p != null)
            {
                p.ValueCurrent = _value;
                return; // DONE
            }

            if (_add_if_name_not_found)
            {
                Parameter.Parameter p_new = new Parameter.Parameter(_name, Parameter.Parameter.GetReservedUnits(_name), _value);
                p_new.ValueMin = double.MinValue;
                p_new.ValueMax = double.MaxValue;
                p_new.TextValue = "generated";
                p_new.Propagation = InfoFlow.INPUT;
                p_new.Category |= Parameter.Parameter.GetReservedCategoryFlag(_name);

                p_new.PropertyChanged += param_PropertyChanged;
                this.ContainedParameters.Add(p_new.ID, p_new);
                this.Category |= Parameter.Parameter.GetReservedCategoryFlag(_name);
            }
        }

        #endregion

        #region METHODS: Parameters (Info)

        // determine, if the component contains the parameter
        // or if a subcomponent contains it
        protected bool ContainsParameter(Parameter.Parameter _p)
        {
            if (_p == null) return false;
           
            if (this.ContainedParameters.ContainsKey(_p.ID)) return true;           

            foreach(var entry in this.contained_components)
            {
                Component sComp = entry.Value;
                if (sComp == null) continue;

                bool found = sComp.ContainsParameter(_p);
                if (found) return true;
            }

            return false;
        }

        protected Parameter.Parameter GetCorresponding(Parameter.Parameter _p)
        {
            if (_p == null) return null;

            // look locally first
            Parameter.Parameter p_corresp = this.ContainedParameters.Values.FirstOrDefault(x => x != null && x.Name == _p.Name && x.Unit == _p.Unit && x.Propagation == _p.Propagation);
            if (p_corresp != null)
                return p_corresp;

            // look in the sub-components
            foreach (var entry in this.contained_components)
            {
                Component sComp = entry.Value;
                if (sComp == null) continue;

                p_corresp = sComp.GetCorresponding(_p);
                if (p_corresp != null)
                    return p_corresp;
            }

            return p_corresp;
        }

        public Parameter.Parameter GetFirstParamByName(string _name)
        {
            if (string.IsNullOrEmpty(_name)) return null;

            Parameter.Parameter p = this.ContainedParameters.Values.FirstOrDefault(x => x != null && x.Name == _name);
            if (p != null)
                return p;

            foreach (var entry in this.contained_components)
            {
                Component sComp = entry.Value;
                if (sComp == null) continue;

                p = sComp.GetFirstParamByName(_name);
                if (p != null)
                    return p;
            }

            return p;
        }

        public Parameter.Parameter GetFirstParamBySuffix(string _suffix)
        {
            if (string.IsNullOrEmpty(_suffix)) return null;

            Parameter.Parameter p = this.ContainedParameters.Values.FirstOrDefault(x => x != null && x.Name.EndsWith(_suffix));
            if (p != null)
                return p;

            foreach (var entry in this.contained_components)
            {
                Component sComp = entry.Value;
                if (sComp == null) continue;

                p = sComp.GetFirstParamBySuffix(_suffix);
                if (p != null)
                    return p;
            }

            return p;
        }

        protected Component GetParameterOwner(Parameter.Parameter _p)
        {
            if (_p == null) return null;

            if (this.ContainedParameters != null && this.ContainedParameters.ContainsKey(_p.ID)) return this;

            foreach (var entry in this.contained_components)
            {
                Component sComp = entry.Value;
                if (sComp == null) continue;

                Component found = sComp.GetParameterOwner(_p);
                if (found != null) return found;
            }

            return null;
        }

        protected Dictionary<long, Parameter.Parameter> GetFlatParamsList()
        {
            Dictionary<long, Parameter.Parameter> list = new Dictionary<long, Parameter.Parameter>();

            if (this.ContainedParameters != null)
            {
                foreach (Parameter.Parameter p in this.ContainedParameters.Values)
                {
                    if (p == null) continue;
                    list.Add(p.ID, p);
                }
            }
            foreach(Component c in this.ContainedComponents.Values)
            {
                if (c == null) continue;
                Dictionary<long, Parameter.Parameter> sList = c.GetFlatParamsList();
                foreach(var entry in sList)
                {
                    list.Add(entry.Key, entry.Value);
                }
            }

            return list;
        }

        protected int GetNrOfFlatParams()
        {
            int nrP = 0;
            if (this.ApplyIsExcludedFromDisplay && this.IsExcludedFromDisplay)
                return nrP;

            if (this.ContainedParameters != null)
            {
                nrP += this.GetOwnParamsFilteredByFactory().Count; // updated 31.08.2017
            }            
            foreach (var entry in this.ContainedComponents)
            {
                Component c = entry.Value;
                if (c == null) continue;
                nrP += c.GetNrOfFlatParams();
            }

            return nrP;
        }

        protected Dictionary<long, Parameter.Parameter> GetFlatParameterCopyRecord()
        {
            Dictionary<long, Parameter.Parameter> list = new Dictionary<long, Parameter.Parameter>();

            if (this.contained_parameters_copy_record != null)
            {
                foreach (var entry in this.contained_parameters_copy_record)
                {
                    if (!list.ContainsKey(entry.Key))
                        list.Add(entry.Key, entry.Value);
                }
            }
            foreach (Component c in this.ContainedComponents.Values)
            {
                if (c == null) continue;
                Dictionary<long, Parameter.Parameter> sList = c.GetFlatParameterCopyRecord();
                foreach (var entry in sList)
                {
                    if (!(list.ContainsKey(entry.Key)))
                        list.Add(entry.Key, entry.Value);
                }
            }

            return list;
        }

        /// <summary>
        /// Find a parameter at any hierarchy level by its ID.
        /// </summary>
        /// <param name="_id"></param>
        /// <returns></returns>
        public Parameter.Parameter GetById(long _id)
        {
            if (_id < 0)
                return null;

            Dictionary<long, Parameter.Parameter> all_params = this.GetFlatParamsList();
            if (all_params.ContainsKey(_id))
                return all_params[_id];
            else
                return null;
        }

        /// <summary>
        /// Find the parameters at any hierarchy level by its ID.
        /// </summary>
        /// <param name="_ids"></param>
        /// <returns></returns>
        public List<Parameter.Parameter> GetByIds(List<long> _ids)
        {
            if (_ids == null) return null;
            if (_ids.Count == 0) return new List<Parameter.Parameter>();

            Dictionary<long, Parameter.Parameter> all_params = this.GetFlatParamsList();
            List<Parameter.Parameter> found = new List<Parameter.Parameter>();
            foreach(long id in _ids)
            {
                if (id < 0) continue;
                if (all_params.ContainsKey(id))
                {
                    Parameter.Parameter p = all_params[id];
                    found.Add(p);
                }
            }

            return found;
        }

        protected List<Parameter.Parameter> GetOwnParamsFilteredByFactory()
        {
            if (this.Factory == null) return new List<Parameter.Parameter>(this.ContainedParameters.Values);
            if (string.IsNullOrEmpty(this.Factory.ParamFilterString)) return new List<Parameter.Parameter>(this.ContainedParameters.Values);

            return this.ContainedParameters.Values.Where(x => x.Name.Contains(this.Factory.ParamFilterString)).ToList();
        }

        #endregion

        #region METHODS: Parameters (Comparison and Bounds)
        public void CompareContainedPCWith(Component _comp)
        {
            if (_comp == null) return;
            foreach(var entry in this.ContainedParameters)
            {
                Parameter.Parameter p = entry.Value;
                if (p == null) continue;
                p.CompareWith(_comp.ContainedParameters.Values.ToList());
            }
            foreach(Calculation calc in this.ContainedCalculations)
            {
                if (calc == null) continue;
                calc.CompareWith(_comp.ContainedCalculations);
            }
        }

        public void CompareContainedPCWith_RemoveMarkings()
        {
            foreach (var entry in this.ContainedParameters)
            {
                Parameter.Parameter p = entry.Value;
                if (p == null) continue;
                p.RemoveComparisonMarking();
            }
            foreach (Calculation calc in this.ContainedCalculations)
            {
                if (calc == null) continue;
                calc.RemoveComparisonMarking();
            }
        }

        // standard behaviour: inform parent
        internal void CheckParamBounds()
        {
            this.HasAllParamsWithinLimits = true;
            foreach (var entry in this.ContainedParameters)
            {
                if (entry.Value != null)
                {
                    if (!entry.Value.IsWithinBounds)
                    {
                        this.HasAllParamsWithinLimits = false;
                        break;
                    }
                }
            }           

            bool cc_have_params_all_within_limits = true;
            foreach (var entry in this.ContainedComponents)
            {
                if (entry.Value == null) continue;
                entry.Value.CheckParamBounds();
                cc_have_params_all_within_limits &= entry.Value.HasAllParamsWithinLimits;
                if (!cc_have_params_all_within_limits)
                {
                    this.HasAllParamsWithinLimits = false;
                    // break;
                    // a break causes not all subcomponents to be checked!
                }
            }           

        }

        protected void PropagateParamBoundsToParent()
        {
            if (this.Factory != null)
            {
                Component c = this.Factory.GetParentComponent(this);
                if (c == null)
                    this.CheckParamBounds();
                else
                    c.CheckParamBounds();
            }
        }

        #endregion

        #region METHODS: Parameter (Value Field)

        // to be called after ValueCurrent or TextValue change
        protected void ExtractValueFromValueField(Parameter.Parameter _p)
        {
            if (_p == null) return;

            // 1. find out, if this is a pointer parameter inside THIS component
            bool is_pxX = (_p.Name.EndsWith(Parameter.Parameter.POINTER_X_NAME_TAG));
            bool is_pyY = (_p.Name.EndsWith(Parameter.Parameter.POINTER_Y_NAME_TAG));
            bool is_pzZ = (_p.Name.EndsWith(Parameter.Parameter.POINTER_Z_NAME_TAG));
            bool is_psS = (_p.Name.EndsWith(Parameter.Parameter.POINTER_STRING_NAME_TAG));
            if (!is_pxX && !is_pyY && !is_pzZ && !is_psS) return;

            string pName = _p.Name.Substring(0, _p.Name.Length - Parameter.Parameter.POINTER_X_NAME_TAG.Length);
            if (string.IsNullOrEmpty(pName)) return;

            Parameter.Parameter pVF = this.ContainedParameters.Values.FirstOrDefault(x => x != null && x.Name == pName);
            if (pVF == null) return;
            if (pVF.ValueField == null) return;

            // 2. find all pointers for the parameter pVF
            Parameter.Parameter pX, pY, pZ, pS;
            pX = this.ContainedParameters.Values.FirstOrDefault(x => x != null && x.Name == (pName + Parameter.Parameter.POINTER_X_NAME_TAG));
            bool has_pxX = (pX != null);
            pY = this.ContainedParameters.Values.FirstOrDefault(x => x != null && x.Name == (pName + Parameter.Parameter.POINTER_Y_NAME_TAG));
            bool has_pyY = (pY != null);
            pZ = this.ContainedParameters.Values.FirstOrDefault(x => x != null && x.Name == (pName + Parameter.Parameter.POINTER_Z_NAME_TAG));
            bool has_pzZ = (pZ != null);
            pS = this.ContainedParameters.Values.FirstOrDefault(x => x != null && x.Name == (pName + Parameter.Parameter.POINTER_STRING_NAME_TAG));
            bool has_psS = (pS != null);
            if (!has_pxX && !has_pyY && !has_pzZ && !has_psS) return;

            // 3. perform the value interpolation in the ValueField
            double vX = (pX == null) ? 0.0 : pX.ValueCurrent;
            double vY = (pY == null) ? 0.0 : pY.ValueCurrent;
            double vZ = (pZ == null) ? 0.0 : pZ.ValueCurrent;
            string vS = (pS == null) ? string.Empty : pS.TextValue;

            if (double.IsNaN(vX) || double.IsNaN(vY) || double.IsNaN(vZ) || vS.EndsWith(Parameter.Parameter.POINTER_STRING)) return;
            Values.MultiValPointer pointer = pVF.MValPointer;
            pVF.ValueField.CreateNewPointer(ref pointer, vX, vY, vZ, vS);
            pVF.MValPointer = pointer;
        }

        #endregion

        #region METHODS: Parameters (Reset)

        internal void ResetParamsDependentOnFlowNetwork()
        {
            foreach(var entry in this.ContainedParameters)
            {
                if (entry.Value == null) continue;
                if (entry.Value.Propagation == InfoFlow.CALC_IN)
                    entry.Value.ValueCurrent = 0.0;
            }
            foreach(var entry in this.ContainedComponents)
            {
                if (entry.Value == null) continue;
                entry.Value.ResetParamsDependentOnFlowNetwork();
            }
        }


        #endregion

        #region METHODS: Calculations

        public Calculation CreateEmptyCalculation()
        {
            ObservableConcurrentDictionary<string, Parameter.Parameter> p_in = new ObservableConcurrentDictionary<string, Parameter.Parameter>();
            p_in.Add("x", null);
            ObservableConcurrentDictionary<string, Parameter.Parameter> p_out = new ObservableConcurrentDictionary<string, Parameter.Parameter>();
            p_out.Add("out01", null);
            this.CalcInEditMode = new Calculation(Component.CALC_CANDIDATE_EXPR, Component.CALC_CANDIDATE_NAME, p_in, p_out);

            return this.CalcInEditMode;
        }

        internal bool TestAndSaveCalculationInEditMode(ComponentManagerType _user, Calculation _to_edit, ref CalculationState calc_state)
        {
            this.CalcInEditMode = _to_edit; // added 02.09.2016
            if (this.CalcInEditMode == null) return false;

            return this.AddCalculation(this.CalcInEditMode.Name, this.CalcInEditMode.Expression,
                                        new Dictionary<string, Parameter.Parameter>(this.CalcInEditMode.InputParams),
                                        new Dictionary<string, Parameter.Parameter>(this.CalcInEditMode.ReturnParams),
                                        _user, ref calc_state);
        }

        // the involved parameters can be of this component or of subcomponents
        // NOT from parent components
        protected bool AddCalculation(string _name, string _expr, Dictionary<string, Parameter.Parameter> _parameters_in,
                                                               Dictionary<string, Parameter.Parameter> _parameters_out,
                                                               ComponentManagerType _user, ref CalculationState calc_state)
        {
            // check if the user has writing access
            bool success = this.RecordWritingAccess(_user);
            if (!success)
            {
                calc_state = CalculationState.NO_WRITING_ACCESS;
                return false; 
            }

            // check input consistency
            if (string.IsNullOrEmpty(_name) || string.IsNullOrEmpty(_expr) ||
                _parameters_in == null || _parameters_in.Count < 1 ||
                _parameters_out == null || _parameters_out.Count < 1)
            {
                calc_state = CalculationState.MISSING_DATA;
                return false;
            }

            // check if the input and output parameters are valid
            // i.e contained in this component or any of its subcomponents
            foreach(var entry in _parameters_in)
            {
                Parameter.Parameter p = entry.Value;
                if (p == null)
                {
                    calc_state = CalculationState.PARAMS_NOT_OF_THIS_OR_CHILD_COMP;
                    return false;
                }

                bool valid = this.ContainsParameter(p);
                if (!valid)
                {
                    calc_state = CalculationState.PARAMS_NOT_OF_THIS_OR_CHILD_COMP;
                    return false; 
                }
            }
            foreach(var entry in _parameters_out)
            {
                Parameter.Parameter p = entry.Value;
                if (p == null) return false;
                // output parameters cannot be coupled with value fields
                if (p.ValueField != null)
                {
                    calc_state = CalculationState.PARAM_OUT_W_VALUE_FILED;
                    return false;
                }
                // output parameters cannot be input
                if (_parameters_in.ContainsValue(p))
                {
                    calc_state = CalculationState.PARAMS_IN_OUT_SAME;
                    return false;
                }

                bool valid = this.ContainsParameter(p);
                if (!valid)
                {
                    calc_state = CalculationState.PARAMS_NOT_OF_THIS_OR_CHILD_COMP;
                    return false;
                }
            }

            // check if the expression can be compiled into a valid function
            Dictionary<string, double> params_in_value = new Dictionary<string, double>();
            foreach (var entry in _parameters_in)
            {
                if (entry.Value == null) continue;
                params_in_value.Add(entry.Key, entry.Value.ValueCurrent);
            }

            try
            {
                // try to parse the expression(SPACES and UNDERLINES in the parameter names cause Exceptions!!!)
                Func<double> func = Component.CALCULATOR.ParseExpression(_expr, params_in_value).Compile();
                if (func == null)
                {
                    calc_state = CalculationState.INVALID_SYNTAX;
                    return false;
                }
            }
            catch (Exception e)
            {
                calc_state = CalculationState.INVALID_SYNTAX;
                Component.ERR_MESSAGES += e.Message + "\n";
                // CLEAN-UP added 30.08.2016
                // check if the component is new or an old one in edit mode
                Calculation found_e = this.ContainedCalculations.Find(x => x.ID == this.CalcInEditMode.ID);
                if (found_e != null)
                {
                    // delete the old calculation, bacuase it contains a syntactic error
                    this.RemoveCalculation(found_e, _user);
                }
                return false;
            }

            // if all is well, create the calculation
            ObservableConcurrentDictionary<string, Parameter.Parameter> p_IN = new ObservableConcurrentDictionary<string, Parameter.Parameter>();
            foreach (var entry in _parameters_in)
            {
                p_IN.Add(entry.Key, entry.Value);
            }
            ObservableConcurrentDictionary<string, Parameter.Parameter> p_OUT = new ObservableConcurrentDictionary<string, Parameter.Parameter>();
            foreach (var entry in _parameters_out)
            {
                p_OUT.Add(entry.Key, entry.Value);
            }

            // check if the component is new or an old one in edit mode: added 29.08.2016
            Calculation found = this.ContainedCalculations.Find(x => x.ID == this.CalcInEditMode.ID);
            if (found != null)
            {
                // delete the old calculation
                this.RemoveCalculation(found, _user);
            }
            Calculation calc_new = new Calculation(_expr, _name, p_IN, p_OUT);

            // save the calculation in the correct order
            return this.SaveCalculationsInOrder(calc_new, ref calc_state);
        }

        protected bool SaveCalculationsInOrder(Calculation _calc_new, ref CalculationState calc_state)
        {
            if (this.ContainedCalculations.Count() == 0)
            {
                this.ContainedCalculations = new List<Calculation> { _calc_new };
                this.HasCalculations = true;
                return true;
            }

            // ------------------------------------- REORDERING (20.09.2017) ------------------------
            this.ReorderCurrentCalculations();
            // ------------------------------------- REORDERING (20.09.2017) ------------------------

            int insert_index_BEFORE = this.ContainedCalculations.Count() + 1; // changed 24.08.2016
            int insert_index_AFTER = -1; // changed 24.08.2016
            for (int i = 0; i < this.ContainedCalculations.Count; i++ )
            {
                Calculation calc_current = this.ContainedCalculations[i];

                //// ------------------------------------- CHECK W/O REORDERING (20.09.2017) ------------------------
                //// check if the current calculation is bound in any calculation chains
                //bool calc_current_bound = false;
                //if (this.Factory != null)
                //{
                //    foreach (var entryI in calc_current.InputParams)
                //    {
                //        Parameter.Parameter pi = entryI.Value;
                //        if (pi == null) continue;
                //        ParameterDeleteResult p_del_test = this.Factory.RemoveParameterFromComponentTest(pi, this, calc_current, false);
                //        if (p_del_test != ParameterDeleteResult.SUCCESS)
                //        {
                //            calc_current_bound = true;
                //            break;
                //        }
                //    }
                //    foreach (var entryR in calc_current.ReturnParams)
                //    {
                //        Parameter.Parameter pr = entryR.Value;
                //        if (pr == null) continue;
                //        ParameterDeleteResult p_del_test = this.Factory.RemoveParameterFromComponentTest(pr, this, calc_current, false);
                //        if (p_del_test != ParameterDeleteResult.SUCCESS)
                //        {
                //            calc_current_bound = true;
                //            break;
                //        }
                //    }
                //}
                //// if not ... skip the following checks
                //if (!calc_current_bound) continue;
                //// ------------------------------------- CHECK W/O REORDERING (20.09.2017) ------------------------

                foreach (var entryNR in _calc_new.ReturnParams)
                {
                    Parameter.Parameter p_r = entryNR.Value;

                    // no two calculations can have the same return parameter
                    if (calc_current.ReturnParams.Values.Contains(p_r))
                    {
                        calc_state = CalculationState.PARAMS_OUT_DUPLICATE;
                        return false;
                    }

                    // if the new calculation supplies the input for another
                    // it has to be BEFORE it in the calculation chain
                    if (calc_current.InputParams.Values.Contains(p_r))
                    {
                        insert_index_BEFORE = Math.Min(insert_index_BEFORE, i);
                    }
                }

                foreach (var entryNI in _calc_new.InputParams)
                {
                    Parameter.Parameter p_i = entryNI.Value;

                    // if the new calculation receives input from another
                    // it has to be AFTER it in the calculation chain
                    if (calc_current.ReturnParams.Values.Contains(p_i))
                    {
                        insert_index_AFTER = Math.Max(insert_index_AFTER, i + 1);
                    }
                }
            }

            int insert_index_final = this.ContainedCalculations.Count(); // added 24.08.2016
            if (insert_index_AFTER > -1 && insert_index_BEFORE <= this.ContainedCalculations.Count()) // changed 24.08.2016
            {
                if (insert_index_BEFORE < insert_index_AFTER)
                {
                    // a loop in the calculation chain
                    calc_state = CalculationState.CAUSES_CALCULATION_LOOP;
                    return false;
                }
                else
                {
                    insert_index_final = insert_index_BEFORE;
                }
            }
            else if (insert_index_AFTER == -1 && insert_index_BEFORE <= this.ContainedCalculations.Count())
            {
                insert_index_final = insert_index_BEFORE;
            }
            else if (insert_index_AFTER > -1 && insert_index_BEFORE > this.ContainedCalculations.Count())
            {
                insert_index_final = insert_index_AFTER;
            }

            // insert at the correct index
            List<Calculation> new_calc_chain = new List<Calculation>(this.ContainedCalculations);
            _calc_new.PropertyChanged += calculation_PropertyChanged;
            new_calc_chain.Insert(insert_index_final, _calc_new); // changed 24.08.2016
            this.ContainedCalculations = new List<Calculation>(new_calc_chain);

            // extract the list of all parameters of this components and its subcomponents
            // that are involved in calculations
            this.GatherParamsInvolvedInCalcs();

            // DONE
            this.CalcInEditMode = null;
            return true;
        }

        /// <summary>
        /// Make sure that calculations w parameters not bound in others are at the beginning of the list.
        /// </summary>
        protected void ReorderCurrentCalculations()
        {
            if (this.Factory == null) return;

            List<Calculation> to_move_to_front = new List<Calculation>();
            for (int i = 0; i < this.ContainedCalculations.Count; i++)
            {
                Calculation calc_current = this.ContainedCalculations[i];

                // check if the current calculation is bound in any calculation chains         
                bool calc_current_bound = false;
                foreach (var entryI in calc_current.InputParams)
                {
                    Parameter.Parameter pi = entryI.Value;
                    if (pi == null) continue;
                    ParameterDeleteResult p_del_test = this.Factory.RemoveParameterFromComponentTest(pi, this, calc_current, false);
                    if (p_del_test != ParameterDeleteResult.SUCCESS)
                    {
                        calc_current_bound = true;
                        break;
                    }
                }
                if (calc_current_bound) continue;

                foreach (var entryR in calc_current.ReturnParams)
                {
                    Parameter.Parameter pr = entryR.Value;
                    if (pr == null) continue;
                    ParameterDeleteResult p_del_test = this.Factory.RemoveParameterFromComponentTest(pr, this, calc_current, false);
                    if (p_del_test != ParameterDeleteResult.SUCCESS)
                    {
                        calc_current_bound = true;
                        break;
                    }
                }
                if (!calc_current_bound)
                    to_move_to_front.Add(calc_current);
            }
            if (to_move_to_front.Count == 0) return;

            // re-order
            List<Calculation> new_order = new List<Calculation>();
            foreach(Calculation calc in this.ContainedCalculations)
            {
                if (to_move_to_front.Contains(calc))
                    new_order.Insert(0, calc);
                else
                    new_order.Add(calc);
            }

            this.ContainedCalculations = new_order;
        }
        

        public bool RemoveCalculation(Calculation _to_remove, ComponentManagerType _user)
        {
            // check if the user has writing access
            bool success = this.RecordWritingAccess(_user);
            if (!success) return false;

            if (_to_remove == null) return false;

            _to_remove.PropertyChanged -= calculation_PropertyChanged;
            bool success0 = this.ContainedCalculations.Remove(_to_remove);
            this.RegisterPropertyChanged("ContainedCalculations");
            this.UpdateChildrenContainer();
            this.UpdateCalcChildrenContainer();
            if (success0)
            {
                if (this.ContainedCalculations.Count == 0)
                    this.HasCalculations = false;

                this.GatherParamsInvolvedInCalcs();
            }

            return success;
        }

        protected void GatherParamsInvolvedInCalcs()
        {
            this.params_involved_in_calculations = new List<long>();
            foreach (Calculation c in this.ContainedCalculations)
            {
                foreach (var entry in c.InputParams)
                {
                    if (entry.Value == null) continue;
                    this.params_involved_in_calculations.Add(entry.Value.ID);
                }
                foreach (var entry in c.ReturnParams)
                {
                    if (entry.Value == null) continue;
                    this.params_involved_in_calculations.Add(entry.Value.ID);
                }
            }
        }

        protected List<long> GatherParamsInvolvedInCalcs(Calculation _calc_to_exclude = null)
        {
            List<long> piic = new List<long>();
            foreach (Calculation c in this.ContainedCalculations)
            {
                if (_calc_to_exclude != null && _calc_to_exclude.ID == c.ID) continue;

                foreach (var entry in c.InputParams)
                {
                    if (entry.Value == null) continue;
                    piic.Add(entry.Value.ID);
                }
                foreach (var entry in c.ReturnParams)
                {
                    if (entry.Value == null) continue;
                    piic.Add(entry.Value.ID);
                }
            }
            return piic;
        }

        internal bool ParamIsInvolvedInCalcs(Parameter.Parameter _p, Calculation _calc_to_exclude = null)
        {
            if (_p == null) return false;

            if (this.params_involved_in_calculations == null)
                this.GatherParamsInvolvedInCalcs();

            // added 20.09.2017
            List<long> piic = this.params_involved_in_calculations;
            if (_calc_to_exclude != null)
                piic = this.GatherParamsInvolvedInCalcs(_calc_to_exclude);

            return piic.Contains(_p.ID);
        }

        internal List<Calculation> GetCalculationsInvolving(Parameter.Parameter _p)
        {
            List<Calculation> calcs = new List<Calculation>();
            if (_p == null) return calcs;

            foreach (Calculation c in this.ContainedCalculations)
            {
                foreach (var entry in c.InputParams)
                {
                    if (entry.Value.ID == _p.ID) 
                        calcs.Add(c);
                }
                foreach (var entry in c.ReturnParams)
                {
                    if (entry.Value.ID == _p.ID)
                        calcs.Add(c);
                }
            }

            return calcs;
        }

        /// <summary>
        /// Looks on all hierarchical levels. Used in component to component mapping.
        /// </summary>
        /// <param name="_p"></param>
        /// <returns></returns>
        internal Calculation GetCalculationOutputtingTo(Parameter.Parameter _p)
        {
            if (_p == null) return null;
            foreach (Calculation c in this.ContainedCalculations)
            {
                Parameter.Parameter found = c.ReturnParams.Values.FirstOrDefault(x => x.ID == _p.ID);
                if (found != null)
                    return c;
            }
            foreach(var entry in this.ContainedComponents)
            {
                Component sComp = entry.Value;
                if (sComp!= null)
                {
                    Calculation sComp_c = sComp.GetCalculationOutputtingTo(_p);
                    if (sComp_c != null)
                        return sComp_c;
                }
            }

            return null;
        }

        /// <summary>
        /// Looks for the input parameter on all sub-component levels. Used in component to component mapping.
        /// </summary>
        /// <param name="_p_result"></param>
        /// <returns></returns>
        public List<Parameter.Parameter> GetInputParamsInvolvedInTheCalculationOf(Parameter.Parameter _p_result)
        {
            List<Parameter.Parameter> input = new List<Parameter.Parameter>();
            if (_p_result == null) return input;
            if (_p_result.Propagation != InfoFlow.OUPUT && _p_result.Propagation != InfoFlow.MIXED) return input;

            Calculation direct_calc = this.GetCalculationOutputtingTo(_p_result);
            if (direct_calc == null) return input;

            HashSet<Parameter.Parameter> unique_input = new HashSet<Parameter.Parameter>();
            foreach(var entry in direct_calc.InputParams)
            {
                Parameter.Parameter p = entry.Value;
                if (p != null)
                {
                    if (p.Propagation == InfoFlow.INPUT)
                    {
                        unique_input.Add(p);
                    }
                    else if (p.Propagation == InfoFlow.MIXED)
                    {
                        // look for a calculation that outputs to it
                        List<Parameter.Parameter> input_for_p = this.GetInputParamsInvolvedInTheCalculationOf(p);
                        if (input_for_p.Count > 0)
                        {
                            foreach (Parameter.Parameter ip in input_for_p)
                            {
                                unique_input.Add(ip);
                            }
                        }
                        else
                        {
                            unique_input.Add(p);
                        }
                    }
                }                    
            }

            return unique_input.ToList();
        }

        public void ExecuteCalculationChain()
        {
            foreach(Calculation c in this.ContainedCalculations)
            {
                c.PerformCalculation();
            }
        }

        public void ExecuteAllCalculationChains()
        {
            foreach(var entry in this.ContainedComponents)
            {
                Component sC = entry.Value;
                if (sC == null) continue;

                sC.ExecuteAllCalculationChains();
            }

            this.ExecuteCalculationChain();
        }

        #endregion

        #region METHODS: Calculation Copying

        public bool AddCopyOfCalculation(Calculation _calc, ComponentManagerType _user)
        {
            // check if the user has writing access
            bool success = this.RecordWritingAccess(_user);
            if (!success) return false;

            // copy calculation
            if (_calc == null) return false;
            if (this.ContainedCalculations == null) return false;

            
            // construct the new parameter lists with the OWN parameters that correspond to the parameters of the calculation
            // works recursively
            ObservableConcurrentDictionary<string, Parameter.Parameter> params_in_new = new ObservableConcurrentDictionary<string, Parameter.Parameter>();
            ObservableConcurrentDictionary<string, Parameter.Parameter> params_out_new = new ObservableConcurrentDictionary<string, Parameter.Parameter>();
            foreach (var entry in _calc.InputParams)
            {
                if (entry.Value == null) continue;

                Parameter.Parameter p = this.GetCorresponding(entry.Value);
                if (p == null) return false;

                params_in_new.Add(entry.Key, p);
            }

            foreach (var entry in _calc.ReturnParams)
            {
                if (entry.Value == null) continue;

                Parameter.Parameter p = this.GetCorresponding(entry.Value);
                if (p == null) return false;

                params_out_new.Add(entry.Key, p);
            }

            // create the new calculation
            Calculation calc_new = new Calculation(_calc.Expression, _calc.Name, params_in_new, params_out_new);
            
            CalculationState result = CalculationState.VALID;
            this.TestAndSaveCalculationInEditMode(_user, calc_new, ref result);

            return (result == CalculationState.VALID);
        }


        #endregion

        #region METHODS: Subcomponents Management

        // call before 'AddSubComponent(string, Component)'
        public bool AddSubComponentSlot(string _slot, ComponentManagerType _user)
        {
            // check if the user has writing access
            bool success = this.RecordWritingAccess(_user);
            if (!success) return false;

            // null or empty string slots not allowed
            if (string.IsNullOrEmpty(_slot)) return false;

            // cannot have the same slot twice
            if (this.ContainedComponents.ContainsKey(_slot)) return false;

            // add slot
            this.ContainedComponents.Add(_slot, null);
            return true;
        }

        // call after 'AddSubComponentSlot(string)'
        public bool AddSubComponent(string _slot, Component _sComp, ComponentManagerType _user)
        {
            // check if the user has writing access
            bool success = this.RecordWritingAccess(_user);
            if (!success) return false;

            // cannot assign self as SubComponent 
            if (Object.ReferenceEquals(this, _sComp)) return false;

            // cannot create a NULL SubComponent
            if (Object.ReferenceEquals(_sComp, null)) return false;

            // cannot take same SubComponent twice
            List<Component> sub_comps_flat = this.GetFlatSubCompList();
            if (sub_comps_flat.Contains(_sComp)) return false;

            // cannot take a referenced component
            if (this.ReferencedComponents.Values.Contains(_sComp)) return false;

            // check if the slot is valid
            if (!(this.ContainedComponents.ContainsKey(_slot))) return false;

            // add component to SELF
            _sComp.PropertyChanged += subComp_PropertyChanged;
            this.ContainedComponents[_slot] = _sComp;
            return true;
        }

        internal bool RemovalOfCompAdmissible()
        {
            // check, if any of its parameters are involved in calculations
            if (this.Factory != null)
            {
                Dictionary<long, Parameter.Parameter> allP = this.GetFlatParamsList();
                foreach (var entry in allP)
                {
                    Parameter.Parameter p = entry.Value;
                    if (p == null) continue;

                    ParameterDeleteResult test = this.Factory.RemoveParameterFromComponentTest(p, this);
                    if (test != ParameterDeleteResult.SUCCESS)
                    {
                        MessageBox.Show("Cannot delete'" + this.Name + "'. Contains parameters bound in a calculation in a parent component.", "Deleting Component", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }
                }
            }

            return true;
        }

        // removes both the subcomponent and/or the slot in this component
        public bool RemoveSubComponent_Level0(string _slot, bool _remove_slot, ComponentManagerType _user)
        {
            // check if the user has writing access
            bool success = this.RecordWritingAccess(_user);
            if (!success) return false;

            if (string.IsNullOrEmpty(_slot)) return false;
            if (!(this.ContainedComponents.ContainsKey(_slot))) return false;

            Component sComp = this.ContainedComponents[_slot];
            if (sComp != null)
            {
                if (!(sComp.RemovalOfCompAdmissible()))
                    return false; // added 11.07.2017

                sComp.PropertyChanged -= subComp_PropertyChanged;
                sComp.UnbindFormNWElements(); // added 31.10.2016
                sComp.RemoveReferenceFromAllComponents(_user); // added 19.05.2017
            }

            if (_remove_slot)
                return this.ContainedComponents.Remove(_slot);
            else
            {
                this.ContainedComponents[_slot] = null;
                return true;
            }
        }

        // removes both the subcomponent and/or the slot in this component 
        // or in any of its subcomponents
        public bool RemoveSubComponent(Component _sComp, bool _remove_slot, ComponentManagerType _user)
        {
            if (_sComp == null) return false;
            bool succes_level0 = this.RemoveSubComponent_Level0(_sComp, _remove_slot, _user);
            if (succes_level0)
            {
                return true;
            }
            else
            {
                foreach (var entry in this.ContainedComponents)
                {
                    Component c = entry.Value;
                    if (c == null) continue;

                    bool succes_levelN = c.RemoveSubComponent(_sComp, _remove_slot, _user);
                    if (succes_levelN)
                        return true;
                }
                return false;
            }
        }

        // removes both the subcomponent and/or the slot in this component 
        private bool RemoveSubComponent_Level0(Component _sComp, bool _remove_slot, ComponentManagerType _user)
        {
            // check if the user has writing access
            bool success = this.RecordWritingAccess(_user);
            if (!success) return false;

            if (_sComp == null) return false;
            
            string key_to_remove = string.Empty;
            foreach(var entry in this.ContainedComponents)
            {
                Component c = entry.Value;
                if (c == null) continue;

                if (c.ID == _sComp.ID)
                {
                    key_to_remove = entry.Key;
                    break;
                }
            }

            if (string.IsNullOrEmpty(key_to_remove)) return false;

            if (!(_sComp.RemovalOfCompAdmissible())) return false; // added 11.07.2017

            _sComp.UnbindFormNWElements(); // added 31.10.2016
            _sComp.RemoveReferenceFromAllComponents(_user); // added 19.05.2017
            _sComp.PropertyChanged -= subComp_PropertyChanged;

            if (_remove_slot)
                return this.ContainedComponents.Remove(key_to_remove);
            else
            {
                this.ContainedComponents[key_to_remove] = null;
                return true;
            }
        }       

        public bool RenameSubComponentSlot_Level0(string _slot_old, string _slot_new, ComponentManagerType _user)
        {
            // check if the user has writing access
            bool success = this.RecordWritingAccess(_user);
            if (!success) return false;

            if (string.IsNullOrEmpty(_slot_old)) return false;
            if (string.IsNullOrEmpty(_slot_new)) return false;
            if (!(this.ContainedComponents.ContainsKey(_slot_old))) return false;

            Component sComp = this.ContainedComponents[_slot_old];
            bool success_0 = this.RemoveSubComponent_Level0(_slot_old, true, _user);
            if (success_0)
            {
                bool success_1 = this.AddSubComponentSlot(_slot_new, _user);
                if (success_1 && sComp != null)
                {
                    bool success_2 = this.AddSubComponent(_slot_new, sComp, _user);
                    if (success_2)
                        return true;
                }
            }

            return false;
        }

        public List<Component> GetFlatSubCompList()
        {
            if (this.ContainedComponents == null || this.ContainedComponents.Count() < 1)
                return new List<Component>();

            List<Component> list = new List<Component>();
            foreach (Component n in this.ContainedComponents.Values)
            {
                if (n == null) continue;
                list.Add(n);
                if (n.ContainedComponents.Count() > 0)
                    list.AddRange(n.GetFlatSubCompList());
            }
            return list;
        }

        public static List<Component> GetFlattenedListOf(List<Component> _comps)
        {
            List<Component> all_c = new List<Component>();
            if (_comps == null || _comps.Count == 0) return all_c;

            foreach(Component c in _comps)
            {
                all_c.Add(c);
                all_c.AddRange(c.GetFlatSubCompList());
            }

            return all_c;
        }

        // added 31.08.2016
        protected List<KeyValuePair<string, Component>> GetSortedFlatSubCompWSlotNameList(string _prefix)
        {
            List<KeyValuePair<string, Component>> list = new List<KeyValuePair<string, Component>>();
            if (this.ContainedComponents == null || this.ContainedComponents.Count() < 1)
                return list;

            SortedList<string, Component> copy_sComps = new SortedList<string, Component>(this.ContainedComponents);
            foreach (var entry in copy_sComps)
            {
                Component comp = entry.Value;
                if (comp == null) continue;
                if (comp.ApplyIsExcludedFromDisplay && comp.IsExcludedFromDisplay) continue; // added 21.08.2017

                string name = (string.IsNullOrEmpty(_prefix)) ? string.Empty : (_prefix + "->");
                name += entry.Key + ": " + comp.Name + "[" + comp.Description + "]";
                string prefix_next = (string.IsNullOrEmpty(_prefix)) ? entry.Key : (_prefix + "->" + entry.Key);

                list.Add(new KeyValuePair<string, Component>(name, comp));
                if (comp.ContainedComponents.Count() > 0)
                {
                    List<KeyValuePair<string, Component>> sList = comp.GetSortedFlatSubCompWSlotNameList(prefix_next);
                    list.AddRange(sList);
                }
            }

            return list;
        }

        #endregion

        #region METHODS: Referenced Components Management

        // call before 'AddReferencedComponent(string, Component)'
        public bool AddReferencedComponentSlot(string _slot, ComponentManagerType _user)
        {
            // check if the user has writing access
            bool success = this.RecordWritingAccess(_user);
            if (!success) return false;

            // perform adding
            return this.AddReferencedComponentSlot(_slot);
        }

        internal bool AddReferencedComponentSlot(string _slot)
        {
            // null or empty string slots not allowed
            if (string.IsNullOrEmpty(_slot)) return false;

            // cannot have the same slot twice
            if (this.ReferencedComponents.ContainsKey(_slot)) return false;

            // add slot
            this.ReferencedComponents.Add(_slot, null);
            return true;
        }

        // call after 'AddReferencedComponentSlot(string)'
        public bool AddReferencedComponent(string _slot, Component _rComp, ComponentManagerType _user)
        {
            // check if the user has writing access
            bool success = this.RecordWritingAccess(_user);
            if (!success) return false;

            // perform operation
            if (_rComp != null)
                _rComp.PropertyChanged += refComp_PropertyChanged; // added 22.08.2016
            return this.AddReferencedComponent(_slot, _rComp);
        }

        internal bool AddReferencedComponent(string _slot, Component _rComp)
        {
            // cannot reference self 
            if (Object.ReferenceEquals(this, _rComp)) return false;

            // cannot reference NULL
            if (Object.ReferenceEquals(_rComp, null)) return false;

            // cannot reference the same Component twice
            if (this.ReferencedComponents.Values.Contains(_rComp)) return false;

            // cannot reference a SubComponent
            List<Component> sub_comps_flat = this.GetFlatSubCompList();
            if (sub_comps_flat.Contains(_rComp)) return false;

            // check if the slot is valid
            if (!(this.ReferencedComponents.ContainsKey(_slot))) return false;

            // add component to SELF (Add and Remove used because of Collection Handler)
            // this.ReferencedComponents[_slot] = _rComp; // OLD
            _rComp.ReferencedBy.Add(this);
            if (this.ReferencedComponents[_slot] != null) // added 12.09.2016
            {
                this.ReferencedComponents[_slot].PropertyChanged -= refComp_PropertyChanged;
                this.ReferencedComponents[_slot].ReferencedBy.Remove(this);
            }
            this.ReferencedComponents.Remove(_slot);
            if (_rComp != null)
                _rComp.PropertyChanged += refComp_PropertyChanged; // added 22.08.2016
            this.ReferencedComponents.Add(_slot, _rComp);
            this.PropagateRefParamValue(_rComp);
            return true;
        }

        // removes both the component and/or the slot from this component
        public bool RemoveReferencedComponent_Level0(string _slot, bool _remove_slot, ComponentManagerType _user)
        {
            // check if the user has writing access
            bool success = this.RecordWritingAccess(_user);
            if (!success) return false;

            if (string.IsNullOrEmpty(_slot)) return false;
            if (!(this.ReferencedComponents.ContainsKey(_slot))) return false;

            if (this.ReferencedComponents[_slot] != null)
            {
                this.ReferencedComponents[_slot].PropertyChanged -= refComp_PropertyChanged; // added 22.08.2016
                this.ReferencedComponents[_slot].ReferencedBy.Remove(this); // added 12.09.2016
            }

            if(_remove_slot)
                return this.ReferencedComponents.Remove(_slot);
            else
            {
                this.ReferencedComponents[_slot] = null;
                return true;
            }
        }

        // removes both the slot and/or the component from this component
        internal bool RemoveReferencedComponent_Level0(Component _rComp, bool _remove_slot, ComponentManagerType _user)
        {
            // check if the user has writing access
            bool success = this.RecordWritingAccess(_user);
            if (!success) return false;

            if (_rComp == null) return false;

            string key_to_remove = string.Empty;
            Component comp_to_remove = null;
            foreach (var entry in this.ReferencedComponents)
            {
                Component c = entry.Value;
                if (c == null) continue;

                if (c.ID == _rComp.ID)
                {
                    key_to_remove = entry.Key;
                    comp_to_remove = c;
                    break;
                }
            }

            if (string.IsNullOrEmpty(key_to_remove)) return false;

            if (comp_to_remove != null)
            {
                comp_to_remove.ReferencedBy.Remove(this);
                comp_to_remove.PropertyChanged -= refComp_PropertyChanged; // added statement 22.08.2016
            }

            if (_remove_slot)
            {               
                return this.ReferencedComponents.Remove(key_to_remove);
            }
            else
            {
                this.ReferencedComponents[key_to_remove] = null;
                return true;
            }
        }

        public bool RenameReferencedComponentSlot_Level0(string _slot_old, string _slot_new, ComponentManagerType _user)
        {
            // check if the user has writing access
            bool success = this.RecordWritingAccess(_user);
            if (!success) return false;

            if (string.IsNullOrEmpty(_slot_old)) return false;
            if (string.IsNullOrEmpty(_slot_new)) return false;
            if (!(this.ReferencedComponents.ContainsKey(_slot_old))) return false;

            Component rComp = this.ReferencedComponents[_slot_old];
            bool success_0 = this.RemoveReferencedComponent_Level0(_slot_old, true, _user);
            if (success_0)
            {
                bool success_1 = this.AddReferencedComponentSlot(_slot_new, _user);
                if (success_1 && rComp != null)
                {
                    bool success_2 = this.AddReferencedComponent(_slot_new, rComp, _user);
                    if (success_2)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// To be called before DELETING this component. Removes the references and mappings to it and its sub-components from all others.
        /// </summary>
        /// <param name="_user"></param>
        /// <returns></returns>
        internal bool RemoveReferenceFromAllComponents(ComponentManagerType _user)
        {
            bool success = true;

            // remove references to self
            List<Component> to_remove = new List<Component>(this.ReferencedBy);
            foreach (Component c in to_remove)
            {
                success &= c.RemoveReferencedComponent_Level0(this, false, _user);
            }
            // added 29.08.2017
            // remove mappings to self
            List<Component> m_to_remove = new List<Component>(this.MappedToBy);
            foreach (Component c in m_to_remove)
            {
                success &= c.RemoveMappingTo(this);
            }

            // remove references and mappings to subcomponents
            foreach(var entry in this.ContainedComponents)
            {
                Component sc = entry.Value;
                if (sc == null) continue;
                success &= sc.RemoveReferenceFromAllComponents(_user);
            }

            return success;
        }

        // info (added 12.09.2016)
        // stackoverflow when circular references present
        internal List<Component> GetAllRefCompsOfRefComps()
        {
            Dictionary<long, Component> found = new Dictionary<long, Component>();

            foreach(var entry in this.ReferencedComponents)
            {
                Component rComp = entry.Value;
                if (rComp == null) continue;

                if (!found.ContainsKey(rComp.ID))
                    found.Add(rComp.ID, rComp);

                List<Component> rComp_found = rComp.GetAllRefCompsOfRefComps();
                foreach(Component rrComp in rComp_found)
                {
                    if (!found.ContainsKey(rrComp.ID))
                        found.Add(rrComp.ID, rrComp);
                }
            }

            return found.Values.ToList();
        }

        // 07.09.2017
        // robust even if circular referencing present
        internal void GetAllRefCompsOfRefComps(ref List<Component> _found_so_far)
        {
            if (_found_so_far == null)
                _found_so_far = new List<Component>();

            foreach (var entry in this.ReferencedComponents)
            {
                Component rComp = entry.Value;
                if (rComp == null) continue;

                Component duplicate = _found_so_far.FirstOrDefault(x => x.ID == rComp.ID);
                if (duplicate != null) continue;

                _found_so_far.Add(rComp);
                rComp.GetAllRefCompsOfRefComps(ref _found_so_far);
            }
        }

        internal bool HasReferencedComp_Level0(long _id)
        {
            foreach(var entry in this.ReferencedComponents)
            {
                Component rComp = entry.Value;
                if (rComp == null) continue;

                if (rComp.ID == _id)
                    return true;
            }

            return false;
        }

        #endregion

        #region METHODS: Parent Components

        public List<Component> FindCommonRootsOf(List<Component> _comps)
        {
            if (this.Factory == null) return new List<Component>();

            return this.Factory.FindCommonRootsOf(_comps);
        }

        #endregion

        #region METHODS: Synchronization w Template

        // compares component to TEMPLATE according to the parameters: if THIS has all the same parameters with 
        // the same name, unit, category and propagation, EXCEPT ONE -> synchronize by adding the missing one
        // does not check other properties of the component!
        // TO BE USED: when a new parameter is added to a template component
        // and the change is to be propagated to all that were created as copies of the template
        internal bool SynchOneParameter(Component _c_template)
        {
            if (_c_template == null) return false;

            if (_c_template.NrParameters != this.NrParameters + 1) return false;

            Parameter.Parameter p_new = null;
            int nr_diff = 0;
            foreach (var entry_T in _c_template.ContainedParameters)
            {
                Parameter.Parameter p_T = entry_T.Value;
                if (p_T == null) continue;

                bool found_same = false;
                foreach(var entry in this.ContainedParameters)
                {
                    Parameter.Parameter p = entry.Value;
                    if (p == null) continue;

                    if (p_T.Name == p.Name && p_T.Unit == p.Unit && p_T.Category == p.Category && p_T.Propagation == p.Propagation)
                    {
                        found_same = true;
                        break;
                    }
                }
                if (!found_same)
                {
                    nr_diff++;                    
                    if (nr_diff > 1)
                        return false;

                    p_new = p_T;
                }
            }

            if (p_new != null)
            {
                // copy and add to own parameters
                Parameter.Parameter p_copy = p_new.Clone();
                p_copy.PropertyChanged += param_PropertyChanged;
                this.ContainedParameters.Add(p_copy.ID, p_copy);
                return true;
            }

            return false;
        }


        #endregion

        #region METHODS: To String

        public override string ToString()
        {
            string output = (this.ApplyIsExcludedFromDisplay && this.IsExcludedFromDisplay) ? "OFF " : "ON ";
            output += (this.IsHidden) ? "h" : "v";
            output += " " + this.ID + ": " + this.Name + "( " + this.Description + " ) ";
            output += "\n[" + ComponentUtils.CategoryToString(this.Category) + "] ";
            output += "sub-comp {" + this.ContainedComponents.Count() + "} ";
            output += "ref-comp {" + this.ReferencedComponents.Count() + "} ";
            output += "params {" + this.ContainedParameters.Count() + "}";

            return output;
        }

        public string ToInfoString()
        {
            return this.CurrentSlot + ": {" + this.ID + "}" + this.Name + " - " + this.Description;
        }

        public virtual void AddToExport(ref StringBuilder _sb, string _key = null)
        {
            if (_sb == null) return;
            string tmp = null;

            // added 01.09.2017
            // reset the default values for components serving as calculator for other components
            if (this.MappedToBy.Count > 0)
            {
                this.ResetToDefaultValuesBeforeCalculation();
            }

            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.COMPONENT);                               // COMPONENT

            if (!(string.IsNullOrEmpty(_key)))
            {
                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_KEY).ToString());
                _sb.AppendLine(_key);
            }

            _sb.AppendLine(((int)ParamStructCommonSaveCode.CLASS_NAME).ToString());
            _sb.AppendLine(this.GetType().ToString());

            // general
            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_ID).ToString());
            _sb.AppendLine(this.ID.ToString());

            _sb.AppendLine(((int)ComponentSaveCode.NAME).ToString());
            _sb.AppendLine(this.Name);

            _sb.AppendLine(((int)ComponentSaveCode.DESCRIPTION).ToString());
            _sb.AppendLine(this.Description);

            _sb.AppendLine(((int)ComponentSaveCode.GENERATED_AUTOMATICALLY).ToString());
            tmp = (this.IsAutomaticallyGenerated) ? "1" : "0";
            _sb.AppendLine(tmp);

            // management
            _sb.AppendLine(((int)ComponentSaveCode.CATEGORY).ToString());
            _sb.AppendLine(ComponentUtils.CategoryToString(this.Category));

            this.AccessLocal.AddToExport(ref _sb, false);

            _sb.AppendLine(((int)ComponentSaveCode.FUNCTION_SLOTS_ALL).ToString());
            _sb.AppendLine(this.FitsInSlots.Count.ToString());
            foreach(string s in this.FitsInSlots)
            {
                _sb.AppendLine(((int)ParamStructCommonSaveCode.STRING_VALUE).ToString());
                _sb.AppendLine(s);
            }

            _sb.AppendLine(((int)ComponentSaveCode.FUNCTION_SLOT_CURRENT).ToString());
            _sb.AppendLine(this.CurrentSlot);

            // CONTAINED COMPONENTS
            if (this.ContainedComponents.Count() == 0)
            {
                _sb.AppendLine(((int)ComponentSaveCode.CONTAINED_COMPONENTS).ToString());
                _sb.AppendLine("0");
                _sb.AppendLine(((int)ComponentSaveCode.CONTAINED_COMPONENT_SLOTS).ToString());
                _sb.AppendLine("0");
            }
            else
            {
                int nr_sComp_full = this.ContainedComponents.Where(x => x.Value != null).Count();
                int nr_sComp_empty = this.ContainedComponents.Count() - nr_sComp_full;
                
                // -- save the actual components
                _sb.AppendLine(((int)ComponentSaveCode.CONTAINED_COMPONENTS).ToString());
                _sb.AppendLine(nr_sComp_full.ToString());

                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
                _sb.AppendLine(ParamStructTypes.ENTITY_SEQUENCE);                         // ENTSEQ

                foreach(var entry in this.ContainedComponents)
                {
                    string slot = entry.Key;
                    Component sComp = entry.Value;
                    if (sComp != null)
                        sComp.AddToExport(ref _sb, slot);                    
                }

                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
                _sb.AppendLine(ParamStructTypes.SEQUENCE_END);                            // SEQEND
                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
                _sb.AppendLine(ParamStructTypes.ENTITY_CONTINUE);                         // ENTCTN

                // -- save the empty slots
                _sb.AppendLine(((int)ComponentSaveCode.CONTAINED_COMPONENT_SLOTS).ToString());
                _sb.AppendLine(nr_sComp_empty.ToString());

                foreach(var entry in this.ContainedComponents)
                {
                    if (entry.Value == null)
                    {
                        // just save the slots
                        _sb.AppendLine(((int)ParamStructCommonSaveCode.STRING_VALUE).ToString());
                        _sb.AppendLine(entry.Key);
                    }
                }
            }

            // REFERENCED COMPONENTS
            _sb.AppendLine(((int)ComponentSaveCode.REFERENCED_COMPONENTS).ToString());
            _sb.AppendLine(this.ReferencedComponents.Count().ToString());

            foreach(var entry in this.ReferencedComponents)
            {
                // save the slot
                _sb.AppendLine(((int)ParamStructCommonSaveCode.STRING_VALUE).ToString());
                _sb.AppendLine(entry.Key);
                // save the component id
                _sb.AppendLine(((int)ParamStructCommonSaveCode.X_VALUE).ToString());
                long id = (entry.Value == null) ? -1 : entry.Value.ID;
                _sb.AppendLine(id.ToString());
            }

            // CONTAINED PARAMETERS
            _sb.AppendLine(((int)ComponentSaveCode.CONTAINED_PARAMETERS).ToString());
            _sb.AppendLine(this.ContainedParameters.Count().ToString());

            if (this.ContainedParameters.Count() > 0)
            {
                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
                _sb.AppendLine(ParamStructTypes.ENTITY_SEQUENCE);                         // ENTSEQ

                foreach (var entry in this.ContainedParameters)
                {
                    if (entry.Value == null) continue;
                    entry.Value.AddToExport(ref _sb);
                }

                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
                _sb.AppendLine(ParamStructTypes.SEQUENCE_END);                            // SEQEND
                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
                _sb.AppendLine(ParamStructTypes.ENTITY_CONTINUE);                         // ENTCTN
            }

            // CONTAINED CALCULATIONS
            _sb.AppendLine(((int)ComponentSaveCode.CONTAINED_CALCULATIONS).ToString());
            _sb.AppendLine(this.ContainedCalculations.Count.ToString());

            if (this.ContainedCalculations.Count > 0)
            {
                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
                _sb.AppendLine(ParamStructTypes.ENTITY_SEQUENCE);                         // ENTSEQ

                foreach(Calculation calc in this.ContainedCalculations)
                {
                    if (calc == null) continue;
                    calc.AddToExport(ref _sb);
                }

                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
                _sb.AppendLine(ParamStructTypes.SEQUENCE_END);                            // SEQEND
                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
                _sb.AppendLine(ParamStructTypes.ENTITY_CONTINUE);                         // ENTCTN
            }

            // GEOMETRIC RELATIONSHIPS (added 15.11.2016)
            _sb.AppendLine(((int)ComponentSaveCode.RELATIONSHIPS_TO_GEOMETRY).ToString());
            _sb.AppendLine(this.R2GInstances.Count.ToString());

            if (this.R2GInstances.Count > 0)
            {
                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
                _sb.AppendLine(ParamStructTypes.ENTITY_SEQUENCE);                         // ENTSEQ

                foreach(GeometricRelationship gr in this.R2GInstances)
                {
                    if (gr == null) continue;
                    gr.AddToExport(ref _sb);
                }

                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
                _sb.AppendLine(ParamStructTypes.SEQUENCE_END);                            // SEQEND
                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
                _sb.AppendLine(ParamStructTypes.ENTITY_CONTINUE);                         // ENTCTN
            }

            // MAPPINGS TO COMPONENTS (added 30.08.2017)
            _sb.AppendLine(((int)ComponentSaveCode.MAPPINGS_TO_COMPONENTS).ToString());
            _sb.AppendLine(this.mappings_to_comps.Count.ToString());

            if (this.mappings_to_comps.Count > 0)
            {
                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
                _sb.AppendLine(ParamStructTypes.ENTITY_SEQUENCE);                         // ENTSEQ

                foreach(Mapping.Mapping2Component m in this.mappings_to_comps)
                {
                    if (m == null) continue;
                    m.AddToExport(ref _sb);
                }

                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
                _sb.AppendLine(ParamStructTypes.SEQUENCE_END);                            // SEQEND
                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
                _sb.AppendLine(ParamStructTypes.ENTITY_CONTINUE);                         // ENTCTN
            }

            // time stamp
            _sb.AppendLine(((int)ComponentSaveCode.TIME_STAMP).ToString());
            _sb.AppendLine(this.TimeStamp.ToString(ParamStructTypes.DT_FORMATTER));

            // symbol
            _sb.AppendLine(((int)ComponentSaveCode.SYMBOL_ID).ToString());
            _sb.AppendLine(this.SymbolId.ToString());

            // signify end of complex entity
            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.SEQUENCE_END);                            // SEQEND
                
        }

        #endregion

        #region METHODS: Utils (Category Accumulation, Referenced Parameters Value Propagation)

        protected void GatherCategoryInfo()
        {
            Category newC = Category.NoNe;
            if (this.ContainedComponents != null)
            {
                foreach(var entry in this.ContainedComponents)
                {
                    if (entry.Value == null) continue;
                    newC |= entry.Value.Category;
                }
            }
            if (this.ContainedParameters != null)
            {
                foreach(var entry in this.ContainedParameters)
                {
                    if (entry.Value == null) continue;
                    newC |= entry.Value.Category;
                }
            }
            this.Category = newC;
        }

        // if a parameter changes -> propagate the change to all who reference it
        protected void PropagateRefParamValue(Parameter.Parameter _p)
        {
            // follow references up the parent chain
            List<Component> parent_chain = new List<Component> {this};
            if (this.Factory != null)
                parent_chain = this.Factory.GetParentComponentChain(this);

            List<Component> comps_referencing_this_or_parent = new List<Component>();
            foreach(Component pC in parent_chain)
            {
                foreach(Component rpC in pC.ReferencedBy)
                {
                    if (!(comps_referencing_this_or_parent.Contains(rpC)))
                        comps_referencing_this_or_parent.Add(rpC);
                }
            }

            // look for referencing parameters
            foreach (Component c in comps_referencing_this_or_parent)
            {
                ICollection<Parameter.Parameter> cPs = c.GetFlatParamsList().Values;
                // direct reference
                List<Parameter.Parameter> to_synch = cPs.Where(x => x != null && x.Propagation == InfoFlow.REF_IN && x.ValueField == null && x.Name == _p.Name).ToList();
                if (to_synch != null && to_synch.Count > 0)
                {
                    foreach (Parameter.Parameter cP in to_synch)
                    {
                        if (cP.ValueCurrent != _p.ValueCurrent)
                            cP.ValueCurrent = _p.ValueCurrent;
                        if (cP.TextValue != _p.TextValue)
                            cP.TextValue = _p.TextValue;
                    }
                }

                // reference as a minimum value
                if (_p.Name.EndsWith("MIN"))
                {
                    string p_name_only = _p.Name.Substring(0, _p.Name.Length - 3);
                    List<Parameter.Parameter> to_synch_min = cPs.Where(x => x != null && x.Name == p_name_only).ToList();
                    if (to_synch_min != null && to_synch_min.Count > 0)
                    {
                        foreach (Parameter.Parameter cP in to_synch_min)
                        {
                            if (cP.ValueMin != _p.ValueCurrent)
                                cP.ValueMin = _p.ValueCurrent;
                        }
                    }
                }

                // reference as a maximum value
                if (_p.Name.EndsWith("MAX"))
                {
                    string p_name_only = _p.Name.Substring(0, _p.Name.Length - 3);
                    List<Parameter.Parameter> to_synch_max = cPs.Where(x => x != null && x.Name == p_name_only).ToList();
                    if (to_synch_max != null && to_synch_max.Count > 0)
                    {
                        foreach (Parameter.Parameter cP in to_synch_max)
                        {
                            if (cP.ValueMax != _p.ValueCurrent)
                                cP.ValueMax = _p.ValueCurrent;
                        }
                    }
                }
            }

            
        }

        // if a parameter changes -> propagate the change to all who reference it
        protected void PropagateRefParamValue(Component _rComp)
        {
            if (_rComp == null) return;

            Dictionary<long, Parameter.Parameter> all_contained_params = _rComp.GetFlatParamsList();
            foreach (var entry in all_contained_params)
            {
                Parameter.Parameter p = entry.Value;
                if (p == null) continue;

                ICollection<Parameter.Parameter> all_params = this.GetFlatParamsList().Values;

                // direct reference
                List<Parameter.Parameter> to_synch = all_params.Where(x => x != null && x.Propagation == InfoFlow.REF_IN && x.ValueField == null && x.Name == p.Name).ToList();
                if (to_synch != null && to_synch.Count > 0)
                {
                    foreach (Parameter.Parameter cP in to_synch)
                    {
                        if (cP.ValueCurrent != p.ValueCurrent)
                            cP.ValueCurrent = p.ValueCurrent;
                        if (cP.TextValue != p.TextValue)
                            cP.TextValue = p.TextValue;
                    }
                }

                // reference as a minimum value
                if (p.Name.EndsWith("MIN"))
                {
                    string p_name_only = p.Name.Substring(0, p.Name.Length - 3);
                    List<Parameter.Parameter> to_synch_min = all_params.Where(x => x != null && x.Name == p_name_only).ToList();
                    if (to_synch_min != null && to_synch_min.Count > 0)
                    {
                        foreach (Parameter.Parameter cP in to_synch_min)
                        {
                            if (cP.ValueMin != p.ValueCurrent)
                                cP.ValueMin = p.ValueCurrent;
                        }
                    }
                }

                // reference as a maximum value
                if (p.Name.EndsWith("MAX"))
                {
                    string p_name_only = p.Name.Substring(0, p.Name.Length - 3);
                    List<Parameter.Parameter> to_synch_max = all_params.Where(x => x != null && x.Name == p_name_only).ToList();
                    if (to_synch_max != null && to_synch_max.Count > 0)
                    {
                        foreach (Parameter.Parameter cP in to_synch_max)
                        {
                            if (cP.ValueMax != p.ValueCurrent)
                                cP.ValueMax = p.ValueCurrent;
                        }
                    }
                }
            }
        }

        // if a parameter changes its properties (name and / or propagation type)
        // look for parameters of the same name in referenced components and copy their value
        internal void AdoptRefParamValue(ref Parameter.Parameter _p)
        {
            if (_p == null) return;

            foreach (var entry in this.ReferencedComponents)
            {
                Component rComp = entry.Value;
                if (rComp == null) continue;

                Dictionary<long, Parameter.Parameter> all_contained_params = rComp.GetFlatParamsList();
                foreach (var pEntry in all_contained_params)
                {
                    Parameter.Parameter rP = pEntry.Value;
                    if (rP == null) continue;

                    if (rP.Name == _p.Name)
                    {
                        _p.ValueCurrent = rP.ValueCurrent;
                        _p.TextValue = rP.TextValue;
                    }
                }
            }
        }

        internal void AdoptLimitingRefParamValue(ref Parameter.Parameter _p)
        {
            if (_p == null) return;

            foreach (var entry in this.ReferencedComponents)
            {
                Component rComp = entry.Value;
                if (rComp == null) continue;

                Dictionary<long, Parameter.Parameter> all_contained_params = rComp.GetFlatParamsList();
                foreach (var pEntry in all_contained_params)
                {
                    Parameter.Parameter rP = pEntry.Value;
                    if (rP == null) continue;

                    if (rP.Name.EndsWith("MIN"))
                    {
                        if (rP.Name.Substring(0, rP.Name.Length - 3) == _p.Name)
                        {
                            _p.ValueMin = rP.ValueCurrent;
                        }
                    }
                    else if (rP.Name.EndsWith("MAX"))
                    {
                        if (rP.Name.Substring(0, rP.Name.Length - 3) == _p.Name)
                        {
                            _p.ValueMax = rP.ValueCurrent;
                        }
                    }
                }
            }
        }


        #endregion

        #region METHODS: Utils (Display)

        internal void ExpandComp()
        {
            this.IsExpanded = true;
            if (this.ContainedComponents.Count() > 0)
            {
                foreach (var entry in this.ContainedComponents)
                {
                    if (entry.Value == null) continue;
                    entry.Value.ExpandComp();
                }
            }
        }

        internal void CollapseComp()
        {
            this.IsExpanded = false;
            if (this.ContainedComponents.Count() > 0)
            {
                foreach (var entry in this.ContainedComponents)
                {
                    if (entry.Value == null) continue;
                    entry.Value.CollapseComp();
                }
            }
        }

        public Parameter.Parameter SelectFirstChildParameter()
        {
            List<Parameter.Parameter> own_params = this.ContainedParameters.Values.ToList();
            if (own_params != null && own_params.Count > 0)
            {
                return own_params[0];
            }
            else
            {
                foreach(var entry in this.ContainedComponents)
                {
                    Component sComp = entry.Value;
                    if (sComp == null) continue;

                    Parameter.Parameter sComp_fP = sComp.SelectFirstChildParameter();
                    if (sComp_fP != null)
                    {
                        return sComp_fP;
                    }
                }
            }
            return null;
        }

        #endregion

        #region EVENT HANDLERS

        private void subComp_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Component comp = sender as Component;
            if (comp == null || e == null) return;

            if (e.PropertyName == "ParamChildren")
            {
                this.UpdateParamChildrenContainer();
            }
            if (e.PropertyName == "CalcChildren")
            {
                this.UpdateCalcChildrenContainer();
            }
        }

        private void refComp_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Component comp = sender as Component;
            if (comp == null || e == null) return;

            if (e.PropertyName == "Name" || e.PropertyName == "Description")
            {
                this.UpdateChildrenContainer();
            }
        }

        private void param_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Parameter.Parameter p = sender as Parameter.Parameter;
            if (p == null || e == null) return;

            bool writing_occured = false;
            if (e.PropertyName == "ValueCurrent")
            {
                writing_occured = true;
                // propagate info to all components that reference this one
                this.PropagateRefParamValue(p);
                // update the Calculation that contains it
                if (this.Factory != null)
                    this.Factory.PassParameterChangeToCalculations(p, this, true);
                // update marker: added 20.10.2016
                this.PropagateParamBoundsToParent();
                // if a pointer parameter -> communicate with its value field: added 26.10.2016
                this.ExtractValueFromValueField(p);
            }
            else if (e.PropertyName == "TextValue")
            {
                writing_occured = true;
                // propagate info to all components that reference this one
                this.PropagateRefParamValue(p);
                // if a pointer parameter -> communicate with its value field: addded 26.10.2016
                this.ExtractValueFromValueField(p);
            }
            else if (e.PropertyName == "Category")
            {
                writing_occured = true;
                this.GatherCategoryInfo();
            }
            else if (e.PropertyName == "Name")
            {
                writing_occured = true;
                if (p.Propagation == InfoFlow.REF_IN && p.ValueField == null)
                {
                    // look for referenced components that have a parameter of the same name
                    // and copy its value
                    if (this.Factory == null)
                        this.AdoptRefParamValue(ref p);
                    else
                        this.Factory.AdoptRefParamValueFromParentChain(ref p, this);
                }
                // look for limiting referenced components
                if (this.Factory == null)
                    this.AdoptLimitingRefParamValue(ref p);
                else
                    this.Factory.AdoptLimitingRefParamValueFromParentChain(ref p, this);
                // update the Calculation that contains it
                if (this.Factory != null)
                    this.Factory.PassParameterChangeToCalculations(p, this);
            }
            else if (e.PropertyName == "Propagation" && p.Propagation == InfoFlow.REF_IN && p.ValueField == null)
            {
                writing_occured = true;
                // look for referenced components that have a parameter of the same name
                // and copy its value
                if (this.Factory == null)
                    this.AdoptRefParamValue(ref p);
                else
                    this.Factory.AdoptRefParamValueFromParentChain(ref p, this);
            }
            else if (e.PropertyName == "ShowInCompInstDisplay")
            {
                Dictionary<string, bool> param_display_info = this.ExtractParamDisplayInInstance();
                foreach(GeometricRelationship gr in this.R2GInstances)
                {
                    gr.DerivedInstanceParamDisplayState = param_display_info;
                }
            }

            // record writing access
            if (writing_occured && this.Factory != null)
            {
                this.RecordWritingAccess(this.Factory.Caller);
            }

        }

        private void calculation_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Calculation calc = sender as Calculation;
            if (calc == null || e == null) return;

            bool writing_occured = false; // added 24.08.2016
            if (e.PropertyName == "InputParams" || e.PropertyName == "ReturnParams")
            {
                this.GatherParamsInvolvedInCalcs();
                // record writing access
                if (this.Factory != null)
                    this.RecordWritingAccess(this.Factory.Caller);
                writing_occured = true;
            }

            if (e.PropertyName == "Name" || e.PropertyName == "Expression")
            {
                writing_occured = true;
            }

            // record writing access (added 24.08.2016)
            if (writing_occured && this.Factory != null)
            {
                this.RecordWritingAccess(this.Factory.Caller);
            }
        }

        private void main_r2g_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            GeometricRelationship gr = sender as GeometricRelationship;
            if (gr == null || e == null) return;

            if (e.PropertyName == "State")
            {
                this.R2GMainState = new Relation2GeomState { IsRealized = gr.State.IsRealized, Type = gr.State.Type };
            }
        }

        #endregion
    }
}
