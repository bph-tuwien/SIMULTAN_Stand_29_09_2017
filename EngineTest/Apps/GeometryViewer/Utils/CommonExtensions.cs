using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.Windows.Data;
using System.ComponentModel;
using System.Globalization;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using Geometry3D = HelixToolkit.SharpDX.Wpf.Geometry3D;
using SharpDX;

namespace GeometryViewer.Utils
{
    #region HELPER CLASSES
    public class PolyLineOffsetContainer
    {
        #region PROPERTIES
        public Vector3 CentralLine_P0 { get; private set; }
        public Vector3 CentralLine_P1 { get; private set; }

        public Vector3 CentralLine_V { get; private set; }
        public Vector3 CrossSection_HRZT { get; private set; }
        public Vector3 CrossSection_VERT { get; private set; }

        public Vector3 OffsetLine_A_P0 { get; private set; }
        public Vector3 OffsetLine_A_P1 { get; private set; }
        public Vector3 OffsetLine_B_P0 { get; private set; }
        public Vector3 OffsetLine_B_P1 { get; private set; }
        public Vector3 OffsetLine_C_P0 { get; private set; }
        public Vector3 OffsetLine_C_P1 { get; private set; }
        public Vector3 OffsetLine_D_P0 { get; private set; }
        public Vector3 OffsetLine_D_P1 { get; private set; }

        public bool IsValid { get; private set; }
        public bool WasMirroredVert { get; private set; }
        public bool WasMirroredHrzt { get; private set; }
        #endregion

        #region DERIVED PROPERTIES
        // derived:
        public Point3D CentralLine_P0_ { get { return this.CentralLine_P0.ToPoint3D(); } }
        public Point3D CentralLine_P1_ { get { return this.CentralLine_P1.ToPoint3D(); } }

        public Vector3D CentralLine_V_ { get { return this.CentralLine_V.ToVector3D(); } }
        public Vector3D CrossSection_HRZT_ { get { return this.CrossSection_HRZT.ToVector3D(); } }
        public Vector3D CrossSection_VERT_ { get { return this.CrossSection_VERT.ToVector3D(); } }
        #endregion

        #region .CTOR
        private PolyLineOffsetContainer(Vector3 _p0, Vector3 _p1, Vector3 _v, Vector3 _v_hrzt, Vector3 _v_vert, 
                                       Vector3 _p0_a, Vector3 _p1_a, Vector3 _p0_b, Vector3 _p1_b, 
                                       Vector3 _p0_c, Vector3 _p1_c, Vector3 _p0_d, Vector3 _p1_d, bool _is_valid)
        {
            this.CentralLine_P0 = _p0;
            this.CentralLine_P1 = _p1;

            this.CentralLine_V = _v;
            this.CrossSection_HRZT = _v_hrzt;
            this.CrossSection_VERT = _v_vert;

            this.OffsetLine_A_P0 = _p0_a;
            this.OffsetLine_A_P1 = _p1_a;
            this.OffsetLine_B_P0 = _p0_b;
            this.OffsetLine_B_P1 = _p1_b;
            this.OffsetLine_C_P0 = _p0_c;
            this.OffsetLine_C_P1 = _p1_c;
            this.OffsetLine_D_P0 = _p0_d;
            this.OffsetLine_D_P1 = _p1_d;

            this.IsValid = _is_valid;
            this.WasMirroredVert = false;
            this.WasMirroredHrzt = false;
        }

        private PolyLineOffsetContainer(Point3D _p0, Point3D _p1, Vector3D _v, Vector3D _v_hrzt, Vector3D _v_vert,
                                       Point3D _p0_a, Point3D _p1_a, Point3D _p0_b, Point3D _p1_b,
                                       Point3D _p0_c, Point3D _p1_c, Point3D _p0_d, Point3D _p1_d, bool _is_valid)
        {
            this.CentralLine_P0 = _p0.ToVector3();
            this.CentralLine_P1 = _p1.ToVector3();

            this.CentralLine_V = _v.ToVector3();
            this.CrossSection_HRZT = _v_hrzt.ToVector3();
            this.CrossSection_VERT = _v_vert.ToVector3();

            this.OffsetLine_A_P0 = _p0_a.ToVector3();
            this.OffsetLine_A_P1 = _p1_a.ToVector3();
            this.OffsetLine_B_P0 = _p0_b.ToVector3();
            this.OffsetLine_B_P1 = _p1_b.ToVector3();
            this.OffsetLine_C_P0 = _p0_c.ToVector3();
            this.OffsetLine_C_P1 = _p1_c.ToVector3();
            this.OffsetLine_D_P0 = _p0_d.ToVector3();
            this.OffsetLine_D_P1 = _p1_d.ToVector3();

            this.IsValid = _is_valid;
            this.WasMirroredVert = false;
            this.WasMirroredHrzt = false;
        }

        #endregion

        #region METHODS: Switch rotation A-->D

        private void Mirror(bool _vertical)
        {
            Vector3 p0_a = this.OffsetLine_A_P0;
            Vector3 p1_a = this.OffsetLine_A_P1;
            Vector3 p0_b = this.OffsetLine_B_P0;
            Vector3 p1_b = this.OffsetLine_B_P1;
            Vector3 p0_c = this.OffsetLine_C_P0;
            Vector3 p1_c = this.OffsetLine_C_P1;
            Vector3 p0_d = this.OffsetLine_D_P0;
            Vector3 p1_d = this.OffsetLine_D_P1;

            if (_vertical)
            {
                this.WasMirroredVert = !this.WasMirroredVert;
                // a <-> b
                // c <-> d
                this.OffsetLine_A_P0 = p0_b;
                this.OffsetLine_A_P1 = p1_b;
                this.OffsetLine_B_P0 = p0_a;
                this.OffsetLine_B_P1 = p1_a;
                this.OffsetLine_C_P0 = p0_d;
                this.OffsetLine_C_P1 = p1_d;
                this.OffsetLine_D_P0 = p0_c;
                this.OffsetLine_D_P1 = p1_c;
            }
            else
            {
                this.WasMirroredHrzt = !this.WasMirroredHrzt;
                // a <-> d
                // b <-> c
                this.OffsetLine_A_P0 = p0_d;
                this.OffsetLine_A_P1 = p1_d;
                this.OffsetLine_B_P0 = p0_c;
                this.OffsetLine_B_P1 = p1_c;
                this.OffsetLine_C_P0 = p0_b;
                this.OffsetLine_C_P1 = p1_b;
                this.OffsetLine_D_P0 = p0_a;
                this.OffsetLine_D_P1 = p1_a;
            }

        }


        #endregion

        #region STATIC .CTOR CALLS
        public static PolyLineOffsetContainer GetInvalid(Vector3 _p0, Vector3 _p1, Vector3 _v)
        {
            return new PolyLineOffsetContainer(_p0, _p1, _v, Vector3.Zero, Vector3.Zero,
                                                Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero,
                                                Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, false);
        }

        public static PolyLineOffsetContainer GetInvalid(Point3D _p0, Point3D _p1, Vector3D _v)
        {
            return new PolyLineOffsetContainer(_p0.ToVector3(), _p1.ToVector3(), _v.ToVector3(), Vector3.Zero, Vector3.Zero,
                                                Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero,
                                                Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, false);
        }
        #endregion

        #region STATIC METHODS: Create

        public static PolyLineOffsetContainer CreateOutOf(Point3D _central_line_p0, Point3D _central_line_p1, Vector3D _up_vect, double _hrz_size, double _vert_size)
        {
            if (_central_line_p0 == null || _central_line_p1 == null || _up_vect == null) return null;

            Vector3D segmentV = _central_line_p1 - _central_line_p0;
            if (segmentV.LengthSquared < Utils.CommonExtensions.LINEDISTCALC_TOLERANCE)
                return null; // segment too small
            
            segmentV.Normalize();

            // get 'horizontal' vector
            Vector3D segment_HRZT = Vector3D.CrossProduct(segmentV, _up_vect);
            if (segment_HRZT.LengthSquared < Utils.CommonExtensions.LINEDISTCALC_TOLERANCE)
                return PolyLineOffsetContainer.GetInvalid(_central_line_p0, _central_line_p1, segmentV);
            segment_HRZT.Normalize();

            // get 'vertical' vector
            Vector3D segment_VERT = Vector3D.CrossProduct(segmentV, segment_HRZT);
            if (segment_VERT.LengthSquared < Utils.CommonExtensions.LINEDISTCALC_TOLERANCE)           
                return PolyLineOffsetContainer.GetInvalid(_central_line_p0, _central_line_p1, segmentV);

            segment_VERT.Normalize();

            // calculate offset points
            Point3D p0_a = _central_line_p0 + segment_HRZT * _hrz_size * 0.5 + segment_VERT * _vert_size * 0.5;
            Point3D p0_b = _central_line_p0 + segment_HRZT * _hrz_size * 0.5 - segment_VERT * _vert_size * 0.5;
            Point3D p0_c = _central_line_p0 - segment_HRZT * _hrz_size * 0.5 - segment_VERT * _vert_size * 0.5;
            Point3D p0_d = _central_line_p0 - segment_HRZT * _hrz_size * 0.5 + segment_VERT * _vert_size * 0.5;

            Point3D p1_a = _central_line_p1 + segment_HRZT * _hrz_size * 0.5 + segment_VERT * _vert_size * 0.5;
            Point3D p1_b = _central_line_p1 + segment_HRZT * _hrz_size * 0.5 - segment_VERT * _vert_size * 0.5;
            Point3D p1_c = _central_line_p1 - segment_HRZT * _hrz_size * 0.5 - segment_VERT * _vert_size * 0.5;
            Point3D p1_d = _central_line_p1 - segment_HRZT * _hrz_size * 0.5 + segment_VERT * _vert_size * 0.5;

            return new PolyLineOffsetContainer(_central_line_p0, _central_line_p1, segmentV, segment_HRZT, segment_VERT,
                                                     p0_a, p1_a, p0_b, p1_b, p0_c, p1_c, p0_d, p1_d, true);
        }

        #endregion

        #region STATIC METHODS: Handle Invalid Entries in a Sequence

        public static bool HandleInvalidEntries(ref List<PolyLineOffsetContainer> _sequence, double _hrzt_size, double _vert_size)
        {
            if (_sequence == null) return false;
            int nrSegments = _sequence.Count;
            if (nrSegments == 0) return false;

            Vector3D upV = new Vector3D(0, 1, 0); // default the Y-axis
            bool success = true;
            for (int i = 0; i < nrSegments; i++)
            {
                if (_sequence[i].IsValid)
                    continue;

                // try to adjust the up-Vector
                Point3D p0 = _sequence[i].CentralLine_P0_;
                Point3D p1 = _sequence[i].CentralLine_P1_;

                // FIRST ENTRY
                if (i == 0)
                {
                    Utils.PolyLineOffsetContainer next_valid = _sequence.FirstOrDefault(x => x.IsValid);
                    if (next_valid == null)
                    {
                        // just take the X-ACHIS as the up-Vector
                        upV = new Vector3D(1, 0, 0);
                        _sequence[i] = Utils.PolyLineOffsetContainer.CreateOutOf(p0, p1, upV, _hrzt_size, _vert_size);
                        // if not successful, try the Z-AXIS
                        if (!_sequence[i].IsValid)
                        {
                            upV = new Vector3D(0, 0, 1);
                            _sequence[i] = Utils.PolyLineOffsetContainer.CreateOutOf(p0, p1, upV, _hrzt_size, _vert_size);
                            if (!_sequence[i].IsValid)
                            {
                                // this should not happen
                                success = false;
                                continue;
                            }
                        }
                    }
                    else
                    {
                        // take the next valid UP VECTOR as an up vector
                        _sequence[i] = Utils.PolyLineOffsetContainer.CreateOutOf(p0, p1, next_valid.CrossSection_VERT_, _hrzt_size, _vert_size);
                        if (!_sequence[i].IsValid)
                        {
                            // take the next valid SEGMENT VECTOR as an up vector
                            _sequence[i] = Utils.PolyLineOffsetContainer.CreateOutOf(p0, p1, next_valid.CentralLine_V_, _hrzt_size, _vert_size);
                            if (!_sequence[i].IsValid)
                            {
                                // this should not happen
                                success = false;
                                continue;
                            }
                        }
                    }
                }

                // LAST ENTRY
                if (i == nrSegments - 1)
                {
                    Utils.PolyLineOffsetContainer prev_valid = _sequence.LastOrDefault(x => x.IsValid);
                    if (prev_valid == null)
                    {
                        // this should not happen
                        success = false;
                        continue;
                    }
                    else
                    {
                        // take the prev valid UP VECTOR as an up vector
                        _sequence[i] = Utils.PolyLineOffsetContainer.CreateOutOf(p0, p1, prev_valid.CrossSection_VERT_, _hrzt_size, _vert_size);
                        if (!_sequence[i].IsValid)
                        {
                            // take the prev valid SEGMENT VECTOR as an up vector
                            _sequence[i] = Utils.PolyLineOffsetContainer.CreateOutOf(p0, p1, prev_valid.CentralLine_V_, _hrzt_size, _vert_size);
                            if (!_sequence[i].IsValid)
                            {
                                // this should not happen
                                success = false;
                                continue;
                            }
                        }
                    }
                }

                // OTHER ENTRIES
                List<Utils.PolyLineOffsetContainer> offset_infos_prev = _sequence.Take(i).ToList();
                Utils.PolyLineOffsetContainer prev_i_valid = offset_infos_prev.LastOrDefault(x => x.IsValid);
                if (prev_i_valid == null)
                {
                    List<Utils.PolyLineOffsetContainer> offset_infos_next = _sequence.Skip(i + 1).ToList();
                    Utils.PolyLineOffsetContainer next_i_valid = offset_infos_next.FirstOrDefault(x => x.IsValid);
                    if (next_i_valid == null)
                    {
                        // this should not happen
                        success = false;
                        continue;
                    }
                    else
                    {
                        // take the next valid UP VECTOR as an up vector
                        _sequence[i] = Utils.PolyLineOffsetContainer.CreateOutOf(p0, p1, next_i_valid.CrossSection_VERT_, _hrzt_size, _vert_size);
                        if (!_sequence[i].IsValid)
                        {
                            // take the next valid SEGMENT VECTOR as an up vector
                            _sequence[i] = Utils.PolyLineOffsetContainer.CreateOutOf(p0, p1, next_i_valid.CentralLine_V_, _hrzt_size, _vert_size);
                            if (!_sequence[i].IsValid)
                            {
                                // this should not happen
                                success = false;
                                continue;
                            }
                        }
                    }
                }
                else
                {
                    // take the prev valid UP VECTOR as an up vector
                    _sequence[i] = Utils.PolyLineOffsetContainer.CreateOutOf(p0, p1, prev_i_valid.CrossSection_VERT_, _hrzt_size, _vert_size);
                    if (!_sequence[i].IsValid)
                    {
                        // take the prev valid SEGMENT VECTOR as an up vector
                        _sequence[i] = Utils.PolyLineOffsetContainer.CreateOutOf(p0, p1, prev_i_valid.CentralLine_V_, _hrzt_size, _vert_size);
                        if (!_sequence[i].IsValid)
                        {
                            // this should not happen
                            success = false;
                            continue;
                        }
                    }
                }

            }

            return success;
        }


        #endregion

        #region STATIC METHODS: Connect a Sequence

        private static void IntersectContainers(PolyLineOffsetContainer c_i, PolyLineOffsetContainer c_i_1, 
                                                out Vector3 connect_A, out Vector3 connect_B, out Vector3 connect_C, out Vector3 connect_D,
                                                out bool success_A, out bool success_B, out bool success_C, out bool success_D)
        {
            // intersection points
            connect_A = Vector3.Zero; connect_B = Vector3.Zero; connect_C = Vector3.Zero; connect_D = Vector3.Zero;
            // intersection results
            success_A = false; success_B = false; success_C = false; success_D = false;

            // intersect the A offsets
            Vector3 prev_A_P1_connect, next_A_P0_connect;
            success_A = CommonExtensions.LineToLineShortestLine3D(c_i.OffsetLine_A_P0, c_i.OffsetLine_A_P1,
                                                                  c_i_1.OffsetLine_A_P0, c_i_1.OffsetLine_A_P1,
                                                                  out prev_A_P1_connect, out next_A_P0_connect);
            if (success_A)
            {
                connect_A = new Vector3(prev_A_P1_connect.X * 0.5f + next_A_P0_connect.X * 0.5f,
                                        prev_A_P1_connect.Y * 0.5f + next_A_P0_connect.Y * 0.5f,
                                        prev_A_P1_connect.Z * 0.5f + next_A_P0_connect.Z * 0.5f);
            }
            else
            {
                success_A = CommonExtensions.IntersectLinesInSamePlane(c_i.OffsetLine_A_P0, c_i.OffsetLine_A_P1,
                                                                       c_i_1.OffsetLine_A_P0, c_i_1.OffsetLine_A_P1,
                                                                       out connect_A);
            }

            // intersect the B offsets
            Vector3 prev_B_P1_connect, next_B_P0_connect;
            success_B = CommonExtensions.LineToLineShortestLine3D(c_i.OffsetLine_B_P0, c_i.OffsetLine_B_P1,
                                                                  c_i_1.OffsetLine_B_P0, c_i_1.OffsetLine_B_P1,
                                                                  out prev_B_P1_connect, out next_B_P0_connect);
            if (success_B)
            {
                connect_B = new Vector3(prev_B_P1_connect.X * 0.5f + next_B_P0_connect.X * 0.5f,
                                        prev_B_P1_connect.Y * 0.5f + next_B_P0_connect.Y * 0.5f,
                                        prev_B_P1_connect.Z * 0.5f + next_B_P0_connect.Z * 0.5f);
            }
            else
            {
                success_B = CommonExtensions.IntersectLinesInSamePlane(c_i.OffsetLine_B_P0, c_i.OffsetLine_B_P1,
                                                                       c_i_1.OffsetLine_B_P0, c_i_1.OffsetLine_B_P1,
                                                                       out connect_B);
            }

            // intersect the C offsets
            Vector3 prev_C_P1_connect, next_C_P0_connect;
            success_C = CommonExtensions.LineToLineShortestLine3D(c_i.OffsetLine_C_P0, c_i.OffsetLine_C_P1,
                                                                  c_i_1.OffsetLine_C_P0, c_i_1.OffsetLine_C_P1,
                                                                  out prev_C_P1_connect, out next_C_P0_connect);
            if (success_C)
            {
                connect_C = new Vector3(prev_C_P1_connect.X * 0.5f + next_C_P0_connect.X * 0.5f,
                                        prev_C_P1_connect.Y * 0.5f + next_C_P0_connect.Y * 0.5f,
                                        prev_C_P1_connect.Z * 0.5f + next_C_P0_connect.Z * 0.5f);
            }
            else
            {
                success_C = CommonExtensions.IntersectLinesInSamePlane(c_i.OffsetLine_C_P0, c_i.OffsetLine_C_P1,
                                                                       c_i_1.OffsetLine_C_P0, c_i_1.OffsetLine_C_P1,
                                                                       out connect_C);
            }

            // intersect the D offsets
            Vector3 prev_D_P1_connect, next_D_P0_connect;
            success_D = CommonExtensions.LineToLineShortestLine3D(c_i.OffsetLine_D_P0, c_i.OffsetLine_D_P1,
                                                                  c_i_1.OffsetLine_D_P0, c_i_1.OffsetLine_D_P1,
                                                                  out prev_D_P1_connect, out next_D_P0_connect);
            if (success_D)
            {
                connect_D = new Vector3(prev_D_P1_connect.X * 0.5f + next_D_P0_connect.X * 0.5f,
                                        prev_D_P1_connect.Y * 0.5f + next_D_P0_connect.Y * 0.5f,
                                        prev_D_P1_connect.Z * 0.5f + next_D_P0_connect.Z * 0.5f);
            }
            else
            {
                success_D = CommonExtensions.IntersectLinesInSamePlane(c_i.OffsetLine_D_P0, c_i.OffsetLine_D_P1,
                                                                       c_i_1.OffsetLine_D_P0, c_i_1.OffsetLine_D_P1,
                                                                       out connect_D);
            }
        }


        private static void CheckWellFormednessOfIntersection(Vector3 _connect_A, Vector3 _connect_B, Vector3 _connect_C, Vector3 _connect_D,
                                                              out bool mirror_vert, out bool mirror_hrzt)
        {
            mirror_vert = false;
            mirror_hrzt = false;
            if ((_connect_A - _connect_B).LengthSquared() < CommonExtensions.GENERAL_CALC_TOLERANCE)
            {
                mirror_vert = true;
            }
            if ((_connect_A - _connect_C).LengthSquared() < CommonExtensions.GENERAL_CALC_TOLERANCE)
            {
                mirror_hrzt = true;
                mirror_vert = true;
            }
            if ((_connect_A - _connect_D).LengthSquared() < CommonExtensions.GENERAL_CALC_TOLERANCE)
            {
                mirror_hrzt = true;
            }
            if ((_connect_B - _connect_C).LengthSquared() < CommonExtensions.GENERAL_CALC_TOLERANCE)
            {
                mirror_hrzt = true;
            }
            if ((_connect_B - _connect_D).LengthSquared() < CommonExtensions.GENERAL_CALC_TOLERANCE)
            {
                mirror_hrzt = true;
                mirror_vert = true;
            }
            if ((_connect_C - _connect_D).LengthSquared() < CommonExtensions.GENERAL_CALC_TOLERANCE)
            {
                mirror_vert = true;
            }
        }


        public static bool ConnectSequence(ref List<PolyLineOffsetContainer> _sequence)
        {
            if (_sequence == null) return false;
            int nrSegments = _sequence.Count;
            if (nrSegments == 0) return false;

            bool success = true;
            for(int i = 0; i < nrSegments - 1; i++)
            {
                // intersection points
                Vector3 connect_A = Vector3.Zero, connect_B = Vector3.Zero, connect_C = Vector3.Zero, connect_D = Vector3.Zero;
                // intersection results
                bool success_A = false, success_B = false, success_C = false, success_D = false;

                // intersect the offsets
                PolyLineOffsetContainer.IntersectContainers(_sequence[i], _sequence[i + 1], 
                                                out connect_A, out connect_B, out connect_C, out connect_D,
                                                out success_A, out success_B, out success_C, out success_D);


                // check if the intersection is well defined
                bool mirror_vert = false;
                bool mirror_hrzt = false;
                PolyLineOffsetContainer.CheckWellFormednessOfIntersection(connect_A, connect_B, connect_C, connect_D,
                                                              out mirror_vert, out mirror_hrzt);

                // check for self-intersection
                Vector3 collision;
                bool self_inters_A0B1 = CommonExtensions.LineWLineCollision3D(_sequence[i].OffsetLine_A_P0, connect_A, 
                                                                              _sequence[i + 1].OffsetLine_B_P1, connect_B, 0.01, out collision);
                bool self_inters_B0A1 = CommonExtensions.LineWLineCollision3D(_sequence[i].OffsetLine_B_P0, connect_B,
                                                                              _sequence[i + 1].OffsetLine_A_P1, connect_A, 0.01, out collision);
                mirror_vert |= (self_inters_A0B1 || self_inters_B0A1);

                bool self_inters_A0D1 = CommonExtensions.LineWLineCollision3D(_sequence[i].OffsetLine_A_P0, connect_A,
                                                                              _sequence[i + 1].OffsetLine_D_P1, connect_D, 0.01, out collision);
                bool self_inters_D0A1 = CommonExtensions.LineWLineCollision3D(_sequence[i].OffsetLine_D_P0, connect_D,
                                                                              _sequence[i + 1].OffsetLine_A_P1, connect_A, 0.01, out collision);
                mirror_hrzt |= (self_inters_A0D1 || self_inters_D0A1);

                // if not, mirror the next segment and try again
                if (mirror_hrzt || mirror_vert)
                {
                    if (mirror_hrzt)
                        _sequence[i + 1].Mirror(false);
                    if (mirror_vert)
                        _sequence[i + 1].Mirror(true);

                    PolyLineOffsetContainer.IntersectContainers(_sequence[i], _sequence[i + 1],
                                                out connect_A, out connect_B, out connect_C, out connect_D,
                                                out success_A, out success_B, out success_C, out success_D);
                    // check if the intersection is well defined
                    PolyLineOffsetContainer.CheckWellFormednessOfIntersection(connect_A, connect_B, connect_C, connect_D,
                                                              out mirror_vert, out mirror_hrzt);
                    if (mirror_hrzt || mirror_vert)
                    {
                        // the mirroring did not produce better results -> reset and discard connection points
                        if (_sequence[i + 1].WasMirroredHrzt)
                            _sequence[i + 1].Mirror(false);
                        if (_sequence[i + 1].WasMirroredVert)
                            _sequence[i + 1].Mirror(true);
                    }
                }
                
                // if all checks are ok -> assign connecting points
                if (!mirror_hrzt && !mirror_vert)
                {
                    if (success_A)
                    {
                        _sequence[i].OffsetLine_A_P1 = connect_A;
                        _sequence[i + 1].OffsetLine_A_P0 = connect_A;
                    }
                    if (success_B)
                    {
                        _sequence[i].OffsetLine_B_P1 = connect_B;
                        _sequence[i + 1].OffsetLine_B_P0 = connect_B;
                    }
                    if (success_C)
                    {
                        _sequence[i].OffsetLine_C_P1 = connect_C;
                        _sequence[i + 1].OffsetLine_C_P0 = connect_C;
                    }
                    if (success_D)
                    {
                        _sequence[i].OffsetLine_D_P1 = connect_D;
                        _sequence[i + 1].OffsetLine_D_P0 = connect_D;
                    }
                }


                success &= success_A && success_B && success_C && success_D && !mirror_hrzt && !mirror_vert;

            }

            return success;
        }


        #endregion
    }
    #endregion

    public class CommonExtensions
    {
        public const double LINEDISTCALC_TOLERANCE = 0.0001;
        public const double GENERAL_CALC_TOLERANCE = 0.0001;

        // ---------------------------------------- GEOMETRY3D.LINE FUNCTIONS ------------------------------------- //

        #region GEOMETRY3D.LINE
        public static bool AreEqual(Geometry3D.Line _l1, Geometry3D.Line _l2)
        {
            return ((_l1.P0 == _l2.P0 && _l1.P1 == _l2.P1) || (_l1.P0 == _l2.P1 && _l1.P1 == _l2.P0));
        }

        public static bool AreNotEqual(Geometry3D.Line _l1, Geometry3D.Line _l2)
        {
            return ((_l1.P0 != _l2.P0 || _l1.P1 != _l2.P1) && (_l1.P0 != _l2.P1 || _l1.P1 != _l2.P0));
        }
        #endregion

        // ---------------------------------------- SHARPDX.VECTOR3 FUNCTIONS ------------------------------------- //

        #region SharpDX.Vector3 Distance and Collision Functions

        /// <summary>
        /// Function determines the shortest distance btw. 2 lines in 3D.
        /// see: http://paulbourke.net/geometry/pointlineplane/
        /// </summary>
        /// <param name="_p1">first 3D point on line A</param>
        /// <param name="_p2">second 3D point on line A</param>
        /// <param name="_p3">first 3D point on line B</param>
        /// <param name="_p4">second 3D point on line B</param>
        /// <param name="prA">intersection of line A and the shortest segment connecting line A and B</param>
        /// <param name="prB">intersection of line B and the shortest segment connecting line A and B</param>
        /// <returns>true if the calculation is possible, false otherwise (i.e. lines not well-defined)</returns>
        public static bool LineToLineShortestLine3D(    Vector3 _p1, Vector3 _p2, Vector3 _p3, Vector3 _p4, 
                                                    out Vector3 prA, out Vector3 prB)
        {
            // on the shortest line connecting line 1 (defined by _p1 and _p2)
            // and line 2 (defined by _p3 and _p4): prA lies on line 1, prB lies on line 2
            prA = Vector3.Zero;
            prB = Vector3.Zero;

            Vector3 v21 = _p2 - _p1;
            Vector3 v13 = _p1 - _p3;
            Vector3 v43 = _p4 - _p3;

            // stop if the lines are not well defined (i.e. definingpoints too close to each other)
            if (v21.LengthSquared() < LINEDISTCALC_TOLERANCE || v43.LengthSquared() < LINEDISTCALC_TOLERANCE)
                return false;

            double d1343 = v13.X * (double)v43.X + v13.Y * (double)v43.Y + v13.Z * (double)v43.Z;
            double d4321 = v43.X * (double)v21.X + v43.Y * (double)v21.Y + v43.Z * (double)v21.Z;
            double d1321 = v13.X * (double)v21.X + v13.Y * (double)v21.Y + v13.Z * (double)v21.Z;
            double d4343 = v43.X * (double)v43.X + v43.Y * (double)v43.Y + v43.Z * (double)v43.Z;
            double d2121 = v21.X * (double)v21.X + v21.Y * (double)v21.Y + v21.Z * (double)v21.Z;

            double denom = d2121 * d4343 - d4321 * d4321;
            if (Math.Abs(denom) < LINEDISTCALC_TOLERANCE)
                return false;

            double numer = d1343 * d4321 - d1321 * d4343;

            double mua = numer / denom;
            double mub = (d1343 + d4321 * (mua)) / d4343;

            prA.X = (float)(_p1.X + mua * v21.X);
            prA.Y = (float)(_p1.Y + mua * v21.Y);
            prA.Z = (float)(_p1.Z + mua * v21.Z);
            prB.X = (float)(_p3.X + mub * v43.X);
            prB.Y = (float)(_p3.Y + mub * v43.Y);
            prB.Z = (float)(_p3.Z + mub * v43.Z);

            return true;
        }

        public static double LineToLineDist3D(Vector3 _p1, Vector3 _p2, Vector3 _p3, Vector3 _p4)
        {
            // well-formedness check
            Vector3 v12 = _p1 - _p2;
            Vector3 v34 = _p3 - _p4;
            if (v12.LengthSquared() < CommonExtensions.GENERAL_CALC_TOLERANCE ||
                v34.LengthSquared() < CommonExtensions.GENERAL_CALC_TOLERANCE)
                return Double.NaN;

            // parallel check
            v12.Normalize();
            v34.Normalize();
            float cos = Vector3.Dot(v12, v34);
            if (Math.Abs(cos) > 1f - CommonExtensions.GENERAL_CALC_TOLERANCE)
            {
                // parallel lines -> project
                Vector3 p1_pr = CommonExtensions.NormalProject(_p1, _p3, _p4);
                Vector3 vDist = p1_pr - _p1;
                return vDist.Length();
            }

            // well-formed non-parallel lines
            Vector3 prA, prB;
            bool success = LineToLineShortestLine3D(_p1, _p2, _p3, _p4, out prA, out prB);
            if (success)
                return Vector3.Distance(prA, prB);
            else               
                return Double.NaN;
                
        }

        public static bool LineWLineCollision3D(Vector3 _p1, Vector3 _p2, Vector3 _p3, Vector3 _p4, 
                                                double _tolerance, out Vector3 _colPos)
        {
            Vector3 prA, prB;
            bool success = LineToLineShortestLine3D(_p1, _p2, _p3, _p4, out prA, out prB);
            if (success)
            {
                var dAB = Vector3.DistanceSquared(prA, prB);
                if (dAB < _tolerance)
                {
                    var d12 = Vector3.DistanceSquared(_p1, _p2);
                    var d1A = Vector3.DistanceSquared(_p1, prA);
                    var d2A = Vector3.DistanceSquared(_p2, prA);
                    if (_tolerance < d1A && d1A < d12 && _tolerance < d2A && d2A < d12)
                    {
                        var d34 = Vector3.DistanceSquared(_p3, _p4);
                        var d3B = Vector3.DistanceSquared(_p3, prB);
                        var d4B = Vector3.DistanceSquared(_p4, prB);
                        if (_tolerance < d3B && d3B < d34 && _tolerance < d4B && d4B < d34)
                        {
                            _colPos = prA;
                            return true;
                        }
                    }
                }

            }
            _colPos = Vector3.Zero;
            return false;
        }

        public static bool LineWLineCollision3D_InclAtEnds(Vector3 _p1, Vector3 _p2, Vector3 _p3, Vector3 _p4,
                                                double _tolerance, out Vector3 _colPos)
        {
            Vector3 prA, prB;
            bool success = LineToLineShortestLine3D(_p1, _p2, _p3, _p4, out prA, out prB);
            if (success)
            {
                var dAB = Vector3.DistanceSquared(prA, prB);
                if (dAB < _tolerance)
                {
                    var d12 = Vector3.DistanceSquared(_p1, _p2);
                    var d1A = Vector3.DistanceSquared(_p1, prA);
                    var d2A = Vector3.DistanceSquared(_p2, prA);
                    if (0 <= d1A && d1A <= d12 && 0 <= d2A && d2A <= d12)
                    {
                        var d34 = Vector3.DistanceSquared(_p3, _p4);
                        var d3B = Vector3.DistanceSquared(_p3, prB);
                        var d4B = Vector3.DistanceSquared(_p4, prB);
                        if (0 <= d3B && d3B <= d34 && 0 <= d4B && d4B <= d34)
                        {
                            _colPos = prA;
                            return true;
                        }
                    }
                }

            }
            _colPos = Vector3.Zero;
            return false;
        }

        #endregion

        #region SharpDX.Vector3 Alignment Functions
        public static SharpDX.Matrix CalcAlignmentTransform(Vector3 a, Vector3 b)
        {
            // see file C:\_TU\Code-Test\_theory\RotationMatrix.docx
            Vector3 target = b - a;
            float targetL = target.Length();
            target.Normalize();
            Vector3 source = new Vector3(1f, 0f, 0f);
            Vector3 v = Vector3.Cross(source, target);
            float s = v.Length();
            float c = Vector3.Dot(source, target);

            float[] Mvx_array = new float[] {    0, -v[2],  v[1], 0, 
                                              v[2],     0, -v[0], 0,
                                             -v[1],  v[0],     0, 0,
                                                  0,    0,     0, 0};
            Matrix Mvx = new Matrix(Mvx_array);
            Mvx.Transpose();
            Matrix R;
            if (s != 0)
                R = Matrix.Identity + Mvx + ((1 - c) / (s * s)) * Mvx * Mvx;
            else
            {
                R = Matrix.Identity;
                if (c < 0)
                    R = Matrix.RotationZ((float)Math.PI);
            }
            //Matrix Sc = Matrix.Scaling(targetL, 1f, 1f);
            Matrix Sc = Matrix.Scaling(targetL);
            Matrix L = Sc * R * Matrix.Translation(a);

            return L;
        }
        #endregion

        #region SharpDX.Vector3 Projection Functions

        public static Vector3 NormalProject(Vector3 _p, Vector3 _q0, Vector3 _q1)
        {
            Vector3 v0 = _q1 - _q0;
            Vector3 v1 = _p - _q0;

            Vector3 e0 = v0.Normalized();
            Vector3 e1 = v1.Normalized();

            var test = Vector3.Dot(e0, e1);
            if (v0.Length() < GENERAL_CALC_TOLERANCE || Math.Abs(Vector3.Dot(e0, e1)) > 0.9999)
            {
                return _p;
            }

            // project v1 onto v0
            Vector3 pPr = _q0 + Vector3.Dot(e0, e1) * v1.Length() * e0;
            return pPr;
        }

        #endregion

        #region SharpDX.Vector3 object-aligned SIZE of polygon

        public static void GetObjAlignedSizeOf(List<Vector3> _polygon, out double width, out double height)
        {
            width = 0.0;
            height = 0.0;

            if (_polygon == null) return;
            if (_polygon.Count < 2) return;
            if (_polygon.Count < 3)
            {
                width = Vector3.Distance(_polygon[0], _polygon[1]);
            }

            // determine the longest side
            int ind_start = -1;
            int nrP = _polygon.Count;
            double len_max = 0.0;
            for (int i = 0; i < nrP; i++)
            {
                double len = Vector3.Distance(_polygon[i], _polygon[(i + 1) % nrP]);
                if (len > len_max)
                {
                    len_max = len;
                    ind_start = i;
                }
            }

            // project all other points onto the longest side
            Vector3 q0 = _polygon[ind_start];
            Vector3 q1 = _polygon[(ind_start + 1) % nrP];
            List<Vector3> p_to_project = _polygon.Where((x, i) => i != ind_start && i != ((ind_start + 1) % nrP)).ToList();
            List<Vector3> p_projected = new List<Vector3>();
            
            foreach(Vector3 p in p_to_project)
            {
                Vector3 pPr = CommonExtensions.NormalProject(p, q0, q1);
                p_projected.Add(pPr);
                
                // get the maximum projection height -> it is the height of the polygon
                double local_height = Vector3.Distance(p, pPr);
                if (local_height > height)
                    height = local_height;
            }

            // determine the width from the projection points
            List<Vector3> all_projections = new List<Vector3>(p_projected);
            all_projections.Add(q0);
            all_projections.Add(q1);

            width = CommonExtensions.GetMaxLenOfPointsOnLine(all_projections);

        }


        private static float GetMaxLenOfPointsOnLine(List<Vector3> _line)
        {
            float max_len = 0f;
            
            float minX = _line.Min(v => v.X);
            float maxX = _line.Max(v => v.X);
            if (Math.Abs(maxX - minX) < GENERAL_CALC_TOLERANCE)
            {
                float minY = _line.Min(v => v.Y);
                float maxY = _line.Max(v => v.Y);
                if (Math.Abs(maxY - minY) < GENERAL_CALC_TOLERANCE)
                {
                    float minZ = _line.Min(v => v.Z);
                    float maxZ = _line.Max(v => v.Z);
                    if (Math.Abs(maxZ - minZ) < GENERAL_CALC_TOLERANCE)
                    {
                        // degenerate line
                        max_len = 0f;
                    }
                    else
                    {
                        int ind_minZ = _line.FindIndex(v => v.Z == minZ);
                        int ind_maxZ = _line.FindIndex(v => v.Z == maxZ);
                        if (ind_maxZ > 0 && ind_maxZ > 0)
                            max_len = Vector3.Distance(_line[ind_minZ], _line[ind_maxZ]);
                        else
                            max_len = 0f;
                    }
                }
                else
                {
                    int ind_minY = _line.FindIndex(v => v.Y == minY);
                    int ind_maxY = _line.FindIndex(v => v.Y == maxY);
                    if (ind_maxY > 0 && ind_maxY > 0)
                        max_len = Vector3.Distance(_line[ind_minY], _line[ind_maxY]);
                    else
                        max_len = 0f;
                }
            }
            else
            {
                int ind_minX = _line.FindIndex(v => v.X == minX);
                int ind_maxX = _line.FindIndex(v => v.X == maxX);
                if (ind_maxX > 0 && ind_maxX > 0)
                    max_len = Vector3.Distance(_line[ind_minX], _line[ind_maxX]);
                else
                    max_len = 0f;
            }

            return max_len;
        }

        #endregion

        #region SharpDX.Vector3 Line Intersection in a plane Functions

        /// <summary>
        /// <para>To be used if the shortest distance in 3d between the lines was unsuccessful.</para>
        /// </summary>
        /// <param name="_p1">first 3D point on line A</param>
        /// <param name="_p2">second 3D point on line A</param>
        /// <param name="_p3">first 3D point on line B</param>
        /// <param name="_p4">second 3D point on line B</param>
        /// <param name="inters">the intersection point</param>
        /// <returns></returns>
        public static bool IntersectLinesInSamePlane(Vector3 _p1, Vector3 _p2, Vector3 _p3, Vector3 _p4, 
                                                    out Vector3 inters)
        {
            inters = Vector3.Zero;

            Vector3 v21 = _p2 - _p1;
            Vector3 v43 = _p4 - _p3;

            // stop if the lines are not well defined (i.e. definingpoints too close to each other)
            if (v21.LengthSquared() < LINEDISTCALC_TOLERANCE || v43.LengthSquared() < LINEDISTCALC_TOLERANCE)
                return false;

            v21.Normalize();
            v43.Normalize();

            // calculate the denominator using different components and choose the one that is not ZERO
            double denom_YX = v43.Y * v21.X - v43.X * v21.Y;
            double denom_XZ = v43.X * v21.Z - v43.Z * v21.X;
            double denom_ZY = v43.Z * v21.Y - v43.Y * v21.Z;
            if (Math.Abs(denom_YX) > Math.Abs(denom_XZ) && Math.Abs(denom_YX) > Math.Abs(denom_ZY))
            {
                float mu = (float)(((_p3.X - _p1.X) * v21.Y - (_p3.Y - _p1.Y) * v21.X) / denom_YX);
                inters = new Vector3(_p3.X + v43.X * mu, _p3.Y + v43.Y * mu, _p3.Z + v43.Z * mu);
                return true;
            }
            else if (Math.Abs(denom_XZ) > Math.Abs(denom_ZY) && Math.Abs(denom_XZ) > Math.Abs(denom_YX))
            {
                float mu = (float)(((_p3.Z - _p1.Z) * v21.X - (_p3.X - _p1.X) * v21.Z) / denom_XZ);
                inters = new Vector3(_p3.X + v43.X * mu, _p3.Y + v43.Y * mu, _p3.Z + v43.Z * mu);
                return true;
            }
            else if (Math.Abs(denom_ZY) >= CommonExtensions.GENERAL_CALC_TOLERANCE)
            {
                float mu = (float)(((_p3.Y - _p1.Y) * v21.Z - (_p3.Z - _p1.Z) * v21.Y) / denom_ZY);
                inters = new Vector3(_p3.X + v43.X * mu, _p3.Y + v43.Y * mu, _p3.Z + v43.Z * mu);
                return true;
            }

            return false;
        }

        #endregion

        // -------------------------------------------- Point3D FUNCTIONS ----------------------------------------- //

        #region Projection of Point onto Plane

        public static Point3D ProjectPointOnPlane(Point3D _point, Point3D _p0, Point3D _p1, Point3D _p2)
        {
            // check if plane is well defined
            Vector3D v1 = _p1 - _p0;
            if (Math.Abs(v1.X) < LINEDISTCALC_TOLERANCE && Math.Abs(v1.Y) < LINEDISTCALC_TOLERANCE && Math.Abs(v1.Z) < LINEDISTCALC_TOLERANCE)
                return _point;
            v1.Normalize();

            Vector3D v2 = _p2 - _p0;
            if (Math.Abs(v2.X) < LINEDISTCALC_TOLERANCE && Math.Abs(v2.Y) < LINEDISTCALC_TOLERANCE && Math.Abs(v2.Z) < LINEDISTCALC_TOLERANCE)
                return _point;
            v2.Normalize();

            if ((Math.Abs(v1.X - v2.X) < LINEDISTCALC_TOLERANCE) &&
                (Math.Abs(v1.Y - v2.Y) < LINEDISTCALC_TOLERANCE) &&
                (Math.Abs(v1.Z - v2.Z) < LINEDISTCALC_TOLERANCE))
                return _point;
            if (Math.Abs(Vector3D.DotProduct(v1, v2)) > (1 - LINEDISTCALC_TOLERANCE))
                return _point;

            // prepare projection
            Vector3D plane_normal = Vector3D.CrossProduct(v1, v2);
            plane_normal.Normalize();

            Vector3D to_point = _point - _p0;
            Vector3D to_pointN = _point - _p0;
            if (to_pointN.Length < CommonExtensions.GENERAL_CALC_TOLERANCE)
                return _point;

            to_pointN.Normalize();
            double angle = Math.Acos(Vector3D.DotProduct(-to_pointN, plane_normal));
            double to_point_projection_length = to_point.Length * Math.Abs(Math.Sin(angle));
            double projection_line_length = Math.Sqrt(Math.Max(0, to_point.LengthSquared - to_point_projection_length * to_point_projection_length));

            Point3D projection = _point + plane_normal * projection_line_length;

            // CHECK if we projected in the wrong direction:
            Vector3D v_pr_check = _p1 - projection;
            
            if (Math.Abs(v_pr_check.X) < LINEDISTCALC_TOLERANCE && Math.Abs(v_pr_check.Y) < LINEDISTCALC_TOLERANCE && Math.Abs(v_pr_check.Z) < LINEDISTCALC_TOLERANCE)
                v_pr_check = _p2 - projection;

            if (Math.Abs(v_pr_check.X) < LINEDISTCALC_TOLERANCE && Math.Abs(v_pr_check.Y) < LINEDISTCALC_TOLERANCE && Math.Abs(v_pr_check.Z) < LINEDISTCALC_TOLERANCE)
                v_pr_check = _p0 - projection;

            v_pr_check.Normalize();
            double cos_check = Vector3D.DotProduct(plane_normal, v_pr_check);
            if (Math.Abs(cos_check) >= CommonExtensions.GENERAL_CALC_TOLERANCE)
            {
                projection = _point - plane_normal * projection_line_length;
            }
            // end CHECK

            return projection;
        }

        public static Vector3 ProjectPointOnPlane(Vector3 _point, Vector3 _p0, Vector3 _p1, Vector3 _p2)
        {
            Point3D p_point = new Point3D(_point.X, _point.Y, _point.Z);
            Point3D p_p0 = new Point3D(_p0.X, _p0.Y, _p0.Z);
            Point3D p_p1 = new Point3D(_p1.X, _p1.Y, _p1.Z);
            Point3D p_p2 = new Point3D(_p2.X, _p2.Y, _p2.Z);

            Point3D projection = CommonExtensions.ProjectPointOnPlane(p_point, p_p0, p_p1, p_p2);
            return new Vector3((float)projection.X, (float)projection.Y, (float)projection.Z);
        }

        #endregion

        // --------------------------- WINDOWS.MEDIA.MEDIA3D.VECTOR3D or POINT3D FUNCTIONS ------------------------ //

        #region Windows.Media.Media3D Point3D and Vector3D functions
        public static void NormalizeVector3D(ref Vector3D _v)
        {
            double vMagn = Math.Sqrt(_v.X * _v.X + _v.Y * _v.Y + _v.Z * _v.Z);
            if (vMagn < LINEDISTCALC_TOLERANCE)
                return;
            _v = new Vector3D(_v.X / vMagn, _v.Y / vMagn, _v.Z / vMagn);
        }

        // assumes the lists are of equal length and the points in them are in the same order
        // returns FALSE if the lists are not comparable (i.e. not versions of the same list)
        //                  or if they are comparable and contain similar enough entries (see _tolerance)
        public static bool Point3DListChanged(List<Point3D> _psVersion1, List<Point3D> _psVersion2, double _tolerance)
        {
            if (!AreComparable(_psVersion1, _psVersion2))
                return false;

            Point3DComparer pc = new Point3DComparer(_tolerance);
            for (int i = 0; i < _psVersion1.Count; i++)
            {
                if (!pc.Equals(_psVersion1[i], _psVersion2[i]))
                    return true;
            }

            return false;
        }

        public static bool AreComparable(List<Point3D> _ps1, List<Point3D> _ps2)
        {
            if (_ps1 == null && _ps2 == null)
                return true;
            if ((_ps1 != null && _ps2 == null) || (_ps1 == null && _ps2 != null))
                return false;

            int n = _ps1.Count;
            int m = _ps2.Count;
            if (n != m)
                return false;

            return true;
        }
        #endregion

        // ---------------------------------------- LIST AND ARRAY CONVERTERS ------------------------------------- //
 
        #region Converter Vector3[] <-> Vector4[]
        public static Vector4[] ConvertVector3ArToVector4Ar(Vector3[] _arIn)
        {
            int n = _arIn.Count();
            Vector4[] arOut = new Vector4[n];

            for(int i = 0; i < n; i++)
            {
                arOut[i] = _arIn[i].ToVector4();
            }

            return arOut;
        }

        public static Vector3[] ConvertVector4ArToVector3Ar(Vector4[] _arIn)
        {
            int n = _arIn.Count();
            Vector3[] arOut = new Vector3[n];

            for(int i = 0; i < n; i++)
            {
                arOut[i] = _arIn[i].ToVector3();
            }

            return arOut;
        }
        #endregion


        #region Converter Point3D Container <-> Vector3 Container
        public static Vector3[] ConvertPoint3DArToVector3Ar(Point3D[] _arIn)
        {
            int n = _arIn.Count();
            Vector3[] arOut = new Vector3[n];

            for(int i = 0; i < n; i++)
            {
                arOut[i] = _arIn[i].ToVector3();
            }

            return arOut;
        }

        public static Point3D[] ConvertVector3ArToPoint3DAr(Vector3[] _arIn)
        {
            int n = _arIn.Count();
            Point3D[] arOut = new Point3D[n];

            for (int i = 0; i < n; i++)
            {
                arOut[i] = _arIn[i].ToPoint3D();
            }

            return arOut;
        }

        public static List<Vector3> ConvertPoints3DListToVector3List(List<Point3D> _arIn)
        {
            int n = _arIn.Count();
            List<Vector3> arOut = new List<Vector3>();

            for (int i = 0; i < n; i++)
            {
                arOut.Add(_arIn[i].ToVector3());
            }

            return arOut;
        }

        public static List<Point3D> ConvertVector3ListToPoint3DList(List<Vector3> _arIn)
        {
            int n = _arIn.Count();
            List<Point3D> arOut = new List<Point3D>();

            for (int i = 0; i < n; i++)
            {
                arOut.Add(_arIn[i].ToPoint3D());
            }

            return arOut;
        }

        public static List<List<Vector3>> ConvertPoints3DListListToVector3ListList(List<List<Point3D>> _arIn)
        {
            int n = _arIn.Count();
            List<List<Vector3>> arOut = new List<List<Vector3>>();

            for (int i = 0; i < n; i++)
            {
                arOut.Add(ConvertPoints3DListToVector3List(_arIn[i]));
            }

            return arOut;
        }

        public static List<List<Point3D>> ConvertVector3ListListToPoint3DListList(List<List<Vector3>> _arIn)
        {
            int n = _arIn.Count();
            List<List<Point3D>> arOut = new List<List<Point3D>>();

            for (int i = 0; i < n; i++)
            {
                arOut.Add(ConvertVector3ListToPoint3DList(_arIn[i]));
            }

            return arOut;
        }

        #endregion

        #region Converter Vector4 List <-> Point3D List

        public static List<Vector4> ConvertPoints3DListToVector4List(List<Point3D> _arIn, float _w = 1f)
        {
            int n = _arIn.Count();
            List<Vector4> arOut = new List<Vector4>();

            for (int i = 0; i < n; i++)
            {
                arOut.Add(new Vector4(_arIn[i].ToVector3(), _w));
            }

            return arOut;
        }

        public static List<Point3D> ConvertVector4ListToPoint3DList(List<Vector4> _arIn, bool _divide_by_w = false)
        {
            int n = _arIn.Count();
            List<Point3D> arOut = new List<Point3D>();

            for (int i = 0; i < n; i++)
            {
                if (_divide_by_w && Math.Abs(_arIn[i].W) > GENERAL_CALC_TOLERANCE)
                    arOut.Add(new Point3D(_arIn[i].X / _arIn[i].W, _arIn[i].Y / _arIn[i].W, _arIn[i].Z / _arIn[i].W));
                else
                    arOut.Add(new Point3D(_arIn[i].X, _arIn[i].Y, _arIn[i].Z));
            }

            return arOut;
        }

        #endregion

    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // ======================================= CUSTOM COMPARER FOR POINT3D ==================================== //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region CUSTOM COMPARER POINT3D
    public class Point3DComparer : IEqualityComparer<Point3D>
    {
        private double tolerance; // raster step
        private Point3D origin;   // raster origin

        public Point3DComparer(double _tolerance)
        {
            this.tolerance = _tolerance;
            this.origin = new Point3D(0, 0, 0);
        }

        public bool Equals(Point3D _p1, Point3D _p2)
        {

            //Check whether the compared objects reference the same data. 
            if (Object.ReferenceEquals(_p1, _p2)) return true;

            //Check whether any of the compared objects is null. 
            if (Object.ReferenceEquals(_p2, null) || Object.ReferenceEquals(_p2, null))
                return false;

            //Check whether the points are close enough together
            int cellX1 = (int)Math.Round(_p1.X / this.tolerance, 0, MidpointRounding.AwayFromZero);
            int cellY1 = (int)Math.Round(_p1.Y / this.tolerance, 0, MidpointRounding.AwayFromZero);
            int cellZ1 = (int)Math.Round(_p1.Z / this.tolerance, 0, MidpointRounding.AwayFromZero);
            int cellX2 = (int)Math.Round(_p2.X / this.tolerance, 0, MidpointRounding.AwayFromZero);
            int cellY2 = (int)Math.Round(_p2.Y / this.tolerance, 0, MidpointRounding.AwayFromZero);
            int cellZ2 = (int)Math.Round(_p2.Z / this.tolerance, 0, MidpointRounding.AwayFromZero);

            return (cellX1 == cellX2) && (cellY1 == cellY2) && (cellZ1 == cellZ2);

        }

        // If Equals() returns true for a pair of objects  
        // then GetHashCode() must return the same value for these objects. 
        public int GetHashCode(Point3D _p)
        {
            //Check whether the object is null 
            if (Object.ReferenceEquals(_p, null)) return 0;

            //Get hash code for the X field
            int cellX = (int) Math.Round(_p.X / this.tolerance, 0, MidpointRounding.AwayFromZero);
            int hashX = cellX.GetHashCode();

            //Get hash code for the Y field 
            int cellY = (int)Math.Round(_p.Y / this.tolerance, 0, MidpointRounding.AwayFromZero);
            int hashY = cellY.GetHashCode();

            //Get hash code for the Z field
            int cellZ = (int)Math.Round(_p.Z / this.tolerance, 0, MidpointRounding.AwayFromZero);
            int hashZ = cellZ.GetHashCode();

            // Calculate the hash code for the point. 
            // return hashX ^ hashY ^ hashZ;
            // uses the 10 ls bits
            return ((hashX << 20) | (hashY << 10) | hashZ);
        }
    }
    #endregion

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // =================================== CUSTOM COMPARER FOR SHARPDX.VECTOR3 ================================ //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region CUSTOM COMPARER SHARPDX.VECTOR3
    public class Vector3Comparer : IEqualityComparer<SharpDX.Vector3>
    {
        private float tolerance; // raster step
        private Vector3 origin;   // raster origin
        public Vector3Comparer(float _tolerance = (float)CommonExtensions.GENERAL_CALC_TOLERANCE)
        {
            this.tolerance = _tolerance;
            this.origin = Vector3.Zero;
        }

        public bool Equals(Vector3 _p1, Vector3 _p2)
        {
            //Check whether the compared objects reference the same data. 
            if (Object.ReferenceEquals(_p1, _p2)) return true;

            //Check whether any of the compared objects is null. 
            if (Object.ReferenceEquals(_p1, null) || Object.ReferenceEquals(_p2, null))
                return false;

            //Check whether the points are close enough together
            int cellX1 = (int)Math.Round(_p1.X / this.tolerance, 0, MidpointRounding.AwayFromZero);
            int cellY1 = (int)Math.Round(_p1.Y / this.tolerance, 0, MidpointRounding.AwayFromZero);
            int cellZ1 = (int)Math.Round(_p1.Z / this.tolerance, 0, MidpointRounding.AwayFromZero);
            int cellX2 = (int)Math.Round(_p2.X / this.tolerance, 0, MidpointRounding.AwayFromZero);
            int cellY2 = (int)Math.Round(_p2.Y / this.tolerance, 0, MidpointRounding.AwayFromZero);
            int cellZ2 = (int)Math.Round(_p2.Z / this.tolerance, 0, MidpointRounding.AwayFromZero);

            return (cellX1 == cellX2) && (cellY1 == cellY2) && (cellZ1 == cellZ2);

        }

        // If Equals() returns true for a pair of objects  
        // then GetHashCode() must return the same value for these objects. 
        public int GetHashCode(Vector3 _p)
        {
            //Check whether the object is null 
            if (Object.ReferenceEquals(_p, null)) return 0;

            //Get hash code for the X field
            int cellX = (int)Math.Round(_p.X / this.tolerance, 0, MidpointRounding.AwayFromZero);
            int hashX = cellX.GetHashCode();

            //Get hash code for the Y field 
            int cellY = (int)Math.Round(_p.Y / this.tolerance, 0, MidpointRounding.AwayFromZero);
            int hashY = cellY.GetHashCode();

            //Get hash code for the Z field
            int cellZ = (int)Math.Round(_p.Z / this.tolerance, 0, MidpointRounding.AwayFromZero);
            int hashZ = cellZ.GetHashCode();

            //Calculate the hash code for the vector
            // uses the 10 ls bits
            return ((hashX << 20) | (hashY << 10) | hashZ);
        }
    }

    public class Vector3XComparer : IComparer<SharpDX.Vector3>
    {
        private float tolerance; // raster step
        public Vector3XComparer(float _tolerance = (float)CommonExtensions.GENERAL_CALC_TOLERANCE)
        {
            this.tolerance = _tolerance;
        }

        public int Compare(Vector3 _v1, Vector3 _v2)
        {
            //// DEFAULT VERSION
            //if (Math.Abs(_v1.X - _v2.X) <= this.tolerance)
            //    return 0;
            //else if (_v1.X > _v2.X)
            //    return 1;
            //else
            //    return -1;

            // ADAPTED VERSION
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
    #endregion

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // ==================================== LINES WITH HISTORY FOR THE OCTREE ================================= //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public enum LineChange { NONE = 0, EDITED = 1, DRAWN = 2, DELETED = 3}

    public class LineWHistory : IEquatable<LineWHistory>
    {
        public long ID { get; private set; }
        public Vector3 P0 { get; private set; }
        public Vector3 P1 { get; private set; }
        public BoundingBox BB { get; private set; }
        public LineChange History { get; set; }

        #region Constructors
        public LineWHistory(long _id)
        {
            this.ID = _id;
            this.P0 = Vector3.Zero;
            this.P1 = Vector3.Zero;
            this.BB = new BoundingBox(this.P0, this.P1);
            this.History = LineChange.NONE;
        }

        public LineWHistory(long _id, Vector3 _p0, Vector3 _p1)
        {
            this.ID = _id;
            this.P0 = _p0;
            this.P1 = _p1;
            this.BB = BoundingBox.FromPoints(new Vector3[] { _p0, _p1 });
            this.History = LineChange.NONE;
        }

        public LineWHistory(long _id, Vector3 _p0, Vector3 _p1, LineChange _change)
        {
            this.ID = _id;
            this.P0 = _p0;
            this.P1 = _p1;
            this.BB = BoundingBox.FromPoints(new Vector3[] { _p0, _p1 });
            this.History = _change;
        }
        #endregion

        #region Class Methods
        public bool Equals(LineWHistory other)
        {
            return (this.ID == other.ID);
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "ID:{0} P0:{1} P1:{2} HIST:{3}", 
                new object[] { this.ID, this.P0, this.P1, this.History });
        }
        

        public static bool FirstCollisionDetected(List<LineWHistory> _list, double _tolerance, out Vector3 _colPos)
        {
            _colPos = Vector3.Zero;
            if (_list == null || _list.Count == 0)
                return false;

            bool detected = false;
            int n = _list.Count;
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    detected = CommonExtensions.LineWLineCollision3D(_list[i].P0, _list[i].P1, 
                                                                     _list[j].P0, _list[j].P1, _tolerance, out _colPos);
                    if (detected)
                        break;
                }
                if (detected)
                    break;
            }

            return detected;

        }

        public static int AllCollisionsDetected(List<LineWHistory> _list, double _tolerance, out List<Vector3> _colPos,
                                                bool _skip_deleted = false)
        {
            _colPos = new List<Vector3>();
            Vector3 currentCol = Vector3.Zero;
            if (_list == null || _list.Count == 0)
                return 0;

            bool detected = false;
            int nrDetected = 0;
            int n = _list.Count;
            for (int i = 0; i < n; i++)
            {
                if (_skip_deleted && _list[i].History == LineChange.DELETED)
                    continue;

                for (int j = i + 1; j < n; j++)
                {
                    if (_skip_deleted && _list[j].History == LineChange.DELETED)
                        continue;

                    detected = CommonExtensions.LineWLineCollision3D(_list[i].P0, _list[i].P1,
                                                                     _list[j].P0, _list[j].P1, _tolerance, out currentCol);
                    if (detected)
                    {
                        _colPos.Add(currentCol);
                        nrDetected++;
                    }
                }

            }

            return nrDetected;

        }


        #endregion

        #region Static Methods for Lists of Lines With History

        public static Vector3 P3D2V3(Point3D p)
        {
            return new Vector3((float)p.X, (float)p.Y, (float)p.Z);
        }

        #region DRAW
        public static void drawLineWHistAt(ref List<Utils.LineWHistory> _list, int _index, Point3D _p0, Point3D _p1)
        {
            if (_list == null)
                return;
            if (_index == _list.Count)
            {
                _list.Add(new Utils.LineWHistory(_index, P3D2V3(_p0), P3D2V3(_p1), Utils.LineChange.DRAWN));
            }
        }

        public static void drawLinesWHistAt(ref List<List<Utils.LineWHistory>> _list, int _index,
                                                 List<Point3D> _ps0, List<Point3D> _ps1)
        {
            if (_list == null)
                return;
            if (_ps0 == null || _ps0.Count < 1 || _ps1 == null || _ps1.Count != _ps0.Count)
                return;

            if (_index > -1 && _index <= _list.Count)
            {
                int n = _ps0.Count;
                List<Utils.LineWHistory> newList = new List<Utils.LineWHistory>();
                for (int i = 0; i < n; i++)
                {
                    newList.Add(new Utils.LineWHistory(i, P3D2V3(_ps0[i]), P3D2V3(_ps1[i]), Utils.LineChange.DRAWN));
                }

                if (_index == _list.Count)
                    _list.Add(newList);
                else
                    _list[_index] = newList;
            }

        }
        #endregion

        #region EDIT
        public static void editLineWHistAt(ref List<Utils.LineWHistory> _list, int _index, Point3D _p0, Point3D _p1)
        {
            if (_list == null || _list.Count < (_index + 1))
                return;
            _list[_index] = new Utils.LineWHistory(_index, P3D2V3(_p0), P3D2V3(_p1), Utils.LineChange.EDITED);
        }

        public static void editLinesWHistAt(ref List<List<Utils.LineWHistory>> _list, int _index,
                                                 List<Point3D> _ps0, List<Point3D> _ps1)
        {
            if (_list == null || _list.Count < (_index + 1))
                return;
            if (_ps0 == null || _ps1 == null || _ps1.Count != _ps0.Count)
                return;

            int n_OLD = _list[_index].Count;
            int n = _ps0.Count;
            List<Utils.LineWHistory> editedList = new List<Utils.LineWHistory>();
            for (int i = 0; i < n; i++)
            {
                if (i < n_OLD)
                    editedList.Add(new Utils.LineWHistory(i, P3D2V3(_ps0[i]), P3D2V3(_ps1[i]), Utils.LineChange.EDITED));
                else
                    editedList.Add(new Utils.LineWHistory(i, P3D2V3(_ps0[i]), P3D2V3(_ps1[i]), Utils.LineChange.DRAWN));
            }
            if (n_OLD > n)
            {
                for (int j = n; j < n_OLD; j++)
                {
                    editedList.Add(new Utils.LineWHistory(j, _list[_index][j].P0, _list[_index][j].P1, Utils.LineChange.DELETED));
                }
            }
            _list[_index] = new List<Utils.LineWHistory>(editedList);

        }
        #endregion

        #region DELETE
        public static void deleteLineWHistAt(ref List<Utils.LineWHistory> _list, int _index)
        {
            if (_list == null || _list.Count < (_index + 1))
                return;
            _list[_index].History = Utils.LineChange.DELETED;
        }

        public static void deleteLinesWHistAt(ref List<List<Utils.LineWHistory>> _list, int _index)
        {
            if (_list == null || _list.Count < (_index + 1))
                return;
            int n = _list[_index].Count;
            for (int i = 0; i < n; i++)
            {
                _list[_index][i].History = Utils.LineChange.DELETED;
            }
        }
        #endregion

        #region REMOVE DELETED FROM LIST
        public static void removeLineWHistDeleted(ref List<Utils.LineWHistory> _list)
        {
            if (_list == null)
                return;

            List<Utils.LineWHistory> toBeRemoved = new List<Utils.LineWHistory>();
            foreach (var line in _list)
            {
                if (line.History == Utils.LineChange.DELETED)
                    toBeRemoved.Add(line);
            }

            foreach (var line in toBeRemoved)
            {
                _list.Remove(line);
            }
        }

        public static void removeLinesWHistDeleted(ref List<List<Utils.LineWHistory>> _list, bool _purge_empty_lists = false)
        {
            if (_list == null)
                return;
            int n = _list.Count;
            for (int i = 0; i < n; i++)
            {
                List<Utils.LineWHistory> innerList = _list[i];
                removeLineWHistDeleted(ref innerList);
                _list[i] = new List<Utils.LineWHistory>(innerList);
            }

            if (_purge_empty_lists)
            {
                List<List<Utils.LineWHistory>> purged_list = new List<List<LineWHistory>>();
                for (int i = 0; i < n; i++)
                {
                    if (_list[i].Count > 0)
                        purged_list.Add(_list[i]);
                }
                _list = new List<List<LineWHistory>>(purged_list);
            }
        }
        #endregion

        #region RESET
        public static void resetHistLineWHist(ref List<Utils.LineWHistory> _list)
        {
            if (_list == null)
                return;

            foreach (var line in _list)
            {
                line.History = Utils.LineChange.NONE;
            }
        }

        public static void resetHistLinesWHist(ref List<List<Utils.LineWHistory>> _list)
        {
            if (_list == null)
                return;

            foreach (var innerList in _list)
            {
                foreach (var line in innerList)
                {
                    line.History = Utils.LineChange.NONE;
                }
            }
        }
        #endregion

        #endregion

        #region Static Converters

        public static void LinesWH2UniqueP3D(List<LineWHistory> _lines, ref List<Point3D> pointsE, ref List<Point3D> pointsM,
                                            bool _skip_deleted_lines = false)
        {
            if (pointsE == null)
                pointsE = new List<Point3D>();
            if (pointsM == null)
                pointsM = new List<Point3D>();
            if (_lines == null)
                return;

            Point3DComparer comparer = new Point3DComparer(0.01);
            foreach(var line in _lines)
            {
                if (_skip_deleted_lines && line.History == LineChange.DELETED)
                    continue;

                Point3D p0 = line.P0.ToPoint3D();
                Point3D p1 = line.P1.ToPoint3D();
                Point3D pM = (p0.ToVector3() * 0.5f + p1.ToVector3() * 0.5f).ToPoint3D();
                if (!pointsE.Contains(p0, comparer))
                    pointsE.Add(p0);
                if (!pointsE.Contains(p1, comparer))
                    pointsE.Add(p1);
                if (!pointsM.Contains(pM, comparer))
                    pointsM.Add(pM);
            }

        }

        #endregion

    }
}
