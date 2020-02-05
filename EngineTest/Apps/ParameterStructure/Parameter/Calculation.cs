using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows;

using Sprache;
using Sprache.Calc;

using ParameterStructure.DXF;

namespace ParameterStructure.Parameter
{
    #region ENUMS

    // [1301 - 1400]
    public enum CalculationSaveCode
    {
        NAME = 1301,
        EXPRESSION = 1302,
        PARAMS_INPUT = 1303,
        PARAMS_OUTPUT = 1304
    }

    public enum CalculationState
    {
        NO_WRITING_ACCESS = 0,
        MISSING_DATA = 1,
        PARAMS_NOT_OF_THIS_OR_CHILD_COMP = 2,
        PARAM_OUT_W_VALUE_FILED = 3,
        PARAMS_OUT_DUPLICATE = 4,
        PARAMS_IN_OUT_SAME = 5,
        INVALID_SYNTAX = 6,
        CAUSES_CALCULATION_LOOP = 7,
        VALID = 8,
        UNKNOWN_ERROR = 9
    }

    #endregion

    #region HELPER CLASSES

    internal class CalculationPreview
    {
        public string Name { get; private set; }
        public string Expression { get; private set; }

        public Dictionary<string, long> InputParamsPreview { get; private set; }
        public Dictionary<string, long> ReturnParamsPreview { get; private set; }

        public CalculationPreview(string _name, string _expression, 
                                    IDictionary<string,long> _input_params_preview, 
                                    IDictionary<string,long> _return_params_preview)
        {
            this.Name = _name;
            this.Expression = _expression;

            if (_input_params_preview == null)
                this.InputParamsPreview = new Dictionary<string, long>();
            else
                this.InputParamsPreview = new Dictionary<string, long>(_input_params_preview);

            if (_return_params_preview == null)
                this.ReturnParamsPreview = new Dictionary<string, long>();
            else
                this.ReturnParamsPreview = new Dictionary<string, long>(_return_params_preview);
        }
    }

    #endregion
    public class CalculationFactory
    {
        #region STATIC

        private static XtensibleCalculator CALCULATOR = new XtensibleCalculator();
        private const string CALC_CANDIDATE_NAME = "candidate";
        private const string CALC_CANDIDATE_EXPR = "x";

        public static string CalcStateToStringDE(CalculationState _state)
        {
            switch(_state)
            {
                case CalculationState.NO_WRITING_ACCESS:
                    return "Der Benutzer hat keine Schreibrechte!";
                case CalculationState.MISSING_DATA:
                    return "Unvollständige Definition!";
                case CalculationState.PARAMS_NOT_OF_THIS_OR_CHILD_COMP:
                    return "Zumindest ein Parameter ist kein Teil dieser Komponente oder einer ihrer Subkomponenten!";
                case CalculationState.PARAM_OUT_W_VALUE_FILED:
                    return "Outputparameter kann kein Kennfeld referenzieren!";
                case CalculationState.PARAMS_OUT_DUPLICATE:
                    return "Outputparameter ist bereits Output einer bestehenden Gleichung!";
                case CalculationState.PARAMS_IN_OUT_SAME:
                    return "Der Outputparameter darf nicht als Input vorkommen!";
                case CalculationState.INVALID_SYNTAX:
                    return "Syntaxfehler!";
                case CalculationState.CAUSES_CALCULATION_LOOP:
                    return "Diese Gleichung verursacht eine Endlosschleife!";
                case CalculationState.VALID:
                    return "OK";
                case CalculationState.UNKNOWN_ERROR:
                    return "Unbekannter Fehler!";
                default:
                    return "Unbekannter Fehler!";
            }
        }

        #endregion

        private Calculation calc_candidate;
        private List<Calculation> calc_record;
        public List<Calculation> CalcRecord { get { return this.calc_record; } }

        public CalculationFactory()
        {
            this.calc_record = new List<Calculation>();
        }

        #region METHODS: Factory Methods

        public Calculation CreateEmptyCalculation()
        {
            ObservableConcurrentDictionary<string, Parameter> p_in = new ObservableConcurrentDictionary<string, Parameter>();
            p_in.Add("x", null);
            ObservableConcurrentDictionary<string, Parameter> p_out = new ObservableConcurrentDictionary<string, Parameter>();
            p_out.Add("out01", null);
            this.calc_candidate = new Calculation(CALC_CANDIDATE_EXPR, CALC_CANDIDATE_NAME, p_in, p_out);
            this.calc_record.Add(this.calc_candidate);
            return this.calc_candidate;
        }

        public Calculation TestAndSaveCalculation()
        {
            if (this.calc_candidate == null) return null;

            bool is_removed = this.calc_record.Remove(this.calc_candidate);
            if (!is_removed) return null;

            return this.CreateCalculation(this.calc_candidate.Name, this.calc_candidate.Expression,
                                            new Dictionary<string, Parameter>(this.calc_candidate.InputParams),
                                            new Dictionary<string, Parameter>(this.calc_candidate.ReturnParams)); 
        }


        /// <summary>
        /// Creates a named calculation that takes a lambda expression, input and output Parameter instances.
        /// </summary>
        /// <param name="_name"></param>
        /// <param name="_expr">a mathematical expression with named parameters (e.g. (x + y/2)*3.5)</param>
        /// <param name="_parameters_in">List of input parameters</param>
        /// <param name="_parameters_out">List of output parameters (each receives the same value)</param>
        /// <returns></returns>
        internal Calculation CreateCalculation(string _name, string _expr, Dictionary<string, Parameter> _parameters_in, 
                                                                           Dictionary<string, Parameter> _parameters_out)
        {
            if (string.IsNullOrEmpty(_name) || string.IsNullOrEmpty(_expr) || 
                _parameters_in == null || _parameters_in.Count < 1 ||
                _parameters_out == null || _parameters_out.Count < 1) 
                return null;

            ObservableConcurrentDictionary<string, Parameter> p_IN = new ObservableConcurrentDictionary<string, Parameter>();
            foreach(var entry in _parameters_in)
            {
                p_IN.Add(entry.Key, entry.Value);
            }
            ObservableConcurrentDictionary<string, Parameter> p_OUT = new ObservableConcurrentDictionary<string, Parameter>();
            foreach (var entry in _parameters_out)
            {
                p_OUT.Add(entry.Key, entry.Value);
            }

            try
            {
                Dictionary<string, double> params_in_value = new Dictionary<string, double>();
                foreach (var entry in p_IN)
                {
                    if (entry.Value == null) continue;
                    params_in_value.Add(entry.Key, entry.Value.ValueCurrent);
                }
                // try to parse the expression(SPACES and UNDERLINES cause Exceptions!!!)
                Func<double> func = CalculationFactory.CALCULATOR.ParseExpression(_expr, params_in_value).Compile();
                if (func == null) return null;
                // create the Calculation instance ONLY if parsing was successful
                Calculation c = new Calculation(_expr, _name, p_IN, p_OUT);
                this.calc_record.Add(c);
                return c;
            }
            catch(Exception e)
            {
                string debug = e.Message;
                MessageBox.Show(debug, "Eingabefehler Gleichung", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        #endregion

        #region METHODS: Record Management

        public bool RemoveCalculation(long _to_remove_id)
        {
            Calculation found = this.calc_record.Find(x => x.ID == _to_remove_id);
            if (found == null) return false;

            this.calc_record.Remove(found);
            return true;
        }

        public Calculation CopyCalculation(long _to_copy_id)
        {
            Calculation found = this.calc_record.Find(x => x.ID == _to_copy_id);
            if (found == null) return null;

            // if the original compiles, so will the copy
            // the copy refers to the SAME parameters as the original (INCLUDING THE RETURN PARAMETERS !!!)
            Calculation copy = new Calculation(found);
            this.calc_record.Add(copy);
            return copy;
        }

        #endregion

    }


    public class Calculation : ComparableContent
    {
        #region STATIC

        public static readonly double MIN_DOUBLE_VAL = 0.0001;

        private static long NR_CALCULATIONS = 0;
        private static XtensibleCalculator CALCULATOR = new XtensibleCalculator();

        private static Regex SPACE_KILLER = new Regex(@"\s+");
        // sequences containing at least one letter at the start and any number of digits afterwards
        // but NOT BEFORE a '(' or a letter
        private static Regex PARAM_DETECTOR = new Regex(@"[a-zA-Z]+[0-9]*(?![\(a-zA-Z])");

        private static bool HaveSameInput(Calculation c1, Calculation c2)
        {
            if (c1 == null || c2 == null) return false;

            if (c1.InputParams.Count() != c2.InputParams.Count()) return false;

            foreach(var entry in c1.InputParams)
            {
                Parameter p1 = entry.Value;
                if (p1 == null) continue;

                Parameter p2 = c2.InputParams.Values.FirstOrDefault(x => x.Name == p1.Name && x.Unit == p1.Unit);
                if (p2 == null)
                    return false;  
            }

            return true;
        }

        private static bool HaveSameOutput(Calculation c1, Calculation c2)
        {
            if (c1 == null || c2 == null) return false;

            if (c1.ReturnParams.Count() != c2.ReturnParams.Count()) return false;

            foreach (var entry in c1.ReturnParams)
            {
                Parameter p1 = entry.Value;
                if (p1 == null) continue;

                Parameter p2 = c2.ReturnParams.Values.FirstOrDefault(x => x.Name == p1.Name && x.Unit == p1.Unit);
                if (p2 == null)
                    return false;
            }

            return true;
        }

        private static bool HaveSameExpression(Calculation c1, Calculation c2)
        {
            if (c1 == null || c2 == null) return false;

            // kill white spaces because unimportant
            // replace the placeholders (could be any identifier) for input parameters with 'P'
            string c1_e = Calculation.SPACE_KILLER.Replace(c1.Expression, string.Empty);
            string c1_eP = Calculation.PARAM_DETECTOR.Replace(c1_e, "P");
            string c2_e = Calculation.SPACE_KILLER.Replace(c2.Expression, string.Empty);
            string c2_eP = Calculation.PARAM_DETECTOR.Replace(c2_e, "P");

            return (c1_eP == c2_eP);
        }

        private static bool HaveSameMathEqForm(Calculation c1, Calculation c2)
        {
            if (c1 == null || c2 == null) return false;

            string c1_me = Calculation.SPACE_KILLER.Replace(c1.ToMathEuqasion(), string.Empty);
            string c2_me = Calculation.SPACE_KILLER.Replace(c2.ToMathEuqasion(), string.Empty);

            return (c1_me == c2_me);
        }

        #endregion

        #region STATIC: Parsing, Merging

        internal static void AdjustInstanceIds(ref List<Calculation> _instances)
        {
            if (_instances == null || _instances.Count == 0)
                return;

            // existing: 0 1 2 3 -> Calculation.NR_CALCULATIONS = 4
            // new: 6 7 12 14
            List<long> all_ids = _instances.Select(x => x.ID).ToList();
            if (all_ids.Min() > Calculation.NR_CALCULATIONS - 1)
                return;

            // existing: 0 1 2 3 -> Calculation.NR_CALCULATIONS = 4
            // new: 2 3 6 7
            // shift all ids by an offset (4 - 2) = 2 -> 2 3 6 7 --> 4 5 8 9
            long offset = Calculation.NR_CALCULATIONS - all_ids.Min();
            Calculation.NR_CALCULATIONS = all_ids.Max() + offset + 1; // 10

            foreach (Calculation calc in _instances)
            {
                calc.ID += offset;
            }
        }

        #endregion

        #region PROPERTIES
        public long ID { get; private set; }

        private string name;
        public string Name 
        {
            get { return this.name; }
            set
            {
                this.name = value;
                this.RegisterPropertyChanged("Name");
            }
        }

        private string expression;
        public string Expression 
        {
            get { return this.expression; }
            set
            {
                if (this.expression != value)
                    this.UpToDate = false;
                this.expression = value;
                this.RegisterPropertyChanged("Expression");
                this.RegisterPropertyChanged("ExpressionExtended");
            }
        }
        public string ExpressionExtended { get { return this.ToMathEuqasion(); } }

        // INPUT PARAMETERS
        private ObservableConcurrentDictionary<string, Parameter> input_params;
        public ObservableConcurrentDictionary<string, Parameter> InputParams 
        {
            get { return this.input_params; }
            set
            {
                if (this.input_params != null)
                    this.input_params.CollectionChanged -= input_params_CC;
                this.input_params = value;
                if (this.input_params != null)
                    this.input_params.CollectionChanged += input_params_CC;
                this.RegisterPropertyChanged("InputParams");
                this.RegisterPropertyChanged("InputParamNames");
                this.RegisterPropertyChanged("ExpressionExtended");
            }
        }

        private void input_params_CC(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.GatherInputParamNames();
            this.RegisterPropertyChanged("InputParams");
            this.RegisterPropertyChanged("InputParamNames");
            this.UpToDate = false;
        }

        internal void GatherInputParamNames(bool _notify = false)
        {
            this.input_param_names = string.Empty;
            if (this.input_params != null)
            {
                foreach(var entry in this.input_params)
                {
                    if (entry.Value != null)
                        this.input_param_names += "{" + entry.Value.ID + "}:" + entry.Value.Name + " ";
                }
            }

            if (_notify)
            {
                this.RegisterPropertyChanged("InputParams");
                this.RegisterPropertyChanged("InputParamNames");
            }
        }

        private string input_param_names;
        public string InputParamNames { get { return this.input_param_names; } }

        // RETURN PARAMETERS
        private ObservableConcurrentDictionary<string, Parameter> return_params;
        public ObservableConcurrentDictionary<string, Parameter> ReturnParams 
        {
            get { return this.return_params; }
            set
            {
                if (this.return_params != null)
                    this.return_params.CollectionChanged -= output_params_CC;
                this.return_params = value;
                if (this.return_params != null)
                    this.return_params.CollectionChanged += output_params_CC;
                this.RegisterPropertyChanged("ReturnParams");
                this.RegisterPropertyChanged("ReturnParamNames");
                this.RegisterPropertyChanged("ExpressionExtended");
            }
        }

        private void output_params_CC(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.GatherReturnParamNames();
            this.RegisterPropertyChanged("ReturnParams");
            this.RegisterPropertyChanged("ReturnParamNames");
            this.UpToDate = false;
        }


        internal void GatherReturnParamNames(bool _notify = false)
        {
            this.return_param_names = string.Empty;
            if (this.return_params != null)
            {
                foreach(var entry in this.return_params)
                {
                    if (entry.Value != null)
                        this.return_param_names += "{" + entry.Value.ID + "}:" + entry.Value.Name + " ";
                }
            }

            if (_notify)
            {
                this.RegisterPropertyChanged("ReturnParams");
                this.RegisterPropertyChanged("ReturnParamNames");
            }
        }

        private string return_param_names;
        public string ReturnParamNames { get { return this.return_param_names;} }

        #endregion

        #region PROPERTIES for Display (IsSelected, IsExpanded, OwnerIsSelected, UpToDate)

        private bool isExpanded;
        public bool IsExpanded
        {
            get { return this.isExpanded; }
            set
            {
                this.isExpanded = value;
                this.RegisterPropertyChanged("IsExpanded");
            }
        }

        private bool isSelected;
        public bool IsSelected
        {
            get { return this.isSelected; }
            set
            {
                this.isSelected = value;
                this.RegisterPropertyChanged("IsSelected");
            }
        }

        private bool  owner_is_selected;
        public bool  OwnerIsSelected
        {
            get { return this.owner_is_selected; }
            set 
            { 
                this.owner_is_selected = value;
                this.RegisterPropertyChanged("OwnerIsSelected");
            }
        }

        private bool up_to_date;
        public bool UpToDate
        {
            get { return this.up_to_date; }
            set
            {
                this.up_to_date = value;
                this.RegisterPropertyChanged("UpToDate");
            }
        }

        #endregion

        #region .CTOR

        /// <summary>
        /// Is to be called only by the CalculationFactory, Component, and DXFComponent.
        /// </summary>
        /// <param name="_expr">a mathematical expression with named parameters (e.g. (x + y/2)*3.5)</param>
        /// <param name="_parameters_in">List of input parameters</param>
        /// <param name="_parameters_out">List of output / return parameters (each receives the same value)</param>
        internal Calculation(string _expr, string _name, ObservableConcurrentDictionary<string, Parameter> _parameters_in,
                                                         ObservableConcurrentDictionary<string, Parameter> _parameters_out)
        {
            this.ID = (++Calculation.NR_CALCULATIONS);
            this.Name = _name;
            this.Expression = _expr;
            this.InputParams = _parameters_in;
            this.ReturnParams = _parameters_out;
            this.GatherInputParamNames();
            this.GatherReturnParamNames();

            this.IsExpanded = false;
            this.IsSelected = false;
            this.OwnerIsSelected = false;
            this.UpToDate = true;
        }

        #endregion

        #region COPY .CTOR

        internal Calculation(Calculation _original)
        {
            this.ID = (++Calculation.NR_CALCULATIONS);
            this.Name = _original.Name;
            this.Expression = _original.Expression;

            this.InputParams = new ObservableConcurrentDictionary<string,Parameter>();
            foreach(var entry in _original.InputParams)
            {
                this.InputParams.Add(entry.Key, entry.Value);
            }
            this.ReturnParams = new ObservableConcurrentDictionary<string, Parameter>();
            foreach (var entry in _original.ReturnParams)
            {
                this.ReturnParams.Add(entry.Key, entry.Value);
            }

            this.GatherInputParamNames();
            this.GatherReturnParamNames();

            this.IsExpanded = false;
            this.IsSelected = false;
            this.OwnerIsSelected = false;
            this.UpToDate = true;
        }

        #endregion

        #region METHODS: Calculation

        public void PerformCalculation()
        {
            double result = double.NaN;
            Dictionary<string, double> params_in_value = new Dictionary<string, double>();
            foreach(var entry in this.input_params)
            {
                if (entry.Value == null) continue;
                params_in_value.Add(entry.Key, entry.Value.ValueCurrent);
            }

            try
            {
                Func<double> func = Calculation.CALCULATOR.ParseExpression(this.Expression, params_in_value).Compile(); // replaced 'input_params' with 'param_values'
                if (func != null)
                    result = func.Invoke();
            }
            catch 
            {
                result = double.NaN;
            }

            // assign the return value to all return parameters
            foreach (var entry in this.ReturnParams)
            {
                Parameter pR = entry.Value;
                if (pR == null) continue;
                pR.ValueCurrent = result;
            }

            // added 26.10.2016
            // if the expression is of the kind "a = b" - i.e. assignment, 
            // and the return param expects a string value (Parameter.POINTER_STRING) 
            // transfer the text value too
            if (this.InputParams.Count() == 1 && this.ReturnParams.Count() == 1)
            {
                string input_string = this.InputParams.ElementAt(0).Value.TextValue;
                string rp_Name = this.ReturnParams.ElementAt(0).Value.Name;

                if (rp_Name.EndsWith(Parameter.POINTER_STRING_NAME_TAG))
                    this.ReturnParams.ElementAt(0).Value.TextValue = input_string;
            }

            this.UpToDate = true;

        }

        /// <summary>
        /// <para>If the input is NULL the calculation is performed normally - i.e. WITH artefacts.</para>
        /// <para>The input dictionary has to contain all parameters necessary for the calculation.</para>
        /// <para>Any additional parameters will not be used and / or changed.</para>
        /// </summary>
        /// <param name="_param_values"></param>
        public void PerformCalculationWoArtefacts(ref Dictionary<string, double> _param_values)
        {
            if (_param_values == null)
            {
                this.PerformCalculation();
                return;
            }

            double result = double.NaN;
            Dictionary<string, double> params_in_value = new Dictionary<string, double>();
            foreach (var entry in this.input_params)
            {
                Parameter pI = entry.Value;
                if (pI == null) continue;
                if (_param_values.ContainsKey(pI.Name))
                    params_in_value.Add(entry.Key, _param_values[pI.Name]);
                else
                    params_in_value.Add(entry.Key, pI.ValueCurrent);
            }

            try
            {
                Func<double> func = Calculation.CALCULATOR.ParseExpression(this.Expression, params_in_value).Compile();
                if (func != null)
                    result = func.Invoke();
            }
            catch
            {
                result = double.NaN;
            }

            // assign the return value
            foreach (var entry in this.ReturnParams)
            {
                Parameter pR = entry.Value;
                if (pR == null) continue;
                if (_param_values.ContainsKey(pR.Name))
                    _param_values[pR.Name] = result;

            }

        }

        #endregion

        #region METHODS: Info



        #endregion

        #region METHODS: To String

        public override string ToString()
        {
            string output = this.ID + ": " + this.Name;
            
            output += " [ ";
            foreach (var entry in this.ReturnParams)
            {
                output += (entry.Value == null) ? entry.Key + " " : entry.Value.Name + " ";
            }
            output += "]= ";
            output += " {" + this.Expression + "} ";

            return output;
        }

        public string ToShortString()
        {
            string output = string.Empty;

            output += " [ ";
            foreach (var entry in this.ReturnParams)
            {
                if (entry.Value == null) continue;

                output += entry.Value.Name + " ";
            }
            output += "]= " + this.Expression;

            return output;
        }

        public string ToInfoString()
        {
            return "{" + this.ID + "}" + this.Name + " [" + this.Expression + "]";
        }

        public string ToLongString()
        {
            string output = string.Empty;

            output += " [ ";
            foreach (var entry in this.ReturnParams)
            {
                if (entry.Value == null) continue;

                output += "{" + entry.Value.ID + "}" + entry.Value.Name + " ";
            }
            output += "]= ";

            string expression_rep = this.Expression;
            foreach(var entry in this.InputParams)
            {
                expression_rep = expression_rep.Replace(entry.Key, entry.Key + ": {" + entry.Value.ID + "}" + entry.Value.Name);
            }
            output += expression_rep;

            return output;
        }

        private string ToMathEuqasion()
        {
            string eq = "[ ";
            foreach (var entry in this.ReturnParams)
            {
                if (entry.Value == null) continue;

                eq += entry.Value.Name + " ";
            }
            eq += "]= ";

            string expression_w_pNames = this.Expression;
            foreach(var entry in this.InputParams)
            {
                if (entry.Value != null)
                    expression_w_pNames = expression_w_pNames.Replace(entry.Key, Calculation.NaturalToSunbscriptString(entry.Value.ID) + entry.Value.Name);
            }

            eq += expression_w_pNames;
            return eq;
        }

        private static string NaturalToSunbscriptString(long _nr)
        {
            string nr_str = _nr.ToString();

            nr_str = nr_str.Replace("0", "₀");
            nr_str = nr_str.Replace("1", "₁");
            nr_str = nr_str.Replace("2", "₂");
            nr_str = nr_str.Replace("3", "₃");
            nr_str = nr_str.Replace("4", "₄");
            nr_str = nr_str.Replace("5", "₅");
            nr_str = nr_str.Replace("6", "₆");
            nr_str = nr_str.Replace("7", "₇");
            nr_str = nr_str.Replace("8", "₈");
            nr_str = nr_str.Replace("9", "₉");

            return nr_str;
        }

        public void AddToExport(ref StringBuilder _sb)
        {
            if (_sb == null) return;

            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.CALCULATION);                             // CALCULATION

            _sb.AppendLine(((int)ParamStructCommonSaveCode.CLASS_NAME).ToString());
            _sb.AppendLine(this.GetType().ToString());

            // general
            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_ID).ToString());
            _sb.AppendLine(this.ID.ToString());

            _sb.AppendLine(((int)CalculationSaveCode.NAME).ToString());
            _sb.AppendLine(this.Name);

            _sb.AppendLine(((int)CalculationSaveCode.EXPRESSION).ToString());
            _sb.AppendLine(this.Expression);

            // parameter: input -> saves only REFERENCES
            _sb.AppendLine(((int)CalculationSaveCode.PARAMS_INPUT).ToString());
            _sb.AppendLine(this.InputParams.Count().ToString());

            foreach(var entry in this.InputParams)
            {
                if (entry.Value == null) continue;
                Parameter p = entry.Value;

                _sb.AppendLine(((int)ParamStructCommonSaveCode.STRING_VALUE).ToString());
                _sb.AppendLine(entry.Key);

                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_REF).ToString());
                _sb.AppendLine(p.ID.ToString());
            }

            // parameter: output -> saves only REFERENCES
            _sb.AppendLine(((int)CalculationSaveCode.PARAMS_OUTPUT).ToString());
            _sb.AppendLine(this.ReturnParams.Count().ToString());

            foreach(var entry in this.ReturnParams)
            {
                if (entry.Value == null) continue;
                Parameter p = entry.Value;

                _sb.AppendLine(((int)ParamStructCommonSaveCode.STRING_VALUE).ToString());
                _sb.AppendLine(entry.Key);

                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_REF).ToString());
                _sb.AppendLine(p.ID.ToString());
            }
        }

        #endregion

        #region METHODS: Comparison

        public override void CompareWith<T>(List<T> _cc)
        {
            if (this.CompResult_Done) return;
            if (_cc == null) return;

            ComparisonResult tmp = this.CompResult;
            foreach (ComparableContent entry in _cc)
            {
                if (!(entry is Calculation))
                    continue;
                if (entry.CompResult_Done)
                    continue;

                Calculation calc = entry as Calculation;
                ComparisonResult tmp_c = calc.CompResult;

                if (tmp == ComparisonResult.UNIQUE)
                {
                    if (Calculation.HaveSameOutput(this, calc))
                    {
                        tmp = ComparisonResult.SAMENAME_DIFFUNIT;
                        this.Buddy = calc;
                        if (tmp_c <= ComparisonResult.SAMENAME_DIFFUNIT)
                        {
                            tmp_c = ComparisonResult.SAMENAME_DIFFUNIT;
                            calc.Buddy = this;
                        }
                    }
                }
                if (tmp == ComparisonResult.SAMENAME_DIFFUNIT)
                {
                    if (Calculation.HaveSameOutput(this, calc) &&
                        Calculation.HaveSameInput(this, calc))
                    {
                        tmp = ComparisonResult.SAMENAMEUNIT_DIFFVALUE;
                        this.Buddy = calc;
                        if (tmp_c <= ComparisonResult.SAMENAMEUNIT_DIFFVALUE)
                        {
                            tmp_c = ComparisonResult.SAMENAMEUNIT_DIFFVALUE;
                            calc.Buddy = this;
                        }
                    }
                }
                if (tmp == ComparisonResult.SAMENAMEUNIT_DIFFVALUE)
                {
                    if (Calculation.HaveSameOutput(this, calc) &&
                        Calculation.HaveSameInput(this, calc) &&
                        Calculation.HaveSameExpression(this, calc))
                    {
                        this.CompResult = ComparisonResult.SAME;
                        this.CompResult_Done = true;
                        this.Buddy = calc;
                        if (tmp_c <= ComparisonResult.SAME)
                        {
                            calc.CompResult = ComparisonResult.SAME;
                            calc.CompResult_Done = true;
                            calc.Buddy = this;
                        }
                        return;
                    }
                }
                calc.CompResult = tmp_c;
            }

            this.CompResult = tmp;
            this.CompResult_Done = true;
        }

        public override void AdoptPropertiesOfBuddy()
        {
            if (this.CompResult != ComparisonResult.SAMENAMEUNIT_DIFFVALUE) return;
            if (this.Buddy == null) return;
            Calculation c = this.Buddy as Calculation;
            if (c == null) return;

            this.Expression = c.Expression;
        }

        #endregion
    }

}
