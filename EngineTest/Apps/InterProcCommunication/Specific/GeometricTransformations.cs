using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows.Media.Media3D;

namespace InterProcCommunication.Specific
{
    public static class GeometricTransformations
    {
        #region Transformations

        public static List<Point3D> TransformBy(List<Point3D> _point, Matrix3D _transform)
        {
            if (_point == null || _point.Count < 1 || _transform == null) return _point;

            Point3D[] point_array = _point.ToArray();
            _transform.Transform(point_array);
            return new List<Point3D>(point_array);
        }

        public static Matrix3D GetTransformWC2LC(Point3D _origin_WC, Vector3D _vecX_WC, Vector3D _vecY_WC, Vector3D _vecZ_WC)
        {
            if (_origin_WC == null || _vecX_WC == null || _vecY_WC == null || _vecZ_WC == null) return Matrix3D.Identity;

            // calculate the transform (World Space -> Local Space)
            Matrix3D ucs_T = Matrix3D.Identity;
            ucs_T.Translate(new Point3D(0, 0, 0) - _origin_WC);
            Matrix3D ucs_R = new Matrix3D(_vecX_WC.X, _vecY_WC.X, _vecZ_WC.X, 0,
                                          _vecX_WC.Y, _vecY_WC.Y, _vecZ_WC.Y, 0,
                                          _vecX_WC.Z, _vecY_WC.Z, _vecZ_WC.Z, 0,
                                          0, 0, 0, 1);

            Matrix3D ucs_Combined = ucs_T * ucs_R;
            //string ucs_Combined_str = GeometryCodeSnippets.MatrixToString(ucs_Combined);
            return ucs_Combined;
        }

        public static Matrix3D GetTransformLC2WC(Point3D _origin_WC, Vector3D _vecX_WC, Vector3D _vecY_WC, Vector3D _vecZ_WC)
        {
            if (_origin_WC == null || _vecX_WC == null || _vecY_WC == null || _vecZ_WC == null) return Matrix3D.Identity;

            // calculate the transform (World Space -> Local Space)
            Matrix3D ucs_T = Matrix3D.Identity;
            ucs_T.Translate(_origin_WC - new Point3D(0, 0, 0));
            Matrix3D ucs_R = new Matrix3D(_vecX_WC.X, _vecX_WC.Y, _vecX_WC.Z, 0,
                                          _vecY_WC.X, _vecY_WC.Y, _vecY_WC.Z, 0,
                                          _vecZ_WC.X, _vecZ_WC.Y, _vecZ_WC.Z, 0,
                                          0, 0, 0, 1);

            Matrix3D ucs_Combined = ucs_R * ucs_T;
            //string ucs_Combined_str = GeometryCodeSnippets.MatrixToString(ucs_Combined);
            return ucs_Combined;
        }

        #endregion

        #region DERVIVED

        public static Matrix3D PackUCS(Point3D _origin_WC, Vector3D _vecX_WC, Vector3D _vecY_WC, Vector3D _vecZ_WC)
        {
            return new Matrix3D(_vecX_WC.X,   _vecX_WC.Y,   _vecX_WC.Z,   0,
                                _vecY_WC.X,   _vecY_WC.Y,   _vecY_WC.Z,   0,
                                _vecZ_WC.X,   _vecZ_WC.Y,   _vecZ_WC.Z,   0,
                                _origin_WC.X, _origin_WC.Y, _origin_WC.Z, 1);
        }

        public static Point3D UnpackOrigin(Matrix3D _matrix)
        {
            return new Point3D(_matrix.OffsetX, _matrix.OffsetY, _matrix.OffsetZ);
        }

        public static Vector3D UnpackXAxis(Matrix3D _matrix)
        {
            return new Vector3D(_matrix.M11, _matrix.M12, _matrix.M13);
        }

        public static Vector3D UnpackYAxis(Matrix3D _matrix)
        {
            return new Vector3D(_matrix.M21, _matrix.M22, _matrix.M23);
        }

        public static Vector3D UnpackZAxis(Matrix3D _matrix)
        {
            return new Vector3D(_matrix.M31, _matrix.M32, _matrix.M33);
        }


        private static void TranslateMatrix(ref Matrix3D _matrix, Vector3D _v, double _amount)
        {
            Point3D origin = new Point3D(_matrix.OffsetX, _matrix.OffsetY, _matrix.OffsetZ);
            _v.Normalize();
            Point3D origin_new = origin + _v * _amount;

            _matrix.OffsetX = origin_new.X;
            _matrix.OffsetY = origin_new.Y;
            _matrix.OffsetZ = origin_new.Z;
        }

        public static void TranslateMatrixAlongX(ref Matrix3D _matrix, double _amount)
        {
            Vector3D vX = new Vector3D(_matrix.M11, _matrix.M12, _matrix.M13);
            TranslateMatrix(ref _matrix, vX, _amount);
        }

        public static void TranslateMatrixAlongY(ref Matrix3D _matrix, double _amount)
        {
            Vector3D vY = new Vector3D(_matrix.M21, _matrix.M22, _matrix.M23);
            TranslateMatrix(ref _matrix, vY, _amount);
        }

        public static void TranslateMatrixAlongZ(ref Matrix3D _matrix, double _amount)
        {
            Vector3D vZ = new Vector3D(_matrix.M31, _matrix.M32, _matrix.M33);
            TranslateMatrix(ref _matrix, vZ, _amount);
        }

        #endregion

        #region Logical Equality

        public static bool LogicalEquality(Matrix3D _m1, Matrix3D _m2, double _tolerance = 0.0001)
        {
            if (_m1 == null && _m2 == null) return true;
            if (_m1 != null && _m2 == null) return false;
            if (_m1 == null && _m2 != null) return false;

            if (!(GeometricTransformations.LogicalEquality(_m1.M11, _m2.M11, _tolerance))) return false;
            if (!(GeometricTransformations.LogicalEquality(_m1.M12, _m2.M12, _tolerance))) return false;
            if (!(GeometricTransformations.LogicalEquality(_m1.M13, _m2.M13, _tolerance))) return false;
            if (!(GeometricTransformations.LogicalEquality(_m1.M14, _m2.M14, _tolerance))) return false;

            if (!(GeometricTransformations.LogicalEquality(_m1.M21, _m2.M21, _tolerance))) return false;
            if (!(GeometricTransformations.LogicalEquality(_m1.M22, _m2.M22, _tolerance))) return false;
            if (!(GeometricTransformations.LogicalEquality(_m1.M23, _m2.M23, _tolerance))) return false;
            if (!(GeometricTransformations.LogicalEquality(_m1.M24, _m2.M24, _tolerance))) return false;

            if (!(GeometricTransformations.LogicalEquality(_m1.M31, _m2.M31, _tolerance))) return false;
            if (!(GeometricTransformations.LogicalEquality(_m1.M32, _m2.M32, _tolerance))) return false;
            if (!(GeometricTransformations.LogicalEquality(_m1.M33, _m2.M33, _tolerance))) return false;
            if (!(GeometricTransformations.LogicalEquality(_m1.M34, _m2.M34, _tolerance))) return false;


            if (!(GeometricTransformations.LogicalEquality(_m1.OffsetX, _m2.OffsetX, _tolerance))) return false;
            if (!(GeometricTransformations.LogicalEquality(_m1.OffsetY, _m2.OffsetY, _tolerance))) return false;
            if (!(GeometricTransformations.LogicalEquality(_m1.OffsetZ, _m2.OffsetZ, _tolerance))) return false;
            if (!(GeometricTransformations.LogicalEquality(_m1.M44, _m2.M44, _tolerance))) return false;

            return true;
        }

        public static bool LogicalEquality(double _d1, double _d2, double _tolerance = 0.0001)
        {
            if (double.IsNaN(_d1) && !double.IsNaN(_d2)) return false;
            if (!double.IsNaN(_d1) && double.IsNaN(_d2)) return false;
            if (double.IsNaN(_d1) && double.IsNaN(_d2)) return true;

            if (Math.Abs(_d1 - _d2) >= _tolerance) return false;

            return true;
        }

        public static bool LogicalEquality(Point3D _p1, Point3D _p2, double _tolerance = 0.0001)
        {
            if (_p1 == null && _p2 != null) return false;
            if (_p1 != null && _p2 == null) return false;
            if (_p1 == null && _p2 == null) return true;

            if (!GeometricTransformations.LogicalEquality(_p1.X, _p2.X, _tolerance)) return false;
            if (!GeometricTransformations.LogicalEquality(_p1.Y, _p2.Y, _tolerance)) return false;
            if (!GeometricTransformations.LogicalEquality(_p1.Z, _p2.Z, _tolerance)) return false;

            return true;
        }

        #endregion

    }

    #region CUSTOM COMPARER: Vector3D, Point4D

    public class Vector3DComparer : IComparer<Vector3D>
    {
        private double tolerance;
        public Vector3DComparer(double _tolerance = 0.0001)
        {
            this.tolerance = _tolerance;
        }

        public int Compare(Vector3D _v1, Vector3D _v2)
        {
            bool sameX = Math.Abs(_v1.X - _v2.X) <= this.tolerance;
            bool sameY = Math.Abs(_v1.Y - _v2.Y) <= this.tolerance;
            bool sameZ = Math.Abs(_v1.Z - _v2.Z) <= this.tolerance;

            if (sameX)
            {
                if (sameZ)
                {
                    if (sameY)
                        return 0;
                    else if (_v1.Y > _v2.Y)
                        return 1;
                    else
                        return -1;
                }
                else if (_v1.Z > _v2.Z)
                    return 1;
                else
                    return -1;
            }
            else if (_v1.X > _v2.X)
                return 1;
            else
                return -1;
        }
    }

    public class Point4DComparer : IComparer<Point4D>
    {
        private double tolerance;
        public Point4DComparer(double _tolerance = 0.0001)
        {
            this.tolerance = _tolerance;
        }

        public int Compare(Point4D _p1, Point4D _p2)
        {
            bool sameX = Math.Abs(_p1.X - _p2.X) <= this.tolerance;
            bool sameY = Math.Abs(_p1.Y - _p2.Y) <= this.tolerance;
            bool sameZ = Math.Abs(_p1.Z - _p2.Z) <= this.tolerance;
            bool sameW = Math.Abs(_p1.W - _p2.W) <= this.tolerance;

            if (sameX)
            {
                if (sameZ)
                {
                    if (sameY)
                    {
                        if (sameW)
                            return 0;
                        else if (_p1.W > _p2.W)
                            return 1;
                        else
                            return -1;
                    }
                    else if (_p1.Y > _p2.Y)
                        return 1;
                    else
                        return -1;
                }
                else if (_p1.Z > _p2.Z)
                    return 1;
                else
                    return -1;
            }
            else if (_p1.X > _p2.X)
                return 1;
            else
                return -1;
        }
    }

    #endregion
}
