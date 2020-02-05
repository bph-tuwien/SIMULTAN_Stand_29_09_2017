using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

namespace DXFImportExport
{
    public class DXFGeometry
    {

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ================================================= STATIC =============================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Instance Utils

        public static readonly float TOLERANCE = 0.0001f;

        public static List<Matrix3D> CalInstanceTransforms(Point3D _origin, Vector3D _axis, Point3D _scale, float _rotation,
                                int _nrRows, float _rowSpacing, int _nrCols, float _colSpacing)
        {
            if (_origin == null || _axis == null || _scale == null || _nrRows < 1 || _nrCols < 1 ||
                (_nrRows > 1 && _rowSpacing < DXFGeometry.TOLERANCE) || 
                (_nrCols > 1 && _colSpacing < DXFGeometry.TOLERANCE))
                return null;

            // calculate the transformation for all instances
            Matrix3D M = Matrix3D.Identity;
            M.Scale(new Vector3D(_scale.X, _scale.Y, _scale.Z));
            M.Rotate(new Quaternion(new Vector3D(0, 0, 1), _rotation)); // rotation in degrees
     
            // calculate the REALTIVE TRANSLATION per instance
            float[] stepsX = new float[_nrCols];
            stepsX[0] = 0f;
            for (int i = 1; i < _nrCols; i++)
            {
                stepsX[i] = stepsX[0] + _colSpacing * i;
            }
            float[] stepsY = new float[_nrRows];
            stepsY[0] = 0f;
            for (int i = 1; i < _nrRows; i++)
            {
                stepsY[i] = stepsY[0] + _rowSpacing * i;
            }
            // calculate the ABSOLUTE TRANSLATION per instance
            List<Matrix3D> matrices = new List<Matrix3D>();
            for (int i = 0; i < _nrCols; i++)
            {
                for (int j = 0; j < _nrRows; j++)
                {
                    Matrix3D ijM = CopyMatrix3D(M);
                    ijM.OffsetX = stepsX[i] * Math.Cos(_rotation * Math.PI / 180) - stepsY[j] * Math.Sin(_rotation * Math.PI / 180);
                    ijM.OffsetY = stepsX[i] * Math.Sin(_rotation * Math.PI / 180) + stepsY[j] * Math.Cos(_rotation * Math.PI / 180);
                    matrices.Add(ijM);
                }
            }

            // TRANSFORM INTO THE CORRECT CS

            // calculate the CS
            Vector3D axisX, axisY, axisZ;
            CalPlaneCS(_origin, _axis, out axisX, out axisY, out axisZ);
            Point3D originNew = new Point3D(0, 0, 0) + _origin.X * axisX + _origin.Y * axisY + _origin.Z * axisZ;
            Matrix3D Mcs = new Matrix3D();
            Mcs.M11 = axisX.X; Mcs.M12 = axisX.Y; Mcs.M13 = axisX.Z;
            Mcs.M21 = axisY.X; Mcs.M22 = axisY.Y; Mcs.M23 = axisY.Z;
            Mcs.M31 = axisZ.X; Mcs.M32 = axisZ.Y; Mcs.M33 = axisZ.Z;
            Mcs.OffsetX = originNew.X; Mcs.OffsetY = originNew.Y; Mcs.OffsetZ = originNew.Z;

            // transform
            for (int k = 0; k < matrices.Count; k++ )
            {
                Matrix3D m = matrices[k];
                Matrix3D mN = m * Mcs;
                matrices[k] = mN;
            }

            return matrices;
        }
        #endregion

        #region General Utils
        private static Matrix3D CopyMatrix3D(Matrix3D _m)
        {
            Matrix3D matrix = new Matrix3D();
            matrix.M11 = _m.M11;
            matrix.M12 = _m.M12;
            matrix.M13 = _m.M13;
            matrix.M14 = _m.M14;

            matrix.M21 = _m.M21;
            matrix.M22 = _m.M22;
            matrix.M23 = _m.M23;
            matrix.M24 = _m.M24;

            matrix.M31 = _m.M31;
            matrix.M32 = _m.M32;
            matrix.M33 = _m.M33;
            matrix.M34 = _m.M34;

            matrix.OffsetX = _m.OffsetX;
            matrix.OffsetY = _m.OffsetY;
            matrix.OffsetZ = _m.OffsetZ;
            matrix.M44 = _m.M44;

            return matrix;
        }
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ======================================= CLASS MEMBERS & CONSTRUCTOR ==================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region INIT

        private string name;
        public string Name 
        {
            get { return this.name; }
            set
            {
                this.name = value;
            }
        }
        public List<Point3D> Coords { get; private set; }
        public List<bool> Connected { get; private set; }
        public List<float> Width { get; private set; }
        public string LayerName { get; set; }
        public DXFColor Color { get; set; }
        public List<string> TextContent { get; private set; }
        public Matrix3D TextTransf { get; private set; }

        private Point3D csOrigin;
        private Vector3D csXaxisV;
        private Vector3D csYaxisV;
        private Vector3D csZaxisV;

        public DXFGeometry()
        {
            this.Name = "Shape";
            this.Coords = new List<Point3D>();
            this.Connected = new List<bool>();
            this.Width = new List<float>();
            this.LayerName = string.Empty;
            this.Color = DXFColor.clNone;
            this.TextContent = new List<string>();
            this.TextTransf = Matrix3D.Identity;

            this.csOrigin = new Point3D(0, 0, 0);
            this.csXaxisV = new Vector3D(1, 0, 0);
            this.csYaxisV = new Vector3D(0, 1, 0);
            this.csZaxisV = new Vector3D(0, 0, 1);
        }

        public DXFGeometry(DXFGeometry _other)
        {
            this.Name = _other.Name;
            this.Coords = new List<Point3D>(_other.Coords);
            this.Connected = new List<bool>(_other.Connected);
            this.Width = new List<float>(_other.Width);
            this.LayerName = string.Empty;
            this.Color = _other.Color;
            this.TextContent = new List<string>(_other.TextContent);

            this.csOrigin = new Point3D(_other.csOrigin.X, _other.csOrigin.Y, _other.csOrigin.Z);
            this.csXaxisV = new Vector3D(_other.csXaxisV.X, _other.csXaxisV.Y, _other.csXaxisV.Z);
            this.csYaxisV = new Vector3D(_other.csYaxisV.X, _other.csYaxisV.Y, _other.csYaxisV.Z);
            this.csZaxisV = new Vector3D(_other.csZaxisV.X, _other.csZaxisV.Y, _other.csZaxisV.Z);
        }

        public bool IsEmpty()
        {
            return (this.Coords.Count < 1);
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ============================================= GEOMETRY DEFINITION ====================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // --------------------------------------------------- VERTEX --------------------------------------------- //

        #region VERTEX
        public void AddVertex(Point3D _p, bool _conn, float _width)
        {
            if (_p == null)
                return;
            this.Coords.Add(_p);
            this.Connected.Add(_conn);
            this.Width.Add(_width);
        }

        public void AddVertex(Point3D _p, bool _conn, float _width, Point3D _origin, Vector3D _normal)
        {
            if (_origin == null || _normal == null || _p == null)
                return;

            // determine the coordinate system:
            CalculatePlaneCS(_origin, _normal);

            Point3D pNew = this.csOrigin + _p.X * this.csXaxisV + _p.Y * this.csYaxisV + _p.Z * this.csZaxisV;
            AddVertex(pNew, _conn, _width);
        }
        #endregion

        // ---------------------------------------------------- LINES --------------------------------------------- //

        #region LINE LWPOLYLINE POLYLINE
        public void AddLines(List<Point3D> _ps, bool _conn, float _width)
        {
            if (_ps == null || _ps.Count < 1)
                return;

            foreach(var p in _ps)
            {
                this.Coords.Add(p);
                this.Connected.Add(_conn);
                this.Width.Add(_width);
            }
            // mark last point as disconnected from the next ones
            int n = this.Coords.Count;
            this.Connected[n - 1] = false;
        }

        public void AddLines(List<Point3D> _ps, bool _conn, List<Point3D> _ws)
        {
            if (_ps == null || _ps.Count < 1 || _ws == null || _ws.Count < 1 || _ps.Count != _ws.Count)
                return;

            int n = _ps.Count;
            for(int i = 0; i < n; i++)
            {
                this.Coords.Add(_ps[i]);
                this.Connected.Add(_conn);
                this.Width.Add((float)_ws[i].X);
            }
            // mark last point as disconnected from the next ones
            n = this.Coords.Count;
            this.Connected[n - 1] = false;
        }

        public void AddLines(List<Point3D> _ps, bool _conn, float _width, Point3D _origin, Vector3D _normal)
        {
            if (_ps == null || _ps.Count < 1 || _origin == null || _normal == null)
                return;

            // determine the coordinate system:
            CalculatePlaneCS(_origin, _normal);

            foreach (var p in _ps)
            {
                Point3D pNew = this.csOrigin + p.X * this.csXaxisV + p.Y * this.csYaxisV + p.Z * this.csZaxisV;
                this.Coords.Add(pNew);
                this.Connected.Add(_conn);
                this.Width.Add(_width);
            }
            // mark last point as disconnected from the next ones
            int n = this.Coords.Count;
            this.Connected[n - 1] = false;

            //// debug
            //Point3D test1 = this.Coords[0];
            //Point3D test3 = test1 + this.csXaxisV * 10;
            //Point3D test4 = test1 + this.csYaxisV * 10;
            //Point3D test5 = test1 + this.csZaxisV * 10;
            //this.Coords.Add(test1);
            //this.Connected.Add(_conn);
            //this.Width.Add(2f);
            //this.Coords.Add(test3);
            //this.Connected.Add(_conn);
            //this.Width.Add(2f);
            //this.Coords.Add(test1);
            //this.Connected.Add(_conn);
            //this.Width.Add(2f);
            //this.Coords.Add(test4);
            //this.Connected.Add(_conn);
            //this.Width.Add(2f);
            //this.Coords.Add(test5);
            //this.Connected.Add(_conn);
            //this.Width.Add(2f);
            //this.Coords.Add(test1);
            //this.Connected.Add(_conn);
            //this.Width.Add(2f);
        }

        public void AddLines(List<Point3D> _ps, bool _conn, List<Point3D> _ws, Point3D _origin, Vector3D _normal)
        {
            if (_ps == null || _ps.Count < 1 || _ws == null || _ws.Count < 1 || _ps.Count != _ws.Count ||
                _origin == null || _normal == null)
                return;

            // determine the coordinate system:
            CalculatePlaneCS(_origin, _normal);

            int n = _ps.Count;
            for (int i = 0; i < n; i++)
            {
                Point3D pNew = this.csOrigin + _ps[i].X * this.csXaxisV + _ps[i].Y * this.csYaxisV + _ps[i].Z * this.csZaxisV;
                this.Coords.Add(pNew);
                this.Connected.Add(_conn);
                this.Width.Add((float)_ws[i].X);
            }
            // mark last point as disconnected from the next ones
            n = this.Coords.Count;
            this.Connected[n - 1] = false;
        }
        #endregion

        // --------------------------------------------------- CIRCLE --------------------------------------------- //

        #region CIRCLE ARC
        // angles supplied in degrees (full rotation: 0 - 360)
        public void AddCircleArc(Point3D _center, Vector3D _normal, float _radius, 
                                 float _angleS, float _angleE, float _width, int _steps = 18)
        {
            if (_center == null || _normal == null || _radius <= 0 || Math.Abs(_angleS - _angleE) < DXFGeometry.TOLERANCE || _steps < 4)
                return;

            // determine the coordinate system of the circle arc:
            CalculatePlaneCS(_center, _normal);
            Point3D centerNew = this.csOrigin + _center.X * this.csXaxisV + _center.Y * this.csYaxisV + _center.Z * this.csZaxisV;

            List<Point3D> points = new List<Point3D>();
            float angleStep = (2f * (float)Math.PI) / _steps;

            float angleS = _angleS * (float)Math.PI / 180f;
            float angleE = _angleE * (float)Math.PI / 180f;

            if (angleE < angleS)
                angleS -= (float)Math.PI * 2f;

            // define circle arc
            for (int i = 0; i <= _steps; i++)
            {
                float angle = angleS + i * angleStep;
                if (angle > angleE)
                    continue;
                if (angleE - angle < 100 * DXFGeometry.TOLERANCE)
                    angle = angleE;

                Point3D pOnCircle = centerNew + _radius * Math.Cos(angle) * this.csXaxisV + 
                                                _radius * Math.Sin(angle) * this.csYaxisV;
                points.Add(pOnCircle);
            }

            // output
            foreach(Point3D p in points)
            {
                this.Coords.Add(p);
                this.Connected.Add(true);
                this.Width.Add(_width);
            }
            int n = this.Coords.Count;
            this.Connected[n - 1] = false;

            //// debug
            //Point3D test1 = centerNew;
            //Point3D test3 = test1 + this.csXaxisV * 10;
            //Point3D test4 = test1 + this.csYaxisV * 10;
            //Point3D test5 = test1 + this.csZaxisV * 10;
            //this.Coords.Add(test1);
            //this.Connected.Add(true);
            //this.Width.Add(2f);
            //this.Coords.Add(test3);
            //this.Connected.Add(true);
            //this.Width.Add(2f);
            //this.Coords.Add(test1);
            //this.Connected.Add(true);
            //this.Width.Add(2f);
            //this.Coords.Add(test4);
            //this.Connected.Add(true);
            //this.Width.Add(2f);
            //this.Coords.Add(test5);
            //this.Connected.Add(true);
            //this.Width.Add(2f);
            //this.Coords.Add(test1);
            //this.Connected.Add(true);
            //this.Width.Add(2f);

        }
        #endregion

        // --------------------------------------------------- ELLIPSE -------------------------------------------- //

        #region ELLIPSE ELLIPSE ARC
        // angles supplied in radians (full rotation: 0 - 6.28)
        // _endMajAxis is relative to the center
        public void AddEllipseArc(Point3D _center, Vector3D _normal, float _a, float _b, Vector3D _endMajAxis,
                                 float _angleS, float _angleE, float _width, int _steps = 24)
        {
            if (_center == null || _normal == null || _endMajAxis == null ||
                                     Math.Abs(_a) < DXFGeometry.TOLERANCE || 
                                     Math.Abs(_b) < DXFGeometry.TOLERANCE || 
                                     Math.Abs(_angleS - _angleE) < DXFGeometry.TOLERANCE || _steps < 4)
                return;

            // determine the coordinate system of the ellipse arc:
            CalculatePlaneCS(_center, _normal);

            // determine the vectors along the major and minor axes of the ellipse arc
            NormalizeVector(ref _endMajAxis);
            Vector3D minAxisV = Vector3D.CrossProduct(this.csZaxisV, _endMajAxis);
            NormalizeVector(ref minAxisV);

            List<Point3D> points = new List<Point3D>();
            float angleStep = (2f * (float)Math.PI) / _steps;

            // define ellipse arc
            for (int i = 0; i <= _steps; i++)
            {
                float angle = i * angleStep;
                if (angle < _angleS || angle > _angleE)
                    continue;
                if (angle - _angleS < 100 * DXFGeometry.TOLERANCE)
                    angle = _angleS;
                if (_angleE - angle < 100 * DXFGeometry.TOLERANCE)
                    angle = _angleE;

                Point3D pOnEllipse = _center - _a * Math.Cos(angle) * _endMajAxis -
                                               _b * Math.Sin(angle) * minAxisV;
                points.Add(pOnEllipse);
            }

            // output
            foreach (Point3D p in points)
            {
                this.Coords.Add(p);
                this.Connected.Add(true);
                this.Width.Add(_width);
            }
            int n = this.Coords.Count;
            this.Connected[n - 1] = false;

        }
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // =============================================== TEXT DEFINITION ======================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public void AddText(string _text, Matrix3D _transf)
        {
            if (_text == null || _text.Count() < 1)
                return;
            this.TextContent.Add(_text);
            this.TextTransf = _transf;
        }

        public void AddText(string[] _texts, Matrix3D _transf)
        {
            if (_texts == null || _texts.Count() < 1)
                return;
            this.TextContent.AddRange(_texts);
            this.TextTransf = _transf;
        }

        // ------------------------------------------------- TEXT UTILS ------------------------------------------- //

        #region Text Utils
        public static float[] GetGeomLengthOfTextLines(string[] _textLines, float _avgCharWidth)
        {
            if (_textLines == null)
                return null;

            int n = _textLines.Count();
            float[] lengths = new float[n];
            for(int i = 0; i < n; i++)
            {
                lengths[i] = GetGeomLengthOfSingleTextLine(_textLines[i], _avgCharWidth);
            }

            return lengths;
        }

        private static float GetGeomLengthOfSingleTextLine(string _text, float _avgCharWidth)
        {
            if (_text == null || _text.Count() < 1)
                return 0f;

            float len = 0f;
            string str_rest = _text;
            string str_current = "";
            int offset = 0; // by how many characters do we advance in the next iteration

            while (str_rest.Count() > 0)
            {
                str_current = str_rest.Substring(0, 1);
                offset = 1;
                switch (str_current)
                {
                    case @"\":
                        if (str_rest.Count() >= 2)
                        {
                            str_current = str_rest.Substring(0, 2);
                            if (str_current == @"\f" || str_current == @"\H" || str_current == @"\W")
                            {
                                // style information: skip it
                                int ind_SC = str_rest.IndexOf(";");
                                if (ind_SC > -1)
                                {
                                    offset = ind_SC + 1;
                                    break;
                                }
                            }
                        }
                        if (str_rest.Count() >= 7)
                        {
                            str_current = str_rest.Substring(0, 3);
                            if (str_current == @"\U+")
                            {
                                // unicode character
                                str_current = str_rest.Substring(1, 6);
                                offset = 7;
                                len += _avgCharWidth * 1.3333f;
                                break;
                            }
                        }
                        // backslash character
                        str_current = str_rest.Substring(0, 1);
                        len += _avgCharWidth;
                        break;
                    case "%":
                        if (str_rest.Count() >= 3)
                        {
                            str_current = str_rest.Substring(0, 2);
                            if (str_current == "%%")
                            {
                                // AutoCAD specific symbols '%%P', '%%C', '%%D'
                                str_current = str_rest.Substring(0, 3);
                                offset = 3;
                                len += _avgCharWidth * 1.3333f;
                                break;
                            }
                        }
                        // percent character
                        str_current = str_rest.Substring(0, 1);
                        len += _avgCharWidth;
                        break;
                    case " ":
                        // space
                        len += _avgCharWidth * 1.1333f;
                        break;
                    default:
                        // regular characers (i.e. 'A')
                        len += _avgCharWidth * 1.3333f;
                        break;
                }
                str_rest = str_rest.Substring(offset);
            }


            return len;
        }

        public static string[] FitTextLines(string[] _textLines, float _avgCharWidth, float _maxTotalWidth)
        {
            if (_textLines == null || _avgCharWidth > _maxTotalWidth)
                return null;

            List<string> fittedLines = new List<string>();
            foreach(string text in _textLines)
            {
                List<string> fitted = FitSingleTextLine(text, _avgCharWidth, _maxTotalWidth);
                fittedLines.AddRange(fitted);
            }
            return fittedLines.ToArray();
        }

        private static List<string> FitSingleTextLine(string _text, float _avgCharWidth, float _maxTotalWidth)
        {
            if (_text == null || _text.Count() < 1 || _avgCharWidth > _maxTotalWidth)
                return null;

            float len = 0f;
            string str_rest = _text;
            string str_current = "";
            List<string> str_fitted = new List<string>();
            int offset = 0; // by how many characters do we advance in the next iteration
            int offset_total = 0;
            int offset_prevIter = 0;

            while (str_rest.Count() > 0)
            {
                len = 0f;
                offset_prevIter = offset_total;
                while (str_rest.Count() > 0 && (len + _avgCharWidth) <= _maxTotalWidth)
                {
                    str_current = str_rest.Substring(0, 1);
                    offset = 1;
                    switch (str_current)
                    {
                        case @"\":
                            if (str_rest.Count() >= 2)
                            {
                                str_current = str_rest.Substring(0, 2);
                                if (str_current == @"\f" || str_current == @"\H" || str_current == @"\W")
                                {
                                    // style information: skip it
                                    int ind_SC = str_rest.IndexOf(";");
                                    if (ind_SC > -1)
                                    {
                                        offset = ind_SC + 1;
                                        break;
                                    }
                                }
                            }
                            if (str_rest.Count() >= 7)
                            {
                                str_current = str_rest.Substring(0, 3);
                                if (str_current == @"\U+")
                                {
                                    // unicode character
                                    str_current = str_rest.Substring(1, 6);
                                    offset = 7;
                                    len += _avgCharWidth * 1.3333f;
                                    break;
                                }
                            }
                            // backslash character
                            str_current = str_rest.Substring(0, 1);
                            len += _avgCharWidth;
                            break;
                        case "%":
                            if (str_rest.Count() >= 3)
                            {
                                str_current = str_rest.Substring(0, 2);
                                if (str_current == "%%")
                                {
                                    // AutoCAD specific symbols '%%P', '%%C', '%%D'
                                    str_current = str_rest.Substring(0, 3);
                                    offset = 3;
                                    len += _avgCharWidth * 1.3333f;
                                    break;
                                }
                            }
                            // percent character
                            str_current = str_rest.Substring(0, 1);
                            len += _avgCharWidth;
                            break;
                        case " ":
                            // space
                            len += _avgCharWidth * 1.1333f;
                            break;
                        default:
                            // regular characers (i.e. 'A')
                            len += _avgCharWidth * 1.3333f;
                            break;
                    }
                    str_rest = str_rest.Substring(offset);
                    offset_total += offset;
                }
                str_fitted.Add(_text.Substring(offset_prevIter, offset_total - offset_prevIter));
            }

            return str_fitted;
        }
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // =============================================== UTILITY METHODS ======================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Calculation of AutoCAD-Style Object Coordinate System
        // implements the Arbitrary CS Algorithm in AutoCAD
        public static void CalPlaneCS(Point3D _p0, Vector3D _n, out Vector3D axisX, out Vector3D axisY, out Vector3D axisZ)
        {
            axisX = new Vector3D(0, 0, 0);
            axisY = new Vector3D(0, 0, 0);
            axisZ = new Vector3D(0, 0, 0);

            if (_p0 == null || _n == null)
                return;

            double nMagn = Math.Sqrt(_n.X * _n.X + _n.Y * _n.Y + _n.Z * _n.Z);
            if (nMagn < DXFGeometry.TOLERANCE)
                return;

            // normalize plane normal
            Vector3D normal = new Vector3D(_n.X / nMagn, _n.Y / nMagn, _n.Z / nMagn);
            float a = (float)normal.X;
            float b = (float)normal.Y;
            float c = (float)normal.Z;

            // special cases
            if (Math.Abs(a) < DXFGeometry.TOLERANCE && Math.Abs(b) < DXFGeometry.TOLERANCE)
            {
                axisX = new Vector3D(1, 0, 0);
                axisY = new Vector3D(0, 1, 0);
                axisZ = new Vector3D(0, 0, 1);
                return;

            }
            else if (Math.Abs(a) < DXFGeometry.TOLERANCE && Math.Abs(c) < DXFGeometry.TOLERANCE)
            {
                axisX = new Vector3D(1, 0, 0);
                axisY = new Vector3D(0, 0, 1);
                axisZ = new Vector3D(0, 1, 0);
                return;

            }
            else if (Math.Abs(b) < DXFGeometry.TOLERANCE && Math.Abs(c) < DXFGeometry.TOLERANCE)
            {
                axisX = new Vector3D(0, 1, 0);
                axisY = new Vector3D(0, 0, 1);
                axisZ = new Vector3D(1, 0, 0);
                return;
            }

            //// the general case
            //axisZ = normal;
            //// X-axis vector projects onto World X-axis and has length 1
            //double denomBC = Math.Sqrt(b * b + c * c);
            //axisX = new Vector3D(0, -c / denomBC, b / denomBC);
            //// Y-axis vector projects onto World Y-axis and has length 1
            //double denomAC = Math.Sqrt(a * a + c * c);
            //axisY = new Vector3D(-c / denomAC, 0, a / denomAC);

            // the general case
            axisZ = normal;
            if ((Math.Abs(axisZ.X) < (1.0 / 64.0)) && (Math.Abs(axisZ.Y) < (1.0 / 64.0)))
                axisX = Vector3D.CrossProduct(new Vector3D(0, 1, 0), axisZ);
            else
                axisX = Vector3D.CrossProduct(new Vector3D(0, 0, 1), axisZ);
            NormalizeVector(ref axisX);
            axisY = Vector3D.CrossProduct(axisZ, axisX);
            NormalizeVector(ref axisY);

        }


        private void CalculatePlaneCS(Point3D _p0, Vector3D _n)
        {
            CalPlaneCS(_p0, _n, out this.csXaxisV, out this.csYaxisV, out this.csZaxisV);
        }
        #endregion

        public static void NormalizeVector(ref Vector3D _v)
        {
            double vMagn = Math.Sqrt(_v.X * _v.X + _v.Y * _v.Y + _v.Z * _v.Z);
            if (vMagn < DXFGeometry.TOLERANCE)
                return;
            _v = new Vector3D(_v.X / vMagn, _v.Y / vMagn, _v.Z / vMagn);
        }

        public void Transform(Matrix3D _transf)
        {
            List<Point3D> coordsNew = new List<Point3D>();
            foreach(Point3D p in this.Coords)
            {
                Point3D pT = _transf.Transform(p);
                coordsNew.Add(pT);
            }
            this.Coords = new List<Point3D>(coordsNew);
        }
    }
}
