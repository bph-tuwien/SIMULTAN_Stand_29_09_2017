using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace ParameterStructure.Values
{
    public class SingleValue : MultiValue
    {
        #region .CTOR
        internal SingleValue(double _value, string _name)
            :base(MultiValueType.SINGLE, _name)
        {
            RectangularValue rv = new RectangularValue()
            {
                LeftBottom = _value,
                RightBottom = _value,
                RightTop = _value,
                LeftTop = _value
            };
            this.MVDisplayVector = new MultiValPointer(new List<int> { 0 }, new Point(10, 10), new Point(1, 1), true, rv, false);
        }
        #endregion
    }

    public class MultiValueTable1D : MultiValueTable
    {
        #region CLASS MEMBERS: Field Values

        private SortedList<double, double> x_fx;

        #endregion

        #region .CTOR
        internal MultiValueTable1D(List<double> _Xs, string _unit_x, string _unit_y, string _unit_z, List<double> _Fxs, bool _can_interpolate, string _name)
            :base(MultiValueType.FIELD_1D, _name)
        {
            this.MVCanInterpolate = _can_interpolate;
            this.x_fx = new SortedList<double, double>();
            this.MVUnitX = (string.IsNullOrEmpty(_unit_x)) ? "-" : _unit_x;
            this.MVUnitY = (string.IsNullOrEmpty(_unit_y)) ? "-" : _unit_y;
            this.MVUnitZ = (string.IsNullOrEmpty(_unit_z)) ? "-" : _unit_z;

            if (_Xs != null && _Fxs != null && _Xs.Count > 0 && _Xs.Count == _Fxs.Count)
            {
                this.x_fx = new SortedList<double, double>();
                this.NrX = _Xs.Count;
                for (int i = 0; i < this.NrX; i++)
                {
                    if (this.x_fx.ContainsKey(_Xs[i])) continue;
                    this.x_fx.Add(_Xs[i], _Fxs[i]);                    
                }
                this.NrX = this.x_fx.Count;
                if (this.NrX > 0)
                {
                    for (int i = 0; i < this.NrX; i++ )
                    {
                        this.field.Add(new Point3D(i, 0, 0), this.x_fx.ElementAt(i).Value);
                    }
                    this.xs = this.x_fx.Keys.ToList();
                    this.MinX = this.x_fx.ElementAt(0).Key;
                    this.MaxX = this.x_fx.ElementAt(this.NrX - 1).Key;
                }
            }
            
        }
        #endregion

        #region .CTOR for Parsing

        internal MultiValueTable1D(long _id, string _name, bool _can_interpolate, MultiValPointer _disp_vect,
                                    int _nr_x, double _min_x, double _max_x, string _unit_x, 
                                    List<double> _Xs, string _unit_y, string _unit_z, Dictionary<Point3D, double> _Fxs)
            :base(_id, MultiValueType.FIELD_1D, _name, _can_interpolate, _disp_vect)
        {

            this.NrX = _nr_x;
            this.MinX = _min_x;
            this.MaxX = _max_x;
            this.MVUnitX = _unit_x;

            this.MVUnitY = _unit_y;
            this.MVUnitZ = _unit_z;

            this.xs = _Xs;
            this.field = _Fxs;
        }

        #endregion

        #region .CTOR for COPYING

        protected MultiValueTable1D(MultiValueTable1D _original)
            :base(_original)
        {     }

        internal override MultiValue Clone()
        {
            return new MultiValueTable1D(this);
        }

        #endregion
    }

    public class MultiValueTable2D : MultiValueTable
    {
        #region .CTOR

        // the ordering of _Xs and _Ys is irrelevant
        internal MultiValueTable2D(List<double> _Xs, string _unit_x, List<double> _Ys, string _unit_y, string _unit_z,
                                 List<double> _Fxys, bool _can_interpolate, string _name)
            :base(MultiValueType.FIELD_2D, _name)
        {
            this.MVCanInterpolate = _can_interpolate;
            this.MVUnitX = (string.IsNullOrEmpty(_unit_x)) ? "-" : _unit_x;
            this.MVUnitY = (string.IsNullOrEmpty(_unit_y)) ? "-" : _unit_y;
            this.MVUnitZ = (string.IsNullOrEmpty(_unit_z)) ? "-" : _unit_z;

            if (_Xs != null && _Ys != null && _Fxys != null &&
                _Xs.Count > 0 && _Ys.Count > 0 &&  _Xs.Count * _Ys.Count == _Fxys.Count)
            {
                this.xs = _Xs;
                this.ys = _Ys;
                
                this.NrX = _Xs.Count;                
                this.MinX = this.xs.Min();
                this.MaxX = this.xs.Max();
                this.NrY = _Ys.Count;
                this.MinY = this.ys.Min();
                this.MaxY = this.ys.Max();
                
                for (int r = 0; r < this.NrY; r++)
                {
                    for (int c = 0; c < this.NrX; c++)
                    {
                        this.field.Add(new Point3D(c, r, 0), _Fxys[r * this.NrX + c]);
                    }
                }   
            }
        }

        #endregion

        #region .CTOR for Parsing

        internal MultiValueTable2D(long _id, string _name, bool _can_interpolate, MultiValPointer _disp_vect,
                                    int _nr_x, double _min_x, double _max_x, string _unit_x,
                                    int _nr_y, double _min_y, double _max_y, string _unit_y,
                                    List<double> _Xs, List<double> _Ys, string _unit_z, Dictionary<Point3D, double> _Fxys)
            :base(_id, MultiValueType.FIELD_2D, _name, _can_interpolate, _disp_vect)
        {

            this.NrX = _nr_x;
            this.MinX = _min_x;
            this.MaxX = _max_x;
            this.MVUnitX = _unit_x;

            this.NrY = _nr_y;
            this.MinY = _min_y;
            this.MaxY = _max_y;
            this.MVUnitY = _unit_y;

            this.MVUnitZ = _unit_z;

            this.xs = _Xs;
            this.ys = _Ys;
            this.field = _Fxys;
        }

        #endregion

        #region .CTOR for COPYING

        protected MultiValueTable2D(MultiValueTable2D _original)
            :base(_original)
        {     }

        internal override MultiValue Clone()
        {
            return new MultiValueTable2D(this);
        }

        #endregion
    }

    public class MultiValueTable3D : MultiValueTable
    {
        #region .CTOR

        internal MultiValueTable3D(List<double> _Xs, string _unit_x, List<double> _Ys, string _unit_y, List<double> _Zs, string _unit_z,
                                 List<double> _Fxyzs, bool _can_interpolate, string _name)
            : base(MultiValueType.FIELD_3D, _name)
        {
            this.MVCanInterpolate = _can_interpolate;
            this.MVUnitX = (string.IsNullOrEmpty(_unit_x)) ? "-" : _unit_x;
            this.MVUnitY = (string.IsNullOrEmpty(_unit_y)) ? "-" : _unit_y;
            this.MVUnitZ = (string.IsNullOrEmpty(_unit_z)) ? "-" : _unit_z;

            if (_Xs != null && _Ys != null && _Zs != null && _Fxyzs != null &&
                _Xs.Count > 0 && _Ys.Count > 0 && _Zs.Count > 0 && _Xs.Count * _Ys.Count * _Zs.Count == _Fxyzs.Count)
            {
                this.xs = _Xs;
                this.ys = _Ys;
                this.zs = _Zs;

                this.NrX = _Xs.Count;
                this.MinX = this.xs.Min();
                this.MaxX = this.xs.Max();
                this.NrY = _Ys.Count;
                this.MinY = this.ys.Min();
                this.MaxY = this.ys.Max();
                this.NrZ = _Zs.Count;
                this.MinZ = this.zs.Min();
                this.MaxZ = this.zs.Max();

                for (int table = 0; table < this.NrZ; table++)
                {
                    for (int r = 0; r < this.NrY; r++)
                    {
                        for (int c = 0; c < this.NrX; c++)
                        {
                            this.field.Add(new Point3D(c, r, table), _Fxyzs[table * this.NrY * this.NrX + r * this.NrX + c]);
                        }
                    }
                }
            }
        }

        #endregion

        #region .CTOR for Parsing

        internal MultiValueTable3D(long _id, string _name, bool _can_interpolate, MultiValPointer _disp_vect,
                                    int _nr_x, double _min_x, double _max_x, string _unit_x,
                                    int _nr_y, double _min_y, double _max_y, string _unit_y,
                                    int _nr_z, double _min_z, double _max_z, string _unit_z,
                                    List<double> _Xs, List<double> _Ys, List<double> _Zs, Dictionary<Point3D, double> _Fxyzs)
            :base(_id, MultiValueType.FIELD_3D, _name, _can_interpolate, _disp_vect)
        {

            this.NrX = _nr_x;
            this.MinX = _min_x;
            this.MaxX = _max_x;
            this.MVUnitX = _unit_x;

            this.NrY = _nr_y;
            this.MinY = _min_y;
            this.MaxY = _max_y;
            this.MVUnitY = _unit_y;

            this.NrZ = _nr_z;
            this.MinZ = _min_z;
            this.MaxZ = _max_z;
            this.MVUnitZ = _unit_z;

            this.xs = _Xs;
            this.ys = _Ys;
            this.zs = _Zs;
            this.field = _Fxyzs;
        }

        #endregion

        #region .CTOR for COPYING

        protected MultiValueTable3D(MultiValueTable3D _original)
            :base(_original)
        {     }

        internal override MultiValue Clone()
        {
            return new MultiValueTable3D(this);
        }

        #endregion
    }
}
