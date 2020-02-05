using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Diagnostics;

using ParameterStructure.Parameter;
using ParameterStructure.DXF;

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
    public enum FlowNetworkSaveCode
    {
        CONTENT_ID = 1501,      // id of associated component, otherwise -1
        IS_VALID = 1502,
        POSITION_X = 1503,        
        POSITION_Y = 1504,
        START_NODE = 1505,      // only FlNetEdge: id of node
        END_NODE = 1506,        // only FlNetEdge: id of node
        NAME = 1507,
        DESCRIPTION = 1508,
        MANAGER = 1509,
        TIMESTAMP = 1510,
        CONTAINED_NODES = 1511, // only FlowNetwork: saved as DXF Entities
        CONTAINED_EDGES = 1512, // only FlowNetwork: saved as DXF Entities
        CONTIANED_NETW = 1513,  // only FlowNetwork: saved as DXF Entities
        NODE_SOURCE = 1514,     // only FlowNetwork (id of the start node)
        NODE_SINK = 1515,       // only FlowNetwork (id of the end node)
        CALC_RULES = 1516,
        CALC_RULE_SUFFIX_OPERANDS = 1517,
        CALC_RULE_SUFFIX_RESULT = 1518,
        CALC_RULE_DIRECTION = 1519,
        CALC_RULE_OPERATOR = 1520
    }

    public enum FlowNetworkOperator
    {
        ADDITION = 0,
        SUBTRACTION = 1,
        MULTIPLICATION = 2,
        DIVISION = 3,
        MINIMUM = 4,
        MAXIMUM = 5,
        ASSIGNMENT = 6
    }

    public enum FlowNetworkCalcDirection
    {
        FORWARD = 0,
        BACKWARD = 1
    }

    #endregion

    #region HELPER CLASSES

    public struct FlowNetworkCalcRule
    {
        public string Suffix_Result { get; set; }
        public string Suffix_Operands { get; set; }
        public FlowNetworkCalcDirection Direction { get; set; }
        public FlowNetworkOperator Operator { get; set; }

        #region STATIC

        public static string OperatorToString(FlowNetworkOperator _op)
        {
            switch(_op)
            {
                case FlowNetworkOperator.ADDITION:
                    return "+";
                case FlowNetworkOperator.SUBTRACTION:
                    return "-";
                case FlowNetworkOperator.MULTIPLICATION:
                    return "*";
                case FlowNetworkOperator.DIVISION:
                    return "/";
                case FlowNetworkOperator.MINIMUM:
                    return "Min";
                case FlowNetworkOperator.MAXIMUM:
                    return "Max";
                case FlowNetworkOperator.ASSIGNMENT:
                    return ":=";
                default:
                    return "+";
            }
        }

        public static FlowNetworkOperator StringToOperator(string _op_as_str)
        {
            if (string.IsNullOrEmpty(_op_as_str)) return FlowNetworkOperator.ADDITION;

            switch(_op_as_str)
            {
                case "+":
                    return FlowNetworkOperator.ADDITION;
                case "-":
                    return FlowNetworkOperator.SUBTRACTION;
                case "*":
                    return FlowNetworkOperator.MULTIPLICATION;
                case "/":
                    return FlowNetworkOperator.DIVISION;
                case "Min":
                    return FlowNetworkOperator.MINIMUM;
                case "Max":
                    return FlowNetworkOperator.MAXIMUM;
                case ":=":
                    return FlowNetworkOperator.ASSIGNMENT;
                default:
                    return FlowNetworkOperator.ADDITION;
            }
        }

        #endregion

        #region METHODS: Calculate

        public double Calculate(double _v1, double _v2)
        {
            switch(this.Operator)
            {
                case FlowNetworkOperator.ADDITION:
                    return _v1 + _v2;
                case FlowNetworkOperator.SUBTRACTION:
                    return _v1 - _v2;
                case FlowNetworkOperator.MULTIPLICATION:
                    return _v1 * _v2;
                case FlowNetworkOperator.DIVISION:
                    return _v1 / _v2;
                case FlowNetworkOperator.MINIMUM:
                    return Math.Min(_v1, _v2);
                case FlowNetworkOperator.MAXIMUM:
                    return Math.Max(_v1, _v2);
                case FlowNetworkOperator.ASSIGNMENT:
                    return _v2; // a hack, but works when called iteratively
                default:
                    return _v1 + _v2;
            }
        }

        #endregion
    }

    #endregion

    #region BASE CLASS

    public abstract class FlNetElement : DisplayableProductDefinition
    {
        #region STATIC

        internal static long NR_FL_NET_ELEMENTS = 0;
        public static System.IFormatProvider NR_FORMATTER = new System.Globalization.NumberFormatInfo();

        #endregion

        #region PROPERTIES: General (Content, IsValid)

        protected Component content;
        public Component Content
        {
            get { return this.content; }
            set 
            {
                ////OLD
                //// if the component is already bound in a network element -> do nothing
                //if (value != null && value.IsBoundInNW)
                //    return;

                //if (this.content != null)
                //    this.content.BindingFNE = null;
                //this.content = value;
                //if (this.content != null)
                //    this.content.BindingFNE = this;

                // NEW
                if (this.content != null)
                {
                    // if no change -> do nothing
                    if (value != null && this.content.ID == value.ID)
                        return;
                   
                    this.content.BindingFNE = null;
                    this.Content.RemoveInstance(this); // performed onyl for Relation2GeomType CONTAINED_IN and CONNECTS
                }
                this.content = value;
                if (this.content != null)
                {
                    this.content.BindingFNE = this;
                    this.Content.CreateInstance(this); // performed onyl for Relation2GeomType CONTAINED_IN and CONNECTS
                }

                this.RegisterPropertyChanged("Content");
            }
        }        

        protected bool is_valid;
        public bool IsValid
        {
            get { return this.is_valid; }
            protected set 
            { 
                this.is_valid = value;
                this.RegisterPropertyChanged("IsValid");
            }
        }

        // derived
        public string ContentInfo 
        { 
            get 
            {
                if (this.Content == null) return string.Empty;
                return this.Content.Name + " " + this.GetBoundInstanceId().ToString();
            } 
        }
        #endregion

        #region PROPERTIES: Display (DO NOT SAVE!)

        public string ParamValueToDisplay { get; protected set; }

        #endregion

        #region METHODS: General

        internal virtual void SetValidity()
        {   }

        #endregion

        internal FlNetElement()
        {
            this.ID = (++FlNetElement.NR_FL_NET_ELEMENTS);
            this.Name = "FlNetElement " + this.ID.ToString();
            this.Description = string.Empty;

            this.isSelected = false;
            this.isExpanded = false;
            this.is_marked = false;
            this.is_markable = false;
            this.is_locked = false;
            this.hide_details = false;
            this.is_hidden = false;
            this.is_excluded_from_display = false;

            this.Content = null;
            this.IsValid = false;
        }

        #region METHODS: Update

        internal virtual void UpdateContent()
        {
            if (this.Content == null) return;
            this.Content.ExecuteAllCalculationChains();
            this.RegisterPropertyChanged("Content");
        }

        internal virtual void ResetContent()
        {
            if (this.Content == null) return;
            this.Content.ResetParamsDependentOnFlowNetwork();
            this.Content.ExecuteAllCalculationChains();
            this.RegisterPropertyChanged("Content");
        }

        /// <summary>
        /// <para>Performs all calculations on all levels within the component instance and</para>
        /// <para>transfers the resluts to other recepients (e.g. the size variables of the instance).</para>
        /// </summary>
        internal virtual void UpdateContentInstance()
        {
            if (this.Content == null) return;
            this.Content.ExecuteCalculationChainWoArtefacts(this);
            this.RegisterPropertyChanged("Content");
        }

        internal virtual void ResetContentInstance(Point _default_offset)
        {
            if (this.Content == null) return;
            this.Content.UpdateInstanceIn(this, _default_offset, true);
            this.RegisterPropertyChanged("Content");
        }

        #endregion

        #region METHODS: Info

        // paramerter _in_flow_dir will has a meaning ONLY in FlowNetwork
        internal virtual Parameter.Parameter GetFirstParamByName(string _name, bool _in_flow_dir)
        {
            if (string.IsNullOrEmpty(_name)) return null;
            if (this.Content == null) return null;

            return this.Content.GetFirstParamByName(_name);
        }

        internal virtual Parameter.Parameter GetFirstParamBySuffix(string _suffix, bool _in_flow_dir)
        {
            if (string.IsNullOrEmpty(_suffix)) return null;
            if (this.Content == null) return null;

            return this.Content.GetFirstParamBySuffix(_suffix);
        }

        public virtual long GetBoundInstanceId()
        {
            Geometry.GeometricRelationship gr = this.GetUpdatedInstance(true);
            if (gr != null)
                return gr.ID;
            else
                return -1L;
        }

        public virtual bool GetBoundInstanceRealizedStatus()
        {
            Geometry.GeometricRelationship gr = this.GetUpdatedInstance(true);
            if (gr != null)
                return gr.State.IsRealized;
            else
                return false;
        }

        internal virtual Geometry.GeometricRelationship GetUpdatedInstance(bool _in_flow_dir)
        {
            if (this.Content == null) return null;
            return this.Content.UpdateInstanceIn(this, new Point(0,0), false);
        }

        public virtual List<double> GetInstanceSize()
        {
            Geometry.GeometricRelationship instance = this.GetUpdatedInstance(true);
            if (instance == null) return new List<double>();

            return instance.InstanceSize;
        }

        public virtual List<Geometry.GeomSizeTransferDef> GetInstanceSizeTransferSettings()
        {
            Geometry.GeometricRelationship instance = this.GetUpdatedInstance(true);
            if (instance == null) return new List<Geometry.GeomSizeTransferDef>();

            return instance.InstanceSizeTransferSettings;
        }

        public virtual double UpdateSingleSizeValue(int _index_in_size, double _size, Geometry.GeomSizeTransferDef _transfer_settings)
        {
            Geometry.GeometricRelationship instance = this.GetUpdatedInstance(true);
            if (instance == null) return 0.0;

            return instance.ApplySizeTransferSettingsTo(_index_in_size, _size, _transfer_settings);
        }

        public bool InstanceHasPath()
        {
            Geometry.GeometricRelationship instance = this.GetUpdatedInstance(true);
            if (instance == null) return false;
            if (instance.State.Type != Geometry.Relation2GeomType.CONNECTS) return false;

            return true;
        }

        public bool InstanceHasValidPath()
        {
            Geometry.GeometricRelationship instance = this.GetUpdatedInstance(true);
            if (instance == null) return false;
            if (instance.State.Type != Geometry.Relation2GeomType.CONNECTS) return false;

            return instance.State.IsRealized;
        }

        #endregion

        #region METHODS: ToString

        public virtual void AddToExport(ref StringBuilder _sb, string _key = null)
        {   }

        protected void AddPartToExport(ref StringBuilder _sb)
        {
            if (_sb == null) return;
            string tmp = null;

            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_ID).ToString());
            _sb.AppendLine(this.ID.ToString());

            _sb.AppendLine(((int)FlowNetworkSaveCode.NAME).ToString());
            _sb.AppendLine(this.Name);

            _sb.AppendLine(((int)FlowNetworkSaveCode.DESCRIPTION).ToString());
            _sb.AppendLine(this.Description);

            _sb.AppendLine(((int)FlowNetworkSaveCode.CONTENT_ID).ToString());
            tmp = (this.Content == null) ? "-1" : this.Content.ID.ToString();
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)FlowNetworkSaveCode.IS_VALID).ToString());
            tmp = (this.IsValid) ? "1" : "0";
            _sb.AppendLine(tmp);
        }

        protected string ContentToString()
        {
            if (this.Content == null)
                return "[ ]";
            else
                return "[ " + this.Content.CurrentSlot + ": " + 
                    this.Content.ID + " " + this.Content.Name + " " + this.Content.Description + " ]";
        }

        #endregion

        #region METHODS: Size Update for Content

        public virtual void UpdateContentInstanceSize(double _min_h, double _min_b, double _min_L, double _max_h, double _max_b, double _max_L)
        {

        }

        public virtual void UpdateContentInstanceSizeAndSettings(List<double> _current_size, List<Geometry.GeomSizeTransferDef> _size_settings)
        {

        }

        public virtual string GetContentInstanceSizeInfo(bool _min = true)
        {
            if (this.Content == null) 
                return "[]";

            if (_min)
                return this.Content.GetMinSizeOfInstanceIn(this.ID);
            else
                return this.Content.GetMaxSizeOfInstanceIn(this.ID);
        }

        public virtual void GetParameterValue(string _param_suffix)
        {
            if (this.Content == null) return;

            this.ParamValueToDisplay = this.Content.GetParamValueOfInstance(this, _param_suffix);
            this.RegisterPropertyChanged("ParamValueToDisplay");
        }

        #endregion

    }

    #endregion

    #region NODE
    public class FlNetNode : FlNetElement
    {
        #region PROPETIES: Specific (Position, derived: Edges_In, derived: Edges_Out)

        protected Point position;
        public Point Position
        {
            get { return this.position; }
            set
            { 
                this.position = value;
                this.SetValidity();
                this.CommunicatePositionUpdateToContent();
                this.RegisterPropertyChanged("Position");
            }
        }

        // derived from association with FlNetEdge objects
        public List<FlNetEdge> Edges_In { get; internal set; }
        public List<FlNetEdge> Edges_Out { get; internal set; }

        // derived, resulting from sorting all nodes in flow direction in nested flow networks 
        public List<FlNetEdge> Edges_In_Nested { get; protected set; }
        public List<FlNetEdge> Edges_Out_Nested { get; protected set; }

        internal void TransferConnectionsToNested()
        {
            this.Edges_In_Nested = new List<FlNetEdge>(this.Edges_In);
            this.Edges_Out_Nested = new List<FlNetEdge>(this.Edges_Out);
        }

        #endregion

        #region PROPERTIES (derived, implementation): Contained Elements of SAME TYPE

        internal override IReadOnlyList<DisplayableProductDefinition> ContainedOfSameType
        {
            get { return new List<FlNetElement>().AsReadOnly(); }
        }

        #endregion

        #region PROPERTIES: Specific (Operations)

        public List<FlowNetworkCalcRule> CalculationRules { get; set; }


        #endregion

        #region METHODS: General overrides

        internal override void SetValidity()
        {
            if (double.IsNaN(this.position.X) || double.IsNaN(this.position.Y) ||
                double.IsInfinity(this.position.X) || double.IsInfinity(this.position.Y) ||
                (this.Edges_In.Count == 0 && this.Edges_Out.Count == 0))
                this.IsValid = false;
            else
                this.IsValid = true;
        }

        #endregion

        #region .CTOR
        internal FlNetNode(Point _pos)
        {
            this.Name = "Node " + this.ID.ToString();
            this.Edges_In = new List<FlNetEdge>();
            this.Edges_Out = new List<FlNetEdge>();
            this.Position = _pos;
            this.CalculationRules = new List<FlowNetworkCalcRule>();
        }

        #endregion

        #region COPY .CTOR

        internal FlNetNode(FlNetNode _original)
        {
            this.Name = _original.Name;
            this.Description = _original.Description;
            this.position = _original.position;
            // we do NOT copy content
            // we do NOT copy derived properties (Edges_In, Edges_Out)
            // we do NOT copy calculation rules
            this.Edges_In = new List<FlNetEdge>();
            this.Edges_Out = new List<FlNetEdge>();
            this.CalculationRules = new List<FlowNetworkCalcRule>();
        }

        #endregion

        #region PARSING .CTOR

        // for parsing
        // the content component has to be parsed FIRST
        internal FlNetNode(long _id, string _name, string _description, Component _content, bool _is_valid, Point _position, List<FlowNetworkCalcRule> _calc_rules)
        {
            this.id = _id;
            FlNetElement.NR_FL_NET_ELEMENTS = Math.Max(FlNetElement.NR_FL_NET_ELEMENTS, this.id);
            this.name = _name;
            this.description = _description;

            this.Content = _content;
            this.is_valid = _is_valid;

            this.position = _position;

            this.CalculationRules = (_calc_rules == null) ? new List<FlowNetworkCalcRule>() : new List<FlowNetworkCalcRule>(_calc_rules);

            // derived propeties: i.e. Edges_In, Edges_Out are filled in later, when the edges are parsed
            this.Edges_In = new List<FlNetEdge>();
            this.Edges_Out = new List<FlNetEdge>();
        }

        #endregion

        #region METHODS: TosString overrides

        public override string ToString()
        {
            return "Node " + this.ID.ToString() + " " + this.ContentToString();
        }

        public override void AddToExport(ref StringBuilder _sb, string _key = null)
        {
            if (_sb == null) return;

            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.FLOWNETWORK_NODE);                        // FLOWNETWORK_NODE

            if (!(string.IsNullOrEmpty(_key)))
            {
                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_KEY).ToString());
                _sb.AppendLine(_key);
            }

            _sb.AppendLine(((int)ParamStructCommonSaveCode.CLASS_NAME).ToString());
            _sb.AppendLine(this.GetType().ToString());

            // general: ENTITY_ID, NAME, DESCRIPTION, CONTENT_ID, IS_VALID
            base.AddPartToExport(ref _sb);

            // node-specific
            this.AddNodeSpecificPartToExport(ref _sb);

        }

        protected void AddNodeSpecificPartToExport(ref StringBuilder _sb)
        {
            if (_sb == null) return;
            string tmp = null;

            _sb.AppendLine(((int)FlowNetworkSaveCode.POSITION_X).ToString());
            tmp = double.IsNaN(this.Position.X) ? Parameter.Parameter.NAN : this.Position.X.ToString("F8", FlNetElement.NR_FORMATTER);
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)FlowNetworkSaveCode.POSITION_Y).ToString());
            tmp = double.IsNaN(this.Position.Y) ? Parameter.Parameter.NAN : this.Position.Y.ToString("F8", FlNetElement.NR_FORMATTER);
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)FlowNetworkSaveCode.CALC_RULES).ToString());
            _sb.AppendLine(this.CalculationRules.Count.ToString());

            foreach (FlowNetworkCalcRule rule in this.CalculationRules)
            {
                _sb.AppendLine(((int)FlowNetworkSaveCode.CALC_RULE_SUFFIX_OPERANDS).ToString());
                _sb.AppendLine(rule.Suffix_Operands);

                _sb.AppendLine(((int)FlowNetworkSaveCode.CALC_RULE_SUFFIX_RESULT).ToString());
                _sb.AppendLine(rule.Suffix_Result);

                _sb.AppendLine(((int)FlowNetworkSaveCode.CALC_RULE_DIRECTION).ToString());
                tmp = (rule.Direction == FlowNetworkCalcDirection.FORWARD) ? "1" : "0";
                _sb.AppendLine(tmp);

                _sb.AppendLine(((int)FlowNetworkSaveCode.CALC_RULE_OPERATOR).ToString());
                _sb.AppendLine(FlowNetworkCalcRule.OperatorToString(rule.Operator));
            }
        }

        #endregion

        #region METHODS: Size Update for Content OVERRIDE

        public override void UpdateContentInstanceSize(double _min_h, double _min_b, double _min_L, double _max_h, double _max_b, double _max_L)
        {
            if (this.Content == null) return;
            this.Content.TransferSize(this.ID, _min_h, _min_b, _min_L, _max_h, _max_b, _max_L);
        }

        public override void UpdateContentInstanceSizeAndSettings(List<double> _current_size, List<Geometry.GeomSizeTransferDef> _size_settings)
        {
            if (this.Content == null) return;
            this.Content.TransferSizeSettings(this.ID, _current_size, _size_settings);
        }

        #endregion

        #region METHODS: Position Uodate for Content

        protected virtual void CommunicatePositionUpdateToContent()
        {
            if (this.Content != null)
                this.Content.UpdateInstancePositionIn(this, new Point(0, 0));

            foreach(FlNetEdge e in this.Edges_In)
            {
                if (e.Content != null)
                    e.Content.UpdateInstancePositionIn(e, new Point(0, 0));
            }

            foreach(FlNetEdge e in this.Edges_Out)
            {
                if (e.Content != null)
                    e.Content.UpdateInstancePositionIn(e, new Point(0, 0));
            }
        }

        #endregion

        #region METHODS: General Flow Calculation

        /*
         *  I. The calculations in the flow network are performed as follows (30.06.2017):
         *  ----------------------------------------------------------------------------- 
         *  1. Create a component with Ralation2GeomType CONTAINES_IN or CONNECTING
         *  2. Add the desired parameters (NOT for size), sub- and referenced components.
         *  3. Place an instance of the Component in a NW Element.
         *      a. Ralation2GeomType CONTAINED_IN only in a node
         *      b. Ralation2GeomType CONNECTING only in an edge
         *  4. The first placement causes the automatic creation of sub-components containing instance and type size parameters.
         *     NOTE: if the Ralation2GeomType of the component is neither CONTAINED_IN nor CONNECTING no sub-components are created
         *  5. Formulate calculations (those can also include the size parameters).
         *     NOTE: Use the window for component comparison for fast copying of parameters and calculations
         *           from one component to another
         *  6. Individual instance sizes are assigned in the NW Editor (see InstanceSize).
         *  7. Each instance also carries an individual copy of the component's parameter values (see InstanceParamValues).
         *     NOTE: This includes the parameters of all sub-components.
         *     NOTE: Excluded from this are parameters with propagation CALC_IN (they are used as type, not instance parameters).
         *  8. Continue placing instances.
         *  9. For nested Flow Networks do not forget to mark the sink and the source nodes.
         *  10. Set the calculation rules in all relevant nodes (e.g. fork, transfer, etc.). The sequence of the rules should correspond to the
         *      sequence in which the calculations are to be performed.
         *      NOTE: To remove a rule choose the operator NoNe.
         *  11. Calculate: calculations are performed on the instance values and NOT on the parameters contained in the Component.
         *      For this reason set the component parameter to their desired inital values.
         *      
         *  NOTE: Parameters with propagation CALC_IN are global and contain cumulative values over all instances.
         *        They are automatically generated and not to be altered by the user.
         *        This differs from the flow propagation method used in the pervious versions of the flow calculation (e.g. SynchFlows)
         *  NOTE: When performing calculations repeatedly with different input values (contained in parameters with propagation INPUT)
         *        perform the reset AFTER entering the new values.
         *  
         *  II. Perform placement in a geometric space in the GeometryViewer program module
         */

        // the nodes need to be SORTED in flow direction before calling this method!
        // Note: Method GetFirstParamBySuffix is called according to the DYNAMIC Type of the caller

        internal void CalculateFlow(bool _in_flow_dir)
        {
            if (this.Content == null) return;

            // get the appropriate instance and update its parameter slots w/o resetting
            Geometry.GeometricRelationship instance = this.GetUpdatedInstance(_in_flow_dir);
            if (instance == null) return;
            this.UpdateContentInstance();
            // Debug.WriteLine("START Instance:\t\t\t" + instance.ToString());

            foreach (FlowNetworkCalcRule rule in this.CalculationRules)
            {
                // check if the calculation direction matches the rule
                if ((rule.Direction == FlowNetworkCalcDirection.BACKWARD && _in_flow_dir) ||
                    (rule.Direction == FlowNetworkCalcDirection.FORWARD && !_in_flow_dir))
                    continue;

                // check if the rule applies to the type component of the instance in this node
                Parameter.Parameter p_result = this.GetFirstParamBySuffix(rule.Suffix_Result, _in_flow_dir);
                if (p_result == null) continue;

                if (!(instance.InstanceParamValues.ContainsKey(p_result.Name)))
                    continue;

                if (_in_flow_dir)
                {
                    if (this.Edges_In_Nested.Count > 0)
                    {
                        foreach (FlNetEdge e in this.Edges_In_Nested)
                        {
                            if (e.Content == null) continue;
                            if (!e.CanCalculateFlow(_in_flow_dir)) continue;

                            Geometry.GeometricRelationship instance_in_e = e.GetUpdatedInstance(_in_flow_dir);
                            Parameter.Parameter p_e_Operand = e.GetFirstParamBySuffix(rule.Suffix_Operands, _in_flow_dir);
                            Parameter.Parameter p_e_Result = e.GetFirstParamBySuffix(rule.Suffix_Result, _in_flow_dir);
                            Geometry.GeometricRelationship instance_in_eStart = e.Start.GetUpdatedInstance(_in_flow_dir);
                            Parameter.Parameter p_eStart_Operand = e.Start.GetFirstParamBySuffix(rule.Suffix_Operands, _in_flow_dir);

                            if (instance_in_e == null || p_e_Operand == null || p_e_Result == null) continue;
                            if (!(instance_in_e.InstanceParamValues.ContainsKey(p_e_Operand.Name)) || !(instance_in_e.InstanceParamValues.ContainsKey(p_e_Result.Name)))
                                continue;

                            // propagate incoming value along edge
                            if (instance_in_eStart != null && p_eStart_Operand != null && instance_in_eStart.InstanceParamValues.ContainsKey(p_eStart_Operand.Name))
                            {
                                e.Start.UpdateContentInstance();
                                instance_in_e.InstanceParamValues[p_e_Result.Name] = instance_in_eStart.InstanceParamValues[p_eStart_Operand.Name];
                                e.UpdateContentInstance();
                            }
                            
                            // perform operation in this node
                            double operation_result = rule.Calculate(instance.InstanceParamValues[p_result.Name], instance_in_e.InstanceParamValues[p_e_Operand.Name]);
                            instance.InstanceParamValues[p_result.Name] = operation_result;
                        }
                    }
                }
                else
                {
                    if (this.Edges_Out_Nested.Count > 0)
                    {
                        foreach (FlNetEdge e in this.Edges_Out_Nested)
                        {
                            if (e.Content == null) continue;
                            if (!e.CanCalculateFlow(_in_flow_dir)) continue;

                            Geometry.GeometricRelationship instance_in_e = e.GetUpdatedInstance(_in_flow_dir);
                            Parameter.Parameter p_e_Operand = e.GetFirstParamBySuffix(rule.Suffix_Operands, _in_flow_dir);
                            Parameter.Parameter p_e_Result = e.GetFirstParamBySuffix(rule.Suffix_Result, _in_flow_dir);
                            Geometry.GeometricRelationship instance_in_eEnd = e.End.GetUpdatedInstance(_in_flow_dir);
                            Parameter.Parameter p_eEnd_Operand = e.End.GetFirstParamBySuffix(rule.Suffix_Operands, _in_flow_dir);

                            if (instance_in_e == null || p_e_Operand == null || p_e_Result == null) continue;
                            if (!(instance_in_e.InstanceParamValues.ContainsKey(p_e_Operand.Name)) || !(instance_in_e.InstanceParamValues.ContainsKey(p_e_Result.Name)))
                                continue;

                            // propagate outgoing value along edge
                            if (instance_in_eEnd != null && p_eEnd_Operand != null && instance_in_eEnd.InstanceParamValues.ContainsKey(p_eEnd_Operand.Name))
                            {
                                e.End.UpdateContentInstance();
                                instance_in_e.InstanceParamValues[p_e_Result.Name] = instance_in_eEnd.InstanceParamValues[p_eEnd_Operand.Name];
                                e.UpdateContentInstance();
                            }
                                

                            // perform operation in this node
                            double operation_result = rule.Calculate(instance.InstanceParamValues[p_result.Name], instance_in_e.InstanceParamValues[p_e_Operand.Name]);
                            instance.InstanceParamValues[p_result.Name] = operation_result;
                        }
                    }
                }
            }

            // Debug.WriteLine("Instance before calc:\t" + instance.ToString());
            this.UpdateContentInstance();
            // Debug.WriteLine("END Instance:\t\t\t" + instance.ToString());
        }


        #endregion

        #region METHODS: Connectivity Update for content

        public void UpdateAdjacentEdgeRealization()
        {
            Geometry.GeometricRelationship instance = this.GetUpdatedInstance(true);
            if (instance == null) return;

            // Edges_In_Nested maybe???
            foreach(FlNetEdge e_in in this.Edges_In)
            {
                FlNetNode n_in = e_in.Start;
                if (n_in == null) continue;

                Geometry.GeometricRelationship instance_e_in = e_in.GetUpdatedInstance(true);
                Geometry.GeometricRelationship instance_n_in = n_in.GetUpdatedInstance(true);
                Geometry.GeometricRelationship.UpdateConnectivityBasedRelaization(instance, instance_n_in, ref instance_e_in);               
            }

            // Edges_Out_Nested maybe???
            foreach(FlNetEdge e_out in this.Edges_Out)
            {
                FlNetNode n_out = e_out.End;
                if (n_out == null) continue;

                Geometry.GeometricRelationship instance_e_out = e_out.GetUpdatedInstance(true);
                Geometry.GeometricRelationship instance_n_out = n_out.GetUpdatedInstance(true);
                Geometry.GeometricRelationship.UpdateConnectivityBasedRelaization(instance, instance_n_out, ref instance_e_out);    
            }
        }

        #endregion

    }
    #endregion

    #region EDGE

    public class FlNetEdge : FlNetElement
    {
        #region PROPERTIES: Specific (Start, End)

        protected FlNetNode start;
        public FlNetNode Start
        {
            get { return this.start; }
            set 
            { 
                this.start = value;
                this.SetValidity();
                this.RegisterPropertyChanged("Start");
            }
        }

        protected FlNetNode end;
        public FlNetNode End
        {
            get { return this.end; }
            set 
            { 
                this.end = value;
                this.SetValidity();
                this.RegisterPropertyChanged("End");
            }
        }
        #endregion

        #region PROPERTIES (derived, implementation): Contained Elements of SAME TYPE

        internal override IReadOnlyList<DisplayableProductDefinition> ContainedOfSameType
        {
            get { return new List<FlNetElement>().AsReadOnly(); }
        }

        #endregion

        #region METHODS: General overrides

        internal override void SetValidity()
        {
            if (this.start == null || this.end == null)
                this.IsValid = false;
            else
                this.IsValid = true;
        }

        #endregion

        #region METHODS: Size Update for Content OVERRIDE

        public override void UpdateContentInstanceSize(double _min_h, double _min_b, double _min_L, double _max_h, double _max_b, double _max_L)
        {
            if (this.Content == null) return;
            this.Content.TransferSize(this.ID, _min_h, _min_b, _min_L, _max_h, _max_b, _max_L);
        }

        public override void UpdateContentInstanceSizeAndSettings(List<double> _current_size, List<Geometry.GeomSizeTransferDef> _size_settings)
        {
            if (this.Content == null) return;
            if (_current_size == null || _size_settings == null) return;
            this.Content.TransferSizeSettings(this.ID, _current_size, _size_settings);
        }

        #endregion

        #region .CTOR
        internal FlNetEdge(FlNetNode _start, FlNetNode _end)
        {
            this.Name = "Edge " + this.ID.ToString();
            this.Start = _start;
            this.End = _end;
        }
        #endregion

        #region COPY .CTOR

        internal FlNetEdge(FlNetEdge _original, FlNetNode _start_copy, FlNetNode _end_copy)
        {
            this.Name = _original.Name;
            this.Description = _original.Description;
            // do not copy start and end !!!
            this.Start = _start_copy;
            this.End = _end_copy;
        }

        #endregion

        #region PARSING .CTOR

        // for parsing
        // the content component has to be parsed FIRST
        internal FlNetEdge(long _id, string _name, string _description, Component _content, bool _is_valid, 
                            FlNetNode _start, FlNetNode _end)
        {
            this.id = _id;
            FlNetElement.NR_FL_NET_ELEMENTS = Math.Max(FlNetElement.NR_FL_NET_ELEMENTS, this.id);
            this.name = _name;
            this.description = _description;

            this.Content = _content;
            this.is_valid = _is_valid;

            this.start = _start;
            this.end = _end;
        }

        #endregion

        #region METHODS: ToString

        public override string ToString()
        {
            return "Edge " + this.ID.ToString() + " " + this.ContentToString();
        }

        public override void AddToExport(ref StringBuilder _sb, string _key = null)
        {
            if (_sb == null) return;
            string tmp = null;

            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.FLOWNETWORK_EDGE);                        // FLOWNETWORK_EDGE

            if (!(string.IsNullOrEmpty(_key)))
            {
                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_KEY).ToString());
                _sb.AppendLine(_key);
            }

            _sb.AppendLine(((int)ParamStructCommonSaveCode.CLASS_NAME).ToString());
            _sb.AppendLine(this.GetType().ToString());

            // general: ENTITY_ID, NAME, DESCRIPTION, CONTENT_ID, IS_VALID
            base.AddPartToExport(ref _sb);

            // edge-specific
            _sb.AppendLine(((int)FlowNetworkSaveCode.START_NODE).ToString());
            tmp = (this.Start == null) ? "-1" : this.Start.ID.ToString();
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)FlowNetworkSaveCode.END_NODE).ToString());
            tmp = (this.End == null) ? "-1" : this.End.ID.ToString();
            _sb.AppendLine(tmp);
        }

        #endregion

        #region METHODS: Content Check

        internal bool CanCalculateFlow(bool _in_flow_dir)
        {
            if (this.Content == null) return false;

            if (_in_flow_dir)
            {
                if (this.Start == null) return false;
                if (this.Start.Content == null && !(this.Start is FlowNetwork)) return false;
                return true;
            }
            else
            {                
                if (this.End == null) return false;
                if (this.End.Content == null && !(this.End is FlowNetwork)) return false;
                return true;
            }
        }

        #endregion

    }

    #endregion

    public class FlowNetwork : FlNetNode
    {
        #region STATIC

        internal static Vector COPY_OFFSET = new Vector(20, 20);

        #endregion

        #region PROPERTIES(DO NOT SAVE) for Display (IsSelected override)

        public override bool IsSelected
        {
            get { return this.isSelected; }
            set
            {
                this.isSelected = value;
                this.ParentIsSelected = this.isSelected;

                // communicate to subelements
                List<FlowNetwork> sNW = this.GetFlatSubNetworkList();
                foreach (FlowNetwork n in sNW)
                {
                    n.ParentIsSelected = this.isSelected;
                }
                foreach (var entry in this.ContainedNodes)
                {
                    FlNetNode n = entry.Value;
                    if (n != null)
                        n.ParentIsSelected = this.isSelected;
                }
                foreach (var entry in this.ContainedEdges)
                {
                    FlNetEdge e = entry.Value;
                    if ( e != null)
                        e.ParentIsSelected = this.isSelected;
                }
                
                this.RegisterPropertyChanged("IsSelected");
            }
        }

        protected string p_suffix_to_display;
        public string Suffix_To_Display 
        {
            get { return this.p_suffix_to_display; } 
            set
            {
                this.p_suffix_to_display = value;
                if (!string.IsNullOrEmpty(this.p_suffix_to_display))
                {
                    foreach(var entry in this.ContainedNodes)
                    {
                        entry.Value.GetParameterValue(this.p_suffix_to_display);
                    }
                    foreach (var entry in this.ContainedEdges)
                    {
                        entry.Value.GetParameterValue(this.p_suffix_to_display);
                    }
                }
                this.RegisterPropertyChanged("Suffix_To_Display");
            }
        }

        #endregion

        #region PROPERTIES EDITING: Manager, TimeStamp

        protected ComponentManagerType manager;
        public ComponentManagerType Manager
        {
            get { return this.manager; }
            protected set
            { 
                this.manager = value;
                this.RegisterPropertyChanged("Manager");
            }
        }

        protected DateTime time_stamp;
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

        #region PROPERTIES: Elements (Nodes, Edges, Networks)

        // NODES
        protected ObservableConcurrentDictionary<long,FlNetNode> contained_nodes;
        public ObservableConcurrentDictionary<long,FlNetNode> ContainedNodes
        {
            get { return this.contained_nodes; }
            protected set 
            { 
                if (this.contained_nodes != null)
                    this.contained_nodes.CollectionChanged -= contained_nodes_CC;
                this.contained_nodes = value;
                if (this.contained_nodes != null)
                    this.contained_nodes.CollectionChanged += contained_nodes_CC;
                this.RegisterPropertyChanged("ContainedNodes");
            }
        }

        // derived
        public int NrNodes { get { return this.contained_nodes.Count(); } }

        private void contained_nodes_CC(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.RegisterPropertyChanged("ContainedNodes");
            this.RegisterPropertyChanged("NrNodes");
            this.UpdateChildrenContainer();
        }

        // EDGES
        protected ObservableConcurrentDictionary<long,FlNetEdge> contained_edges;
        public ObservableConcurrentDictionary<long,FlNetEdge> ContainedEdges
        {
            get { return this.contained_edges; }
            protected set 
            {
                this.contained_edges = value;

                if (this.contained_edges != null)
                    this.contained_edges.CollectionChanged -= contained_edges_CC;
                this.contained_edges = value;
                if (this.contained_edges != null)
                    this.contained_edges.CollectionChanged += contained_edges_CC;
                this.RegisterPropertyChanged("ContainedEdges");
            }
        }

        // derived
        public int NrEdges { get { return this.contained_edges.Count(); } }

        private void contained_edges_CC(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.RegisterPropertyChanged("ContainedEdges");
            this.RegisterPropertyChanged("NrEdges");
            this.UpdateChildrenContainer();
        }


        // NETWORKS
        protected ObservableConcurrentDictionary<long, FlowNetwork> contained_flow_networks;
        public ObservableConcurrentDictionary<long, FlowNetwork> ContainedFlowNetworks
        {
            get { return this.contained_flow_networks; }
            protected set
            {
                if (this.contained_flow_networks != null)
                    this.contained_flow_networks.CollectionChanged -= contained_flow_networks_CC;
                this.contained_flow_networks = value;
                if (this.contained_flow_networks != null)
                    this.contained_flow_networks.CollectionChanged += contained_flow_networks_CC;
                this.RegisterPropertyChanged("ContainedFlowNetworks");
            }
        }

        // derived
        public int NrFlNetworks { get { return this.contained_flow_networks.Count(); } }

        private void contained_flow_networks_CC(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.RegisterPropertyChanged("ContainedFlowNetworks");
            this.RegisterPropertyChanged("NrFlNetworks");
            this.UpdateChildrenContainer();
        }

        #endregion

        #region PROPERTIES: First(Start, Source) Node, Last(End, Sink) Node

        private long node_start_id;
        public long NodeStart_ID
        {
            get { return this.node_start_id; }
            set 
            {
                if (value > -1 && 
                    (this.ContainedNodes.ContainsKey(value) || this.ContainedFlowNetworks.ContainsKey(value)))
                {
                    this.node_start_id = value;
                    this.RegisterPropertyChanged("NodeStart_ID");
                }
            }
        }

        private long node_end_id;
        public long NodeEnd_ID
        {
            get { return this.node_end_id; }
            set 
            {
                if (value > -1 &&
                    (this.ContainedNodes.ContainsKey(value) || this.ContainedFlowNetworks.ContainsKey(value)))
                {
                    this.node_end_id = value;
                    this.RegisterPropertyChanged("NodeEnd_ID");
                }
            }
        }

        #endregion

        #region PROPERTIES (derived): all contained elements as Children

        private CompositeCollection children;
        public CompositeCollection Children { get { return this.children; } }

        public void UpdateChildrenContainer()
        {
            this.SetValidity();
            this.children = new CompositeCollection
            {
                new CollectionContainer { Collection = this.ContainedNodes.Values },
                new CollectionContainer { Collection = this.ContainedEdges.Values },
                new CollectionContainer { Collection = this.ContainedFlowNetworks.Values }
            };
            this.TimeStamp = DateTime.Now;
            this.RegisterPropertyChanged("Children");
        }

        #endregion

        #region PROPERTIES (derived, implementation): Contained Elements of SAME TYPE

        internal override IReadOnlyList<DisplayableProductDefinition> ContainedOfSameType
        {
            get { return this.ContainedFlowNetworks.Values.Cast<DisplayableProductDefinition>().ToList().AsReadOnly(); }
        }

        #endregion

        #region .CTOR
        public FlowNetwork(Point _position, string _name, string _description, ComponentManagerType _manager)
            :base(_position)
        {
            // base DisplayableProductDefinition: ID, TypeName, Description, IsSelected, IsExpanded, IsMarkable, IsMarked, IsLocked, HideDetails
            // base FlNetElement                : Content, IsValid
            // base FlNetNode                   : Position, Edges_In, Edges_Out

            this.Name = (string.IsNullOrEmpty(_name)) ? "FlowNetwork " + this.ID.ToString() : _name;
            this.Description = _description;

            this.Manager = _manager;
            this.TimeStamp = DateTime.Now;

            this.ContainedNodes = new ObservableConcurrentDictionary<long, FlNetNode>();
            this.ContainedEdges = new ObservableConcurrentDictionary<long, FlNetEdge>();
            this.ContainedFlowNetworks = new ObservableConcurrentDictionary<long, FlowNetwork>();

            this.node_start_id = -1;
            this.node_end_id = -1;
        }
        #endregion

        #region COPY .CTOR

        internal FlowNetwork(FlowNetwork _original)
            :base(_original)
        {
            this.Manager = _original.Manager;
            this.TimeStamp = DateTime.Now;

            // NODES
            this.ContainedNodes = new ObservableConcurrentDictionary<long, FlNetNode>();
            Dictionary<long, FlNetNode> id_old_node_new = new Dictionary<long, FlNetNode>();
            foreach(FlNetNode node in _original.ContainedNodes.Values)
            {
                FlNetNode node_copy = new FlNetNode(node);
                id_old_node_new.Add(node.ID, node_copy);
                this.ContainedNodes.Add(node_copy.ID, node_copy);
            }

            // NETWORKS
            this.ContainedFlowNetworks = new ObservableConcurrentDictionary<long, FlowNetwork>();
            Dictionary<long, FlowNetwork> id_old_network_new = new Dictionary<long, FlowNetwork>();
            foreach(FlowNetwork nw in _original.ContainedFlowNetworks.Values)
            {
                FlowNetwork nw_copy = new FlowNetwork(nw);
                id_old_network_new.Add(nw.ID, nw_copy);
                this.ContainedFlowNetworks.Add(nw_copy.ID, nw_copy);
            }

            // EDGES
            this.ContainedEdges = new ObservableConcurrentDictionary<long, FlNetEdge>();
            foreach(FlNetEdge edge in _original.ContainedEdges.Values)
            {
                FlNetNode start_copy = null;
                if (edge.Start != null)
                {
                    if (edge.Start is FlowNetwork && id_old_network_new.ContainsKey(edge.Start.ID))
                        start_copy = id_old_network_new[edge.Start.ID];
                    else if (id_old_node_new.ContainsKey(edge.Start.ID))
                        start_copy = id_old_node_new[edge.Start.ID];
                }

                FlNetNode end_copy = null;
                if (edge.End != null)
                {
                    if (edge.End is FlowNetwork && id_old_network_new.ContainsKey(edge.End.ID))
                        end_copy = id_old_network_new[edge.End.ID];
                    else if (id_old_node_new.ContainsKey(edge.End.ID))
                        end_copy = id_old_node_new[edge.End.ID];
                }

                FlNetEdge edge_copy = new FlNetEdge(edge, start_copy, end_copy);
                
                // establish connection to nodes
                if (start_copy != null)
                {
                    start_copy.Edges_Out.Add(edge_copy);
                    start_copy.SetValidity();
                }
                if (end_copy != null)
                {
                    end_copy.Edges_In.Add(edge_copy);
                    end_copy.SetValidity();
                }

                this.ContainedEdges.Add(edge_copy.ID, edge_copy);
            }

            // copy the references to the copies of the start and end nodes
            if (id_old_node_new.ContainsKey(_original.node_start_id))
                this.node_start_id = id_old_node_new[_original.node_start_id].ID;
            if (id_old_network_new.ContainsKey(_original.node_start_id))
                this.node_start_id = id_old_network_new[_original.node_start_id].ID;

            if (id_old_node_new.ContainsKey(_original.node_end_id))
                this.node_end_id = id_old_node_new[_original.node_end_id].ID;
            if (id_old_network_new.ContainsKey(_original.node_end_id))
                this.node_end_id = id_old_network_new[_original.node_end_id].ID;

        }

        #endregion

        #region PARSING .CTOR

        internal FlowNetwork(long _id, string _name, string _description, Component _content, bool _is_valid, Point _position,
                            ComponentManagerType _manager, DateTime _time_stamp,
                            IList<FlNetNode> _nodes, IList<FlNetEdge> _edges, IList<FlowNetwork> _subnetworks,
                            long _node_start_id, long _node_end_id, List<FlowNetworkCalcRule> _calc_rules)
            :base(_id, _name, _description, _content, _is_valid, _position, _calc_rules)
        {
            // base (FlNetElement)
            this.id = _id;
            FlNetElement.NR_FL_NET_ELEMENTS = Math.Max(FlNetElement.NR_FL_NET_ELEMENTS, this.id);
            this.name = _name;
            this.description = _description;
            this.Content = _content;
            this.is_valid = _is_valid;

            // base (FlNetNode)
            this.position = _position;
            this.Edges_In = new List<FlNetEdge>();
            this.Edges_Out = new List<FlNetEdge>();

            // contained entities (FlowNetwork)
            // add nodes
            this.ContainedNodes = new ObservableConcurrentDictionary<long, FlNetNode>();
            if (_nodes != null && _nodes.Count > 0)
            {
                foreach(FlNetNode n in _nodes)
                {
                    this.ContainedNodes.Add(n.ID, n);
                }
            }

            // add subnetworks
            this.ContainedFlowNetworks = new ObservableConcurrentDictionary<long, FlowNetwork>();
            if (_subnetworks != null && _subnetworks.Count > 0)
            {
                foreach(FlowNetwork nw in _subnetworks)
                {
                    this.ContainedFlowNetworks.Add(nw.ID, nw);
                }
            }

            // add edges
            this.ContainedEdges = new ObservableConcurrentDictionary<long, FlNetEdge>();
            if (_edges != null && _edges.Count > 0)
            {
                foreach(FlNetEdge e in _edges)
                {
                    // establish connection to nodes
                    if (e.Start != null)
                    {
                        e.Start.Edges_Out.Add(e);
                        // e.Start.SetValidity();
                    }
                    if (e.End != null)
                    {
                        e.End.Edges_In.Add(e);
                        // e.End.SetValidity();
                    }
                    this.ContainedEdges.Add(e.ID, e);
                }
            }

            // parse start (source) and end (sink)
            this.NodeStart_ID = _node_start_id;
            this.NodeEnd_ID = _node_end_id;

            // finalize (FlowNetwork)
            this.manager = _manager;
            this.time_stamp = _time_stamp;
        }


        #endregion

        #region METHODS: Overrides

        internal override void SetValidity()
        {
            base.SetValidity(); // FlNetNode
            bool is_valid = this.IsValid;
            if (!is_valid) return;
            
            foreach(var entry in this.ContainedNodes)
            {
                is_valid &= entry.Value.IsValid;
            }
            foreach(var entry in this.ContainedEdges)
            {
                is_valid &= entry.Value.IsValid;
            }
            foreach(var entry in this.ContainedFlowNetworks)
            {
                is_valid &= entry.Value.IsValid;
            }

            this.IsValid = is_valid;
        }

        #endregion

        #region METHODS: Info

        public List<FlowNetwork> GetFlatSubNetworkList()
        {
            if (this.ContainedFlowNetworks == null || this.ContainedFlowNetworks.Count() < 1)
                return new List<FlowNetwork>();

            List<FlowNetwork> list = new List<FlowNetwork>();
            foreach (FlowNetwork n in this.ContainedFlowNetworks.Values)
            {
                if (n == null) continue;
                list.Add(n);
                if (n.ContainedFlowNetworks.Count() > 0)
                    list.AddRange(n.GetFlatSubNetworkList());
            }
            return list;
        }

        public List<string> GetUniquePAramNamesInContent()
        {
            List<Component> content = new List<Component>();
            foreach(var entry in this.ContainedNodes)
            {
                if (entry.Value.Content != null)
                {
                    Component duplicate = content.FirstOrDefault(x => x.ID == entry.Value.Content.ID);
                    if (duplicate == null)
                        content.Add(entry.Value.Content);
                }
            }

            return Component.GetUniqueParameterNamesFor(content);
        }

        // updated 16.08.2017
        internal List<Component> GetAllContent()
        {
            List<Component> contents = new List<Component>();
            foreach (var entry in this.ContainedNodes)
            {
                FlNetNode n = entry.Value;
                if (n == null) continue;
                if (n.Content == null) continue;

                Component duplicate = contents.FirstOrDefault(x => x.ID == n.Content.ID);
                if (duplicate != null) continue;

                contents.Add(n.Content);
            }
            foreach (var entry in this.ContainedEdges)
            {
                FlNetEdge e = entry.Value;
                if (e == null) continue;
                if (e.Content == null) continue;

                Component duplicate = contents.FirstOrDefault(x => x.ID == e.Content.ID);
                if (duplicate != null) continue;

                contents.Add(e.Content);
            }
            foreach(var entry in this.ContainedFlowNetworks)
            {
                FlowNetwork nw = entry.Value;
                if (nw == null) continue;

                List<Component> nw_contents = nw.GetAllContent();
                foreach(Component nw_c in nw_contents)
                {
                    Component duplicate = contents.FirstOrDefault(x => x.ID == nw_c.ID);
                    if (duplicate != null) continue;

                    contents.Add(nw_c);
                }
            }

            return contents;
        }

        internal List<Geometry.GeometricRelationship> GetAllContentInstances()
        {
            List<Geometry.GeometricRelationship> instances = new List<Geometry.GeometricRelationship>();

            // TODO.............!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

            return instances;
        }

        internal override Parameter.Parameter GetFirstParamByName(string _name, bool _in_flow_dir)
        {
            if (string.IsNullOrEmpty(_name)) return null;

            long id = -1;
            if (_in_flow_dir)
            {
                if (this.NodeEnd_ID < 0) return null;
                id = this.NodeEnd_ID;
            }
            else
            {
                if (this.NodeStart_ID < 0) return null;
                id = this.NodeStart_ID;
            }

            if (this.ContainedNodes.ContainsKey(id))
            {
                return this.ContainedNodes[id].GetFirstParamByName(_name, _in_flow_dir);
            }
            else if (this.ContainedFlowNetworks.ContainsKey(id))
            {
                return this.ContainedFlowNetworks[id].GetFirstParamByName(_name, _in_flow_dir);
            }

            return null;
        }

        internal override Parameter.Parameter GetFirstParamBySuffix(string _suffix, bool _in_flow_dir)
        {
            if (string.IsNullOrEmpty(_suffix)) return null;

            long id = -1;
            if (_in_flow_dir)
            {
                if (this.NodeEnd_ID < 0) return null;
                id = this.NodeEnd_ID;
            }
            else
            {
                if (this.NodeStart_ID < 0) return null;
                id = this.NodeStart_ID;
            }

            if (this.ContainedNodes.ContainsKey(id))
            {
                return this.ContainedNodes[id].GetFirstParamBySuffix(_suffix, _in_flow_dir);
            }
            else if (this.ContainedFlowNetworks.ContainsKey(id))
            {
                return this.ContainedFlowNetworks[id].GetFirstParamBySuffix(_suffix, _in_flow_dir);
            }

            return null;
        }

        internal override Geometry.GeometricRelationship GetUpdatedInstance(bool _in_flow_dir)
        {
            long id = -1;
            if (_in_flow_dir)
            {
                if (this.NodeEnd_ID < 0) return null;
                id = this.NodeEnd_ID;
            }
            else
            {
                if (this.NodeStart_ID < 0) return null;
                id = this.NodeStart_ID;
            }

            if (this.ContainedNodes.ContainsKey(id))
            {
                return this.ContainedNodes[id].GetUpdatedInstance(_in_flow_dir);
            }
            else if (this.ContainedFlowNetworks.ContainsKey(id))
            {
                return this.ContainedFlowNetworks[id].GetUpdatedInstance(_in_flow_dir);
            }

            return null;
        }

        public List<FlNetElement> GetAllContainersOf(Component _comp)
        {
            if (_comp == null)
                return new List<FlNetElement>();

            List<long> container_ids = _comp.GetIdsOfAllInstanceContainers();
            List<FlNetElement> containers = new List<FlNetElement>();
            foreach(long id in container_ids)
            {
                if (this.ContainedNodes.ContainsKey(id))
                    containers.Add(this.ContainedNodes[id]);
                else if (this.ContainedEdges.ContainsKey(id))
                    containers.Add(this.ContainedEdges[id]);
            }

            return containers;
        }

        public FlNetElement GetById(long _id)
        {
            if (_id < 0) return null;

            if (this.ContainedNodes.ContainsKey(_id)) 
                return this.ContainedNodes[_id];

            if (this.ContainedEdges.ContainsKey(_id))
                return this.ContainedEdges[_id];

            if (this.ContainedFlowNetworks.ContainsKey(_id))
                return this.ContainedFlowNetworks[_id];

            foreach(var entry in this.ContainedFlowNetworks)
            {
                return entry.Value.GetById(_id);
            }

            return null;
        }

        #endregion

        #region METHODS: Manager Switch

        internal void ChangeManager(ComponentManagerType _manger_new)
        {
            if (this.Manager == _manger_new) return;

            this.Manager = _manger_new;
            foreach(var entry in this.ContainedFlowNetworks)
            {
                entry.Value.ChangeManager(_manger_new);
            }
        }

        #endregion

        #region METHODS: record merging

        /// <summary>
        /// Use when merging records in the ComponentFactory.
        /// </summary>
        /// <param name="max_current_id"></param>
        /// <returns></returns>
        internal Dictionary<long, long> UpdateAllElementIds(ref long max_current_id)
        {
            Dictionary<long, long> old_new = new Dictionary<long, long>();
            List<FlNetElement> to_change_id = new List<FlNetElement>();

            // gather all ids (recursively)
            List<long> all_ids = this.GetAllElementIds();
            all_ids.Add(this.ID);

            // record with which to merge: 0 1 2 3 -> max_current_id = 3
            // this nw: 6 7 12 14
            if (all_ids.Min() > max_current_id)
                return old_new;

            // record with which to merge: 0 1 2 3 -> max_current_id = 3
            // this nw: 2 3 6 7
            // shift all ids by an offset (3 - 2 + 1) = 2 -> 2 3 6 7 --> 4 5 8 9
            long offset = max_current_id - all_ids.Min() + 1;
            max_current_id = all_ids.Max() + offset; // 9

            foreach(long id in all_ids)
            {
                old_new.Add(id, id + offset);
            }

            this.ShiftContainedElementIds(old_new);
            this.ID = old_new[this.ID];

            return old_new;
        }

        internal List<long> GetAllElementIds()
        {
            List<long> all_ids = new List<long>();
            all_ids.AddRange(this.ContainedNodes.Keys);
            all_ids.AddRange(this.ContainedEdges.Keys);
            all_ids.AddRange(this.ContainedFlowNetworks.Keys);

            foreach(var entry in this.ContainedFlowNetworks)
            {
                all_ids.AddRange(entry.Value.GetAllElementIds());
            }

            return all_ids;
        }


        protected void ShiftContainedElementIds(Dictionary<long,long> _old_new)
        {
            // perform shift: Nodes
            List<FlNetNode> nodes = this.ContainedNodes.Values.ToList();
            this.ContainedNodes = new ObservableConcurrentDictionary<long, FlNetNode>();
            if (nodes != null && nodes.Count > 0)
            {
                foreach (FlNetNode n in nodes)
                {
                    if (n.Content != null)
                        n.Content.UpdateInstanceToContainerBinding(n.ID, _old_new[n.ID], "Node " + _old_new[n.ID].ToString());

                    n.ID = _old_new[n.ID];
                    n.Name = "Node " + n.ID.ToString();

                    this.ContainedNodes.Add(n.ID, n);
                }
            }

            // perform shift: Edges
            List<FlNetEdge> edges = this.ContainedEdges.Values.ToList();
            this.ContainedEdges = new ObservableConcurrentDictionary<long, FlNetEdge>();
            if (edges != null && edges.Count > 0)
            {
                foreach (FlNetEdge e in edges)
                {
                    if (e.Content != null)
                        e.Content.UpdateInstanceToContainerBinding(e.ID, _old_new[e.ID], "Edge " + _old_new[e.ID].ToString());

                    e.ID = _old_new[e.ID];
                    e.Name = "Edge " + e.ID.ToString();

                    this.ContainedEdges.Add(e.ID, e);
                }
            }

            // perform shift: Networks
            List<FlowNetwork> subnetworks = this.ContainedFlowNetworks.Values.ToList();
            this.ContainedFlowNetworks = new ObservableConcurrentDictionary<long, FlowNetwork>();
            if (subnetworks != null && subnetworks.Count > 0)
            {
                foreach (FlowNetwork nw in subnetworks)
                {
                    nw.ID = _old_new[nw.ID];
                    nw.ShiftContainedElementIds(_old_new);
                    this.ContainedFlowNetworks.Add(nw.ID, nw);
                }
            }

            // update the connectivity info in the Geometric Relationships in edges
            foreach(var entry in this.ContainedEdges)
            {
                FlNetEdge e = entry.Value;
                if (e.Content != null)
                    e.Content.UpdateInstanceConnectivityInfo(e);
            }

            this.time_stamp = DateTime.Now;
        }


        #endregion

        #region METHODS: Add, Remove Nodes and Edges

        public long AddNode(Point _pos)
        {
            FlNetNode node = new FlNetNode(_pos);
            if (this.ContainedNodes.ContainsKey(node.ID)) return -1;

            this.ContainedNodes.Add(node.ID, node);
            return node.ID;
        }

        internal long AddFlowNetwork(Point _pos, string _name, string _description)
        {
            FlowNetwork nw = new FlowNetwork(_pos, _name, _description, this.Manager);
            if (this.ContainedFlowNetworks.ContainsKey(nw.ID)) return -1;

            this.ContainedFlowNetworks.Add(nw.ID, nw);
            return nw.ID;
        }

        internal bool AddFlowNetwork(FlowNetwork _to_add)
        {
            if (_to_add == null) return false;
            if (this.ContainedFlowNetworks.ContainsKey(_to_add.ID)) return false;

            this.ContainedFlowNetworks.Add(_to_add.ID, _to_add);
            return true;
        }

        protected bool RemoveNodeOrNetwork(FlNetNode _node)
        {
            bool is_flow_network = _node is FlowNetwork;
            if (is_flow_network && !this.ContainedFlowNetworks.ContainsKey(_node.ID)) return false;
            if (!is_flow_network && !this.ContainedNodes.ContainsKey(_node.ID)) return false;

            // alert contained component(S)
            if (_node.Content != null)
                _node.Content.BindingFNE = null;
            if (is_flow_network)
            {
                FlowNetwork fnw = _node as FlowNetwork;
                List<Component> contents = fnw.GetAllContent();
                foreach(Component c in contents)
                {
                    c.BindingFNE = null;
                }
            }

            // remove all connections
            bool success = true;
            foreach (FlNetEdge edge in _node.Edges_In)
            {
                if (this.ContainedEdges.ContainsKey(edge.ID))
                {
                    if (edge.Content != null)
                        edge.Content.BindingFNE = null;
                    success &= edge.Start.Edges_Out.Remove(edge);
                    success &= this.ContainedEdges.Remove(edge.ID);
                }
                else
                    success = false;
            }
            foreach (FlNetEdge edge in _node.Edges_Out)
            {
                if (this.ContainedEdges.ContainsKey(edge.ID))
                {
                    if (edge.Content != null)
                        edge.Content.BindingFNE = null;
                    success &= edge.End.Edges_In.Remove(edge);
                    success &= this.ContainedEdges.Remove(edge.ID);
                }
                else
                    success = false;
            }

            if (is_flow_network)
                success &= this.ContainedFlowNetworks.Remove(_node.ID);
            else
                success &= this.ContainedNodes.Remove(_node.ID);

            return success;
        }


        public bool RemoveNode(FlNetNode _node)
        {
            if (_node == null) return false;
            if (_node is FlowNetwork) return false;

            return this.RemoveNodeOrNetwork(_node);
        }

        internal bool RemoveNetwork(FlowNetwork _nw)
        {
            if (_nw == null) return false;

            return this.RemoveNodeOrNetwork(_nw);
        }

        public long AddEdge(FlNetNode _start, FlNetNode _end)
        {
            if (_start == null || _end == null) return -1;
            if (!this.ContainedNodes.ContainsKey(_start.ID) &&
                !this.ContainedFlowNetworks.ContainsKey(_start.ID)) return -1;
            if (!this.ContainedNodes.ContainsKey(_end.ID) &&
                !this.ContainedFlowNetworks.ContainsKey(_end.ID)) return -1;

            FlNetEdge edge = new FlNetEdge(_start, _end);
            if (!edge.IsValid) return -1;
            if (this.ContainedEdges.ContainsKey(edge.ID)) return -1;

            // establish connection to nodes
            _start.Edges_Out.Add(edge);
            _end.Edges_In.Add(edge);
            _start.SetValidity();
            _end.SetValidity();

            this.ContainedEdges.Add(edge.ID, edge);
            return edge.ID;
        }

        public bool RemoveEdge(FlNetEdge _edge)
        {
            if (_edge == null) return false;
            if (!this.ContainedEdges.ContainsKey(_edge.ID)) return false;

            // alert contained component
            if (_edge.Content != null)
                _edge.Content.BindingFNE = null;

            // remove connection to nodes
            if (_edge.Start != null)
                _edge.Start.Edges_Out.Remove(_edge);
            if (_edge.End != null)
                _edge.End.Edges_In.Remove(_edge);

            _edge.Start.SetValidity();
            _edge.End.SetValidity();

            return this.ContainedEdges.Remove(_edge.ID);
        }

        #endregion

        #region METHODS: Redirect Edges

        public bool RedirectEdge(FlNetEdge _edge, bool _rerout_start, FlNetNode _to_node)
        {
            if (_edge == null || _to_node == null) return false;

            if (!this.ContainedEdges.ContainsKey(_edge.ID)) return false;
            if (!this.ContainedFlowNetworks.ContainsKey(_to_node.ID) &&
                !this.ContainedNodes.ContainsKey(_to_node.ID)) return false;

            if (_rerout_start)
            {
                // detach from old node
                if (_edge.Start != null)
                    _edge.Start.Edges_Out.Remove(_edge);
                _edge.Start.SetValidity();

                // attach to new node
                _edge.Start = _to_node;
                _to_node.Edges_Out.Add(_edge);
            }
            else
            {
                // detach from old node
                if (_edge.End != null)
                    _edge.End.Edges_In.Remove(_edge);
                _edge.End.SetValidity();

                // attach to new node
                _edge.End = _to_node;
                _to_node.Edges_In.Add(_edge);               
            }

            _to_node.SetValidity();
            return true;
        }

        #endregion

        #region METHODS: Convert btw Node and Subnetwork

        internal FlowNetwork NodeToNetwork(FlNetNode _node)
        {
            if (_node == null) return null;
            if (!this.ContainedNodes.ContainsKey(_node.ID)) return null;
            if (_node is FlowNetwork && this.ContainedFlowNetworks.ContainsKey(_node.ID)) return _node as FlowNetwork;

            // create a new network
            FlowNetwork created = new FlowNetwork(_node.Position, _node.Name, _node.Description, this.Manager);

            // redirect edges
            foreach(FlNetEdge e_in in _node.Edges_In)
            {
                e_in.End = created;
                created.Edges_In.Add(e_in);
            }
            _node.Edges_In.Clear();
            foreach(FlNetEdge e_out in _node.Edges_Out)
            {
                e_out.Start = created;
                created.Edges_Out.Add(e_out);
            }
            _node.Edges_Out.Clear();

            // delete node
            _node.Content = null;
            this.ContainedNodes.Remove(_node.ID);

            // add network
            this.ContainedFlowNetworks.Add(created.ID, created);

            return created;
        }

        internal FlNetNode NetworkToNode(FlowNetwork _nw)
        {
            if (_nw == null) return null;
            if (!this.ContainedFlowNetworks.ContainsKey(_nw.ID)) return null;

            // create a new node
            FlNetNode created = new FlNetNode(_nw.Position);
            created.Name = _nw.Name;
            created.Description = _nw.Description;

            // redirect edges
            foreach(FlNetEdge e_in in _nw.Edges_In)
            {
                e_in.End = created;
                created.Edges_In.Add(e_in);
            }
            _nw.Edges_In.Clear();
            foreach(FlNetEdge e_out in _nw.Edges_Out)
            {
                e_out.Start = created;
                created.Edges_Out.Add(e_out);
            }
            _nw.Edges_Out.Clear();

            // allert content(S)
            _nw.Content = null;
            List<Component> contents = _nw.GetAllContent();
            foreach (Component c in contents)
            {
                c.BindingFNE = null;
            }
            // delete network
            this.ContainedFlowNetworks.Remove(_nw.ID);

            // add node
            this.ContainedNodes.Add(created.ID, created);

            return created;
        }

        #endregion

        #region METHODS: ToString

        public override string ToString()
        {
            string fl_net_str = "FlowNetwork " + this.ID.ToString() + " " + this.Name + " " + this.Description + " ";
            fl_net_str += "[ " + this.NrNodes + " nodes, " + this.NrEdges + " edges ]";

            return fl_net_str;
        }

        public override void AddToExport(ref StringBuilder _sb, string _key = null)
        {
            if (_sb == null) return;

            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.FLOWNETWORK);                             // FLOWNETWORK

            if (!(string.IsNullOrEmpty(_key)))
            {
                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_KEY).ToString());
                _sb.AppendLine(_key);
            }

            _sb.AppendLine(((int)ParamStructCommonSaveCode.CLASS_NAME).ToString());
            _sb.AppendLine(this.GetType().ToString());

            // general: ENTITY_ID, NAME, DESCRIPTION, CONTENT_ID, IS_VALID
            base.AddPartToExport(ref _sb);

            // node-specific
            this.AddNodeSpecificPartToExport(ref _sb);

            // flow network-specific: EDITING
            _sb.AppendLine(((int)FlowNetworkSaveCode.MANAGER).ToString());
            _sb.AppendLine(ComponentUtils.ComponentManagerTypeToLetter(this.Manager));

            _sb.AppendLine(((int)FlowNetworkSaveCode.TIMESTAMP).ToString());
            _sb.AppendLine(this.TimeStamp.ToString(ParamStructTypes.DT_FORMATTER));

            // flow network-specific: CONTAINED NODES
            _sb.AppendLine(((int)FlowNetworkSaveCode.CONTAINED_NODES).ToString());
            _sb.AppendLine(this.NrNodes.ToString());

            if (this.NrNodes > 0)
            {
                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
                _sb.AppendLine(ParamStructTypes.ENTITY_SEQUENCE);                         // ENTSEQ

                foreach (var entry in this.ContainedNodes)
                {
                    if (entry.Value == null) continue;
                    entry.Value.AddToExport(ref _sb);
                }

                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
                _sb.AppendLine(ParamStructTypes.SEQUENCE_END);                            // SEQEND
                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
                _sb.AppendLine(ParamStructTypes.ENTITY_CONTINUE);                         // ENTCTN
            }

            // flow network-specific: CONTAINED FLOW NETWORKS
            _sb.AppendLine(((int)FlowNetworkSaveCode.CONTIANED_NETW).ToString());
            _sb.AppendLine(this.NrFlNetworks.ToString());

            if (this.NrFlNetworks > 0)
            {
                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
                _sb.AppendLine(ParamStructTypes.ENTITY_SEQUENCE);                         // ENTSEQ

                foreach (var entry in this.ContainedFlowNetworks)
                {
                    if (entry.Value == null) continue;
                    entry.Value.AddToExport(ref _sb);
                }

                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
                _sb.AppendLine(ParamStructTypes.SEQUENCE_END);                            // SEQEND
                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
                _sb.AppendLine(ParamStructTypes.ENTITY_CONTINUE);                         // ENTCTN
            }

            // flow network-specific: CONTAINED EDGES
            // has to come AFTER the contained nodes and networks because the edges reference them
            _sb.AppendLine(((int)FlowNetworkSaveCode.CONTAINED_EDGES).ToString());
            _sb.AppendLine(this.NrEdges.ToString());

            if (this.NrEdges > 0)
            {
                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
                _sb.AppendLine(ParamStructTypes.ENTITY_SEQUENCE);                         // ENTSEQ

                foreach (var entry in this.ContainedEdges)
                {
                    if (entry.Value == null) continue;
                    entry.Value.AddToExport(ref _sb);
                }

                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
                _sb.AppendLine(ParamStructTypes.SEQUENCE_END);                            // SEQEND
                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
                _sb.AppendLine(ParamStructTypes.ENTITY_CONTINUE);                         // ENTCTN
            }

            // start and end nodes
            _sb.AppendLine(((int)FlowNetworkSaveCode.NODE_SOURCE).ToString());
            _sb.AppendLine(this.node_start_id.ToString());
            _sb.AppendLine(((int)FlowNetworkSaveCode.NODE_SINK).ToString());
            _sb.AppendLine(this.node_end_id.ToString());

            // signify end of complex entity
            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.SEQUENCE_END);                            // SEQEND
        }

        #endregion

        #region METHODS: Size Update for Content OVERRIDE

        public override void UpdateContentInstanceSize(double _min_h, double _min_b, double _min_L, double _max_h, double _max_b, double _max_L)
        {   }

        public override void UpdateContentInstanceSizeAndSettings(List<double> _current_size, List<Geometry.GeomSizeTransferDef> _size_settings)
        {   }

        #endregion

        #region METHODS: Position Update for Content OVERRIDE

        protected override void CommunicatePositionUpdateToContent()
        {
            FlNetNode first = this.SortAndGetFirstNode();
            if (first == null)
                return;

            Point offset = new Point(this.Position.X - first.Position.X, this.Position.Y - first.Position.Y);
            foreach (FlNetEdge e in this.Edges_In)
            {
                if (e.Content != null)
                    e.Content.UpdateInstancePositionIn(e, offset);
            }

            foreach (FlNetEdge e in this.Edges_Out)
            {
                if (e.Content != null)
                    e.Content.UpdateInstancePositionIn(e, offset);
            }
        }

        #endregion

        #region METHODS: Sorting acc. to flow direction

        protected List<FlNetNode> SortNodesInFlowDirection()
        {
            List<FlNetNode> sorted = new List<FlNetNode>();
            if (this.ContainedNodes == null || this.ContainedFlowNetworks == null)
                return sorted;

            // inialize sorting record
            Dictionary<long, bool> was_sorted = new Dictionary<long, bool>();
            foreach(var entry in this.ContainedNodes)
            {
                was_sorted.Add(entry.Key, false);
            }
            foreach(var entry in this.ContainedFlowNetworks)
            {
                was_sorted.Add(entry.Key, false);
            }

            int nr_nodes_added = 0;
            // 1. find first all nodes with NO INCOMING EDGES
            foreach(var entry in this.ContainedNodes)
            {
                FlNetNode n = entry.Value;
                if (n == null) continue;

                if (n.Edges_In.Count > 0) 
                    continue;
                else
                {
                    if (!was_sorted[entry.Key])
                    {
                        sorted.Add(n);
                        nr_nodes_added++;
                        was_sorted[entry.Key] = true;
                    }
                }
            }
            foreach (var entry in this.ContainedFlowNetworks)
            {
                FlowNetwork nw = entry.Value;
                if (nw == null) continue;

                if (nw.Edges_In.Count > 0)
                    continue;
                else
                {
                    if (!was_sorted[entry.Key])
                    {
                        sorted.Add(nw);
                        nr_nodes_added++;
                        was_sorted[entry.Key] = true;
                    }
                }
            }

            // 2. and START from them or end, if nothing was added
            // but only add nodes WITH OUTGOING EDGES
            while (nr_nodes_added > 0)
            {               
                int nr_nodes_added_old = nr_nodes_added;
                nr_nodes_added = 0;
                for (int i = sorted.Count - 1; i >= sorted.Count - nr_nodes_added_old; i--)
                {
                    FlNetNode n = sorted[i];
                    if (n.Edges_Out.Count == 0) continue;

                    foreach (FlNetEdge e_out in n.Edges_Out)
                    {
                        if (e_out.End != null && !was_sorted[e_out.End.ID] && e_out.End.Edges_Out.Count > 0)
                        {
                            sorted.Add(e_out.End);
                            nr_nodes_added++;
                            was_sorted[e_out.End.ID] = true;
                        }
                    }
                }

                if (nr_nodes_added == 0) break;
            }

            // 3. finally start from the sorted nodes
            // and add the ones W/O OUTGOING EDGES
            nr_nodes_added = was_sorted.Count(x => x.Value == true);
            while (nr_nodes_added > 0)
            {
                int nr_nodes_added_old = nr_nodes_added;
                nr_nodes_added = 0;
                for (int i = sorted.Count - 1; i >= 0; i--)
                {
                    FlNetNode n = sorted[i];
                    if (n.Edges_Out.Count == 0) continue;

                    foreach (FlNetEdge e_out in n.Edges_Out)
                    {
                        if (e_out.End != null && !was_sorted[e_out.End.ID])
                        {
                            sorted.Add(e_out.End);
                            nr_nodes_added++;
                            was_sorted[e_out.End.ID] = true;
                        }
                    }
                }

                if (nr_nodes_added == 0) break;
            }

            return sorted;
        }

        protected List<FlNetNode> SortNodesInFlowDirectionUnfoldNW()
        {
            List<FlNetNode> sorted_all = this.SortNodesInFlowDirection();
            int nr_foldedNW = sorted_all.Count(x => x is FlowNetwork);

            // even if there are no nested networks, copy the edges to the appropriate lists
            if (nr_foldedNW == 0)
            {
                foreach (FlNetNode n in sorted_all)
                {
                    n.TransferConnectionsToNested();
                }
            }

            while(nr_foldedNW > 0)
            {
                List<FlNetNode> sorted_unfolded = new List<FlNetNode>();
                foreach (FlNetNode n in sorted_all)
                {
                    n.TransferConnectionsToNested();
                    if (n is FlowNetwork)
                    {
                        FlowNetwork fnw = n as FlowNetwork;
                        List<FlNetNode> fnw_sorted = fnw.SortNodesInFlowDirection();
                        int nr_unfolded = fnw_sorted.Count;
                        // connect properly for value propagation
                        if (nr_unfolded > 0)
                        {
                            foreach(FlNetNode fnw_n in fnw_sorted)
                            {
                                fnw_n.TransferConnectionsToNested();
                            }
                            fnw_sorted[0].Edges_In_Nested.AddRange(fnw.Edges_In);
                            fnw_sorted[nr_unfolded - 1].Edges_Out_Nested.AddRange(fnw.Edges_Out);
                        }
                        sorted_unfolded.AddRange(fnw_sorted);
                    }
                    else
                    {
                        sorted_unfolded.Add(n);
                    }
                }

                sorted_all = new List<FlNetNode>(sorted_unfolded);
                nr_foldedNW = sorted_all.Count(x => x is FlowNetwork);
            }

            return sorted_all;
        }

        internal FlNetNode SortAndGetFirstNode()
        {
            List<FlNetNode> sorted = this.SortNodesInFlowDirectionUnfoldNW();
            if (sorted.Count > 0)
                return sorted[0];
            else
                return null;
        }

        internal FlNetNode SortAndGetLastNode()
        {
            List<FlNetNode> sorted = this.SortNodesInFlowDirectionUnfoldNW();
            if (sorted.Count > 0)
                return sorted[sorted.Count - 1];
            else
                return null;
        }

        #endregion

        #region METHODS: Flow Calculation General

        public void ResetAllContentInstances(Point _offse_parent)
        {
            Point offset = new Point();
            if (_offse_parent.X == 0 && _offse_parent.Y == 0)
            {
                offset = new Point(this.Position.X, this.Position.Y);
            }
            else
            {
                FlNetNode first = this.SortAndGetFirstNode();
                offset = new Point(_offse_parent.X - first.Position.X, _offse_parent.Y - first.Position.Y);
            }           
            
            foreach (var entry in this.ContainedNodes)
            {
                if (entry.Value == null) continue;
                entry.Value.ResetContentInstance(offset);
            }
            foreach (var entry in this.ContainedEdges)
            {
                if (entry.Value == null) continue;
                entry.Value.ResetContentInstance(offset);
            }
            foreach (var entry in this.ContainedFlowNetworks)
            {
                if (entry.Value == null) continue;
                entry.Value.ResetAllContentInstances(entry.Value.Position);
            }
        }

        public void CalculateAllFlows(bool _in_flow_dir)
        {
            List<FlNetNode> sorted = this.SortNodesInFlowDirectionUnfoldNW();
            if (_in_flow_dir)
            {
                // e.g. for exhaust air -> in the flow direction
            }
            else
            {
                // e.g. for air supply -> opposite the flow direction
                sorted.Reverse();
            }

            foreach (FlNetNode n in sorted)
            {
                string node_conent_name = (n.Content == null) ? n.Name : n.Content.Name;
                // Debug.WriteLine("Calculating Node: " + node_conent_name);
                n.CalculateFlow(_in_flow_dir);
            }
        }

        public void PropagateCalculationRulesToAllInstances(FlNetNode _source)
        {
            if (_source == null) return;
            if (_source.Content == null) return;

            foreach(var entry in this.ContainedNodes)
            {
                if (entry.Value == null) continue;
                if (entry.Value.Content == null) continue;

                if (entry.Value.Content.ID == _source.Content.ID)
                    entry.Value.CalculationRules = new List<FlowNetworkCalcRule>(_source.CalculationRules);
            }
        }

        #endregion

        #region METHODS: Step by Step Flow Calculation

        public List<FlNetNode> PrepareToCalculateFlowStepByStep(bool _in_flow_dir)
        {
            List<FlNetNode> sorted = this.SortNodesInFlowDirectionUnfoldNW();
            if (_in_flow_dir)
            {
                // e.g. for exhaust air -> in the flow direction
            }
            else
            {
                // e.g. for air supply -> opposite the flow direction
                sorted.Reverse();
            }
            return sorted;
        }

        public string CalculateFlowStep(List<FlNetNode> _sorted_nodes, bool _in_flow_dir, int _step_index, bool _show_only_changes, out FlNetNode _current_node)
        {
            _current_node = null;
            if (_sorted_nodes == null || _sorted_nodes.Count == 0) return null;
            if (_step_index < 0 || _sorted_nodes.Count - 1 < _step_index) return null;

            _current_node = _sorted_nodes[_step_index];
            if (_current_node == null) return null;
            List<FlNetEdge> edges_prev = (_in_flow_dir) ? _current_node.Edges_In_Nested : _current_node.Edges_Out_Nested;

            // define feedback String
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("N " + _current_node.Name);

            // extract the relevant component instances
            Geometry.GeometricRelationship instance_in_node = _current_node.GetUpdatedInstance(_in_flow_dir);
            
            List<Geometry.GeometricRelationship> instances_in_edges_prev = new List<Geometry.GeometricRelationship>();
            if (edges_prev != null)
                instances_in_edges_prev = edges_prev.Select(x => x.GetUpdatedInstance(_in_flow_dir)).ToList();

            // document state BEFORE calculation step
            Dictionary<string, double> values_in_node_BEFORE = new Dictionary<string, double>(instance_in_node.InstanceParamValues);
            List<Dictionary<string, double>> values_in_edges_prev_BEFORE = instances_in_edges_prev.Select(x => new Dictionary<string, double>(x.InstanceParamValues)).ToList();
            
            // CALCULATE FLOW
            _current_node.CalculateFlow(_in_flow_dir);

            // document state AFTER calculation step
            Dictionary<string, double> values_in_node_AFTER = new Dictionary<string, double>(instance_in_node.InstanceParamValues);
            List<Dictionary<string, double>> values_in_edges_prev_AFTER = instances_in_edges_prev.Select(x => new Dictionary<string, double>(x.InstanceParamValues)).ToList();

            // write the transitions
            FlowNetwork.ParallelDictionariesToString(values_in_node_BEFORE, values_in_node_AFTER, _show_only_changes, ref sb);
            for (int i = 0; i < values_in_edges_prev_BEFORE.Count; i++ )
            {
                if (edges_prev != null && edges_prev.Count > i)
                    sb.AppendLine("E " + edges_prev[i].Name);
                FlowNetwork.ParallelDictionariesToString(values_in_edges_prev_BEFORE[i], values_in_edges_prev_AFTER[i], _show_only_changes, ref sb);
            }

            return sb.ToString();
        }


        private static void ParallelDictionariesToString(Dictionary<string, double> _dict_1, Dictionary<string, double> _dict_2, bool _show_only_differences, ref StringBuilder sb)
        {
            if (sb == null)
                sb = new StringBuilder();

            if (_dict_1 != null && _dict_2 != null && _dict_1.Count == _dict_2.Count)
            {
                for (int p = 0; p < _dict_1.Count; p++)
                {
                    if (_show_only_differences)
                    {
                        double diff = Math.Abs(_dict_1.ElementAt(p).Value - _dict_2.ElementAt(p).Value);
                        if (diff < Parameter.Parameter.VALUE_TOLERANCE)
                            continue;
                    }

                    string line = "'" + _dict_1.ElementAt(p).Key + "':";
                    line = line.PadRight(20, ' ') + "\t";
                    line += Parameter.Parameter.ValueToString(_dict_1.ElementAt(p).Value, "F4") + " -> ";
                    line += Parameter.Parameter.ValueToString(_dict_2.ElementAt(p).Value, "F4");
                    sb.AppendLine(line);
                }
            }

        }

        #endregion
    }
}
