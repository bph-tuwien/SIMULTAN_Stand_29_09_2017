using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;

namespace GeometryViewer.SpatialOrganization
{
    public enum ViewFrustumOrientation { VF_NEAR = 0, VF_FAR = 1, VF_LEFT = 2, VF_RIGHT = 3, VF_TOP = 4, VF_BOTTOM = 5}

    public class ViewFrustumFunctions : GroupModel3D
    {
        
        //private static readonly double VF_SCALE = 0.0002;
        private static readonly double MIN_VISIBILTY_COS = -0.7071; // 135°

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ==================================== DEPENDENCY PROPERTIES FOR BINDING ================================= //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region CAMERA

        public bool SynchrCam
        {
            get { return (bool)GetValue(SynchrCamProperty); }
            set { SetValue(SynchrCamProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SynchrCam.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SynchrCamProperty =
            DependencyProperty.Register("SynchrCam", typeof(bool), typeof(ViewFrustumFunctions),
            new UIPropertyMetadata(false));

        public HelixToolkit.SharpDX.Camera CurrentCam
        {
            get { return (HelixToolkit.SharpDX.Camera)GetValue(CurrentCamProperty); }
            set { SetValue(CurrentCamProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CurrentCam.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentCamProperty =
            DependencyProperty.Register("CurrentCam", typeof(HelixToolkit.SharpDX.Camera), typeof(ViewFrustumFunctions),
            new UIPropertyMetadata(new HelixToolkit.SharpDX.PerspectiveCamera(),
                                    new PropertyChangedCallback(MyCurrentCamPropertyChangedCallback),
                                    new CoerceValueCallback(MyCurrentCamCoerceValueCallback)));
        private static void MyCurrentCamPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ViewFrustumFunctions vf = d as ViewFrustumFunctions;
            object newVal = e.NewValue;
            HelixToolkit.SharpDX.ProjectionCamera cam = newVal as HelixToolkit.SharpDX.ProjectionCamera;

            if (vf != null && cam != null && vf.SynchrCam)
            {
                // clear children list
                vf.Children.Clear();
                // get the size of the viewport
                double parentH = 0;
                double parentW = 0;
                Viewport3DX parent = vf.Parent as Viewport3DX;
                if (parent != null)
                {
                    vf.CurrentPos = parent.CurrentPosition;
                    parentH = parent.ActualHeight;
                    parentW = parent.ActualWidth;
                    //var test = parent.Parent;
                }

                // add new View Frustum
                vf.DefineViewFrustum(cam, parentH, parentW);
                vf.BuildViewFrustumModel();
            }
        }
        private static object MyCurrentCamCoerceValueCallback(DependencyObject d, object value)
        {
            //var test1 = d;
            //var test2 = value;
            return value;
        }
        
        public Point3D CurrentPos
        {
            get { return (Point3D)GetValue(CurrentPosProperty); }
            set { SetValue(CurrentPosProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CurrentPos.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentPosProperty =
            DependencyProperty.Register("CurrentPos", typeof(Point3D), typeof(ViewFrustumFunctions),
            new UIPropertyMetadata(new Point3D()));

        #endregion

        #region Appearance

        public bool ShowFrustum
        {
            get { return (bool)GetValue(ShowFrustumProperty); }
            set { SetValue(ShowFrustumProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowFrustum.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowFrustumProperty =
            DependencyProperty.Register("ShowFrustum", typeof(bool), typeof(ViewFrustumFunctions),
            new UIPropertyMetadata(true, new PropertyChangedCallback(MyShowFrustumPropertyChangedCallback)));

        private static void MyShowFrustumPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ViewFrustumFunctions vff = d as ViewFrustumFunctions;
            if (vff != null && vff.model != null)
            {
                if (vff.ShowFrustum)
                    vff.model.Visibility = Visibility.Visible;
                else
                    vff.model.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ============================================ CLASS MEMBERS ============================================= //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        Point3D nearBL, nearBR, nearTL, nearTR;
        Point3D farBL, farBR, farTL, farTR;

        private Vector3D[] normals;
        private Point3D[] posOnBoundary;

        private LineGeometryModel3D model;

        public ViewFrustumFunctions()
        {
            this.normals = new Vector3D[6];
            this.posOnBoundary = new Point3D[6];
        }

        #region Build Frustum Volume
        private void DefineViewFrustum(HelixToolkit.SharpDX.ProjectionCamera _cam, double _vpH, double _vpW)
        {
            if (_cam == null)
                return;
            
            // check for orthographic projection
            HelixToolkit.SharpDX.OrthographicCamera camOrtho = _cam as HelixToolkit.SharpDX.OrthographicCamera;
            
            // calculate CAMERA CS
            Point3D camPos = _cam.Position;
            Vector3D camViewDir = _cam.LookDirection;
            double distToTarget = camViewDir.Length;
            Utils.CommonExtensions.NormalizeVector3D(ref camViewDir);
            Vector3D camUpDir = _cam.UpDirection;
            Utils.CommonExtensions.NormalizeVector3D(ref camUpDir);

            // turn to orthogonal CS
            Vector3D camXDir = Vector3D.CrossProduct(camViewDir, camUpDir);
            Utils.CommonExtensions.NormalizeVector3D(ref camXDir);
            camUpDir = Vector3D.CrossProduct(camViewDir, camXDir);
            Utils.CommonExtensions.NormalizeVector3D(ref camUpDir);

            float zFar = (float)_cam.FarPlaneDistance;
            float zNear = (float)_cam.NearPlaneDistance;

            // calculate base of frustum pyramid
            // perspective projection
            float nHalfH = (float)((zNear / zFar) * _vpH);
            float nHalfW = (float)((zNear / zFar) * _vpW);
            if (camOrtho != null)
            {
                // orthographic projection
                nHalfH = (float)((zNear / zFar) * distToTarget * _vpH);
                nHalfW = (float)((zNear / zFar) * distToTarget * _vpW);
            }
            this.nearBL = camPos + zNear * camViewDir - nHalfW * camXDir
                                                      - nHalfH * camUpDir;
            this.nearBR = camPos + zNear * camViewDir + nHalfW * camXDir
                                                      - nHalfH * camUpDir;
            this.nearTL = camPos + zNear * camViewDir - nHalfW * camXDir
                                                      + nHalfH * camUpDir;
            this.nearTR = camPos + zNear * camViewDir + nHalfW * camXDir
                                                      + nHalfH * camUpDir;
            // perspective projection
            float fHalfH = nHalfH * zFar / zNear;
            float fHalfW = nHalfW * zFar / zNear;
            if (camOrtho != null)
            {
                // orthographic projection
                fHalfH = nHalfH;
                fHalfW = nHalfW;
            }
            this.farBL = camPos + zFar * camViewDir - fHalfW * camXDir
                                                    - fHalfH * camUpDir;
            this.farBR = camPos + zFar * camViewDir + fHalfW * camXDir
                                                    - fHalfH * camUpDir;
            this.farTL = camPos + zFar * camViewDir - fHalfW * camXDir
                                                    + fHalfH * camUpDir;
            this.farTR = camPos + zFar * camViewDir + fHalfW * camXDir
                                                    + fHalfH * camUpDir;
   
            // calculate normals

            // near
            this.posOnBoundary[(int)ViewFrustumOrientation.VF_NEAR] = new Point3D(this.nearBL.X * 0.5 + this.nearTR.X * 0.5,
                                                                                  this.nearBL.Y * 0.5 + this.nearTR.Y * 0.5,
                                                                                  this.nearBL.Z * 0.5 + this.nearTR.Z * 0.5);
            this.normals[(int)ViewFrustumOrientation.VF_NEAR] = camViewDir;
            // far
            this.posOnBoundary[(int)ViewFrustumOrientation.VF_FAR] = new Point3D(this.farBL.X * 0.5 + this.farTR.X * 0.5,
                                                                                 this.farBL.Y * 0.5 + this.farTR.Y * 0.5,
                                                                                 this.farBL.Z * 0.5 + this.farTR.Z * 0.5);
            this.normals[(int)ViewFrustumOrientation.VF_FAR] = -camViewDir;
            // left
            this.posOnBoundary[(int)ViewFrustumOrientation.VF_LEFT] = new Point3D(this.nearBL.X * 0.499 + this.nearTL.X * 0.499 + this.farBL.X * 0.001 + this.farTL.X * 0.001,
                                                                                  this.nearBL.Y * 0.499 + this.nearTL.Y * 0.499 + this.farBL.Y * 0.001 + this.farTL.Y * 0.001,
                                                                                  this.nearBL.Z * 0.499 + this.nearTL.Z * 0.499 + this.farBL.Z * 0.001 + this.farTL.Z * 0.001);
            Vector3D normalL = Vector3D.CrossProduct((this.farBL - this.nearBL), (this.nearBL - this.nearTL));
            Utils.CommonExtensions.NormalizeVector3D(ref normalL);
            this.normals[(int)ViewFrustumOrientation.VF_LEFT] = normalL;
            // right
            this.posOnBoundary[(int)ViewFrustumOrientation.VF_RIGHT] = new Point3D(this.nearBR.X * 0.499 + this.nearTR.X * 0.499 + this.farBR.X * 0.001 + this.farTR.X * 0.001,
                                                                                   this.nearBR.Y * 0.499 + this.nearTR.Y * 0.499 + this.farBR.Y * 0.001 + this.farTR.Y * 0.001,
                                                                                   this.nearBR.Z * 0.499 + this.nearTR.Z * 0.499 + this.farBR.Z * 0.001 + this.farTR.Z * 0.001);
            Vector3D normalR = Vector3D.CrossProduct((this.farBR - this.nearBR), (this.nearTR - this.nearBR));
            Utils.CommonExtensions.NormalizeVector3D(ref normalR);
            this.normals[(int)ViewFrustumOrientation.VF_RIGHT] = normalR;
            // top
            this.posOnBoundary[(int)ViewFrustumOrientation.VF_TOP] = new Point3D(this.nearTR.X * 0.499 + this.nearTL.X * 0.499 + this.farTR.X * 0.001 + this.farTL.X * 0.001,
                                                                                 this.nearTR.Y * 0.499 + this.nearTL.Y * 0.499 + this.farTR.Y * 0.001 + this.farTL.Y * 0.001,
                                                                                 this.nearTR.Z * 0.499 + this.nearTL.Z * 0.499 + this.farTR.Z * 0.001 + this.farTL.Z * 0.001);
            Vector3D normalT = Vector3D.CrossProduct((this.farTR - this.nearTR), (this.nearTL - this.nearTR));
            Utils.CommonExtensions.NormalizeVector3D(ref normalT);
            this.normals[(int)ViewFrustumOrientation.VF_TOP] = normalT;
            // bottom
            this.posOnBoundary[(int)ViewFrustumOrientation.VF_BOTTOM] = new Point3D(this.nearBR.X * 0.499 + this.nearBL.X * 0.499 + this.farBR.X * 0.001 + this.farBL.X * 0.001,
                                                                                    this.nearBR.Y * 0.499 + this.nearBL.Y * 0.499 + this.farBR.Y * 0.001 + this.farBL.Y * 0.001,
                                                                                    this.nearBR.Z * 0.499 + this.nearBL.Z * 0.499 + this.farBR.Z * 0.001 + this.farBL.Z * 0.001);
            Vector3D normalB = Vector3D.CrossProduct((this.farBR - this.nearBR), (this.nearBR - this.nearBL));
            Utils.CommonExtensions.NormalizeVector3D(ref normalB);
            this.normals[(int)ViewFrustumOrientation.VF_BOTTOM] = normalB;
        }

        private void BuildViewFrustumModel()
        {
            // frustum
            LineBuilder b1 = new LineBuilder();
            b1.AddLine(this.nearBL.ToVector3(), this.farBL.ToVector3());
            b1.AddLine(this.nearBR.ToVector3(), this.farBR.ToVector3());
            b1.AddLine(this.nearTR.ToVector3(), this.farTR.ToVector3());
            b1.AddLine(this.nearTL.ToVector3(), this.farTL.ToVector3());

            b1.AddLine(this.nearBL.ToVector3(), this.nearBR.ToVector3());
            b1.AddLine(this.nearBR.ToVector3(), this.nearTR.ToVector3());
            b1.AddLine(this.nearTR.ToVector3(), this.nearTL.ToVector3());
            b1.AddLine(this.nearTL.ToVector3(), this.nearBL.ToVector3());

            b1.AddLine(this.farBL.ToVector3(), this.farBR.ToVector3());
            b1.AddLine(this.farBR.ToVector3(), this.farTR.ToVector3());
            b1.AddLine(this.farTR.ToVector3(), this.farTL.ToVector3());
            b1.AddLine(this.farTL.ToVector3(), this.farBL.ToVector3());

            this.model = new LineGeometryModel3D()
            {
                Geometry = b1.ToLineGeometry3D(),
                Color = Color.Yellow,
                Thickness = 0.5f,
                Visibility = (this.ShowFrustum) ? Visibility.Visible : Visibility.Collapsed,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
            };
            this.Children.Add(this.model);

            //// normals
            //LineBuilder b2 = new LineBuilder();
            //for (int i = 0; i < 6; i++)
            //{
            //    int fact;
            //    if (i == 1)
            //        fact = 1000;
            //    else
            //        fact = 2;
            //    b2.AddLine(this.posOnBoundary[i].ToVector3(), (this.posOnBoundary[i] + fact * this.normals[i]).ToVector3());
            //}
            //LineGeometryModel3D debug = new LineGeometryModel3D()
            //{
            //    Geometry = b2.ToLineGeometry3D(),
            //    Color = Color.Orange,
            //    Thickness = 2f,
            //    Visibility = (this.ShowFrustum) ? Visibility.Visible : Visibility.Collapsed,
            //    IsHitTestVisible = false,
            //    Transform = new MatrixTransform3D(Matrix3D.Identity),
            //};
            //this.Children.Add(debug);

            // re-attach to renderer
            if (this.renderHost != null)
                this.Attach(this.renderHost);
        }

        #endregion

        #region Testing
        public bool IsInFrustum(Point3D _p)
        {
            if (_p == null)
                return false;

            for (int i = 0; i < 6; i++ )
            {
                Vector3D v = _p - this.posOnBoundary[i];
                Utils.CommonExtensions.NormalizeVector3D(ref v);
                double cos = Vector3D.DotProduct(v, this.normals[i]);
                if (cos < 0)
                    return false;
            }

            return true;
        }


        // axis-aligned box
        public bool IsInFrustum(BoundingBox _bb)
        {
            return IsInThisPosRelToFrustum(_bb, true);
        }
        public bool IsOutsideFrustum(BoundingBox _bb)
        {
            return IsInThisPosRelToFrustum(_bb, false);
        }

        private bool IsInThisPosRelToFrustum(BoundingBox _bb, bool _inside)
        {
            if (_bb == null)
                return false;

            for (int i = 0; i < 6; i++)
            {
                // determine positive and negative point relative to the normal
                Point3D posV = _bb.Minimum.ToPoint3D();
                Point3D negV = _bb.Maximum.ToPoint3D();
                if (this.normals[i].X >= 0)
                {
                    posV.X = _bb.Maximum.X;
                    negV.X = _bb.Minimum.X;
                }
                if (this.normals[i].Y >= 0)
                {
                    posV.Y = _bb.Maximum.Y;
                    negV.Y = _bb.Minimum.Y;
                }
                if (this.normals[i].Z >= 0)
                {
                    posV.Z = _bb.Maximum.Z;
                    negV.Z = _bb.Minimum.Z;
                }

                // get relevant values
                Vector3D vP = posV - this.posOnBoundary[i];
                Utils.CommonExtensions.NormalizeVector3D(ref vP);
                double cosP = Vector3D.DotProduct(vP, this.normals[i]);
                Vector3D vN = negV - this.posOnBoundary[i];
                Utils.CommonExtensions.NormalizeVector3D(ref vN);
                double cosN = Vector3D.DotProduct(vN, this.normals[i]);

                // test
                if (_inside)
                {
                    if (cosP < MIN_VISIBILTY_COS || cosN < MIN_VISIBILTY_COS)
                        return false;
                }
                else
                {
                    if (cosP < 0)
                        return true;
                }
            }

            if (_inside)
                return true;
            else
                return false;
        }


        #endregion

    }
}
