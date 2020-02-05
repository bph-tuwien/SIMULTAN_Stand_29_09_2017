using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Reflection;

using System.Windows.Input;
using Media3D = System.Windows.Media.Media3D;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Matrix3D = System.Windows.Media.Media3D.Matrix3D;
using Transform3D = System.Windows.Media.Media3D.Transform3D;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using Line3D = HelixToolkit.SharpDX.Wpf.Geometry3D.Line;
using SharpDX;

using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

using DXFImportExport;
using GeometryViewer.EntityGeometry;
using GeometryViewer.ComponentInteraction;
using GeometryViewer.ComponentReps;

using System.Globalization;

namespace GeometryViewer
{
    public enum Orientation { XZ = 0, XY = 1, YZ = 2}
    class AppViewModel : BaseViewModel
    {
        public static readonly int GRID_SIZE = 20;
        public static readonly int SCALE_CP = 7;

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ======================================== DATA MANAGEMENT PROPERTIES ==================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region DATA MANAGEMENT PROPERTIES
        public ICommand OpenCmd { get; private set; }
        public string FileName { get; private set; }
        public ICommand ImportDXFCmd { get; private set; }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // =========================================== MAIN VIEW PROPERTIES ======================================= //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region MAIN VIEW PROPERTIES
        // LIGHTS
        public SharpDX.Vector3 Light1LookAt { get; private set; }
        public SharpDX.Vector3 Light2LookAt { get; private set; }
        public SharpDX.Vector3 Light3LookAt { get; private set; }

        // COLORS
        public SharpDX.Color4 BackgroundColor { get; private set; }
        public SharpDX.Color4 LightDiffuseColor { get; private set; }
        public SharpDX.Color4 LightAmbientColor { get; private set; }

        // RENDER TECHNIQUES
        private static IList<string> renderTechniques = new List<string>
        {
            Techniques.RenderColors,
            Techniques.RenderBlinn,
            Techniques.RenderPositions,
            Techniques.RenderNormals,
            Techniques.RenderWires
        };
        public IList<string> RenderTechniques
        {
            get { return renderTechniques; }
        }

        // CAMERA SWITCH: Perspective <-> Orthographic
        public ICommand SwitchCameraCmd { get; private set; }

        private bool cameraIsPerspective;
        public bool CameraIsPerspective
        {
            get { return this.cameraIsPerspective; }
            set 
            { 
                this.cameraIsPerspective = value;
                OnPropertyChanged("CameraIsPerspective");
            }
        }


        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ======================================== HELPER GEOMETRY PROPERTIES ==================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region GRID
        private LineGeometry3D grid;
        public LineGeometry3D Grid
        {
            get { return this.grid; }
            private set
            {
                this.grid = value;
                OnPropertyChanged("Grid");
            }
        }

        private SharpDX.Color gridColor;
        public SharpDX.Color GridColor
        {
            get { return this.gridColor; } 
            private set
            {
                this.gridColor = value;
                OnPropertyChanged("GridColor");
            }
        }

        private Transform3D gridTransform;
        public Transform3D GridTransform 
        {
            get { return this.gridTransform; }
            private set
            {
                this.gridTransform = value;
                OnPropertyChanged("GridTransform");
            }
        }

        private float gridDistFRomOrigin = -1f;
        public float GridDistFRomOrigin
        {
            get { return gridDistFRomOrigin; }
            set
            {
                gridDistFRomOrigin = value;
                OnPropertyChanged("GridDistFRomOrigin");
                if (this.GridOrientation == Orientation.XZ)
                {
                    this.GridTransform = new Media3D.TranslateTransform3D(-GRID_SIZE / 2, this.GridDistFRomOrigin, -GRID_SIZE / 2);
                }
                else if (this.GridOrientation == Orientation.XY)
                {
                    this.GridTransform = new Media3D.TranslateTransform3D(-GRID_SIZE / 2, -GRID_SIZE / 2, this.GridDistFRomOrigin);
                }
                else
                {
                    this.GridTransform = new Media3D.TranslateTransform3D(this.GridDistFRomOrigin, -GRID_SIZE / 2, -GRID_SIZE / 2);
                }
            }
        }

        private Orientation gridOrientation;
        public Orientation GridOrientation
        {
            get { return this.gridOrientation; }
            set 
            { 
                this.gridOrientation = value;
                OnPropertyChanged("GridOrientation");
                if (value == Orientation.XZ)
                {
                    this.Grid = LineBuilder.GenerateGrid(Vector3.UnitY, 0, GRID_SIZE);
                    this.GridColor = SharpDX.Color.DarkGray;
                    this.GridTransform = new Media3D.TranslateTransform3D(-GRID_SIZE / 2, this.GridDistFRomOrigin, -GRID_SIZE / 2);
                    this.ConstrPlG = GenerateConstrPlane(Orientation.XZ);
                }
                else if (value == Orientation.XY)
                {
                    this.Grid = LineBuilder.GenerateGrid(Vector3.UnitZ, 0, GRID_SIZE);
                    this.GridColor = SharpDX.Color.DarkBlue;
                    this.GridTransform = new Media3D.TranslateTransform3D(-GRID_SIZE / 2, -GRID_SIZE / 2, this.GridDistFRomOrigin);
                    this.ConstrPlG = GenerateConstrPlane(Orientation.XY);
                }
                else
                {
                    this.Grid = LineBuilder.GenerateGrid(Vector3.UnitX, 0, GRID_SIZE);
                    this.GridColor = SharpDX.Color.DarkRed;
                    this.GridTransform = new Media3D.TranslateTransform3D(this.GridDistFRomOrigin, -GRID_SIZE / 2, -GRID_SIZE / 2);
                    this.ConstrPlG = GenerateConstrPlane(Orientation.YZ);
                }
            }
        }
        #endregion

        #region CONSTRUCTION PLANE (lines are drawn on it) and SNAP
        private MeshGeometry3D constrPlg;
        public MeshGeometry3D ConstrPlG 
        {
            get { return this.constrPlg; } 
            set
            {
                this.constrPlg = value;
                OnPropertyChanged("ConstrPlG");
            }
        }

        public PhongMaterial ConstrPlM { get; set; }
        public IEnumerable<SharpDX.Matrix> ConstrPlIN { get; set; }

        private bool snapToGrid;

        public bool SnapToGrid
        {
            get { return this.snapToGrid; }
            set
            { 
                this.snapToGrid = value;
                OnPropertyChanged("SnapToGrid");
            }
        }

        private float snapMagnet;
        public float SnapMagnet
        {
            get { return this.snapMagnet; }
            set 
            { 
                this.snapMagnet = value;
                OnPropertyChanged("SnapMagnet");
            }
        }

        #endregion

        #region ORIGIN
        public LineGeometry3D OriginLines { get; private set; }
        public SharpDX.Color OriginColor { get; private set; }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ====================================== GEOMETRY DEFINITION PROPERTIES ================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region VISUALIZIERUNGEN

        private BitmapSource opacityMap;
        public BitmapSource OpacityMap
        {
            get { return this.opacityMap; }
            private set
            {
                this.opacityMap = value;
                OnPropertyChanged("OpacityMap");
            }
        }

        #endregion

        #region MANAGEMENT: Components

        private CompRepManager rmanager_comp_reps_backup;
        private CompRepManager rmanager_comp_reps;
        public CompRepManager RManager_CompReps
        {
            get { return this.rmanager_comp_reps; }
            private set
            { 
                this.rmanager_comp_reps = value;
                OnPropertyChanged("RManager_CompReps");
            }
        }

        #endregion

        #region MANAGEMENT: Geometry (ARC + BPH)

        private List<DXFLayer> dxfLayers;
        public List<DXFLayer> DXFLayers
        {
            get { return dxfLayers; }
            set 
            { 
                dxfLayers = value;
                OnPropertyChanged("DXFLayers");
            }
        }

        private List<DXFGeometry> dxfGeometry;
        public List<DXFGeometry> DXFGeometry
        {
            get { return this.dxfGeometry; }
            set 
            { 
                this.dxfGeometry = value;
                OnPropertyChanged("DXFGeometry");
            }
        }

        private EntityManager eManager_arc;
        public EntityManager EManager_ARC
        {
            get { return this.eManager_arc; }
            private set 
            { 
                this.eManager_arc = value;
                OnPropertyChanged("EManager_ARC");
            }
        }

        private EntityManager eManager_bph;
        public EntityManager EManager_BPH
        {
            get { return this.eManager_bph; }
            private set 
            {
                this.eManager_bph = value;
                OnPropertyChanged("EManager_BPH");
            }
        }

        #endregion

        #region MANAGEMENT: Components (BPH + ?)

        private MaterialManager cpManager;
        public MaterialManager MLManager
        {
            get { return this.cpManager; }
            set 
            { 
                this.cpManager = value;
                OnPropertyChanged("CPManager");
            }
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // =============================================== CONSTRUCTORS =========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region STANDARD CONSTRUCTOR
        public AppViewModel()
        {
            // ============================ GENERAL SET-UP ========================== //
            // ------------------------------- menu items --------------------------- //
            this.OpenCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnOpenClick());
            this.ImportDXFCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnImportDXF());

            // -------------------------------- viewport ---------------------------- //
            this.Title = "Main 3D View";
            this.SubTitle = "Project SIMULTAN";
            this.BackgroundColor = new Color4(0.5f, 0.5f, 0.5f, 1.0f);

            // ------------------------------ camera set-up ------------------------- //
            this.Camera = new PerspectiveCamera()
            {
                Position = new Point3D(1, 1, 1.5),
                LookDirection = new Vector3D(-1, -1, -1.5),
                UpDirection = new Vector3D(0, 1, 0)
            };
            this.SwitchCameraCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnSwitchCameraCommand());
            this.CameraIsPerspective = true;
            // ------------------------------ lights set-up ------------------------- //
            this.LightAmbientColor = new Color4(0.2f, 0.2f, 0.2f, 1.0f);
            this.LightDiffuseColor = new Color4(0.5f, 0.45f, 0.45f, 1.0f);
            this.Light1LookAt = -new Vector3(2.0f, 1.0f, 1.5f);
            this.Light2LookAt = -new Vector3(-2.0f, -1.0f, -1.5f);
            this.Light3LookAt = -new Vector3(0.0f, 2.0f, -1.0f);

            // ----------------------------- rendering set-up ----------------------- //
            this.RenderTechnique = Techniques.RenderPhong;

            // ======================== HELP GEOMETRY SET-UP ======================== //
            // ------------------------------ grid set-up --------------------------- //
            this.Grid = LineBuilder.GenerateGrid(Vector3.UnitY, 0, GRID_SIZE);
            this.GridColor = SharpDX.Color.DarkGray;
            this.gridDistFRomOrigin = -1f;
            this.GridTransform = new Media3D.TranslateTransform3D(-GRID_SIZE / 2, this.gridDistFRomOrigin, -GRID_SIZE / 2);

            // --------------------------- construction plane ------------------------ //
            this.ConstrPlG = GenerateConstrPlane(Orientation.XZ);
            this.ConstrPlM = new PhongMaterial();
            this.ConstrPlM.DiffuseColor = new Color4(0.8f);
            this.ConstrPlM.SpecularColor = new Color4(1.0f);
            this.ConstrPlM.SpecularShininess = 4f;
            var list = new List<SharpDX.Matrix>();
            list.Add(SharpDX.Matrix.Translation(new Vector3(0.0f)));
            this.ConstrPlIN = list;
            // ------------------------------ origin set-up -------------------------- //
            this.OriginLines = generateOrigin();
            this.OriginColor = SharpDX.Color.LightGray;

            // ---------------------- visualization help set-up ---------------------- //
            
            #region Opacity Map
            //string assemblyPath = Assembly.GetExecutingAssembly().EscapedCodeBase;
            //int locLastSlash = assemblyPath.LastIndexOf("/");
            //string parentDir = assemblyPath.Substring(0, locLastSlash);
            //string mapPath = string.Concat(parentDir, @"/Data/Maps/opacity_map.jpg");
            this.OpacityMap = BitmapFrame.Create(new Uri(@"./Data/Maps/opacity_map_rgb_6.png", UriKind.RelativeOrAbsolute),
                BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            //var debug = this.OpacityMap.Format;
            //var debug1 = this.OpacityMap.Palette;
            //var data = this.OpacityMap.ToByteArray();


            //// alternative: procedurally created bitmap
            //PixelFormat pf = PixelFormats.Bgra32;
            //int om_width = 64;
            //int om_height = 16;
            //int om_stride = 4 * ((om_width * pf.BitsPerPixel + 31) / 32);
            //int om_nr_pixels = om_height * om_stride;
            
            //byte[] om_pixels = new byte[om_nr_pixels];
            //for (int i = 0; i < om_nr_pixels; i += 4)
            //{
            //    if (i < om_nr_pixels * 0.25)
            //    {
            //        om_pixels[i] = 0;       //B
            //        om_pixels[i + 1] = 0;   //G
            //        om_pixels[i + 2] = 255;   //R
            //        om_pixels[i + 3] = 102;   //A
            //    }
            //    else if (i < om_nr_pixels * 0.50)
            //    {
            //        om_pixels[i] = 0;       //B
            //        om_pixels[i + 1] = 255;   //G
            //        om_pixels[i + 2] = 0;   //R
            //        om_pixels[i + 3] = 153;   //A
            //    }
            //    else if (i < om_nr_pixels * 0.75)
            //    {
            //        om_pixels[i] = 255;       //B
            //        om_pixels[i + 1] = 0;   //G
            //        om_pixels[i + 2] = 0;   //R
            //        om_pixels[i + 3] = 204;   //A
            //    }
            //    else
            //    {
            //        om_pixels[i] = 255;       //B
            //        om_pixels[i + 1] = 255;   //G
            //        om_pixels[i + 2] = 255;   //R
            //        om_pixels[i + 3] = 255;   //A
            //    }
            //}

            //this.OpacityMap = BitmapSource.Create(om_width, om_height, 96, 96, pf, null, om_pixels, om_stride);
            //// debug
            //debug = this.OpacityMap.Format;
            //debug1 = this.OpacityMap.Palette;
            //data = this.OpacityMap.ToByteArray();

            #endregion

            // ========================= MAIN GEOMETRY SET-UP ======================== //

            // ----------------------- COMPONENT manager set-up ---------------------- //
            this.RManager_CompReps = new CompRepManager();
            this.RManager_CompReps.PropertyChanged += RManager_CompReps_PropertyChanged;

            // ------------------- architectural geometry manager set-up ------------- //
            this.EManager_ARC = new EntityManager();

            // ----------------- zone (baupyhsik) geometry manager set-up ------------ //
            this.EManager_BPH = new EntityManager();

            #region EManager_BPH SETUP
            //// test layers BPH
            //Layer layer1 = new Layer("Layer 1", SharpDX.Color.Blue);
            //Layer layer1A = new Layer("SubLayer 1.1", SharpDX.Color.Red);
            //layer1.AddEntity(layer1A);
            //EManager_BPH.AddLayer(layer1);
            //Layer layer2 = new Layer("Layer 2", SharpDX.Color.Indigo);
            //Layer layer2A = new Layer("SubLayer 2.1", SharpDX.Color.OrangeRed);
            //Layer layer2B = new Layer("SubLayer 2.2", SharpDX.Color.DarkOrange);
            //layer2.AddEntity(layer2A);
            //layer2.AddEntity(layer2B);
            //EManager_BPH.AddLayer(layer2);

            #region Test data 1

            //// test data BPH (1)
            //ZonedPolygon e1 = new ZonedPolygon(this.EManager_BPH.Layers[0], TestPoly1);
            //ZonedPolygon e2 = new ZonedPolygon(this.EManager_BPH.Layers[0], TestPolyA1);
            //ZonedPolygon e3 = new ZonedPolygon("TestPolyA2", this.EManager_BPH.Layers[0], TestPolyA2);

            //ZonedPolygon e4 = new ZonedPolygon("TestPolyA3", this.EManager_BPH.Layers[1], TestPolyA3);
            //ZonedPolygon e5 = new ZonedPolygon("TestPolyA4", layer1A, TestPolyA4);
            //ZonedPolygon e5_h = new ZonedPolygon("TestPolyA4_hole", layer1A, TestPolyA4_HOLE);
            //e5_h.ReDefineAllZones(new List<int> { 11, 12, 13, 14 });
            //ZonedPolygon e6 = new ZonedPolygon("TestPolyA5", layer1, TestPolyA5);
            //ZonedPolygon e6_h = new ZonedPolygon("TestPolyA5_hole", layer1, TestPolyA5_HOLE);
            //e6_h.ReDefineAllZones(new List<int> { 11, 12, 13, 14 });
            //ZonedPolygon e6d = new ZonedPolygon("TestPolyA6", layer1, TestPolyA6);

            //e5.AddOpening(3, 0.25f, 1f, 1.2f, 1.5f);

            //e5.AddOpening(4, 2f, 4f, 0.85f, 1.25f);
            //e5.AddOpening(4, 5f, 1.5f, 2.50f, 0.55f);

            //e5.AddOpening(4, 2f, 4f, 0.85f, 1.25f);
            //e5.AddOpening(4, 6.5f, 1.5f, 1.25f, 0.55f);
            //e5.AddOpening(4, 8.8f, 1.2f, 1.45f, 0.25f);

            //e5.AddOpening(4, 2f, 4f, 1.35f, 0.55f);
            //e5.AddOpening(4, 6.5f, 1.5f, 0.85f, 1.25f);

            #endregion

            #region Test data 2

            //// test data BPH (2)
            //ZonedPolygon e7 = new ZonedPolygon("EG_POLY_1", layer2, EG_POLY_1);
            //e7.ReDefineAllZones(new List<int> { 7, 6, 5, 4, 3, 2, 1, 0 });
            //e7.Reverse();
            //ZonedPolygon e7_1 = new ZonedPolygon("EG_POLY_1_HOLE_1", layer2, EG_POLY_1_HOLE_1);
            //e7_1.ReDefineAllZones(new List<int> { 25, 24, 23, 22 });
            //e7_1.Reverse();
            //ZonedPolygon e7_2 = new ZonedPolygon("EG_POLY_1_HOLE_2", layer2, EG_POLY_1_HOLE_2);
            //e7_2.ReDefineAllZones(new List<int> { 29, 28, 27, 26 });
            //e7_2.Reverse();
            //ZonedPolygon e7_3 = new ZonedPolygon("EG_POLY_1_HOLE_3", layer2, EG_POLY_1_HOLE_3);
            //e7_3.ReDefineAllZones(new List<int> { 33, 32, 31, 30 });
            //e7_3.Reverse();

            //ZonedPolygon e8 = new ZonedPolygon("EG_POLY_2", layer2, EG_POLY_2);
            //e8.ReDefineAllZones(new List<int> { 1, 13, 12, 11, 5, 10, 9, 8 });
            //e8.Reverse();
            //ZonedPolygon e8_1 = new ZonedPolygon("EG_POLY_2_HOLE_1", layer2, EG_POLY_2_HOLE_1);
            //e8_1.ReDefineAllZones(new List<int> { 37, 36, 35, 34 });
            //e8_1.Reverse();
            //ZonedPolygon e8_2 = new ZonedPolygon("EG_POLY_2_HOLE_2", layer2, EG_POLY_2_HOLE_2);
            //e8_2.ReDefineAllZones(new List<int> { 41, 40, 39, 38 });
            //e8_2.Reverse();

            //ZonedPolygon e9 = new ZonedPolygon("OG1_POLY_1", layer2A, OG1_POLY_1);
            //e9.ReDefineAllZones(new List<int> { 18, 17, 16, 6, 7, 0, 15, 14 });
            //e9.Reverse();
            //ZonedPolygon e9a = new ZonedPolygon("OG1_POLY_1A", layer2A, OG1_POLY_1A);
            //e9a.ReDefineAllZones(new List<int> { 18, 17, 116, 106, 107, 100, 115, 14 });
            //e9a.Reverse();
            //ZonedPolygon e10 = new ZonedPolygon("OG1_POLY_2", layer2A, OG1_POLY_2);
            //e10.ReDefineAllZones(new List<int> { 15, 8, 9, 10, 16, 21, 20, 19 });
            //e10.Reverse();
            //ZonedPolygon e10a = new ZonedPolygon("OG1_POLY_2A", layer2A, OG1_POLY_2A);
            //e10a.ReDefineAllZones(new List<int> { 115, 108, 109, 110, 116, 21, 20, 19 });
            //e10a.Reverse();

            //ZonedPolygon e11 = new ZonedPolygon("OG2_POLY_1", layer2B, OG2_POLY_1);
            //e11.ReDefineAllZones(new List<int> { 107, 106, 110, 109, 108, 100 }); // 18, 17, 21, 20, 19, 14
            //e11.Reverse();
            //ZonedPolygon e11_1 = new ZonedPolygon("OG2_POLY_1_HOLE_1", layer2B, OG2_POLY_1_HOLE_1);
            //e11_1.ReDefineAllZones(new List<int> { 25, 24, 23, 22 });
            //e11_1.Reverse();
            //ZonedPolygon e11_2 = new ZonedPolygon("OG2_POLY_1_HOLE_2", layer2B, OG2_POLY_1_HOLE_2);
            //e11_2.ReDefineAllZones(new List<int> { 29, 28, 27, 26 });
            //e11_2.Reverse();
            //ZonedPolygon e11_3 = new ZonedPolygon("OG2_POLY_1_HOLE_3", layer2B, OG2_POLY_1_HOLE_3);
            //e11_3.ReDefineAllZones(new List<int> { 33, 32, 31, 30 });
            //e11_3.Reverse();
            //ZonedPolygon e11_4 = new ZonedPolygon("OG2_POLY_1_HOLE_4", layer2B, OG2_POLY_1_HOLE_4);
            //e11_4.ReDefineAllZones(new List<int> { 37, 36, 35, 34 });
            //e11_4.Reverse();
            //ZonedPolygon e11_5 = new ZonedPolygon("OG2_POLY_1_HOLE_5", layer2B, OG2_POLY_1_HOLE_5);
            //e11_5.ReDefineAllZones(new List<int> { 41, 40, 39, 38 });
            //e11_5.Reverse();
            //ZonedPolygon e11_6 = new ZonedPolygon("OG2_POLY_1_HOLE_6", layer2B, OG2_POLY_1_HOLE_6);
            //e11_6.ReDefineAllZones(new List<int> { 3, 4, 11, 12, 13, 2 });
            //e11_6.Reverse();

            #endregion

            #region Test data 3

            //// test data BPH (3)
            //Point3D[] B1_OG_ar = B1_OG.ToArray();
            //Matrix3D M1 = Matrix3D.Identity;
            //M1.Rotate(new Media3D.Quaternion(new Vector3D(0, 1, 0), 5));
            //M1.Translate(new Vector3D(0, 2, 0));

            //M1.Transform(B1_OG_ar);
            //List<Point3D> B1_OG1 = new List<Point3D>(B1_OG_ar);

            //M1.Transform(B1_OG_ar);
            //List<Point3D> B1_OG2 = new List<Point3D>(B1_OG_ar);

            //M1.Transform(B1_OG_ar);
            //List<Point3D> B1_OG3 = new List<Point3D>(B1_OG_ar);

            //M1.Transform(B1_OG_ar);
            //List<Point3D> B1_OG4 = new List<Point3D>(B1_OG_ar);

            //ZonedPolygon b1_eg = new ZonedPolygon("B1_EG", layer1A, B1_EG);
            //ZonedPolygon b1_og0 = new ZonedPolygon("B1_OG", layer1A, B1_OG);
            //ZonedPolygon b1_og1 = new ZonedPolygon("B1_OG1", layer1, B1_OG1);
            //ZonedPolygon b1_og2 = new ZonedPolygon("B1_OG2", layer1A, B1_OG2);
            //ZonedPolygon b1_og3 = new ZonedPolygon("B1_OG2", layer1, B1_OG3);
            //ZonedPolygon b1_og4 = new ZonedPolygon("B1_OG2", layer1A, B1_OG4);

            //b1_eg.AddOpening(0, 1f, 0.5f, 0.25f, 2.00f);
            //b1_eg.AddOpening(0, 2f, 0.5f, 0.50f, 1.75f);
            //b1_eg.AddOpening(0, 3f, 0.5f, 0.75f, 1.50f);
            //b1_eg.AddOpening(0, 4f, 0.5f, 1.00f, 1.25f);

            //ZonedPolygon b2_eg = new ZonedPolygon("B2_EG", layer2, B2_EG);
            //ZonedPolygon b2_og1 = new ZonedPolygon("B2_OG1", layer2, B2_OG1);
            //ZonedPolygon b2_og2 = new ZonedPolygon("B2_OG2", layer2, B2_OG2);
            //ZonedPolygon b2_og3 = new ZonedPolygon("B2_OG3", layer2, B2_OG3);

            //b2_og2.AddOpening(0, 1f, 4f, 0.25f, 0.25f);
            //b2_og2.AddOpening(0, 3f, 4f, 1.00f, 0.25f);

            #endregion

            #region Test data 4

            //// test data BPH (4)
            //Point3D[] GenericPoly_ar2 = GenericPoly.ToArray();
            //Point3D[] GenericPoly_ar3 = GenericPoly.ToArray();
            //Matrix3D M2 = Matrix3D.Identity;
            //M2.Translate(new Vector3D(3, 0, 0));
            //Matrix3D M3 = Matrix3D.Identity;
            //M3.Translate(new Vector3D(0, 0, 2));

            //M3.Transform(GenericPoly_ar3);

            //M2.Transform(GenericPoly_ar2);
            //M2.Transform(GenericPoly_ar3);
            //List<Point3D> TrPoly_1 = new List<Point3D>(GenericPoly_ar2);
            //ZonedPolygon g1 = new ZonedPolygon("ROOM_1", layer1, TrPoly_1);
            //g1.Height = 2;
            //List<Point3D> TrPoly_1A = new List<Point3D>(GenericPoly_ar3);
            //ZonedPolygon g1A = new ZonedPolygon("ROOM_1A", layer1A, TrPoly_1A);
            //g1A.Height = 2;

            //M2.Transform(GenericPoly_ar2);
            //M2.Transform(GenericPoly_ar3);
            //List<Point3D> TrPoly_2 = new List<Point3D>(GenericPoly_ar2);
            //ZonedPolygon g2 = new ZonedPolygon("ROOM_2", layer1, TrPoly_2);
            //g2.Height = 3;
            //List<Point3D> TrPoly_2A = new List<Point3D>(GenericPoly_ar3);
            //ZonedPolygon g2A = new ZonedPolygon("ROOM_2A", layer1A, TrPoly_2A);
            //g2A.Height = 3;

            //M2.Transform(GenericPoly_ar2);
            //M2.Transform(GenericPoly_ar3);
            //List<Point3D> TrPoly_3 = new List<Point3D>(GenericPoly_ar2);
            //ZonedPolygon g3 = new ZonedPolygon("ROOM_3", layer1, TrPoly_3);
            //g3.Height = 4;
            //List<Point3D> TrPoly_3A = new List<Point3D>(GenericPoly_ar3);
            //ZonedPolygon g3A = new ZonedPolygon("ROOM_3A", layer1A, TrPoly_3A);
            //g3A.Height = 4;

            //M2.Transform(GenericPoly_ar2);
            //M2.Transform(GenericPoly_ar3);
            //List<Point3D> TrPoly_4 = new List<Point3D>(GenericPoly_ar2);
            //ZonedPolygon g4 = new ZonedPolygon("ROOM_4", layer1, TrPoly_4);
            //g4.Height = 5;
            //List<Point3D> TrPoly_4A = new List<Point3D>(GenericPoly_ar3);
            //ZonedPolygon g4A = new ZonedPolygon("ROOM_4A", layer1A, TrPoly_4A);
            //g4A.Height = 5;

            //M2.Transform(GenericPoly_ar2);
            //M2.Transform(GenericPoly_ar3);
            //List<Point3D> TrPoly_5 = new List<Point3D>(GenericPoly_ar2);
            //ZonedPolygon g5 = new ZonedPolygon("ROOM_5", layer1, TrPoly_5);
            //g5.Height = 6;
            //List<Point3D> TrPoly_5A = new List<Point3D>(GenericPoly_ar3);
            //ZonedPolygon g5A = new ZonedPolygon("ROOM_5A", layer1A, TrPoly_5A);
            //g5A.Height = 6;

            #endregion

            #region Test data PR 9.11.15

            //ZonedPolygon pr_9_11_15_sr1 = new ZonedPolygon("SmallRoom_01", layer2A, SmallRoom_01);
            //pr_9_11_15_sr1.Height = 3.5f;

            //ZonedPolygon pr_9_11_15_br1 = new ZonedPolygon("BigRoom_01", layer2B, BigRoom_01);
            //pr_9_11_15_br1.Height = 3.5f;
            //ZonedPolygon pr_9_11_15_br1_col = new ZonedPolygon("BigRoom_01_col", layer2B, BigRoom_01_col);
            //pr_9_11_15_br1_col.ReDefineAllZones(new List<int> { 11, 12, 13, 14 });
            //ZonedPolygon pr_9_11_15_br1_colB = new ZonedPolygon("BigRoom_01_col_base", layer2B, BigRoom_01_col_base);
            //pr_9_11_15_br1_colB.ReDefineAllZones(new List<int> { 11, 12, 13, 14 });
            //ZonedPolygon pr_9_11_15_br1_shaft = new ZonedPolygon("BigRoom_01_shaft", layer2B, BigRoom_01_shaft);
            //pr_9_11_15_br1_shaft.ReDefineAllZones(new List<int> { 21, 22, 23, 24 });
            //pr_9_11_15_br1_shaft.Height = 3.5f;

            //ZonedPolygon pr_9_11_15_br2 = new ZonedPolygon("BigRoom_02", layer2B, BigRoom_02);
            //ZonedPolygon pr_9_11_15_br2_shaft = new ZonedPolygon("BigRoom_02_shaft", layer2B, BigRoom_02_shaft);
            //pr_9_11_15_br2_shaft.ReDefineAllZones(new List<int> { 21, 22, 23, 24 });


            //ZonedPolygon pr_9_11_15_stw1 = new ZonedPolygon("StairWell_01", layer2, StairWell_01);

            //ZonedPolygon pr_9_11_15_stw2 = new ZonedPolygon("StairWell_02", layer2, StairWell_02);
            //ZonedPolygon pr_9_11_15_stw2a = new ZonedPolygon("StairWell_02a", layer2, StairWell_02a);
            //pr_9_11_15_stw2a.ReDefineAllZones(new List<int> { 11, 12, 13, 14 });

            //ZonedPolygon pr_9_11_15_stw3 = new ZonedPolygon("StairWell_03", layer2, StairWell_03);
            //pr_9_11_15_stw3.ReDefineAllZones(new List<int> { 11, 12, 13, 14 });
            //ZonedPolygon pr_9_11_15_stw3a = new ZonedPolygon("StairWell_03a", layer2, StairWell_03a);
            //pr_9_11_15_stw3a.ReDefineAllZones(new List<int> { 21, 22, 23, 24 });

            //ZonedPolygon pr_9_11_15_stw4 = new ZonedPolygon("StairWell_04", layer2, StairWell_04);
            //pr_9_11_15_stw4.ReDefineAllZones(new List<int> { 11, 12, 13, 15 });
            //ZonedPolygon pr_9_11_15_stw4a = new ZonedPolygon("StairWell_04a", layer2, StairWell_04a);
            //pr_9_11_15_stw4a.ReDefineAllZones(new List<int> { 21, 25, 23, 24 });

            //// add openings
            //pr_9_11_15_br1.AddOpening(4, 0.5f, 2f, 1.2f, 1.5f);
            //pr_9_11_15_sr1.AddOpening(0, 0.5f, 1f, 1.2f, 1.5f);

            //// add zoned volumes

            //// multiple polygons
            //ZonedVolume volume_Big = new ZonedVolume(layer2B, new List<ZonedPolygon> { pr_9_11_15_br1, pr_9_11_15_br2 });

            //// extrusion
            //ZonedVolume volume_Small = new ZonedVolume(layer2A, pr_9_11_15_sr1, new Vector3(0, pr_9_11_15_sr1.Height, 0));

            //// levels
            //ZonedPolygonGroup zpg_SW_01 = new ZonedPolygonGroup("zpg_SW_01", new List<ZonedPolygon> { pr_9_11_15_stw1 });
            //zpg_SW_01.IsBottomClosure = true;
            //ZonedPolygonGroup zpg_SW_02 = new ZonedPolygonGroup("zpg_SW_02", new List<ZonedPolygon> { pr_9_11_15_stw2 });
            //ZonedPolygonGroup zpg_SW_02a = new ZonedPolygonGroup("zpg_SW_02a", new List<ZonedPolygon> { pr_9_11_15_stw2a });
            //ZonedPolygonGroup zpg_SW_03 = new ZonedPolygonGroup("zpg_SW_03", new List<ZonedPolygon> { pr_9_11_15_stw3 });
            //ZonedPolygonGroup zpg_SW_03a = new ZonedPolygonGroup("zpg_SW_03a", new List<ZonedPolygon> { pr_9_11_15_stw3a });
            //zpg_SW_03a.IsBottomClosure = true;
            //ZonedPolygonGroup zpg_SW_04 = new ZonedPolygonGroup("zpg_SW_04", new List<ZonedPolygon> { pr_9_11_15_stw4 });
            //zpg_SW_04.IsTopClosure = true;
            //ZonedPolygonGroup zpg_SW_04a = new ZonedPolygonGroup("zpg_SW_04a", new List<ZonedPolygon> { pr_9_11_15_stw4a });
            //zpg_SW_04a.IsTopClosure = true;

            //ZonedVolume volume_SW3 = new ZonedVolume(layer2, new List<ZonedPolygonGroup> { zpg_SW_01, zpg_SW_02, zpg_SW_02a, zpg_SW_03, zpg_SW_03a, zpg_SW_04, zpg_SW_04a });

            #endregion

            #endregion

            // ---------- component (bauphysik + gebaeudetechnik) manager set-up ----- //
            this.MLManager = new MaterialManager();

            #region MLManager SETUP

            //ComponentInteraction.Material mat1 = new ComponentInteraction.Material("material 1", 0.75f, MaterialPosToWallAxisPlane.OUT);
            //ComponentInteraction.Material mat2 = new ComponentInteraction.Material("material 2", 0.25f, MaterialPosToWallAxisPlane.IN);
            //ComponentInteraction.Material mat3 = new ComponentInteraction.Material("material 3", 1.00f, MaterialPosToWallAxisPlane.MIDDLE);
            //ComponentInteraction.Material mat_COL = new ComponentInteraction.Material("COL_310cm", 3.10f, MaterialPosToWallAxisPlane.OUT);
            //ComponentInteraction.Material mat_AW_40 = new ComponentInteraction.Material("AW 40cm", 0.40f, MaterialPosToWallAxisPlane.IN);
            //ComponentInteraction.Material mat_AW_60 = new ComponentInteraction.Material("AW 60cm", 0.60f, MaterialPosToWallAxisPlane.IN);
            //ComponentInteraction.Material mat_IW_25m = new ComponentInteraction.Material("IW 25cm M", 0.25f, MaterialPosToWallAxisPlane.MIDDLE);
            //ComponentInteraction.Material mat_IW_25o = new ComponentInteraction.Material("IW 25cm O", 0.25f, MaterialPosToWallAxisPlane.OUT);
            //ComponentInteraction.Material mat_IW_25i = new ComponentInteraction.Material("IW 25cm I", 0.25f, MaterialPosToWallAxisPlane.IN);
            //ComponentInteraction.Material mat_IW_10m = new ComponentInteraction.Material("IW 10cm M", 0.10f, MaterialPosToWallAxisPlane.MIDDLE);

            //this.MLManager.AddMaterial(mat1);
            //this.MLManager.AddMaterial(mat2);
            //this.MLManager.AddMaterial(mat3);

            //this.MLManager.AddMaterial(mat_COL);
            //this.MLManager.AddMaterial(mat_AW_40);
            //this.MLManager.AddMaterial(mat_AW_60);
            //this.MLManager.AddMaterial(mat_IW_25m);
            //this.MLManager.AddMaterial(mat_IW_25o);
            //this.MLManager.AddMaterial(mat_IW_25i);
            //this.MLManager.AddMaterial(mat_IW_10m);

            #endregion

        }

        

        
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ========================================= PRIVATE UTILITY METHODS ====================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region COMMANDS
        private void OnOpenClick()
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog()
                {
                    Filter = "object files|*.obj",
                };
                if (dlg.ShowDialog().Value)
                {
                    if (File.Exists(dlg.FileName))
                    {
                        // TODO: load the model 
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "FileOpen Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnImportDXF()
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog()
                {
                    Filter = "dxf files|*.dxf"
                };
                // this causes a Securiy Exception (SQLite) if the propgram is not run as Administrator,
                // but it has no effect on the rest
                if (dlg.ShowDialog().Value)
                {
                    if (File.Exists(dlg.FileName))
                    {
                        //imports the DXF file
                        DXFConverter dxfConv = new DXFConverter();
                        dxfConv.Base = new Point3D(0, 0, 0);
                        dxfConv.LoadFromFile(dlg.FileName);
                        //// debug
                        //string test = dxfConv.ToString();
                        // transform into drawable geometry
                        this.DXFLayers = dxfConv.GetLayers();
                        this.DXFGeometry = dxfConv.ConvertToDrawable();                       
                        //// loading fonts
                        // string def = Utils.FontAssembler.GetCoordsOfFontChar(this.DXFGeometry);
                    }
                }
            }
            catch(Exception ex)
            {

                MessageBox.Show(ex.Message, "DXF File Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // MessageBox.Show(ex.StackTrace, "DXF File Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }


        private void OnSwitchCameraCommand()
        {
            PerspectiveCamera pc = this.Camera as PerspectiveCamera;
            Point3D pos = this.Camera.Position;
            Vector3D look = this.Camera.LookDirection;
            Vector3D up = this.Camera.UpDirection;

            if (pc == null)
            {
                // ortho -> perspective
                this.CameraIsPerspective = true;
                this.Camera = new PerspectiveCamera()
                {
                    Position = pos,
                    LookDirection = look,
                    UpDirection = up,
                    NearPlaneDistance = 0.1,
                    FarPlaneDistance = 1000,
                };
                double test = (this.Camera as PerspectiveCamera).FieldOfView;
            }
            else
            {
                // perspective -> ortho
                this.CameraIsPerspective = false;
                this.Camera = new OrthographicCamera()
                {
                    Position = pos,
                    LookDirection = look,
                    UpDirection = up,
                    NearPlaneDistance = 0.1,
                    FarPlaneDistance = 1000,
                    Width = 0.25 * look.Length, // default 10
                };  
            }
            
        }

        #endregion

        #region EVENT HANDLERS

        private void RManager_CompReps_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            CompRepManager crm = sender as CompRepManager;
            if (crm == null || e == null) return;

            if (e.PropertyName == "RecordChanged")
            {
                // signal change to all binding properties
                this.rmanager_comp_reps_backup = this.RManager_CompReps;
                this.RManager_CompReps = null;
                this.RManager_CompReps = this.rmanager_comp_reps_backup;
            }
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ========================================= STATIC DATA AND METHODS ====================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region STATIC GEOMETRY GENERATION
        private static MeshGeometry3D GenerateConstrPlane(Orientation orientation)
        {
            int size = GRID_SIZE * SCALE_CP;
            int offsetA = (size - GRID_SIZE) / 2;
            int offsetB = (size + GRID_SIZE) / 2;

            var x = new MeshBuilder();
            if (orientation == Orientation.XZ)
            {
                x.AddQuad(new Vector3(-offsetA, 0f, -offsetA),
                           new Vector3(-offsetA, 0f, offsetB),
                           new Vector3(offsetB, 0f, offsetB),
                           new Vector3(offsetB, 0f, -offsetA));
            }
            else if (orientation == Orientation.XY)
            {
                x.AddQuad(new Vector3(-offsetA, -offsetA, 0f),
                           new Vector3(-offsetA, offsetB, 0f),
                           new Vector3(offsetB, offsetB, 0f),
                           new Vector3(offsetB, -offsetA, 0f));
            }
            else
            {
                x.AddQuad(new Vector3(0f, -offsetA, -offsetA),
                           new Vector3(0f, -offsetA, offsetB),
                           new Vector3(0f, offsetB, offsetB),
                           new Vector3(0f, offsetB, -offsetA));
            }
            return x.ToMeshGeometry3D();
        }

        private static LineGeometry3D generateOrigin()
        {
            var x = new LineBuilder();
            x.AddLine(Vector3.Zero, Vector3.UnitX);
            x.AddLine(Vector3.Zero, Vector3.UnitY);
            x.AddLine(Vector3.Zero, Vector3.UnitZ);
            return x.ToLineGeometry3D();
        }
        #endregion

        #region STATIC ZONE GEOMETRY TEST DATA

        private static List<Point3D> TestPoly1 = new List<Point3D>()
        {
            new Point3D(-4, -2, -4),
            new Point3D( 3, -2, -4),
            new Point3D( 3, -2, 3),
            new Point3D( -4, -2, 3),
        };

        private static List<Point3D> TestPolyA1 = new List<Point3D>()
        {
            new Point3D(-2, 10,  2),
            new Point3D( 1, 10,  2),
            new Point3D( 1, 10,  3),
            new Point3D( 3, 10,  3),
            new Point3D( 5, 10, -2),
            new Point3D(-2, 10, -2),
            new Point3D(-2, 10,  1),
        };

        private static List<Point3D> TestPolyA2 = new List<Point3D>()
        {
            new Point3D(-2, 13,  2),
            new Point3D( 1, 13,  2),
            new Point3D( 1, 13,  3),
            new Point3D( 3, 13,  3),
            new Point3D( 5, 13, -2),
            new Point3D(-2, 13, -2),
            new Point3D(-2, 13,  1),
        };

        private static List<Point3D> TestPolyA3 = new List<Point3D>()
        {
            new Point3D(-3, 13,  3),
            new Point3D( 1, 13,  3),
            new Point3D( 1, 13,  3),
            new Point3D( 3, 13,  3),
            new Point3D( 5, 13, -2),
            new Point3D(-3, 13, -2),
            new Point3D(-3, 13,  1),
        };

        private static List<Point3D> TestPolyA4 = new List<Point3D>()
        {
            new Point3D(-3, 16,  3),
            new Point3D( 1, 16,  3),
            new Point3D( 1, 16,  3),
            new Point3D( 4, 16,  3),
            new Point3D( 6, 16, -2),
            new Point3D(-3.5454, 16, -2),
            new Point3D(-3, 16,  1),
        };

        private static List<Point3D> TestPolyA4_HOLE = new List<Point3D>()
        {
            new Point3D(-0.5, 16, -0.5),
            new Point3D(-0.5, 16,  0.5),
            new Point3D( 0.5, 16,  0.5),
            new Point3D( 0.5, 16, -0.5),
        };

        private static List<Point3D> TestPolyA5 = new List<Point3D>()
        {
            new Point3D(-3, 20,  3),
            new Point3D( 1, 20,  3),
            new Point3D( 1, 20,  3),
            new Point3D( 5, 20,  3),
            new Point3D( 8, 20, -4.5),
            new Point3D(-4, 20, -4.5),
            new Point3D(-3, 20,  1),
        };

        private static List<Point3D> TestPolyA5_HOLE = new List<Point3D>()
        {
            new Point3D(-1, 20, -1),
            new Point3D(-1, 20,  1),
            new Point3D( 1, 20,  1),
            new Point3D( 1, 20, -1),
        };

        private static List<Point3D> TestPolyA6 = new List<Point3D>()
        {
            new Point3D(-1, 22, -0.75),
            new Point3D( 1, 22, -0.75),
            new Point3D( 1, 22, -0.75),
            new Point3D( 4, 22, -0.75),
            new Point3D( 4.2, 22, -1.25),
            new Point3D(-1.0667, 22, -1.25),
            new Point3D(-1, 22, -0.8833),
        };

        #endregion

        #region STATIC ZONE GEOMETRY TEST DATA: Split Level w Holes

        private static List<Point3D> EG_POLY_1 = new List<Point3D>()
        {
            new Point3D(1, 0, 2),
            new Point3D(9, 0, 2),
            new Point3D(9, 0, 5),
            new Point3D(8, 0, 5),
            new Point3D(8, 0, 8),
            new Point3D(9, 0, 8),
            new Point3D(9, 0,10),
            new Point3D(3, 0,10),
        };

        private static List<Point3D> EG_POLY_2 = new List<Point3D>()
        {
            new Point3D( 9, 0.5, 2),
            new Point3D(14, 0.5, 2),
            new Point3D(16, 0.5,10),
            new Point3D( 9, 0.5,10),
            new Point3D( 9, 0.5, 8),
            new Point3D(11, 0.5, 8),
            new Point3D(11, 0.5, 5),
            new Point3D( 9, 0.5, 5),
        };

        private static List<Point3D> EG_POLY_1_HOLE_1 = new List<Point3D>()
        {
            new Point3D(5, 0, 8),
            new Point3D(7, 0, 8),
            new Point3D(7, 0, 9),
            new Point3D(5, 0, 9),
        };

        private static List<Point3D> EG_POLY_1_HOLE_2 = new List<Point3D>()
        {
            new Point3D(4, 0, 6),
            new Point3D(6, 0, 6),
            new Point3D(6, 0, 7),
            new Point3D(4, 0, 7),
        };

        private static List<Point3D> EG_POLY_1_HOLE_3 = new List<Point3D>()
        {
            new Point3D(5, 0, 4),
            new Point3D(7, 0, 4),
            new Point3D(7, 0, 5),
            new Point3D(5, 0, 5),
        };

        private static List<Point3D> EG_POLY_2_HOLE_1 = new List<Point3D>()
        {
            new Point3D(13, 0.5, 7),
            new Point3D(14, 0.5, 7),
            new Point3D(14, 0.5, 9),
            new Point3D(13, 0.5, 9),
        };

        private static List<Point3D> EG_POLY_2_HOLE_2 = new List<Point3D>()
        {
            new Point3D(12, 0.5, 3),
            new Point3D(13, 0.5, 3),
            new Point3D(13, 0.5, 4),
            new Point3D(12, 0.5, 4),
        };

        private static List<Point3D> OG1_POLY_1 = new List<Point3D>()
        {
            new Point3D(0, 3, 0),
            new Point3D(9, 3, 0),
            new Point3D(9, 3, 2),
            new Point3D(1, 3, 2),
            new Point3D(3, 3,10),
            new Point3D(9, 3,10),
            new Point3D(9, 3,12),
            new Point3D(0, 3,12),
        };

        private static List<Point3D> OG1_POLY_1A = new List<Point3D>()
        {
            new Point3D(-1, 6, 0),
            new Point3D(9, 6, 0),
            new Point3D(9, 6, 2),
            new Point3D(1, 6, 2),
            new Point3D(3, 6,10),
            new Point3D(9, 6,10),
            new Point3D(9, 6,12),
            new Point3D(-1, 6,12),
        };

        private static List<Point3D> OG1_POLY_2 = new List<Point3D>()
        {
            new Point3D( 9, 4, 0),
            new Point3D(16, 4, 0),
            new Point3D(19, 4,12),
            new Point3D( 9, 4,12),
            new Point3D( 9, 4,10),
            new Point3D(16, 4,10),
            new Point3D(14, 4, 2),
            new Point3D( 9, 4, 2),
        };

        private static List<Point3D> OG1_POLY_2A = new List<Point3D>()
        {
            new Point3D( 9, 7, 0),
            new Point3D(15, 7, 0),
            new Point3D(18, 7,12),
            new Point3D( 9, 7,12),
            new Point3D( 9, 7,10),
            new Point3D(16, 7,10),
            new Point3D(14, 7, 2),
            new Point3D( 9, 7, 2),
        };

        private static List<Point3D> OG2_POLY_1 = new List<Point3D>()
        {
            new Point3D( 1, 10, 2),
            new Point3D( 9, 10, 2),
            new Point3D(14, 10, 2),
            new Point3D(16, 10,10),
            new Point3D( 9, 10,10),
            new Point3D( 3, 10,10),
        };

        private static List<Point3D> OG2_POLY_1_HOLE_1 = new List<Point3D>()
        {
            new Point3D(5, 10, 8),
            new Point3D(7, 10, 8),
            new Point3D(7, 10, 9),
            new Point3D(5, 10, 9),
        };

        private static List<Point3D> OG2_POLY_1_HOLE_2 = new List<Point3D>()
        {
            new Point3D(4, 10, 6),
            new Point3D(6, 10, 6),
            new Point3D(6, 10, 7),
            new Point3D(4, 10, 7),
        };

        private static List<Point3D> OG2_POLY_1_HOLE_3 = new List<Point3D>()
        {
            new Point3D(5, 10, 4),
            new Point3D(7, 10, 4),
            new Point3D(7, 10, 5),
            new Point3D(5, 10, 5),
        };

        private static List<Point3D> OG2_POLY_1_HOLE_4 = new List<Point3D>()
        {
            new Point3D(13, 10, 7),
            new Point3D(14, 10, 7),
            new Point3D(14, 10, 9),
            new Point3D(13, 10, 9),
        };

        private static List<Point3D> OG2_POLY_1_HOLE_5 = new List<Point3D>()
        {
            new Point3D(12, 10, 3),
            new Point3D(13, 10, 3),
            new Point3D(13, 10, 4),
            new Point3D(12, 10, 4),
        };

        private static List<Point3D> OG2_POLY_1_HOLE_6 = new List<Point3D>()
        {
            new Point3D( 8, 10, 5),
            new Point3D( 9, 10, 5),
            new Point3D(11, 10, 5),
            new Point3D(11, 10, 8),
            new Point3D( 9, 10, 8),
            new Point3D( 8, 10, 8),
        };

        #endregion

        #region STATIC GEOMETRY TEST DATA: 'Two Buildings'

        private static List<Point3D> B1_EG = new List<Point3D>()
        {
            new Point3D(-3, 0, 3),
            new Point3D( 3, 0, 3),
            new Point3D( 3, 0,-3),
            new Point3D(-3, 0,-3),
        };

        private static List<Point3D> B1_OG = new List<Point3D>()
        {
            new Point3D(-3, 3, 3),
            new Point3D( 3, 3, 3),
            new Point3D( 3, 3,-3),
            new Point3D(-3, 3,-3),
        };

        private static List<Point3D> B2_EG = new List<Point3D>()
        {
            new Point3D(11, 0, 3),
            new Point3D(17, 0, 3),
            new Point3D(17, 0, 0),
            new Point3D(11, 0, 0),

        };

        private static List<Point3D> B2_OG1 = new List<Point3D>()
        {
            new Point3D(11, 2, 3),
            new Point3D(18, 2, 3),
            new Point3D(18, 2, 0),
            new Point3D(11, 2, 0),
        };

        private static List<Point3D> B2_OG2 = new List<Point3D>()
        {
            new Point3D(10, 2, 4),
            new Point3D(20, 2, 4),
            new Point3D(20, 2,-1),
            new Point3D(10, 2,-1),
        };

        private static List<Point3D> B2_OG3 = new List<Point3D>()
        {
            new Point3D(10, 4, 4),
            new Point3D(20, 4, 4),
            new Point3D(20, 4,-1),
            new Point3D(10, 4,-1),
        };


        #endregion

        #region STATIC GEOMETRY TEST DATA: Multiple Volumes

        private static List<Point3D> GenericPoly = new List<Point3D>()
        {
            new Point3D(0, 0, 2),
            new Point3D(3, 0, 2),
            new Point3D(3, 0, 0),
            new Point3D(0, 0, 0),
        };

        #endregion

        #region STATIC GEOMETRY TEST DATA: PR  9.11.15

        private static List<Point3D> SmallRoom_01 = new List<Point3D>()
        {
            new Point3D(5.000, 3.5,  2.000),
            new Point3D(5.000, 3.5, -1.275),
            new Point3D(3.350, 3.5, -1.275),
            new Point3D(3.350, 3.5, -3.000),
            new Point3D(1.475, 3.5, -3.000),
            new Point3D(1.475, 3.5,  2.000),
        };

        private static List<Point3D> BigRoom_01 = new List<Point3D>()
        {
            new Point3D(-1.525, 3.5,  4.000),
            new Point3D(-1.525, 3.5, -3.000),
            new Point3D(-6.350, 3.5, -3.000),
            new Point3D(-6.350, 3.5,  0.400),
            new Point3D(-6.464, 3.5,  0.400),
            new Point3D(-6.464, 3.5,  4.000),
        };

        private static List<Point3D> BigRoom_01_col = new List<Point3D>()
        {
            new Point3D(-3.200, 3.5, 2.000),
            new Point3D(-3.200, 3.5, 1.600),
            new Point3D(-4.000, 3.5, 1.600),
            new Point3D(-4.000, 3.5, 2.000),
        };

        private static List<Point3D> BigRoom_01_col_base = new List<Point3D>()
        {
            new Point3D(-3.200, 3.1, 2.000),
            new Point3D(-3.200, 3.1, 1.600),
            new Point3D(-4.000, 3.1, 1.600),
            new Point3D(-4.000, 3.1, 2.000),
        };

        private static List<Point3D> BigRoom_01_shaft = new List<Point3D>()
        {
            new Point3D(-3.250, 3.5, 2.950),
            new Point3D(-3.250, 3.5, 2.000),
            new Point3D(-3.950, 3.5, 2.000),
            new Point3D(-3.950, 3.5, 2.950),
        };

        private static List<Point3D> BigRoom_02 = new List<Point3D>()
        {
            new Point3D(-1.525, 7,  4.000),
            new Point3D(-1.525, 7, -3.000),
            new Point3D(-6.350, 7, -3.000),
            new Point3D(-6.350, 7,  0.400),
            new Point3D(-7.464, 7,  0.400),
            new Point3D(-7.464, 7,  4.000),
        };

        private static List<Point3D> BigRoom_02_shaft = new List<Point3D>()
        {
            new Point3D(-3.250, 7, 2.950),
            new Point3D(-3.250, 7, 2.000),
            new Point3D(-3.950, 7, 2.000),
            new Point3D(-3.950, 7, 2.950),
        };

        private static List<Point3D> StairWell_01 = new List<Point3D>()
        {
            new Point3D( 1.750, -2.5,  2.000),
            new Point3D( 1.750, -2.5, -3.000),
            new Point3D(-1.650, -2.5, -3.000),
            new Point3D(-1.650, -2.5,  2.000),
        };

        private static List<Point3D> StairWell_02 = new List<Point3D>()
        {
            new Point3D( 1.750, 0,  2.000),
            new Point3D( 1.750, 0, -3.000),
            new Point3D(-1.650, 0, -3.000),
            new Point3D(-1.650, 0,  2.000),
        };

        private static List<Point3D> StairWell_02a = new List<Point3D>()
        {
            new Point3D( 1.450, 0,  2.000),
            new Point3D( 1.450, 0, -3.000),
            new Point3D(-1.650, 0, -3.000),
            new Point3D(-1.650, 0,  2.000),
        };

        private static List<Point3D> StairWell_03 = new List<Point3D>()
        {
            new Point3D( 1.450, 3.5,  2.000),
            new Point3D( 1.450, 3.5, -3.000),
            new Point3D(-1.650, 3.5, -3.000),
            new Point3D(-1.650, 3.5,  2.000),
        };

        private static List<Point3D> StairWell_03a = new List<Point3D>()
        {
            new Point3D( 1.600, 3.5,  4.000),
            new Point3D( 1.600, 3.5,  2.000),
            new Point3D(-1.650, 3.5,  2.000),
            new Point3D(-1.650, 3.5,  4.000),
        };

        private static List<Point3D> StairWell_04 = new List<Point3D>()
        {
            new Point3D( 1.450, 7,  2.000),
            new Point3D( 1.450, 7, -3.000),
            new Point3D(-1.650, 7, -3.000),
            new Point3D(-1.650, 7,  2.000),
        };

        private static List<Point3D> StairWell_04a = new List<Point3D>()
        {
            new Point3D( 1.600, 7,  4.000),
            new Point3D( 1.600, 7,  2.000),
            new Point3D(-1.650, 7,  2.000),
            new Point3D(-1.650, 7,  4.000),
        };

        #endregion

    }
}
