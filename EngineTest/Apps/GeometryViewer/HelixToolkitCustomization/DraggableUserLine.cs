using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;

namespace GeometryViewer.HelixToolkitCustomization
{
    class DraggableUserLine : UserLine, ISelectable
    {
        // DEPENDENCY PROPPERTIES FOR BINDING
        public bool DragX
        {
            get { return (bool)GetValue(DragXProperty); }
            set { SetValue(DragXProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DragX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DragXProperty =
            DependencyProperty.Register("DragX", typeof(bool), typeof(DraggableUserLine), new UIPropertyMetadata(true));

        public bool DragY
        {
            get { return (bool)GetValue(DragYProperty); }
            set { SetValue(DragYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DragY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DragYProperty =
            DependencyProperty.Register("DragY", typeof(bool), typeof(DraggableUserLine), new UIPropertyMetadata(true));

        public bool DragZ
        {
            get { return (bool)GetValue(DragZProperty); }
            set { SetValue(DragZProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DragZ.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DragZProperty =
            DependencyProperty.Register("DragZ", typeof(bool), typeof(DraggableUserLine), new UIPropertyMetadata(true));

        // TRANSFORMATION PROPERTIES
        protected bool isCaptured;
        protected Viewport3DX viewport;
        protected HelixToolkit.SharpDX.Camera camera;
        public MatrixTransform3D DragTransform { get; private set; }
        public Point3D LastHitPos { get; private set; }

        // CONSTRUCTORS
        public DraggableUserLine() : base()
        {
            this.DragTransform = new MatrixTransform3D(this.Transform.Value);
        }

        // METHOD OVERRIDES
        public override void OnMouse3DDown(object sender, RoutedEventArgs e)
        {
            base.OnMouse3DDown(sender, e);

            var args = e as Mouse3DEventArgs;
            if (args == null) return;
            if (args.Viewport == null) return;

            this.isCaptured = true;
            this.viewport = args.Viewport;
            this.camera = args.Viewport.Camera;
            this.LastHitPos = args.HitTestResult.PointHit;
        }

        public override void OnMouse3DUp(object sender, RoutedEventArgs e)
        {
            base.OnMouse3DUp(sender, e);
            if (this.isCaptured)
            { 
                this.isCaptured = false;
                this.viewport = null;
                this.camera = null;
            }
        }

        public override void OnMouse3DMove(object sender, RoutedEventArgs e)
        {
            base.OnMouse3DMove(sender, e);
            if (this.isCaptured)
            {
                var args = e as Mouse3DEventArgs;

                // move dragmodel                         
                var normal = this.camera.LookDirection;

                // hit position                        
                var newHit = this.viewport.UnProjectOnPlane(args.Position, LastHitPos, normal);
                if (newHit.HasValue)
                {
                    var offset = (newHit.Value - LastHitPos);
                    var trafo = this.Transform.Value;

                    if (this.DragX)
                        trafo.OffsetX += offset.X;

                    if (this.DragY)
                        trafo.OffsetY += offset.Y;

                    if (this.DragZ)
                        trafo.OffsetZ += offset.Z;

                    this.DragTransform.Matrix = trafo;

                    this.LastHitPos = newHit.Value;

                    // OLD                      
                    // this.Transform = this.DragTransform;
                    // NEW
                    TransformPositions();
                }
            }
        }

        private void TransformPositions()
        {
            // get old positions
            Vector3[] p = this.Geometry.Positions.Select(a => a).ToArray();
            int n = p.Count();
            Vector4[] ph = new Vector4[n];

            // to homogeneous coordiantes
            for(int i = 0; i < n; i++)
            { 
                ph[i] = p[i].ToVector4(1f);   
            }
            // transform by this.DragTransform
            var T = this.DragTransform.ToMatrix();
            Vector4.Transform(ph, ref T, ph);
            
            // to inhomogeneous coordinates
            for (int i = 0; i < n; i++)
            {
                p[i] = ph[i].ToVector3();
            }

            // save as new positions in new geometry
            Color4[] colors = this.Geometry.Colors;
            Int32[] ind = this.Geometry.Indices;

            LineGeometry3D newG = new HelixToolkit.SharpDX.Wpf.LineGeometry3D();
            newG.Positions = p;
            newG.Colors = colors;
            newG.Indices = ind;

            this.Geometry = newG;
        }
  
    }
}
