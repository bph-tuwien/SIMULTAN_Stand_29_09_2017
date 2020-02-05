using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Windows;
using System.Windows.Media.Media3D;

namespace ClassGenerator.CodeSnippets
{
    #region HELPER CLASSES
    internal class Coords2D
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Coords2D(double _x, double _y)
        {
            this.X = _x;
            this.Y = _y;
        }

        public static bool InGeneralPosition(Coords2D _xy1, Coords2D _xy2)
        {
            if (_xy1 == null || _xy2 == null) return false;

            double diffX = _xy1.X - _xy2.X;
            double diffY = _xy1.Y - _xy2.Y;
            return (Math.Abs(diffX) >= Calculation.MIN_DOUBLE_VAL || Math.Abs(diffY) >= Calculation.MIN_DOUBLE_VAL);
        }

        public static bool InGeneralPosition(Coords2D _xy1, Coords2D _xy2, Coords2D _xy3)
        {
            return Coords2D.InGeneralPosition(_xy1, _xy2) && 
                   Coords2D.InGeneralPosition(_xy2, _xy3) && 
                   Coords2D.InGeneralPosition(_xy3, _xy1);
        }

        public static bool IsInsideTriangle(Coords2D _xyA, Coords2D _xyB, Coords2D _xyC, Coords2D _xy, bool _check_triangle = false)
        {
            if (_check_triangle)
                if (!Coords2D.InGeneralPosition(_xyA, _xyB, _xyC)) return false;

            if (_xyA == null || _xyB == null || _xyC == null) return false;

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

        public static void GetBarycentricCoords(Coords2D _xyA, Coords2D _xyB, Coords2D _xyC, Coords2D _xy,
                                                out double a, out double b, out double c)
        {
            a = double.NaN;
            b = double.NaN;
            c = double.NaN;
            if (!Coords2D.IsInsideTriangle(_xyA, _xyB, _xyC, _xy, true)) return;

            // solving the eq system:
            // _xy.X = a * _xyA.X + b * _xyB.X + (1 - a - b) * _xyC.X
            // _xy.Y = a * _xyA.Y + b * _xyB.Y + (1 - a - b) * _xyC.Y
            double denominator = (_xyB.Y - _xyC.Y) * (_xyA.X - _xyC.X) + (_xyC.X - _xyB.X) * (_xyA.Y - _xyC.Y);
            if (Math.Abs(denominator) < Calculation.MIN_DOUBLE_VAL) return;

            a = ((_xyB.Y - _xyC.Y) * (_xy.X - _xyC.X) + (_xyC.X - _xyB.X) * (_xy.Y - _xyC.Y)) / denominator;
            b = ((_xy.Y - _xyC.Y) * (_xyA.X - _xyC.X) + (_xyC.X - _xy.X) * (_xyA.Y - _xyC.Y)) / denominator;
            c = 1 - a - b;
        }
    }
    internal class Coords2DComparer : IComparer<Coords2D>
    {
        public int Compare(Coords2D _xy1, Coords2D _xy2)
        {
            if (_xy1 == null && _xy2 == null) return 0;
            if (_xy1 != null && _xy2 == null) return 1;
            if (_xy1 == null && _xy2 != null) return -1;

            double diffX = _xy1.X - _xy2.X;
            double diffY = _xy1.Y - _xy2.Y;

            if (Math.Abs(diffX) < Calculation.MIN_DOUBLE_VAL)
            {
                if (Math.Abs(diffY) < Calculation.MIN_DOUBLE_VAL)
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
    #endregion

    internal static class Calculation
    {
        public static double MIN_DOUBLE_VAL = 0.0001;

        #region Value Alignment PRIVATE

        private static Dictionary<double, double> Lists2Func1D(List<double> _xs, List<double> _ys)
        {
            if (_xs == null || _ys == null) return null;

            int n = _xs.Count;
            if (n < 1 || n != _ys.Count) return new Dictionary<double,double>();

            Dictionary<double, double> Fx = new Dictionary<double, double>();
            for(int i = 0; i < n; i++)
            {
                if (Fx.ContainsKey(_xs[i])) continue;
                Fx.Add(_xs[i], _ys[i]);
            }
            return Fx;
        }

        private static SortedList<double, double> List2Func1DSorted(List<double> _xs, List<double> _ys)
        {
            if (_xs == null || _ys == null) return null;

            int n = _xs.Count;
            if (n < 1 || n != _ys.Count) return new SortedList<double, double>();

            SortedList<double, double> Fx = new SortedList<double, double>();
            for (int i = 0; i < n; i++)
            {
                if (Fx.ContainsKey(_xs[i])) continue;
                Fx.Add(_xs[i], _ys[i]);
            }
            return Fx;
        }

        private static Dictionary<Coords2D, double> Lists2Func2D(List<Coords2D> _xys, List<double> _zs)
        {
            if (_xys == null || _zs == null) return null;

            int n = _xys.Count;
            if (n < 1 || n != _zs.Count) return new Dictionary<Coords2D, double>();

            Dictionary<Coords2D, double> Fxy = new Dictionary<Coords2D, double>();
            for (int i = 0; i < n; i++)
            {
                if (Fxy.ContainsKey(_xys[i])) continue;
                Fxy.Add(_xys[i], _zs[i]);
            }
            return Fxy;
        }

        private static Dictionary<Coords2D, double> Lists2Func2D(List<double> _xs, List<double> _ys, List<double> _zs)
        {
            if (_xs == null || _ys == null || _zs == null) return null;

            int n = _xs.Count;
            if (n < 1 || n != _ys.Count || n != _zs.Count) return new Dictionary<Coords2D, double>();

            Dictionary<Coords2D, double> Fxy = new Dictionary<Coords2D, double>();
            for (int i = 0; i < n; i++)
            {
                Coords2D xy = new Coords2D(_xs[i], _ys[i]);
                if (Fxy.ContainsKey(xy)) continue;
                Fxy.Add(xy, _zs[i]);
            }
            return Fxy;
        }

        #endregion

        #region Value Interpolation: 1D, 2D PRIVATE
        private static double Interpolate1D(double _x, SortedList<double, double> _Fx)
        {
            if (_Fx == null || _Fx.Count < 2) return double.NaN;

            // find closest x values
            double x0 = double.MinValue;
            double x1 = double.MaxValue;
            foreach (double x_current in _Fx.Keys)
            {
                if (x_current < _x) x0 = x_current;
                if (x_current >= _x)
                {
                    x1 = x_current;
                    break;
                }
            }

            // out of range check
            if (x0 == double.MinValue || x1 == double.MaxValue)
                return double.NaN;
            
            double rangex = Math.Abs(x1 - x0);
            double x0_x = Math.Abs(_x - x0);
            double x_x1 = Math.Abs(x1 - _x);

            // exact match check
            if (x0_x < Calculation.MIN_DOUBLE_VAL)
                return _Fx[x0];
            if (x_x1 < Calculation.MIN_DOUBLE_VAL)
                return _Fx[x1];
            if (rangex < Calculation.MIN_DOUBLE_VAL)
                return double.NaN;

            // interpolate
            return (x0_x / rangex) * _Fx[x0] + (x_x1 / rangex) * _Fx[x1];
        }

        private static double Interpolate2D(Coords2D _xy, Dictionary<Coords2D, double> _Fxy)
        {
            if (_Fxy == null || _Fxy.Count < 3) return double.NaN;

            // find closest xy values
            SortedList<double, Coords2D> distances = new SortedList<double, Coords2D>();
            foreach (Coords2D xy_current in _Fxy.Keys)
            {
                double dist = Math.Abs(_xy.X - xy_current.X) + Math.Abs(_xy.Y - xy_current.Y);
                distances.Add(dist, xy_current);
            }

            // construct the smallest well-defined triangle containing _xy
            Coords2D xyA = distances.ElementAt(0).Value;
            Coords2D xyB = distances.ElementAt(1).Value;
            int i = 1;
            if (!Coords2D.InGeneralPosition(xyA, xyB))
            {
                for(; i < distances.Count; i++)
                {
                    xyB = distances.ElementAt(i).Value;
                    if (Coords2D.InGeneralPosition(xyA, xyB))
                        break;
                }
            }
            if (!Coords2D.InGeneralPosition(xyA, xyB)) return double.NaN;

            Coords2D xyC = null;
            for(int j = 1; j < distances.Count && j != i; j++)
            {
                xyC = distances.ElementAt(j).Value;
                if (Coords2D.InGeneralPosition(xyA, xyB, xyC) && Coords2D.IsInsideTriangle(xyA, xyB, xyC, _xy, false))
                    break;
            }
            if (!Coords2D.InGeneralPosition(xyA, xyB, xyC) || !Coords2D.IsInsideTriangle(xyA, xyB, xyC, _xy, false)) return double.NaN;

            // perform actual interpolation
            double a, b, c;
            Coords2D.GetBarycentricCoords(xyA, xyB, xyC, _xy, out a, out b, out c);
            if (double.IsNaN(a) || double.IsNaN(b) || double.IsNaN(c)) return double.NaN;

            return a * _Fxy[xyA] + b * _Fxy[xyB] + c * _Fxy[xyC];
        }

        #endregion

        #region Value Interpolation: 1D, 2D PUBLIC

        public static double Interpolate1D(double _x, List<double> _Xs, List<double> _Ys)
        {
            SortedList<double, double> Fx = List2Func1DSorted(_Xs, _Ys);
            return Interpolate1D(_x, Fx);
        }

        public static double Interpolate2D(Coords2D _xy, List<Coords2D> _XYs, List<double> _Zs)
        {
            Dictionary<Coords2D, double> Fxy = Lists2Func2D(_XYs, _Zs);
            return Interpolate2D(_xy, Fxy);
        }

        public static double Interpolate2D(double _x, double _y, List<double> _Xs, List<double> _Ys, List<double> _Zs)
        {
            Dictionary<Coords2D, double> Fxy = Lists2Func2D(_Xs, _Ys, _Zs);
            return Interpolate2D(new Coords2D(_x, _y), Fxy);
        }

        #endregion
    }
}
