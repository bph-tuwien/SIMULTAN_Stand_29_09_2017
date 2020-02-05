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
    public abstract class LineManipulator3D : GroupModel3Dext
    {
        // ================================================= CONSTANTS ============================================ //
        protected static float END_GRIP_SIZE = 0.02f;
        protected static float EDGE_GRIP_SIZE = 0.1f;
        protected static float MID_GRIP_SIZE = 0.01f;

        // ==================================== DEPENDENCY PROPERTIES FOR BINDING ================================= //

        #region Grip Scale depending on distance to Camera

        public Point3D CamPos
        {
            get { return (Point3D)GetValue(CamPosProperty); }
            set { SetValue(CamPosProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CamPos.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CamPosProperty =
            DependencyProperty.Register("CamPos", typeof(Point3D), typeof(LineManipulator3D),
            new UIPropertyMetadata(new Point3D(0,0,0)));

        #endregion

        #region Selection

        private bool isSelectedCopy;
        public bool IsSelectedCopy
        {
            get { return this.isSelectedCopy; }
            set 
            { 
                this.isSelectedCopy = value;
                base.RegisterPropertyChanged("IsSelectedCopy");
            }
        }

        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }
        // Using a DependencyProperty as the backing store for IsSelected.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register("IsSelected", typeof(bool), typeof(LineManipulator3D),
            new UIPropertyMetadata(false, new PropertyChangedCallback(MyIsSelectedPropertyChangedCallback)));

        protected static void MyIsSelectedPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LineManipulator3D lm = d as LineManipulator3D;
            if (lm != null)
            {
                lm.IsSelectedCopy = lm.IsSelected;
            }
        }

        public HelixToolkit.SharpDX.Wpf.Material SelMaterial
        {
            get { return (HelixToolkit.SharpDX.Wpf.Material)GetValue(SelMaterialProperty); }
            set { SetValue(SelMaterialProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelMaterial.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelMaterialProperty =
            DependencyProperty.Register("SelMaterial", typeof(HelixToolkit.SharpDX.Wpf.Material), typeof(LineManipulator3D),
            new UIPropertyMetadata(MaterialChanged));

        protected static void MaterialChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is PhongMaterial)
            {
                foreach (var item in ((GroupModel3D)d).Children)
                {
                    var model = item as MaterialGeometryModel3D;
                    if (model != null)
                    {
                        model.Material = e.NewValue as PhongMaterial;
                    }
                }
            }
        }

        protected HelixToolkit.SharpDX.Wpf.Material materialLines;

        #endregion

        #region Communication_with_LineGenerator3D
        public bool IsPolyLineClosed
        {
            get { return (bool)GetValue(IsPolyLineClosedProperty); }
            set { SetValue(IsPolyLineClosedProperty, value); } // when called from XAML SetValue is called directly, bypassing the set
        }

        // Using a DependencyProperty as the backing store for IsPolyLineClosed.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsPolyLineClosedProperty =
            DependencyProperty.Register("IsPolyLineClosed", typeof(bool), typeof(LineManipulator3D),
            new UIPropertyMetadata(true, new PropertyChangedCallback(MyIsPolyLineClosedPropertyChangedCallback),
                                         new CoerceValueCallback(MyIsPolyLineClosedCoerceValueCallback)));

        protected static void MyIsPolyLineClosedPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // var test1 = d;
            // var test2 = e;
            ((LineManipulator3D)d).OnIsPolyLineClosedProperty_IsFalse();
        }

        protected static object MyIsPolyLineClosedCoerceValueCallback(DependencyObject d, object value)
        {
            // var test1 = d;
            // var test2 = value;
            return value;
        }

        public List<Point3D> CoordsTargetIn
        {
            get { return (List<Point3D>)GetValue(CoordsTargetInProperty); }
            set { SetValue(CoordsTargetInProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CoordsSel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CoordsTargetInProperty =
            DependencyProperty.Register("CoordsTargetIn", typeof(List<Point3D>), typeof(LineManipulator3D),
            new UIPropertyMetadata(new List<Point3D>(), new PropertyChangedCallback(MyCoordsTargetInPropertyChangedCallback),
                                         new CoerceValueCallback(MyCoordsTargetInCoerceValueCallback)));

        protected static void MyCoordsTargetInPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LineManipulator3D lm = d as LineManipulator3D;
            if (lm != null)
            {
                List<Point3D> newVal = e.NewValue as List<Point3D>;
                if (newVal != null)
                {
                    if (newVal.Count > 0)
                        lm.IsSelected = true;
                    else
                        lm.IsSelected = false;
                }
                lm.UpdateOnCoordsChange();
            }
        }
        protected static object MyCoordsTargetInCoerceValueCallback(DependencyObject d, object value)
        {
            LineManipulator3DGraphic lmG = d as LineManipulator3DGraphic;
            LineManipulator3DNumeric lmN = d as LineManipulator3DNumeric;
            if ((lmG != null && !lmG.InputNumeric) || (lmN != null && lmN.InputNumeric))
            {
                return value;
            }
            else
            {
                return new List<Point3D>();
            }
        }

        public List<Point3D> CoordsTargetOut
        {
            get { return (List<Point3D>)GetValue(CoordsTargetOutProperty); }
            set { SetValue(CoordsTargetOutProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CoordsTargetOut.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CoordsTargetOutProperty =
            DependencyProperty.Register("CoordsTargetOut", typeof(List<Point3D>), typeof(LineManipulator3D),
            new UIPropertyMetadata(new List<Point3D>(), new PropertyChangedCallback(MyCoordsTargetOutPropertyChangedCallback),
                                         new CoerceValueCallback(MyCoordsTargetOutCoerceValueCallback)));

        protected static void MyCoordsTargetOutPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //var test1 = d;
            //var test2 = e;   
        }
        protected static object MyCoordsTargetOutCoerceValueCallback(DependencyObject d, object value)
        {
            LineManipulator3DGraphic lmG = d as LineManipulator3DGraphic;
            LineManipulator3DNumeric lmN = d as LineManipulator3DNumeric;
            if ((lmG != null && !lmG.InputNumeric) || (lmN != null && lmN.InputNumeric))
            {
                return value;
            }
            else
            {
                return new List<Point3D>();
            }
        }
        

        #endregion

        #region Communication_with_User_Input_Options


        // !!! THIS PROPERTY IS REGISTERED AS ATTACHED SO IT PROPAGATES ALONG INHERITANCE TREE !!!
        public bool InputNumeric
        {
            get { return (bool)GetValue(InputNumericProperty); }
            set { SetValue(InputNumericProperty, value); }
        }

        // Using a DependencyProperty as the backing store for InputNumeric.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InputNumericProperty =
            DependencyProperty.RegisterAttached("InputNumeric", typeof(bool), typeof(LineGenerator3D),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits,
                                            new PropertyChangedCallback(MyInputNumericPropertyChangedCallback),
                                            new CoerceValueCallback(MyInputNumericCoerceValueCallback)));

        private static void MyInputNumericPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //var test1 = d;
            //var test2 = e;
        }
        private static object MyInputNumericCoerceValueCallback(DependencyObject d, object value)
        {
            //var test1 = d;
            //var test2 = value;
            return value;
        }

        #endregion

        // ==================================== INFO PROPERTIES AS BINDING SOURCES ================================ //

        protected System.Windows.Media.Media3D.Point3D deltaTransf;
        public System.Windows.Media.Media3D.Point3D DeltaTransf
        {
            get { return this.deltaTransf; }
            set
            { 
                deltaTransf = value;
                base.RegisterPropertyChanged("DeltaTransf");
            }

        }

        // ========================================= TRANSFORMATION PROPERTIES ==================================== //

        protected bool isCaptured;
        protected Viewport3DX viewport;
        protected HelixToolkit.SharpDX.Camera camera;

        // ============================================= HANDLE GEOMETRY ========================================== //
        protected List<Vector3> pos;
        protected List<DraggableGeometryWoSnapModel3D> endHandles;
        protected List<int> endH_indInChildren;
        protected List<MeshGeometryModel3D> midpointHandles;
        protected List<int> midPH_indInChildren;
        protected List<MeshGeometryModel3D> edgeHandles;
        protected List<int> edgeH_indInChildren;
        protected LineGeometryModel3D modifyLines;
        protected Vector3 posML_old;

        protected static HelixToolkit.SharpDX.Wpf.Geometry3D NodeGeometry;
        protected static HelixToolkit.SharpDX.Wpf.Geometry3D MidEdgeGeometry;
        protected static HelixToolkit.SharpDX.Wpf.Geometry3D EdgeGeometry;
        protected static HelixToolkit.SharpDX.Wpf.Geometry3D LineGeometry;

        protected float scaleCamPos;

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // =============================================== CONSTRUCTORS =========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Initialization

        static LineManipulator3D()
        {
            var b1 = new MeshBuilder();
            b1.AddSphere(Vector3.Zero, END_GRIP_SIZE, 8, 8);
            NodeGeometry = b1.ToMeshGeometry3D();

            var b2 = new MeshBuilder();
            b2.AddSphere(Vector3.Zero, MID_GRIP_SIZE, 8, 8);
            MidEdgeGeometry = b2.ToMeshGeometry3D();

            var b3 = new MeshBuilder();
            b3.AddCylinder(new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f), EDGE_GRIP_SIZE, 4);
            EdgeGeometry = b3.ToMeshGeometry3D();

            var b4 = new LineBuilder();
            b4.AddBox(new Vector3(0f, 0f, 0f), 1, 1, 1);
            LineGeometry = b4.ToLineGeometry3D();
        }

        public LineManipulator3D()
        {
            // define materials
            var sm = new PhongMaterial();
            sm.DiffuseColor = new Color4(1f);
            sm.AmbientColor = new Color4(0.8f, 0.8f, 0.8f, 1f);
            sm.SpecularColor = new Color4(1f, 0.75f, 0f, 1f);
            sm.SpecularShininess = 3;
            sm.EmissiveColor = new Color4(0.5f, 0.5f, 0.5f, 1f);
            // sm.ReflectiveColor = new Color4(0f, 0f, 0f, 1f);
            this.SelMaterial = sm;

            var mpm = new PhongMaterial();
            mpm.DiffuseColor = new Color4(0.8f, 0.8f, 0.8f, 1f);
            mpm.AmbientColor = new Color4(0.6f, 0.6f, 0.6f, 1f);
            mpm.SpecularColor = new Color4(1f, 0.75f, 0f, 1f);
            mpm.SpecularShininess = 3;
            mpm.EmissiveColor = new Color4(0.3f, 0.3f, 0.3f, 1f);
            this.materialLines = mpm;
        }

        protected virtual void UpdateOnCoordsChange()
        {
            // initialize coordinates
            Coords2Pos();
                       
            if (pos == null)
                return;

            this.Children.Clear();

            // initialise handle lists
            int n = pos.Count;
            endHandles = new List<DraggableGeometryWoSnapModel3D>(n);
            midpointHandles = new List<MeshGeometryModel3D>(n);
            edgeHandles = new List<MeshGeometryModel3D>(n);

            endH_indInChildren = new List<int>(n);
            midPH_indInChildren = new List<int>(n);
            edgeH_indInChildren = new List<int>(n);

            // determine scaling factor of grips
            BoundingBox bb = BoundingBox.FromPoints(pos.ToArray());
            float avgDistCam = Vector3.Distance(Vector3.Lerp(bb.Maximum, bb.Minimum, 0.5f), this.CamPos.ToVector3());
            this.scaleCamPos = Math.Max(1f,avgDistCam);

            for (int i = 0; i < n; i++)
            {
                // this.scaleCamPos = Vector3.Distance(pos[i], this.CamPos.ToVector3());
                // ENDPOINT handles
                Matrix3D T = Matrix3D.Identity;
                T.Scale(new Vector3D(this.scaleCamPos, this.scaleCamPos, this.scaleCamPos));
                T.Translate(pos[i].ToVector3D());
                var m1 = new DraggableGeometryWoSnapModel3D()
                {
                    Visibility = Visibility.Visible,
                    Material = this.SelMaterial,
                    Geometry = NodeGeometry,
                    Transform = new MatrixTransform3D(T),
                };
                m1.MouseDown3D += OnNodeMouse3DDown;
                m1.MouseMove3D += OnNodeMouse3DMove;
                m1.MouseUp3D += OnNodeMouse3DUp;
                this.endHandles.Add(m1);
                this.Children.Add(m1);
                this.endH_indInChildren.Add(this.Children.Count - 1);

                // MIDPOINT handles
                Matrix3D M = Matrix3D.Identity;
                M.Scale(new Vector3D(this.scaleCamPos, this.scaleCamPos, this.scaleCamPos));
                M.Translate(Vector3.Lerp(pos[i], pos[(i + 1) % n], 0.5f).ToVector3D());
                var m2 = new MeshGeometryModel3D()
                {
                    Geometry = MidEdgeGeometry,
                    Material = this.SelMaterial,
                    Visibility = Visibility.Visible,
                    IsHitTestVisible = true,
                    Transform = new MatrixTransform3D(M),
                };
                m2.MouseDown3D += OnMidNodeMouse3DDown;
                m2.MouseMove3D += OnMidNodeMouse3DMove;
                m2.MouseUp3D += OnMidNodeMouse3DUp;
                this.midpointHandles.Add(m2);
                this.Children.Add(m2);
                this.midPH_indInChildren.Add(this.Children.Count - 1);

                // EDGE handles
                Matrix L = calcTransf(pos[i], pos[(i + 1) % n], this.scaleCamPos * EDGE_GRIP_SIZE);
                var m3 = new MeshGeometryModel3D()
                {
                    Geometry = EdgeGeometry,
                    Material = this.materialLines,
                    Visibility = Visibility.Visible,
                    IsHitTestVisible = true,
                    Transform = new MatrixTransform3D(L.ToMatrix3D()),
                };
                m3.MouseDown3D += OnEdgeMouse3DDown;
                m3.MouseMove3D += OnEdgeMouse3DMove;
                m3.MouseUp3D += OnEdgeMouse3DUp;
                this.edgeHandles.Add(m3);
                this.Children.Add(m3);
                this.edgeH_indInChildren.Add(this.Children.Count - 1);
            }

            // lines showing Modification along axes (hidden for the moment)
            this.modifyLines = new LineGeometryModel3D()
            {
                Geometry = LineGeometry,
                Color = SharpDX.Color.White,
                Thickness = 0.5,
                Visibility = Visibility.Hidden,
                IsHitTestVisible = false,
            };
            this.Children.Add(modifyLines);
            this.posML_old = Vector3.Zero;

            // adjust for open / closed polyline
            OnIsPolyLineClosedProperty_IsFalse();

            // !!!!!!! REATTACH TO THE RENDER HOST !!!!!!!
            if (this.renderHost != null)
                Attach(this.renderHost);
        }

        protected SharpDX.Matrix calcTransf(Vector3 a, Vector3 b, float thickness = 1f)
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
                                                 0,     0,     0, 0};            
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
            Matrix Sc = Matrix.Scaling(targetL, thickness, thickness);
            Matrix L = Sc * R * Matrix.Translation(a);

            return L;
        }

        protected void OnIsPolyLineClosedProperty_IsFalse()
        {
            if (pos == null)
                return;
            int n = pos.Count;
            if (n > 0)
            {
                midpointHandles[n - 1].Visibility = Visibility.Hidden;
                midpointHandles[n - 1].IsHitTestVisible = false;
                edgeHandles[n - 1].Visibility = Visibility.Hidden;
                edgeHandles[n - 1].IsHitTestVisible = false;
            }
        }

        protected void Coords2Pos()
        {
            if (CoordsTargetIn == null)
                return;

            pos = new List<Vector3>();
            int n = CoordsTargetIn.Count;
            for (int i = 0; i < n; i++)
            {
                Point3D p = CoordsTargetIn[i];
                pos.Add(new Vector3((float)p.X, (float)p.Y, (float)p.Z));
            }

            CoordsTargetOut = new List<Point3D>();
        }

        protected void Pos2Coords()
        {
            if (pos == null)
                return;
            int n = pos.Count;
            Point3D[] raw = new Point3D[n];
            for (int i = 0; i < n; i++)
            {
                raw[i] = pos[i].ToPoint3D();
            }
            CoordsTargetOut = new List<Point3D>(raw);
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ========================================== TRANSFORMATION METHODS ====================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public void TransformModifyingLines(Vector3 posML_new)
        {
            Vector3 rv0 = new Vector3(1f, 1f, 1f);
            Vector3 rv1 = posML_new - this.posML_old;
            Vector3 scale = new Vector3(rv1.X / rv0.X, rv1.Y / rv0.Y, rv1.Z / rv0.Z);

            Matrix ML_t = Matrix.Translation(this.posML_old + 0.5f * scale);
            Matrix ML_s = Matrix.Scaling(scale);
            Matrix ML_st = Matrix.Translation(0.5f * scale);
            Matrix ML_new = ML_s * ML_t;
            this.modifyLines.Transform = new MatrixTransform3D(ML_new.ToMatrix3D());

            // update the info fields
            this.DeltaTransf = new Point3D(rv1.X, rv1.Y, rv1.Z);
        }

        // --------------------------------------------- ENDPOINT handles ----------------------------------------- //

        #region Endpoint_Handles

        protected virtual void OnNodeMouse3DDown(object sender, RoutedEventArgs e)
        { }
        protected virtual void OnNodeMouse3DUp(object sender, RoutedEventArgs e)
        { }
        protected virtual void OnNodeMouse3DMove(object sender, RoutedEventArgs e)
        { }

        protected virtual void UpdatePosOnly()
        {
            var endTransf = this.endHandles.Select(x => (x.Transform as MatrixTransform3D)).ToArray();
            var endMatrices = endTransf.Select(x => x.Value).ToArray();
            var pos_NEW = endMatrices.Select(x => x.ToMatrix().TranslationVector).ToArray();
            this.pos = new List<Vector3>(pos_NEW);
        }

        protected virtual void UpdateTransforms(object sender)
        {
            int n = this.pos.Count;
            for (int i = 0; i < n; i++)
            {
                // update the MIDPOINT handles
                Matrix3D M = Matrix3D.Identity;
                M.Scale(new Vector3D(this.scaleCamPos, this.scaleCamPos, this.scaleCamPos));
                M.Translate(Vector3.Lerp(pos[i], pos[(i + 1) % n], 0.5f).ToVector3D());
                this.midpointHandles[i].Transform = new MatrixTransform3D(M);

                // update the EDGE handels
                Matrix L = calcTransf(pos[i], pos[(i + 1) % n], this.scaleCamPos * EDGE_GRIP_SIZE);
                this.edgeHandles[i].Transform = new MatrixTransform3D(L.ToMatrix3D());

                // update the MODIFYING lines
                if (sender != null && sender == endHandles[i])
                {
                    Vector3 posML_new = pos[i];
                    TransformModifyingLines(posML_new);
                }
            }

        }

        #endregion

        // --------------------------------------------- MIDPOINT handles ----------------------------------------- //

        #region Midpoint_Handles

        protected virtual void OnMidNodeMouse3DDown(object sender, RoutedEventArgs e)
        { }
        protected virtual void OnMidNodeMouse3DUp(object sender, RoutedEventArgs e)
        { }
        protected virtual void OnMidNodeMouse3DMove(object sender, RoutedEventArgs e)
        { }

        protected virtual void UpdatePosAfterMidNode(object sender, SharpDX.Matrix T)
        {
            int n = this.pos.Count;
            for (int i = 0; i < n; i++)
            {
                if (sender == midpointHandles[i])
                {
                    // to homogeneous coordiantes
                    Vector4 v0 = this.pos[i].ToVector4(1f);
                    Vector4 v1 = this.pos[(i + 1) % n].ToVector4(1f);
                    // transform by T
                    v0 = Vector4.Transform(v0, T);
                    v1 = Vector4.Transform(v1, T);
                    // to INhomogeneous coordinates
                    this.pos[i] = v0.ToVector3();
                    this.pos[(i + 1) % n] = v1.ToVector3();
                }

            }
        }

        protected virtual void UpdateTransformsAfterMidNode(object sender)
        {
            int n = this.pos.Count;
            for (int i = 0; i < n; i++)
            {
                if (sender == midpointHandles[i])
                {
                    // update the ENDPOINT handles
                    Matrix3D Tr0 = Matrix3D.Identity;
                    Tr0.Scale(new Vector3D(this.scaleCamPos, this.scaleCamPos, this.scaleCamPos));
                    Tr0.Translate(pos[i].ToVector3D());
                    this.endHandles[i].Transform = new MatrixTransform3D(Tr0);

                    Matrix3D Tr1 = Matrix3D.Identity;
                    Tr1.Scale(new Vector3D(this.scaleCamPos, this.scaleCamPos, this.scaleCamPos));
                    Tr1.Translate(pos[(i + 1) % n].ToVector3D());
                    this.endHandles[(i + 1) % n].Transform = new MatrixTransform3D(Tr1);

                    // update the MIDPOINT handles
                    Matrix3D M = Matrix3D.Identity;
                    M.Scale(new Vector3D(this.scaleCamPos, this.scaleCamPos, this.scaleCamPos));
                    M.Translate(Vector3.Lerp(pos[i], pos[(i + 1) % n], 0.5f).ToVector3D());
                    this.midpointHandles[i].Transform = new MatrixTransform3D(M);

                    Matrix3D M_prev = Matrix3D.Identity;
                    M_prev.Scale(new Vector3D(this.scaleCamPos, this.scaleCamPos, this.scaleCamPos));
                    M_prev.Translate(Vector3.Lerp(pos[(n + i - 1) % n], pos[i], 0.5f).ToVector3D());
                    this.midpointHandles[(n + i - 1) % n].Transform = new MatrixTransform3D(M_prev);

                    Matrix3D M_next = Matrix3D.Identity;
                    M_next.Scale(new Vector3D(this.scaleCamPos, this.scaleCamPos, this.scaleCamPos));
                    M_next.Translate(Vector3.Lerp(pos[(i + 1) % n], pos[(i + 2) % n], 0.5f).ToVector3D());
                    this.midpointHandles[(i + 1) % n].Transform = new MatrixTransform3D(M_next);

                    // update the EDGE handles
                    Matrix L = calcTransf(pos[i], pos[(i + 1) % n], this.scaleCamPos * EDGE_GRIP_SIZE);
                    this.edgeHandles[i].Transform = new MatrixTransform3D(L.ToMatrix3D());

                    Matrix L_prev = calcTransf(pos[(n + i - 1) % n], pos[i], this.scaleCamPos * EDGE_GRIP_SIZE);
                    this.edgeHandles[(n + i - 1) % n].Transform = new MatrixTransform3D(L_prev.ToMatrix3D());

                    Matrix L_next = calcTransf(pos[(i + 1) % n], pos[(i + 2) % n], this.scaleCamPos * EDGE_GRIP_SIZE);
                    this.edgeHandles[(i + 1) % n].Transform = new MatrixTransform3D(L_next.ToMatrix3D());

                    // update modify LinesIn 
                    Vector3 posML_new = Vector3.Lerp(pos[i], pos[(i + 1) % n], 0.5f);
                    TransformModifyingLines(posML_new);
                }
            }
        }

        #endregion

        // ----------------------------------------------- EDGE handles ------------------------------------------- //

        #region Edge_Handles
        protected virtual void OnEdgeMouse3DDown(object sender, RoutedEventArgs e)
        { }
        protected virtual void OnEdgeMouse3DUp(object sender, RoutedEventArgs e)
        { }
        protected virtual void OnEdgeMouse3DMove(object sender, RoutedEventArgs e)
        { }

        protected virtual void UpdatePosAfterEdge(SharpDX.Matrix T)
        {
            // get old positions
            int n = this.pos.Count;
            Vector4[] ph = new Vector4[n];

            // to homogeneous coordiantes
            for (int i = 0; i < n; i++)
            {
                ph[i] = this.pos[i].ToVector4(1f);
            }
            // transform by T            
            Vector4.Transform(ph, ref T, ph);

            // to inhomogeneous coordinates
            for (int i = 0; i < n; i++)
            {
                this.pos[i] = ph[i].ToVector3();
            }
        }

        protected virtual void UpdateTransformsAfterEdge()
        {
            int n = pos.Count;
            for (int i = 0; i < n; i++)
            {
                // update the ENDPOINT handles
                Matrix3D Tr = Matrix3D.Identity;
                Tr.Scale(new Vector3D(this.scaleCamPos, this.scaleCamPos, this.scaleCamPos));
                Tr.Translate(pos[i].ToVector3D());
                this.endHandles[i].Transform = new MatrixTransform3D(Tr);

                // update the MIDPOINT handles
                Matrix3D M = Matrix3D.Identity;
                M.Scale(new Vector3D(this.scaleCamPos, this.scaleCamPos, this.scaleCamPos));
                M.Translate(Vector3.Lerp(pos[i], pos[(i + 1) % n], 0.5f).ToVector3D());
                this.midpointHandles[i].Transform = new MatrixTransform3D(M);

                // update the EDGE handles
                Matrix L = calcTransf(pos[i], pos[(i + 1) % n], this.scaleCamPos * EDGE_GRIP_SIZE);
                this.edgeHandles[i].Transform = new MatrixTransform3D(L.ToMatrix3D());
            }
            
        }

        #endregion

        // ------------------------------------------------- OVERRIDES -------------------------------------------- //

        public override void Attach(IRenderHost host)
        {
            base.Attach(host);
        }

        protected override BoundingBox GetBounds()
        {
            if (this.pos.Count < 1)
                return new BoundingBox();
            else
                return BoundingBox.FromPoints(this.pos.ToArray());
        }

    }
}
