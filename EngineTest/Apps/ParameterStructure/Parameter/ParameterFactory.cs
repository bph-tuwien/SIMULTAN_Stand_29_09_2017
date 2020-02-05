using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ParameterStructure.DXF;
using ParameterStructure.Values;
using ParameterStructure.Component;

namespace ParameterStructure.Parameter
{
    public class ParameterFactory
    {
        public const string PARAMETER_RECORD_FILE_NAME = "ParameterRecord";

        private List<Parameter> parameter_record;
        public IReadOnlyCollection<Parameter> ParameterRecord { get { return this.parameter_record.AsReadOnly(); } }

        public ParameterFactory()
        {
            this.parameter_record = new List<Parameter>();
        }

        #region METHODS: Factory Methods

        public Parameter CreateParameter()
        {
            Parameter created = new Parameter();
            if (created != null)
                this.parameter_record.Add(created);

            return created;
        }

        // only call when parsing
        internal Parameter ReconstructParameter(long _id, string _name, string _unit, Category _category, InfoFlow _propagation,
                                                double _value_min, double _value_max, double _value_current, bool _is_within_bounds,
                                                MultiValue _value_field, long _value_field_ref, MultiValPointer _value_field_pointer,
                                                DateTime _time_stamp, string _text_value)
        {
            Parameter created = new Parameter( _id, _name, _unit, _category, _propagation,
                                               _value_min, _value_max, _value_current, _is_within_bounds,
                                               _text_value, _value_field_ref, _value_field_pointer);

            // value field
            created.ValueField = _value_field; // resets ValueFieldRef und MValPointer (e.g. if the field is NULL)
            if (_value_field != null)
                created.MValPointer = _value_field_pointer; // DO NOT FORGET to corrct the MValPointer
            
            // time stamp
            if (_time_stamp != null)
                created.TimeStamp = _time_stamp;
            else
                created.TimeStamp = DateTime.Now;

            // check if a valid value table was created
            if (created == null) return null;

            // create
            this.parameter_record.Add(created);
            // adjust type counter
            Parameter.NR_PARAMS = Math.Max(Parameter.NR_PARAMS, created.ID);
            // done
            return created;
        }

        #endregion

        #region METHODS: DXF Export

        public StringBuilder ExportRecord(bool _finalize = false)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_START);
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_NAME).ToString());
            sb.AppendLine(ParamStructTypes.ENTITY_SECTION);

            if (this.parameter_record.Count > 0)
            {
                foreach (Parameter record in this.parameter_record)
                {
                    record.AddToExport(ref sb);
                }
            }

            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_END);

            if (_finalize)
            {
                sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
                sb.AppendLine(ParamStructTypes.EOF);
            }

            return sb;
        }


        #endregion

        #region METHODS: Record Management

        public bool DeleteRecord(long _record_id)
        {
            Parameter found = this.parameter_record.Find(x => x.ID == _record_id);
            if (found == null) return false;

            this.parameter_record.Remove(found);
            return true;
        }

        public void ClearRecord()
        {
            this.parameter_record.Clear();
        }

        public Parameter CopyRecord(Parameter _record)
        {
            if (_record == null) return null;
            if (!this.parameter_record.Contains(_record)) return _record;

            Parameter copy = _record.Clone();
            if (copy != null)
                this.parameter_record.Add(copy);

            return copy;
        }

        public Parameter CopyWithoutRecord(Parameter _original)
        {
            if (_original == null) return null;
            Parameter copy = _original.Clone();
            return copy;
        }

        public void CreatePointerParameters(Parameter _p, out Parameter pX, out Parameter pY, out Parameter pZ, out Parameter pS)
        {
            pX = null; pY = null; pZ = null; pS = null;
            if (_p == null) return;
            if (_p.ValueField == null) return;

            // create params according to value field type

            pX = new Parameter(_p.Name + Parameter.POINTER_X_NAME_TAG, "-", double.NaN);
            pX.ValueMin = double.MinValue;
            pX.ValueMax = double.MaxValue;
            pX.TextValue = _p.Name + Parameter.POINTER_X;

            if (_p.ValueField is MultiValueTable || _p.ValueField is MultiValueBigTable)
            {
                pY = new Parameter(_p.Name + Parameter.POINTER_Y_NAME_TAG, "-", double.NaN);
                pY.ValueMin = double.MinValue;
                pY.ValueMax = double.MaxValue;
                pY.TextValue = _p.Name + Parameter.POINTER_Y;
            }

            if (_p.ValueField is MultiValueTable)
            {
                pZ = new Parameter(_p.Name + Parameter.POINTER_Z_NAME_TAG, "-", double.NaN);
                pZ.ValueMin = double.MinValue;
                pZ.ValueMax = double.MaxValue;
                pZ.TextValue = _p.Name + Parameter.POINTER_Z;
            }

            if (_p.ValueField is MultiValueFunction || _p.ValueField is MultiValueBigTable)
            {
                pS = new Parameter(_p.Name + Parameter.POINTER_STRING_NAME_TAG, "-", double.NaN);
                pS.ValueMin = double.MinValue;
                pS.ValueMax = double.MaxValue;
                pS.TextValue = _p.Name + Parameter.POINTER_STRING;
            }
        }

        #endregion

        #region METHODS: Getter

        public List<Parameter> GetByName(string _pname)
        {
            if (string.IsNullOrEmpty(_pname)) return new List<Parameter>();
            
            List<Parameter> found = this.parameter_record.FindAll(x => x.Name == _pname).ToList();
            if (found != null)
                return found;
            else
                return new List<Parameter>();
        }

        public Parameter GetByID(long _id)
        {
            return this.parameter_record.Find(x => x.ID == _id);            
        }

        #endregion
    }
}
