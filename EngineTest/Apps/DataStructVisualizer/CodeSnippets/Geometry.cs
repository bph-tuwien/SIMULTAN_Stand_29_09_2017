using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

using System.Windows.Media.Media3D;
using DotNetMatrix;

namespace ClassGenerator.CodeSnippets
{
    internal static class GeometryTransf
    {
        #region TEST DATA

        // big box used as reference for placing others
        public static List<Point3D> BOX_01 = new List<Point3D>()
        {
            new Point3D(4.0000, 2.0000, 3.0000), // lower Rect CCW
            new Point3D(5.4572, 0.6642, 3.3036), // lower Rect CCW
            new Point3D(6.3301, 2.4100, 6.7951), // lower Rect CCW
            new Point3D(4.8729, 3.7457, 6.4915), // lower Rect CCW
            new Point3D(5.9477, 3.8086, 1.6088),
            new Point3D(7.4049, 2.4728, 1.9124),
            new Point3D(8.2778, 4.2185, 5.4039),
            new Point3D(6.8206, 5.5543, 5.1003)
        };

        public static List<Point3D> BOX_01_HALF = new List<Point3D>()
        {
            new Point3D(4.0000, 2.0000, 3.0000), // lower Rect CCW
            new Point3D(5.4572, 0.6642, 3.3036), // lower Rect CCW
            new Point3D(6.3301, 2.4100, 6.7951), // lower Rect CCW
            new Point3D(4.8729, 3.7457, 6.4915)  // lower Rect CCW
        };

        // big irregular object used as reference for placing others
        public static List<Point3D> OBJ_01 = new List<Point3D>()
        {
            new Point3D(5.9477, 3.8086, 1.6088), // upper Rect CCW
            new Point3D(7.4049, 2.4728, 1.9124), // upper Rect CCW
            new Point3D(8.2778, 4.2185, 5.4039), // upper Rect CCW
            new Point3D(6.8206, 5.5543, 5.1003), // upper Rect CCW

            new Point3D(4.5455, 3.0911, 5.1822), // middle Rect CCW
            new Point3D(5.2742, 2.4232, 5.3340), // middle Rect CCW
            new Point3D(5.6015, 3.0778, 6.6433), // middle Rect CCW
            new Point3D(4.8729, 3.7457, 6.4915), // middle Rect CCW

            new Point3D(3.2471, 1.8854, 6.1097),  // lower Rect CCW
            new Point3D(3.9757, 1.2175, 6.2615),  // lower Rect CCW
            new Point3D(4.3030, 1.8721, 7.5708),  // lower Rect CCW
            new Point3D(3.5744, 2.5400, 7.4190)   // lower Rect CCW
        };

        public static Vector3D OBJ_01_CORRECTION = new Point3D(6.8206, 5.5543, 5.1003) - new Point3D(4.8729, 3.7457, 6.4915);

        // small box to be placed
        public static List<Point3D> BOX_02_LC = new List<Point3D>()
        {
            new Point3D( 0.5000, 1.0000, -1.5000),
            new Point3D(-0.5000, 1.0000, -1.5000),
            new Point3D(-0.5000, 1.0000,  1.5000),
            new Point3D( 0.5000, 1.0000,  1.5000),
            new Point3D( 0.5000,-1.0000, -1.5000),
            new Point3D(-0.5000,-1.0000, -1.5000),
            new Point3D(-0.5000,-1.0000,  1.5000),
            new Point3D( 0.5000,-1.0000,  1.5000)
        };

        public static List<Point3D> TEST = new List<Point3D>()
        {
            new Point3D(90, 60, 90),
            new Point3D(90, 90, 30),
            new Point3D(60, 60, 60),
            new Point3D(60, 60, 90),
            new Point3D(30, 30, 30)
        };

        #endregion

        #region Property Extraction: AABB, Pivot, Vector Alignment

        // AA = axis-aligned
        private static void GetAAExtremePoints(List<Point3D> _points, out Point3D pMin, out Point3D pMax)
        {
            if (_points == null || _points.Count < 1)
            {
                pMin = new Point3D(0, 0, 0);
                pMax = new Point3D(0, 0, 0);
                return;
            }
            pMin = _points[0];
            pMax = _points[0];
            foreach (Point3D p in _points)
            {
                if (p.X < pMin.X) pMin.X = p.X;
                if (p.Y < pMin.Y) pMin.Y = p.Y;
                if (p.Z < pMin.Z) pMin.Z = p.Z;

                if (p.X > pMax.X) pMax.X = p.X;
                if (p.Y > pMax.Y) pMax.Y = p.Y;
                if (p.Z > pMax.Z) pMax.Z = p.Z;
            }
        }

        private static Point3D GetPivot(List<Point3D> _points)
        {
            Point3D pivot = new Point3D(0, 0, 0);
            if (_points == null || _points.Count < 1) return pivot;

            int nrP = _points.Count;
            for (int i = 0; i < nrP; i++)
            {
                pivot.X += _points[i].X;
                pivot.Y += _points[i].Y;
                pivot.Z += _points[i].Z;
            }
            pivot.X /= nrP;
            pivot.Y /= nrP;
            pivot.Z /= nrP;

            return pivot;
        }

        private static Matrix3D AlingVector(Vector3D _fixed, Vector3D _to_rotate)
        {
            if (_fixed == null || _to_rotate == null) return Matrix3D.Identity;

            // see file C:\_TU\Code-Test\_theory\RotationMatrix.docx

            Vector3D v_fixed = new Vector3D(_fixed.X, _fixed.Y, _fixed.Z);
            Vector3D v_to_rotate = new Vector3D(_to_rotate.X, _to_rotate.Y, _to_rotate.Z);
            v_fixed.Normalize();
            v_to_rotate.Normalize();

            Vector3D v = Vector3D.CrossProduct(v_to_rotate, v_fixed);
            double s = v.Length;
            double c = Vector3D.DotProduct(v_to_rotate, v_fixed);

            Matrix3D Mvx_ = new Matrix3D(0, -v.Z, v.Y, 0,
                                         v.Z, 0, -v.X, 0,
                                        -v.Y, v.X, 0, 0,
                                           0, 0, 0, 0);
            Matrix3D Mvx = Mvx_.Transpose();

            Matrix3D R;
            if (s != 0)
            {
                //R = Matrix3D.Identity + Mvx + ((1 - c) / (s * s)) * (Mvx * Mvx);
                // Matrix3D R_1 = Mvx.Add(Matrix3D.Identity);

                R = Mvx.Add(Matrix3D.Identity).Add((Mvx * Mvx).Mult((1 - c) / (s * s)));
            }
            else
            {
                R = Matrix3D.Identity;
                if (c < 0)
                    R.Rotate(new Quaternion(new Vector3D(0, 0, 1), 180));
            }

            // debug
            //Vector3D test = R.Transform(v_to_rotate);
            // debug

            return R;
        }

        #endregion

        #region PCA (Principle Component Analysis)

        public static void ComputePCATransf(List<Point3D> _points, out Matrix3D trWC2LC, out Matrix3D trLC2WC)
        {
            trWC2LC = Matrix3D.Identity;
            trLC2WC = Matrix3D.Identity;
            if (_points == null || _points.Count < 1) return;

            Point3D pivot;
            Vector3D vec0, vec1, vec2;
            GeometryTransf.CalculatePCA(_points, out pivot, out vec0, out vec1, out vec2);

            // calculate the transforms
            trWC2LC = GeometryTransf.GetTransformWC2LC(pivot, vec0, vec1, vec2);
            trLC2WC = GeometryTransf.GetTransformLC2WC(pivot, vec0, vec1, vec2);
        }

        // vector _y as user input
        public static void ComputePCATransf(List<Point3D> _points, Vector3D v_correction, out Matrix3D trWC2LC, out Matrix3D trLC2WC)
        {
            trWC2LC = Matrix3D.Identity;
            trLC2WC = Matrix3D.Identity;
            if (_points == null || _points.Count < 1 || v_correction == null) return;

            Point3D pivot;
            Vector3D vec0, vec1, vec2;
            GeometryTransf.CalculatePCA(_points, out pivot, out vec0, out vec1, out vec2);

            // detect which base vector is closest to the correction vector
            v_correction.Normalize();
            double angle0 = Vector3D.DotProduct(v_correction, vec0);
            double angle1 = Vector3D.DotProduct(v_correction, vec1);
            double angle2 = Vector3D.DotProduct(v_correction, vec2);

            Vector3D vec_to_rotate = vec2;
            if (Math.Abs(angle0) < Math.Abs(angle1) && Math.Abs(angle0) < Math.Abs(angle2))
                vec_to_rotate = (angle0 > 0) ? vec0 : -vec0;
            else if (Math.Abs(angle1) < Math.Abs(angle0) && Math.Abs(angle1) < Math.Abs(angle2))
                vec_to_rotate = (angle1 > 0) ? vec1 : -vec1;
            else
                vec_to_rotate = (angle2 > 0) ? vec2 : -vec2;

            // correct by user-defined vector
            Matrix3D R = GeometryTransf.AlingVector(v_correction, vec_to_rotate);
            Vector3D vec0_r = R.Transform(vec0);
            Vector3D vec1_r = R.Transform(vec1);
            Vector3D vec2_r = R.Transform(vec2);

            // debug
            List<Vector3D> ucs_1 = new List<Vector3D> { vec0, vec1, vec2, v_correction };
            List<Vector3D> ucs_2 = new List<Vector3D> { vec0_r, vec1_r, vec2_r };
            string ucs_1_str = GeometryTransf.VectorListToString(ucs_1);
            string ucs_2_str = GeometryTransf.VectorListToString(ucs_2);
            // debug

            // calculate the transforms WITH CORRECTION
            trWC2LC = GeometryTransf.GetTransformWC2LC(pivot, vec0_r, vec1_r, vec2_r);
            trLC2WC = GeometryTransf.GetTransformLC2WC(pivot, vec0_r, vec1_r, vec2_r);
        }

        private static void CalculatePCA(List<Point3D> _points, out Point3D pivotO,
                                            out Vector3D vecX, out Vector3D vecY, out Vector3D vecZ)
        {
            pivotO = new Point3D(0, 0, 0);
            vecX = new Vector3D(0, 0, 0);
            vecY = new Vector3D(0, 0, 0);
            vecZ = new Vector3D(0, 0, 0);
            if (_points == null || _points.Count < 1) return;

            Point3D pivot = GeometryTransf.GetPivot(_points);
            pivotO = new Point3D(pivot.X, pivot.Y, pivot.Z);
            List<Vector3D> point_deviations = _points.Select(x => x - pivot).ToList();
            int nrP = _points.Count;

            #region COVARIANCE:Old
            //// compute the covariance matrix           
            //double[] m = new double[3*nrP];
            //for(int i = 0; i < nrP; i++)
            //{
            //    m[i*3] = point_deviations[i].X;
            //    m[i*3 + 1] = point_deviations[i].Y;
            //    m[i*3 + 2] = point_deviations[i].Z;
            //}
            //MatrixNxN M = new MatrixNxN(nrP, 3, m);
            //MatrixNxN MtxM = MatrixNxN.Squared(M);
            //MtxM.Scale(1.0 / nrP);
            #endregion

            // compute the covariance matrix ...
            // using 3rd party library DotNetMatrix
            double[][] pd_as_array = new double[nrP][];
            for (int i = 0; i < nrP; i++)
            {
                pd_as_array[i] = new double[] { point_deviations[i].X, point_deviations[i].Y, point_deviations[i].Z };
            }
            GeneralMatrix gm_M = new GeneralMatrix(pd_as_array);
            GeneralMatrix gm_Mt = gm_M.Transpose();
            GeneralMatrix gm_Msq = gm_Mt.Multiply(gm_M);
            GeneralMatrix gm_Msqn = gm_Msq.Multiply(1.0 / nrP);

            // extract the sorted Eigenvalues of the matrix...
            // using 3rd party library DotNetMatrix
            EigenvalueDecomposition decomp = gm_Msqn.Eigen();
            GeneralMatrix gm_EVec = decomp.GetV();
            double[] gm_EVal = decomp.RealEigenvalues;

            // from smallest to largest eigenvalue
            vecX = new Vector3D(gm_EVec.GetElement(0, 0), gm_EVec.GetElement(1, 0), gm_EVec.GetElement(2, 0));
            vecY = new Vector3D(gm_EVec.GetElement(0, 1), gm_EVec.GetElement(1, 1), gm_EVec.GetElement(2, 1));
            vecZ = new Vector3D(gm_EVec.GetElement(0, 2), gm_EVec.GetElement(1, 2), gm_EVec.GetElement(2, 2));
        }

        #endregion

        #region Transformations

        public static List<Point3D> TransformBy(List<Point3D> _point, Matrix3D _transform)
        {
            if (_point == null || _point.Count < 1 || _transform == null) return _point;

            Point3D[] point_array = _point.ToArray();
            _transform.Transform(point_array);
            return new List<Point3D>(point_array);
        }

        private static Matrix3D GetTransformWC2LC(Point3D _origin_WC, Vector3D _vecX_WC, Vector3D _vecY_WC, Vector3D _vecZ_WC)
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

        private static Matrix3D GetTransformLC2WC(Point3D _origin_WC, Vector3D _vecX_WC, Vector3D _vecY_WC, Vector3D _vecZ_WC)
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

        #region UTILS

        public static string PointListToString(List<Point3D> _points)
        {
            string output = string.Empty;
            if (_points == null || _points.Count < 1) return output;

            System.IFormatProvider format = new System.Globalization.NumberFormatInfo();
            int n = _points.Count;
            for (int i = 0; i < n; i++)
            {
                Point3D p = _points[i];
                output += "[" + i + "][" + p.X.ToString("F4", format) + "," +
                                           p.Y.ToString("F4", format) + "," +
                                           p.Z.ToString("F4", format) + "]\t";
                output += "{" + p.X.ToString("F4", format) + "," +
                                p.Z.ToString("F4", format) + "," +
                                p.Y.ToString("F4", format) + "}\n";
            }

            return output;
        }

        public static string VectorListToString(List<Vector3D> _vectors)
        {
            string output = string.Empty;
            if (_vectors == null || _vectors.Count < 1) return output;

            System.IFormatProvider format = new System.Globalization.NumberFormatInfo();
            int n = _vectors.Count;
            for (int i = 0; i < n; i++)
            {
                Vector3D v = _vectors[i];
                output += "[" + i + "][" + v.X.ToString("F4", format) + "," +
                                           v.Y.ToString("F4", format) + "," +
                                           v.Z.ToString("F4", format) + "]\t";
                output += "{" + v.X.ToString("F4", format) + "," +
                                v.Z.ToString("F4", format) + "," +
                                v.Y.ToString("F4", format) + "}\n";
            }

            return output;
        }

        public static string MatrixToString(Matrix3D _m)
        {
            string output = string.Empty;
            if (_m == null) return output;

            System.IFormatProvider format = new System.Globalization.NumberFormatInfo();

            output += "|" + _m.M11.ToString("F4", format) + " " + _m.M12.ToString("F4", format) + " " + _m.M13.ToString("F4", format) + " " + _m.M14.ToString("F4", format) + "|\n";
            output += "|" + _m.M21.ToString("F4", format) + " " + _m.M22.ToString("F4", format) + " " + _m.M23.ToString("F4", format) + " " + _m.M24.ToString("F4", format) + "|\n";
            output += "|" + _m.M31.ToString("F4", format) + " " + _m.M32.ToString("F4", format) + " " + _m.M33.ToString("F4", format) + " " + _m.M34.ToString("F4", format) + "|\n";
            output += "|" + _m.OffsetX.ToString("F4", format) + " " + _m.OffsetY.ToString("F4", format) + " " + _m.OffsetZ.ToString("F4", format) + " " + _m.M44.ToString("F4", format) + "|";

            return output;
        }

        #endregion
    }

    #region HELPER CLASSES

    internal class MatrixNxN
    {
        private int nr_rows;
        private int nr_cols;
        private int nr_entries;
        private bool is_valid;
        private double[] values; // row-wise: [row 0][row 1]...[row n]
        public List<double> Values { get { return new List<double>(this.values); } }

        public MatrixNxN(int _nrRows, int _nrCols, double[] _values)
        {
            this.nr_rows = Math.Max(Math.Abs(_nrRows), 1);
            this.nr_cols = Math.Max(Math.Abs(_nrCols), 1);
            this.nr_entries = this.nr_rows * this.nr_cols;

            if (_values == null || _values.Length != this.nr_entries)
                this.is_valid = false;
            else
            {
                this.values = _values;
                this.is_valid = true;
            }
        }

        public void Scale(double _fact)
        {
            if (!this.is_valid) return;
            this.values = this.values.Select(x => x * _fact).ToArray();
        }

        public static MatrixNxN ONEVALUE = new MatrixNxN(1, 1, new double[] { 0 });
        public static MatrixNxN Transpose(MatrixNxN _A)
        {
            if (_A == null || !_A.is_valid) return _A;

            int nR = _A.nr_rows;
            int nC = _A.nr_cols;
            int nE = _A.nr_entries;
            double[] vals = _A.values;
            double[] new_vals = new double[nE];

            // a b c d e f -> a d b e c f
            //---------------------------
            // a b c -> a d
            // d e f    b e
            //          c f
            int counter = 0;
            for (int j = 0; j < nC; j++)
            {
                for (int i = j; i < nE && counter < nE; i += nC, counter++)
                {
                    new_vals[counter] = vals[i];
                }
            }
            return new MatrixNxN(nC, nR, new_vals);
        }

        // A'x A
        public static MatrixNxN Squared(MatrixNxN _A)
        {
            if (_A == null || !_A.is_valid) return MatrixNxN.ONEVALUE;

            int nR = _A.nr_rows;
            int nC = _A.nr_cols;
            int nE = _A.nr_entries;
            double[] vals = _A.values;
            double[] calc = new double[nC * nC];

            double[,] rowsA = new double[nR, nC];
            double[,] colsA = new double[nC, nR];

            // extract the rows of A
            for (int i = 0; i < nR; i++)
            {
                for (int j = 0; j < nC; j++)
                {
                    rowsA[i, j] = vals[i * nC + j];
                }
            }

            // extract the columns of A
            for (int j = 0; j < nC; j++)
            {
                for (int i = j, counter = 0; i < nE && counter < nR; i += nC, counter++)
                {
                    colsA[j, counter] = vals[i];
                }
            }

            // perform calculation
            for (int i = 0; i < nC; i++)
            {
                for (int j = 0; j < nC; j++)
                {
                    calc[i * nC + j] = 0;
                    for (int k = 0; k < nR; k++)
                    {
                        calc[i * nC + j] += colsA[i, k] * colsA[j, k];
                    }
                }
            }

            return new MatrixNxN(nC, nC, calc);
        }

    }
    #endregion

    #region EXTENSION CLASSES

    public static class Matrix3DExtensions
    {
        public static Matrix3D Add(this Matrix3D _m1, Matrix3D _m2)
        {
            if (_m1 == null || _m2 == null) return Matrix3D.Identity;

            Matrix3D added = Matrix3D.Identity;

            added.M11 = _m1.M11 + _m2.M11;
            added.M12 = _m1.M12 + _m2.M12;
            added.M13 = _m1.M13 + _m2.M13;
            added.M14 = _m1.M14 + _m2.M14;

            added.M21 = _m1.M21 + _m2.M21;
            added.M22 = _m1.M22 + _m2.M22;
            added.M23 = _m1.M23 + _m2.M23;
            added.M24 = _m1.M24 + _m2.M24;

            added.M31 = _m1.M31 + _m2.M31;
            added.M32 = _m1.M32 + _m2.M32;
            added.M33 = _m1.M33 + _m2.M33;
            added.M34 = _m1.M34 + _m2.M34;

            added.OffsetX = _m1.OffsetX + _m2.OffsetX;
            added.OffsetY = _m1.OffsetY + _m2.OffsetY;
            added.OffsetZ = _m1.OffsetZ + _m2.OffsetZ;
            added.M44 = _m1.M44 + _m2.M44;

            return added;
        }

        public static Matrix3D Mult(this Matrix3D _m, double _fact)
        {
            if (_m == null) return Matrix3D.Identity;

            Matrix3D factored = Matrix3D.Identity;

            factored.M11 = _m.M11 * _fact;
            factored.M12 = _m.M12 * _fact;
            factored.M13 = _m.M13 * _fact;
            factored.M14 = _m.M14 * _fact;

            factored.M21 = _m.M21 * _fact;
            factored.M22 = _m.M22 * _fact;
            factored.M23 = _m.M23 * _fact;
            factored.M24 = _m.M24 * _fact;

            factored.M31 = _m.M31 * _fact;
            factored.M32 = _m.M32 * _fact;
            factored.M33 = _m.M33 * _fact;
            factored.M34 = _m.M34 * _fact;

            factored.OffsetX = _m.OffsetX * _fact;
            factored.OffsetY = _m.OffsetY * _fact;
            factored.OffsetZ = _m.OffsetZ * _fact;
            factored.M44 = _m.M44 * _fact;

            return factored;
        }

        public static Matrix3D Transpose(this Matrix3D _m)
        {
            if (_m == null) return Matrix3D.Identity;

            Matrix3D transposed = Matrix3D.Identity;
            transposed.M11 = _m.M11;
            transposed.M12 = _m.M21;
            transposed.M13 = _m.M31;
            transposed.M14 = _m.OffsetX;

            transposed.M21 = _m.M12;
            transposed.M22 = _m.M22;
            transposed.M23 = _m.M32;
            transposed.M24 = _m.OffsetY;

            transposed.M31 = _m.M13;
            transposed.M32 = _m.M23;
            transposed.M33 = _m.M33;
            transposed.M34 = _m.OffsetZ;

            transposed.OffsetX = _m.M14;
            transposed.OffsetY = _m.M24;
            transposed.OffsetZ = _m.M34;
            transposed.M44 = _m.M44;

            return transposed;
        }

    }

    #endregion
}
