using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Visual = System.Windows.Media.Visual;
using System.Windows.Media.Media3D;
using System.Windows.Data;
using System.ComponentModel;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;

using GeometryViewer.Utils;
using GeometryViewer.HelixToolkitCustomization;

namespace GeometryViewer
{
    public struct GeometryConnection
    {
        public int prev;
        public int next;
    }
    public enum NrSegmentsInLine { NR_SEGMENTS_NONE = 0, NR_SEGMENTS_DUMMY = 1, NR_SEGMENTS_LINES = 2 };

    public class LineGenerator3D : GroupModel3Dext
    {
        public static readonly float SIZE_MARKER = 0.05f;
        public static readonly float POINT_BREAKING_TOLERANCE = 0.25f;
        public static readonly double EQUALITY_THRESHOLD = 0.01;

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ==================================== DEPENDENCY PROPERTIES FOR BINDING ================================= //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // ---------------------------------- WITH USER MESH AS CONSRUCTION PLANE --------------------------------- //

        #region Construction_Plane
        public Point3D HitPointCp
        {
            get { return (Point3D)GetValue(HitPointCpProperty); }
            set { SetValue(HitPointCpProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HitPointCp.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HitPointCpProperty =
            DependencyProperty.Register("HitPointCp", typeof(Point3D), typeof(LineGenerator3D),
            new UIPropertyMetadata(new Point3D(), new PropertyChangedCallback(MyHitPointCpPropertyChangedCallback),
                                         new CoerceValueCallback(MyHitPointCpCoerceValueCallback)));
        private static void MyHitPointCpPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }
        private static object MyHitPointCpCoerceValueCallback(DependencyObject d, object value)
        {
            return value;
        }

        public Point3D HitPointPrevCp
        {
            get { return (Point3D)GetValue(HitPointPrevCpProperty); }
            set { SetValue(HitPointPrevCpProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HitPointPrevCp.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HitPointPrevCpProperty =
            DependencyProperty.Register("HitPointPrevCp", typeof(Point3D), typeof(LineGenerator3D),
            new UIPropertyMetadata(new Point3D(), new PropertyChangedCallback(MyHitPointPrevCpPropertyChangedCallback),
                                         new CoerceValueCallback(MyHitPointPrevCpCoerceValueCallback)));
        private static void MyHitPointPrevCpPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }
        private static object MyHitPointPrevCpCoerceValueCallback(DependencyObject d, object value)
        {
            return value;
        }

        public int NrConseqHitsCp
        {
            get { return (int)GetValue(NrConseqHitsCpProperty); }
            set { SetValue(NrConseqHitsCpProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NrConseqHitsCp.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NrConseqHitsCpProperty =
            DependencyProperty.Register("NrConseqHitsCp", typeof(int), typeof(LineGenerator3D),
            new UIPropertyMetadata(0, new PropertyChangedCallback(MyNrConseqHitsCpPropertyChangedCallback),
                                         new CoerceValueCallback(MyNrConseqHitsCpCoerceValueCallback)));
        private static void MyNrConseqHitsCpPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LineGenerator3D lg = d as LineGenerator3D;
            int newVal = (int)e.NewValue;
            if (lg != null)
            {
                if (newVal > 0)
                {
                    lg.LastHitPos = lg.HitPointCp;
                    Vector3 v = lg.LastHitPos.ToVector3();
                    lg.TransformLeadingLine(v, v, true);
                }
                if (lg.DrawMode)
                {
                    lg.DrawLinesGraphicInput();
                    if (newVal > 0)
                        lg.leadingLine.Visibility = Visibility.Visible;
                }               
            }
        }
        private static object MyNrConseqHitsCpCoerceValueCallback(DependencyObject d, object value)
        {
            return value;
        }
        #endregion

        // --------------------------------------- FOR NUMERICAL USER INPUT --------------------------------------- //

        #region Numerical_Drawing_Input
        public float UserX
        {
            get { return (float)GetValue(UserXProperty); }
            set { SetValue(UserXProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UserX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UserXProperty =
            DependencyProperty.Register("UserX", typeof(float), typeof(LineGenerator3D),
            new UIPropertyMetadata(0f, new PropertyChangedCallback(MyUserXPropertyChangedCallback),
                                         new CoerceValueCallback(MyUserXCoerceValueCallback)));

        private static void MyUserXPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }
        private static object MyUserXCoerceValueCallback(DependencyObject d, object value)
        {
            return value;
        }


        public float UserY
        {
            get { return (float)GetValue(UserYProperty); }
            set { SetValue(UserYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UserY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UserYProperty =
            DependencyProperty.Register("UserY", typeof(float), typeof(LineGenerator3D),
            new UIPropertyMetadata(0f, new PropertyChangedCallback(MyUserYPropertyChangedCallback),
                                         new CoerceValueCallback(MyUserYCoerceValueCallback)));
        private static void MyUserYPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }
        private static object MyUserYCoerceValueCallback(DependencyObject d, object value)
        {
            return value;
        }

        public float UserZ
        {
            get { return (float)GetValue(UserZProperty); }
            set { SetValue(UserZProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UserZ.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UserZProperty =
            DependencyProperty.Register("UserZ", typeof(float), typeof(LineGenerator3D),
            new UIPropertyMetadata(0f, new PropertyChangedCallback(MyUserZPropertyChangedCallback),
                                         new CoerceValueCallback(MyUserZCoerceValueCallback)));

        private static void MyUserZPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }
        private static object MyUserZCoerceValueCallback(DependencyObject d, object value)
        {
            return value;
        }

        // for counting both numerical and graphical input
        public int NrNumericPoints { get; private set; }
        public Point3D LastPoint { get; private set; }

        #endregion

        public ICommand PlaceNumericalPointCmd { get; private set; }

        // ---------------------------------- WITH LINE MANIPULATOR FOR EDITING ----------------------------------- //

        #region Manipulator

        private List<Point3D> coordsSourceOut;
        public List<Point3D> CoordsSourceOut
        {
            get { return coordsSourceOut; }
            set
            { 
                coordsSourceOut = value;
                base.RegisterPropertyChanged("CoordsSourceOut");
            }
        }

        public List<Point3D> CoordsSourceIn
        {
            get { return (List<Point3D>)GetValue(CoordsSourceInProperty); }
            set { SetValue(CoordsSourceInProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CoordsSourceIn.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CoordsSourceInProperty =
            DependencyProperty.Register("CoordsSourceIn", typeof(List<Point3D>), typeof(LineGenerator3D),
            new UIPropertyMetadata(new List<Point3D>(), new PropertyChangedCallback(MyCoordsSourceInPropertyChangedCallback),
                                         new CoerceValueCallback(MyCoordsSourceInCoerceValueCallback)));
        private static void MyCoordsSourceInPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LineGenerator3D lg = d as LineGenerator3D;
            if (lg != null)
            {
                // check for actual change
                bool change = CommonExtensions.Point3DListChanged(lg.CoordsSourceOut, lg.CoordsSourceIn, EQUALITY_THRESHOLD);
                if (!change)
                    return;

                // record the current state for the undo procedure
                if (lg.undoAction == null)
                    lg.RecordSimpleInnerState(true);

                lg.UpdateOnCoordsChange();

                // transmit undo / redo procedure
                lg.RecordSimpleInnerState(false);
                lg.TransmitActions();
            }
        }
        private static object MyCoordsSourceInCoerceValueCallback(DependencyObject d, object value)
        {
            return value;
        }

        #endregion

        // --------------- WITH LINE GROUP DISPLAY FOR SWITCHING BTW SELECTABLE AND NON-SELECTABLE ---------------- //

        #region Architecture Display (incl. DXF Imported Geometry Display)

        private List<Point3D> coords0ToARC;
        public List<Point3D> Coords0ToARC
        {
            get { return coords0ToARC; }
            set 
            {
                coords0ToARC = value;
                base.RegisterPropertyChanged("Coords0ToARC");
            }
        }

        private List<Point3D> coords1ToARC;
        public List<Point3D> Coords1ToARC
        {
            get { return coords1ToARC; }
            set 
            { 
                coords1ToARC = value;
                base.RegisterPropertyChanged("Coords1ToARC");
            }
        }

        private List<int> connectedToARC;
        public List<int> ConnectedToARC
        {
            get { return connectedToARC; }
            set 
            { 
                connectedToARC = value;
                base.RegisterPropertyChanged("ConnectedToARC");
            }
        }

        public List<Point3D> Coords0FromARC
        {
            get { return (List<Point3D>)GetValue(Coords0FromLGDProperty); }
            set { SetValue(Coords0FromLGDProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Coords0FromARC.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty Coords0FromLGDProperty =
            DependencyProperty.Register("Coords0FromARC", typeof(List<Point3D>), typeof(LineGenerator3D),
            new UIPropertyMetadata(new List<Point3D>()));

        public List<Point3D> Coords1FromARC
        {
            get { return (List<Point3D>)GetValue(Coords1FromLGDProperty); }
            set { SetValue(Coords1FromLGDProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Coords1FromARC.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty Coords1FromLGDProperty =
            DependencyProperty.Register("Coords1FromARC", typeof(List<Point3D>), typeof(LineGenerator3D),
            new UIPropertyMetadata(new List<Point3D>()));

        public List<int> ConnectedFromARC
        {
            get { return (List<int>)GetValue(ConnectedFromLGDProperty); }
            set { SetValue(ConnectedFromLGDProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ConnectedFromARC.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ConnectedFromLGDProperty =
            DependencyProperty.Register("ConnectedFromARC", typeof(List<int>), typeof(LineGenerator3D),
            new UIPropertyMetadata(new List<int>()));

        #endregion

        // ------------------------------------ WITH USER CONTROLS FOR DRAWING ------------------------------------ //

        #region Draw

        private NrSegmentsInLine nrSegments;

        private bool drawModeCopy;
        public bool DrawModeCopy
        {
            get { return this.drawModeCopy; }
            set 
            { 
                this.drawModeCopy = value;
                RegisterPropertyChanged("DrawModeCopy");
            }
        }

        public bool DrawMode
        {
            get { return (bool)GetValue(DrawModeProperty); }
            set { SetValue(DrawModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DrawMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DrawModeProperty =
            DependencyProperty.Register("DrawMode", typeof(bool), typeof(LineGenerator3D),
            new UIPropertyMetadata(false, new PropertyChangedCallback(MyDrawModePropertyChangedCallback),
                                         new CoerceValueCallback(MyDrawModeCoerceValueCallback)));
        private static void MyDrawModePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //var test1 = d;
            //var test2 = e;
            LineGenerator3D lg = d as LineGenerator3D;
            bool newVal = (bool)e.NewValue;
            if (lg != null)
            {
                lg.DrawModeCopy = newVal;
                if (newVal)
                {
                    // record the current state for the undo procedure
                    if (lg.undoAction == null)
                        lg.RecordSimpleInnerState(true);
                }
                else
                {
                    // finialize drawing session
                    lg.nrSegments = NrSegmentsInLine.NR_SEGMENTS_NONE;
                    lg.NrNumericPoints = 0;
                    lg.leadingLine.Visibility = Visibility.Hidden;
                    lg.leadingLine.Transform = new MatrixTransform3D(Matrix3D.Identity);
                    // finalize connectivity of last point
                    int n = lg.connected.Count;
                    if (n > 0)
                        lg.connected[n - 1] = -1;
                    // update the connectedness info
                    lg.connectionInfo = GetFullConnectivity(lg.connected);

                    // transmit undo / redo procedure
                    lg.RecordSimpleInnerState(false);
                    lg.TransmitActions();
                }
            }
        }
        private static object MyDrawModeCoerceValueCallback(DependencyObject d, object value)
        {
            //var test1 = d;
            //var test2 = value;
            return value;
        }

        public bool NewPointConnected
        {
            get { return (bool)GetValue(NewPointConnectedProperty); }
            set { SetValue(NewPointConnectedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NewPointConnected.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NewPointConnectedProperty =
            DependencyProperty.Register("NewPointConnected", typeof(bool), typeof(LineGenerator3D),
            new UIPropertyMetadata(false, new PropertyChangedCallback(MyNewPointConnectedPropertyChangedCallback),
                                         new CoerceValueCallback(MyNewPointConnectedCoerceValueCallback)));

        private static void MyNewPointConnectedPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //var test1 = d;
            //var test2 = e;
        }
        private static object MyNewPointConnectedCoerceValueCallback(DependencyObject d, object value)
        {
            //var test1 = d;
            //var test2 = value;
            return value;
        }

        #endregion

        #region Break / Weld
        public bool BreakPointMode
        {
            get { return (bool)GetValue(BreakPointModeProperty); }
            set { SetValue(BreakPointModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BreakPointMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BreakPointModeProperty =
            DependencyProperty.Register("BreakPointMode", typeof(bool), typeof(LineGenerator3D),
            new UIPropertyMetadata(false, new PropertyChangedCallback(MyBreakPointModePropertyChangedCallback),
                                         new CoerceValueCallback(MyBreakPointModeCoerceValueCallback)));

        private static void MyBreakPointModePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LineGenerator3D lg = d as LineGenerator3D;
            if (lg != null)
            {
                if ((bool)e.NewValue)
                {
                    // record the current state for the undo procedure
                    if (lg.undoAction == null)
                        lg.RecordSimpleInnerState(true);

                    lg.showClickedLine = true;
                }
                else
                {
                    lg.BreakOrWeldInProgress = false;

                    // transmit undo / redo procedure
                    lg.RecordSimpleInnerState(false);
                    lg.TransmitActions();
                }
            }
        }
        private static object MyBreakPointModeCoerceValueCallback(DependencyObject d, object value)
        {
            return value;
        }

        public bool WeldPointMode
        {
            get { return (bool)GetValue(WeldPointModeProperty); }
            set { SetValue(WeldPointModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for WeldPointMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WeldPointModeProperty =
            DependencyProperty.Register("WeldPointMode", typeof(bool), typeof(LineGenerator3D),
            new UIPropertyMetadata(false, new PropertyChangedCallback(MyBreakPointModePropertyChangedCallback)));


        public float BreakingTolerance
        {
            get { return (float)GetValue(BreakingToleranceProperty); }
            set { SetValue(BreakingToleranceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BreakingTolerance.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BreakingToleranceProperty =
            DependencyProperty.Register("BreakingTolerance", typeof(float), typeof(LineGenerator3D),
            new UIPropertyMetadata(POINT_BREAKING_TOLERANCE, new PropertyChangedCallback(MyBreakingTolerancePropertyChangedCallback),
                                         new CoerceValueCallback(MyBreakingToleranceCoerceValueCallback)));

        private static void MyBreakingTolerancePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }
        private static object MyBreakingToleranceCoerceValueCallback(DependencyObject d, object value)
        {
            return value;
        }

        private bool BreakOrWeldInProgress;
        private int segmentToConnectA;
        private int segmentToConnectB;
        private bool segmentToConnectA_end; // true if the coords1 entry is closer to the clicked point
        private Point3D weldPointA;
        private bool showClickedLine;

        #endregion

        #region GUI DISPLAY HELPERS

        public bool DisplayLineDir
        {
            get { return (bool)GetValue(DisplayLineDirProperty); }
            set { SetValue(DisplayLineDirProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DisplayLineDir.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DisplayLineDirProperty =
            DependencyProperty.Register("DisplayLineDir", typeof(bool), typeof(LineGenerator3D),
            new UIPropertyMetadata(true, new PropertyChangedCallback(MyDisplayLineDirPropertyChangedCallback)));

        private static void MyDisplayLineDirPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LineGenerator3D lg = d as LineGenerator3D;
            if (lg != null)
            {
                if ((bool)e.NewValue)
                    lg.lineDirectionMarkers.Visibility = Visibility.Visible;
                else
                    lg.lineDirectionMarkers.Visibility = Visibility.Hidden;
            }
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ========================================== INTERNAL CLASS MEMBERS ====================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // ---------------------------------------------- LINE GEOMETRY ------------------------------------------- //

        private List<Point3D> coords0; // first point of line P0
        private List<Point3D> coords1; // second point of line P1
        private List<int> connected; // saves the index of the line segment the point in coords1 is connected to

        private SelectableUserLine allLines;
        private LineGeometryModel3D unselectedLines;
        private LineGeometryModel3D leadingLine;
        private MeshGeometryModel3D lineEndMarkers;
        private LineGeometryModel3D clickedLine; // for break and weld
        private LineGeometryModel3D lineDirectionMarkers; // arrow showing direction from coords0 to coords1

        private static HelixToolkit.SharpDX.Wpf.Geometry3D LineGeometry;
        private static HelixToolkit.SharpDX.Wpf.Geometry3D MarkerGeometry;
        private static HelixToolkit.SharpDX.Wpf.Geometry3D ArrowGeometry;

        // ------------------------------- VISUALIZATION OF MOUSE MOVEMENT WHILE DRAWING -------------------------- //
        protected bool isCaptured;
        protected Viewport3DX viewport;
        protected HelixToolkit.SharpDX.Camera camera;

        private Point3D lastHitPos;
        public Point3D LastHitPos
        {
            get { return lastHitPos; }
            private set
            {
                this.lastHitPos = value;
                base.RegisterPropertyChanged("LastHitPos");
            }
        }

        // ---------------------------------------------- OBJECT SNAP --------------------------------------------- //
        private bool use_lastHitPos_snapOverride;
        public bool Use_LastHitPos_SnapOverride
        {
            get { return this.use_lastHitPos_snapOverride; }
            set
            { 
                this.use_lastHitPos_snapOverride = value;
                base.RegisterPropertyChanged("Use_LastHitPos_SnapOverride");
            }
        }

        private Point3D lastHitPos_snapOverride;
        public Point3D LastHitPos_SnapOverride
        {
            get { return this.lastHitPos_snapOverride; }
            set 
            { 
                this.lastHitPos_snapOverride = value;
                base.RegisterPropertyChanged("LastHitPos_SnapOverride");
            }
        }


        // -------------------------------- BINDING WITH SELECTABLELINE AS allLines ------------------------------- //

        #region Selectable_Line

        public List<int> connectedSpan { get; private set; }
        private List<GeometryConnection> connectionInfo;

        public ulong? IndexSelectedLineCp
        {
            get { return (ulong?)GetValue(IndexSelectedLineCpProperty); }
            set { SetValue(IndexSelectedLineCpProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IndexSelectedLineCp.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IndexSelectedLineCpProperty =
            DependencyProperty.Register("IndexSelectedLineCp", typeof(ulong?), typeof(LineGenerator3D),
            new UIPropertyMetadata(null, new PropertyChangedCallback(MyIndexSelectedLineCpPropertyChangedCallback)));
        private static void MyIndexSelectedLineCpPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == null)
                return;

            LineGenerator3D lg = d as LineGenerator3D;
            if (lg != null)
            {
                var parent = lg.Parent as Viewport3DXext;
                if (parent != null && parent.ActionMode == Communication.ActionType.LINE_EDIT)
                {
                    if (!lg.DrawMode && !lg.BreakPointMode && !lg.WeldPointMode)
                    {
                        lg.SelectGeometry((ulong?)e.NewValue);
                    }
                }
            }
            
        }

        public Point3D HitPosOnLine
        {
            get { return (Point3D)GetValue(HitPosOnLineProperty); }
            set { SetValue(HitPosOnLineProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HitPosOnLine.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HitPosOnLineProperty =
            DependencyProperty.Register("HitPosOnLine", typeof(Point3D), typeof(LineGenerator3D),
            new UIPropertyMetadata(new Point3D(), new PropertyChangedCallback(MyHitPosOnLinePropertyChangedCallback)));

        private static void MyHitPosOnLinePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == null)
                return;

            LineGenerator3D lg = d as LineGenerator3D;
            if (lg != null)
            {
                var parent = lg.Parent as Viewport3DXext;
                if (parent != null && parent.ActionMode == Communication.ActionType.LINE_EDIT)
                {
                    if (lg.BreakPointMode)
                    {
                        lg.SetConnectivity(false);
                    }
                    else if (lg.WeldPointMode)
                    {
                        lg.SetConnectivity(true);
                    }
                }
            }
        }

        #endregion
        
        public ICommand DeselectCmd { get; private set; }
        public ICommand DeleteSelectedCmd { get; private set; }

        public ICommand TransferToArcDisplayCmd { get; private set; }
        public ICommand UnpackFromArcDisplayCmd { get; private set; }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // =============================================== CONSTRUCTORS =========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region INITIALIZATION
        static LineGenerator3D()
        {
            var b1 = new LineBuilder();
            b1.AddLine(new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f));
            LineGeometry = b1.ToLineGeometry3D();

            var b2 = new MeshBuilder();
            b2.AddBox(Vector3.Zero, SIZE_MARKER, SIZE_MARKER, SIZE_MARKER);
            MarkerGeometry = b2.ToMeshGeometry3D();

            var b3 = new LineBuilder();
            b3.AddLine(new Vector3(-0.125f, 0f,  0.125f), new Vector3(0f, 0f, 0f));
            b3.AddLine(new Vector3(-0.125f, 0f, -0.125f), new Vector3(0f, 0f, 0f));
            ArrowGeometry = b3.ToLineGeometry3D();
        }

        public LineGenerator3D()
        {
            // deselection command
            this.DeselectCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => DeselectCommand(), (x) => CanExecute_DeselectCommand());
            // delete command
            this.DeleteSelectedCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => DeleteSelectedLineCommand(), (x) => CanExecute_DeleteSelectedLineCommand());
            // place point numerically command
            this.PlaceNumericalPointCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => PlacePointNumerically(), (x) => CanExecute_PlacePointNumerically());
            this.NrNumericPoints = 0;
            // commands for transfer of info to and from a simple displayer
            this.TransferToArcDisplayCmd =
                new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnTransferToArcDisplay(),
                                               (x) => CanExecute_OnTransferToArcDisplay());
            this.UnpackFromArcDisplayCmd =
                new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnUnpackFromArcDisplay(),
                                               (x) => CanExecute_OnUnpackFromArcDisplay());

            // initialize dummy selected lines
            this.CoordsSourceOut = new List<Point3D>();

            // initialize output for LineGroupDisplay
            this.Coords0ToARC = new List<Point3D>();
            this.Coords1ToARC = new List<Point3D>();
            this.ConnectedToARC = new List<int>();

            // initialize default coordinates
            coords0 = new List<Point3D>();

            //coords0.Add(new Point3D(2.2, 0, -2.2)); // 0
            //coords0.Add(new Point3D(0, 0, -2)); // 1
            //coords0.Add(new Point3D(-3, 0, 0)); // 2
            //coords0.Add(new Point3D(-3.2, 0, 3.2)); // 3
            //coords0.Add(new Point3D(0, 0, 3.2)); // 4
            //coords0.Add(new Point3D(-3.2, 0, 3.2)); // 5

            //coords0.Add(new Point3D(3.12, 0, -1.44)); // 0
            //coords0.Add(new Point3D(2.28, 0, 0.44)); // 1
            //coords0.Add(new Point3D(-0.37, 0, -0.07)); // 2
            //coords0.Add(new Point3D(-1.78, 0, -0.16)); // 3
            //coords0.Add(new Point3D(-2.59, 0, 1.26)); // 4
            //coords0.Add(new Point3D(-0.25, 0, 0.31)); // 5
            //coords0.Add(new Point3D(0.94, 0, 2.12)); // 6

            coords1 = new List<Point3D>();

            //coords1.Add(new Point3D(0, 0, -2)); // 0
            //coords1.Add(new Point3D(-3, 0, 0)); // 1
            //coords1.Add(new Point3D(-3.2, 0, 3.2)); // 2
            //coords1.Add(new Point3D(0, 0, 3.2)); // 3
            //coords1.Add(new Point3D(2.2, 0, -2.2)); // 4
            //coords1.Add(new Point3D(-4, 0, 4)); // 5

            //coords1.Add(new Point3D(2.28, 0, 0.44)); // 0
            //coords1.Add(new Point3D(0.30, 0, 0.09)); // 1
            //coords1.Add(new Point3D(-1.78, 0, -0.16)); // 2
            //coords1.Add(new Point3D(-2.59, 0, 1.26)); // 3
            //coords1.Add(new Point3D(-0.50, 0, 0.24)); // 4
            //coords1.Add(new Point3D(0.94, 0, 2.12)); // 5
            //coords1.Add(new Point3D(-2.26, 0, 1.52)); // 6

            connected = new List<int>();

            //connected.Add(-1); // 0
            //connected.Add(2); // 1
            //connected.Add(5); // 2
            //connected.Add(4); // 3
            //connected.Add(0); // 4
            //connected.Add(-1); // 5

            //connected.Add(1); // 0
            //connected.Add(-1); // 1
            //connected.Add(3); // 2
            //connected.Add(4); // 3
            //connected.Add(-1); // 4
            //connected.Add(6); // 5
            //connected.Add(-1); // 6

            this.connectionInfo = new List<GeometryConnection>();

            this.BreakOrWeldInProgress = false;
            this.segmentToConnectA = -1;
            this.segmentToConnectB = -1;
            this.showClickedLine = false;

            // initialize model holding all lines
            this.allLines = new SelectableUserLine()
            {
                Geometry = null,
                Color = SharpDX.Color.Black,
                Thickness = 1,
                HitTestThickness = 3,
                Visibility = Visibility.Hidden,
                IsHitTestVisible = true,
                IndexSelected = null,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
            };
            this.allLines.MouseDown3D += allLines_MouseDown3D;
            this.allLines.MouseUp3D +=allLines_MouseUp3D;
            this.Children.Add(allLines);

            // initialize unselected lines
            this.unselectedLines = new LineGeometryModel3D()
            {
                Geometry = null,
                Color = SharpDX.Color.Black,
                Thickness = 1,
                Visibility = Visibility.Visible,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
            };
            this.Children.Add(unselectedLines);

            // line showing connecting the current point with the next in draw mode
            this.leadingLine = new LineGeometryModel3D()
            {
                Geometry = LineGeometry,
                Color = SharpDX.Color.Black,
                Thickness = 0.5,
                Visibility = Visibility.Hidden,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
            };
            this.Children.Add(leadingLine);

            // boxes showing non-Connected vertices
            this.lineEndMarkers = new MeshGeometryModel3D()
            {
                Geometry = MarkerGeometry,
                Material = PhongMaterials.Black,
                Visibility = Visibility.Visible,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
            };
            this.Children.Add(this.lineEndMarkers);

            // line showing the clicked line segment while breaking or welding
            this.clickedLine = new LineGeometryModel3D()
            {
                Geometry = null,
                Color = SharpDX.Color.Yellow,
                Thickness = 4,
                Visibility = Visibility.Hidden,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
            };
            this.Children.Add(this.clickedLine);

            // lines showing the direction of the segment (coords0 to coords1)
            this.lineDirectionMarkers = new LineGeometryModel3D()
            {
                Geometry = ArrowGeometry,
                Color = SharpDX.Color.Black,
                Thickness = 0.5,
                Visibility = Visibility.Visible,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
            };
            this.Children.Add(this.lineDirectionMarkers);

            // initialize the internal data bindings
            Binding db1 = new Binding("IndexSelected");
            db1.Source = this.allLines;
            this.SetBinding(LineGenerator3D.IndexSelectedLineCpProperty, db1);
            Binding db2 = new Binding("HitPos");
            db2.Source = this.allLines;
            this.SetBinding(LineGenerator3D.HitPosOnLineProperty, db2);

            UpdateOnCoordsChange();
        } 
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ======================================= METHODS: DRAWING, SELECTION ==================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region SELECT CONNECTED LINE CHAINS

        private void SelectGeometry(ulong? _index)
        {
            // this.linesWH = CoordsToLinesWHist();
            if (!_index.HasValue)
            {
                ResetSelection();
            }
            else
            {
                // disable further selection until user presses ESC
                this.allLines.IsHitTestVisible = false;
                this.lineEndMarkers.IsHitTestVisible = false;

                int n = coords0.Count;
                int i = (int)_index.Value;

                if (i < n)
                {
                    List<int> conSpan;
                    getConnectedSpan(i, out conSpan);
                    this.connectedSpan = new List<int>(conSpan);

                    // set selected line coordinates
                    // if not filled AT CREATION TIME, change is not properly communicated in XAML
                    List<Point3D> coords_out = extractSelected(this.connectedSpan);                    
                    CoordsSourceOut = new List<Point3D>(coords_out);

                    // update the non-selected lines
                    var b = new LineBuilder();
                    for (int c = 0; c < n; c++)
                    {
                        if (IsInsideSpan(this.connectedSpan, c))
                            continue;
                        b.AddLine(P3D2V3(coords0[c]), P3D2V3(coords1[c]));
                    }
                    var newGeometry = b.ToLineGeometry3D();
                    if (newGeometry.Positions.Count() == 0)
                        this.unselectedLines.Geometry = null;
                    else
                        this.unselectedLines.Geometry = b.ToLineGeometry3D();
                }    
            }
        }

        private void getConnectedSpan(int _index, out List<int> conn)
        {
            conn = new List<int>();
            conn.Add(_index);

            int n = this.connectionInfo.Count;
            int counter = -1;
            while (counter < n + 1)
            {                
                counter++;
                int first = conn.First();
                int last = conn.Last();
                // look forwards
                int next = this.connectionInfo[last].next;
                if (next != -1 && !conn.Contains(next))
                    conn.Add(next);
                // look backwards
                int prev = this.connectionInfo[first].prev;
                if (prev != -1 && !conn.Contains(prev))
                    conn.Insert(0, prev);
            }

        }

        private List<Point3D> extractSelected(List<int> _connectedSpan)
        {
            List<Point3D> coords_out = new List<Point3D>();

            int n = _connectedSpan.Count;
            coords_out.Add(coords0[_connectedSpan[0]]);
            coords_out.Add(coords1[_connectedSpan[0]]);
            for (int i = 1; i < n; i++ )
            {
                coords_out.Add(coords1[_connectedSpan[i]]);
            }

            return coords_out;
        }

        private bool IsInsideSpan(List<int> _connectedSpan, int _index)
        {
            if (_connectedSpan == null || _connectedSpan.Count < 1)
                return false;

            return _connectedSpan.Contains(_index);
        }

        private void ResetSelection()
        {
            UpdateOnCoordsChange();

            // delete selected lines
            CoordsSourceOut = new List<Point3D>(); 

            // update all lines and the non-selected lines
            int n = coords0.Count;
            if (n > 0)
            {
                var b = new LineBuilder();
                for (int c = 0; c < n; c++)
                {
                    b.AddLine(P3D2V3(coords0[c]), P3D2V3(coords1[c]));
                }
                this.unselectedLines.Geometry = b.ToLineGeometry3D();
            }
        }
        private void DeselectCommand()
        {
            // enable selection again after user pressed a key (ENTER)
            this.allLines.IsHitTestVisible = true;
            ResetSelection();
        }
        private bool CanExecute_DeselectCommand()
        {
            return this.IndexSelectedLineCp.HasValue;
        }
        #endregion

        #region DRAW LINES
        private void DrawLinesGraphicInput()
        {
            Point3D p1 = this.HitPointCp;
            if (this.Use_LastHitPos_SnapOverride)
            {
                // OBjECT SNAP
                p1 = this.LastHitPos_SnapOverride;
                this.Use_LastHitPos_SnapOverride = false;
            }            
            DrawLine(p1);
        }
        private void DrawLinesNumericInput()
        {
            this.NrNumericPoints++;
            Point3D p1 = new Point3D(UserX, UserY, UserZ);

            this.LastHitPos = p1;
            Vector3 v = this.LastHitPos.ToVector3();
            TransformLeadingLine(v, v, true);

            DrawLine(p1);
        }

        private void PlacePointNumerically()
        {
            DrawLinesNumericInput();
        }

        private bool CanExecute_PlacePointNumerically()
        {
            return this.DrawMode;
        }

        private void DrawLine(Point3D p1)
        {
            // check if drawing is at all possible
            int NrConseqPointsTotal = this.NrConseqHitsCp + this.NrNumericPoints;
            if (NrConseqPointsTotal == 0)
                return;
            if (NrConseqPointsTotal == 1 && this.nrSegments == NrSegmentsInLine.NR_SEGMENTS_LINES)
                return;

            // draw
            int n;
            if (this.nrSegments == NrSegmentsInLine.NR_SEGMENTS_NONE)
            {
                // add dummy for mouse3Ddown to have a hit target
                coords0.Add(p1);
                coords1.Add(new Point3D(p1.X * 1.01, p1.Y * 1.01, p1.Z * 1.01));
                if (this.NewPointConnected)
                    connected.Add(coords0.Count);
                else
                    connected.Add(-1);
            }
            else if (this.nrSegments == NrSegmentsInLine.NR_SEGMENTS_DUMMY)
            {
                // remove the dummy
                n = coords0.Count;
                if (n > 0)
                {
                    coords0.RemoveAt(n - 1);
                    coords1.RemoveAt(n - 1);
                    connected.RemoveAt(n - 1);
                }

                // add the first actual line segment
                coords0.Add(this.LastPoint);
                coords1.Add(p1);
                if (this.NewPointConnected)
                    connected.Add(coords0.Count);
                else
                    connected.Add(-1);
                int ci = coords0.Count - 1;
            }
            else
            {
                // add the next line segment
                coords0.Add(this.LastPoint);
                coords1.Add(p1);
                if (this.NewPointConnected)
                    connected.Add(coords0.Count);
                else
                    connected.Add(-1);
                int ci = coords0.Count - 1;
            }

            UpdateGeometry(-1);
            this.nrSegments++;
            this.LastPoint = p1;
        }
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ======================================= METHODS: DELETING, BREAK, WELD ================================= //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region DELETE BREAK WELD
        private void DeleteSelectedLineCommand()
        {
            if (this.CoordsSourceOut.Count < 2)
                return;

            int ind_sel = (this.allLines.IndexSelected.HasValue) ? (int)this.allLines.IndexSelected.Value : -1;
            if (ind_sel == -1)
                return;

            // enable selection again after user pressed a key (ENTER)
            this.allLines.IsHitTestVisible = true;

            // record the current state for the undo procedure
            if (this.undoAction == null)
                this.RecordSimpleInnerState(true);

            // update coordiante arrays
            int n = coords0.Count;
            int skip_counter = 0;
            List<Point3D> coords0_new = new List<Point3D>();
            List<Point3D> coords1_new = new List<Point3D>();
            List<int> connected_new = new List<int>();
            for (int c = 0; c < n; c++)
            {
                if (IsInsideSpan(this.connectedSpan, c))
                {
                    skip_counter++;
                    continue;
                }
                coords0_new.Add(coords0[c]);
                coords1_new.Add(coords1[c]);
                if (connected[c] == -1)
                    connected_new.Add(-1);
                else
                    connected_new.Add(connected[c] - skip_counter);
            }
            coords0 = new List<Point3D>(coords0_new);
            coords1 = new List<Point3D>(coords1_new);
            connected = new List<int>(connected_new);

            // delete selected lines
            CoordsSourceOut = new List<Point3D>();

            UpdateGeometry(-1);

            // transmit undo / redo procedure
            this.RecordSimpleInnerState(false);
            this.TransmitActions();
        }

        private bool CanExecute_DeleteSelectedLineCommand()
        {
            return (this.CoordsSourceOut.Count >= 2);
        }

        private void SetConnectivity(bool _connect)
        {
            if (!this.BreakOrWeldInProgress)
            {  
                if (this.IndexSelectedLineCp.HasValue)
                {
                    // signal start of process
                    this.BreakOrWeldInProgress = true;

                    this.segmentToConnectA = (int)this.IndexSelectedLineCp.Value;
                    float distA0 = Vector3.Distance(this.HitPosOnLine.ToVector3(), coords0[this.segmentToConnectA].ToVector3());
                    float distA1 = Vector3.Distance(this.HitPosOnLine.ToVector3(), coords1[this.segmentToConnectA].ToVector3());

                    if (distA0 > distA1)
                    {
                        this.segmentToConnectA_end = true;
                        this.weldPointA = coords1[this.segmentToConnectA];
                    }
                    else
                    {
                        this.segmentToConnectA_end = false;
                        this.weldPointA = coords0[this.segmentToConnectA];
                    }
                }
            }
            else
            {
                if (this.IndexSelectedLineCp.HasValue)
                {
                    this.segmentToConnectB = (int)this.IndexSelectedLineCp.Value;
                    float distB0 = Vector3.Distance(this.HitPosOnLine.ToVector3(), coords0[this.segmentToConnectB].ToVector3());
                    float distB1 = Vector3.Distance(this.HitPosOnLine.ToVector3(), coords1[this.segmentToConnectB].ToVector3());

                    // determine the distance btw the points to weld
                    Point3D weldPointB;
                    if (distB0 > distB1)
                        weldPointB = coords1[this.segmentToConnectB];
                    else
                        weldPointB = coords0[this.segmentToConnectB];
                    float weldDist = Vector3.Distance(this.weldPointA.ToVector3(), weldPointB.ToVector3());

                    if (weldDist <= this.BreakingTolerance)
                    {

                        if (_connect && this.segmentToConnectA != this.segmentToConnectB)
                        {
                            // WELD POINTS
                            if (this.segmentToConnectA_end && (distB0 < distB1))
                            {
                                // A last -> B first (STANDARD)
                                this.coords1[this.segmentToConnectA] = this.coords0[this.segmentToConnectB];
                                // -------------------------------------------- disconnect B from prev!:
                                int indConnectedToB = this.GetConnectedToMe(this.segmentToConnectB);
                                if (indConnectedToB != -1)
                                    this.connected[indConnectedToB] = -1;
                                // reconnect
                                this.connected[this.segmentToConnectA] = this.segmentToConnectB;
                            }
                            else if (this.segmentToConnectA_end && (distB0 >= distB1))
                            {
                                // A last -> B last => the B segment has to be REVERSED
                                // (including all segments connected to it)

                                // disconnect from next:
                                this.connected[this.segmentToConnectB] = -1;
                                // update the connectedness info
                                this.connectionInfo = GetFullConnectivity(this.connected);
                                string test11 = PrintInternalState();
                                // reverse
                                this.ReverseConnectedSpan(this.segmentToConnectB);
                                // re-connect
                                this.coords1[this.segmentToConnectA] = this.coords0[this.segmentToConnectB];
                                this.connected[this.segmentToConnectA] = this.segmentToConnectB;
                            }
                            else if (!this.segmentToConnectA_end && (distB0 < distB1))
                            {
                                // A first -> B first => the A segment has to be REVERSED 
                                // (including all segments connected to it)

                                // -------------------------------------------- disconnect B from prev!:
                                int indConnectedToB = this.GetConnectedToMe(this.segmentToConnectB);
                                if (indConnectedToB != -1)
                                    this.connected[indConnectedToB] = -1;
                                // update the connectedness info
                                this.connectionInfo = GetFullConnectivity(this.connected);
                                string test21 = PrintInternalState();
                                // reverse
                                this.ReverseConnectedSpan(this.segmentToConnectA);
                                // re-connect
                                this.coords1[this.segmentToConnectA] = this.coords0[this.segmentToConnectB];
                                this.connected[this.segmentToConnectA] = this.segmentToConnectB;
                            }
                            else if (!this.segmentToConnectA_end && (distB0 >= distB1))
                            {
                                // A first -> B last (REVERSED STANDARD)
                                this.coords0[this.segmentToConnectA] = this.coords1[this.segmentToConnectB];
                                // -------------------------------------------- disconnect A from prev!:
                                int indConnectedToA = this.GetConnectedToMe(this.segmentToConnectA);
                                if (indConnectedToA != -1)
                                    this.connected[indConnectedToA] = -1;
                                // re-connect
                                this.connected[this.segmentToConnectB] = this.segmentToConnectA;
                            }
                        }
                        else if (!_connect && this.segmentToConnectA != this.segmentToConnectB)
                        {
                            // BREAK POINTS
                            if (this.connected[this.segmentToConnectA] == this.segmentToConnectB)
                                this.connected[this.segmentToConnectA] = -1;
                            if (this.connected[this.segmentToConnectB] == this.segmentToConnectA)
                                this.connected[this.segmentToConnectB] = -1;
                        }
                    }
                    UpdateGeometry(-1);
                    // debug:
                    string str_ci_debug = PrintInternalState();
                }
                // signal end of process               
                this.BreakPointMode = false;
                this.WeldPointMode = false;
                this.BreakOrWeldInProgress = false;
            }
        }
        #endregion

        #region IMPORT FROM DXF Display

        private void OnTransferToArcDisplay()
        {
            // send information
            this.Coords0ToARC = new List<Point3D>(this.coords0);
            this.Coords1ToARC = new List<Point3D>(this.coords1);
            this.ConnectedToARC = new List<int>(this.connected);

            // empty own containers
            this.coords0 = new List<Point3D>();
            this.coords1 = new List<Point3D>();
            this.connected = new List<int>();

            // make change visible
            UpdateGeometry(-1);    
        }
        private bool CanExecute_OnTransferToArcDisplay()
        {
            return true;
        }

        private void OnUnpackFromArcDisplay()
        {
            // transfer coordinates
            int n = this.Coords0FromARC.Count;
            int c = this.coords0.Count;
            for (int i = 0; i < n; i++)
            {
                this.coords0.Add(this.Coords0FromARC[i]);
                this.coords1.Add(this.Coords1FromARC[i]);
                this.connected.Add(this.ConnectedFromARC[i]);
                int ci = c + i;
            }
            
            // make change visible
            UpdateGeometry(-1);
        }
        private bool CanExecute_OnUnpackFromArcDisplay()
        {
            int n = this.Coords0FromARC.Count;
            int m = this.Coords1FromARC.Count;
            int k = this.ConnectedFromARC.Count;

            return (n > 0 && n == m && n == k);
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // =============================================== EVENT HANDLERS ========================================= //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Event_Handlers
        void allLines_MouseDown3D(object sender, RoutedEventArgs e)
        {
            if (this.DrawMode)
            {
                if (this.isCaptured)
                {
                    this.isCaptured = false;
                    this.camera = null;
                    this.viewport = null;
                }
            }
            else if (this.showClickedLine)
            {
                if (this.IndexSelectedLineCp.HasValue)
                {
                    LineBuilder b = new LineBuilder();
                    int index = (int)this.IndexSelectedLineCp.Value;
                    b.AddLine(coords0[index].ToVector3(), coords1[index].ToVector3());
                    this.clickedLine.Geometry = b.ToLineGeometry3D();
                    this.clickedLine.Visibility = Visibility.Visible;                   
                }
                else
                {
                    this.clickedLine.Geometry = null;
                    this.clickedLine.Visibility = Visibility.Hidden;
                }
            }

        }

        void allLines_MouseUp3D(object sender, RoutedEventArgs e)
        {
            if (this.DrawMode)
            {
                var args = e as Mouse3DEventArgs;
                if (args == null) return;
                if (args.Viewport == null) return;

                this.isCaptured = true;
                this.viewport = args.Viewport;
                this.camera = args.Viewport.Camera;
                this.LastHitPos = args.HitTestResult.PointHit;

                var normal = this.camera.LookDirection;
                var newHit = this.viewport.UnProjectOnPlane(args.Position, LastHitPos, normal);
                if (newHit.HasValue)
                {
                    this.LastHitPos = newHit.Value;
                }
            }
            else if (this.showClickedLine)
            {
                this.clickedLine.Geometry =null;
                this.clickedLine.Visibility = Visibility.Hidden;
                if (!this.BreakPointMode && !this.WeldPointMode)
                    this.showClickedLine = false;
            }
  
        }

        public void OnMouseMove(Point currentHit)
        {
            if (this.DrawMode)
            {
                if (this.isCaptured)
                {
                    var normal = this.camera.LookDirection;
                    // hit position                        
                    var newHit = this.viewport.UnProjectOnPlane(currentHit, LastHitPos, normal);
                    if (newHit.HasValue)
                    {
                        // transform line
                        TransformLeadingLine(this.LastHitPos.ToVector3(), newHit.Value.ToVector3());
                    }
                }
            }

        }

        private void TransformLeadingLine(Vector3 a, Vector3 b, bool adjust = false)
        {
            Vector3 bm = b;
            if (adjust && (Vector3.Distance(a,b) < 0.0001f))
            {
                bm = a + new Vector3(0.0001f);
            }

            Matrix L = calcTransf(a, bm);
            this.leadingLine.Transform = new MatrixTransform3D(L.ToMatrix3D());
            this.LLScale = L.ScaleVector.ToPoint3D();
            this.LLOffset = L.TranslationVector.ToPoint3D();
            this.LLVisible = this.leadingLine.Visibility.Equals(Visibility.Visible);
        }

        #endregion

        #region Leading_Line_Debug

        private Point3D llScale;
        public Point3D LLScale
        {
            get { return llScale; }
            set 
            { 
                llScale = value;
                base.RegisterPropertyChanged("LLScale");
            }
        }

        private Point3D llOffset;

        public Point3D LLOffset
        {
            get { return llOffset; }
            set
            { 
                llOffset = value;
                base.RegisterPropertyChanged("LLOffset");
            }
        }

        private bool llVisible;

        public bool LLVisible
        {
            get { return llVisible; }
            set 
            { 
                llVisible = value;
                base.RegisterPropertyChanged("LLVisible");
            }
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ============================================== UTILITY METHODS ========================================= //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Utilities UPDATE METHODS
        private void UpdateOnCoordsChange()
        {
            int n = coords0.Count;
            int ind_sel = (this.allLines.IndexSelected.HasValue) ? (int)this.allLines.IndexSelected.Value : -1;

            // transfer the coordinates of the selected line, in case it was modified   
            if (ind_sel > -1 && ind_sel < n && CoordsSourceIn.Count > 0)
            {
                int m = CoordsSourceIn.Count;
                int counter = -1;
                foreach(int index in this.connectedSpan)
                {
                    counter++;
                    this.coords0[index] = CoordsSourceIn[counter % m];
                    this.coords1[index] = CoordsSourceIn[(counter + 1) % m];
                }

            }
            UpdateGeometry(ind_sel);
        }

        private void UpdateGeometry(int _selInd)
        {
            int n = coords0.Count;

            // update all lines
            var b1 = new LineBuilder();
            var b2 = new LineBuilder();
            for (int i = 0; i < n; i++)
            {
                b1.AddLine(P3D2V3(coords0[i]), P3D2V3(coords1[i]));
                if (_selInd > -1 && IsInsideSpan(this.connectedSpan, i))
                    continue;
                b2.AddLine(P3D2V3(coords0[i]), P3D2V3(coords1[i]));
            }

            var newGeometry1 = b1.ToLineGeometry3D();
            if (newGeometry1.Positions.Count() == 0)
                this.allLines.Geometry = null;
            else
                this.allLines.Geometry = b1.ToLineGeometry3D();

            var newGeometry2 = b2.ToLineGeometry3D();
            if (newGeometry2.Positions.Count() == 0)
                this.unselectedLines.Geometry = null;
            else
                this.unselectedLines.Geometry = b2.ToLineGeometry3D();

            // update the connectedness info
            this.connectionInfo = GetFullConnectivity(this.connected);
            // string str_ci_debug = PrintInternalState();

            // update the end markers
             List<SharpDX.Matrix> instancesEM = new List<Matrix>();
            for (int c = 0; c < n; c++)
            {
                if (this.connectionInfo[c].next == -1)
                    instancesEM.Add(Matrix.Translation(coords1[c].ToVector3()));
                if (this.connectionInfo[c].prev == -1)
                    instancesEM.Add(Matrix.Translation(coords0[c].ToVector3()));
            }
            this.lineEndMarkers.Instances = instancesEM;
            // update the line direction markers
            List<SharpDX.Matrix> instancesDM = new List<Matrix>();
            for (int c = 0; c < n; c++)
            {
                Vector3 mid = new Vector3((float)(coords0[c].X + coords1[c].X) * 0.5f,
                                          (float)(coords0[c].Y + coords1[c].Y) * 0.5f,
                                          (float)(coords0[c].Z + coords1[c].Z) * 0.5f);
                Matrix Tr = calcTransf(mid, coords1[c].ToVector3());
                instancesDM.Add(Tr);
            }
            this.lineDirectionMarkers.Instances = instancesDM;
 
        }


        private void ReverseConnectedSpan(int _index)
        {
            List<int> conSp;
            getConnectedSpan(_index, out conSp);

            int m = conSp.Count;
            for(int i = 0; i < m; i++)
            {
                // coordinates
                Point3D tmp = coords0[conSp[i]];
                coords0[conSp[i]] = coords1[conSp[i]];
                coords1[conSp[i]] = tmp;

                // connection
                if (i > 0)
                    connected[conSp[i]] = conSp[i - 1];
                else
                    connected[conSp[i]] = -1;
            }
        }

        private int GetConnectedToMe(int _index)
        {
            int n = this.connected.Count;
            for(int i = 0; i < n; i++)
            {
                if (this.connected[i] == _index)
                    return i;
            }

            return -1;
        }

        private string PrintInternalState()
        {
            string output = "";
            int n = this.coords0.Count;
            for(int i = 0; i < n; i++)
            {
                output += String.Format("[{0:00}]: S[{1: 0.00;-0.00; 0   } {2: 0.00;-0.00; 0   } {3: 0.00;-0.00; 0   }]->E[{4: 0.00;-0.00; 0   } {5: 0.00;-0.00; 0   } {6: 0.00;-0.00; 0   }] p[{7: 0;-0}] n[{8: 0;-0}]\n",
                                        i,
                                        this.coords0[i].X, this.coords0[i].Y, this.coords0[i].Z,
                                        this.coords1[i].X, this.coords1[i].Y, this.coords1[i].Z,
                                        this.connectionInfo[i].prev, this.connectionInfo[i].next);
            }

            return output;
        }

        #endregion

        #region Utilities TRANSFORMS, CONVERTER
        private static SharpDX.Matrix calcTransf(Vector3 a, Vector3 b)
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

        private static Vector3 P3D2V3(Point3D p)
        {
            return new Vector3((float)p.X, (float)p.Y, (float)p.Z);
        }

        private static Vector3[] P3D2V3array(Point3D[] ps)
        {
            Vector3[] result;
            int n = ps.Count();
            result = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                result[i] = P3D2V3(ps[i]);
            }
            return result;
        }
        #endregion

        #region CONNECTIVITY
        private static List<GeometryConnection> GetFullConnectivity(List<int> _connectionsToNext)
        {
            List<GeometryConnection> connections = new List<GeometryConnection>();

            if (_connectionsToNext == null)
                return connections;
            int n = _connectionsToNext.Count;
            if (n == 0)
                return connections;

            for (int i = 0; i < n; i++ )
            {
                GeometryConnection gc = new GeometryConnection();
                gc.prev = -1;
                gc.next = (_connectionsToNext[i] < n) ? _connectionsToNext[i] : -1;

                for(int j = 0; j < n; j++)
                {
                    if (j == i)
                        continue;
                    if (_connectionsToNext[j] == i)
                    {
                        gc.prev = j;
                        break;
                    }
                }
                connections.Add(gc);
            }

            return connections;
        }

        private static string PrintFullConnectivity(List<GeometryConnection> _connections)
        {
            string output = "";
            if (_connections == null || _connections.Count < 1)
                return output;

            int n = _connections.Count;
            for (int i = 0; i < n; i++ )
            {
                output += String.Format("{0}:<{1} {2}>\n", i, _connections[i].prev, _connections[i].next);
            }


            return output;
        }

        #endregion

        #region UNDO / REDO
        private void RecordSimpleInnerState(bool forUndo)
        {
            List<Point3D> c0 = new List<Point3D>(this.coords0);
            List<Point3D> c1 = new List<Point3D>(this.coords1);
            List<int> cn = new List<int>(this.connected);
            int n = c0.Count;

            Func<int> action = delegate
            {
                this.coords0 = c0;
                this.coords1 = c1;
                this.connected = cn;
                this.UpdateGeometry(-1);
                return n;
            };

            if (forUndo && this.undoAction == null)
            {
                this.undoAction = action;
            }
            if (!forUndo && this.redoAction == null)
            {
                this.redoAction = action;
            }
        }
        #endregion

    }
}
