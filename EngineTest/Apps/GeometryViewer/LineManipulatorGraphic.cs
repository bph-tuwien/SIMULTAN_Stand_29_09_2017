using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.ComponentModel;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;

using GeometryViewer.HelixToolkitCustomization;

namespace GeometryViewer
{
    public class LineManipulator3DGraphic : LineManipulator3D
    {

        // ==================================== DEPENDENCY PROPERTIES FOR BINDING ================================= //

        #region Drag

        public bool DragX
        {
            get { return (bool)GetValue(DragXProperty); }
            set { SetValue(DragXProperty, value); }
        }
        // Using a DependencyProperty as the backing store for DragX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DragXProperty =
            DependencyProperty.Register("DragX", typeof(bool), typeof(LineManipulator3D),
            new UIPropertyMetadata(true, new PropertyChangedCallback(MyDragXPropertyChangedCallback),
                                         new CoerceValueCallback(MyDragXCoerceValueCallback)));

        protected static void MyDragXPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //var test1 = d;
            //var test2 = e;
        }

        protected static object MyDragXCoerceValueCallback(DependencyObject d, object value)
        {
            //var test1 = d;
            //var test2 = value;
            return value;
        }

        public bool DragY
        {
            get { return (bool)GetValue(DragYProperty); }
            set { SetValue(DragYProperty, value); }
        }
        // Using a DependencyProperty as the backing store for DragY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DragYProperty =
            DependencyProperty.Register("DragY", typeof(bool), typeof(LineManipulator3D),
            new UIPropertyMetadata(true, new PropertyChangedCallback(MyDragYPropertyChangedCallback),
                                         new CoerceValueCallback(MyDragYCoerceValueCallback)));

        protected static void MyDragYPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //var test1 = d;
            //var test2 = e;
        }

        protected static object MyDragYCoerceValueCallback(DependencyObject d, object value)
        {
            //var test1 = d;
            //var test2 = value;
            return value;
        }

        public bool DragZ
        {
            get { return (bool)GetValue(DragZProperty); }
            set { SetValue(DragZProperty, value); }
        }
        // Using a DependencyProperty as the backing store for DragZ.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DragZProperty =
            DependencyProperty.Register("DragZ", typeof(bool), typeof(LineManipulator3D),
            new UIPropertyMetadata(true, new PropertyChangedCallback(MyDragZPropertyChangedCallback),
                                         new CoerceValueCallback(MyDragZCoerceValueCallback)));

        protected static void MyDragZPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //var test1 = d;
            //var test2 = e;
        }

        protected static object MyDragZCoerceValueCallback(DependencyObject d, object value)
        {
            var test1 = d;
            var test2 = value;
            return value;
        }

        #endregion

        #region SNAP to Grid

        public bool SnapToGrid
        {
            get { return (bool)GetValue(SnapToGridProperty); }
            set { SetValue(SnapToGridProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SnapToGrid.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SnapToGridProperty =
            DependencyProperty.Register("SnapToGrid", typeof(bool), typeof(LineManipulator3D),
            new UIPropertyMetadata(false));

        public float SnapMagnet
        {
            get { return (float)GetValue(SnapMagnetProperty); }
            set { SetValue(SnapMagnetProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SnapMagnet.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SnapMagnetProperty =
            DependencyProperty.Register("SnapMagnet", typeof(float), typeof(LineManipulator3D),
            new UIPropertyMetadata(0f));

        protected Vector3D dragTransform_current;
        protected List<Vector3> pos_current;
        protected List<Vector3> endH_currentTV;

        #endregion

        #region SNAP to Object

        private bool use_oSnapPoint;
        public bool Use_OSnapPoint
        {
            get { return this.use_oSnapPoint; }
            set 
            { 
                this.use_oSnapPoint = value;
                base.RegisterPropertyChanged("Use_OSnapPoint");
            }
        }

        private Point3D oSnapPoint;
        public Point3D OSnapPoint
        {
            get { return this.oSnapPoint; }
            set 
            { 
                this.oSnapPoint = value;
                base.RegisterPropertyChanged("OSnapPoint");
            }
        }

        #endregion

        // ========================================= TRANSFORMATION PROPERTIES ==================================== //

        #region TRANSFORMATION
        public MatrixTransform3D DragTransform { get; private set; }
        public Point3D LastHitPos { get; private set; }
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // =============================================== CONSTRUCTORS =========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Initialization

        public LineManipulator3DGraphic()
            :base()
        {
            this.DragTransform = new MatrixTransform3D(this.Transform.Value);
            UpdateOnCoordsChange();
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ========================================== TRANSFORMATION METHODS ====================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // --------------------------------------------- ENDPOINT handles ----------------------------------------- //

        #region Endpoint_Handles

        protected override void OnNodeMouse3DDown(object sender, RoutedEventArgs e)
        {
            var args = e as Mouse3DEventArgs;
            if (args == null) return;
            if (args.Viewport == null) return;

            this.isCaptured = true;
            Application.Current.MainWindow.Cursor = Cursors.SizeAll;

            // prepare for SNAP
            this.pos_current = new List<Vector3>(this.pos);
            this.endH_currentTV = new List<Vector3>(this.edgeHandles.Select(x => x.Transform.ToMatrix().TranslationVector).ToArray());

            // update the modifying lines
            int n = this.pos.Count();
            for (int i = 0; i < n; i++)
            {
                if (sender == this.endHandles[i])
                {
                    this.posML_old = pos[i];
                    Matrix ML_t = Matrix.Translation(this.posML_old);
                    Matrix ML_new = Matrix.Scaling(0f) * ML_t;
                    this.modifyLines.Transform = new MatrixTransform3D(ML_new.ToMatrix3D());

                    DraggableGeometryWoSnapModel3D dgm = sender as DraggableGeometryWoSnapModel3D;
                    if (dgm != null)
                        dgm.OnMouseDownOverride(pos[i].ToPoint3D());
                }
            }
            this.modifyLines.Visibility = Visibility.Visible;
        }

        protected override void OnNodeMouse3DUp(object sender, RoutedEventArgs e)
        {
            if (this.isCaptured)
            {
                Application.Current.MainWindow.Cursor = Cursors.Arrow;
                this.modifyLines.Visibility = Visibility.Hidden;

                // adjust for snap
                if (this.SnapToGrid && this.SnapMagnet > 0.0001f)
                {
                    UpdatePosOnlySnapOn(sender);
                    UpdateTransforms(sender);
                }

                // adjust for object snap
                this.Use_OSnapPoint = false;

                Pos2Coords();
            }
        }

        protected override void OnNodeMouse3DMove(object sender, RoutedEventArgs e)
        {
            DraggableGeometryWoSnapModel3D model = sender as DraggableGeometryWoSnapModel3D;
            if (this.isCaptured && model != null)
            {
                model.DragX = this.DragX;
                model.DragY = this.DragY;
                model.DragZ = this.DragZ;
                if (this.Use_OSnapPoint)                  
                    model.OnMouse3DMoveDelayed(this.OSnapPoint);

                UpdatePosOnly();
                UpdateTransforms(sender);
            }
        }

        protected void UpdatePosOnlySnapOn(object sender)
        {
            // update positions
            var endTransf = this.endHandles.Select(x => (x.Transform as MatrixTransform3D)).ToArray();
            var endMatrices = endTransf.Select(x => x.Value).ToArray();
            var pos_NEW = endMatrices.Select(x => x.ToMatrix().TranslationVector).ToArray();

            int n = pos_NEW.Count();
            int i;
            for (i = 0; i < n; i++)
            {
                Vector3 diff = pos_NEW[i] - this.pos_current[i];
                Vector3D diffA = AdjustForSnap(diff.ToVector3D());
                this.pos[i] = this.pos_current[i] + diffA.ToVector3();
            }

            // update sender
            // ENDPOINT handles
            for (i = 0; i < n; i++)
            {
                if (sender == this.endHandles[i])
                {
                    var T = Matrix3DExtensions.Translate3D(pos[i].ToVector3D());

                    this.endHandles[i].Transform = new MatrixTransform3D(T);
                    var child = (HelixToolkit.SharpDX.Wpf.GeometryModel3D)this.Children[this.endH_indInChildren[i]];
                    if (child != null)
                    {
                        child.Transform = new MatrixTransform3D(T);
                    }
                }
            }
        }


        #endregion

        // --------------------------------------------- MIDPOINT handles ----------------------------------------- //

        #region Midpoint_Handles

        protected override void OnMidNodeMouse3DDown(object sender, RoutedEventArgs e)
        {
            var args = e as Mouse3DEventArgs;
            if (args == null) return;
            if (args.Viewport == null) return;

            this.isCaptured = true;
            Application.Current.MainWindow.Cursor = Cursors.SizeNS;
            this.viewport = args.Viewport;
            this.camera = args.Viewport.Camera;
            this.LastHitPos = args.HitTestResult.PointHit;
            // prepare for snap
            this.dragTransform_current = new Vector3D(0, 0, 0);
            this.pos_current = new List<Vector3>(this.pos);

            // update the modifying lines
            int n = this.pos.Count();
            for (int i = 0; i < n; i++)
            {
                if (sender == this.midpointHandles[i])
                {
                    this.posML_old = Vector3.Lerp(this.pos[i], this.pos[(i + 1) % n], 0.5f);
                    Matrix ML_t = Matrix.Translation(this.posML_old);
                    Matrix ML_new = Matrix.Scaling(0f) * ML_t;
                    this.modifyLines.Transform = new MatrixTransform3D(ML_new.ToMatrix3D());
                }
            }
            this.modifyLines.Visibility = Visibility.Visible;
        }

        protected override void OnMidNodeMouse3DUp(object sender, RoutedEventArgs e)
        {
            if (this.isCaptured)
            {
                Application.Current.MainWindow.Cursor = Cursors.Arrow;
                this.isCaptured = false;
                this.camera = null;
                this.viewport = null;
                this.modifyLines.Visibility = Visibility.Hidden;

                // adjust for snap
                if (this.SnapToGrid && this.SnapMagnet > 0.0001)
                {
                    this.pos = new List<Vector3>(this.pos_current); // reload the positions
                    AdjustDragTransfOnSnap(); // adjust the transform
                    UpdateEdgeTransforms(sender); // apply transform
                }

                // adjust for object snap
                this.Use_OSnapPoint = false;

                Pos2Coords();
            }
        }

        protected override void OnMidNodeMouse3DMove(object sender, RoutedEventArgs e)
        {
            if (this.isCaptured)
            {
                var args = e as Mouse3DEventArgs;

                // move dragmodel                         
                var normal = this.camera.LookDirection;

                // hit position                        
                var newHit = this.viewport.UnProjectOnPlane(args.Position, LastHitPos, normal);
                if (newHit.HasValue)
                {
                    Point3D p = newHit.Value;
                    if (this.Use_OSnapPoint)
                        p = this.OSnapPoint;

                    Vector3D offset = (p - LastHitPos);                   
                    
                    var trafo = this.Transform.Value;

                    if (this.DragX)
                    {
                        trafo.OffsetX += offset.X;
                        this.dragTransform_current.X += offset.X;
                    }

                    if (this.DragY)
                    {
                        trafo.OffsetY += offset.Y;
                        this.dragTransform_current.Y += offset.Y;
                    }

                    if (this.DragZ)
                    {
                        trafo.OffsetZ += offset.Z;
                        this.dragTransform_current.Z += offset.Z;
                    }

                    this.DragTransform.Matrix = trafo;
                    // this.Transform = this.DragTransform;
                    UpdateEdgeTransforms(sender);
                    this.LastHitPos = p;
                }
            }
        }

        protected void UpdateEdgeTransforms(object sender)
        {
            var T = this.DragTransform.ToMatrix();
            UpdatePosAfterMidNode(sender, T);
            UpdateTransformsAfterMidNode(sender);
        }

        #endregion

        // ----------------------------------------------- EDGE handles ------------------------------------------- //

        #region Edge_Handles

        protected override void OnEdgeMouse3DDown(object sender, RoutedEventArgs e)
        {
            var args = e as Mouse3DEventArgs;
            if (args == null) return;
            if (args.Viewport == null) return;

            this.isCaptured = true;
            Application.Current.MainWindow.Cursor = Cursors.Hand;
            this.viewport = args.Viewport;
            this.camera = args.Viewport.Camera;
            this.LastHitPos = args.HitTestResult.PointHit;
            // prepare for snap
            this.dragTransform_current = new Vector3D(0, 0, 0);
            this.pos_current = new List<Vector3>(this.pos);

            // update the modifying lines
            int n = this.pos.Count();
            for (int i = 0; i < n; i++)
            {
                if (sender == this.edgeHandles[i])
                {
                    this.posML_old = LastHitPos.ToVector3();
                    Matrix ML_t = Matrix.Translation(this.posML_old);
                    Matrix ML_new = Matrix.Scaling(0f) * ML_t;
                    this.modifyLines.Transform = new MatrixTransform3D(ML_new.ToMatrix3D());
                }
            }
            this.modifyLines.Visibility = Visibility.Visible;
        }

        protected override void OnEdgeMouse3DUp(object sender, RoutedEventArgs e)
        {
            if (this.isCaptured)
            {
                Application.Current.MainWindow.Cursor = Cursors.Arrow;
                this.isCaptured = false;
                this.camera = null;
                this.viewport = null;
                this.modifyLines.Visibility = Visibility.Hidden;

                // adjust for snap
                if (this.SnapToGrid && this.SnapMagnet > 0.0001)
                {
                    this.pos = new List<Vector3>(this.pos_current); // reload the positions
                    AdjustDragTransfOnSnap(); // adjust the transform
                    UpdateTransformsAndPositions(); // apply transform
                }

                Pos2Coords();
            }
        }

        protected override void OnEdgeMouse3DMove(object sender, RoutedEventArgs e)
        {
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
                    {
                        trafo.OffsetX += offset.X;
                        this.dragTransform_current.X += offset.X;
                    }

                    if (this.DragY)
                    {
                        trafo.OffsetY += offset.Y;
                        this.dragTransform_current.Y += offset.Y;
                    }

                    if (this.DragZ)
                    {
                        trafo.OffsetZ += offset.Z;
                        this.dragTransform_current.Z += offset.Z;
                    }


                    this.DragTransform.Matrix = trafo;

                    // this.Transform = this.DragTransform;
                    UpdateTransformsAndPositions();
                    this.LastHitPos = newHit.Value;
                }
            }
        }

        protected void UpdateTransformsAndPositions()
        {
            // update positions and handles
            var T = this.DragTransform.ToMatrix();
            UpdatePosAfterEdge(T);
            UpdateTransformsAfterEdge();

            // update modify LinesIn
            Vector3 posML_new = this.LastHitPos.ToVector3();
            if (!this.DragX)
                posML_new.X = this.posML_old.X;
            if (!this.DragY)
                posML_new.Y = this.posML_old.Y;
            if (!this.DragZ)
                posML_new.Z = this.posML_old.Z;
            TransformModifyingLines(posML_new);

        }

        #endregion

        // ------------------------------------------------ UTILITIES --------------------------------------------- //

        #region Utilities

        protected void AdjustDragTransfOnSnap()
        {
            if (this.SnapToGrid && this.SnapMagnet > 0.0001)
            {
                var trafo = this.DragTransform.Matrix;

                Vector3D diffA = AdjustForSnap(this.dragTransform_current);

                trafo.OffsetX = diffA.X;
                trafo.OffsetY = diffA.Y;
                trafo.OffsetZ = diffA.Z;

                this.DragTransform.Matrix = trafo;
            }
        }

        protected Vector3D AdjustForSnap(Vector3D p)
        {
            Vector3D result = p;

            var x = p.X / this.SnapMagnet;
            var xi = Math.Round(x);
            var y = p.Y / this.SnapMagnet;
            var yi = Math.Round(y);
            var z = p.Z / this.SnapMagnet;
            var zi = Math.Round(z);
            result = new Vector3D(xi * this.SnapMagnet, yi * this.SnapMagnet, zi * this.SnapMagnet);

            return result;
        }

        #endregion
    }
}
