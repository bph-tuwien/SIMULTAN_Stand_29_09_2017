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

namespace GeometryViewer
{
    public enum HandleType { ENDPOINT = 0, MIDPOINT = 1, EDGE = 2, NONE = 3 }

    class LineManipulator3DNumeric : LineManipulator3D
    {
        // ==================================== DEPENDENCY PROPERTIES FOR BINDING ================================= //

        #region User_Input

        public float DeltaX
        {
            get { return (float)GetValue(DeltaXProperty); }
            set { SetValue(DeltaXProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DeltaX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DeltaXProperty =
            DependencyProperty.Register("DeltaX", typeof(float), typeof(LineManipulator3D),
            new UIPropertyMetadata(0f, new PropertyChangedCallback(MyDeltaXPropertyChangedCallback),
                                         new CoerceValueCallback(MyDeltaXCoerceValueCallback)));

        protected static void MyDeltaXPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var test1 = d;
            var test2 = e;
        }

        protected static object MyDeltaXCoerceValueCallback(DependencyObject d, object value)
        {
            var test1 = d;
            var test2 = value;
            return value;
        }

        public float DeltaY
        {
            get { return (float)GetValue(DeltaYProperty); }
            set { SetValue(DeltaYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DeltaY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DeltaYProperty =
            DependencyProperty.Register("DeltaY", typeof(float), typeof(LineManipulator3D),
            new UIPropertyMetadata(0f, new PropertyChangedCallback(MyDeltaYPropertyChangedCallback),
                                         new CoerceValueCallback(MyDeltaYCoerceValueCallback)));
        protected static void MyDeltaYPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var test1 = d;
            var test2 = e;
        }

        protected static object MyDeltaYCoerceValueCallback(DependencyObject d, object value)
        {
            var test1 = d;
            var test2 = value;
            return value;
        }

        public float DeltaZ
        {
            get { return (float)GetValue(DeltaZProperty); }
            set { SetValue(DeltaZProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DeltaZ.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DeltaZProperty =
            DependencyProperty.Register("DeltaZ", typeof(float), typeof(LineManipulator3D),
            new UIPropertyMetadata(0f, new PropertyChangedCallback(MyDeltaZPropertyChangedCallback),
                                         new CoerceValueCallback(MyDeltaZCoerceValueCallback)));

        protected static void MyDeltaZPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var test1 = d;
            var test2 = e;
        }

        protected static object MyDeltaZCoerceValueCallback(DependencyObject d, object value)
        {
            var test1 = d;
            var test2 = value;
            return value;
        }


        #endregion

        public ICommand ApplyDeltaToHandleCmd { get; private set; }

        // ====================================== INTERNAL SELECTION PROPERTIES =================================== //

        protected HelixToolkit.SharpDX.Wpf.Material RedMaterial { get; private set; }
        protected int SelHandleIndInChildren { get; private set; }
        protected HandleType SelHandleType { get; private set; }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // =============================================== CONSTRUCTORS =========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Initialization

        public LineManipulator3DNumeric()
            :base()
        {
            // define materials
            var rm = new PhongMaterial();
            rm.DiffuseColor = new Color4(1f, 0f, 0f, 1f);
            rm.AmbientColor = new Color4(0.8f, 0.4f, 0.4f, 1f);
            rm.SpecularColor = new Color4(1f, 0.75f, 0f, 1f);
            rm.SpecularShininess = 3;
            rm.EmissiveColor = new Color4(0.8f, 0f, 0f, 1f);
            this.RedMaterial = rm;

            // define commands
            this.ApplyDeltaToHandleCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => ApplyDeltaToHandle(), 
                                                                                   (x) => CanExecute_ApplyDeltaToHandle());

            UpdateOnCoordsChange();
            this.SelHandleIndInChildren = -1;
            this.SelHandleType = HandleType.NONE;
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

            if (isCaptured)
            {
                this.isCaptured = false;
                MarkEndHandle(sender, false);
                this.SelHandleType = HandleType.NONE;
            }
            else
            {
                this.isCaptured = true;
                this.SelHandleType = HandleType.ENDPOINT;
                MarkEndHandle(sender, true);
            }
        }

        protected override void OnNodeMouse3DMove(object sender, RoutedEventArgs e)
        {
            var db = sender as DraggableGeometryModel3D;
            if (db != null)
            {
                db.DragX = false;
                db.DragY = false;
                db.DragZ = false;
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

            if (isCaptured)
            {
                this.isCaptured = false;
                MarkMidpointHandle(sender, false);
                this.SelHandleType = HandleType.NONE;
            }
            else
            {
                this.isCaptured = true;
                this.SelHandleType = HandleType.MIDPOINT;
                MarkMidpointHandle(sender, true);
            }
        }

        #endregion

        // ----------------------------------------------- EDGE handles ------------------------------------------- //

        #region Edge_Handles

        protected override void OnEdgeMouse3DDown(object sender, RoutedEventArgs e)
        {
            var args = e as Mouse3DEventArgs;
            if (args == null) return;
            if (args.Viewport == null) return;

            if (isCaptured)
            {
                this.isCaptured = false;
                MarkEdgeHandle(sender, false);
                this.SelHandleType = HandleType.NONE;
            }
            else
            {
                this.isCaptured = true;
                this.SelHandleType = HandleType.EDGE;
                MarkEdgeHandle(sender, true);
            }
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ================================================= COMMANDS ============================================= //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region COMMAND: Apply Delta to the Handle
        protected void ApplyDeltaToHandle()
        {
            var handle = this.Children[this.SelHandleIndInChildren];

            if (this.SelHandleType == HandleType.ENDPOINT)
            {
                DraggableGeometryModel3D db = handle as DraggableGeometryModel3D;
                if (db == null)
                    return;

                // transform handle
                var T = db.Transform.ToMatrix();
                T.TranslationVector += new Vector3(this.DeltaX, this.DeltaY, this.DeltaZ);
                db.Transform = new MatrixTransform3D(T.ToMatrix3D());

                // transform positions
                UpdatePosOnly();
                UpdateTransforms(null);

                // repaint handle
                db.Material = this.SelMaterial;
            }
            else if (this.SelHandleType == HandleType.MIDPOINT)
            {
                MeshGeometryModel3D mgm = handle as MeshGeometryModel3D;
                if (mgm == null)
                    return;

                // transform handle
                var T = mgm.Transform.ToMatrix();
                T.TranslationVector += new Vector3(this.DeltaX, this.DeltaY, this.DeltaZ);
                mgm.Transform = new MatrixTransform3D(T.ToMatrix3D());

                // transform positions and other handles
                Matrix TP = Matrix.Translation(this.DeltaX, this.DeltaY, this.DeltaZ);
                UpdatePosAfterMidNode(mgm, TP);
                UpdateTransformsAfterMidNode(mgm);

                // repaint handle
                mgm.Material = this.SelMaterial;
            }
            else if (this.SelHandleType == HandleType.EDGE)
            {
                MeshGeometryModel3D mgm = handle as MeshGeometryModel3D;
                if (mgm == null)
                    return;

                // transform handle
                var T = mgm.Transform.ToMatrix();
                T.TranslationVector += new Vector3(this.DeltaX, this.DeltaY, this.DeltaZ);
                mgm.Transform = new MatrixTransform3D(T.ToMatrix3D());

                // transform positions and other handles
                Matrix TP = Matrix.Translation(this.DeltaX, this.DeltaY, this.DeltaZ);
                UpdatePosAfterEdge(TP);
                UpdateTransformsAfterEdge();

                // repaint handle
                mgm.Material = this.SelMaterial;

            }

            // output
            Pos2Coords();
            this.isCaptured = false;
            this.SelHandleIndInChildren = -1;
            this.SelHandleType = HandleType.NONE;
        }

        protected bool CanExecute_ApplyDeltaToHandle()
        {
            return (this.isCaptured && this.SelHandleIndInChildren >= 0 && this.SelHandleIndInChildren < this.Children.Count);
        }
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ================================================ UTILITIES ============================================= //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Utilities

        private void MarkEndHandle(object sender, bool asSelected)
        {
            int n = this.endHandles.Count;
            for (int i = 0; i < n; i++)
            {
                if (sender == this.endHandles[i])
                {
                    this.SelHandleIndInChildren = asSelected ? this.endH_indInChildren[i] : -1;
                    this.endHandles[i].Material = asSelected ? this.RedMaterial : this.SelMaterial;
                }
                else
                {
                    this.endHandles[i].Material = this.SelMaterial;
                }
            }
            
            if(asSelected)
            {
                foreach(var handle in this.midpointHandles)
                {
                    handle.Material = this.SelMaterial;
                }
                foreach(var handle in this.edgeHandles)
                {
                    handle.Material = this.materialLines;
                }
            }
        }

        private void MarkMidpointHandle(object sender, bool asSelected)
        {
            int n = this.midpointHandles.Count;
            for (int i = 0; i < n; i++)
            {
                if (sender == this.midpointHandles[i])
                {
                    this.SelHandleIndInChildren = asSelected ? this.midPH_indInChildren[i] : -1;
                    this.midpointHandles[i].Material = asSelected ? this.RedMaterial : this.SelMaterial;                   
                }
                else
                {
                    this.midpointHandles[i].Material = this.SelMaterial;
                }
            }

            if (asSelected)
            {
                foreach (var handle in this.endHandles)
                {
                    handle.Material = this.SelMaterial;
                }
                foreach (var handle in this.edgeHandles)
                {
                    handle.Material = this.materialLines;
                }
            }
        }

        private void MarkEdgeHandle(object sender, bool asSelected)
        {
            int n = this.edgeHandles.Count;
            for (int i = 0; i < n; i++)
            {
                if (sender == this.edgeHandles[i])
                {
                    this.SelHandleIndInChildren = asSelected ? this.edgeH_indInChildren[i] : -1;
                    this.edgeHandles[i].Material = asSelected ? this.RedMaterial : this.materialLines;
                }
                else
                {
                    this.edgeHandles[i].Material = this.materialLines;
                }
            }

            if (asSelected)
            {
                foreach (var handle in this.endHandles)
                {
                    handle.Material = this.SelMaterial;
                }
                foreach (var handle in this.midpointHandles)
                {
                    handle.Material = this.SelMaterial;
                }
            }
        }

        #endregion

    }
}
