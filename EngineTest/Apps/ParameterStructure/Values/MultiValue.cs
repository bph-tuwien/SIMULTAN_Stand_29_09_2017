using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Media3D;

using ParameterStructure.DXF;
using ParameterStructure.Parameter;

namespace ParameterStructure.Values
{
    #region ENUMS
    public enum MultiValueType
    {
        SINGLE,
        FIELD_1D,
        FIELD_2D,
        FIELD_3D,
        FUNCTION_ND,
        TABLE
    }

    // [1101 - 1300]
    public enum MultiValueSaveCode
    {
        MVType = 1101,
        MVCanInterpolate = 1102,
        MVName = 1103,

        MVDisplayVector_NUMDIM = 1200,
        MVDisplayVector_CELL_INDEX_X = 1201,
        MVDisplayVector_CELL_INDEX_Y = 1202,
        MVDisplayVector_CELL_INDEX_Z = 1203,
        MVDisplayVector_CELL_INDEX_W = 1204,
        MVDisplayVector_POS_IN_CELL_REL_X = 1205,
        MVDisplayVector_POS_IN_CELL_REL_Y = 1206,
        MVDisplayVector_POS_IN_CELL_REL_Z = 1207,
        MVDisplayVector_POS_IN_CELL_ABS_X = 1208,
        MVDisplayVector_POS_IN_CELL_ABS_Y = 1209,
        MVDisplayVector_POS_IN_CELL_ABS_Z = 1210,
        MVDisplayVector_VALUE = 1211,
        MVDisplayVector_CELL_SIZE_W = 1212,
        MVDisplayVector_CELL_SIZE_H = 1213,

        MVUnitX = 1104,
        MVUnitY = 1105,
        MVUnitZ = 1106,

        NrX = 1107,
        MinX = 1108,
        MaxX = 1109,

        NrY = 1110,
        MinY = 1111,
        MaxY = 1112,

        NrZ = 1113,
        MinZ = 1114,
        MaxZ = 1115,

        XS = 1116,
        YS = 1117,
        ZS = 1118,

        FIELD = 1119,
        ROW_NAMES = 1120
    }

    #endregion

    public abstract class MultiValue : INotifyPropertyChanged
    {
        #region STATIC

        internal static long NR_MULTI_VALUES = 0;

        public static string MVTypeToString(MultiValueType _type)
        {
            switch(_type)
            {
                case MultiValueType.FIELD_1D:
                    return "FIELD_1D";
                case MultiValueType.FIELD_2D:
                    return "FIELD_2D";
                case MultiValueType.FIELD_3D:
                    return "FIELD_3D";
                case MultiValueType.FUNCTION_ND:
                    return "FUNCTION_ND";
                case MultiValueType.TABLE:
                    return "TABLE";
                default:
                    return "SINGLE";
            }
        }

        public static MultiValueType StringToMVType(string _type_as_str)
        {
            if (string.IsNullOrEmpty(_type_as_str)) return MultiValueType.SINGLE;
            switch(_type_as_str)
            {
                case "FIELD_1D":
                    return MultiValueType.FIELD_1D;
                case "FIELD_2D":
                    return MultiValueType.FIELD_2D;
                case "FIELD_3D":
                    return MultiValueType.FIELD_3D;
                case "FUNCTION_ND":
                    return MultiValueType.FUNCTION_ND;
                case "TABLE":
                    return MultiValueType.TABLE;
                default:
                    return MultiValueType.SINGLE;
            }
        }

        protected static void GetLinearCoords(double _aX, double _bX, double _x, out double a, out double b)
        {
            a = double.NaN;
            b = double.NaN;

            double diff_ax = _aX - _x;
            double diff_bx = _bX - _x;
            double diff_ab = _aX - _bX;

            a = diff_ax / diff_ab;
            b = diff_bx / -diff_ab;
        }

        protected static void GetBarycentricCoords(double aX, double aY, double bX, double bY, double cX, double cY,
                                                  double pX, double pY,
                                                  out double a, out double b, out double c)
        {
            a = double.NaN;
            b = double.NaN;
            c = double.NaN;

            // solving the eq system:
            // _xy.X = a * _xyA.X + b * _xyB.X + (1 - a - b) * _xyC.X
            // _xy.Y = a * _xyA.Y + b * _xyB.Y + (1 - a - b) * _xyC.Y
            double denominator = (bY - cY) * (aX - cX) + (cX - bX) * (aY - cY);
            if (Math.Abs(denominator) < Calculation.MIN_DOUBLE_VAL) return;

            a = ((bY - cY) * (pX - cX) + (cX - bX) * (pY - cY)) / denominator;
            b = ((pY - cY) * (aX - cX) + (cX - pX) * (aY - cY)) / denominator;
            c = 1 - a - b;
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

        #region PROPERTIES: Common

        private long mv_id;
        public long MVID
        {
            get { return this.mv_id; }
            private set
            {
                this.mv_id = value;
                this.RegisterPropertyChanged("MVID");
            }
        }

        protected MultiValueType mv_type;
        public MultiValueType MVType
        {
            get { return this.mv_type; }
            protected set
            {
                this.mv_type = value;
                this.RegisterPropertyChanged("MVType");
            }
        }

        protected string mv_name;
        public string MVName
        {
            get { return this.mv_name; }
            set 
            { 
                this.mv_name = value;
                this.RegisterPropertyChanged("MVName");
            }
        }


        private bool mv_can_interpolate;
        public bool MVCanInterpolate
        {
            get { return this.mv_can_interpolate; }
            set 
            { 
                this.mv_can_interpolate = value;
                this.RegisterPropertyChanged("MVCanInterpolate");
            }
        }

        #endregion

        #region PROPERTIES: Common Display

        // contains the selected value
        private MultiValPointer mv_display_vector;
        public MultiValPointer MVDisplayVector
        {
            get { return this.mv_display_vector; }
            set 
            { 
                this.mv_display_vector = value;
                this.RegisterPropertyChanged("MVDisplayVector");
            }
        }

        public virtual string MVDisplayVectorAsString { get { return "[ " + this.MVDisplayVector.ToString() + " ]"; } }

        #endregion

        #region PROPERTIES: Common Info
        public string MVUnitX { get; protected set; }
        public string MVUnitY { get; protected set; }
        public string MVUnitZ { get; protected set; }

        public virtual string MVUnitsAsString { get { return "[ " + this.MVUnitX + ", " + this.MVUnitY + ", " + this.MVUnitZ + " ]"; } }

        #endregion

        #region .CTOR

        internal MultiValue(MultiValueType _type, string _name)
        {
            this.MVID = (++MultiValue.NR_MULTI_VALUES);
            this.MVType = _type;
            this.MVName = (string.IsNullOrEmpty(_name)) ? "MV " + this.MVID : _name;
            this.MVCanInterpolate = false;
            this.MVDisplayVector = MultiValPointer.INVALID;

            this.MVUnitX = "-";
            this.MVUnitY = "-";
            this.MVUnitZ = "-";
        }

        #endregion

        #region .CTOR for PARSING

        internal MultiValue(long _id, MultiValueType _type, string _name, bool _can_interpolate, MultiValPointer _disp_vect)
        {
            this.MVID = _id;
            this.MVType = _type;
            this.MVName = (string.IsNullOrEmpty(_name)) ? "MV " + _id : _name;
            this.MVCanInterpolate = _can_interpolate;
            this.MVDisplayVector = _disp_vect;

            this.MVUnitX = "-";
            this.MVUnitY = "-";
            this.MVUnitZ = "-";
        }

        #endregion

        #region .CTOR for COPYING

        protected MultiValue(MultiValue _original)
        {
            if (_original == null)
            {
                this.MVID = (++MultiValue.NR_MULTI_VALUES);
                this.MVType = MultiValueType.SINGLE;
                this.MVName = "MV " + this.MVID;
                this.MVCanInterpolate = false;
                this.MVDisplayVector = MultiValPointer.INVALID;

                this.MVUnitX = "-";
                this.MVUnitY = "-";
                this.MVUnitZ = "-";
            }
            else
            {
                this.MVID = (++MultiValue.NR_MULTI_VALUES);
                this.MVType = _original.MVType;
                this.MVName = _original.MVName;
                this.MVCanInterpolate = _original.MVCanInterpolate;
                this.MVDisplayVector = new MultiValPointer(_original.MVDisplayVector);

                this.MVUnitX = _original.MVUnitX;
                this.MVUnitY = _original.MVUnitY;
                this.MVUnitZ = _original.MVUnitZ;
            }
        }

        internal virtual MultiValue Clone()
        {
            return null;
        }

        #endregion

        #region METHODS: Info

        public virtual string GetSymbol()
        {
            return ";";
        }

        #endregion

        #region METHODS: To and From String

        public virtual void AddToExport(ref StringBuilder _sb)
        {
            if (_sb == null) return;
            string tmp = null;

            // common
            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_ID).ToString());
            _sb.AppendLine(this.MVID.ToString());

            _sb.AppendLine(((int)MultiValueSaveCode.MVType).ToString());
            _sb.AppendLine(((int)this.MVType).ToString());

            _sb.AppendLine(((int)MultiValueSaveCode.MVName).ToString());
            _sb.AppendLine(this.MVName);

            _sb.AppendLine(((int)MultiValueSaveCode.MVCanInterpolate).ToString());
            tmp = (this.MVCanInterpolate) ? "1" : "0";
            _sb.AppendLine(tmp);

            // common display
            _sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_NUMDIM).ToString());
            _sb.AppendLine(this.MVDisplayVector.NrDim.ToString());

            _sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_CELL_INDEX_X).ToString());
            tmp = (this.MVDisplayVector.NrDim > 0) ? this.MVDisplayVector.CellIndices[0].ToString() : "0";
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_CELL_INDEX_Y).ToString());
            tmp = (this.MVDisplayVector.NrDim > 1) ? this.MVDisplayVector.CellIndices[1].ToString() : "0";
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_CELL_INDEX_Z).ToString());
            tmp = (this.MVDisplayVector.NrDim > 2) ? this.MVDisplayVector.CellIndices[2].ToString() : "0";
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_CELL_INDEX_W).ToString());
            tmp = (this.MVDisplayVector.NrDim > 3) ? this.MVDisplayVector.CellIndices[3].ToString() : "0";
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_CELL_SIZE_W).ToString());
            tmp = double.IsNaN(this.MVDisplayVector.CellSize.X) ? Parameter.Parameter.NAN : this.MVDisplayVector.CellSize.X.ToString("F8", Parameter.Parameter.NR_FORMATTER);
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_CELL_SIZE_H).ToString());
            tmp = double.IsNaN(this.MVDisplayVector.CellSize.Y) ? Parameter.Parameter.NAN : this.MVDisplayVector.CellSize.Y.ToString("F8", Parameter.Parameter.NR_FORMATTER);
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_POS_IN_CELL_REL_X).ToString());
            tmp = double.IsNaN(this.MVDisplayVector.PosInCell_Relative.X) ? Parameter.Parameter.NAN : this.MVDisplayVector.PosInCell_Relative.X.ToString("F8", Parameter.Parameter.NR_FORMATTER);
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_POS_IN_CELL_REL_Y).ToString());
            tmp = double.IsNaN(this.MVDisplayVector.PosInCell_Relative.Y) ? Parameter.Parameter.NAN : this.MVDisplayVector.PosInCell_Relative.Y.ToString("F8", Parameter.Parameter.NR_FORMATTER);
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_POS_IN_CELL_REL_Z).ToString());
            _sb.AppendLine("0");

            _sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_POS_IN_CELL_ABS_X).ToString());
            tmp = double.IsNaN(this.MVDisplayVector.PosInCell_AbsolutePx.X) ? Parameter.Parameter.NAN : this.MVDisplayVector.PosInCell_AbsolutePx.X.ToString("F8", Parameter.Parameter.NR_FORMATTER);
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_POS_IN_CELL_ABS_Y).ToString());
            tmp = double.IsNaN(this.MVDisplayVector.PosInCell_AbsolutePx.Y) ? Parameter.Parameter.NAN : this.MVDisplayVector.PosInCell_AbsolutePx.Y.ToString("F8", Parameter.Parameter.NR_FORMATTER);
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_POS_IN_CELL_ABS_Z).ToString());
            _sb.AppendLine("0");

            _sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_VALUE).ToString());
            tmp = double.IsNaN(this.MVDisplayVector.Value) ? Parameter.Parameter.NAN : this.MVDisplayVector.Value.ToString("F8", Parameter.Parameter.NR_FORMATTER);
            _sb.AppendLine(tmp);

            // common info
            _sb.AppendLine(((int)MultiValueSaveCode.MVUnitX).ToString());
            _sb.AppendLine(this.MVUnitX);

            _sb.AppendLine(((int)MultiValueSaveCode.MVUnitY).ToString());
            _sb.AppendLine(this.MVUnitY);

            _sb.AppendLine(((int)MultiValueSaveCode.MVUnitZ).ToString());
            _sb.AppendLine(this.MVUnitZ);
        
        }

        #endregion

        #region METHODS: External Pointer

        internal virtual void CreateNewPointer(ref MultiValPointer pointer_prev, double _pX, double _pY, double _pZ, string _pS)
        {    }

        #endregion

    }

    public abstract class MultiValueTable : MultiValue
    {
        #region PROPERTIES: Common Info

        public int NrX { get; protected set; }
        public double MinX { get; protected set; }
        public double MaxX { get; protected set; }        

        public int NrY { get; protected set; }
        public double MinY { get; protected set; }
        public double MaxY { get; protected set; }        

        public int NrZ { get; protected set; } // Tables
        public double MinZ { get; protected set; }
        public double MaxZ { get; protected set; }        

        #endregion

        #region PROPERTIES: Common Value Field

        protected List<double> xs;
        public List<double> Xs { get { return this.xs; } }

        protected List<double> ys;
        public List<double> Ys { get { return this.ys; } }

        protected List<double> zs;
        public List<double> Zs { get { return this.zs; } }

        protected Dictionary<Point3D, double> field; //x => index in Xs, y => index in Ys, z => index Zs
        public Dictionary<Point3D, double> Field { get { return this.field; } }

        #endregion

        #region .CTOR

        internal MultiValueTable(MultiValueType _type, string _name)
            : base(MultiValueTable.CheckType(_type), _name)
        {
            this.NrX = 0;
            this.MinX = double.NaN;
            this.MaxX = double.NaN;            

            this.NrY = 0;
            this.MinY = double.NaN;
            this.MaxY = double.NaN;            

            this.NrZ = 0;
            this.MinZ = double.NaN;
            this.MaxZ = double.NaN;

            this.xs = new List<double>();
            this.ys = new List<double>();
            this.zs = new List<double>();

            this.field = new Dictionary<Point3D, double>();
        }

        private static MultiValueType CheckType(MultiValueType _type)
        {
            if (_type == MultiValueType.FIELD_1D || _type == MultiValueType.FIELD_2D || _type == MultiValueType.FIELD_3D)
                return _type;
            else
                return MultiValueType.FIELD_1D;
        }

        #endregion

        #region .CTOR for PARSING

        internal MultiValueTable(long _id, MultiValueType _type, string _name, bool _can_interpolate, MultiValPointer _disp_vect)
            : base(_id, MultiValueTable.CheckType(_type), _name, _can_interpolate, _disp_vect)
        {
            this.NrX = 0;
            this.MinX = double.NaN;
            this.MaxX = double.NaN;

            this.NrY = 0;
            this.MinY = double.NaN;
            this.MaxY = double.NaN;

            this.NrZ = 0;
            this.MinZ = double.NaN;
            this.MaxZ = double.NaN;

            this.xs = new List<double>();
            this.ys = new List<double>();
            this.zs = new List<double>();

            this.field = new Dictionary<Point3D, double>();
        }

        #endregion

        #region .CTOR for COPYING

        protected MultiValueTable(MultiValueTable _original)
            :base(_original)
        {            
            if (_original == null)
            {
                this.NrX = 0;
                this.MinX = double.NaN;
                this.MaxX = double.NaN;

                this.NrY = 0;
                this.MinY = double.NaN;
                this.MaxY = double.NaN;

                this.NrZ = 0;
                this.MinZ = double.NaN;
                this.MaxZ = double.NaN;

                this.xs = new List<double>();
                this.ys = new List<double>();
                this.zs = new List<double>();

                this.field = new Dictionary<Point3D, double>();
            }
            else
            {
                this.NrX = _original.NrX;
                this.MinX = _original.MinX;
                this.MaxX = _original.MaxX;

                this.NrY = _original.NrY;
                this.MinY = _original.MinY;
                this.MaxY = _original.MaxY;

                this.NrZ = _original.NrZ;
                this.MinZ = _original.MinZ;
                this.MaxZ = _original.MaxZ;

                this.xs = new List<double>(_original.xs);
                this.ys = new List<double>(_original.ys);
                this.zs = new List<double>(_original.zs);

                this.field = new Dictionary<Point3D, double>(_original.field);
            }
        }

        #endregion

        #region METHODS: Info

        public override string GetSymbol()
        {
            return "$";
        }

        #endregion

        #region To String

        public override string ToString()
        {
            string mvt_string = "(" + this.MVID + ") "
                                + this.MVType.ToString() + " ["
                                + this.MVUnitX + ", " + this.MVUnitY + ", " + this.MVUnitZ + "]:["
                                + this.NrX + ", " + this.NrY + ", " + this.NrZ + "]";
            mvt_string += " [" + this.MVDisplayVector.ToString() + "]";
            return mvt_string;
        }

        public override void AddToExport(ref StringBuilder _sb)
        {
            if (_sb == null) return;
            string tmp = null;

            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.VALUE_FIELD);                             // VALUE_FIELD

            _sb.AppendLine(((int)ParamStructCommonSaveCode.CLASS_NAME).ToString());
            _sb.AppendLine(this.GetType().ToString());

            // common, common display, common info   
            base.AddToExport(ref _sb);

            // common TABLE Info
            _sb.AppendLine(((int)MultiValueSaveCode.NrX).ToString());
            _sb.AppendLine(this.NrX.ToString());
            _sb.AppendLine(((int)MultiValueSaveCode.MinX).ToString());
            tmp = double.IsNaN(this.MinX) ? Parameter.Parameter.NAN : this.MinX.ToString("F8", Parameter.Parameter.NR_FORMATTER);                        
            _sb.AppendLine(tmp);
            _sb.AppendLine(((int)MultiValueSaveCode.MaxX).ToString());
            tmp = double.IsNaN(this.MaxX) ? Parameter.Parameter.NAN : this.MaxX.ToString("F8", Parameter.Parameter.NR_FORMATTER);
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)MultiValueSaveCode.NrY).ToString());
            _sb.AppendLine(this.NrY.ToString());
            _sb.AppendLine(((int)MultiValueSaveCode.MinY).ToString());
            tmp = double.IsNaN(this.MinY) ? Parameter.Parameter.NAN : this.MinY.ToString("F8", Parameter.Parameter.NR_FORMATTER);
            _sb.AppendLine(tmp);
            _sb.AppendLine(((int)MultiValueSaveCode.MaxY).ToString());
            tmp = double.IsNaN(this.MaxY) ? Parameter.Parameter.NAN : this.MaxY.ToString("F8", Parameter.Parameter.NR_FORMATTER);
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)MultiValueSaveCode.NrZ).ToString());
            _sb.AppendLine(this.NrZ.ToString());
            _sb.AppendLine(((int)MultiValueSaveCode.MinZ).ToString());
            tmp = double.IsNaN(this.MinZ) ? Parameter.Parameter.NAN : this.MinZ.ToString("F8", Parameter.Parameter.NR_FORMATTER);
            _sb.AppendLine(tmp);
            _sb.AppendLine(((int)MultiValueSaveCode.MaxZ).ToString());
            tmp = double.IsNaN(this.MaxZ) ? Parameter.Parameter.NAN : this.MaxZ.ToString("F8", Parameter.Parameter.NR_FORMATTER);
            _sb.AppendLine(tmp);

            // common TABLE VALUE Info
            _sb.AppendLine(((int)MultiValueSaveCode.XS).ToString());
            _sb.AppendLine(this.Xs.Count.ToString());
            for (int i = 0; i < this.Xs.Count; i++)
            {
                _sb.AppendLine(((int)ParamStructCommonSaveCode.X_VALUE).ToString());
                tmp = double.IsNaN(this.Xs[i]) ? Parameter.Parameter.NAN : this.Xs[i].ToString("F8", Parameter.Parameter.NR_FORMATTER);
                _sb.AppendLine(tmp);
            }

            _sb.AppendLine(((int)MultiValueSaveCode.YS).ToString());
            _sb.AppendLine(this.Ys.Count.ToString());
            for (int i = 0; i < this.Ys.Count; i++)
            {
                _sb.AppendLine(((int)ParamStructCommonSaveCode.X_VALUE).ToString());
                tmp = double.IsNaN(this.Ys[i]) ? Parameter.Parameter.NAN : this.Ys[i].ToString("F8", Parameter.Parameter.NR_FORMATTER);
                _sb.AppendLine(tmp);
            }

            _sb.AppendLine(((int)MultiValueSaveCode.ZS).ToString());
            _sb.AppendLine(this.Zs.Count.ToString());
            for (int i = 0; i < this.Zs.Count; i++)
            {
                _sb.AppendLine(((int)ParamStructCommonSaveCode.X_VALUE).ToString());
                tmp = double.IsNaN(this.Zs[i]) ? Parameter.Parameter.NAN : this.Zs[i].ToString("F8", Parameter.Parameter.NR_FORMATTER);
                _sb.AppendLine(tmp);
            }

            _sb.AppendLine(((int)MultiValueSaveCode.FIELD).ToString());
            _sb.AppendLine(this.Field.Count.ToString());
            for (int i = 0; i < this.Field.Count; i++)
            {
                Point3D coords = this.Field.ElementAt(i).Key;
                double value = this.Field.ElementAt(i).Value;
                _sb.AppendLine(((int)ParamStructCommonSaveCode.X_VALUE).ToString());
                _sb.AppendLine(coords.X.ToString("F8", Parameter.Parameter.NR_FORMATTER));
                _sb.AppendLine(((int)ParamStructCommonSaveCode.Y_VALUE).ToString());
                _sb.AppendLine(coords.Y.ToString("F8", Parameter.Parameter.NR_FORMATTER));
                _sb.AppendLine(((int)ParamStructCommonSaveCode.Z_VALUE).ToString());
                _sb.AppendLine(coords.Z.ToString("F8", Parameter.Parameter.NR_FORMATTER));
                _sb.AppendLine(((int)ParamStructCommonSaveCode.W_VALUE).ToString());
                _sb.AppendLine(value.ToString("F8", Parameter.Parameter.NR_FORMATTER));
            }
        }

        #endregion

        #region METHODS: External Pointer

        internal override void CreateNewPointer(ref MultiValPointer pointer_prev, double _pX, double _pY, double _pZ, string _pS)
        {
            if (pointer_prev == null) return;
            // we can ignore the _pS

            // determine table index
            int ind_z_closest = 0;
            double minDist = double.MaxValue;
            for (int table = 0; table < this.NrZ; table++ )
            {
                double dist = Math.Abs(_pZ - this.zs[table]);
                if (dist < minDist)
                {
                    minDist = dist;
                    ind_z_closest = table;
                }
            }

            // determine the cell position in the table
            Point rel_pos = new Point();
            int ind_y_closest = 0;
            int ind_y_closest_1 = 0;
            for (int r = 0; r < this.NrY - 1; r++)
            {
                double a, b;
                MultiValue.GetLinearCoords(this.ys[r], this.ys[r + 1], _pY, out a, out b);
                if (r == 0 && a < 0)
                {
                    // smaller than the lower bound of the interval
                    rel_pos.Y = 0;
                    break;
                }
                if (0 <= a && a <= 1 && 0 <= b && b <= 1)
                {
                    ind_y_closest = r;
                    ind_y_closest_1 = r + 1;
                    rel_pos.Y = a;
                    break;
                }
                if ( r == this.NrY - 2 && b < 0)
                {
                    // larger than the upper bound of the interval
                    ind_y_closest = r + 1;
                    ind_y_closest_1 = r + 1;
                    rel_pos.Y = Math.Min(-b, 1);
                }
            }

            int ind_x_closest = 0;
            int ind_x_closest_1 = 0;
            for (int c = 0; c < this.NrX - 1; c++)
            {
                double a, b;
                MultiValue.GetLinearCoords(this.xs[c], this.xs[c + 1], _pX, out a, out b);
                if (c == 0 && a < 0)
                {
                    // smaller than the lower bound of the interval
                    rel_pos.X = 0;
                    break;
                }
                if (0 < a && a < 1 && 0 < b && b < 1)
                {
                    ind_x_closest = c;
                    ind_x_closest_1 = c + 1;
                    rel_pos.X = a;
                    break;
                }
                if (c == this.NrX - 2 && b < 0)
                {
                    // larger than the upper bound of the interval
                    ind_x_closest = c + 1;
                    ind_x_closest_1 = c + 1;
                    rel_pos.X = Math.Min(-b, 1);
                }
            }

            RectangularValue rv = new RectangularValue();
            rv.LeftBottom = this.field.ElementAt(ind_z_closest * this.NrY * this.NrX + ind_y_closest * this.NrX + ind_x_closest).Value;
            rv.LeftTop = this.field.ElementAt(ind_z_closest * this.NrY * this.NrX + ind_y_closest_1 * this.NrX + ind_x_closest).Value;
            rv.RightBottom = this.field.ElementAt(ind_z_closest * this.NrY * this.NrX + ind_y_closest * this.NrX + ind_x_closest_1).Value;
            rv.RightTop = this.field.ElementAt(ind_z_closest * this.NrY * this.NrX + ind_y_closest_1 * this.NrX + ind_x_closest_1).Value;

            MultiValPointer pointer = new MultiValPointer(new List<int> { Math.Max(this.NrY,1) - ind_y_closest - 1, ind_x_closest + 2, ind_z_closest },
                                                          new Point(pointer_prev.CellSize.X, pointer_prev.CellSize.Y),
                                                          rel_pos,
                                                          false, rv, this.MVCanInterpolate);
            pointer_prev = pointer;
        }

        

        #endregion
    }

    public abstract class MultiValueFunction : MultiValue
    {
        #region PROPERTIES: Common Info

        public double MinX { get; protected set; } // over all functions in one Table
        public double MaxX { get; protected set; } // over all functions in one Table
        public double MinY { get; protected set; } // over all functions in one Table
        public double MaxY { get; protected set; } // over all functions in one Table

        public int NrZ { get; protected set; } // Tables
        public double MinZ { get; protected set; }
        public double MaxZ { get; protected set; }     

        #endregion

        #region PROPERTIES: Common Function Field

        protected List<List<Point3D>> graphs; // x-coord, y-coord, z-index(Table) connected left-to-right
        public List<List<Point3D>> Graphs { get { return this.graphs; } }

        protected List<string> graph_names;
        public List<string> Graph_Names { get { return this.graph_names; } }

        protected List<double> zs; // Tables
        public List<double> Zs { get { return this.zs; } }

        #endregion

        #region PROPETIES: Display Specific

        public override string MVUnitsAsString
        {
            get
            {
                return base.MVUnitsAsString + "[ " + this.graphs.Count + " funct]";
            }
        }

        #endregion

        #region .CTOR

        internal MultiValueFunction(MultiValueType _type, string _name)
            : base(MultiValueFunction.CheckType(_type), _name)
        {
            this.MinX = double.NaN;
            this.MaxX = double.NaN;
            this.MinY = double.NaN;
            this.MaxY = double.NaN;

            this.NrZ = 0;
            this.MinZ = double.NaN;
            this.MaxZ = double.NaN;

            this.zs = new List<double>();

            this.graphs = new List<List<Point3D>>();
            this.graph_names = new List<string>();
        }

        private static MultiValueType CheckType(MultiValueType _type)
        {
            return MultiValueType.FUNCTION_ND;
        }

        #endregion

        #region .CTOR for PARSING

        internal MultiValueFunction(long _id, MultiValueType _type, string _name, bool _can_interpolate, MultiValPointer _disp_vect)
            : base(_id, MultiValueFunction.CheckType(_type), _name, _can_interpolate, _disp_vect)
        {
            this.MinX = double.NaN;
            this.MaxX = double.NaN;
            this.MinY = double.NaN;
            this.MaxY = double.NaN;

            this.NrZ = 0;
            this.MinZ = double.NaN;
            this.MaxZ = double.NaN;

            this.zs = new List<double>();
            this.graphs = new List<List<Point3D>>();
            this.graph_names = new List<string>();
        }

        #endregion

        #region .CTOR for COPYING

        protected MultiValueFunction(MultiValueFunction _original)
            :base(_original)
        {            
            if (_original == null)
            {
                this.MinX = double.NaN;
                this.MaxX = double.NaN;
                this.MinY = double.NaN;
                this.MaxY = double.NaN;

                this.NrZ = 0;
                this.MinZ = double.NaN;
                this.MaxZ = double.NaN;

                this.zs = new List<double>();
                this.graphs = new List<List<Point3D>>();
                this.graph_names = new List<string>();
            }
            else
            {
                this.MinX = _original.MinX;
                this.MaxX = _original.MaxX;
                this.MinY = _original.MinY;
                this.MaxY = _original.MaxY;

                this.NrZ = _original.NrZ;
                this.MinZ = _original.MinZ;
                this.MaxZ = _original.MaxZ;

                this.zs = new List<double>(_original.zs);
                this.graphs = new List<List<Point3D>>(_original.graphs);
                this.graph_names = new List<string>(_original.graph_names);
            }
        }

        #endregion

        #region METHODS: Info

        public override string GetSymbol()
        {
            return "#";
        }

        #endregion

        #region To String

        public override string ToString()
        {
            string mvf_string = "(" + this.MVID + ") "
                                + this.MVType.ToString() + " ["
                                + this.MVUnitX + ", " + this.MVUnitY + ", " + this.MVUnitZ + "]: ["
                                + this.graphs.Count + " funct]";
            mvf_string += " [" + this.MVDisplayVector.ToString() + "]";
            return mvf_string;
        }

        public override void AddToExport(ref StringBuilder _sb)
        {
            if (_sb == null) return;
            string tmp = null;

            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.FUNCTION_FIELD);                          // FUNCTION_FIELD

            _sb.AppendLine(((int)ParamStructCommonSaveCode.CLASS_NAME).ToString());
            _sb.AppendLine(this.GetType().ToString());

            // common, common display, common info   
            base.AddToExport(ref _sb);

            // common FUNCTION Info
            _sb.AppendLine(((int)MultiValueSaveCode.MinX).ToString());
            tmp = double.IsNaN(this.MinX) ? Parameter.Parameter.NAN : this.MinX.ToString("F8", Parameter.Parameter.NR_FORMATTER);
            _sb.AppendLine(tmp);
            _sb.AppendLine(((int)MultiValueSaveCode.MaxX).ToString());
            tmp = double.IsNaN(this.MaxX) ? Parameter.Parameter.NAN : this.MaxX.ToString("F8", Parameter.Parameter.NR_FORMATTER);
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)MultiValueSaveCode.MinY).ToString());
            tmp = double.IsNaN(this.MinY) ? Parameter.Parameter.NAN : this.MinY.ToString("F8", Parameter.Parameter.NR_FORMATTER);
            _sb.AppendLine(tmp);
            _sb.AppendLine(((int)MultiValueSaveCode.MaxY).ToString());
            tmp = double.IsNaN(this.MaxY) ? Parameter.Parameter.NAN : this.MaxY.ToString("F8", Parameter.Parameter.NR_FORMATTER);
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)MultiValueSaveCode.NrZ).ToString());
            _sb.AppendLine(this.NrZ.ToString());
            _sb.AppendLine(((int)MultiValueSaveCode.MinZ).ToString());
            tmp = double.IsNaN(this.MinZ) ? Parameter.Parameter.NAN : this.MinZ.ToString("F8", Parameter.Parameter.NR_FORMATTER);
            _sb.AppendLine(tmp);
            _sb.AppendLine(((int)MultiValueSaveCode.MaxZ).ToString());
            tmp = double.IsNaN(this.MaxZ) ? Parameter.Parameter.NAN : this.MaxZ.ToString("F8", Parameter.Parameter.NR_FORMATTER);
            _sb.AppendLine(tmp);

            // common FUNCTION VALUE Info
            _sb.AppendLine(((int)MultiValueSaveCode.ZS).ToString());
            _sb.AppendLine(this.Zs.Count.ToString());
            for (int i = 0; i < this.Zs.Count; i++)
            {
                _sb.AppendLine(((int)ParamStructCommonSaveCode.X_VALUE).ToString());
                tmp = double.IsNaN(this.Zs[i]) ? Parameter.Parameter.NAN : this.Zs[i].ToString("F8", Parameter.Parameter.NR_FORMATTER);
                _sb.AppendLine(tmp);
            }

            _sb.AppendLine(((int)MultiValueSaveCode.FIELD).ToString());
            _sb.AppendLine(this.Graphs.Count.ToString());
            for (int gi = 0; gi < this.Graphs.Count; gi++)
            {
                List<Point3D> function = this.graphs[gi];
                if (function.Count < 1) continue;

                for (int i = 0; i < function.Count; i++)
                {
                    _sb.AppendLine(((int)ParamStructCommonSaveCode.X_VALUE).ToString());
                    _sb.AppendLine(function[i].X.ToString("F8", Parameter.Parameter.NR_FORMATTER));

                    _sb.AppendLine(((int)ParamStructCommonSaveCode.Y_VALUE).ToString());
                    _sb.AppendLine(function[i].Y.ToString("F8", Parameter.Parameter.NR_FORMATTER));

                    _sb.AppendLine(((int)ParamStructCommonSaveCode.Z_VALUE).ToString());
                    _sb.AppendLine(function[i].Z.ToString("F8", Parameter.Parameter.NR_FORMATTER)); // index of the table the graph belongs to (z-axis)

                    _sb.AppendLine(((int)ParamStructCommonSaveCode.W_VALUE).ToString());
                    if (i < function.Count - 1)
                        _sb.AppendLine(ParamStructTypes.NOT_END_OF_LIST.ToString());
                    else
                        _sb.AppendLine(ParamStructTypes.END_OF_LIST.ToString());
                }
            }
            _sb.AppendLine(((int)MultiValueSaveCode.ROW_NAMES).ToString());
            _sb.AppendLine(this.Graphs.Count.ToString());
            for(int gi = 0; gi < this.Graph_Names.Count; gi++)
            {
                string fct_name = this.graph_names[gi];
                _sb.AppendLine(((int)ParamStructCommonSaveCode.STRING_VALUE).ToString());
                _sb.AppendLine(fct_name);
            }
        }


        #endregion

        #region METHODS: External Pointer

        internal override void CreateNewPointer(ref MultiValPointer pointer_prev, double _pX, double _pY, double _pZ, string _pS)
        {
            if (pointer_prev == null) return;
            Point upper_left_prev = new Point(0, 0);
            Point offset_prev = new Point(0, 0);
            Point scale_prev = new Point(0, 0);
            this.GetScaleNOffset(pointer_prev, out upper_left_prev, out offset_prev, out scale_prev);
            if (scale_prev.X == 0 || scale_prev.Y == 0) return;

            // table index and y index are ignored
            // the inportant pointer are: FUNCTION NAME and X VALUE

            // determine graph (by TypeName)
            int index_graph = 0;
            if (!string.IsNullOrEmpty(_pS))
            {
                index_graph = this.graph_names.FindIndex(x => x == _pS);
                if (index_graph == -1)
                    index_graph = 0;
            }

            int nr_graphs_on_prev_tables = 0;
            for (int i = 0; i < this.graphs.Count; i++)
            {
                if (this.graphs[i] != null && this.graphs[i].Count > 0)
                {
                    if (this.graphs[i][0].Z < pointer_prev.CellIndices[2])
                        nr_graphs_on_prev_tables++;
                }
                else
                {
                    nr_graphs_on_prev_tables++;
                }
            }
            int index_graph_in_table = index_graph - nr_graphs_on_prev_tables;

            // determine the graph and value            
            Point pos = new Point();
            int ind_x_closest = 0;
            int ind_x_closest_1 = 1;

            List<Point3D> graph = this.graphs[index_graph];
            for (int g = 0; g < graph.Count - 1; g++)
            {
                double a, b;
                MultiValue.GetLinearCoords(graph[g].X, graph[g + 1].X, _pX, out a, out b);
                if (g == 0 && a < 0)
                {
                    // smaller than the lower bound of the interval
                    ind_x_closest = 0;
                    ind_x_closest_1 = 1;
                    pos.X = graph[g].X;
                    pos.Y = graph[g].Y;
                    break;
                }
                if (0 <= a && a <= 1 && 0 <= b && b <= 1)
                {
                    ind_x_closest = g;
                    ind_x_closest_1 = g + 1;
                    pos.X = b * graph[g].X + a * graph[g + 1].X;
                    pos.Y = b * graph[g].Y + a * graph[g + 1].Y;
                    break;
                }
                if (g == graph.Count - 2 && b < 0)
                {
                    // larger than the upper bound of the interval
                    ind_x_closest = g;
                    ind_x_closest_1 = g + 1;
                    pos.X = graph[g + 1].X;
                    pos.Y = graph[g + 1].Y;
                }
            }

            Point3D p1 = graph[ind_x_closest];
            Point3D p2 = graph[ind_x_closest_1];

            // construct the cell:
            double x_left = Math.Min(p1.X, p2.X);
            double x_right = Math.Max(p1.X, p2.X);
            double y_top = Math.Max(p1.Y, p2.Y);
            double y_bottom = Math.Min(p1.Y, p2.Y);

            // get the relative position in the cell
            double y_left = 0;
            double y_right = 0;
            if (x_left == p1.X)
            {
                y_left = p1.Y;
                y_right = p2.Y;
            }
            else
            {
                y_left = p2.Y;
                y_right = p1.Y;
            }

            Point pos_rel = new Point(0, 0);
            pos_rel.X = (Math.Abs(p1.X - p2.X) < ParameterStructure.Parameter.Calculation.MIN_DOUBLE_VAL) ? 1 : Math.Abs(x_left - pos.X) / Math.Abs(p1.X - p2.X);
            pos_rel.Y = (Math.Abs(p1.Y - p2.Y) < ParameterStructure.Parameter.Calculation.MIN_DOUBLE_VAL) ? 1 : Math.Abs(y_bottom - pos.Y) / Math.Abs(p1.Y - p2.Y);
            if (pos_rel.X < 0) pos_rel.X = 0;
            if (pos_rel.X > 1) pos_rel.X = 1;
            if (pos_rel.Y < 0) pos_rel.Y = 0;
            if (pos_rel.Y > 1) pos_rel.Y = 1;

            // calculate values 
            RectangularValue rv = new RectangularValue();
            rv.LeftTop = y_left;
            rv.LeftBottom = y_left;
            rv.RightTop = y_right;
            rv.RightBottom = y_right;

            // calculate new offset
            Point offset_new = new Point(0, 0);
            offset_new.X = offset_prev.X + (x_left - upper_left_prev.X) * scale_prev.X;
            offset_new.Y = offset_prev.Y - (y_top - upper_left_prev.Y) * scale_prev.Y;

            // adapt the pointer
            MultiValPointer pointer = new MultiValPointer(new List<int> { index_graph_in_table, ind_x_closest, (int)p1.Z },
                                                          new Point(Math.Abs(p1.X - p2.X) * scale_prev.X, Math.Abs(p1.Y - p2.Y) * scale_prev.Y),
                                                          pos_rel,
                                                          false, rv, this.MVCanInterpolate,
                                                          offset_new.X, offset_new.Y);
            pointer_prev = pointer;

        }

        protected void GetScaleNOffset(MultiValPointer _pointer, out Point cell_upper_left, out Point offset, out Point scale)
        {
            cell_upper_left = new Point(0, 0);
            scale = new Point(1, 1);
            offset = new Point(0, 0);
            if (_pointer == null) return;
            if (_pointer == MultiValPointer.INVALID) return;
            if (double.IsNaN(_pointer.Value)) return;

            offset.X = _pointer.PosInCell_AbsolutePx.X - _pointer.CellSize.X * _pointer.PosInCell_Relative.X;
            offset.Y = _pointer.PosInCell_AbsolutePx.Y - _pointer.CellSize.Y * (1.0 - _pointer.PosInCell_Relative.Y);

            // find the actual line segment where the pointer lies
            Point3D p1, p2;
            int ind_table = _pointer.CellIndices[2];
            int ind_graph_in_table = _pointer.CellIndices[0];
            int ind_line_segm = _pointer.CellIndices[1];
            int nr_graphs_on_prev_tables = 0;
            for (int i = 0; i < this.graphs.Count; i++)
            {
                if (this.graphs[i] != null && this.graphs[i].Count > 0)
                {
                    if (this.graphs[i][0].Z < ind_table)
                        nr_graphs_on_prev_tables++;
                }
                else
                {
                    nr_graphs_on_prev_tables++;
                }
            }
            int ind_graph = ind_graph_in_table + nr_graphs_on_prev_tables;

            if (-1 < ind_graph && ind_graph < this.graphs.Count)
            {
                
                List<Point3D> graph = this.graphs[ind_graph];
                if (-1 < ind_line_segm && ind_line_segm < graph.Count - 1)
                {
                    p1 = graph[ind_line_segm];
                    p2 = graph[ind_line_segm + 1];
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }

            // get the actual position
            //cell_upper_left.X = p1.X * _pointer.PosInCell_Relative.X + p2.X * (1 - _pointer.PosInCell_Relative.X);
            //cell_upper_left.Y = p1.Y * _pointer.PosInCell_Relative.Y + p2.Y * (1 - _pointer.PosInCell_Relative.Y);
            //cell_upper_left.X = p1.X * (1 - _pointer.PosInCell_Relative.X) + p2.X * _pointer.PosInCell_Relative.X;
            //cell_upper_left.Y = p1.Y * (1 - _pointer.PosInCell_Relative.Y) + p2.Y * _pointer.PosInCell_Relative.Y;
            cell_upper_left.X = Math.Min(p1.X, p2.X);
            cell_upper_left.Y = Math.Max(p1.Y, p2.Y);

            // get the scaling factors
            Point cell_size_actual = new Point(Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));
            scale.X = (cell_size_actual.X < Calculation.MIN_DOUBLE_VAL) ? _pointer.CellSize.X : _pointer.CellSize.X / cell_size_actual.X;
            scale.Y = (cell_size_actual.Y < Calculation.MIN_DOUBLE_VAL) ? _pointer.CellSize.Y : _pointer.CellSize.Y / cell_size_actual.Y;
        }

        #endregion
    }

    public class MultiValueBigTable : MultiValue
    {
        #region PROPERTIES: Info

        private List<string> names;
        private List<string> units;
        public List<string> Names { get { return this.names; } }
        public List<string> Units { get { return this.units; } }

        #endregion

        #region PROPERTIES: Values

        private List<List<double>> values;
        public List<List<double>> Values { get { return this.values; } }

        private List<string> row_names;
        public List<string> RowNames { get { return this.row_names; } }

        #endregion

        #region PROPERTIES : Dsiplay Specific

        public override string MVUnitsAsString
        {
            get
            {
                string t_string = string.Empty;
                for (int i = 0; i < this.units.Count && i < 3; i++)
                {
                    if (i < this.units.Count - 1 && i < 2)
                        t_string += this.units[i] + ", ";
                    else
                        t_string += (this.units.Count <= 3) ? this.units[i] + "]" : this.units[i] + "...]";
                }
                t_string += " : [" + this.values.Count + " rows]";
                return t_string;
            }
        }

        #endregion

        #region .CTOR

        internal MultiValueBigTable(string _name, List<string> _names, List<string> _units, List<List<double>> _values)
            :base(MultiValueType.TABLE, _name)
        {
            this.names = new List<string>();
            this.units = new List<string>();
            this.values = new List<List<double>>();
            this.row_names = null;

            if (_names == null || _units == null || _values == null || _values.Count < 1) return;

            int nrN = _names.Count;
            int nrU = _units.Count;
            int nrV = _values[0].Count;
            if (nrN != nrU || nrN != nrV) return;

            this.names = new List<string>(_names);
            this.units = new List<string>(_units);
            this.values = new List<List<double>>(_values);

            this.MVUnitX = (this.units.Count > 0) ? this.units[0] : "-";
            this.MVUnitY = (this.units.Count > 1) ? this.units[1] : "-";
            this.MVUnitZ = (this.units.Count > 2) ? this.units[2] : "-";
        }

        internal MultiValueBigTable(string _name, List<string> _names, List<string> _units, List<List<double>> _values, List<string> _row_names)
            : base(MultiValueType.TABLE, _name)
        {
            this.names = new List<string>();
            this.units = new List<string>();
            this.values = new List<List<double>>();
            this.row_names = new List<string>();

            if (_names == null || _units == null || _values == null || _values.Count < 1 || _row_names == null) return;

            int nrN = _names.Count;
            int nrU = _units.Count;
            int nrV = _values[0].Count;
            int nrRows = _values.Count;
            int nrRN = _row_names.Count;

            if (nrN != nrU || nrN != nrV + 1 || nrRows != nrRN) return;

            this.names = new List<string>(_names);
            this.units = new List<string>(_units);
            this.values = new List<List<double>>(_values);
            this.row_names = new List<string>(_row_names);

            this.MVUnitX = (this.units.Count > 0) ? this.units[0] : "-";
            this.MVUnitY = (this.units.Count > 1) ? this.units[1] : "-";
            this.MVUnitZ = (this.units.Count > 2) ? this.units[2] : "-";
        }

        #endregion

        #region .CTOR for Parsing

        internal MultiValueBigTable(long _id, string _name, MultiValPointer _disp_vect,
                                    string _unit_x, string _unit_y, string _unit_z,
                                    List<string> _names, List<string> _units, 
                                    List<List<double>> _values, List<string> _row_names)
            :base(_id, MultiValueType.TABLE, _name, false, _disp_vect)
        {
            this.MVUnitX = _unit_x;
            this.MVUnitY = _unit_y;
            this.MVUnitZ = _unit_z;

            this.names = new List<string>(_names);
            this.units = new List<string>(_units);
            this.values = new List<List<double>>(_values);
            if (_row_names != null)
                this.row_names = new List<string>(_row_names);
        }

        #endregion

        #region METHODS: Info

        public override string GetSymbol()
        {
            return "%";
        }

        #endregion

        #region To String

        public override string ToString()
        {
            string t_string = "(" + this.MVID + ") "
                                + this.MVType.ToString() + " [";
            for (int i = 0; i < this.units.Count; i++ )
            {
                if (i < this.units.Count - 1)
                    t_string += this.units[i] + ", ";
                else
                    t_string += this.units[i] + "]";
            }
            t_string += ": [" + this.values.Count + " rows]";
            t_string += " [" + this.MVDisplayVector.ToString() + "]";

            return t_string;
        }

        public override void AddToExport(ref StringBuilder _sb)
        {
            if (_sb == null) return;
            
            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.BIG_TABLE);                               // BIG_TABLE

            _sb.AppendLine(((int)ParamStructCommonSaveCode.CLASS_NAME).ToString());
            _sb.AppendLine(this.GetType().ToString());

            // common, common display, common info   
            base.AddToExport(ref _sb);

            // names
            _sb.AppendLine(((int)MultiValueSaveCode.XS).ToString());
            _sb.AppendLine(this.names.Count.ToString());
            for (int i = 0; i < this.names.Count; i++)
            {
                _sb.AppendLine(((int)ParamStructCommonSaveCode.X_VALUE).ToString());
                _sb.AppendLine(this.names[i]);
            }

            // units
            _sb.AppendLine(((int)MultiValueSaveCode.YS).ToString());
            _sb.AppendLine(this.units.Count.ToString());
            for (int i = 0; i < this.units.Count; i++)
            {
                _sb.AppendLine(((int)ParamStructCommonSaveCode.X_VALUE).ToString());
                _sb.AppendLine(this.units[i]);
            }

            // values
            _sb.AppendLine(((int)MultiValueSaveCode.FIELD).ToString());
            _sb.AppendLine(this.Values.Count.ToString());
            _sb.AppendLine(((int)ParamStructCommonSaveCode.NUMBER_OF).ToString());
            _sb.AppendLine(this.Values[0].Count.ToString());
            for (int bi = 0; bi < this.Values.Count; bi++)
            {
                List<double> row = this.Values[bi];
                if (row.Count < 1) continue;

                if (row.Count >= 1)
                {
                    _sb.AppendLine(((int)ParamStructCommonSaveCode.X_VALUE).ToString());
                    _sb.AppendLine(row[0].ToString("F8", Parameter.Parameter.NR_FORMATTER));
                }
                if (row.Count >= 2)
                {
                    _sb.AppendLine(((int)ParamStructCommonSaveCode.Y_VALUE).ToString());
                    _sb.AppendLine(row[1].ToString("F8", Parameter.Parameter.NR_FORMATTER));
                }
                if (row.Count >= 3)
                {
                    _sb.AppendLine(((int)ParamStructCommonSaveCode.Z_VALUE).ToString());
                    _sb.AppendLine(row[2].ToString("F8", Parameter.Parameter.NR_FORMATTER));
                }
                if (row.Count >= 4)
                {
                    _sb.AppendLine(((int)ParamStructCommonSaveCode.W_VALUE).ToString());
                    _sb.AppendLine(row[3].ToString("F8", Parameter.Parameter.NR_FORMATTER));
                }
                if (row.Count >= 5)
                {
                    _sb.AppendLine(((int)ParamStructCommonSaveCode.V5_VALUE).ToString());
                    _sb.AppendLine(row[4].ToString("F8", Parameter.Parameter.NR_FORMATTER));
                }
                if (row.Count >= 6)
                {
                    _sb.AppendLine(((int)ParamStructCommonSaveCode.V6_VALUE).ToString());
                    _sb.AppendLine(row[5].ToString("F8", Parameter.Parameter.NR_FORMATTER));
                }
                if (row.Count >= 7)
                {
                    _sb.AppendLine(((int)ParamStructCommonSaveCode.V7_VALUE).ToString());
                    _sb.AppendLine(row[6].ToString("F8", Parameter.Parameter.NR_FORMATTER));
                }
                if (row.Count >= 8)
                {
                    _sb.AppendLine(((int)ParamStructCommonSaveCode.V8_VALUE).ToString());
                    _sb.AppendLine(row[7].ToString("F8", Parameter.Parameter.NR_FORMATTER));
                }
                if (row.Count >= 9)
                {
                    _sb.AppendLine(((int)ParamStructCommonSaveCode.V9_VALUE).ToString());
                    _sb.AppendLine(row[8].ToString("F8", Parameter.Parameter.NR_FORMATTER));
                }
                if (row.Count >= 10)
                {
                    _sb.AppendLine(((int)ParamStructCommonSaveCode.V10_VALUE).ToString());
                    _sb.AppendLine(row[9].ToString("F8", Parameter.Parameter.NR_FORMATTER));
                }
            }

            // row names
            if (this.row_names != null)
            {
                _sb.AppendLine(((int)MultiValueSaveCode.ROW_NAMES).ToString());
                _sb.AppendLine(this.row_names.Count.ToString());
                foreach(string name in this.row_names)
                {
                    _sb.AppendLine(((int)ParamStructCommonSaveCode.STRING_VALUE).ToString());
                    _sb.AppendLine(name);
                }
            }
            else
            {
                _sb.AppendLine(((int)MultiValueSaveCode.ROW_NAMES).ToString());
                _sb.AppendLine("0");
            }
        }

        #endregion

        #region METHODS: External Pointer

        internal override void CreateNewPointer(ref MultiValPointer pointer_prev, double _pX, double _pY, double _pZ, string _pS)
        {
            // ignore _pZ and _pS
            if (pointer_prev == null) return;

            // get the correct cell
            int index_col = 0;
            int index_row = 0;
            if (this.row_names == null)
            {
                if (-1 < _pY-1 && _pY-1 < this.values.Count)
                    index_row = (int)_pY - 1;
            }
            else
            {
                index_row = this.row_names.FindIndex(x => x == _pS);
                if (index_row == -1)
                    index_row = 0;
            }
            if (this.values.Count > 0 && this.values[0] != null && this.values[0].Count > 0 &&
                -1 < _pX-1 && _pX-1 < this.values[0].Count)
            {
                index_col = (int)_pX - 1;
            }

            double value = this.values[index_row][index_col];
            RectangularValue rv = new RectangularValue();
            rv.LeftTop = value;
            rv.LeftBottom = value;
            rv.RightTop = value;
            rv.RightBottom = value;

            MultiValPointer pointer = new MultiValPointer(new List<int> { index_row, (int)_pX, 0 }, new Point(1, 1), new Point(0, 0), true, rv, false);
            pointer_prev = pointer;
        }

        #endregion
    }
}
