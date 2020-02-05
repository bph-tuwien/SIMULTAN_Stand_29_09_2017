using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.Windows.Data;
using System.Globalization;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;

using InterProcCommunication.Specific;

using GeometryViewer.HelixToolkitCustomization;
using GeometryViewer.ComponentReps;

namespace GeometryViewer
{
    public enum PathEditingMode
    {
        NEUTRAL = 0,
        ADD_POINT_SINGLE = 1,
        ADD_POINT_MULTIPLE = 2,
        MOVE_POINT_SINGLE = 3,
        MOVE_POINT_MULTIPLE = 4,
        DELETE_POINT = 5        
    }
    public class ComponentDisplay : GroupModel3Dext
    {
        #region STATIC: PathEditingMode

        public const int TOLERANCE_FACTOR = 100;

        public static string PathEditingModeToString(PathEditingMode _mode)
        {
            switch(_mode)
            {
                case PathEditingMode.ADD_POINT_SINGLE:
                    return "ADD_POINT_SINGLE";
                case PathEditingMode.ADD_POINT_MULTIPLE:
                    return "ADD_POINT_MULTIPLE";
                case PathEditingMode.MOVE_POINT_SINGLE:
                    return "MOVE_POINT_SINGLE";
                case PathEditingMode.MOVE_POINT_MULTIPLE:
                    return "MOVE_POINT_MULTIPLE";
                case PathEditingMode.DELETE_POINT:
                    return "DELETE_POINT";
                default:
                    return "NEUTRAL";
            }
        }

        public static PathEditingMode StringToPathEditingMode(string _str_mode)
        {
            if (string.IsNullOrEmpty(_str_mode)) return PathEditingMode.NEUTRAL;

            switch(_str_mode)
            {
                case "ADD_POINT_SINGLE":
                    return PathEditingMode.ADD_POINT_SINGLE;
                case "ADD_POINT_MULTIPLE":
                    return PathEditingMode.ADD_POINT_MULTIPLE;
                case "MOVE_POINT_SINGLE":
                    return PathEditingMode.MOVE_POINT_SINGLE;
                case "MOVE_POINT_MULTIPLE":
                    return PathEditingMode.MOVE_POINT_MULTIPLE;
                case "DELETE_POINT":
                    return PathEditingMode.DELETE_POINT;
                default:
                    return PathEditingMode.NEUTRAL;
            }
        }

        #endregion

        #region STATIC: Display Materials

        private static PhongMaterial MAT_MESH_NEUTRAL;
        private static PhongMaterial MAT_MESH_PLACED;
        private static Color COL_LINE_NEUTRAL;
        private static Color COL_LINE_PLACED;

        private static PhongMaterial MAT_MESH_BACKGROUND;
        private static Color COL_LINE_BACKGROUND;

        private static PhongMaterial MAT_VERT_NEUTRAL;
        private static PhongMaterial MAT_VERT_SELECTED;
        private static PhongMaterial MAT_VERT_ALIGNED_TO_SELECTED;
        private static Color COL_LINE_SELECTED;

        static ComponentDisplay()
        {
            MAT_MESH_NEUTRAL = new PhongMaterial();
            MAT_MESH_NEUTRAL.DiffuseColor = Color.OrangeRed;
            MAT_MESH_NEUTRAL.SpecularColor = Color.Black;
            MAT_MESH_NEUTRAL.SpecularShininess = 3f;

            MAT_MESH_PLACED = new PhongMaterial();
            MAT_MESH_PLACED.DiffuseColor = new Color4(0.5f, 0.0f, 0.0f, 1f);
            MAT_MESH_PLACED.SpecularColor = Color.Black;
            MAT_MESH_PLACED.SpecularShininess = 3f;

            COL_LINE_NEUTRAL = Color.OrangeRed;
            COL_LINE_PLACED = new Color(0.5f, 0.0f, 0.0f, 1f);

            MAT_MESH_BACKGROUND = new PhongMaterial();
            MAT_MESH_BACKGROUND.DiffuseColor = Color.Gray;
            MAT_MESH_BACKGROUND.SpecularColor = Color.Gray;
            MAT_MESH_BACKGROUND.SpecularShininess = 3f;

            COL_LINE_BACKGROUND = Color.DimGray;

            MAT_VERT_NEUTRAL = new PhongMaterial();
            MAT_VERT_NEUTRAL.DiffuseColor = new Color4(1f);
            MAT_VERT_NEUTRAL.AmbientColor = new Color4(0.8f, 0.8f, 0.8f, 1f);
            MAT_VERT_NEUTRAL.SpecularColor = new Color4(1f, 0.75f, 0f, 1f);
            MAT_VERT_NEUTRAL.SpecularShininess = 3;
            MAT_VERT_NEUTRAL.EmissiveColor = new Color4(0.5f, 0.5f, 0.5f, 1f);

            MAT_VERT_SELECTED = new PhongMaterial();
            MAT_VERT_SELECTED.DiffuseColor = new Color4(1f, 0f, 0f, 1f);
            MAT_VERT_SELECTED.AmbientColor = new Color4(0.8f, 0.4f, 0.4f, 1f);
            MAT_VERT_SELECTED.SpecularColor = new Color4(1f, 0.75f, 0f, 1f);
            MAT_VERT_SELECTED.SpecularShininess = 3;
            MAT_VERT_SELECTED.EmissiveColor = new Color4(0.8f, 0f, 0f, 1f);

            MAT_VERT_ALIGNED_TO_SELECTED = new PhongMaterial();
            MAT_VERT_ALIGNED_TO_SELECTED.DiffuseColor = new Color4(0f, 0f, 1f, 1f);
            MAT_VERT_ALIGNED_TO_SELECTED.AmbientColor = new Color4(0.4f, 0.4f, 0.8f, 1f);
            MAT_VERT_ALIGNED_TO_SELECTED.SpecularColor = new Color4(0f, 0.75f, 1f, 1f);
            MAT_VERT_ALIGNED_TO_SELECTED.SpecularShininess = 3;
            MAT_VERT_ALIGNED_TO_SELECTED.EmissiveColor = new Color4(0f, 0f, 0.8f, 1f);

            COL_LINE_SELECTED = Color.White;
        }

        #endregion

        #region PROPERTIES: Selection

        public CompRepInfo SelectedCompRep
        {
            get { return (CompRepInfo)GetValue(SelectedCompRepProperty); }
            set { SetValue(SelectedCompRepProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedCompRep.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedCompRepProperty =
            DependencyProperty.Register("SelectedCompRep", typeof(CompRepInfo), typeof(ComponentDisplay),
            new UIPropertyMetadata(null, new PropertyChangedCallback(SelectedCompRepPropertyChangedCallback)));

        private static void SelectedCompRepPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ComponentDisplay instance = d as ComponentDisplay;
            if (instance == null) return;

            instance.ShowCS = false;
            instance.SelectedIsConnectingInstance = (instance.SelectedCompRep is CompRepConnects_Instance);
            instance.SelectedIsContainedInInstance = (instance.SelectedCompRep is CompRepContainedIn_Instance);
            instance.ManageDisplayAccordingToSelection();
        }


        public bool SelectedIsConnectingInstance
        {
            get { return (bool)GetValue(SelectedIsConnectingInstanceProperty); }
            set { SetValue(SelectedIsConnectingInstanceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedIsConnectingInstance.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedIsConnectingInstanceProperty =
            DependencyProperty.Register("SelectedIsConnectingInstance", typeof(bool), typeof(ComponentDisplay),
            new UIPropertyMetadata(false));



        public bool SelectedIsContainedInInstance
        {
            get { return (bool)GetValue(SelectedIsContainedInInstanceProperty); }
            set { SetValue(SelectedIsContainedInInstanceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedIsContainedInInstance.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedIsContainedInInstanceProperty =
            DependencyProperty.Register("SelectedIsContainedInInstance", typeof(bool), typeof(ComponentDisplay), 
            new UIPropertyMetadata(false));


        #endregion

        #region PROPERTIES: Connection to Component Representations

        public CompRepManager CompRepMANAGER
        {
            get { return (CompRepManager)GetValue(CompRepMANAGERProperty); }
            set { SetValue(CompRepMANAGERProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CompRepMANAGER.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CompRepMANAGERProperty =
            DependencyProperty.Register("CompRepMANAGER", typeof(CompRepManager), typeof(ComponentDisplay),
            new UIPropertyMetadata(null, new PropertyChangedCallback(CompRepMANAGERPropertyChangedCallback),
                new CoerceValueCallback(CompRepMANAGERCoerceValueCallback)));

        private static object CompRepMANAGERCoerceValueCallback(DependencyObject d, object baseValue)
        {
            //ComponentDisplay instance = d as ComponentDisplay;
            //if (instance == null) return baseValue;

            //if (instance.CompRepMANAGER != null)
            //    instance.CompRepMANAGER.PropertyChanged -= instance.comp_rep_man_PropertyChanged;

            return baseValue;
        }

        private static void CompRepMANAGERPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ComponentDisplay instance = d as ComponentDisplay;
            if (instance == null) return;
            if (instance.CompRepMANAGER == null) return;

            //instance.CompRepMANAGER.PropertyChanged += instance.comp_rep_man_PropertyChanged;
            instance.UpdateCompRepList();
            instance.SelectedCompRep = null;
        }

        public List<CompRepInfo> ExistingCompReps
        {
            get { return (List<CompRepInfo>)GetValue(ExistingCompRepsProperty); }
            set { SetValue(ExistingCompRepsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ExistingCompReps.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ExistingCompRepsProperty =
            DependencyProperty.Register("ExistingCompReps", typeof(List<CompRepInfo>), typeof(ComponentDisplay),
            new UIPropertyMetadata(new List<CompRepInfo>(), new PropertyChangedCallback(ExistingCompRepsPropertyChangedCallback)));

        private static void ExistingCompRepsPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ComponentDisplay instance = d as ComponentDisplay;
            if (instance == null) return;
            if (instance.CompRepMANAGER == null) return;

            instance.DescribingCompRepsLoaded = (instance.CompRepMANAGER.GetNrOfLoaded(Relation2GeomType.DESCRIBES) > 0);
            instance.AlignedWithCompRepsLoaded = (instance.CompRepMANAGER.GetNrOfLoaded(Relation2GeomType.ALIGNED_WITH) > 0);
            instance.ContainedInCompRepsLoaded = (instance.CompRepMANAGER.GetNrOfLoaded(Relation2GeomType.CONTAINED_IN) > 0);
        }


        // just for user info display

        public bool DescribingCompRepsLoaded
        {
            get { return (bool)GetValue(DescribingCompRepsLoadedProperty); }
            set { SetValue(DescribingCompRepsLoadedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DescribingCompRepsLoaded.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DescribingCompRepsLoadedProperty =
            DependencyProperty.Register("DescribingCompRepsLoaded", typeof(bool), typeof(ComponentDisplay), new UIPropertyMetadata(false));


        public bool AlignedWithCompRepsLoaded
        {
            get { return (bool)GetValue(AlignedWithCompRepsLoadedProperty); }
            set { SetValue(AlignedWithCompRepsLoadedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AlignedWithCompRepsLoaded.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AlignedWithCompRepsLoadedProperty =
            DependencyProperty.Register("AlignedWithCompRepsLoaded", typeof(bool), typeof(ComponentDisplay), new UIPropertyMetadata(false));


        public bool ContainedInCompRepsLoaded
        {
            get { return (bool)GetValue(ContainedInCompRepsLoadedProperty); }
            set { SetValue(ContainedInCompRepsLoadedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ContainedInCompRepsLoaded.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ContainedInCompRepsLoadedProperty =
            DependencyProperty.Register("ContainedInCompRepsLoaded", typeof(bool), typeof(ComponentDisplay), new UIPropertyMetadata(false));


        #endregion

        #region PROPERTIES: Placement Translation

        public bool ShowCS
        {
            get { return (bool)GetValue(ShowCSProperty); }
            set { SetValue(ShowCSProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowCS.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowCSProperty =
            DependencyProperty.Register("ShowCS", typeof(bool), typeof(ComponentDisplay),
            new UIPropertyMetadata(false, new PropertyChangedCallback(ShowCSPropertyChangedCallback)));

        private static void ShowCSPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ComponentDisplay instance = d as ComponentDisplay;
            if (instance == null) return;

            instance.AdaptCoordinateSystemToSelection();
        }

        public double TranslationStep
        {
            get { return (double)GetValue(TranslationStepProperty); }
            set { SetValue(TranslationStepProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TranslationStep.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TranslationStepProperty =
            DependencyProperty.Register("TranslationStep", typeof(double), typeof(ComponentDisplay), 
            new UIPropertyMetadata(0.1));

        #endregion

        #region PROPERTIES: Path Editing Mode

        public PathEditingMode PathEditMode
        {
            get { return (PathEditingMode)GetValue(PathEditModeProperty); }
            set { SetValue(PathEditModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PathEditMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PathEditModeProperty =
            DependencyProperty.Register("PathEditMode", typeof(PathEditingMode), typeof(ComponentDisplay), 
            new UIPropertyMetadata(PathEditingMode.NEUTRAL));

        public bool PathEditingModeOn
        {
            get { return (bool)GetValue(PathEditingModeOnProperty); }
            set { SetValue(PathEditingModeOnProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PathEditingModeOn.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PathEditingModeOnProperty =
            DependencyProperty.Register("PathEditingModeOn", typeof(bool), typeof(ComponentDisplay),
            new UIPropertyMetadata(false, new PropertyChangedCallback(PathEditingModeOnPropertyChangedCallback)));

        private static void PathEditingModeOnPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ComponentDisplay instance = d as ComponentDisplay;
            if (instance == null) return;

            if (instance.SelectedCompRep == null) return;

            if (instance.PathEditingModeOn)
            {
                instance.selectedCompForPathEditing = instance.SelectedCompRep as CompRepConnects_Instance;
                instance.ManageDisplayAccordingToParent(instance.selectedCompForPathEditing, 2);
            }
            else
            {
                // transfer path back to the component representation
                instance.TransferPathChanges();                
                instance.selectedCompForPathEditing = null;
                instance.PathEditMode = PathEditingMode.NEUTRAL;
                instance.undo_pathInEditMode.Clear();
                instance.redo_pathInEditMode.Clear();
                instance.ManageDisplayAccordingToSelection();
            }
            
            instance.UpdateSelectedPathDisplay();
        }

        #endregion     

        #region PROPERTIES: Path Editing Constraints

        public bool DragX
        {
            get { return (bool)GetValue(DragXProperty); }
            set { SetValue(DragXProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DragX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DragXProperty =
            DependencyProperty.Register("DragX", typeof(bool), typeof(ComponentDisplay), new UIPropertyMetadata(true));

        public bool DragY
        {
            get { return (bool)GetValue(DragYProperty); }
            set { SetValue(DragYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DragY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DragYProperty =
            DependencyProperty.Register("DragY", typeof(bool), typeof(ComponentDisplay), new UIPropertyMetadata(true));

        public bool DragZ
        {
            get { return (bool)GetValue(DragZProperty); }
            set { SetValue(DragZProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DragZ.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DragZProperty =
            DependencyProperty.Register("DragZ", typeof(bool), typeof(ComponentDisplay), new UIPropertyMetadata(true));



        public bool LinkX
        {
            get { return (bool)GetValue(LinkXProperty); }
            set { SetValue(LinkXProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LinkX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LinkXProperty =
            DependencyProperty.Register("LinkX", typeof(bool), typeof(ComponentDisplay), new UIPropertyMetadata(false));

        public bool LinkY
        {
            get { return (bool)GetValue(LinkYProperty); }
            set { SetValue(LinkYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LinkY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LinkYProperty =
            DependencyProperty.Register("LinkY", typeof(bool), typeof(ComponentDisplay), new UIPropertyMetadata(false));

        public bool LinkZ
        {
            get { return (bool)GetValue(LinkZProperty); }
            set { SetValue(LinkZProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LinkZ.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LinkZProperty =
            DependencyProperty.Register("LinkZ", typeof(bool), typeof(ComponentDisplay), new UIPropertyMetadata(false));


        #endregion

        #region CLASS MEMBERS

        public ICommand SelectNoneCmd { get; private set; }
        public ICommand ShowCompInfoCmd { get; private set; }
        public ICommand EditCompCmd { get; private set; }

        private LineGeometryModel3D selectedDisplayLines;
        private LineGeometryModel3D selectedDisplayLines_2;
        private MeshGeometryModel3D selectedDisplayMesh;

        // goemetry descriptor editing
        public ICommand DisAssociateCompRepWZonedVolumeCmd { get; private set; }

        // placable editing
        private List<MeshGeometryModel3D> csDisplay;
        public ICommand MovePlacableAlongXCmd { get; private set; }
        public ICommand MovePlacableAlongYCmd { get; private set; }
        public ICommand MovePlacableAlongZCmd { get; private set; }
        public ICommand RemovingPlacementCmd { get; private set; }
        
        // path editing
        private CompRepConnects_Instance selectedCompForPathEditing;        
        List<Point3D> pathInEditMode;

        private SelectableUserLine editablePathDisplayLine;
        private bool path_line_captured = false;
        
        private List<DraggableGeometryWoSnapModel3D> editablePathVerticesDisplayMesh;
        private bool path_vertex_captured = false;
        private DraggableGeometryWoSnapModel3D captured_vertex;
        private Dictionary<int, DraggableGeometryWoSnapModel3D> captured_vertices_for_multi_move;
        private int captured_index;
        private Point3D path_vertex_pos_old;

        // path editing visualization aids
        private LineGeometryModel3D modifying_lines;

        // path editing Ctrl+Z buffer
        Stack<List<Point3D>> undo_pathInEditMode;
        Stack<List<Point3D>> redo_pathInEditMode;

        // editing mode management
        public ICommand SwitchPathEditModeCmd { get; private set; }
        public ICommand TraverseHistoryBackwardsCmd { get; private set; }
        public ICommand TraverseHistoryForewardsCmd { get; private set; }

        #endregion

        #region .CTOR

        public ComponentDisplay()
        {
            this.AddDisplayableLines();
            this.AddDisplayableLinesSecondary();
            this.AddDisplayableMesh();
            this.AddSelectedPathDisplay();
            this.AddModifyingLinesDisplay();
            this.AddCoordinateSystemDisplay();

            this.undo_pathInEditMode = new Stack<List<Point3D>>();
            this.redo_pathInEditMode = new Stack<List<Point3D>>();

            // commands general
            this.SelectNoneCmd = new RelayCommand((x) => this.SelectedCompRep = null, (x) => this.SelectedCompRep != null);
            this.ShowCompInfoCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnShowCompRepInfoCommand(),
                                                                  (x) => CanExecute_OnShowCompRepInfoCommand()); // not in use!
            this.EditCompCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnEditCompRepCommand(),
                                                          (x) => CanExecute_OnShowCompRepInfoCommand()); // not in use!
            
            // commands for geomtry descriptors
            this.DisAssociateCompRepWZonedVolumeCmd = new RelayCommand((x) => OnDisAssociateCompRepWZonedVoume(),
                                                            (x) => CanExecute_OnDisAssociateCompRepWZonedVoume());
            
            // commands placable ediing
            this.MovePlacableAlongXCmd = new RelayCommand((x) => OnMovePlacable(x, Orientation.YZ));
            this.MovePlacableAlongYCmd = new RelayCommand((x) => OnMovePlacable(x, Orientation.XZ));
            this.MovePlacableAlongZCmd = new RelayCommand((x) => OnMovePlacable(x, Orientation.XY));
            this.RemovingPlacementCmd = new RelayCommand((x) => OnRemovingPlacement(),
                                              (x) => CanExecute_OnRemovingPlacement());

            // commands path editing
            this.SwitchPathEditModeCmd = new RelayCommand((x) => this.OnSwitchPathEditMode(x),
                                                          (x) => this.SelectedIsConnectingInstance);
            this.TraverseHistoryBackwardsCmd = new RelayCommand((x) => this.OnTraverseHistory(true),
                                                     (x) => this.CanExecute_OnTraverseHistory(true));
            this.TraverseHistoryForewardsCmd = new RelayCommand((x) => this.OnTraverseHistory(false),
                                                     (x) => this.CanExecute_OnTraverseHistory(false));
        }

        #endregion

        #region COMMANDS: Info, Edit

        private void OnShowCompRepInfoCommand()
        {
            if (this.SelectedCompRep == null)
                return;

            Window window = Window.GetWindow(this);
            if (window != null)
            {
                MainWindow mw = window as MainWindow;
                if (mw != null)
                {
                    if (this.SelectedCompRep is CompRepDescirbes)
                        mw.OpenComponentViewerForInfo(this.SelectedCompRep as CompRepDescirbes);
                }
            }
        }

        private bool CanExecute_OnShowCompRepInfoCommand()
        {
            return (this.SelectedCompRep != null);
        }

        private void OnEditCompRepCommand()
        {
            if (this.SelectedCompRep == null)
                return;

            Window window = Window.GetWindow(this);
            if (window != null)
            {
                MainWindow mw = window as MainWindow;
                if (mw != null)
                {
                    if (this.SelectedCompRep is CompRepDescirbes)
                    {
                        mw.OpenComponentViewerForEditing(this.SelectedCompRep as CompRepDescirbes);
                        this.UpdateCompRepList(this.SelectedCompRep.GR_State.Type);
                    }
                }
            }
        }

        #endregion

        #region COMMANDS: Dis-association from ZonedVolume

        /// <summary>
        /// <para>Removes the association with a ZonedVolume and destroys the automatically generated sub-components.</para>
        /// <para>The neighborhood relationships are removed and ,consequently, the referenced components representing them.</para>
        /// <para>The opposite command is OnAssociateCompRepWZonedVoume in CommandExt.</para>
        /// <para>It requires the synchronization of two display containers: ZoneGroupDisplay and ComponentDisplay.</para>
        /// </summary>
        private void OnDisAssociateCompRepWZonedVoume()
        {
            CompRepDescirbes descriptor = this.SelectedCompRep as CompRepDescirbes;
            descriptor.SetSubCompRepsTransferState(false);
            descriptor.Geom_Zone = null; // the processing logic is in Geom_Zone.set
            this.UpdateCompRepList();
        }

        private bool CanExecute_OnDisAssociateCompRepWZonedVoume()
        {
            if (this.SelectedCompRep == null) return false;
            CompRepDescirbes descriptor = this.SelectedCompRep as CompRepDescirbes;
            if (descriptor == null) return false;
            
            return true;
        }

        /// <summary>
        /// <para>This only dis-associates the instance from the ZonedVolume. It does NOT delete it.</para>
        /// <para>Deletion is possible only in the ComponentBuilder module.</para>
        /// </summary>
        private void OnRemovingPlacement()
        {
            CompRepContainedIn_Instance placed_instance = this.SelectedCompRep as CompRepContainedIn_Instance;
            placed_instance.RemovePlacement();
            this.UpdateCompRepList();
        }

        private bool CanExecute_OnRemovingPlacement()
        {
            if (this.SelectedCompRep == null) return false;
            CompRepContainedIn_Instance placed_instance = this.SelectedCompRep as CompRepContainedIn_Instance;
            if (placed_instance == null) return false;
            if (!(placed_instance.IsPlaced)) return false;

            return true;
        }

        #endregion

        #region COMMANDS: Manipulating placable

        private void OnMovePlacable(object _positive_obj, Orientation _normal_to_plane)
        {
            bool positive_dir = true;
            if (_positive_obj == null) return;

            if (_positive_obj is bool)
                positive_dir = (bool)_positive_obj;

            if (this.SelectedCompRep is CompRepContainedIn_Instance)
            {
                CompRepContainedIn_Instance selection_to_modify = this.SelectedCompRep as CompRepContainedIn_Instance;
                double amount = (positive_dir) ? Math.Abs(this.TranslationStep) : -Math.Abs(this.TranslationStep);

                switch(_normal_to_plane)
                {
                    case Orientation.YZ:
                        // along x-axis
                        selection_to_modify.MoveAlongX(amount);
                        break;
                    case Orientation.XY:
                        // along z-axis
                        selection_to_modify.MoveAlongZ(amount);
                        break;
                    case Orientation.XZ:
                        // along y-axis
                        selection_to_modify.MoveAlongY(amount);
                        break;
                }
                this.ManageDisplayAccordingToSelection();
            }
        }

        #endregion

        #region COMMANDS: Path Editing

        internal void OnSwitchPathEditMode(object _to_mode)
        {
            if (_to_mode == null) return;
            PathEditingMode new_mode = ComponentDisplay.StringToPathEditingMode(_to_mode.ToString());

            if (new_mode == this.PathEditMode)
                this.PathEditMode = PathEditingMode.NEUTRAL;
            else
                this.PathEditMode = new_mode;

            // entering new mode
            switch (this.PathEditMode)
            {
                case PathEditingMode.NEUTRAL:
                    if (this.editablePathDisplayLine != null)
                        this.editablePathDisplayLine.IsHitTestVisible = true;
                    // display reset
                    this.UpdateSelectedPathDisplayDuringEdit(-1, null);
                    break;
                case PathEditingMode.MOVE_POINT_SINGLE:
                case PathEditingMode.MOVE_POINT_MULTIPLE:
                    if (this.editablePathDisplayLine != null)
                        this.editablePathDisplayLine.IsHitTestVisible = false;
                    break;
            }
        }

        private void OnTraverseHistory(bool _backwards)
        {
            if(_backwards && this.undo_pathInEditMode.Count > 0)
            {
                this.redo_pathInEditMode.Push(new List<Point3D>(this.pathInEditMode));
                this.pathInEditMode = this.undo_pathInEditMode.Pop();
            }
            else if (!_backwards && this.redo_pathInEditMode.Count > 0)
            {
                this.undo_pathInEditMode.Push(new List<Point3D>(this.pathInEditMode));
                this.pathInEditMode = this.redo_pathInEditMode.Pop();
            }
            this.UpdateSelectedPathDisplayDuringEdit(-1, null);
        }

        private bool CanExecute_OnTraverseHistory(bool _backwards)
        {
            if (this.PathEditMode != PathEditingMode.NEUTRAL)
                return false;

            if (_backwards)
                return (this.undo_pathInEditMode.Count > 0);
            else
                return (this.redo_pathInEditMode.Count > 0);
        }

        private void TransferPathChanges()
        {
            if (this.selectedCompForPathEditing != null && this.pathInEditMode != null)
            {
                List<Point3D> internal_points = new List<Point3D>();
                for (int i = 2; i < this.pathInEditMode.Count - 1; i++)
                {
                    internal_points.Add(this.pathInEditMode[i]);
                }
                // triggers communication with the Component Builder
                this.selectedCompForPathEditing.TransferPathChange(internal_points);
            }
        }

        #endregion

        #region METHODS: Content Update

        public void UpdateCompRepList(Relation2GeomType _type = Relation2GeomType.NONE)
        {
            this.ExistingCompReps = new List<CompRepInfo>(this.CompRepMANAGER.FullCompRepRecord());      
        }

        #endregion

        #region METHODS: Display Update
        private void AddDisplayableLines()
        {
            this.selectedDisplayLines = new LineGeometryModel3D()
            {
                Geometry = null,
                Color = COL_LINE_NEUTRAL,
                Thickness = 1,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
                Tag = 1,
            };
            this.Children.Add(this.selectedDisplayLines);
        }

        private void AddDisplayableLinesSecondary()
        {
            this.selectedDisplayLines_2 = new LineGeometryModel3D()
            {
                Geometry = null,
                Color = COL_LINE_NEUTRAL,
                Thickness = 0.5,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
                Tag = 1,
            };
            this.Children.Add(this.selectedDisplayLines_2);
        }

        private void AddDisplayableMesh()
        {
            this.selectedDisplayMesh = new MeshGeometryModel3D()
            {
                Geometry = null,
                Material = MAT_MESH_NEUTRAL,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Tag = 1,
            };
            this.Children.Add(this.selectedDisplayMesh);
        }

        private void AddSelectedPathDisplay()
        {
            this.editablePathDisplayLine = new SelectableUserLine()
            {
                Geometry = null,
                Color = COL_LINE_SELECTED,
                Thickness = 1,
                HitTestThickness = 3,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = true,
                IndexSelected = null,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
                Tag = 1,
            };
            this.editablePathDisplayLine.MouseDown3D += edit_add_MouseDown3D;
            this.editablePathDisplayLine.MouseUp3D += edit_add_MouseUp3D;
            this.Children.Add(this.editablePathDisplayLine);
        }

        private void AddSelectedPathVerticesToDisplay(int _ind_selected, List<int> _inds_aligned)
        {
            this.editablePathVerticesDisplayMesh = new List<DraggableGeometryWoSnapModel3D>();

            if (this.pathInEditMode == null) return;
            int nrP = this.pathInEditMode.Count;
            if (nrP < 2) return;

            MeshBuilder mb = new MeshBuilder();
            mb.AddSphere(Vector3.Zero, 0.25, 8, 8);
            
            // we start from the second entry, the first contains connectivity information only
            for(int i = 1; i < nrP; i++)
            {
                bool is_inner_vertex = (i > 1 && i < nrP - 1);
                Matrix3D T = Matrix3D.Identity;
                T.Translate(new Vector3D(this.pathInEditMode[i].X, this.pathInEditMode[i].Y, this.pathInEditMode[i].Z));
                var m1 = new DraggableGeometryWoSnapModel3D()
                {
                    Visibility = Visibility.Visible,
                    Material = is_inner_vertex ? MAT_VERT_NEUTRAL : MAT_MESH_BACKGROUND,
                    Geometry = mb.ToMeshGeometry3D(),
                    IsHitTestVisible = is_inner_vertex,
                    Transform = new MatrixTransform3D(T),
                    Tag = i,
                    DragX = false,
                    DragY = false,
                    DragZ = false,
                };

                if (is_inner_vertex)
                {
                    m1.MouseDown3D += OnVertexMouse3DDown;
                    m1.MouseMove3D += OnVertexMouse3DMove;
                    m1.MouseUp3D += OnVertexMouse3DUp;
                }

                // handle selection highlighting
                if (i == _ind_selected)
                {
                    m1.Material = MAT_VERT_SELECTED;
                }
                if (_inds_aligned != null && _inds_aligned.Count > 0)
                {
                    if (_inds_aligned.Contains(i))
                        m1.Material = MAT_VERT_ALIGNED_TO_SELECTED;
                }

                this.editablePathVerticesDisplayMesh.Add(m1);
                this.Children.Add(m1);
            }
        }

        private void RemoveOldSelectedVerticesFromDisplay()
        {
            if (this.editablePathVerticesDisplayMesh == null) return;
            foreach (DraggableGeometryWoSnapModel3D entry in this.editablePathVerticesDisplayMesh)
            {
                this.Children.Remove(entry);
            }
        }

        private void UpdateSelectedPathDisplay()
        {
            if (this.selectedCompForPathEditing == null)
            {
                this.pathInEditMode = new List<Point3D>();

                // deactivate the line display
                this.editablePathDisplayLine.Geometry = null;
                this.editablePathDisplayLine.Visibility = System.Windows.Visibility.Collapsed;

                // remove the vertex display
                this.RemoveOldSelectedVerticesFromDisplay();

                // update the material of the other lines
                this.selectedDisplayLines.Color = COL_LINE_NEUTRAL;
                this.selectedDisplayLines_2.Color = COL_LINE_NEUTRAL;
            }
            else
            {
                // if this causes an exception, something with the comp rep generation went wrong
                this.pathInEditMode = this.selectedCompForPathEditing.GR_Relationships[0].InstPath.ToList();
                this.undo_pathInEditMode.Push(new List<Point3D>(this.pathInEditMode));

                // update the line display
                LineBuilder lb = new LineBuilder();
                CompRepConnects.AddPolyline(ref lb, this.pathInEditMode, new Vector3D(0, 0, 0), new Vector3D(0, 0, 0));
                this.editablePathDisplayLine.Geometry = lb.ToLineGeometry3D();
                this.editablePathDisplayLine.Visibility = System.Windows.Visibility.Visible;

                // update the vertex display
                this.RemoveOldSelectedVerticesFromDisplay();
                this.AddSelectedPathVerticesToDisplay(-1, null);

                // update the material of the other lines
                this.selectedDisplayLines.Color = COL_LINE_BACKGROUND;
                this.selectedDisplayLines_2.Color = COL_LINE_BACKGROUND;
            }

            // do not forget when changing the children to be rendered
            if (this.renderHost != null)
                Attach(this.renderHost);
        }

        private void UpdateSelectedPathDisplayDuringEdit(int _ind_selected, List<int> _inds_aligned)
        {
            if (this.pathInEditMode == null) return;

            // update the line display
            LineBuilder lb = new LineBuilder();
            CompRepConnects.AddPolyline(ref lb, this.pathInEditMode, new Vector3D(0, 0, 0), new Vector3D(0, 0, 0));
            this.editablePathDisplayLine.Geometry = lb.ToLineGeometry3D();
            this.editablePathDisplayLine.Visibility = System.Windows.Visibility.Visible;

            // update the vertex display
            this.RemoveOldSelectedVerticesFromDisplay();
            this.AddSelectedPathVerticesToDisplay(_ind_selected, _inds_aligned);            

            // do not forget when changing the children to be rendered
            if (this.renderHost != null)
                Attach(this.renderHost);
        }

        /// <summary>
        /// <para>Manages the visible component representations depending on the selection.</para>
        /// <para>If an entire system is selected, it shows all of its components.</para>
        /// <para>If only a part is selected, only it and its sub-components are visible.</para>
        /// </summary>
        private void ManageDisplayAccordingToSelection()
        {
            this.ManageDisplayAccordingTo(this.SelectedCompRep, -1L, -1L);
        }

        private void ManageDisplayAccordingToParent(CompRepInfo _cri, int _level)
        {
            if (this.CompRepMANAGER == null) return;
            if (_cri == null) return;

            // extract the geometric relationship to exclude (because it is displayed as selected)
            bool contains_gr_for_exclusion = (_cri is CompRepContainedIn_Instance || _cri is CompRepConnects_Instance) &&
                                             (_cri.GR_Relationships != null) && (_cri.GR_Relationships.Count > 0);
            long id_gr_to_exclude = (contains_gr_for_exclusion) ? _cri.GR_Relationships[0].GrID : -1L;
            long id_cr_whose_gr_to_exclude = (contains_gr_for_exclusion) ? _cri.CR_Parent : -1L;

            // look for the parent on the appropriate level
            CompRepInfo parent = null;
            CompRepInfo current = _cri;
            int level = _level;
            if (level == 0)
                parent = _cri;

            while (level > 0)
            {
                parent = this.CompRepMANAGER.FindById(current.CR_Parent) as CompRepInfo;
                if (parent == null)
                    level = 0;
                else
                {
                    current = parent;
                    level--;
                }
            }

            if (parent == null) return;

            this.ManageDisplayAccordingTo(parent, id_cr_whose_gr_to_exclude, id_gr_to_exclude);            
        }

        private void ManageDisplayAccordingTo(CompRepInfo _cri, long _id_cr_whose_gr_to_exclude, long _id_gr_to_exclude)
        {
            // geometry display
            if (_cri == null)
            {
                this.ResetSelectionDisplay();
            }
            else
            {
                // gather display geometry
                _cri.AdaptDisplay(_id_cr_whose_gr_to_exclude, _id_gr_to_exclude);

                // check geometry validity (to avoid System.IndexOutOfRangeException in SharpDX.dll)
                bool valid_lines = ComponentDisplay.LineGeometry3DIsDisplayable(_cri.DisplayLines);
                if (!valid_lines)
                {
                    this.ResetSelectionDisplay();
                    return;
                }

                // if valid -> update colors
                bool placable_is_placed = (_cri is CompRepContainedIn_Instance) && ((_cri as CompRepContainedIn_Instance).IsPlaced);
                try
                {
                    if (placable_is_placed)
                    {
                        this.selectedDisplayMesh.Material = MAT_MESH_PLACED;
                        this.selectedDisplayLines.Color = COL_LINE_PLACED;
                    }
                    else
                    {
                        this.selectedDisplayMesh.Material = MAT_MESH_NEUTRAL;
                        this.selectedDisplayLines.Color = COL_LINE_NEUTRAL;
                    }
                }
                catch
                {
                    // do nothing, it's irrelevant
                }                
                // update display
                this.selectedDisplayLines.Geometry = _cri.DisplayLines;
                this.selectedDisplayLines.Visibility = (_cri.DisplayLines == null) ? Visibility.Collapsed : Visibility.Visible;
                this.selectedDisplayLines_2.Geometry = _cri.DisplayLinesSecondary;
                this.selectedDisplayLines_2.Visibility = (_cri.DisplayLinesSecondary == null) ? Visibility.Collapsed : Visibility.Visible;
                this.selectedDisplayMesh.Geometry = _cri.DisplayMesh;
                this.selectedDisplayMesh.Visibility = (_cri.DisplayMesh == null) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void ResetSelectionDisplay()
        {
            this.selectedDisplayLines.Geometry = null;
            this.selectedDisplayLines.Visibility = Visibility.Collapsed;
            this.selectedDisplayLines_2.Geometry = null;
            this.selectedDisplayLines_2.Visibility = Visibility.Collapsed;
            this.selectedDisplayMesh.Geometry = null;
            this.selectedDisplayMesh.Visibility = Visibility.Collapsed;
        }

        

        private void AddModifyingLinesDisplay()
        {
            LineBuilder lb = new LineBuilder();
            lb.AddBox(Vector3.Zero, 1, 1, 1);
            this.modifying_lines = new LineGeometryModel3D()
            {
                Geometry = lb.ToLineGeometry3D(),
                Color = Color.White,
                Thickness = 0.5,
                Visibility = System.Windows.Visibility.Hidden,
                IsHitTestVisible = false,
            };
            this.Children.Add(this.modifying_lines);
        }

        #endregion

        #region METHODS: Display Update of Local CS

        private void AddCoordinateSystemDisplay()
        {
            MeshBuilder mb = new MeshBuilder();
            mb.AddArrow(Vector3.Zero, Vector3.UnitX, 0.05, 6, 12);

            this.csDisplay = new List<MeshGeometryModel3D>(3);
            
            MeshGeometryModel3D x_axis = new MeshGeometryModel3D()
            {
                Geometry = mb.ToMeshGeometry3D(),
                Material = MAT_VERT_SELECTED,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Tag = "x_axis",
            };
            this.csDisplay.Add(x_axis);
            this.Children.Add(x_axis);

            MeshGeometryModel3D z_axis = new MeshGeometryModel3D()
            {
                Geometry = mb.ToMeshGeometry3D(),
                Material = MAT_VERT_ALIGNED_TO_SELECTED,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Tag = "z_axis",
            };
            this.csDisplay.Add(z_axis);
            this.Children.Add(z_axis);

            MeshGeometryModel3D y_axis = new MeshGeometryModel3D()
            {
                Geometry = mb.ToMeshGeometry3D(),
                Material = MAT_MESH_BACKGROUND,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Tag = "Y_axis",
            };
            this.csDisplay.Add(y_axis);
            this.Children.Add(y_axis);
        }

        private void AdaptCoordinateSystemToSelection()
        {
            if (this.csDisplay == null || this.csDisplay.Count < 3)
                return;

            if (this.SelectedCompRep == null || !(this.SelectedCompRep is CompRepContainedIn_Instance) || !this.ShowCS)
            {
                this.csDisplay[0].Visibility = System.Windows.Visibility.Collapsed;
                this.csDisplay[1].Visibility = System.Windows.Visibility.Collapsed;
                this.csDisplay[2].Visibility = System.Windows.Visibility.Collapsed;
                return;
            }

            // transform the axes display
            CompRepContainedIn_Instance placable = this.SelectedCompRep as CompRepContainedIn_Instance;
            
            Matrix3D local_cs = placable.GR_Relationships[0].GrUCS;
            Vector3 cs_origin = GeometricTransformations.UnpackOrigin(local_cs).ToVector3();
            
            Vector3D cs_X = GeometricTransformations.UnpackXAxis(local_cs);
            cs_X.Normalize();
            Vector3 cs_p_on_X = cs_origin + cs_X.ToVector3();
            
            Vector3D cs_Y = GeometricTransformations.UnpackYAxis(local_cs);
            cs_Y.Normalize();
            Vector3 cs_p_on_Y = cs_origin + cs_Y.ToVector3();
            
            Vector3D cs_Z = GeometricTransformations.UnpackZAxis(local_cs);
            cs_Z.Normalize();
            Vector3 cs_p_on_Z = cs_origin + cs_Z.ToVector3();

            Matrix transf_X = Utils.CommonExtensions.CalcAlignmentTransform(cs_origin, cs_p_on_X);
            Matrix transf_Y = Utils.CommonExtensions.CalcAlignmentTransform(cs_origin, cs_p_on_Y);
            Matrix transf_Z = Utils.CommonExtensions.CalcAlignmentTransform(cs_origin, cs_p_on_Z);

            this.csDisplay[0].Visibility = System.Windows.Visibility.Visible;
            this.csDisplay[0].Transform = new MatrixTransform3D(transf_X.ToMatrix3D());
            this.csDisplay[1].Visibility = System.Windows.Visibility.Visible;
            this.csDisplay[1].Transform = new MatrixTransform3D(transf_Z.ToMatrix3D());
            this.csDisplay[2].Visibility = System.Windows.Visibility.Visible;
            this.csDisplay[2].Transform = new MatrixTransform3D(transf_Y.ToMatrix3D());

        }

        #endregion

        #region EVENT HANDLER: Content

        // not in use at the moment
        private void comp_rep_man_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            CompRepManager crm = sender as CompRepManager;
            if (crm == null || e == null) return;

            if (e.PropertyName == "RecordChanged")
            {
                if (crm.RecordChanged)
                    this.UpdateCompRepList();
            }
        }

        #endregion

        #region EVENT HANDLER: Path Editing (ADD)

        private void edit_add_MouseDown3D(object sender, RoutedEventArgs e)
        {
            if (this.PathEditMode == PathEditingMode.ADD_POINT_SINGLE ||
                this.PathEditMode == PathEditingMode.ADD_POINT_MULTIPLE)
            {
                if (this.path_line_captured)
                    this.path_line_captured = false;
            }
        }

        private void edit_add_MouseUp3D(object sender, RoutedEventArgs e)
        {
            if (this.PathEditMode == PathEditingMode.ADD_POINT_SINGLE ||
                this.PathEditMode == PathEditingMode.ADD_POINT_MULTIPLE)
            {
                ulong index = this.editablePathDisplayLine.IndexSelected.Value;
                Point3D hit = this.editablePathDisplayLine.HitPos;

                // edit local path representation
                // the change is propagated to the component representation after exiting the editing mode
                if (index < (ulong)(this.pathInEditMode.Count - 2))
                {
                    // look at the neighbors
                    Point3D prev = this.pathInEditMode[(int)index + 1];
                    Point3D next = this.pathInEditMode[(int)index + 2];
                    
                    double diffY = Math.Abs(prev.Y - next.Y);
                    double diff_prevV = (prev - hit).LengthSquared;
                    double diff_nextV = (next - hit).LengthSquared;

                    // insert new points only if they are not too close to existing ones
                    if (diff_prevV >= Utils.CommonExtensions.LINEDISTCALC_TOLERANCE * TOLERANCE_FACTOR &&
                        diff_nextV >= Utils.CommonExtensions.LINEDISTCALC_TOLERANCE * TOLERANCE_FACTOR)
                    {
                        if (this.PathEditMode == PathEditingMode.ADD_POINT_SINGLE ||
                            diffY < Utils.CommonExtensions.GENERAL_CALC_TOLERANCE * TOLERANCE_FACTOR)
                        {
                            // insert ONE new point at the correct index
                            this.pathInEditMode.Insert((int)(index + 2), hit);
                        }
                        else if (this.PathEditMode == PathEditingMode.ADD_POINT_MULTIPLE &&
                                 diffY >= Utils.CommonExtensions.GENERAL_CALC_TOLERANCE)
                        {
                            // insert TWO new point at the correct index
                            this.pathInEditMode.Insert((int)(index + 2), new Point3D(hit.X, prev.Y, hit.Z));
                            this.pathInEditMode.Insert((int)(index + 3), new Point3D(hit.X, next.Y, hit.Z));
                        }

                        // update display
                        this.UpdateSelectedPathDisplayDuringEdit((int)(index + 2), new List<int> { (int)(index + 3) });
                        // write change to history
                        this.undo_pathInEditMode.Push(new List<Point3D>(this.pathInEditMode));
                        this.redo_pathInEditMode.Clear();
                    }
                }
            }
        }

        #endregion

        #region EVENT HANDLER: Path Editing (MOVE, DELETE)

        private void OnVertexMouse3DDown(object sender, RoutedEventArgs e)
        {
            // capture vertex information
            this.captured_vertex = sender as DraggableGeometryWoSnapModel3D;
            if (this.captured_vertex == null) return;
            
            this.captured_index = -1;
            bool success = int.TryParse(this.captured_vertex.Tag.ToString(), out this.captured_index);
            if (!success) return;

            this.path_vertex_captured = true;

            // change material as feedback
            this.captured_vertex.Material = MAT_VERT_SELECTED;

            // mode-specific processing
            if (this.PathEditMode == PathEditingMode.MOVE_POINT_SINGLE ||
                this.PathEditMode == PathEditingMode.MOVE_POINT_MULTIPLE)
            {
                Application.Current.MainWindow.Cursor = Cursors.SizeAll;

                // reset the modifying lines
                this.path_vertex_pos_old = this.pathInEditMode[this.captured_index];
                Matrix ML_t = Matrix.Translation(this.path_vertex_pos_old.ToVector3());
                Matrix ML_new = Matrix.Scaling(0f) * ML_t;
                this.modifying_lines.Transform = new MatrixTransform3D(ML_new.ToMatrix3D());
                this.modifying_lines.Visibility = Visibility.Visible;
                
                // capture the vertices to move
                if (this.PathEditMode == PathEditingMode.MOVE_POINT_SINGLE)
                {
                    this.captured_vertex.OnMouseDownOverride(this.pathInEditMode[this.captured_index]);
                }
                else if (this.PathEditMode == PathEditingMode.MOVE_POINT_MULTIPLE)
                {
                    this.captured_vertices_for_multi_move = this.CaptureAlignedVertices(this.LinkX, this.LinkY, this.LinkZ);
                }
            }
        }

        private void OnVertexMouse3DUp(object sender, RoutedEventArgs e)
        {
            // check if the sender is the captured vertex
            if (!this.path_vertex_captured) return;
            if (this.captured_vertex == null) return;
            if (this.captured_index < 0) return;

            DraggableGeometryWoSnapModel3D vertex = sender as DraggableGeometryWoSnapModel3D;
            if (vertex == null || vertex.Tag != this.captured_vertex.Tag) return;

            // mode-specific processing
            if (this.PathEditMode == PathEditingMode.DELETE_POINT)
            {
                // delete path point according to the index saved in the tag
                this.pathInEditMode.RemoveAt(this.captured_index);
                // display change
                this.UpdateSelectedPathDisplayDuringEdit(-1, null);
                // write change to history
                this.undo_pathInEditMode.Push(new List<Point3D>(this.pathInEditMode));
                this.redo_pathInEditMode.Clear();          
            }
            else if (this.PathEditMode == PathEditingMode.MOVE_POINT_SINGLE ||
                     this.PathEditMode == PathEditingMode.MOVE_POINT_MULTIPLE)
            {
                // done (position transfer in the Mouse3DMove handler)
                Application.Current.MainWindow.Cursor = Cursors.Arrow;
                this.modifying_lines.Visibility = Visibility.Hidden;
                // if there is a change, write to history
                Vector3D moveV = this.path_vertex_pos_old - this.pathInEditMode[this.captured_index];
                if (moveV.LengthSquared > Utils.CommonExtensions.LINEDISTCALC_TOLERANCE * TOLERANCE_FACTOR)
                {
                    this.undo_pathInEditMode.Push(new List<Point3D>(this.pathInEditMode));
                    this.redo_pathInEditMode.Clear();
                }                    
            }

            // release vertex
            this.path_vertex_captured = false;
            this.captured_vertex.DragX = false;
            this.captured_vertex.DragY = false;
            this.captured_vertex.DragZ = false;
            this.captured_vertex = null;
            this.captured_index = -1;
        }

        private void OnVertexMouse3DMove(object sender, RoutedEventArgs e)
        {
            // check if the sender is the captured vertex
            if (!this.path_vertex_captured) return;
            if (this.captured_vertex == null) return;
            if (this.captured_index < 0) return;

            DraggableGeometryWoSnapModel3D vertex = sender as DraggableGeometryWoSnapModel3D;
            if (vertex == null || vertex.Tag != this.captured_vertex.Tag) return;

            // mode-specific processing
            if (this.PathEditMode == PathEditingMode.MOVE_POINT_SINGLE)
            {
                // transfer moving constraints
                this.captured_vertex.DragX = this.DragX;
                this.captured_vertex.DragY = this.DragY;
                this.captured_vertex.DragZ = this.DragZ;
                // transfer vertex position to the path
                MatrixTransform3D transf = this.captured_vertex.Transform as MatrixTransform3D;
                if (transf != null)
                {
                    Matrix3D matrix = transf.Value;
                    Vector3 pos_new = matrix.ToMatrix().TranslationVector;
                    this.pathInEditMode[this.captured_index] = new Point3D(pos_new.X, pos_new.Y, pos_new.Z);
                    // display change
                    this.UpdateSelectedPathDisplayDuringEdit(this.captured_index, null);
                    // transform the modifying lines
                    this.TransformModifyingLines(pos_new);
                }                   
            }
            else if (this.PathEditMode == PathEditingMode.MOVE_POINT_MULTIPLE)
            {
                int nrCaptured = this.captured_vertices_for_multi_move.Count;
                Vector3 pos_new_for_ML = Vector3.Zero;
                Vector3 increment = Vector3.Zero;
                List<int> aligned_indices = new List<int>();

                for (int i = 0; i < nrCaptured; i++ )
                {
                    DraggableGeometryWoSnapModel3D cv = this.captured_vertices_for_multi_move.ElementAt(i).Value;
                    int cv_ind = this.captured_vertices_for_multi_move.ElementAt(i).Key;
                    if (i > 0)
                        aligned_indices.Add(cv_ind);

                    // transfer moving constraints
                    cv.DragX = this.DragX;
                    cv.DragY = this.DragY;
                    cv.DragZ = this.DragZ;

                    
                    if (i == 0)
                    {
                        // transfer vertex position to the path
                        MatrixTransform3D transf = cv.Transform as MatrixTransform3D;
                        if (transf != null)
                        {
                            Matrix3D matrix = transf.Value;
                            Vector3 pos_new = matrix.ToMatrix().TranslationVector;

                            pos_new_for_ML = pos_new;
                            increment = pos_new - this.pathInEditMode[cv_ind].ToVector3();

                            this.pathInEditMode[cv_ind] = new Point3D(pos_new.X, pos_new.Y, pos_new.Z);  
                        }
                    }
                    else
                    {
                        // calculate path position and transfer to the vertex
                        this.pathInEditMode[cv_ind] += increment.ToVector3D();

                        MatrixTransform3D transf = cv.Transform as MatrixTransform3D;
                        if (transf != null)
                        {
                            Matrix3D M = Matrix3D.Identity;
                            M.Translate(new Vector3D(this.pathInEditMode[cv_ind].X, this.pathInEditMode[cv_ind].Y, this.pathInEditMode[cv_ind].Z));
                            cv.Transform = new MatrixTransform3D(M);                            
                        }
                    }
                }

                // display change
                this.UpdateSelectedPathDisplayDuringEdit(this.captured_index, aligned_indices);
                // transform the modifying lines
                this.TransformModifyingLines(pos_new_for_ML);
            }
        }

        #endregion

        #region UTILITIES

        /// <summary>
        /// <para>Avoids System.IndexOutOfRangeException in SharpDX.dll.</para>
        /// </summary>
        /// <returns></returns>
        private static bool LineGeometry3DIsDisplayable(LineGeometry3D _lines)
        {
            if (_lines == null) return true;

            List<HelixToolkit.SharpDX.Wpf.Geometry3D.Line> lines = _lines.Lines.ToList();
            if (lines.Count == 0)
            {
                return false;
            }
            else if (lines.Count == 1)
            {
                if (lines[0].P0.X == 0f && lines[0].P0.Y == 0f && lines[0].P0.Z == 0f &&
                    lines[0].P1.X == 0f && lines[0].P1.Y == 0f && lines[0].P1.Z == 0f)
                {
                    return false;
                }
            }

            return true;
        }

        private void TransformModifyingLines(Vector3 _pos_new)
        {
            Vector3 rv0 = new Vector3(1f, 1f, 1f);
            Vector3 rv1 = (_pos_new - this.path_vertex_pos_old.ToVector3());
            Vector3 scale = new Vector3(rv1.X / rv0.X, rv1.Y / rv0.Y, rv1.Z / rv0.Z);

            Matrix ML_t = Matrix.Translation(this.path_vertex_pos_old.ToVector3() + 0.5f * scale);
            Matrix ML_s = Matrix.Scaling(scale);
            Matrix ML_st = Matrix.Translation(0.5f * scale);
            Matrix ML_new = ML_s * ML_t;
            
            this.modifying_lines.Transform = new MatrixTransform3D(ML_new.ToMatrix3D());
        }


        // assumes that a vertex has been captured -> no NULL checks
        private Dictionary<int, DraggableGeometryWoSnapModel3D> CaptureAlignedVertices(bool _alongX, bool _alongY, bool _alongZ)
        {
            Dictionary<int, DraggableGeometryWoSnapModel3D> found = new Dictionary<int, DraggableGeometryWoSnapModel3D>();
            this.captured_vertex.OnMouseDownOverride(this.pathInEditMode[this.captured_index]);
            found.Add(this.captured_index, this.captured_vertex);

            int nrP = this.pathInEditMode.Count;
            for(int i = 2; i < nrP - 1; i++)
            {
                if (i == this.captured_index)
                    continue;

                double diffX = Math.Abs(this.pathInEditMode[i].X - this.path_vertex_pos_old.X);
                double diffY = Math.Abs(this.pathInEditMode[i].Y - this.path_vertex_pos_old.Y);
                double diffZ = Math.Abs(this.pathInEditMode[i].Z - this.path_vertex_pos_old.Z);

                bool capture = false;
                if (_alongX)
                    capture |= (diffY < Utils.CommonExtensions.GENERAL_CALC_TOLERANCE * TOLERANCE_FACTOR &&
                                diffZ < Utils.CommonExtensions.GENERAL_CALC_TOLERANCE * TOLERANCE_FACTOR);
                if (_alongY)
                    capture |= (diffX < Utils.CommonExtensions.GENERAL_CALC_TOLERANCE * TOLERANCE_FACTOR &&
                                diffZ < Utils.CommonExtensions.GENERAL_CALC_TOLERANCE * TOLERANCE_FACTOR);
                if(_alongZ)
                    capture |= (diffX < Utils.CommonExtensions.GENERAL_CALC_TOLERANCE * TOLERANCE_FACTOR &&
                                diffY < Utils.CommonExtensions.GENERAL_CALC_TOLERANCE * TOLERANCE_FACTOR);

                if(capture)
                {
                    // search for the vertex
                    foreach (DraggableGeometryWoSnapModel3D dgm in this.editablePathVerticesDisplayMesh)
                    {
                        if (dgm.Tag == null) continue;

                        int index = -1;
                        bool parsed = int.TryParse(dgm.Tag.ToString(), out index);
                        if (parsed && index == i)
                        {
                            dgm.OnMouseDownOverride(this.pathInEditMode[i]);
                            found.Add(i, dgm);
                            break;
                        }                           
                    }
                }
            }

            return found;
        }

        #endregion
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // ==================================== VALUE CONVERTER FOR PATH EDIT MODE ================================ //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region VALUE CONVERTERS

    [ValueConversion(typeof(PathEditingMode), typeof(Boolean))]
    public class PathEditingModeToBooleanConverter : IValueConverter
    {
        // in order to react to more than one action mode at once
        // use + as OR; * as AND
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            PathEditingMode mode = PathEditingMode.NEUTRAL;
            if (value is PathEditingMode)
                mode = (PathEditingMode)value;

            string str_param = parameter.ToString();

            string[] str_params_OR = str_param.Split(new char[] { '+' });

            if (str_params_OR.Count() < 2)
                return (mode == ComponentDisplay.StringToPathEditingMode(str_param));
            else
            {
                foreach (string p in str_params_OR)
                {
                    if (mode == ComponentDisplay.StringToPathEditingMode(p))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    #endregion

}
