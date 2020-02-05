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

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using Geometry3D = HelixToolkit.SharpDX.Wpf.Geometry3D;
using SharpDX;

using GeometryViewer.Utils;

namespace GeometryViewer.SpatialOrganization
{
    class OcTreeManager : GroupModel3D
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ============================================== STATIC ELEMENTS ========================================= //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region STATIC: Constants

        private static readonly double SNAPPOSMARKER_FACT = 0.04;
        private static readonly float SNAP_ORTHOFACT = 0.15f;
        private static readonly double VIS_OFFSET = 0.1;

        private static readonly LineGeometry3D CellFrame;
        private static readonly LineGeometry3D EndPoint;
        private static readonly LineGeometry3D MidPoint;
        private static readonly LineGeometry3D IntPoint;

        private static readonly HelixToolkit.SharpDX.Wpf.MeshGeometry3D ChamferedBoxMesh;
        private static readonly LineGeometry3D ChamferedBoxMesh_Normals;

        private static readonly PhongMaterial RedTransparent;
        private static readonly PhongMaterial YellowTransparent;

        #endregion

        #region STATIC: Initializer

        static OcTreeManager()
        {
            LineBuilder b = new LineBuilder();
            b.AddBox(Vector3.Zero, 1, 1, 1);
            CellFrame = b.ToLineGeometry3D();

            EndPoint = LinesCutom.GetEndPointMarker(Vector3.Zero, (float)SNAPPOSMARKER_FACT * 0.8f);
            MidPoint = LinesCutom.GetMidPointMarker(Vector3.Zero, (float)SNAPPOSMARKER_FACT);
            IntPoint = LinesCutom.GetIntersectionMarker(Vector3.Zero, (float)SNAPPOSMARKER_FACT);
            
            ChamferedBoxMesh = MeshesCustom.GetChamferedBox(Vector3.Zero, 1f, 1f, 1f, 0.2f, 0.05f);
            MeshesCustom.CompressMesh(ref ChamferedBoxMesh);

            ChamferedBoxMesh_Normals = MeshesCustom.GetVertexNormalsAsLines(ChamferedBoxMesh, 0.15f);

            RedTransparent = new PhongMaterial();
            RedTransparent.DiffuseColor = new Color4(0.8f, 0f, 0f, 0.25f);
            RedTransparent.AmbientColor = new Color4(0.6f, 0f, 0f, 1f);
            RedTransparent.SpecularColor = new Color4(1f, 0.75f, 0f, 1f);
            RedTransparent.SpecularShininess = 1;

            YellowTransparent = new PhongMaterial();
            YellowTransparent.DiffuseColor = new Color4(1f, 0.93f, 0f, 0.5f);
            YellowTransparent.AmbientColor = new Color4(0.92f, 0.69f, 0f, 1f);
            YellowTransparent.SpecularColor = new Color4(1f, 1f, 1f, 1f);
            YellowTransparent.SpecularShininess = 3;
  
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ==================================== DEPENDENCY PROPERTIES FOR BINDING ================================= //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Volume

        public double MinVolumeSize
        {
            get { return (double)GetValue(MinVolumeSizeProperty); }
            set { SetValue(MinVolumeSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MinVolumeSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MinVolumeSizeProperty =
            DependencyProperty.Register("MinVolumeSize", typeof(double), typeof(OcTreeManager),
            new UIPropertyMetadata(1.0));

        #endregion

        #region Contained_Objects

        public List<Utils.LineWHistory> LinesIn
        {
            get { return (List<Utils.LineWHistory>)GetValue(LinesInProperty); }
            set { SetValue(LinesInProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LinesIn.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LinesInProperty =
            DependencyProperty.Register("LinesIn", typeof(List<Utils.LineWHistory>), typeof(OcTreeManager),
            new UIPropertyMetadata(new List<Utils.LineWHistory>(),
                                   new PropertyChangedCallback(MyLinesInPropertyChangedCallback),
                                   new CoerceValueCallback(MyLinesInCoerceValueCallback)));

        private static void MyLinesInPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OcTreeManager otm = d as OcTreeManager;
            if (otm != null)
            {
                List<Utils.LineWHistory> lines = e.NewValue as List<Utils.LineWHistory>;
                if (lines != null)
                {
                    if (!otm.root.TreeBuilt)
                        otm.root.BuildTree(otm.MinVolumeSize, lines);
                    else
                        otm.root.UpdateTree(lines);
                    //// DEBUG
                    //otm.root.BuildTree(otm.MinVolumeSize, lines);

                    // DEBUG
                    var test = otm.root.ToString();

                    otm.UpdateGeometry();
                }
            }
        }
        private static object MyLinesInCoerceValueCallback(DependencyObject d, object value)
        {
            //var test1 = d;
            //var test2 = value;
            return value;
        }

        #endregion

        #region Appearance

        public bool ShowTree
        {
            get { return (bool)GetValue(ShowTreeProperty); }
            set { SetValue(ShowTreeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowTree.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowTreeProperty =
            DependencyProperty.Register("ShowTree", typeof(bool), typeof(OcTreeManager), 
            new UIPropertyMetadata(true, new PropertyChangedCallback(MyShowTreePropertyChangedCallback)));

        private static void MyShowTreePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
 	        OcTreeManager otm = d as OcTreeManager;
            if (otm != null)
            {
                if (otm.ShowTree)
                {
                    otm.cell.Visibility = Visibility.Visible;
                    otm.colPointMarker.Visibility = Visibility.Visible;
                }
                else
                {
                    otm.cell.Visibility = Visibility.Collapsed;
                    otm.colPointMarker.Visibility = Visibility.Collapsed;
                }
            }
        }


        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ========================================== INTERNAL CLASS MEMBERS ====================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region CLASS MEMBERS

        private OcTree root;
        private LineGeometryModel3D cell;
        private MeshGeometryModel3D cellFill;
        private MeshGeometryModel3D cellVisible;
        private LineGeometryModel3D cellVisibleNormals; // DEBUG
        private LineGeometryModel3D colPointMarker;
        private LineGeometryModel3D snapMarker;


        // object snap
        public List<Point3D> PointsCollision { get; private set; }
        public List<Point3D> PointsVisible_End { get; private set; }
        public List<Point3D> PointsVisible_Mid { get; private set; }

        public ICommand CheckForCollisionsSimpleCmd { get; private set; }
        public ICommand CheckForCollisionsCmd { get; private set; }
        public ICommand CheckForVisibilitySimpleCmd { get; private set; }
        public ICommand CheckForVisibilityCmd { get; private set; }
        public ICommand UpdateOcTreeCmd { get; private set; }
        public ICommand CleanVisualAidsCmd { get; private set; }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // =============================================== CONSTRUCTORS =========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region CONSTRUCTOR
        public OcTreeManager()
        {
            this.root = new OcTree();

            // OcTree CELL visualization
            this.cell = new LineGeometryModel3D()
            {
                Geometry = OcTreeManager.CellFrame,
                Color = SharpDX.Color.White,
                Thickness = 0.5,
                Visibility = Visibility.Visible,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
            };
            this.Children.Add(cell);

            // Collision MARKERS visualization
            this.colPointMarker = new LineGeometryModel3D()
            {
                Geometry = OcTreeManager.IntPoint,
                Color = SharpDX.Color.Red,
                Thickness = 1.5,
                Visibility = Visibility.Visible,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
            };
            this.Children.Add(colPointMarker);

            // Collision REGIONS visualization
            this.cellFill = new MeshGeometryModel3D()
            {
                Geometry = OcTreeManager.ChamferedBoxMesh,
                Material = OcTreeManager.RedTransparent,
                Visibility = Visibility.Visible,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
            };
            this.Children.Add(cellFill);

            // Visible REGIONS visualization
            this.cellVisible = new MeshGeometryModel3D()
            {
                Geometry = OcTreeManager.ChamferedBoxMesh,
                Material = OcTreeManager.YellowTransparent,
                Visibility = Visibility.Visible,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
            };
            this.Children.Add(cellVisible);

            // DEBUG
            this.cellVisibleNormals = new LineGeometryModel3D()
            {
                Geometry = OcTreeManager.ChamferedBoxMesh_Normals,
                Color = Color.DarkBlue,
                Thickness = 0.5,
                Visibility = Visibility.Hidden,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
            };
            this.Children.Add(cellVisibleNormals);
            // DEBUG

            // ------------------------------------ OBJECT SNAP -------------------------------------- //
            this.PointsCollision = new List<Point3D>();
            this.PointsVisible_End = new List<Point3D>();
            this.PointsVisible_Mid = new List<Point3D>();
            this.snapMarker = new LineGeometryModel3D()
            {
                Geometry = OcTreeManager.EndPoint,
                Color = SharpDX.Color.Yellow,
                Thickness = 1,
                Visibility = Visibility.Hidden,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
            };
            this.Children.Add(this.snapMarker);
            // ------------------------------------ OBJECT SNAP -------------------------------------- //
            
            // commands
            this.CheckForCollisionsSimpleCmd = new RelayCommand((x) => CheckForCollisionSimpleCommand(), (x) => CanExecute_CheckForCollisionsCommand());
            this.CheckForCollisionsCmd = new RelayCommand((x) => CheckForCollisionsCommand(), (x) => CanExecute_CheckForCollisionsCommand());

            this.CheckForVisibilitySimpleCmd = new RelayCommand((x) => CheckForVisibilitySimpleCommand(x), (x) => CanExecute_CheckForVisibilityCommand(x));
            this.CheckForVisibilityCmd = new RelayCommand((x) => CheckForVisibilityCommand(x), (x) => CanExecute_CheckForVisibilityCommand(x));
            
            this.UpdateOcTreeCmd = new RelayCommand((x) => UpdateOcTree());
            this.CleanVisualAidsCmd = new RelayCommand((x) => CleanUpCells());
        }
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ================================================== METHODS ============================================= //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Update Methods
        private void UpdateGeometry()
        {
            List<BoundingBox> regionList = this.root.GetRegionList();
            List<SharpDX.Matrix> instances = new List<Matrix>();
            foreach(var region in regionList)
            {
                Vector3 dimensions = region.Maximum - region.Minimum;
                Vector3 half = dimensions / 2f;
                Vector3 center = region.Minimum + half;
                SharpDX.Matrix Sc = SharpDX.Matrix.Scaling(dimensions);
                SharpDX.Matrix Tr = SharpDX.Matrix.Translation(center);
                instances.Add(Sc * Tr);
            }
            this.cell.Instances = instances;
            
            this.cellFill.Instances = new List<SharpDX.Matrix>();
            this.cellFill.Visibility = Visibility.Hidden;

            this.cellVisible.Instances = new List<SharpDX.Matrix>();
            this.cellVisible.Visibility = Visibility.Hidden;
            this.cellVisibleNormals.Instances = new List<SharpDX.Matrix>();
            this.cellVisibleNormals.Visibility = Visibility.Hidden;
        }

        private void UpdateOcTree()
        {
            this.root.BuildTree(OcTree.VOLUME_SIZE_MAX, this.LinesIn);
            UpdateGeometry();
        }

        private void CleanUpCells()
        {
            this.cellFill.Instances = new List<SharpDX.Matrix>();
            this.cellFill.Visibility = Visibility.Hidden;

            this.cellVisible.Instances = new List<SharpDX.Matrix>();
            this.cellVisible.Visibility = Visibility.Hidden;
            this.cellVisibleNormals.Instances = new List<SharpDX.Matrix>();
            this.cellVisibleNormals.Visibility = Visibility.Hidden;   
        }
        #endregion

        #region Collision / Visibility

        private void CheckForCollisionSimpleCommand()
        {
            List<BoundingBox> regions = new List<BoundingBox>();
            List<Vector3> colPos = new List<Vector3>();
            this.root.GetCollisionRegions(ref regions, ref colPos, 0.01);
            this.PointsCollision = new List<Point3D>(CommonExtensions.ConvertVector3ListToPoint3DList(colPos));
        }

        private void CheckForCollisionsCommand()
        {
            List<BoundingBox> regions = new List<BoundingBox>();
            List<Vector3> colPos = new List<Vector3>();
            this.root.GetCollisionRegions(ref regions, ref colPos, 0.01);
            this.PointsCollision = new List<Point3D>(CommonExtensions.ConvertVector3ListToPoint3DList(colPos));

            if (regions.Count > 0)
            {
                // the line marker FIRST, because the of the TRANSPARENCY SORTING
                List<SharpDX.Matrix> instances_LM = new List<Matrix>();
                foreach (var pos in colPos)
                {
                    SharpDX.Matrix Tr = SharpDX.Matrix.Translation(pos);
                    instances_LM.Add(Tr);
                }
                this.colPointMarker.Instances = instances_LM;
                this.colPointMarker.Visibility = Visibility.Visible;

                // cell marker SECOND
                List<SharpDX.Matrix> instances_CM = new List<Matrix>();
                int rn = regions.Count;
                for (int i = rn - 1; i >= 0; i--)
                {
                    BoundingBox region = regions[i];
                    Vector3 dimensions = region.Maximum - region.Minimum;
                    Vector3 half = dimensions / 2f;
                    Vector3 center = region.Minimum + half;
                    SharpDX.Matrix Sc = SharpDX.Matrix.Scaling(dimensions - new Vector3((float)VIS_OFFSET));
                    SharpDX.Matrix Tr = SharpDX.Matrix.Translation(center);
                    instances_CM.Add(Sc * Tr);
                }
                this.cellFill.Instances = instances_CM;
                this.cellFill.Visibility = Visibility.Visible;
            }
            else
            {
                this.cellFill.Instances = new List<Matrix>();
                this.cellFill.Visibility = Visibility.Hidden;
                
                this.colPointMarker.Instances = new List<Matrix>();
                this.colPointMarker.Visibility = Visibility.Hidden;
            }
        }

        private bool CanExecute_CheckForCollisionsCommand()
        {
            return (this.root.TreeBuilt && this.LinesIn.Count > 1);
        }


        private void CheckForVisibilitySimpleCommand(object _vff)
        {
            ViewFrustumFunctions viewFF = _vff as ViewFrustumFunctions;
            if (viewFF == null)
                return;

            List<BoundingBox> regions = new List<BoundingBox>();
            List<Point3D> newPointsVisible_End = new List<Point3D>();
            List<Point3D> newPointsVisible_Mid = new List<Point3D>();
            this.root.GetVisibleRegions(ref regions, ref newPointsVisible_End, ref newPointsVisible_Mid, viewFF);
            this.PointsVisible_End = new List<Point3D>(newPointsVisible_End);
            this.PointsVisible_Mid = new List<Point3D>(newPointsVisible_Mid);
        }

        private void CheckForVisibilityCommand(object _vff)
        {
            ViewFrustumFunctions viewFF = _vff as ViewFrustumFunctions;
            if (viewFF == null)
                return;

            List<BoundingBox> regions = new List<BoundingBox>();
            List<Point3D> newPointsVisible_End = new List<Point3D>();
            List<Point3D> newPointsVisible_Mid = new List<Point3D>();
            this.root.GetVisibleRegions(ref regions, ref newPointsVisible_End, ref newPointsVisible_Mid, viewFF);
            this.PointsVisible_End = new List<Point3D>(newPointsVisible_End);
            this.PointsVisible_Mid = new List<Point3D>(newPointsVisible_Mid);

            if (regions.Count > 0)
            {
                List<SharpDX.Matrix> instances = new List<Matrix>();
                int rn = regions.Count;
                for (int i = rn - 1; i >= 0; i--)
                {
                    BoundingBox region = regions[i];
                    Vector3 dimensions = region.Maximum - region.Minimum;
                    Vector3 half = dimensions / 2f;
                    Vector3 center = region.Minimum + half;
                    SharpDX.Matrix Sc = SharpDX.Matrix.Scaling(dimensions - new Vector3((float)VIS_OFFSET));
                    SharpDX.Matrix Tr = SharpDX.Matrix.Translation(center);
                    instances.Add(Sc * Tr);
                }               
                this.cellVisible.Instances = instances;
                this.cellVisible.Visibility = Visibility.Visible;
                this.cellVisibleNormals.Instances = instances;
                this.cellVisibleNormals.Visibility = Visibility.Visible;
            }
            else
            {
                this.cellVisible.Instances = new List<SharpDX.Matrix>();
                this.cellVisible.Visibility = Visibility.Hidden;
                this.cellVisibleNormals.Instances = new List<SharpDX.Matrix>();
                this.cellVisibleNormals.Visibility = Visibility.Hidden;
            }
        }

        private bool CanExecute_CheckForVisibilityCommand(object _vff)
        {
            return (this.root.TreeBuilt && this.LinesIn.Count > 1 && _vff != null);
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ============================================ OBJECT SNAP HANDLING ====================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region OBJECT SNAP

        // OLD: NOT IN USE
        public int OnMouseMove(Point _currentHit, Viewport3DXext _vp, out Point3D snapPoint, int _pixelTolerance = 20)
        {
            snapPoint = new Point3D(0, 0, 0);

            if (_currentHit == null || _vp == null)
                return -1;

            // test the potential candidates
            int n = this.PointsVisible_End.Count;
            if (n == 0)
                return -1;

            for (int i = 0; i < n; i++)
            {
                // device coordinates
                // lower left: (-1,-1), upper right: (1,1)
                Point test = _vp.Project(this.PointsVisible_End[i]);
                if (test != null)
                {
                    // viewport pixel coordinates
                    // screen upper left: (0,0), lower right (width, height)
                    Point test_PixelCoords = new Point((1 + test.X) * 0.5 * _vp.ActualWidth, (1 - test.Y) * 0.5 * _vp.ActualHeight);
                    double dist = Math.Abs(_currentHit.X - test_PixelCoords.X) + Math.Abs(_currentHit.Y - test_PixelCoords.Y);
                    if (dist < _pixelTolerance)
                    {
                        // snap found
                        Vector3D offset = this.PointsVisible_End[i] - new Point3D(0, 0, 0);
                        double scale = Vector3.Distance(this.PointsVisible_End[i].ToVector3(), _vp.Camera.Position.ToVector3());
                        float factOrthoCam = ((_vp.Camera as HelixToolkit.SharpDX.OrthographicCamera) != null) ? SNAP_ORTHOFACT : 1.0f;
                        scale *= factOrthoCam;

                        Matrix3D M = Matrix3D.Identity;
                        M.Scale(new Vector3D(scale, scale, scale));
                        M.Translate(offset);
                        this.snapMarker.Transform = new MatrixTransform3D(M);
                        this.snapMarker.Visibility = Visibility.Visible;
                        
                        snapPoint = this.PointsVisible_End[i];
                        return i;
                    }
                    else
                    {
                        this.snapMarker.Transform = new MatrixTransform3D(Matrix3D.Identity);
                        this.snapMarker.Visibility = Visibility.Hidden;
                    }
                }
            }

            return -1;

        }

        public int OnMouseMove(Point _currentHit, Viewport3DXext _vp, out Point3D snapPoint, 
                               bool _testEnd, bool _testMid, bool _testInters, int _pixelTolerance = 20)
        {
            int foundIndex = -1;
            snapPoint = new Point3D(0, 0, 0);

            if (_currentHit == null || _vp == null)
                return foundIndex;

            // test points priority: END > INTERSECTION > MIDPOINT
            if (_testEnd)
            {
                foundIndex = OcTreeManager.SnapTestPoints(_currentHit, _vp, out snapPoint, _pixelTolerance,
                                        this.PointsVisible_End);
                if (foundIndex != -1)
                    this.snapMarker.Geometry = OcTreeManager.EndPoint;
            }

            if (_testInters && foundIndex == -1)
            {
                foundIndex = OcTreeManager.SnapTestPoints(_currentHit, _vp, out snapPoint, _pixelTolerance,
                                        this.PointsCollision);
                if (foundIndex != -1)
                    this.snapMarker.Geometry = OcTreeManager.IntPoint;
            }

            if (_testMid && foundIndex == -1)
            {
                foundIndex = OcTreeManager.SnapTestPoints(_currentHit, _vp, out snapPoint, _pixelTolerance,
                                        this.PointsVisible_Mid);
                if (foundIndex != -1)
                    this.snapMarker.Geometry = OcTreeManager.MidPoint;
            }

            // process display
            if (foundIndex != -1)
            {
                // snap found
                Vector3D offset = snapPoint - new Point3D(0, 0, 0);
                double scale = Vector3.Distance(snapPoint.ToVector3(), _vp.Camera.Position.ToVector3());
                float factOrthoCam = ((_vp.Camera as HelixToolkit.SharpDX.OrthographicCamera) != null) ? SNAP_ORTHOFACT : 1.0f;
                scale *= factOrthoCam;                        

                Matrix3D M = Matrix3D.Identity;
                M.Scale(new Vector3D(scale, scale, scale));
                M.Translate(offset);

                this.snapMarker.Transform = new MatrixTransform3D(M);
                this.snapMarker.Visibility = Visibility.Visible;
            }
            else
            {
                this.snapMarker.Transform = new MatrixTransform3D(Matrix3D.Identity);
                this.snapMarker.Visibility = Visibility.Hidden;
            }

            return foundIndex;
        }


        private static int SnapTestPoints(Point _currentHit, Viewport3DXext _vp, out Point3D snapPoint, int _pixelTolerance,
                                        List<Point3D> _pointsToTest)
        {
            snapPoint = new Point3D(0, 0, 0);

            if (_currentHit == null || _vp == null || _pointsToTest == null)
                return -1;

            // test the potential candidates
            int n = _pointsToTest.Count;
            if (n == 0)
                return -1;

            for (int i = 0; i < n; i++)
            {
                // device coordinates
                // lower left: (-1,-1), upper right: (1,1)
                Point test = _vp.Project(_pointsToTest[i]);
                if (test != null)
                {
                    // viewport pixel coordinates
                    // screen upper left: (0,0), lower right (width, height)
                    Point test_PixelCoords = new Point((1 + test.X) * 0.5 * _vp.ActualWidth, (1 - test.Y) * 0.5 * _vp.ActualHeight);
                    double dist = Math.Abs(_currentHit.X - test_PixelCoords.X) + Math.Abs(_currentHit.Y - test_PixelCoords.Y);
                    if (dist < _pixelTolerance)
                    {
                        snapPoint = _pointsToTest[i];
                        return i;
                    }
                }
            }

            return -1;
        }



        #endregion


    }
}
