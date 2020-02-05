using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ParameterStructure.Values
{
    public struct RectangularValue
    {
        public double LeftBottom;
        public double RightBottom;
        public double RightTop;
        public double LeftTop;
    }
    public class MultiValPointer
    {
        #region STATIC

        private static bool InputValid(List<int> _cell_indices, Point _cell_size, Point _position, bool _pos_is_absolute)
        {
            bool input_valid = true;

            input_valid &= (_cell_indices != null && _cell_size != null && _position != null);
            if (!input_valid) return false;

            input_valid &= (_cell_indices.Count > 1 && (_cell_size.X > 0 || _cell_size.Y > 0));
            if (!input_valid) return false;

            if (_pos_is_absolute)
                input_valid &= (0 <= _position.X && _position.X <= _cell_size.X && 0 <= _position.Y && _position.Y <= _cell_size.Y);
            else
                input_valid &= (0 <= _position.X && _position.X <= 1 && 0 <= _position.Y && _position.Y <= 1);

            return input_valid;
        }

        public static readonly MultiValPointer INVALID;
        static MultiValPointer()
        {
            RectangularValue rv = new RectangularValue()
            {
                LeftBottom = double.NaN,
                RightBottom = double.NaN,
                RightTop = double.NaN,
                LeftTop = double.NaN
            };
            MultiValPointer.INVALID = new MultiValPointer(new List<int>(), new Point(0, 0), new Point(0, 0), true, rv, false);
        }

        #endregion

        #region PROPERTIES

        public int NrDim { get; private set; }

        private List<int> cell_indices;
        private Point cell_size;
        private Point pos_in_cell_relative;
        private Point pos_in_cell_abs_Px;

        public List<int> CellIndices { get { return this.cell_indices; } }
        public Point CellSize { get { return this.cell_size; } }
        public Point PosInCell_Relative { get { return this.pos_in_cell_relative; } } //2D 0,0 is lower left corner; 1,1 is upper right corner
        public Point PosInCell_AbsolutePx { get { return this.pos_in_cell_abs_Px; } } //2D 0,0 is upper left corner; z.B. 30px,15px is lower right corner

        public double Value { get; private set; } 

        #endregion

        #region .CTOR

        public MultiValPointer(List<int> _cell_indices, Point _cell_size, Point _position, bool _pos_is_absolute, 
                                RectangularValue _cell_values, bool _can_interpolate, 
                                double _abs_offset_x = 0, double _abs_offset_y = 0)
        {
            this.NrDim = 0;
            this.cell_indices = new List<int>();
            this.cell_size = new Point(0, 0);
            this.pos_in_cell_relative = new Point(0, 0);
            this.pos_in_cell_abs_Px = new Point(0, 0);
            this.Value = double.NaN;

            if (!MultiValPointer.InputValid(_cell_indices, _cell_size, _position, _pos_is_absolute)) return;

            // calculate position
            this.NrDim = _cell_indices.Count;
            this.cell_indices = _cell_indices;
            this.cell_size = _cell_size;

            if (_pos_is_absolute)
            {
                this.pos_in_cell_abs_Px = new Point(_position.X + _abs_offset_x, _position.Y + _abs_offset_y);
                this.pos_in_cell_relative = new Point(0, 0);
                this.pos_in_cell_relative.X = (_cell_size.X < Parameter.Calculation.MIN_DOUBLE_VAL) ? 1 : _position.X / _cell_size.X;
                this.pos_in_cell_relative.Y = (_cell_size.Y < Parameter.Calculation.MIN_DOUBLE_VAL) ? 1 : (_cell_size.Y - _position.Y) / _cell_size.Y;
            }
            else
            {
                this.pos_in_cell_relative = _position;
                this.pos_in_cell_abs_Px = new Point(_cell_size.X * _position.X + _abs_offset_x, 
                                                    _cell_size.Y * (1 - _position.Y) + _abs_offset_y);
            }  
         
            // calculate value
            double result = double.NaN;
            if (_can_interpolate)
            {
                double resultY_bottom = _cell_values.LeftBottom * (1.0 - this.pos_in_cell_relative.X) + _cell_values.RightBottom * this.pos_in_cell_relative.X;
                double resultY_top = _cell_values.LeftTop * (1.0 - this.pos_in_cell_relative.X) + _cell_values.RightTop * this.pos_in_cell_relative.X;
                result = resultY_bottom * (1.0 - this.pos_in_cell_relative.Y) + resultY_top * this.pos_in_cell_relative.Y;
            }
            else
            {
                bool take_right = (this.pos_in_cell_relative.X > 0.5); 
                bool take_bottom = (this.pos_in_cell_relative.Y < 0.5);
                if (take_right && take_bottom)
                    result = _cell_values.RightBottom;
                else if (take_right && !take_bottom)
                    result = _cell_values.RightTop;
                else if (!take_right && take_bottom)
                    result = _cell_values.LeftBottom;
                else
                    result = _cell_values.LeftTop;
            }
            this.Value = result;
        }

        #endregion

        #region .CTOR for Parsing

        // to be called only by the parser
        internal MultiValPointer(int _nr_dim, List<int> _cell_indices, Point _cell_size, Point _pos_rel, Point _pos_abs, double _value)
        {
            this.NrDim = _nr_dim;
            this.cell_indices = new List<int>();
            this.cell_size = new Point(0, 0);
            this.pos_in_cell_relative = new Point(0, 0);
            this.pos_in_cell_abs_Px = new Point(0, 0);
            this.Value = _value;

            if (_cell_indices == null || _pos_abs == null || _pos_rel == null) return;
            if (this.NrDim != _cell_indices.Count) return;

            this.cell_indices = _cell_indices;
            this.cell_size = _cell_size;
            this.pos_in_cell_relative = _pos_rel;
            this.pos_in_cell_abs_Px = _pos_abs;
        }
        
        #endregion

        #region .CTOR for COPYING

        public MultiValPointer(MultiValPointer _original)
        {
            if (_original == null)
            {
                this.NrDim = 0;
                this.cell_indices = new List<int>();
                this.cell_size = new Point(0, 0);
                this.pos_in_cell_relative = new Point(0, 0);
                this.pos_in_cell_abs_Px = new Point(0, 0);
                this.Value = double.NaN;
            }
            else
            {
                this.NrDim = _original.NrDim;
                this.cell_indices = new List<int>(_original.cell_indices);
                this.cell_size = new Point(_original.cell_size.X, _original.cell_size.Y);
                this.pos_in_cell_relative = new Point(_original.pos_in_cell_relative.X, _original.pos_in_cell_relative.Y);
                this.pos_in_cell_abs_Px = new Point(_original.pos_in_cell_abs_Px.X, _original.pos_in_cell_abs_Px.Y);
                this.Value = _original.Value;
            }
        }

        #endregion

        #region To String

        public override string ToString()
        {
            string output = this.NrDim + " [ ";
            foreach(int index in this.CellIndices)
            {
                output += index.ToString() + " ";
            }
            output += "]: " + this.Value.ToString("F4", Parameter.Parameter.NR_FORMATTER);

            return output;
        } 

        #endregion

        #region METHODS: Comparing Pointers

        public static bool AreEqual(MultiValPointer _p1, MultiValPointer _p2)
        {
            if (_p1 == null && _p2 != null) return false;
            if (_p1 != null && _p2 == null) return false;
            if (_p1 == null && _p2 == null) return true;

            int dim = _p1.CellIndices.Count;
            if (dim != _p2.CellIndices.Count) return false;

            bool positions_equal = (_p1.PosInCell_AbsolutePx.X == _p2.PosInCell_AbsolutePx.X &&
                                    _p1.PosInCell_AbsolutePx.Y == _p2.PosInCell_AbsolutePx.Y);

            bool indices_equal = true;
            for(int i = 0; i < dim; i++)
            {
                indices_equal &= (_p1.CellIndices[i] == _p2.CellIndices[i]);
            }

            return positions_equal && indices_equal;
        }

        #endregion

    }
}
