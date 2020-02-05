using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace ParameterStructure.Values
{
    public class MultiValueFunctionND : MultiValueFunction
    {
        #region .CTOR

        internal MultiValueFunctionND(string _unit_x, string _unit_y, string _unit_z, Point4D _bounds,
                                       List<double> _Zs, List<List<Point3D>> _functions, List<string> _fct_names, string _name)
            :base(MultiValueType.FUNCTION_ND, _name)
        {
            this.MVCanInterpolate = true;
            this.MVUnitX = (string.IsNullOrEmpty(_unit_x)) ? "-" : _unit_x;
            this.MVUnitY = (string.IsNullOrEmpty(_unit_y)) ? "-" : _unit_y;
            this.MVUnitZ = (string.IsNullOrEmpty(_unit_z)) ? "-" : _unit_z;

            if (_Zs != null && _Zs.Count > 0 && _functions != null && _functions.Count > 0)
            {
                if (_bounds != null)
                {
                    this.MinX = _bounds.X;
                    this.MaxX = _bounds.Y;
                    this.MinY = _bounds.Z;
                    this.MaxY = _bounds.W;
                }

                this.zs = _Zs;
                this.NrZ = _Zs.Count;
                this.MinZ = this.zs.Min();
                this.MaxZ = this.zs.Max();

                this.graphs = new List<List<Point3D>>();
                foreach (List<Point3D> funct in _functions)
                {
                    if (funct == null || funct.Count < 1) continue;
                    this.graphs.Add(funct);
                }
                this.graph_names = new List<string>(_fct_names);

            }
        }

        #endregion

        #region .CTOR for PARSING

        internal MultiValueFunctionND(long _id, string _name, bool _can_interpolate, MultiValPointer _disp_vect,
                                    string _unit_x, string _unit_y, string _unit_z, Point4D _bounds,
                                    List<double> _Zs, List<List<Point3D>> _Functions, List<string> _Fct_Names)
            :base(_id, MultiValueType.FUNCTION_ND, _name, _can_interpolate, _disp_vect)
        {
            this.MVUnitX = _unit_x;
            this.MVUnitY = _unit_y;
            this.MVUnitZ = _unit_z;
            
            this.MinX = _bounds.X;
            this.MaxX = _bounds.Y;
            this.MinY = _bounds.Z;
            this.MaxY = _bounds.W;          

            this.zs = _Zs;
            this.NrZ = _Zs.Count;
            this.MinZ = this.zs.Min();
            this.MaxZ = this.zs.Max();

            this.graphs = new List<List<Point3D>>();
            foreach (List<Point3D> funct in _Functions)
            {
                if (funct == null || funct.Count < 1) continue;
                this.graphs.Add(funct);
            }
            this.graph_names = new List<string>(_Fct_Names);
        }

        #endregion

        #region .CTOR for COPYING

        internal MultiValueFunctionND(MultiValueFunction _original)
            :base(_original)
        {   }

        internal override MultiValue Clone()
        {
            return new MultiValueFunctionND(this);
        }

        #endregion
    }
}
