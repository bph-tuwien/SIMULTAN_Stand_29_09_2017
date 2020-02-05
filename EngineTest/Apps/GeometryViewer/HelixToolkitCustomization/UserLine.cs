using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;

namespace GeometryViewer.HelixToolkitCustomization
{
    class UserLine : LineGeometryModel3D
    {
        private Color? initColor = null;
        private double? initThickness = null;
        protected HitTestResult hResult;
        public float HitTestThickness { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rayWS">Position: position of the Camera</param>
        /// <param name="hits"></param>
        /// <returns></returns>
        public override bool HitTest(Ray rayWS, ref List<HitTestResult> hits)
        {
            if (initColor == null)
            {
                initColor = this.Color;
            }
            if (initThickness == null)
            {
                initThickness = this.Thickness;
            }

            var result = MyHitTest(rayWS, ref hits);
            return result;
        }

        // alternative to base.HitTest(rayWS, ref hits);
        protected bool MyHitTest(Ray rayWS, ref List<HitTestResult> hits)
        {
            LineGeometry3D lineGeometry3D = this.Geometry as LineGeometry3D;
            Viewport3DX viewport = FindVisualAncestor<Viewport3DX>(this.renderHost as DependencyObject);

            if (this.Visibility == System.Windows.Visibility.Collapsed ||
                this.IsHitTestVisible == false ||
                viewport  == null ||
                lineGeometry3D == null)
            {
                return false;
            }

            var result = new HitTestResult { IsValid = false, Distance = double.MaxValue };
            var lastDist = double.MaxValue;
            var index = 0;
            foreach (var line in lineGeometry3D.Lines)
            {
                var t0 = Vector3.TransformCoordinate(line.P0, this.ModelMatrix);
                var t1 = Vector3.TransformCoordinate(line.P1, this.ModelMatrix);
                Vector3 sp, tp;
                float sc, tc;
                var distance = GetRayToLineDistance(rayWS, t0, t1, out sp, out tp, out sc, out tc);
                var svpm = viewport.GetScreenViewProjectionMatrix();
                Vector4 sp4;
                Vector4 tp4;
                Vector3.Transform(ref sp, ref svpm, out sp4);
                Vector3.Transform(ref tp, ref svpm, out tp4);
                var sp3 = sp4.ToVector3();
                var tp3 = tp4.ToVector3();
                var tv2 = new Vector2(tp3.X - sp3.X, tp3.Y - sp3.Y);
                var dist = tv2.Length();
                if (dist < lastDist && dist <= this.HitTestThickness)
                {
                    lastDist = dist;
                    result.PointHit = sp.ToPoint3D();
                    result.NormalAtHit = (sp - tp).ToVector3D(); // not normalized to get length
                    result.Distance = distance;
                    result.ModelHit = this;
                    result.IsValid = true;
                    result.Tag = index; // ToDo: LineHitTag with additional info
                }

                index++;
            }

            if (result.IsValid)
            {
                hits.Add(result);
            }
            this.hResult = result;
            return result.IsValid;
        }

        public static T FindVisualAncestor<T>(DependencyObject obj) where T : DependencyObject
        {
            if (obj != null)
            {
                var parent = System.Windows.Media.VisualTreeHelper.GetParent(obj);
                while (parent != null)
                {
                    var typed = parent as T;
                    if (typed != null)
                    {
                        return typed;
                    }

                    parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
                }
            }

            return null;
        }

        public static float GetRayToLineDistance(
            Ray ray, Vector3 t0, Vector3 t1, out Vector3 sp, out Vector3 tp, out float sc, out float tc)
        {
            var s0 = ray.Position;
            var s1 = ray.Position + ray.Direction;
            return GetLineToLineDistance(s0, s1, t0, t1, out sp, out tp, out sc, out tc, true);
        }

        public static float GetLineToLineDistance(
            Vector3 s0, Vector3 s1, Vector3 t0, Vector3 t1, out Vector3 sp, out Vector3 tp, out float sc, out float tc, bool sIsRay = false)
        {
            Vector3 u = s1 - s0;
            Vector3 v = t1 - t0;
            Vector3 w = s0 - t0;

            float a = Vector3.Dot(u, u); // always >= 0
            float b = Vector3.Dot(u, v);
            float c = Vector3.Dot(v, v); // always >= 0
            float d = Vector3.Dot(u, w);
            float e = Vector3.Dot(v, w);
            float D = a * c - b * b;     // always >= 0
            float sN, sD = D;            // sc = sN / sD, default sD = D >= 0
            float tN, tD = D;            // tc = tN / tD, default tD = D >= 0

            // compute the line parameters of the two closest points
            if (D < float.Epsilon)
            {
                // the lines are almost parallel
                sN = 0.0f; // force using point P0 on segment S1
                sD = 1.0f; // to prevent possible division by 0.0 later
                tN = e;
                tD = c;
            }
            else
            {
                // get the closest points on the infinite lines
                sN = (b * e - c * d);
                tN = (a * e - b * d);

                if (!sIsRay)
                {
                    if (sN < 0.0f)
                    {
                        // sc < 0 => the s=0 edge is visible
                        sN = 0.0f;
                        tN = e;
                        tD = c;
                    }
                    else if (sN > sD)
                    {
                        // sc > 1  => the s=1 edge is visible
                        sN = sD;
                        tN = e + b;
                        tD = c;
                    }
                }
            }

            if (tN < 0.0f)
            {
                // tc < 0 => the t=0 edge is visible
                tN = 0.0f;
                // recompute sc for this edge
                if (-d < 0.0f)
                {
                    sN = 0.0f;
                }
                else if (-d > a)
                {
                    sN = sD;
                }
                else
                {
                    sN = -d;
                    sD = a;
                }
            }
            else if (tN > tD)
            {
                // tc > 1  => the t=1 edge is visible
                tN = tD;
                // recompute sc for this edge
                if ((-d + b) < 0.0f)
                {
                    sN = 0;
                }
                else if ((-d + b) > a)
                {
                    sN = sD;
                }
                else
                {
                    sN = (-d + b);
                    sD = a;
                }
            }

            // finally do the division to get sc and tc
            sc = (Math.Abs(sN) < float.Epsilon ? 0.0f : sN / sD);
            tc = (Math.Abs(tN) < float.Epsilon ? 0.0f : tN / tD);

            // get the difference of the two closest points
            sp = s0 + (sc * u);
            tp = t0 + (tc * v);
            var tv = sp - tp;

            return tv.Length(); // return the closest distance
        }

    }

}
