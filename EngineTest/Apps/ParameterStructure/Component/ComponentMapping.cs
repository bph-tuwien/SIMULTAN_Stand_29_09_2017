using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

using ParameterStructure.Mapping;

namespace ParameterStructure.Component
{
    public enum Comp2CompMappingErr
    {
        NONE,
        COULD_NOT_EVALUATE,
        SELF_REFERENCE,
        NOT_ALL_INPUT_ASSIGNED,
        NOT_ALL_OUTPUT_ASSIGNED,
        NO_CALCULATION_FOUND,
        DUPLICATE_CALCULATOR,
    }


    public partial class Component : DisplayableProductDefinition
    {
        #region PROPERTIES:

        private List<Mapping2Component> mappings_to_comps;
        public ReadOnlyCollection<Mapping2Component> Mappings2Comps { get { return this.mappings_to_comps.AsReadOnly(); } }

        public List<Component> MappedToBy { get; private set; }

        #endregion

        #region METHODS: Pre-Mapping Evaluation

        public bool MappingToCompExists(Component _calculator)
        {
            Mapping2Component duplicate_mapping = this.mappings_to_comps.FirstOrDefault(x => x.Calculator.ID == _calculator.ID);
            return (duplicate_mapping != null);
        }

        public Comp2CompMappingErr MappingPreview(Component _calculator, Dictionary<Parameter.Parameter, Parameter.Parameter> _input_mapping, 
                                                                         Dictionary<Parameter.Parameter, Parameter.Parameter> _output_mapping)
        {
            if (_calculator == null)
                return Comp2CompMappingErr.COULD_NOT_EVALUATE;

            if (_calculator.ID == this.ID)
                return Comp2CompMappingErr.SELF_REFERENCE;

            Mapping2Component duplicate_mapping = this.mappings_to_comps.FirstOrDefault(x => x.Calculator.ID == _calculator.ID);
            if (duplicate_mapping != null)
                return Comp2CompMappingErr.DUPLICATE_CALCULATOR;

            if (_input_mapping == null || _input_mapping.Count == 0)
                return Comp2CompMappingErr.NOT_ALL_INPUT_ASSIGNED;

            if (_output_mapping == null || _output_mapping.Count == 0)
                return Comp2CompMappingErr.NOT_ALL_OUTPUT_ASSIGNED;

            foreach (Parameter.Parameter p in _output_mapping.Values)
            {
                if (p.Propagation != InfoFlow.MIXED && p.Propagation != InfoFlow.OUPUT)
                    return Comp2CompMappingErr.NOT_ALL_OUTPUT_ASSIGNED;

                List<Parameter.Parameter> required_input_for_p = _calculator.GetInputParamsInvolvedInTheCalculationOf(p);
                if (required_input_for_p.Count == 0)
                    return Comp2CompMappingErr.NO_CALCULATION_FOUND;

                // partial input acceptable
                //foreach (Parameter.Parameter ip in required_input_for_p)
                //{
                //    if (!(_input_mapping.ContainsValue(ip)))
                //        return Comp2CompMappingErr.NOT_ALL_INPUT_ASSIGNED;
                //}
            }

            return Comp2CompMappingErr.NONE;
        }

        public Comp2CompMappingErr MappingEditPreview(Mapping2Component _map, Dictionary<Parameter.Parameter, Parameter.Parameter> _new_input_mapping, 
                                                                              Dictionary<Parameter.Parameter, Parameter.Parameter> _new_output_mapping)
        {
            if (!(this.Mappings2Comps.Contains(_map))) return Comp2CompMappingErr.COULD_NOT_EVALUATE;

            if (_new_input_mapping == null || _new_input_mapping.Count == 0)
                return Comp2CompMappingErr.NOT_ALL_INPUT_ASSIGNED;

            if (_new_output_mapping == null || _new_output_mapping.Count == 0)
                return Comp2CompMappingErr.NOT_ALL_OUTPUT_ASSIGNED;

            foreach (Parameter.Parameter p in _new_output_mapping.Values)
            {
                if (p.Propagation != InfoFlow.MIXED && p.Propagation != InfoFlow.OUPUT)
                    return Comp2CompMappingErr.NOT_ALL_OUTPUT_ASSIGNED;

                List<Parameter.Parameter> required_input_for_p = _map.Calculator.GetInputParamsInvolvedInTheCalculationOf(p);
                if (required_input_for_p.Count == 0)
                    return Comp2CompMappingErr.NO_CALCULATION_FOUND;
            }

            return Comp2CompMappingErr.NONE;
        }

        #endregion

        #region METHODS: Mapping

        /// <summary>
        /// Called by the component carrying the data to input into the calculator.
        /// </summary>
        /// <param name="_name"></param>
        /// <param name="_calculator">Component carrying the calculation(s)</param>
        /// <param name="_input_mapping">key = source parameter id, value = target parameter id</param>
        /// <param name="_output_mapping"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        public Mapping2Component CreateMappingTo(string _name, Component _calculator, Dictionary<Parameter.Parameter, Parameter.Parameter> _input_mapping,
                                                                                      Dictionary<Parameter.Parameter, Parameter.Parameter> _output_mapping, out Comp2CompMappingErr err)
        {
            err = this.MappingPreview(_calculator, _input_mapping, _output_mapping);
            if (err != Comp2CompMappingErr.NONE)
                return null;

            Dictionary<long, long> input_mapping = _input_mapping.Select(x => new KeyValuePair<long, long>(x.Key.ID, x.Value.ID)).ToDictionary(x => x.Key, x => x.Value);
            Dictionary<long, long> output_mapping = _output_mapping.Select(x => new KeyValuePair<long, long>(x.Key.ID, x.Value.ID)).ToDictionary(x => x.Key, x => x.Value);

            return this.CreateMappingTo(_name, _calculator, input_mapping, output_mapping);
        }

        /// <summary>
        /// Called by the component carrying the data to input into the calculator.
        /// </summary>
        /// <param name="_calculator">Component carrying the calculation(s)</param>
        /// <param name="_input_mapping">key = source parameter id, value = target parameter id</param>
        private Mapping2Component CreateMappingTo(string _name, Component _calculator, Dictionary<long, long> _input_mapping, Dictionary<long, long> _output_mapping)
        {
            if (_calculator == null) return null;
            if (_calculator.ID == this.ID) return null;
            if (_input_mapping == null || _output_mapping == null) return null;
            if (_input_mapping.Count == 0 || _output_mapping.Count == 0) return null;

            _calculator.MappedToBy.Add(this);

            Mapping2Component mapping = new Mapping2Component(_name, _calculator, _input_mapping, _output_mapping);
            this.mappings_to_comps.Add(mapping);
            return mapping;
        }

        public bool RemoveMapping(Mapping2Component _mapping)
        {
            if (_mapping == null) return false;

            if (_mapping.Calculator != null)
                _mapping.Calculator.MappedToBy.Remove(this);

            return this.mappings_to_comps.Remove(_mapping);
        }


        internal bool RemoveMappingTo(Component _calc)
        {
            if (_calc == null) return false;

            Mapping2Component to_remove = this.mappings_to_comps.FirstOrDefault(x => x.Calculator != null && x.Calculator.ID == _calc.ID);
            if (to_remove != null)
                return this.RemoveMapping(to_remove);
            else
                return false;
        }

        public void EditMapping(Mapping2Component _map, Dictionary<Parameter.Parameter, Parameter.Parameter> _input_mapping,
                                                        Dictionary<Parameter.Parameter, Parameter.Parameter> _output_mapping, out Comp2CompMappingErr err)
        {
            err = this.MappingEditPreview(_map, _input_mapping, _output_mapping);
            if (err != Comp2CompMappingErr.NONE) return;

            // apply changes
            Dictionary<long, long> input_mapping = _input_mapping.Select(x => new KeyValuePair<long, long>(x.Key.ID, x.Value.ID)).ToDictionary(x => x.Key, x => x.Value);
            Dictionary<long, long> output_mapping = _output_mapping.Select(x => new KeyValuePair<long, long>(x.Key.ID, x.Value.ID)).ToDictionary(x => x.Key, x => x.Value);
            _map.Adjust(input_mapping, output_mapping);
        }

        /// <summary>
        /// Recursively called by the component carrying the data to input into the calculator.
        /// </summary>
        public void EvaluateAllMappings()
        {
            foreach(var entry in this.ContainedComponents)
            {
                Component sC = entry.Value;
                if (sC == null) continue;

                sC.EvaluateAllMappings();
            }
            foreach(Mapping2Component mapping in this.mappings_to_comps)
            {
                this.EvaluateMapping(mapping);
            }
        }

        /// <summary>
        /// Called by the component carrying the data to input into the calculator.
        /// </summary>
        /// <param name="_mapping"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void EvaluateMapping(Mapping2Component _mapping)
        {
            if (_mapping == null) return;

            Dictionary<long, Parameter.Parameter> all_params = this.GetFlatParamsList();

            // reset the calculator
            _mapping.Calculator.ResetToDefaultValuesBeforeCalculation();

            // gather the input values
            Dictionary<long, double> input_values = new Dictionary<long, double>();
            foreach(var entry in _mapping.InputMapping)
            {
                if (all_params.ContainsKey(entry.Key))
                    input_values.Add(entry.Value, all_params[entry.Key].ValueCurrent);
            }
            // gather the output values
            Dictionary<long, double> output_values = new Dictionary<long, double>();
            foreach(var entry in _mapping.OutputMapping)
            {
                if (all_params.ContainsKey(entry.Key))
                    output_values.Add(entry.Value, all_params[entry.Key].ValueCurrent);
                else
                    output_values.Add(entry.Value, 0.0);
            }

            // pass the parameters to the calculator
            _mapping.Calculator.CalculateAndMap(input_values, ref output_values);

            // retrieve the result
            foreach (var entry in _mapping.OutputMapping)
            {
                if (output_values.ContainsKey(entry.Value))
                    all_params[entry.Key].ValueCurrent = output_values[entry.Value];
            }
        }

        /// <summary>
        /// Called by the calculator component in the mapping.
        /// </summary>
        /// <param name="_input_values">Does not need to be complete.</param>
        /// <param name="_output_values"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void CalculateAndMap(Dictionary<long, double> _input_values, ref Dictionary<long, double> _output_values)
        {
            // check the applicability of the value lists
            if (_input_values == null || _output_values == null) return;
            if (_input_values.Count == 0 || _output_values.Count == 0) return;

            Dictionary<long, Parameter.Parameter> all_params = this.GetFlatParamsList();
            foreach (var entry in _input_values)
            {
                if (!all_params.ContainsKey(entry.Key)) return;

                Parameter.Parameter p = all_params[entry.Key];
                if (p == null) return;
                if (p.Propagation == InfoFlow.OUPUT) return;
            }

            foreach(var entry in _output_values)
            {
                if (!all_params.ContainsKey(entry.Key)) return;

                Parameter.Parameter p = all_params[entry.Key];
                if (p == null) return;
                if (p.Propagation == InfoFlow.INPUT || p.Propagation == InfoFlow.CALC_IN || p.Propagation == InfoFlow.REF_IN) return;
            }

            // apply input values:
            foreach(var entry in _input_values)
            {
                all_params[entry.Key].ValueCurrent = entry.Value;
            }

            // calculate
            this.ExecuteAllCalculationChains();

            // apply output values:
            Dictionary<long, double> results = new Dictionary<long, double>();
            foreach(var entry in _output_values)
            {
                results.Add(entry.Key, all_params[entry.Key].ValueCurrent);
            }
            foreach(var entry in results)
            {
                _output_values[entry.Key] = entry.Value;
            }
        }

        public void TranslateMapping(Mapping2Component _mapping, out Dictionary<Parameter.Parameter, Parameter.Parameter> input_mapping,
                                                                 out Dictionary<Parameter.Parameter, Parameter.Parameter> output_mapping)
        {
            input_mapping = new Dictionary<Parameter.Parameter, Parameter.Parameter>();
            output_mapping = new Dictionary<Parameter.Parameter, Parameter.Parameter>();
            if (_mapping == null) return;

            List<Parameter.Parameter> input_source = this.GetByIds(_mapping.InputMapping.Keys.ToList());
            List<Parameter.Parameter> input_target = _mapping.Calculator.GetByIds(_mapping.InputMapping.Values.ToList());
            
            if (input_source.Count != input_target.Count) return;
            input_mapping = input_source.Zip(input_target, (x, y) => new KeyValuePair<Parameter.Parameter, Parameter.Parameter>(x, y)).ToDictionary(x => x.Key, x => x.Value);

            List<Parameter.Parameter> output_source = this.GetByIds(_mapping.OutputMapping.Keys.ToList());
            List<Parameter.Parameter> output_target = _mapping.Calculator.GetByIds(_mapping.OutputMapping.Values.ToList());

            if (output_source.Count != output_target.Count) return;
            output_mapping = output_source.Zip(output_target, (x, y) => new KeyValuePair<Parameter.Parameter, Parameter.Parameter>(x, y)).ToDictionary(x => x.Key, x => x.Value);
        }

        #endregion

        #region METHODS: Default Values

        /// <summary>
        /// For calculator components: To be called only once, after the first mapping as a calculator.
        /// </summary>
        internal void SaveDefaultValuesBeforeCalculation()
        {
            if (this.R2GInstances[0].InstanceParamValues.Count > 0) return;

            Dictionary<long, Parameter.Parameter> all_params = this.GetFlatParamsList();
            Dictionary<string, double> all_param_values = new Dictionary<string, double>();
            foreach(var entry in all_params)
            {
                Parameter.Parameter p = entry.Value;
                if (p == null) continue;

                if (!(all_param_values.ContainsKey(p.Name)))
                    all_param_values.Add(p.Name, p.ValueCurrent);
            }
            this.R2GInstances[0].InstanceParamValues = all_param_values;
        }

        /// <summary>
        /// For calculator components: To be called by the user.
        /// </summary>
        public void SaveNewDefaultValues()
        {
            Dictionary<long, Parameter.Parameter> all_params = this.GetFlatParamsList();
            Dictionary<string, double> all_param_values = new Dictionary<string, double>();
            foreach (var entry in all_params)
            {
                Parameter.Parameter p = entry.Value;
                if (p == null) continue;

                if (!(all_param_values.ContainsKey(p.Name)))
                    all_param_values.Add(p.Name, p.ValueCurrent);
            }
            this.R2GInstances[0].InstanceParamValues = all_param_values;
        }

        /// <summary>
        /// For calculator components: To be called before each calculation.
        /// </summary>
        public void ResetToDefaultValuesBeforeCalculation()
        {
            List<Parameter.Parameter> all_params = this.GetFlatParamsList().Values.ToList();

            foreach(var entry in this.R2GInstances[0].InstanceParamValues)
            {
                List<Parameter.Parameter> found = all_params.FindAll(x => x.Name == entry.Key);
                foreach(Parameter.Parameter p in found)
                {
                    p.ValueCurrent = entry.Value;
                }
            }
        }

        #endregion
    }
}
