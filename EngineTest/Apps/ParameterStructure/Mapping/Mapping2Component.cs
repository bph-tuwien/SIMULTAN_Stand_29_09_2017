using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ParameterStructure.Parameter;
using ParameterStructure.Component;
using ParameterStructure.DXF;

namespace ParameterStructure.Mapping
{
    #region ENUMS

    public enum Mapping2ComponentSaveCode
    {
        NAME = 1701,
        CALCULATOR = 1702,
        INPUT_MAPPING = 1703,
        INPUT_MAPPING_KEY = 1704,
        INPUT_MAPPING_VALUE = 1705,
        OUTPUT_MAPPING = 1706,
        OUTPUT_MAPPING_KEY = 1707,
        OUTPUT_MAPPING_VALUE = 1708
    }

    #endregion

    public class Mapping2Component
    {
        #region PROPERTIES

        public string Name { get; set; }

        private Component.Component calculator;
        public Component.Component Calculator 
        {
            get { return this.calculator; }
            internal set
            {
                this.calculator = value;
                if (this.calculator != null)
                    this.calculator.SaveDefaultValuesBeforeCalculation();                
            }
        }

        public long ParsingCalculatorID { get; private set; }

        // the dictionaries do not contain the actual mapped parameters
        // so that theycannot be manipulated from here
        private Dictionary<long, long> input_mapping;
        public Dictionary<long, long> InputMapping { get { return new Dictionary<long, long>(this.input_mapping); } }


        private Dictionary<long, long> output_mapping;
        public Dictionary<long, long> OutputMapping { get { return new Dictionary<long, long>(this.output_mapping); } }

        #endregion

        #region .CTOR
        public Mapping2Component(string _name, Component.Component _calculator, Dictionary<long, long> _input_mapping, Dictionary<long, long> _output_mapping)
        {
            this.Name = _name;
            this.Calculator = _calculator;
            this.input_mapping = _input_mapping;
            this.output_mapping = _output_mapping;
        }

        #endregion

        #region PARSING .CTOR
        internal Mapping2Component(string _name, long _calculator_id, Dictionary<long, long> _input_mapping, Dictionary<long, long> _output_mapping)
        {
            this.Name = _name;
            this.ParsingCalculatorID = _calculator_id;
            this.input_mapping = _input_mapping;
            this.output_mapping = _output_mapping;
        }

        #endregion

        /// <summary>
        /// Call when copying a component.
        /// </summary>
        /// <param name="_parameter_copy_record">key = id of parameter in the original, value = id of the parameter in the copy</param>
        /// <returns></returns>
        internal Mapping2Component CopyForDataCarrier(Dictionary<long, long> _parameter_copy_record)
        {
            if (_parameter_copy_record == null) return null;

            // adapt the mappings
            Dictionary<long, long> new_input_mapping = new Dictionary<long, long>();
            foreach(var entry in this.input_mapping)
            {
                long pid_data_orig = entry.Key;
                long pid_calculator = entry.Value;
                
                if (!_parameter_copy_record.ContainsKey(pid_data_orig))
                    return null;
                
                long pid_data_copy = _parameter_copy_record[pid_data_orig];
                new_input_mapping.Add(pid_data_copy, pid_calculator);
            }

            Dictionary<long, long> new_output_mapping = new Dictionary<long, long>();
            foreach(var entry in this.output_mapping)
            {
                long pid_data_orig = entry.Key;
                long pid_calculator = entry.Value;

                if (!_parameter_copy_record.ContainsKey(pid_data_orig))
                    return null;

                long pid_data_copy = _parameter_copy_record[pid_data_orig];
                new_output_mapping.Add(pid_data_copy, pid_calculator);
            }

            return new Mapping2Component(this.Name, this.Calculator, new_input_mapping, new_output_mapping);
        }

        internal void Adjust(Dictionary<long, long> _input_mapping, Dictionary<long, long> _output_mapping)
        {
            this.input_mapping = _input_mapping;
            this.output_mapping = _output_mapping;
        }

        #region TO_STRING

        public override string ToString()
        {
            return this.Name + " to " + this.Calculator.ToInfoString();

        }

        public void AddToExport(ref StringBuilder _sb)
        {
            if (_sb == null) return;

            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.MAPPING_TO_COMP);                         // MAPPING2COMPONENT

            _sb.AppendLine(((int)ParamStructCommonSaveCode.CLASS_NAME).ToString());
            _sb.AppendLine(this.GetType().ToString());

            // name
            _sb.AppendLine(((int)Mapping2ComponentSaveCode.NAME).ToString());
            _sb.AppendLine(this.Name);

            // calculator
            _sb.AppendLine(((int)Mapping2ComponentSaveCode.CALCULATOR).ToString());
            _sb.AppendLine(this.Calculator.ID.ToString());

            // input mapping
            _sb.AppendLine(((int)Mapping2ComponentSaveCode.INPUT_MAPPING).ToString());
            _sb.AppendLine(this.input_mapping.Count.ToString());

            foreach(var entry in this.input_mapping)
            {
                _sb.AppendLine(((int)Mapping2ComponentSaveCode.INPUT_MAPPING_KEY).ToString());
                _sb.AppendLine(entry.Key.ToString());

                _sb.AppendLine(((int)Mapping2ComponentSaveCode.INPUT_MAPPING_VALUE).ToString());
                _sb.AppendLine(entry.Value.ToString());
            }

            // output mapping
            _sb.AppendLine(((int)Mapping2ComponentSaveCode.OUTPUT_MAPPING).ToString());
            _sb.AppendLine(this.output_mapping.Count.ToString());
            foreach(var entry in this.output_mapping)
            {
                _sb.AppendLine(((int)Mapping2ComponentSaveCode.OUTPUT_MAPPING_KEY).ToString());
                _sb.AppendLine(entry.Key.ToString());

                _sb.AppendLine(((int)Mapping2ComponentSaveCode.OUTPUT_MAPPING_VALUE).ToString());
                _sb.AppendLine(entry.Value.ToString());
            }
        }

        #endregion
    }
}
