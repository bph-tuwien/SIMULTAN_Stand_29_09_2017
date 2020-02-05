using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using System.Windows.Input;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;

using GeometryViewer.Utils;
using GeometryViewer.EntityGeometry;

namespace GeometryViewer
{
    public enum TriangulationStage
    {
        INIT,
        SPLIT_ALONG_HOLES,
        SPLIT_INTO_MONOTONE_POLYGONS,
        TRIANGULATE_MONOTONE_POLYGONS,
        SHOW_RESULT,
        RESET
    }

    public class TriangulationDisplay : GroupModel3D
    {
        #region CLASS MEMBERS

        private List<ZonedPolygon> test_polygons;
        
        // stage: init
        private int outer_polygon_index;
        private List<Point3D> outer_polygon;
        private List<List<Point3D>> holes;

        // stage: split along holes
        private List<Point3D> splittingPath;
        private List<List<Point3D>> simplePolys;

        // stage: split into monotone polygons
        private List<List<Point3D>> monotonePolys;

        // stage: triangulate the monotone polygons
        private List<HelixToolkit.SharpDX.Wpf.MeshGeometry3D> triangles;

        // stage: show final result
        private HelixToolkit.SharpDX.Wpf.MeshGeometry3D fill;

        private MeshGeometryModel3D triangulation_full;

        private LineGeometryModel3D lines_polygons;
        private LineGeometryModel3D lines_connecting_for_split;
        private LineGeometryModel3D lines_connecting_for_split_stepwise;
        private LineGeometryModel3D lines_connecting_for_split_stepwise2;
        private LineGeometryModel3D lines_connecting_for_split_stepwise3;
        private LineGeometryModel3D lines_connecting_for_split_stepwise4;
        private LineGeometryModel3D lines_split_along_holes;
        private LineGeometryModel3D lines_split_to_monotone_polys;
        private LineGeometryModel3D lines_triangulation_of_monotone_polys;

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ================================================ PROPERTIES ============================================ //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        #region PROPERTIES: Triangulation Stage

        private TriangulationStage t_stage;
        public TriangulationStage TStage
        {
            get { return this.t_stage; }
            set
            {
                this.t_stage = value;
                this.UpdateGeometry();
            }
        }

        public ICommand TestPathFindingForSplitCmd { get; private set; }
        public ICommand TestPathFindingForSplit2Cmd { get; private set; }
        public ICommand SetTriangulationStageSplitAlongHolesCmd { get; private set; }
        public ICommand SetTriangulationStageSplitIntoMonotoneCmd { get; private set; }
        public ICommand SetTriangulationStageTriangulateMonotoneCmd { get; private set; }
        public ICommand SetTriangulationStageShowResultCmd { get; private set; }
        public ICommand ResetAllCmd { get; private set; }

        #endregion

        #region DEPENDENCY PROPETIES


        public bool IsPickingPolygons
        {
            get { return (bool)GetValue(IsPickingPolygonsProperty); }
            set { SetValue(IsPickingPolygonsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsPickingPolygons.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsPickingPolygonsProperty =
            DependencyProperty.Register("IsPickingPolygons", typeof(bool), typeof(TriangulationDisplay),
            new UIPropertyMetadata(false, new PropertyChangedCallback(IsPickingPolygonsPropertyChangedCallback)));

        private static void IsPickingPolygonsPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TriangulationDisplay instance = d as TriangulationDisplay;
            if (instance == null) return;

            if (instance.IsPickingPolygons)
            {
                instance.ResetAll();
                instance.test_polygons = new List<ZonedPolygon>();
                instance.CanTestTriangulation = false;
            }
            else
            {
                if (instance.test_polygons != null && instance.test_polygons.Count > 0)
                {
                    instance.t_stage = TriangulationStage.INIT;
                    instance.CanTestTriangulation = true;
                    instance.ResetGeometry();
                    instance.SeparateIntoContourAndHoles();
                    instance.UpdateGeometry();
                }
            }
                
        }


        public ZonedPolygon SelectedPolygon
        {
            get { return (ZonedPolygon)GetValue(SelectedPolygonProperty); }
            set { SetValue(SelectedPolygonProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedPolygon.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedPolygonProperty =
            DependencyProperty.Register("SelectedPolygon", typeof(ZonedPolygon), typeof(TriangulationDisplay),
            new UIPropertyMetadata(null, new PropertyChangedCallback(SelectedPolygonPropertyChangedCallback),
                new CoerceValueCallback(SelectedPolygonCoerceValueCallback)));

        private static object SelectedPolygonCoerceValueCallback(DependencyObject d, object baseValue)
        {
            TriangulationDisplay instance = d as TriangulationDisplay;
            if (instance == null) return baseValue;

            if (!(baseValue is ZonedPolygon)) return null;

            return baseValue as ZonedPolygon;
        }

        private static void SelectedPolygonPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TriangulationDisplay instance = d as TriangulationDisplay;
            if (instance == null) return;

            if (instance.SelectedPolygon != null)
            {
                if (instance.test_polygons == null)
                    instance.test_polygons = new List<ZonedPolygon>();
                instance.test_polygons.Add(instance.SelectedPolygon);
            }
        }



        public bool CanTestTriangulation
        {
            get { return (bool)GetValue(CanTestTriangulationProperty); }
            set { SetValue(CanTestTriangulationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CanTestTriangulation.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CanTestTriangulationProperty =
            DependencyProperty.Register("CanTestTriangulation", typeof(bool), typeof(TriangulationDisplay), new UIPropertyMetadata(false));




        public int PolygonIndexToInspect
        {
            get { return (int)GetValue(PolygonIndexToInspectProperty); }
            set { SetValue(PolygonIndexToInspectProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PolygonIndexToInspect.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PolygonIndexToInspectProperty =
            DependencyProperty.Register("PolygonIndexToInspect", typeof(int), typeof(TriangulationDisplay),
            new UIPropertyMetadata(-1, new PropertyChangedCallback(PolygonIndexToInspectPropertyChangedCallback)));

        private static void PolygonIndexToInspectPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TriangulationDisplay instance = d as TriangulationDisplay;
            if (instance == null) return;

            if (instance.TStage == TriangulationStage.TRIANGULATE_MONOTONE_POLYGONS)
            {
                if (instance.triangles != null)
                {
                    LineBuilder lb = new LineBuilder();
                    List<List<Vector3>> triangle_coords = instance.triangles.Select(x => x.Positions.ToList()).ToList();
                    List<List<Point3D>> triangle_coords_P3D = CommonExtensions.ConvertVector3ListListToPoint3DListList(triangle_coords);

                    if (0 <= instance.PolygonIndexToInspect && instance.PolygonIndexToInspect < instance.triangles.Count)
                        TriangulationDisplay.AddLines(triangle_coords_P3D[instance.PolygonIndexToInspect], true, ref lb);
                    else
                        TriangulationDisplay.AddLines(triangle_coords_P3D, true, ref lb);

                    instance.lines_triangulation_of_monotone_polys.Geometry = lb.ToLineGeometry3D();
                }
            }
        }


        #endregion

        #region DEPENDENCY PROPERTIES: Algorithm Breakdown

        public int TestPathFindingStepNr
        {
            get { return (int)GetValue(TestPathFindingStepNrProperty); }
            set { SetValue(TestPathFindingStepNrProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TestPathFindingStepNr.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TestPathFindingStepNrProperty =
            DependencyProperty.Register("TestPathFindingStepNr", typeof(int), typeof(TriangulationDisplay), new UIPropertyMetadata(0));



        public int TestPathFindingStepNr2
        {
            get { return (int)GetValue(TestPathFindingStepNr2Property); }
            set { SetValue(TestPathFindingStepNr2Property, value); }
        }

        // Using a DependencyProperty as the backing store for TestPathFindingStepNr2.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TestPathFindingStepNr2Property =
            DependencyProperty.Register("TestPathFindingStepNr2", typeof(int), typeof(TriangulationDisplay), new UIPropertyMetadata(0));



        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ============================================== INITIALIZATION ========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region STATIC

        public static readonly PhongMaterial MAT_MESH;
        public static readonly PhongMaterial MAT_MESH_1;
        public static readonly PhongMaterial MAT_MESH_2;
        public static readonly Color COL_DEFAULT;
        public static readonly Color COL_SUB_1;
        public static readonly Color COL_SUB_2;
        public static readonly Color COL_HIGHLIGHT;

        static TriangulationDisplay()
        {
            MAT_MESH = new PhongMaterial();
            MAT_MESH.DiffuseColor = new Color4(0.5f, 0.5f, 0.5f, 0.5f);

            MAT_MESH_1 = new PhongMaterial();
            MAT_MESH.DiffuseColor = new Color4(0.2f, 0.1f, 0.0f, 0.5f);

            MAT_MESH_2 = new PhongMaterial();
            MAT_MESH.DiffuseColor = new Color4(0.0f, 0.0f, 0.2f, 0.5f);

            COL_DEFAULT = Color.Black;
            COL_SUB_1 = Color.Brown;
            COL_SUB_2 = Color.Blue;
            COL_HIGHLIGHT = Color.White;
        }

        private static void AddLines(List<Point3D> _poly, bool _close, ref LineBuilder _lb)
        {
            if (_poly == null) return;
            if (_poly.Count < 2) return;

            int n = _poly.Count;
            if (_close)
            {
                for (int i = 0; i < n; i++)
                {
                    _lb.AddLine(_poly[i].ToVector3(), _poly[(i + 1) % n].ToVector3());
                }
            }
            else
            {
                for (int i = 0; i < n - 1; i++)
                {
                    _lb.AddLine(_poly[i].ToVector3(), _poly[i + 1].ToVector3());
                }
            }            
        }

        private static void AddLines(List<List<Point3D>> _polys, bool _close, ref LineBuilder _lb)
        {
            if (_polys == null) return;
            if (_polys.Count == 0) return;

            foreach(List<Point3D> poly in _polys)
            {
                TriangulationDisplay.AddLines(poly, _close, ref _lb);
            }
        }

        private static void AddSeparateLines(List<Point3D> _lines, ref LineBuilder _lb)
        {
            if (_lines == null) return;
            if (_lines.Count < 2) return;

            int n = _lines.Count;
            for (int i = 0; i < n - 1; i += 2)
            {
                _lb.AddLine(_lines[i].ToVector3(), _lines[i + 1].ToVector3());
            }
        }

        private static List<List<Point3D>> ExtractTrianglesFromMesh(HelixToolkit.SharpDX.Wpf.MeshGeometry3D _mesh)
        {
            List<List<Point3D>> triangles = new List<List<Point3D>>();
            if (_mesh == null) return triangles;
            if (_mesh.Positions == null || _mesh.Indices == null) return triangles;
            if (_mesh.Positions.Length == 0 || _mesh.Indices.Length == 0) return triangles;

            for(int i = 0; i < _mesh.Indices.Length - 2; i += 3)
            {
                List<Point3D> tri = new List<Point3D>
                {
                    _mesh.Positions[_mesh.Indices[i]].ToPoint3D(),
                    _mesh.Positions[_mesh.Indices[i + 1]].ToPoint3D(),
                    _mesh.Positions[_mesh.Indices[i + 2]].ToPoint3D(),
                };
                triangles.Add(tri);
            }

            return triangles;
        }

        #endregion

        #region .CTOR

        public TriangulationDisplay()
        {
            // geometry
            this.triangulation_full = new MeshGeometryModel3D()
            {
                Geometry = null,
                Material = TriangulationDisplay.MAT_MESH,
                Visibility = System.Windows.Visibility.Visible,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
            };
            this.Children.Add(this.triangulation_full);

            this.lines_polygons = new LineGeometryModel3D()
            {
                Geometry = null,
                Color = TriangulationDisplay.COL_DEFAULT,
                Thickness = 2,
                Visibility = System.Windows.Visibility.Visible,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
            };
            this.Children.Add(this.lines_polygons);

            this.lines_connecting_for_split = new LineGeometryModel3D()
            {
                Geometry = null,
                Color = TriangulationDisplay.COL_HIGHLIGHT,
                Thickness = 0.5,
                Visibility = System.Windows.Visibility.Visible,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
            };
            this.Children.Add(this.lines_connecting_for_split);

            this.lines_connecting_for_split_stepwise = new LineGeometryModel3D()
            {
                Geometry = null,
                Color = TriangulationDisplay.COL_HIGHLIGHT,
                Thickness = 1,
                Visibility = System.Windows.Visibility.Visible,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
            };
            this.Children.Add(this.lines_connecting_for_split_stepwise);

            this.lines_connecting_for_split_stepwise2 = new LineGeometryModel3D()
            {
                Geometry = null,
                Color = TriangulationDisplay.COL_SUB_2,
                Thickness = 0.5,
                Visibility = System.Windows.Visibility.Visible,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
            };
            this.Children.Add(this.lines_connecting_for_split_stepwise2);

            this.lines_connecting_for_split_stepwise3 = new LineGeometryModel3D()
            {
                Geometry = null,
                Color = TriangulationDisplay.COL_SUB_2,
                Thickness = 1,
                Visibility = System.Windows.Visibility.Visible,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
            };
            this.Children.Add(this.lines_connecting_for_split_stepwise3);

            this.lines_connecting_for_split_stepwise4 = new LineGeometryModel3D()
            {
                Geometry = null,
                Color = TriangulationDisplay.COL_HIGHLIGHT,
                Thickness = 1.5,
                Visibility = System.Windows.Visibility.Visible,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
            };
            this.Children.Add(this.lines_connecting_for_split_stepwise4);

            this.lines_split_along_holes = new LineGeometryModel3D()
            {
                Geometry = null,
                Color = TriangulationDisplay.COL_SUB_2,
                Thickness = 1.5,
                Visibility = System.Windows.Visibility.Visible,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
            };
            this.Children.Add(this.lines_split_along_holes);

            this.lines_split_to_monotone_polys = new LineGeometryModel3D()
            {
                Geometry = null,
                Color = TriangulationDisplay.COL_SUB_1,
                Thickness = 1,
                Visibility = System.Windows.Visibility.Visible,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
            };
            this.Children.Add(this.lines_split_to_monotone_polys);

            this.lines_triangulation_of_monotone_polys = new LineGeometryModel3D()
            {
                Geometry = null,
                Color = TriangulationDisplay.COL_SUB_2,
                Thickness = 0.5,
                Visibility = System.Windows.Visibility.Visible,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
            };
            this.Children.Add(this.lines_triangulation_of_monotone_polys);

            // commands
            this.TStage = TriangulationStage.INIT;
            this.SetTriangulationStageSplitAlongHolesCmd = new RelayCommand((x) => this.TStage = TriangulationStage.SPLIT_ALONG_HOLES,
                                                                            (x) => this.TStage == TriangulationStage.INIT);
            this.SetTriangulationStageSplitIntoMonotoneCmd = new RelayCommand((x) => this.TStage = TriangulationStage.SPLIT_INTO_MONOTONE_POLYGONS,
                                                                              (x) => this.TStage == TriangulationStage.SPLIT_ALONG_HOLES);
            this.SetTriangulationStageTriangulateMonotoneCmd = new RelayCommand((x) => this.TStage = TriangulationStage.TRIANGULATE_MONOTONE_POLYGONS,
                                                                                (x) => this.TStage == TriangulationStage.SPLIT_INTO_MONOTONE_POLYGONS);
            this.SetTriangulationStageShowResultCmd = new RelayCommand((x) => this.TStage = TriangulationStage.SHOW_RESULT,
                                                                       (x) => this.TStage == TriangulationStage.TRIANGULATE_MONOTONE_POLYGONS);
            this.ResetAllCmd = new RelayCommand((x) => this.TStage = TriangulationStage.RESET);

            this.cpwch_step_counter = 0;
            this.cpwch_finished_LR = false;
            this.cpwch_finished_RL = false;
            this.TestPathFindingForSplitCmd = new RelayCommand((x) => this.OnTestPathFindingForSplit(),
                                                    (x) => this.CanExecute_OnTestPathFindingForSplit());
            this.disp_step_counter = 0;
            this.disp_finished_path_search = false;
            this.disp_finished_polygon_split = false;
            this.disp_started = false;
            this.TestPathFindingForSplit2Cmd = new RelayCommand((x) => this.OnTestPathFindingForSplit2(),
                                                     (x) => this.CanExecute_OnTestPathFindingForSplit());
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ====================================== GEOMETRY DEFINITION / FUNCTIONS ================================= //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region METHODS: Update visible geometry

        private void UpdateGeometry()
        {                           
            if (this.outer_polygon == null || this.outer_polygon.Count == 0)
            {
                this.ResetGeometry();
                return;
            }
            else
            {
                switch(this.TStage)
                {
                    case TriangulationStage.INIT:
                        LineBuilder lb_1 = new LineBuilder();
                        TriangulationDisplay.AddLines(this.outer_polygon, true, ref lb_1);
                        TriangulationDisplay.AddLines(this.holes, true, ref lb_1);
                        this.lines_polygons.Geometry = lb_1.ToLineGeometry3D();
                        break;
                    case TriangulationStage.SPLIT_ALONG_HOLES:
                        this.GetSplittingLines();
                        if (this.splittingPath != null)
                        {
                            LineBuilder lb_2x = new LineBuilder();
                            TriangulationDisplay.AddSeparateLines(this.splittingPath, ref lb_2x);
                            this.lines_connecting_for_split.Geometry = lb_2x.ToLineGeometry3D();
                        }
                        this.SplitAlongHoles();
                        if (this.simplePolys != null)
                        {
                            LineBuilder lb_2 = new LineBuilder();
                            TriangulationDisplay.AddLines(this.simplePolys, true, ref lb_2);
                            this.lines_split_along_holes.Geometry = lb_2.ToLineGeometry3D();
                        }
                        break;
                    case TriangulationStage.SPLIT_INTO_MONOTONE_POLYGONS:
                        this.SplitIntoMonotonePolygons();
                        if (this.monotonePolys != null && this.monotonePolys.Count > 0)
                        {
                            LineBuilder lb_3 = new LineBuilder();
                            TriangulationDisplay.AddLines(this.monotonePolys, true, ref lb_3);
                            this.lines_split_to_monotone_polys.Geometry = lb_3.ToLineGeometry3D();
                        }
                        break;
                    case TriangulationStage.TRIANGULATE_MONOTONE_POLYGONS:
                        this.TriangulateMonotonePolygons();
                        if (this.triangles != null && this.triangles.Count > 0)
                        {
                            LineBuilder lb_4 = new LineBuilder();
                            List<List<Point3D>> triangle_coords_P3D = this.triangles.Select(x => TriangulationDisplay.ExtractTrianglesFromMesh(x)).Aggregate((x, y) => x.Concat(y).ToList()).ToList();
                            TriangulationDisplay.AddLines(triangle_coords_P3D, true, ref lb_4);
                            this.lines_triangulation_of_monotone_polys.Geometry = lb_4.ToLineGeometry3D();
                        }
                        break;
                    case TriangulationStage.SHOW_RESULT:
                        this.FillTriangulation();
                        if (this.fill != null)
                        {
                            this.triangulation_full.Geometry = this.fill;
                        }
                        break;
                    default:
                        this.ResetAll();
                        break;
                }
                
            } 
        }


        private void ResetGeometry()
        {
            this.triangulation_full.Geometry = null;
            this.lines_polygons.Geometry = null;
            this.lines_connecting_for_split.Geometry = null;
            this.lines_connecting_for_split_stepwise.Geometry = null;
            this.lines_connecting_for_split_stepwise2.Geometry = null;
            this.lines_connecting_for_split_stepwise3.Geometry = null;
            this.lines_connecting_for_split_stepwise4.Geometry = null;
            this.lines_split_along_holes.Geometry = null;
            this.lines_split_to_monotone_polys.Geometry = null;
            this.lines_triangulation_of_monotone_polys.Geometry = null;
        }

        private void ResetAll()
        {
            this.test_polygons = null;
            // stage: init
            this.outer_polygon_index = -1;
            this.outer_polygon = null;
            this.holes = null;
            this.splittingPath = null;
            this.simplePolys = null;
            this.monotonePolys = null;
            this.triangles = null;

            this.cpwch_step_counter = 0;
            this.cpwch_finished_LR = false;
            this.cpwch_finished_RL = false;

            this.disp_step_counter = 0;
            this.disp_started = false;
            this.disp_finished_path_search = false;
            this.disp_finished_polygon_split = false;

            this.ResetGeometry();
        }

        #endregion

        #region METHODS: Generate triangulation

        private void SeparateIntoContourAndHoles()
        {
            this.outer_polygon_index = -1;
            this.outer_polygon = new List<Point3D>();
            this.holes = new List<List<Point3D>>();

            if (this.test_polygons == null || this.test_polygons.Count == 0) return;

            List<List<Point3D>> coordinates = this.test_polygons.Select(x => x.Polygon_Coords).ToList();
            MeshesCustom.ToPolygonWithHoles(coordinates, Orientation.XZ,
                                    out this.outer_polygon_index, out this.outer_polygon, out this.holes);
        }

        /// <summary>
        /// Algorithm copied from MeshesCustom.DecomposeInSimplePolygons
        /// </summary>
        private void GetSplittingLines()
        {
            if (this.outer_polygon == null) return;

            if (this.holes == null || this.holes.Count < 1) return;

            int n = this.outer_polygon.Count;
            if (n < 3) return;

            // make sure the winding direction of the polygon and all contained holes is the same!
            bool figure_is_valid;
            bool polygon_cw = MeshesCustom.CalculateIfPolygonClockWise(this.outer_polygon, CommonExtensions.GENERAL_CALC_TOLERANCE, out figure_is_valid);
            int nrH = this.holes.Count;
            for (int i = 0; i < nrH; i++)
            {
                bool hole_cw = MeshesCustom.CalculateIfPolygonClockWise(this.holes[i], CommonExtensions.GENERAL_CALC_TOLERANCE, out figure_is_valid);
                if (polygon_cw != hole_cw)
                {
                    this.holes[i] = MeshesCustom.ReversePolygon(this.holes[i]);
                }
            }

            // create connections btw the polygon and the holes contained in it
            // this method assumes that the polygon is not self-intersecting
            // and that the holes are disjunct and completely inside the polygon
            List<Vector4> connectingLines;
            MeshesCustom.ConnectPolygonWContainedHolesTwice(this.outer_polygon, this.holes, out connectingLines);

            this.splittingPath = new List<Point3D>();
            foreach(Vector4 conn in connectingLines)
            {
                if (conn.X == -1)
                    this.splittingPath.Add(this.outer_polygon[(int)conn.Y]);
                else
                    this.splittingPath.Add(this.holes[(int)conn.X][(int)conn.Y]);

                if (conn.Z == -1)
                    this.splittingPath.Add(this.outer_polygon[(int)conn.W]);
                else
                    this.splittingPath.Add(this.holes[(int)conn.Z][(int)conn.W]);
            }
        }

        private void SplitAlongHoles()
        {
            this.simplePolys = MeshesCustom.DecomposeInSimplePolygons_Improved(this.outer_polygon, this.holes);
        }

        private void SplitIntoMonotonePolygons()
        {
            if (this.simplePolys == null || this.simplePolys.Count == 0) return;

            this.monotonePolys = new List<List<Point3D>>();
            foreach(List<Point3D> sP in this.simplePolys)
            {
                this.monotonePolys.AddRange(MeshesCustom.DecomposeInMonotonePolygons(sP));
            }
        }

        private void TriangulateMonotonePolygons()
        {
            if (this.monotonePolys == null || this.monotonePolys.Count == 0) return;

            this.triangles = new List<HelixToolkit.SharpDX.Wpf.MeshGeometry3D>();
            foreach (List<Point3D> mpoly in this.monotonePolys)
            {
                List<Vector3> mpoly_V3 = CommonExtensions.ConvertPoints3DListToVector3List(mpoly);
                HelixToolkit.SharpDX.Wpf.MeshGeometry3D fill = MeshesCustom.PolygonFillMonotone(mpoly_V3);
                if (fill != null)
                    this.triangles.Add(fill);
            }
        }

        private void FillTriangulation()
        {
            if (this.triangles == null || this.triangles.Count == 0) return;
            this.fill = MeshesCustom.CombineMeshes(this.triangles);
        }


        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ========================================== ALGORITHMS STEP-BY-STEP ===================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region ALGORITHMS BREAKDOWNS: MeshesCustom.ConnectPolygonWContainedHoles

        private int cpwch_step_counter;
        private bool cpwch_finished_LR;
        private bool cpwch_finished_RL;
        private void OnTestPathFindingForSplit()
        {
            this.TestPathFindingStepNr = this.cpwch_step_counter;
            if (!this.cpwch_finished_LR && !this.cpwch_finished_RL && this.cpwch_step_counter == 0)
            {
                // vis
                this.cpwch_path = new List<Point3D>();
                this.ConnectPolygonWContainedHoles_Start();
            }

            if (!this.cpwch_finished_LR && !this.cpwch_finished_RL)
            {
                ConnectPolygonWContainedHoles_TranverseLR_1Step(this.cpwch_step_counter, out this.cpwch_finished_LR);
                if (this.cpwch_finished_LR)
                    this.cpwch_step_counter = 0;
                else
                    this.cpwch_step_counter++;
                return;
            }
            else if (this.cpwch_finished_LR && !this.cpwch_finished_RL)
            {
                ConnectPolygonWContainedHoles_TranverseRL_1Step(this.cpwch_step_counter, out this.cpwch_finished_RL);
                if (this.cpwch_finished_RL)
                    this.cpwch_step_counter = 0;
                else
                    this.cpwch_step_counter++;
                return;
            }
        }

        private bool CanExecute_OnTestPathFindingForSplit()
        {
            return (this.TStage == TriangulationStage.INIT);
        }

        private List<Vector4> cpwch_connectingLines;
        private SortedList<Vector3, int> cpwch_vertices_ordered;
        // left to right
        List<Vector4> cpwch_connectingLines_LR;
        List<int> cpwch_ind_connected_holes_LR;
        // right to left
        List<Vector4> cpwch_connectingLines_RL;
        List<int> cpwch_ind_connected_holes_RL;

        // vis        
        List<Point3D> cpwch_path;

        private void ShowStepConnectionFinding(Vector4 _conn)
        {
            LineBuilder lb = new LineBuilder();

            if (_conn.X == -1)
                this.cpwch_path.Add(this.outer_polygon[(int)_conn.Y]);
            else
                this.cpwch_path.Add(this.holes[(int)_conn.X][(int)_conn.Y]);

            if (_conn.Z == -1)
                this.cpwch_path.Add(this.outer_polygon[(int)_conn.W]);
            else
                this.cpwch_path.Add(this.holes[(int)_conn.Z][(int)_conn.W]);

            TriangulationDisplay.AddSeparateLines(this.cpwch_path, ref lb);
            this.lines_connecting_for_split_stepwise.Geometry = lb.ToLineGeometry3D();
        }

        private void ConnectPolygonWContainedHoles_Start()
        {
            // [X]:-1 for polygon / otherwise hole index [Y]: index in polygon / hole, [Z]:hole index, [W]:index in hole
            this.cpwch_connectingLines = new List<Vector4>();
            // left to right
            this.cpwch_connectingLines_LR = new List<Vector4>();
            this.cpwch_ind_connected_holes_LR = new List<int>();
            // right to left
            this.cpwch_connectingLines_RL = new List<Vector4>();
            this.cpwch_ind_connected_holes_RL = new List<int>();

            if (this.outer_polygon == null || this.holes == null)
                return;

            int n = this.outer_polygon.Count;
            int nrH = this.holes.Count;
            if (n < 3 || nrH < 1)
                return;

            // order the vertices according to the X component
            Vector3XComparer vec3Xcomp = new Vector3XComparer();
            this.cpwch_vertices_ordered = new SortedList<Vector3, int>(vec3Xcomp);
            for (int i = 0; i < n; i++)
            {
                if (this.cpwch_vertices_ordered.ContainsKey(this.outer_polygon[i].ToVector3()))
                    continue;

                try
                {
                    this.cpwch_vertices_ordered.Add(this.outer_polygon[i].ToVector3(), i + 1);
                }
                catch (ArgumentException)
                {
                    // if the same vertex occurs more than once, just skip it
                    continue;
                }
            }
            for (int j = 0; j < nrH; j++)
            {
                List<Point3D> hole = this.holes[j];
                if (hole == null || hole.Count < 3)
                    continue;
                int h = hole.Count;
                for (int i = 0; i < h; i++)
                {
                    if (this.cpwch_vertices_ordered.ContainsKey(hole[i].ToVector3()))
                        continue;

                    try
                    {
                        this.cpwch_vertices_ordered.Add(hole[i].ToVector3(), (j + 1) * 1000 + i + 1);
                    }
                    catch (ArgumentException)
                    {
                        // if the same vertex occurs more than once, just skip it
                        continue;
                    }
                }
            }
        }

        private void ConnectPolygonWContainedHoles_TranverseLR_1Step(int _index, out bool _finished)
        {
            _finished = false;
            int m = this.cpwch_vertices_ordered.Count;
            int nrH = this.holes.Count;

            if (_index >= m - 1)
            {
                _finished = true;
                return; 
            }

            // prepare polygon and holes for evaluating functions
            List<Vector3> polygon_asV3 = CommonExtensions.ConvertPoints3DListToVector3List(this.outer_polygon);
            List<List<Vector3>> holes_asV3 = CommonExtensions.ConvertPoints3DListListToVector3ListList(this.holes);

            // -------------------------------- TRAVERSAL LEFT -> RIGHT ------------------------------------- //

            // traverse the polygon in X-direction to determine the admissible diagonals 
            // connecting the FIRST points of each hole with the polygon vertices to the LEFT of them
            // (if such do not exist -> try connecting to previous holes to the LEFT)

            Vector3 current_alongX = this.cpwch_vertices_ordered.ElementAt(_index).Key;
            int ind_current_alongX = this.cpwch_vertices_ordered.ElementAt(_index).Value - 1;
            int ind_hole = ind_current_alongX / 1000 - 1;
            if (ind_hole < 0 || ind_hole > nrH - 1 || this.cpwch_ind_connected_holes_LR.Contains(ind_hole))
            {
                return;
            }

            // get information of the neighbor vertices in the hole
            List<Point3D> hole = this.holes[ind_hole];
            int nHole = hole.Count;
            int ind_in_hole = ind_current_alongX % 1000;
            Vector3 prev = hole[(nHole + ind_in_hole - 1) % nHole].ToVector3();
            Vector3 next = hole[(ind_in_hole + 1) % nHole].ToVector3();

            if (prev.X >= current_alongX.X && next.X >= current_alongX.X)
            {
                // START VERTEX -> Connect to a polygon vertex that is before this one along the X axis
                Vector3 prev_poly_alongX;
                int ind_prev_poly_alongX;
                for (int c = 1; c < _index + 1; c++)
                {
                    prev_poly_alongX = this.cpwch_vertices_ordered.ElementAt(_index - c).Key;
                    ind_prev_poly_alongX = this.cpwch_vertices_ordered.ElementAt(_index - c).Value - 1;
                    int ind_prev_hole = ind_prev_poly_alongX / 1000 - 1;
                    if (ind_prev_hole == ind_hole)
                        continue;

                    int ind_prev_in_hole = ind_prev_poly_alongX % 1000;


                    if (prev_poly_alongX.X < current_alongX.X)
                    {
                        // check if the diagonal is valid
                        bool isAdmissible = false;
                        if (ind_prev_hole == -1)
                        {
                            // check admissibility in the polygon
                            isAdmissible = MeshesCustom.LineIsValidInPolygonWHoles(polygon_asV3, holes_asV3,
                                                                                ind_prev_poly_alongX, ind_hole, ind_in_hole);
                        }
                        else
                        {
                            // check admissiblity w regard to two holes contained in the polygon
                            isAdmissible = MeshesCustom.LineIsValidInPolygonWHoles(polygon_asV3, holes_asV3,
                                                                        ind_prev_hole, ind_prev_in_hole, ind_hole, ind_in_hole);
                        }
                        if (isAdmissible)
                        {
                            this.cpwch_connectingLines_LR.Add(new Vector4(ind_prev_hole, ind_prev_in_hole, ind_hole, ind_in_hole));
                            this.cpwch_ind_connected_holes_LR.Add(ind_hole);
                            this.ShowStepConnectionFinding(new Vector4(ind_prev_hole, ind_prev_in_hole, ind_hole, ind_in_hole)); // VIS
                            break;
                        }
                    }
                }
            }
            this.cpwch_connectingLines.AddRange(this.cpwch_connectingLines_LR);
        }

        private void ConnectPolygonWContainedHoles_TranverseRL_1Step(int _index, out bool _finished)
        {
            _finished = false;
            int m = this.cpwch_vertices_ordered.Count;
            int nrH = this.holes.Count;

            if (_index >= m - 1)
            {
                _finished = true;
                return;
            }

            // prepare polygon and holes for evaluating functions
            List<Vector3> polygon_asV3 = CommonExtensions.ConvertPoints3DListToVector3List(this.outer_polygon);
            List<List<Vector3>> holes_asV3 = CommonExtensions.ConvertPoints3DListListToVector3ListList(this.holes);

            // -------------------------------- TRAVERSAL RIGHT -> LEFT ------------------------------------- //
            // traverse the polygon in X-direction to determine the admissible diagonals 
            // connecting the LAST points of each hole with the polygon vertices to the RIGHT of them
            // (if such do not exist -> try connecting to previous holes to the RIGHT)

            Vector3 current_alongX = this.cpwch_vertices_ordered.ElementAt(_index).Key;
            int ind_current_alongX = this.cpwch_vertices_ordered.ElementAt(_index).Value - 1;
            int ind_hole = ind_current_alongX / 1000 - 1;
            if (ind_hole < 0 || ind_hole > nrH - 1 || this.cpwch_ind_connected_holes_RL.Contains(ind_hole))
            {
                return;
            }

            // get information of the neighbor vertices in the hole
            List<Point3D> hole = this.holes[ind_hole];
            int nHole = hole.Count;
            int ind_in_hole = ind_current_alongX % 1000;
            Vector3 prev = hole[(nHole + ind_in_hole - 1) % nHole].ToVector3();
            Vector3 next = hole[(ind_in_hole + 1) % nHole].ToVector3();

            if (prev.X <= current_alongX.X && next.X <= current_alongX.X)
            {
                // END VERTEX -> Connect to a polygon vertex that is after this one along the X axis
                Vector3 next_poly_alongX;
                int ind_next_poly_alongX;
                for (int c = 1; c < m - _index; c++)
                {
                    next_poly_alongX = this.cpwch_vertices_ordered.ElementAt(_index + c).Key;
                    ind_next_poly_alongX = this.cpwch_vertices_ordered.ElementAt(_index + c).Value - 1;
                    int ind_next_hole = ind_next_poly_alongX / 1000 - 1;
                    if (ind_next_hole == ind_hole)
                        continue;

                    int ind_next_in_hole = ind_next_poly_alongX % 1000;

                    if (next_poly_alongX.X > current_alongX.X)
                    {
                        // check if the diagonal is valid
                        bool isAdmissible = false;
                        if (ind_next_hole == -1)
                        {
                            // check admissibility in the polygon
                            isAdmissible = MeshesCustom.LineIsValidInPolygonWHoles(polygon_asV3, holes_asV3,
                                                                                ind_next_poly_alongX, ind_hole, ind_in_hole);
                        }
                        else
                        {
                            // check admissiblity w regard to two holes contained in the polygon
                            isAdmissible = MeshesCustom.LineIsValidInPolygonWHoles(polygon_asV3, holes_asV3,
                                                                        ind_next_hole, ind_next_in_hole, ind_hole, ind_in_hole);
                        }
                        // check if the diagonal intersects any diagonals from the previous traversal
                        if (isAdmissible)
                        {
                            Vector3 p1 = holes_asV3[ind_hole][ind_in_hole];
                            Vector3 p2;
                            if (ind_next_hole == -1)
                                p2 = polygon_asV3[ind_next_poly_alongX];
                            else
                                p2 = holes_asV3[ind_next_hole][ind_next_in_hole];

                            foreach (Vector4 entry in this.cpwch_connectingLines_LR)
                            {
                                Vector3 q1 = holes_asV3[(int)entry.Z][(int)entry.W];
                                Vector3 q2;
                                if (entry.X == -1)
                                    q2 = polygon_asV3[(int)entry.Y];
                                else
                                    q2 = holes_asV3[(int)entry.X][(int)entry.Y];

                                Vector3 _colPos;
                                bool intersection = CommonExtensions.LineWLineCollision3D(p1, p2, q1, q2,
                                                                CommonExtensions.GENERAL_CALC_TOLERANCE, out _colPos);
                                if (intersection)
                                {
                                    isAdmissible = false;
                                    break;
                                }
                            }
                        }
                        if (isAdmissible)
                        {
                            this.cpwch_connectingLines_RL.Add(new Vector4(ind_next_hole, ind_next_in_hole, ind_hole, ind_in_hole));
                            this.cpwch_ind_connected_holes_RL.Add(ind_hole);
                            this.ShowStepConnectionFinding(new Vector4(ind_next_hole, ind_next_in_hole, ind_hole, ind_in_hole)); // VIS
                            break;
                        }
                    }
                }
            }
            this.cpwch_connectingLines.AddRange(this.cpwch_connectingLines_RL);
        }


        #endregion

        #region ALGORITHM BREAKDOWN: MeshesCustom.DecomposeInSimplePolygons

        private int disp_step_counter;
        private bool disp_started;
        private bool disp_finished_path_search;
        private bool disp_finished_polygon_split;

        private void OnTestPathFindingForSplit2()
        {
            this.TestPathFindingStepNr2 = this.disp_step_counter;
            if (this.disp_step_counter == 0 && !this.disp_started)
            {
                // vis
                this.disp_path = new List<Point3D>();
                this.DecomposeInSimplePolygons_Start();
                this.disp_started = true;
                return;
            }

            if (!this.disp_finished_path_search)
            {
                this.disp_finished_path_search = this.DecomposeInSimplePolygons_IterationStep();
                if (this.disp_finished_path_search)
                    this.disp_step_counter = 0;
                else
                    this.disp_step_counter++;
            }

            if (this.disp_finished_path_search && !this.disp_finished_polygon_split)
            {
                if (this.disp_step_counter == 0)
                    this.DecomposeInSimplePolygons_PolygonExtractionStart();
                
                this.disp_finished_polygon_split = this.DecomposeInSimplePolygons_PolygonExtractionStep(this.disp_step_counter);
                if (this.disp_finished_polygon_split)
                    this.disp_step_counter = 0;
                else
                    this.disp_step_counter++;
            }
            
        }

        private void ShowStepFindingBase()
        {
            LineBuilder lb = new LineBuilder();
            foreach (Vector4 connection in this.disp_connectingLines)
            {
                if (connection.X == -1)
                    this.disp_path.Add(this.outer_polygon[(int)connection.Y]);
                else
                    this.disp_path.Add(this.holes[(int)connection.X][(int)connection.Y]);

                if (connection.Z == -1)
                    this.disp_path.Add(this.outer_polygon[(int)connection.W]);
                else
                    this.disp_path.Add(this.holes[(int)connection.Z][(int)connection.W]);

                TriangulationDisplay.AddSeparateLines(this.disp_path, ref lb);
            }
            this.lines_connecting_for_split_stepwise2.Geometry = lb.ToLineGeometry3D();
        }

        private void ShowStepPathFinding(Vector4 _conn)
        {
            LineBuilder lb = new LineBuilder();

            if (_conn.X == -1)
                this.disp_path.Add(this.outer_polygon[(int)_conn.Y]);
            else
                this.disp_path.Add(this.holes[(int)_conn.X][(int)_conn.Y]);

            if (_conn.Z == -1)
                this.disp_path.Add(this.outer_polygon[(int)_conn.W]);
            else
                this.disp_path.Add(this.holes[(int)_conn.Z][(int)_conn.W]);

            TriangulationDisplay.AddSeparateLines(this.disp_path, ref lb);
            this.lines_connecting_for_split_stepwise3.Geometry = lb.ToLineGeometry3D();
        }

        private void ShowCurrenPolygonSplit()
        {
            LineBuilder lb = new LineBuilder();
            TriangulationDisplay.AddLines(this.disp_list_before_Split_polys, true, ref lb);
            this.lines_connecting_for_split_stepwise4.Geometry = lb.ToLineGeometry3D();
        }

        private List<Vector4> disp_connectingLines;
        private List<bool> disp_connectingLines_used;
        private List<int> disp_holes_toSplit;
        private List<List<Vector4>> disp_splitting_paths;

        private List<List<Point3D>> disp_list_before_Split_polys;
        private List<List<Vector2>> disp_list_before_Split_inds;

        private List<List<Point3D>> disp_list_after_Split_polys;
        private List<List<Vector2>> disp_list_after_Split_inds;

        private List<Point3D> disp_path;

        public void DecomposeInSimplePolygons_Start()
        {
            if (this.outer_polygon == null) return;

            if (this.holes == null || this.holes.Count < 1) return;

            int n = this.outer_polygon.Count;
            if (n < 3) return;

            // make sure the winding direction of the polygon and all contained holes is the same!
            bool figure_is_valid;
            bool polygon_cw = MeshesCustom.CalculateIfPolygonClockWise(this.outer_polygon, CommonExtensions.GENERAL_CALC_TOLERANCE, out figure_is_valid);
            int nrH = this.holes.Count;
            for (int i = 0; i < nrH; i++)
            {
                bool hole_cw = MeshesCustom.CalculateIfPolygonClockWise(this.holes[i], CommonExtensions.GENERAL_CALC_TOLERANCE, out figure_is_valid);
                if (polygon_cw != hole_cw)
                {
                    this.holes[i] = MeshesCustom.ReversePolygon(this.holes[i]);
                }
            }

            // create connections btw the polygon and the holes contained in it
            // this method assumes that the polygon is not self-intersecting
            // and that the holes are disjunct and completely inside the polygon
            MeshesCustom.ConnectPolygonWContainedHolesTwice(this.outer_polygon, this.holes, out this.disp_connectingLines);
            this.ShowStepFindingBase(); // VIS
            int nrCL = this.disp_connectingLines.Count;
            if (nrCL < 2) return;

            // perform decomposition (no duplicates in the connecting lines)
            this.disp_holes_toSplit = Enumerable.Range(0, nrH).ToList();
            this.disp_connectingLines_used = Enumerable.Repeat(false, nrCL).ToList();

            // find all SPLITTING PATHS
            this.disp_splitting_paths = new List<List<Vector4>>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>TRUE when no more iterations are possible</returns>
        private bool DecomposeInSimplePolygons_IterationStep()
        {
            int nrCL = this.disp_connectingLines.Count;
            if (this.disp_holes_toSplit.Count > 0)
            {
                // look for a connected path of connecting lines 
                // that STARTS at the polygon, goes THROUGH a not yet split hole, and ENDS at the polygon
                // or a hole that has been split already
                List<Vector4> splitting_path = new List<Vector4>();
                // START
                bool reached_other_end = false;
                List<int> holes_to_remove_from_todo = new List<int>(); // ...........................................................................................
                for (int i = 0; i < nrCL; i++)
                {
                    if (this.disp_connectingLines_used[i])
                        continue;
                    // start at the polygon or a hole that has already been split
                    if (this.disp_connectingLines[i].X == -1 || !this.disp_holes_toSplit.Contains((int)this.disp_connectingLines[i].X))
                    {
                        splitting_path.Add(this.disp_connectingLines[i]);
                        this.ShowStepPathFinding(this.disp_connectingLines[i]); // VIS
                        this.disp_connectingLines_used[i] = true;

                        int split_hole_ind = (int)splitting_path[0].Z;
                        if (this.disp_holes_toSplit.Contains(split_hole_ind))
                        {
                            // this.disp_holes_toSplit.Remove(split_hole_ind);
                            if (!holes_to_remove_from_todo.Contains(split_hole_ind)) // ............................................................................
                                holes_to_remove_from_todo.Add(split_hole_ind); 
                            reached_other_end = false;
                        }
                        else
                            reached_other_end = true;

                        break;
                    }
                }

                // HOLES and END
                int nrSP = splitting_path.Count;
                if (nrSP == 0)
                    return false;

                int maxNrIter = this.disp_connectingLines.Count;
                int counter_iterations = 0;

                List<int> holes_used_in_this_path = new List<int>(); // .......................................................................................
                while (!reached_other_end && counter_iterations <= maxNrIter)
                {
                    counter_iterations++;
                    
                    for (int i = 0; i < nrCL; i++)
                    {
                        if (this.disp_connectingLines_used[i])
                            continue;

                        if (this.disp_connectingLines[i].X == splitting_path[nrSP - 1].Z && this.disp_connectingLines[i].Y != splitting_path[nrSP - 1].W &&
                            !holes_used_in_this_path.Contains((int)this.disp_connectingLines[i].Z)) // ............................................................
                        {
                            splitting_path.Add(this.disp_connectingLines[i]);
                            this.ShowStepPathFinding(this.disp_connectingLines[i]); // VIS
                            holes_used_in_this_path.Add((int)this.disp_connectingLines[i].X); // ..................................................................
                        }
                        else if (this.disp_connectingLines[i].Z == splitting_path[nrSP - 1].Z && this.disp_connectingLines[i].W != splitting_path[nrSP - 1].W &&
                                !holes_used_in_this_path.Contains((int)this.disp_connectingLines[i].X)) // ........................................................
                        {
                            Vector4 conn_new = new Vector4(this.disp_connectingLines[i].Z, this.disp_connectingLines[i].W,
                                                           this.disp_connectingLines[i].X, this.disp_connectingLines[i].Y);
                            splitting_path.Add(conn_new);
                            this.ShowStepPathFinding(conn_new); // VIS
                            holes_used_in_this_path.Add((int)this.disp_connectingLines[i].Z); // ..................................................................
                        }
                        else
                            continue;

                        nrSP = splitting_path.Count;
                        this.disp_connectingLines_used[i] = true;

                        int split_hole_ind = (int)splitting_path[nrSP - 1].Z;
                        if (this.disp_holes_toSplit.Contains(split_hole_ind))
                        {
                            // this.disp_holes_toSplit.Remove(split_hole_ind);
                            if (!holes_to_remove_from_todo.Contains(split_hole_ind))  // ..........................................................................
                                holes_to_remove_from_todo.Add(split_hole_ind);
                            reached_other_end = false;
                        }
                        else
                            reached_other_end = true;

                        break;
                    }                    
                }
                foreach (int id in holes_to_remove_from_todo) // ...................................................................................................
                {
                    this.disp_holes_toSplit.Remove(id);
                }

                this.disp_splitting_paths.Add(splitting_path);
                return false;
            }
            return true;
        }

        private void DecomposeInSimplePolygons_PolygonExtractionStart()
        {
            this.disp_list_before_Split_polys = new List<List<Point3D>>();
            this.disp_list_before_Split_inds = new List<List<Vector2>>();

            this.disp_list_after_Split_polys = new List<List<Point3D>>();
            this.disp_list_after_Split_inds = new List<List<Vector2>>();

            
            this.disp_list_before_Split_polys.Add(this.outer_polygon);
            this.disp_list_before_Split_inds.Add(MeshesCustom.GenerateDoubleIndices(-1, 0, this.outer_polygon.Count));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_index"></param>
        /// <returns>TRUE if the iteration index exceeded the number of splitting paths</returns>
        private bool DecomposeInSimplePolygons_PolygonExtractionStep(int _index)
        {
            // perform splitting
            int d = this.disp_splitting_paths.Count;
            int n = this.outer_polygon.Count;

            if (_index >= 0 && _index < d)
            {
                int nrToSplit = this.disp_list_before_Split_polys.Count;
                for (int k = 0; k < nrToSplit; k++)
                {
                    List<Point3D> polyA, polyB;
                    List<Vector2> originalIndsA, originalIndsB;
                    bool inputValid;
                    MeshesCustom.SplitPolygonWHolesAlongPath(this.disp_list_before_Split_polys[k], this.disp_list_before_Split_inds[k],
                                                this.disp_splitting_paths[_index], true, this.holes,
                                                out polyA, out polyB, out originalIndsA, out originalIndsB, out inputValid);

                    if (polyA.Count > 2 && polyB.Count > 2)
                    {
                        // successful split
                        this.disp_list_after_Split_polys.Add(polyA);
                        this.disp_list_after_Split_inds.Add(originalIndsA);
                        this.disp_list_after_Split_polys.Add(polyB);
                        this.disp_list_after_Split_inds.Add(originalIndsB);
                    }
                    else
                    {
                        // no split
                        this.disp_list_after_Split_polys.Add(this.disp_list_before_Split_polys[k]);
                        this.disp_list_after_Split_inds.Add(this.disp_list_before_Split_inds[k]);
                    }
                }
                // swap lists
                this.disp_list_before_Split_polys = new List<List<Point3D>>(this.disp_list_after_Split_polys);
                this.disp_list_before_Split_inds = new List<List<Vector2>>(this.disp_list_after_Split_inds);
                this.disp_list_after_Split_polys = new List<List<Point3D>>();
                this.disp_list_after_Split_inds = new List<List<Vector2>>();
                // VIS
                this.ShowCurrenPolygonSplit();
                return false;
            }
            else
                return true;
        }

        #endregion

    }
}
