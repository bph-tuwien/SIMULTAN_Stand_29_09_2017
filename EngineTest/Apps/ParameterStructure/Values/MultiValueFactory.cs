using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

using ParameterStructure.DXF;

namespace ParameterStructure.Values
{
    public class MultiValueFactory
    {
        public static readonly string VALUE_RECORD_FILE_NAME = "MultiValueRecord";

        private List<MultiValue> value_record;
        public IReadOnlyCollection<MultiValue> ValueRecord { get { return this.value_record.AsReadOnly(); } }

        public MultiValueFactory()
        {
            this.value_record = new List<MultiValue>();
        }

        #region FACTORY METHODS: Value Fields

        public MultiValueTable CreateValueTable(List<double> _Xs, string _unit_x, 
                                                List<double> _Ys, string _unit_y, 
                                                List<double> _Zs, string _unit_z,
                                                List<double> _Fs, bool _can_interpolate, string _name)
        {
            // determine which constructor to call
            if (_Fs == null || _Fs.Count < 1) return null;
            if (_Xs == null || _Xs.Count < 1) return null;
            
            MultiValueTable created = null;
            if (_Ys == null || _Ys.Count < 2)
                created = new MultiValueTable1D(_Xs, _unit_x, _unit_y, _unit_z, _Fs, _can_interpolate, _name);
            else if (_Zs == null || _Zs.Count < 2)
                created = new MultiValueTable2D(_Xs, _unit_x, _Ys, _unit_y, _unit_z,_Fs, _can_interpolate, _name);
            else
                created = new MultiValueTable3D(_Xs, _unit_x, _Ys, _unit_y, _Zs, _unit_z, _Fs, _can_interpolate, _name);

            // check if a valid value table was created
            if (created == null) return null;
            if (created.NrX == 0) return null;

            this.value_record.Add(created);
            return created;
        }

        // only call when parsing
        internal MultiValueTable ReconstructTable(long _id, string _name, bool _can_interpolate, MultiValPointer _disp_vect,
                                                  int _nr_x, double _min_x, double _max_x, string _unit_x,
                                                  int _nr_y, double _min_y, double _max_y, string _unit_y,
                                                  int _nr_z, double _min_z, double _max_z, string _unit_z,
                                    List<double> _Xs, List<double> _Ys, List<double> _Zs, Dictionary<Point3D, double> _Fs)
        {
            if (_id < 0) return null;
            // determine which constructor to call
            if (_Fs == null || _Fs.Count < 1) return null;
            if (_Xs == null || _Xs.Count < 1) return null;

            MultiValueTable created = null;
            if (_Ys == null || _Ys.Count < 2)
                created = new MultiValueTable1D(_id, _name, _can_interpolate, _disp_vect,
                                                _nr_x, _min_x, _max_x, _unit_x, 
                                                _Xs, _unit_y, _unit_z, _Fs);
            else if (_Zs == null || _Zs.Count < 2)
                created = new MultiValueTable2D(_id, _name, _can_interpolate, _disp_vect,
                                                _nr_x, _min_x, _max_x, _unit_x,
                                                _nr_y, _min_y, _max_y, _unit_y,
                                                _Xs, _Ys, _unit_z, _Fs);
            else
                created = new MultiValueTable3D(_id, _name, _can_interpolate, _disp_vect,
                                                _nr_x, _min_x, _max_x, _unit_x,
                                                _nr_y, _min_y, _max_y, _unit_y,
                                                _nr_z, _min_z, _max_z, _unit_z,
                                                _Xs, _Ys, _Zs, _Fs);

            // check if a valid value table was created
            if (created == null) return null;
            if (created.NrX == 0) return null;

            // create
            this.value_record.Add(created);
            // adjust type counter
            MultiValue.NR_MULTI_VALUES = Math.Max(MultiValue.NR_MULTI_VALUES, created.MVID);
            // done
            return created;
        }


        public void UpdateValueTable(MultiValueTable _to_update,
                                        List<double> _Xs, string _unit_x,
                                        List<double> _Ys, string _unit_y,
                                        List<double> _Zs, string _unit_z,
                                        List<double> _Fs, bool _can_interpolate, string _name)
        {
            if (_to_update == null) return;
            if (_Xs == null || _Ys == null || _Zs == null || _Fs == null) return;
            if (!this.value_record.Contains(_to_update)) return;

            // remove old
            bool successful_delete = this.DeleteRecord(_to_update.MVID);
            if (!successful_delete) return;

            // create new
            Dictionary<Point3D, double> field = MultiValueFactory.ConvertToDictionary(_Fs, _Xs.Count, _Ys.Count, _Zs.Count);
            if (field.Count < 1) return;

            double min_x = _Xs.Min();
            double max_x = _Xs.Max();
            double min_y = _Ys.Min();
            double max_y = _Ys.Max();
            double min_z = _Zs.Min();
            double max_z = _Zs.Max();

            MultiValueTable created = this.ReconstructTable(_to_update.MVID, _name, _can_interpolate, _to_update.MVDisplayVector,
                                                            _Xs.Count, min_x, max_x, _unit_x,
                                                            _Ys.Count, min_y, max_y, _unit_y,
                                                            _Zs.Count, min_z, max_z, _unit_z,
                                                            _Xs, _Ys, _Zs, field);
        }

        #endregion

        #region FACTORY METHODS: Function Fields

        public MultiValueFunction CreateFunction(string _unit_x, string _unit_y, string _unit_z, Point4D _bounds,
                                                 List<double> _Zs, List<List<Point3D>> _functions, List<string> _fct_names, string _name)
        {
            if (_Zs == null || _Zs.Count < 1 || _functions == null || _functions.Count < 1 ||
                _fct_names == null || _fct_names.Count != _functions.Count) return null;

            MultiValueFunction created = new MultiValueFunctionND(_unit_x, _unit_y, _unit_z, _bounds, _Zs, _functions, _fct_names, _name);
            if (created == null) return null;
            if (created.NrZ == 0) return null;

            this.value_record.Add(created);
            return created;
        }

        // call only when parsing
        internal MultiValueFunction ReconstructFunction(long _id, string _name, bool _can_interpolate, MultiValPointer _disp_vect,
                                                        string _unit_x, string _unit_y, string _unit_z, Point4D _bounds,
                                                        List<double> _Zs, List<List<Point3D>> _Functions, List<string> _Fct_Names)
        {
            if (_id < 0) return null;
            if (_Zs == null || _Zs.Count < 1) return null;
            if (_Functions == null || _Functions.Count < 1) return null;
            if (_Fct_Names == null || _Fct_Names.Count != _Functions.Count) return null;

            MultiValueFunction created = new MultiValueFunctionND(_id, _name, _can_interpolate, _disp_vect, 
                                                                    _unit_x, _unit_y, _unit_z, _bounds, _Zs, _Functions, _Fct_Names);

            // check if a valid function table was created
            if (created == null) return null;
            if (created.NrZ == 0) return null;

            // create
            this.value_record.Add(created);
            // adjust type counter
            MultiValue.NR_MULTI_VALUES = Math.Max(MultiValue.NR_MULTI_VALUES, created.MVID);
            // done
            return created;
        }

        public void UpdateFunction(MultiValueFunction _to_update,
                                   string _unit_x, string _unit_y, string _unit_z, Point4D _bounds,
                                   List<double> _Zs, List<List<Point3D>> _Functions, List<string> _Fct_Names, string _name)
        {
            if (_to_update == null) return;
            if (_bounds == null || _Zs == null || _Functions == null || _Fct_Names == null) return;
            if (!this.value_record.Contains(_to_update)) return;

            // remove old
            bool successful_delete = this.DeleteRecord(_to_update.MVID);
            if (!successful_delete) return;

            // create new
            MultiValueFunction created = this.ReconstructFunction(_to_update.MVID, _name, _to_update.MVCanInterpolate, _to_update.MVDisplayVector,
                                                                  _unit_x, _unit_y, _unit_z, _bounds, _Zs, _Functions, _Fct_Names);
        }

        #endregion

        #region FACTORY METHODS: Big EXCEL Tables

        public MultiValueBigTable CreateBigTable(string _name, List<string> _names, List<string> _units, List<List<double>> _values, List<string> _row_names = null)
        {
            MultiValueBigTable created = null;
            if (_row_names == null)
                created = new MultiValueBigTable(_name, _names, _units, _values);
            else
                created = new MultiValueBigTable(_name, _names, _units, _values, _row_names);

            if (created == null) return null;
            if (created.Values.Count == 0) return null;

            // check if a valid value table was created
            this.value_record.Add(created);
            return created;
        }

        // only call when parsing
        internal MultiValueBigTable ReconstructBigTable(long _id, string _name, MultiValPointer _disp_vect,
                                                        string _unit_x, string _unit_y, string _unit_z,
                                                        List<string> _names, List<string> _units, 
                                                        List<List<double>> _values, List<string> _row_names)
        {
            if (_id < 0) return null;
            MultiValueBigTable created = new MultiValueBigTable(_id, _name, _disp_vect, _unit_x, _unit_y, _unit_z, _names, _units, _values, _row_names);

            // check if a valid value table was created
            if (created == null) return null;
            if (created.Values.Count == 0) return null;

            // create
            this.value_record.Add(created);
            // adjust type counter
            MultiValue.NR_MULTI_VALUES = Math.Max(MultiValue.NR_MULTI_VALUES, created.MVID);
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

            if (this.value_record.Count > 0)
            {
                foreach (var record in this.value_record)
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
            MultiValue found = this.value_record.Find(x => x.MVID == _record_id);
            if (found == null) return false;

            this.value_record.Remove(found);
            return true;
        }

        public MultiValue CopyRecord(MultiValue _record)
        {
            if (_record == null) return null;
            if (!this.value_record.Contains(_record)) return null;

            MultiValue copy = _record.Clone();
            if (copy != null)
                this.value_record.Add(copy);

            return copy;
        }

        public MultiValue CopyWithoutRecord(MultiValue _original)
        {
            if (_original == null) return null;
            MultiValue copy = _original.Clone();
            return copy;
        }

        public void ClearRecord()
        {
            this.value_record.Clear();
        }

        #endregion

        #region METHODS: Getter

        public MultiValue GetByID(long _id)
        {
            return this.value_record.Find(x => x.MVID == _id);
        }

        #endregion

        #region METHODS: Utils

        private static Dictionary<Point3D, double> ConvertToDictionary(List<double> _data, int _nr_x, int _nr_y, int _nr_z)
        {
            Dictionary<Point3D, double> converted = new Dictionary<Point3D, double>();
            if (_data == null || _data.Count < 1 || _data.Count != _nr_x * _nr_y * _nr_z) return converted;

            for (int table = 0; table < _nr_z; table++)
            {
                for (int r = 0; r < _nr_y; r++)
                {
                    for (int c = 0; c < _nr_x; c++)
                    {
                        converted.Add(new Point3D(c, r, table), _data[table * _nr_y * _nr_x + r * _nr_x + c]);
                    }
                }
            }
            return converted;
        }

        #endregion
    }
}
