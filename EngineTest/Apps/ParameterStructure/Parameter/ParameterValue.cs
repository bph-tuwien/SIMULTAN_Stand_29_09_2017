// not in use (here for reference purposes)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows;

namespace ParameterStructure.Parameter
{
    #region ENUMS

    public enum PValueTableSaveCode
    {
        UX = 1,
        UY = 2,
        UF = 3,
        ISVALID = 4,
        CANINTERPOLATE = 5,
        X = 10,
        Y = 20,
        F = 30
    }

    public enum ParameterValueDim { DIM_0, DIM_1, DIM_2 }

    #endregion

    public abstract class PValue : IEquatable<PValue>, IComparable<PValue>, IComparable
    {
        #region INSTANCE

        public ParameterValueDim Dim { get; protected set; }
        public double X { get; set; }
        public double Y { get; set; }

        protected PValue(double _x, double _y)
        {
            this.Dim = ParameterValueDim.DIM_0;
            this.X = _x;
            this.Y = _y;
        }

        public override string ToString()
        {
            return "( " + this.X.ToString("F4", PValue.FORMATTER) + ", " + this.Y.ToString("F4", PValue.FORMATTER) + " )";
        }

        #endregion

        #region IEquatable

        public bool Equals(PValue _pv)
        {
            if (_pv == null) return false;

            bool isEqual = true;
            isEqual &= (this.Dim == _pv.Dim);
            isEqual &= PValue.NearlyEqual(this.X, _pv.X, Calculation.MIN_DOUBLE_VAL);
            isEqual &= PValue.NearlyEqual(this.Y, _pv.Y, Calculation.MIN_DOUBLE_VAL);

            return isEqual;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            PValue pv = obj as PValue;
            if (pv == null)
                return false;
            else
                return this.Equals(pv);
        }

        public override int GetHashCode()
        {
            return PValue.ShiftAndWrap(this.X.GetHashCode(), 8) ^ this.Y.GetHashCode();
        }

        #endregion

        #region IComparable<T>, IComparable

        public int CompareTo(PValue _other)
        {
            // If other is not a valid object reference, this instance is greater.
            if (_other == null) return 1;

            double diffX = this.X - _other.X;
            double diffY = this.Y - _other.Y;

            if (PValue.NearlyEqual(this.X, _other.X, Calculation.MIN_DOUBLE_VAL))
            {
                if (PValue.NearlyEqual(this.Y, _other.Y, Calculation.MIN_DOUBLE_VAL))
                    return 0;
                else if (diffY > 0)
                    return 1;
                else
                    return -1;
            }
            else
            {
                if (diffX > 0)
                    return 1;
                else
                    return -1;
            }
        }

        int IComparable.CompareTo(object obj)
        {
            if (obj == null) return 1;

            PValue pv = obj as PValue;
            if (pv != null)
                return this.CompareTo(pv);
            else
                throw new ArgumentException("Object is not a PValue");
        }

        #endregion

        #region STATIC: general

        protected static readonly string CLASS_NAME = "PVALUE";

        #endregion

        #region STATIC: Operators

        public static bool operator ==(PValue _pv1, PValue _pv2)
        {
            if (((object)_pv1) == null || ((object)_pv2) == null)
                return Object.Equals(_pv1, _pv2);

            return _pv1.Equals(_pv2);
        }

        public static bool operator !=(PValue _pv1, PValue _pv2)
        {
            if (((object)_pv1) == null || ((object)_pv2) == null)
                return !Object.Equals(_pv1, _pv2);

            return !(_pv1.Equals(_pv2));
        }

        public static bool operator >(PValue _pv1, PValue _pv2)
        {
            if (_pv1 == null) return false;
            return _pv1.CompareTo(_pv2) == 1;
        }

        public static bool operator <(PValue _pv1, PValue _pv2)
        {
            if (_pv1 == null) return true;
            return _pv1.CompareTo(_pv2) == -1;
        }

        public static bool operator >=(PValue _pv1, PValue _pv2)
        {
            if (_pv1 == null) return false;
            return _pv1.CompareTo(_pv2) >= 0;
        }

        public static bool operator <=(PValue _pv1, PValue _pv2)
        {
            if (_pv1 == null) return true;
            return _pv1.CompareTo(_pv2) <= 0;
        }

        #endregion

        #region STATIC: Comparisons general

        public static bool NearlyEqual(double _a, double _b, double _epsilon = double.Epsilon)
        {
            double absA = Math.Abs(_a);
            double absB = Math.Abs(_b);
            double diff = Math.Abs(_a - _b);

            if (_a == _b)
            {
                // shortcut, handles infinities
                return true;
            }
            else if (_a == 0 || _b == 0 || diff < double.Epsilon)
            {
                // a or b is zero or both are extremely close to it
                // relative error is less meaningful here
                return diff < (_epsilon * double.Epsilon);
            }
            else
            {
                // use relative error
                return diff / Math.Min((absA + absB), double.MaxValue) < _epsilon;
            }
        }

        public static int ShiftAndWrap(double _d, int _positions)
        {
            // convert the double to an int representation
            long val_L = BitConverter.DoubleToInt64Bits(_d);
            int val_I = (int)(val_L >> 32);

            _positions =_positions & 0x1F;

            // Save the existing bit pattern, but interpret it as an unsigned integer.
            uint number = BitConverter.ToUInt32(BitConverter.GetBytes(val_I), 0);
            // Preserve the bits to be discarded.
            uint wrapped = number >> (32 - _positions);
            // Shift and wrap the discarded bits.
            return BitConverter.ToInt32(BitConverter.GetBytes((number << _positions) | wrapped), 0);
        }

        public static int ShiftAndWrap(int _i, int _positions)
        {
            _positions = _positions & 0x1F;

            // Save the existing bit pattern, but interpret it as an unsigned integer.
            uint number = BitConverter.ToUInt32(BitConverter.GetBytes(_i), 0);
            // Preserve the bits to be discarded.
            uint wrapped = number >> (32 - _positions);
            // Shift and wrap the discarded bits.
            return BitConverter.ToInt32(BitConverter.GetBytes((number << _positions) | wrapped), 0);
        }

        #endregion

        #region STATIC: Comparisons specific

        public static System.Globalization.NumberFormatInfo FORMATTER = new System.Globalization.NumberFormatInfo();

        // ----------------------------- POSITION CHECKS ------------------------------------ //

        public static bool InGeneralPosition1D(PValue _x1, PValue _x2)
        {
            if (_x1 == null || _x2 == null) return false;

            //double diffX = _x1.X - _x2.X;
            //return (Math.Abs(diffX) >= Calculation.MIN_DOUBLE_VAL);

            return !PValue.NearlyEqual(_x1.X, _x2.X, Calculation.MIN_DOUBLE_VAL);
        }

        public static bool InGeneralPosition2D(PValue _xy1, PValue _xy2)
        {
            if (_xy1 == null || _xy2 == null) return false;

            //double diffX = _xy1.X - _xy2.X;
            //double diffY = _xy1.Y - _xy2.Y;
            //return (Math.Abs(diffX) >= Calculation.MIN_DOUBLE_VAL || Math.Abs(diffY) >= Calculation.MIN_DOUBLE_VAL);

            return (!PValue.NearlyEqual(_xy1.X, _xy2.X, Calculation.MIN_DOUBLE_VAL) ||
                    !PValue.NearlyEqual(_xy1.Y, _xy2.Y, Calculation.MIN_DOUBLE_VAL));
        }

        public static bool InGeneralPosition2D(PValue _xy1, PValue _xy2, PValue _xy3)
        {
            return PValue.InGeneralPosition2D(_xy1, _xy2) &&
                   PValue.InGeneralPosition2D(_xy2, _xy3) &&
                   PValue.InGeneralPosition2D(_xy3, _xy1);
        }

        // ------------------------ INSIDE / OUTSIDE CHECKS --------------------------------- //

        public static bool IsInsideLineSegment(PValue _xA, PValue _xB, PValue _x)
        {
            if (_xA == null || _xB == null || _x == null) return false;
            if (!PValue.InGeneralPosition1D(_xA, _xB)) return false;

            double diff_xA = _x.X - _xA.X;
            double diff_xB = _x.X - _xB.X;

            return Math.Sign(diff_xA) != Math.Sign(diff_xB);
        }

        public static bool IsInsideTriangle(PValue _xyA, PValue _xyB, PValue _xyC, PValue _xy, bool _check_triangle = false)
        {
            if (_xyA == null || _xyB == null || _xyC == null) return false;

            if (_check_triangle)
                if (!PValue.InGeneralPosition2D(_xyA, _xyB, _xyC)) return false;

            double diff_xA = _xy.X - _xyA.X;
            double diff_xB = _xy.X - _xyB.X;
            double diff_xC = _xy.X - _xyC.X;
            bool inside_x = (Math.Sign(diff_xA) != Math.Sign(diff_xB)) || (Math.Sign(diff_xA) != Math.Sign(diff_xC));

            double diff_yA = _xy.Y - _xyA.Y;
            double diff_yB = _xy.Y - _xyB.Y;
            double diff_yC = _xy.Y - _xyC.Y;
            bool inside_y = (Math.Sign(diff_yA) != Math.Sign(diff_yB)) || (Math.Sign(diff_yA) != Math.Sign(diff_yC));

            return inside_x && inside_y;
        }

        // -------------------- RELATIVE COORDINATES EXTRACTION ----------------------------- //
        public static void GetLinearCoords(PValue _xA, PValue _xB, PValue _x, out double a, out double b)
        {
            a = double.NaN;
            b = double.NaN;
            if (!PValue.IsInsideLineSegment(_xA, _xB, _x)) return;

            double diff_xA = _x.X - _xA.X;
            double diffxAB = _xA.X - _xB.X;

            a = Math.Abs(diff_xA) / Math.Abs(diffxAB);
            b = 1 - a;
        }

        public static void GetBarycentricCoords(PValue _xyA, PValue _xyB, PValue _xyC, PValue _xy,
                                                out double a, out double b, out double c)
        {
            a = double.NaN;
            b = double.NaN;
            c = double.NaN;
            if (!PValue.IsInsideTriangle(_xyA, _xyB, _xyC, _xy, true)) return;

            // solving the eq system:
            // _xy.X = a * _xyA.X + b * _xyB.X + (1 - a - b) * _xyC.X
            // _xy.Y = a * _xyA.Y + b * _xyB.Y + (1 - a - b) * _xyC.Y
            double denominator = (_xyB.Y - _xyC.Y) * (_xyA.X - _xyC.X) + (_xyC.X - _xyB.X) * (_xyA.Y - _xyC.Y);
            if (Math.Abs(denominator) < Calculation.MIN_DOUBLE_VAL) return;

            a = ((_xyB.Y - _xyC.Y) * (_xy.X - _xyC.X) + (_xyC.X - _xyB.X) * (_xy.Y - _xyC.Y)) / denominator;
            b = ((_xy.Y - _xyC.Y) * (_xyA.X - _xyC.X) + (_xyC.X - _xy.X) * (_xyA.Y - _xyC.Y)) / denominator;
            c = 1 - a - b;
        }

        #endregion

    }

    #region HELPER CLASSES : IComparer<T>

    internal class PValueComparer : IComparer<PValue>
    {
        public int Compare(PValue _xy1, PValue _xy2)
        {
            if (_xy1 == null && _xy2 == null) return 0;
            if (_xy1 != null && _xy2 == null) return 1;
            if (_xy1 == null && _xy2 != null) return -1;

            double diffX = _xy1.X - _xy2.X;
            double diffY = _xy1.Y - _xy2.Y;

            if (PValue.NearlyEqual(_xy1.X, _xy2.X, Calculation.MIN_DOUBLE_VAL))
            {
                if (PValue.NearlyEqual(_xy1.Y, _xy2.Y, Calculation.MIN_DOUBLE_VAL))
                    return 0;
                else if (diffY > 0)
                    return 1;
                else
                    return -1;
            }
            else
            {
                if (diffX > 0)
                    return 1;
                else
                    return -1;
            }
        }
    }

    internal class PValueDistComparer : IComparer<PValue>
    {
        private PValue pv_ref;
        public PValueDistComparer(PValue _pv_reference)
        {
            this.pv_ref = (_pv_reference == null) ? new PValue0D() : _pv_reference;
        }
        public int Compare(PValue _xy1, PValue _xy2)
        {
            double dist1 = Math.Abs(_xy1.X - this.pv_ref.X) + Math.Abs(_xy1.Y - this.pv_ref.Y);
            double dist2 = Math.Abs(_xy2.X - this.pv_ref.X) + Math.Abs(_xy2.Y - this.pv_ref.Y);

            if (PValue.NearlyEqual(dist1, dist2, Calculation.MIN_DOUBLE_VAL))
                return 0;
            else if (dist1 > dist2)
                return 1;
            else
                return -1;
        }
    }

    #endregion

    #region IMPLEMENTATION: PValue

    public class PValue0D : PValue
    {
        public PValue0D()
            :base(0, 0)
        {
            this.Dim = ParameterValueDim.DIM_0;
        }
    }

    public class PValue1D : PValue
    {
        public PValue1D(double _x)
            :base(_x, 0)
        {
            this.Dim = ParameterValueDim.DIM_1;
        }
    }

    public class PValue2D : PValue
    {
        public PValue2D(double _x, double _y)
            :base(_x, _y)
        {
            this.Dim = ParameterValueDim.DIM_2;
        }
    }

    #endregion

    #region HELPER CLASSES: Display of PValue Table

    public class DisplayVector
    {
        public int RowIndex { get; private set; }
        public int ColIndex { get; private set; }
        public double PosInRow { get; private set; }
        public double PosInCol { get; private set; }
        public double Value { get; private set; }

        public DisplayVector(int _row_index, int _col_index, double _pos_in_row, double _pos_in_col, double _value)
        {
            this.RowIndex = _row_index;
            this.ColIndex = _col_index;
            this.PosInRow = _pos_in_row;
            this.PosInCol = _pos_in_col;
            this.Value = _value;
        }
    }


    #endregion

    #region PValue Usage

    public class PValueTable
    {
        #region STATIC

        protected static readonly string CLASS_NAME = "PVALUETABLE";

        #endregion

        #region PROPERTIES

        private SortedList<PValue, double> table;
        public double this[PValue index]
        {
            get 
            {
                if (!this.IsValid)
                    return double.NaN;

                if (this.table.ContainsKey(index))
                    return this.table[index];
                else
                    return this.RetrieveValue(index);
            }
            set 
            {
                if (this.IsValid && this.table.ContainsKey(index)) 
                    this.table[index] = value; 
            }
        }

        public bool IsValid { get; private set; }
        public bool CanInterpolate { get; private set; }

        public string Unit_X { get; private set; }
        public string Unit_Y { get; private set; }

        public List<double> Steps_X { get; private set; }
        public List<double> Steps_Y { get; private set; }

        // only for display -> do not influence the internal logic
        private Dictionary<Point, double> table_for_display;
        public ReadOnlyDictionary<Point, double> TableForDisplay { get { return new ReadOnlyDictionary<Point,double>( this.table_for_display); } }
        public DisplayVector VectorForDisplay { get; set; }

        #endregion

        #region .CTOR

        public PValueTable(List<double> _Xs, string _unit_X, List<double> _Ys, string _unit_Y, 
                           List<double> _Fxys, bool _can_interpolate)
        {
            this.IsValid = true;
            this.CanInterpolate = _can_interpolate;
            this.Unit_X = (string.IsNullOrEmpty(_unit_X)) ? string.Empty : _unit_X;
            this.Unit_Y = (string.IsNullOrEmpty(_unit_Y)) ? string.Empty : _unit_Y;
            this.table = new SortedList<PValue, double>(new PValueComparer());
            if (_Xs == null || _Ys == null || _Fxys == null)
            {
                this.IsValid = false;
                return;
            }

            int n = _Xs.Count;
            int m = _Ys.Count;
            if (n < 1 || m < 1 || n * m != _Fxys.Count)
            {
                this.IsValid = false;
                return;
            }

            this.Steps_X = _Xs;
            this.Steps_Y = _Ys;
            this.table_for_display = new Dictionary<Point, double>();
            for(int i = 0; i < n; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    PValue q = new PValue2D(_Xs[i], _Ys[j]);
                    if (this.table.ContainsKey(q)) continue;

                    this.table.Add(q, _Fxys[i*m + j]);
                    this.table_for_display.Add(new Point(i, j), _Fxys[i * m + j]);
                }
            }
 
        }

        public PValueTable(List<double> _Xs, string _unit_X, List<double> _Fxs, bool _can_interpolate)
        {
            this.IsValid = true;
            this.CanInterpolate = _can_interpolate;
            this.Unit_X = (string.IsNullOrEmpty(_unit_X)) ? string.Empty : _unit_X;
            this.Unit_Y = string.Empty;
            this.table = new SortedList<PValue, double>(new PValueComparer());
            if (_Xs == null || _Fxs == null)
            {
                this.IsValid = false;
                return;
            }

            int n = _Xs.Count;
            if (n < 1 || n != _Fxs.Count)
            {
                this.IsValid = false;
                return;
            }


            this.Steps_X = _Xs;
            this.Steps_Y = new List<double> { 0.0 };
            this.table_for_display = new Dictionary<Point, double>();
            for (int i = 0; i < n; i++)
            {
                PValue q = new PValue1D(_Xs[i]);
                if (this.table.ContainsKey(q)) continue;

                this.table.Add(q, _Fxs[i]);
                this.table_for_display.Add(new Point(i, 0), _Fxs[i]);
            }

            
        }

        public PValueTable(double _single_value)
        {
            this.IsValid = true;
            this.CanInterpolate = false;
            this.Unit_X = string.Empty;
            this.Unit_Y = string.Empty;
            this.table = new SortedList<PValue, double>(new PValueComparer());
            this.table.Add(new PValue0D(), _single_value);

            this.Steps_X = new List<double>();
            this.Steps_Y = new List<double>();
            this.table_for_display = new Dictionary<Point, double>();
            this.table_for_display.Add(new Point(0, 0), _single_value);
        }

        #endregion

        #region METHODS: Interpolation PRIVATE

        private void FindNearestNeighbors(PValue _pv, out PValue _pvSmaller, out PValue _pvBigger)
        {
            int n = this.table.Count;
            int counter = -1;
            foreach(var entry in this.table)
            {
                counter++;
                if (entry.Key >= _pv)
                    break;
            }

            _pvSmaller = this.table.ElementAt(counter).Key;
            if (counter == 0 || counter == n - 1)
                _pvBigger = this.table.ElementAt(counter).Key;
            else
                _pvBigger = this.table.ElementAt(counter + 1).Key;   
        }


        private double Interpolate1D(PValue _pv)
        {
            if (_pv == null) return double.NaN;

            PValue pv1, pv2;
            this.FindNearestNeighbors(_pv, out pv1, out pv2);

            // check the neighbors
            bool neighb_not_same = PValue.InGeneralPosition1D(pv1, pv2);
            if (neighb_not_same)
            {
                // interpolate
                double f1, f2;
                PValue.GetLinearCoords(pv1, pv2, _pv, out f1, out f2);
                if (this.CanInterpolate)
                    return f1 * this.table[pv1] + f2 * this.table[pv2];
                else
                    return (f1 > f2) ? this.table[pv1] : this.table[pv2];
            }
            else
            {
                // the neighbors are the same -> we are out of bounds
                return this.table[pv1];
            }
        }

        private void FindNearestNeighbors(PValue _pv, out PValue _pvSmaller, out PValue _pvBigger, out PValue _pv3)
        {
            int n = this.table.Count;
            int counter = -1;
            foreach (var entry in this.table)
            {
                counter++;
                if (entry.Key >= _pv)
                    break;
            }

            _pvSmaller = this.table.ElementAt(counter).Key;
            if (counter == 0 || counter == n - 1)
            {
                // A. out of range
                _pvBigger = this.table.ElementAt(counter).Key;
                _pv3 = null;
                return;
            }
            else
                _pvBigger = this.table.ElementAt(counter + 1).Key; 

            // B. find a third point for an interpolation triangle

            // order the table values according to distance from the query value _pv
            SortedList<PValue, double> sorted_acc_to_dist = new SortedList<PValue, double>(new PValueDistComparer(_pv));
            foreach(var entry in this.table)
            {
                if (entry.Key == _pvSmaller || entry.Key == _pvBigger)
                    continue;

                sorted_acc_to_dist.Add(entry.Key, entry.Value);
            }

            // find closest value that builds a triangle around the query value _pv
            foreach(var entry in sorted_acc_to_dist)
            {
                if (PValue.IsInsideTriangle(_pvSmaller, _pvBigger, entry.Key, _pv, true))
                {
                    _pv3 = entry.Key;
                    return;
                }
            }

            // C. no well-defined triangle could be found
            // if none could be found, leave the third point empty
            _pv3 = null;

        }

        private double Interpolate2D(PValue _pv)
        {
            if (_pv == null) return double.NaN;

            PValue pv1, pv2, pv3;
            this.FindNearestNeighbors(_pv, out pv1, out pv2, out pv3);

            // check the neighbors
            if (pv3 == null)
            {
                // 1D interpolation
                return Interpolate1D(_pv);
            }
            else
            {
                // 2D interpolation 
                if (this.CanInterpolate)
                {
                    double f1, f2, f3;
                    PValue.GetBarycentricCoords(pv1, pv2, pv3, _pv, out f1, out f2, out f3);
                    return f1 * this.table[pv1] + f2 * this.table[pv2] + f3 * this.table[pv3];
                }
                else
                {
                    SortedList<PValue, double> sorted_neighb = new SortedList<PValue, double>(new PValueDistComparer(_pv));
                    sorted_neighb.Add(pv1, this.table[pv1]);
                    sorted_neighb.Add(pv2, this.table[pv2]);
                    sorted_neighb.Add(pv3, this.table[pv3]);

                    return sorted_neighb.ElementAt(0).Value;
                }
            }

        }

        private double RetrieveValue(PValue _pv)
        {
            if (_pv == null) return double.NaN;

            if (_pv.Dim == ParameterValueDim.DIM_1)
                return this.Interpolate1D(_pv);
            else if (_pv.Dim == ParameterValueDim.DIM_2)
                return this.Interpolate2D(_pv);
            else
                return this.table.ElementAt(0).Value;
        }

        #endregion

        #region METHODS: To and From String
        public override string ToString()
        {
            string output = "[valid: " + ((this.IsValid) ? "1" : "0") + " interp: " + ((this.CanInterpolate) ? "1" : "0") + "pValue table:\n";
            foreach(var entry in this.table)
            {
                output += entry.Key.ToString() + " -> " + entry.Value.ToString("F4", PValue.FORMATTER) + "\n";
            }
            return output;
        }

        public string ToExportString()
        {
            string export = "0\n";
            export += PValueTable.CLASS_NAME + "\n";
            
            // todo: units

            export += ((int)PValueTableSaveCode.ISVALID).ToString() + "\n";
            export += (this.IsValid) ? "1\n" : "0\n";
            export += ((int)PValueTableSaveCode.CANINTERPOLATE).ToString() + "\n";
            export += (this.CanInterpolate) ? "1\n" : "0\n";

            // save all entries
            foreach(var entry in this.table)
            {
                export += ((int)PValueTableSaveCode.X).ToString() + "\n";
                export += entry.Key.X.ToString("F4", PValue.FORMATTER) + "\n";
                export += ((int)PValueTableSaveCode.Y).ToString() + "\n";
                export += entry.Key.Y.ToString("F4", PValue.FORMATTER) + "\n";
                export += ((int)PValueTableSaveCode.F).ToString() + "\n";
                export += entry.Value.ToString("F4", PValue.FORMATTER) + "\n";
            }

            return export;
        }

        #endregion

    }

    #endregion
}
