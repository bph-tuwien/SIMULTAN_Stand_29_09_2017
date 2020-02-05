using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Resources;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.CompilerServices;

using GeometryViewer.Utils;
using GeometryViewer.Communication;
using GeometryViewer.EntityGeometry;
using GeometryViewer.ComponentReps;
using GeometryViewer.HelixToolkitCustomization;

using InterProcCommunication;

namespace GeometryViewer
{
    public class Viewport3DXext : Viewport3DX
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ========================================== DEPENDENCY PROPERTIES ======================================= //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Action Mode
        public Communication.ActionType ActionMode
        {
            get { return (Communication.ActionType)GetValue(ActionModeProperty); }
            set { SetValue(ActionModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ActionMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ActionModeProperty =
            DependencyProperty.Register("ActionMode", typeof(Communication.ActionType), typeof(Viewport3DXext),
            new UIPropertyMetadata(Communication.ActionType.NO_ACTION,
                                    new PropertyChangedCallback(MyActionModePropertyChangedCallback),
                                    new CoerceValueCallback(MyActionModeCoerceValueCallback)));

        private static void MyActionModePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Viewport3DXext vp = d as Viewport3DXext;
            Communication.ActionType at = Communication.ActionType.NO_ACTION;
            if (e.NewValue is Communication.ActionType)
                at = (Communication.ActionType)e.NewValue;
            if (vp != null)
            {
                // change cursor
                SetCursorWhen(vp, at);                
            }
        }

        private static object MyActionModeCoerceValueCallback(DependencyObject d, object value)
        {
            //var test1 = d;
            //var test2 = value;
            return value;
        }
        #endregion

        #region Object Snap

        public bool SnapToEndPointsOn
        {
            get { return (bool)GetValue(SnapToEndPointsOnProperty); }
            set { SetValue(SnapToEndPointsOnProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SnapToEndPointsOn.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SnapToEndPointsOnProperty =
            DependencyProperty.Register("SnapToEndPointsOn", typeof(bool), typeof(Viewport3DXext), 
            new UIPropertyMetadata(false, new PropertyChangedCallback(SnapAllPropertiesChangedCallback)));

        public bool SnapToIntesectionPointsOn
        {
            get { return (bool)GetValue(SnapToIntesectionPointsOnProperty); }
            set { SetValue(SnapToIntesectionPointsOnProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SnapToIntesectionPointsOn.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SnapToIntesectionPointsOnProperty =
            DependencyProperty.Register("SnapToIntesectionPointsOn", typeof(bool), typeof(Viewport3DXext),
            new UIPropertyMetadata(false, new PropertyChangedCallback(SnapAllPropertiesChangedCallback)));

        public bool SnapToMidPointsOn
        {
            get { return (bool)GetValue(SnapToMidPointsOnProperty); }
            set { SetValue(SnapToMidPointsOnProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SnapToMidPointsOn.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SnapToMidPointsOnProperty =
            DependencyProperty.Register("SnapToMidPointsOn", typeof(bool), typeof(Viewport3DXext),
            new UIPropertyMetadata(false, new PropertyChangedCallback(SnapAllPropertiesChangedCallback)));

        private static void SnapAllPropertiesChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Viewport3DXext vp = d as Viewport3DXext;
            if (vp != null)
            {
                bool calc_oSnap = vp.SnapToEndPointsOn || vp.SnapToMidPointsOn || vp.SnapToIntesectionPointsOn;
                if (calc_oSnap && (vp.ActionMode == ActionType.LINE_DRAW || vp.ActionMode == ActionType.LINE_EDIT))
                {
                    // vp.UpdateOcTree();
                    vp.SynchronizeViewFrustum(true);
                }
                else
                {
                    vp.SynchronizeViewFrustum(false);
                }
            }
        }

        #endregion

        #region Console

        public bool ConsoleVisible
        {
            get { return (bool)GetValue(ConsoleVisibleProperty); }
            set { SetValue(ConsoleVisibleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ConsoleVisible.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ConsoleVisibleProperty =
            DependencyProperty.Register("ConsoleVisible", typeof(bool), typeof(Viewport3DXext), 
            new UIPropertyMetadata(true));



        public List<LoggerEntry> ExistingLoggerEntries
        {
            get { return (List<LoggerEntry>)GetValue(ExistingLoggerEntriesProperty); }
            set { SetValue(ExistingLoggerEntriesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ExistingLoggerEntries.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ExistingLoggerEntriesProperty =
            DependencyProperty.Register("ExistingLoggerEntries", typeof(List<LoggerEntry>), typeof(Viewport3DXext), 
            new UIPropertyMetadata(new List<LoggerEntry>()));


        #endregion

        #region Logged User

        public UserRole LoggedUser
        {
            get { return (UserRole)GetValue(LoggedUserProperty); }
            set { SetValue(LoggedUserProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LoggedUser.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LoggedUserProperty =
            DependencyProperty.Register("LoggedUser", typeof(UserRole), typeof(Viewport3DXext), new UIPropertyMetadata(UserRole.ALL_RIGHTS));

        #endregion

        #region COMM


        public bool SendNOUpdatesOverComm
        {
            get { return (bool)GetValue(SendNOUpdatesOverCommProperty); }
            set { SetValue(SendNOUpdatesOverCommProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SendNOUpdatesOverComm.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SendNOUpdatesOverCommProperty =
            DependencyProperty.Register("SendNOUpdatesOverComm", typeof(bool), typeof(Viewport3DXext), new UIPropertyMetadata(false));


        public bool FinishedSendingMessages
        {
            get { return (bool)GetValue(FinishedSendingMessagesProperty); }
            set { SetValue(FinishedSendingMessagesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FinishedSendingMessages.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FinishedSendingMessagesProperty =
            DependencyProperty.Register("FinishedSendingMessages", typeof(bool), typeof(Viewport3DXext), 
            new UIPropertyMetadata(true));

        public int NrReferencesToSendToCB
        {
            get { return (int)GetValue(NrReferencesToSendToCBProperty); }
            set { SetValue(NrReferencesToSendToCBProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NrReferencesToSendToCB.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NrReferencesToSendToCBProperty =
            DependencyProperty.Register("NrReferencesToSendToCB", typeof(int), typeof(Viewport3DXext),
            new UIPropertyMetadata(0, new PropertyChangedCallback(NrReferencesToSendToCBPropertyChangedCallback)));

        private static void NrReferencesToSendToCBPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Viewport3DXext instance = d as Viewport3DXext;
            if (instance == null) return;

            // workaround: the tooltip does not accept a StringFormat in .NET 4.0
            instance.NrReferencesToSendToCBString = "Komponenten zu übertragen: " + instance.NrReferencesToSendToCB;
        }



        public string NrReferencesToSendToCBString
        {
            get { return (string)GetValue(NrReferencesToSendToCBStringProperty); }
            set { SetValue(NrReferencesToSendToCBStringProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NrReferencesToSendToCBString.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NrReferencesToSendToCBStringProperty =
            DependencyProperty.Register("NrReferencesToSendToCBString", typeof(string), typeof(Viewport3DXext), 
            new UIPropertyMetadata(string.Empty));

        

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ============================================= STATIC METHODS =========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly double ZOOM_CORR = 0.5;
        private static readonly double CALC_TOLERANCE = 0.0001;
        private static readonly int SNAP_MAGNET = 20;
        private static Dictionary<Communication.ActionType, Cursor> CURSORS;

        #region Cursor Management

        private static void PopulateCursorList()
        {
            CURSORS = new Dictionary<Communication.ActionType, Cursor>();
            System.Windows.Resources.StreamResourceInfo sriCursor;

            sriCursor = Application.GetResourceStream(new Uri("/Data/Icons/cursor_1_look_at.cur", UriKind.Relative));
            if (sriCursor != null)
                CURSORS.Add(Communication.ActionType.NAVI_LOOK_AT, new Cursor(sriCursor.Stream));

            sriCursor = Application.GetResourceStream(new Uri("/Data/Icons/cursor_arc.cur", UriKind.Relative));
            if (sriCursor != null)
                CURSORS.Add(Communication.ActionType.ARCHITECTURE, new Cursor(sriCursor.Stream));

            sriCursor = Application.GetResourceStream(new Uri("/Data/Icons/cursor_arc_edit.cur", UriKind.Relative));
            if (sriCursor != null)
                CURSORS.Add(Communication.ActionType.ARCHITECTURE_SELECT, new Cursor(sriCursor.Stream));

            sriCursor = Application.GetResourceStream(new Uri("/Data/Icons/cursor_polygon_edit.cur", UriKind.Relative));
            if (sriCursor != null)
                CURSORS.Add(Communication.ActionType.BUILDING_PHYSICS, new Cursor(sriCursor.Stream));

            sriCursor = Application.GetResourceStream(new Uri("/Data/Icons/cursor_polygon_editHL.cur", UriKind.Relative));
            if (sriCursor != null)
                CURSORS.Add(Communication.ActionType.BUILDING_PHYSICS_SELECT, new Cursor(sriCursor.Stream));
        }

        private static void SetCursorWhen(Viewport3DXext _vp, ActionType _at)
        {
            if (_vp == null)
                return;

            if (Viewport3DXext.CURSORS == null)
                Viewport3DXext.PopulateCursorList();

            switch (_at)
            {
                case Communication.ActionType.NAVI_LOOK_AT:
                    _vp.Cursor = CURSORS[_at];
                    break;
                case ActionType.ARCHITECTURE:
                    _vp.Cursor = CURSORS[_at];
                    break;
                case ActionType.BUILDING_PHYSICS:
                    _vp.Cursor = CURSORS[_at];
                    break;
                case ActionType.OPTIONS:
                case ActionType.LINE_DRAW:
                case ActionType.LINE_EDIT:   
                case ActionType.MODERATOR:
                case ActionType.ENERGY_NETWORK_OPERATOR:
                case ActionType.EENERGY_SUPPLIER:
                case ActionType.BUILDING_DEVELOPER:
                case ActionType.BUILDING_OPERATOR:
                case ActionType.FIRE_SAFETY:
                case ActionType.MEP_HVAC:
                case ActionType.PROCESS_MEASURING_CONTROL:
                case ActionType.SPACES_OPENINGS:
                    // leave it to the class performing the actions
                    _vp.Cursor = null;
                    break;
                default:
                    _vp.Cursor = Cursors.Arrow;
                    break;
            }
        }
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ============================================== CLASS MEMBERS =========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region INIT

        // internal logging
        public LocalLogger LoggingService { get; protected set; }
        public ICommand SaveLogEntriesCmd { get; private set; }

        // inter-process communication
        private CommDuplex CommUnit;
        private LocalLogger CU_Logger;
        private bool CU_running;
        // - for incoming
        private InterProcCommunication.CommMessageType CU_current_action_to_take;
        private InterProcCommunication.Specific.ComponentMessage CU_current_message;
        protected List<InterProcCommunication.Specific.ComponentMessage> unprocessed_message_queue;
        // - for outgoing
        private NotifyingBool comm_can_send_next_msg;
        private Queue<string> messages_to_send;
        private string message_last_sent;
        // - for deferred outgoing
        private Dictionary<long, List<CompRep>> comps_to_send_deferred;
        public ICommand TransferDeferredCompRepsCmd { get; private set; }

        // other
        public ICommand SwitchActionModeCmd { get; private set; }
        public ICommand PerformZoomExtentsCmd { get; private set; }
        public ICommand PerformZoomSelectedCmd { get; private set; }

        public ICommand SetBackgroundColorCmd { get; private set; }

        private ActionModeManager amManager;
        private Point3D camTarget;

        private LineGenerator3D lg = null;
        private Boolean lg_in_drawMode = false;
        private LineManipulator3DGraphic lmGr = null;
        private ArchitectureDisplay arc_display = null;
        private ZoneGroupDisplay zp_display = null;
        private ComponentDisplay cp_display = null;
        private SpatialOrganization.OcTreeManager otm = null;
        private SpatialOrganization.ViewFrustumFunctions vff = null;
        private SpatialOrganization.NeighborhoodGraph neighb_graph = null;

        public Viewport3DXext()
        {
            this.Loaded += Viewport3DXext_Loaded;

            this.SaveLogEntriesCmd = new RelayCommand((x) => this.LoggingService.SaveLogToFile("Geometrische Ansicht"));
            this.SwitchActionModeCmd = new RelayCommand((x) => OnSwitchActionMode(x));
            this.PerformZoomExtentsCmd = new RelayCommand((x) => ZoomGeometryExtents());
            this.PerformZoomSelectedCmd = new RelayCommand((x) => ZoomSelected());

            this.SetBackgroundColorCmd = new RelayCommand((x) => OnSetBackgroundColorCommand());

            this.amManager = new ActionModeManager();
            this.camTarget = new Point3D(0, 0, 0);

            this.LoggedUser = UserRole.ALL_RIGHTS;
            this.LoggingService = new LocalLogger(UserRoleUtils.UserRoleToString(this.LoggedUser));
            this.LoggingService.PropertyChanged += ls_PropertyChanged;
            this.LoggingService.LogInfo("Viewport initialized.");

            this.CU_running = false;
            
            this.comm_can_send_next_msg = new NotifyingBool(false);
            this.comm_can_send_next_msg.ReturnToTrueAfterTimeOut = true;
            this.comm_can_send_next_msg.PropertyChanged += comm_can_send_next_msg_PropertyChanged;
            this.messages_to_send = new Queue<string>();

            this.TransferDeferredCompRepsCmd = new RelayCommand((x) => OnTransferDeferredCompReps(),
                                                     (x) => CanExecute_OnTransferDeferredCompReps());
        }

        #endregion

        #region ActionMode
        private void OnSwitchActionMode(object _mode)
        {
            if (_mode == null)
                return;

            this.ActionMode = this.amManager.SetAction(_mode.ToString());
            bool calc_oSnap = this.SnapToEndPointsOn || this.SnapToMidPointsOn || this.SnapToIntesectionPointsOn;
            this.LoggingService.LogInfo("Switching to mode: " + this.ActionMode.ToString());

            switch(this.ActionMode)
            {
                case ActionType.LINE_DRAW:
                    // ResetARCDisplay();
                    ResetLineGen();
                    if (calc_oSnap)
                    {
                        UpdateOcTree();
                        SynchronizeViewFrustum(true);
                    }
                    break;
                case ActionType.LINE_EDIT:
                    if (calc_oSnap)
                    {
                        UpdateOcTree();
                        SynchronizeViewFrustum(true);
                    }
                    break;
                case ActionType.ARCHITECTURE:
                    ResetLineGen();
                    ResetZPDisplay();
                    SynchronizeViewFrustum(false);
                    break;
                default:
                    ResetCPDisplay();
                    ResetZPDisplay();
                    ResetARCDisplay();
                    ResetLineGen();
                    SynchronizeViewFrustum(false);
                    break;
            }
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ======================================== SPECIALIZED CLASS METHODS ===================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Navigation
        private void ChangeCamViewDir()
        {
            foreach (object item in this.Items)
            {
                UserMesh cp = item as UserMesh;
                if (cp != null)
                {
                    Point3D hit = cp.HitPoint;
                    if (hit.X != 0 || hit.Y != 0 || hit.Z != 0)
                    {
                        this.camTarget = hit;
                        this.Camera.LookDirection = cp.HitPoint - this.Camera.Position;
                        this.Camera.UpDirection = new Vector3D(0, 1, 0);
                        break;
                    }
                }
            }
        }

        private double MaxDistFromCamTarget()
        {
            double dist = 0.0;
            BoundingBox bb = new BoundingBox();
            foreach (object item in this.Items)
            {
                // skip elements
                var fe = item as FrameworkElement;
                if (fe != null && fe.Tag != null)
                {
                    if (fe.Tag.ToString() == "NON_ZOOMABLE")
                        continue;
                }

                var model = item as IBoundable;
                if (model != null)
                {
                    if (model.Visibility != Visibility.Collapsed)
                    {
                        bb = BoundingBox.Merge(bb, model.Bounds);
                    }
                }
            }
            dist = Math.Max(Vector3.Distance(this.camTarget.ToVector3(), bb.Minimum),
                            Vector3.Distance(this.camTarget.ToVector3(), bb.Maximum));
            return dist;
        }

        private void ZoomSelected()
        {
            // find the bound of the selected object
            BoundingBox bb = new BoundingBox();
            foreach (object item in this.Items)
            {
                if (this.arc_display != null && this.arc_display.SelectedEntity != null)
                {
                    BoundingBox arc_bb = this.arc_display.SelectedGeometryBounds;
                    if (Vector3.Distance(arc_bb.Minimum, arc_bb.Maximum) >= CALC_TOLERANCE)
                    {
                        bb = arc_bb;
                        break;
                    }
                }
                if (this.zp_display != null && this.zp_display.SelectedEntity != null)
                {
                    BoundingBox zp_bb = this.zp_display.SelectedGeometryBounds;
                    if (Vector3.Distance(zp_bb.Minimum, zp_bb.Maximum) >= CALC_TOLERANCE)
                    {
                        bb = zp_bb;
                        break;
                    }
                }
            }

            double dist = Vector3.Distance(bb.Minimum, bb.Maximum) * 0.5;
            if (dist < CALC_TOLERANCE)
                return;

            Point3D newCamTarget = new Point3D( bb.Minimum.X * 0.5 + bb.Maximum.X * 0.5,
                                                bb.Minimum.Y * 0.5 + bb.Maximum.Y * 0.5,
                                                bb.Minimum.Z * 0.5 + bb.Maximum.Z * 0.5);
            // adjust camera view diection
            this.camTarget = newCamTarget;
            this.Camera.LookDirection = newCamTarget - this.Camera.Position;
            this.Camera.UpDirection = new Vector3D(0, 1, 0);

            // zoom            
            this.ZoomExtents(this.camTarget, dist * ZOOM_CORR, 500);
        }


        private void ZoomGeometryExtents()
        {
            this.ZoomExtents(this.camTarget, MaxDistFromCamTarget() * ZOOM_CORR, 500);
        }

        #endregion

        #region Get and Manage various contained objects

        private void GetActors()
        {
            foreach (object item in this.Items)
            {
                if (this.lg == null)
                {
                    this.lg = item as LineGenerator3D;
                    if (this.lg != null)
                        this.lg.PropertyChanged += lg_PropertyChanged;
                }
                if (this.lmGr == null)
                {
                    this.lmGr = item as LineManipulator3DGraphic;
                    if (this.lmGr != null)
                        this.lmGr.PropertyChanged += lmGr_PropertyChanged;
                }
                if (this.arc_display == null)
                    this.arc_display = item as ArchitectureDisplay;
                if (this.zp_display == null)
                    this.zp_display = item as ZoneGroupDisplay;
                if (this.cp_display == null)
                {
                    this.cp_display = item as ComponentDisplay;
                    if (this.cp_display != null && this.cp_display.CompRepMANAGER != null)
                        this.cp_display.CompRepMANAGER.SetCommunicationManager(this);
                }
                if (this.otm == null)
                    this.otm = item as SpatialOrganization.OcTreeManager;
                if (this.vff == null)
                    this.vff = item as SpatialOrganization.ViewFrustumFunctions;
                if (this.neighb_graph == null)
                    this.neighb_graph = item as SpatialOrganization.NeighborhoodGraph;
            }
        }

        private void lg_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            LineGenerator3D lg3d = sender as LineGenerator3D;
            if (e == null || lg3d == null)
                return;

            if (e.PropertyName == "DrawModeCopy")
            {
                if (lg3d.DrawMode)
                    this.PrepareForObjectSnap();
            }
        }

        private void ResetLineGen()
        {
            if (this.lg != null)
            {
                // stop drawing
                if (this.lg.DrawMode)
                    this.lg.DrawMode = false;
                // deselect
                this.lg.DeselectCmd.Execute(null);
            }
        }

        void lmGr_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            LineManipulator3DGraphic lm3d = sender as LineManipulator3DGraphic;
            if (e == null || lm3d == null)
                return;

            if (e.PropertyName == "IsSelectedCopy")
            {
                if (lm3d.IsSelectedCopy)
                    this.PrepareForObjectSnap();
            }
        }

        private void ResetARCDisplay()
        {
            if (this.arc_display != null)
            {
                // deselect
                if (this.arc_display.SelectedEntity != null)
                    this.arc_display.SelectedEntity = null;
            }
        }


        private void ResetZPDisplay()
        {
            if (this.zp_display != null)
            {                
                // switch off edit modes
                if (this.zp_display.SwitchZoneEditModeCmd.CanExecute(ZoneEditType.NO_EDIT))
                    this.zp_display.SwitchZoneEditModeCmd.Execute(ZoneEditType.NO_EDIT);
                // deselect
                if (this.zp_display.SelectedEntity != null)
                    this.zp_display.SelectedEntity = null;
            }
        }

        private void ResetCPDisplay()
        {
            if (this.cp_display != null)
            {
                // switch off edit modes
                if (this.cp_display.PathEditingModeOn)
                {
                    this.cp_display.OnSwitchPathEditMode(PathEditingMode.NEUTRAL);
                    this.cp_display.PathEditingModeOn = false;
                }
                // deselect
                if (this.cp_display.SelectedCompRep != null)
                    this.cp_display.SelectNoneCmd.Execute(null);
            }
        }

        private void UpdateOcTree()
        {
            if (this.otm != null)
                this.otm.UpdateOcTreeCmd.Execute(null);
        }

        private void SynchronizeViewFrustum(bool _synchronize)
        {
            if (this.vff != null)
                this.vff.SynchrCam = _synchronize;
        }

        private void PrepareForObjectSnap()
        {
            if (this.otm != null && this.vff != null)
            {
                this.otm.CheckForCollisionsSimpleCmd.Execute(null);
                if (this.otm.CheckForVisibilitySimpleCmd.CanExecute(this.vff))
                    this.otm.CheckForVisibilitySimpleCmd.Execute(this.vff);
            }
        }
        
        #endregion

        #region Component Representations: Materials / Wall Construction

        public long GetCompReferenceFromMaterialID(long _material_id)
        {
            if (_material_id < 0) return -1L;
            if (this.cp_display == null) return -1L;
            if (this.cp_display.CompRepMANAGER == null) return -1L;

            return this.cp_display.CompRepMANAGER.ConvertMaterialID2CompReference(_material_id);
        }

        public long GetMissingCompReferenceFromMaterialID(long _material_id)
        {
            if (_material_id < 0) return -1L;
            if (this.zp_display == null) return -1L;
            if (this.zp_display.MLManager == null) return -1L;
            
            ComponentInteraction.Material mat = this.zp_display.MLManager.FindByID(_material_id);
            if (mat == null) return -1L;

            return mat.BoundCRID;
        }

        public void PropagateCompReferenceToAllAffectedSurfaceReps(CompRepAlignedWith _cra)
        {
            if (_cra == null) return;
            if (_cra.WallConstr == null) return;
            if (this.cp_display == null) return;
            if (this.cp_display.CompRepMANAGER == null) return;

            IList<CompRepDescribes2DorLess> affected = this.cp_display.CompRepMANAGER.AddMaterialReferenceToAffectedSurfaceReps(_cra.WallConstr.ID, _cra.Comp_ID);
            foreach(CompRepDescribes2DorLess c in affected)
            {
                this.AddForDeferredSendingToCompBuilder(_cra.CR_ID, c);
            }
        }

        public void PropagateMaterialOffsetChangeToVolumes(ComponentInteraction.Material _mat)
        {
            if (_mat == null) return;
            if (this.zp_display == null) return;
            if (this.zp_display.EManager == null) return;

            this.zp_display.EManager.UpdateVolumesAfterMaterialChange(_mat);
        }

        #endregion

        #region Component Representations: Placement

        public CompRepDescirbes GetPlacementContainer(long _vol_id)
        {
            if (this.zp_display == null) return null;
            if (this.zp_display.EManager == null) return null;

            ZonedVolume container = this.zp_display.EManager.GetVolumeByID(_vol_id);
            if (container == null) return null;

            CompRepInfo container_d = container.GetDescribingCompOrFirst();
            CompRepDescirbes container_descriptor = container_d as CompRepDescirbes;
            return container_descriptor;
        }

        public ZonedVolume GetPlacementVolume(long _vol_id)
        {
            if (this.zp_display == null) return null;
            if (this.zp_display.EManager == null) return null;

            return this.zp_display.EManager.GetVolumeByID(_vol_id);
        }

        #endregion

        #region Component Representations: Neighbors

        public List<long> GetNeighborsFor(CompRepDescirbes _main_descriptor)
        {
            List<long> ref_ids = new List<long>();

            if (this.neighb_graph == null) return ref_ids;
            if (_main_descriptor == null) return ref_ids;
            if (_main_descriptor.Geom_Zone == null) return ref_ids;

            // search for the volume's neighbors in the neighborhood graph
            List<ZonedVolume> neighbors = this.neighb_graph.GetNeighborsOf(_main_descriptor.Geom_Zone);
            if (neighbors.Count > 0)
            {
                foreach(ZonedVolume zvn in neighbors)
                {
                    CompRepInfo zvn_d = zvn.GetDescribingCompOrFirst();
                    CompRepDescirbes zvn_descriptor = zvn_d as CompRepDescirbes;
                    if (zvn_descriptor == null) continue;

                    ref_ids.Add(zvn_descriptor.Comp_ID);
                    bool added = zvn_descriptor.AddNeighborReferenceIfMissing(_main_descriptor);
                    if (added)
                        this.AddForDeferredSendingToCompBuilder(_main_descriptor.CR_ID, zvn_descriptor);
                }
            }
            return ref_ids;
        }

        public List<long> ReleaseNeighborsFrom(CompRepDescirbes _main_descriptor)
        {
            List<long> ref_ids = new List<long>();

            if (this.neighb_graph == null) return ref_ids;
            if (_main_descriptor == null) return ref_ids;
            if (_main_descriptor.Geom_Zone == null) return ref_ids;

            // search for the volume's neighbors in the neighborhood graph
            List<ZonedVolume> neighbors = this.neighb_graph.GetNeighborsOf(_main_descriptor.Geom_Zone);
            if (neighbors.Count > 0)
            {
                foreach (ZonedVolume zvn in neighbors)
                {
                    CompRepInfo zvn_d = zvn.GetDescribingCompOrFirst();
                    CompRepDescirbes zvn_descriptor = zvn_d as CompRepDescirbes;
                    if (zvn_descriptor == null) continue;

                    ref_ids.Add(zvn_descriptor.Comp_ID);
                    bool removed = zvn_descriptor.RemoveNeighborReferenceIfPresent(_main_descriptor);
                    if (removed)
                        this.AddForDeferredSendingToCompBuilder(_main_descriptor.CR_ID, zvn_descriptor);
                }
            }
            return ref_ids;
        }


        public long GetOtherSideOf(CompRepDescribes2DorLess _surface_or_opening)
        {
            if (this.neighb_graph == null) return -1L;
            if (_surface_or_opening == null) return -1L;
            if (_surface_or_opening.GR_Relationships == null || _surface_or_opening.GR_Relationships.Count < 1) return -1L;

            // search for the surface in the edges of the neighborhood graph
            Point4D key = _surface_or_opening.GR_Relationships[0].GrIds;
            ZonedVolume other_side = this.neighb_graph.GetOtherSideOf((long)key.X, _surface_or_opening.IsWall, (int)key.Y, (int)key.Z, (long)key.W);
            if (other_side == null) return -1L;

            CompRepInfo other_side_descriptor = other_side.GetDescribingCompOrFirst();
            if (other_side_descriptor == null || !(other_side_descriptor is CompRepDescirbes)) return -1L;

            return other_side_descriptor.Comp_ID;
        }

        #endregion

        #region Component Representations: Deferred sending

        public void UpdateRefsForSingleCompRep(CompRepInfo _cri)
        {
            this.AddForDeferredSendingToCompBuilder(_cri.CR_ID, _cri);
        }

        /// <summary>
        /// This method is to be used to update references ONLY.
        /// </summary>
        /// <param name="_key">the id of the component representation triggering the change</param>
        /// <param name="_to_add">the cmponent representation whose references need to be updated</param>
        private void AddForDeferredSendingToCompBuilder(long _key, CompRep _to_add)
        {
            if (this.comps_to_send_deferred == null)
                this.comps_to_send_deferred = new Dictionary<long, List<CompRep>>();

            if (_to_add == null) return;
            if (_key < 0) return;

            if (!(this.comps_to_send_deferred.ContainsKey(_key)))
                this.comps_to_send_deferred.Add(_key, new List<CompRep>());

            CompRep duplicate = this.comps_to_send_deferred[_key].FirstOrDefault(x => x.CR_ID == _to_add.CR_ID);
            if (duplicate != null) return;

            this.comps_to_send_deferred[_key].Add(_to_add);
            this.NrReferencesToSendToCB = this.comps_to_send_deferred.Select(x => x.Value.Count).Sum();
        }

        /// <summary>
        /// This method is to be used to automatically update references ONLY.
        /// </summary>
        /// <param name="_key"></param>
        private void SendDataToCompBuilderDeferred(long _key)
        {
            if (this.comps_to_send_deferred == null) return;
            if (this.comps_to_send_deferred.Count == 0) return;
            if (!(this.comps_to_send_deferred.ContainsKey(_key))) return;

            List<CompRep> to_send = this.comps_to_send_deferred[_key];
            foreach(CompRep cr in to_send)
            {
                this.Dispatcher.Invoke(
                    new Action(() =>
                    {
                        this.SendDataToCompBuilder(cr, CommMessageType.REF_UPDATE);
                    })
                );               
            }
            this.comps_to_send_deferred.Remove(_key);
            this.NrReferencesToSendToCB = this.comps_to_send_deferred.Select(x => x.Value.Count).Sum();
        }

        private void OnTransferDeferredCompReps()
        {
            foreach(var entry in this.comps_to_send_deferred)
            {
                List<CompRep> to_send = entry.Value;
                foreach(CompRep cr in to_send)
                {
                    this.SendDataToCompBuilder(cr, CommMessageType.REF_UPDATE);
                }
            }
            this.comps_to_send_deferred = new Dictionary<long, List<CompRep>>();
            this.NrReferencesToSendToCB = 0;
        }

        private bool CanExecute_OnTransferDeferredCompReps()
        {
            if (this.CommUnit == null) return false;
            if (this.CommUnit.IsStopped) return false;

            if (this.comps_to_send_deferred == null) return false;
            if (this.comps_to_send_deferred.Count == 0) return false;

            return true;
        }

        #endregion

        #region Viewport Visual Properties
        private void OnSetBackgroundColorCommand()
        {
            Window window = Window.GetWindow(this);
            if (window != null)
            {
                MainWindow mw = window as MainWindow;
                if (mw != null)
                {
                    System.Windows.Media.Color nc = mw.OpenColorPicker();
                    this.BackgroundColor = new SharpDX.Color(nc.R, nc.G, nc.B, nc.A);
                    this.LoggingService.LogInfo("Changed viewport background color.");
                }
            }
        }
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ============================================== EVENT HANDLERS ========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        #region SELF: Inter-Process Communication

        protected void Viewport3DXext_Loaded(object sender, RoutedEventArgs e)
        {
                
            GeometryViewer.App app = Application.Current as GeometryViewer.App;
            if (app != null && this.LoggingService != null)
            {
                // get user role        
                this.LoggedUser = UserRoleUtils.TranslateUserRole(app.CallingUser);
                this.LoggingService.Prefix = UserRoleUtils.UserRoleToString(this.LoggedUser);

                // get comm unit parameters
                if (!(string.IsNullOrEmpty(app.CU_server_name)) && 
                    !(string.IsNullOrEmpty(app.CU_client_name)) && 
                    !(string.IsNullOrEmpty(app.CU_authentication)) &&
                    !this.CU_running)
                {
                    try
                    {
                        // inter-process communications
                        this.CU_Logger = new LocalLogger(this.LoggingService.Prefix);
                        this.CU_Logger.PropertyChanged += CU_Logger_PropertyChanged;
                        this.CommUnit = new CommDuplex("GV", app.CU_server_name, app.CU_client_name, app.CU_authentication, this.CU_Logger);
                        this.CommUnit.AnswerRequestHandler = this.AnswerRequestFromCompBuilder;
                        // the CommUnit receives requests over the Property CurrentInpu 
                        this.SendNOUpdatesOverComm = !app.CU_send_updates_back;

                        // start communication
                        Task task_cu = new Task(() => this.CommUnit.StartDuplex());
                        task_cu.ContinueWith(this.Debug, CancellationToken.None);
                        task_cu.Start();
                        this.CU_running = true;
                    }
                    catch (Exception ex)
                    {
                        this.LoggingService.LogError("Geometry Viewer Communication Unit konnte nicht gestartet werden: " + ex.Message);
                    }
                }
            }
            
        }

        internal void ShutDownCommUnit(bool _closing_initiated_by_other)
        {
            // close down communication
            this.Dispatcher.Invoke(
                new Action(() =>
                {
                    this.CU_running = false;
                })
            );
            if (this.CommUnit == null) return;

            this.LoggingService.LogAsProcess("communit {0} closing ...", this.CommUnit.Name);
            this.CommUnit.StopDuplex(_closing_initiated_by_other);
            this.CommUnit.AnswerRequestHandler -= this.AnswerRequestFromCompBuilder;
            this.CommUnit = null;
        }

        protected string AnswerRequestFromCompBuilder(string _request)
        {
            System.Diagnostics.Debug.WriteLine("GV[{0}]: 'AnswerRequestFromCompBuilder' for " + _request.Substring(0, 2), Thread.CurrentThread.ManagedThreadId);
            // analyze message
            CommMessageType msg_type;
            string msg_body;
            CommMessageUtils.DecomposeMessage(_request, out msg_type, out msg_body);

            string return_msg = string.Empty;
            switch(msg_type)
            {
                case CommMessageType.SYNCH:
                case CommMessageType.UPDATE:
                case CommMessageType.EDIT:
                    this.CU_current_action_to_take = msg_type;
                    this.CU_current_message = InterProcCommunication.Specific.ComponentMessage.FromString(msg_body);
                    // process message
                    try
                    {
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            if (this.cp_display != null && this.cp_display.CompRepMANAGER != null)
                            {
                                this.CommUnit.CurrentUserInput = CommMessageUtils.CMT_OK;
                                this.IncomingComponentHandler(this.CU_current_message);
                            }
                        }));
                    }
                    catch
                    { }
                    return_msg = CommMessageUtils.CMT_OK;
                    break;
                case CommMessageType.OK:
                default:
                    this.CU_current_action_to_take = CommMessageType.UNKNOWN;
                    this.CU_current_message = null;
                    return_msg = CommMessageUtils.CMT_OK;
                    break;
            }

            // added 19.05.2017
            this.comm_can_send_next_msg.Value_NotifyOnTrue = true;
            return return_msg;
        }

        // NEW 19.05.2017
        /// <summary>
        /// <para>Calls the CompRepMANAGER.ExtractMessagesFrom method, which in turn calls</para>
        /// <para>the ExtractMessage method for _cr and all its sub-comp reps.</para>
        /// <para>The ExtractMessage method should call a synchronization method before generating the message.</para>
        /// </summary>
        /// <param name="_cr"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SendDataToCompBuilder(CompRep _cr, CommMessageType _comm_type = CommMessageType.UPDATE)
        {
            if (this.CanNOTSendUpdatesOverComm()) return; // added 29.09.2017
            if (_cr == null) return;
            if (this.CommUnit == null) return;
            if (this.CommUnit.IsStopped) return;
            if (this.cp_display == null) return;
            if (this.cp_display.CompRepMANAGER == null) return;

            // 1. relegate the message extraction to the CompRepManager...
            // this.messages_to_send = new Queue<string>();
            List<InterProcCommunication.Specific.ComponentMessage> message_bodies = this.cp_display.CompRepMANAGER.ExtractMessagesFrom(_cr);
            foreach (InterProcCommunication.Specific.ComponentMessage body in message_bodies)
            {
                System.Diagnostics.Debug.WriteLine("GV[{2}]: Packing message at {0} for comp {1}", body.MsgPos, body.CompDescr, Thread.CurrentThread.ManagedThreadId);
                string msg = InterProcCommunication.CommMessageUtils.ComposeMessage(_comm_type, body.ToString());
                this.messages_to_send.Enqueue(msg);
            }

            // 2. send the first string
            // the rest will be sent in the event handler of 'comm_can_send_next_msg'
            if (this.messages_to_send.Count > 0 && this.comm_can_send_next_msg.Value) // second condition added 22.08.2017
            {
                this.FinishedSendingMessages = false;
                this.comm_can_send_next_msg.Value_NotifyOnTrue = false;
                System.Diagnostics.Debug.WriteLine("GV[{0}]>> sending 1st message...", Thread.CurrentThread.ManagedThreadId);
                // this.message_last_sent = this.messages_to_send.Peek(); // HACK: sending the first message twice, because the first time the CommUnit does not react...
                this.message_last_sent = this.messages_to_send.Dequeue();
                this.CommUnit.CurrentUserInput = this.message_last_sent;
                this.FinishedSendingMessages = (this.messages_to_send.Count == 0);
            }
        }

        private void Debug(Task _t)
        {

        }

        // NEW ... 04.05.2017
        private void IncomingComponentHandler(InterProcCommunication.Specific.ComponentMessage _msg)
        {
            System.Diagnostics.Debug.WriteLine("GV[{2}]: 'Incoming Message Handler' at {0} for comp {1}", _msg.MsgPos, _msg.CompDescr, Thread.CurrentThread.ManagedThreadId);

            // handle selection synchronization - added 20.05.2017
            if (this.CU_current_action_to_take == CommMessageType.SYNCH)
            {
                if (this.cp_display != null && this.cp_display.CompRepMANAGER != null)
                {
                    long id_to_select = _msg.CompID;
                    if (id_to_select > -1)
                    {
                        CompRepInfo comp_to_select = this.cp_display.CompRepMANAGER.FindByCompId(id_to_select);
                        if (comp_to_select != null)
                        {
                            this.cp_display.SelectedCompRep = comp_to_select;
                            this.cp_display.CompRepMANAGER.Select(comp_to_select);
                        }
                    }
                }
                return;
            }

            // look at the position of the message in a sequence
            switch(_msg.MsgPos)
            {
                case InterProcCommunication.Specific.MessagePositionInSeq.SEQUENCE_START_MESSAGE:
                    this.unprocessed_message_queue = new List<InterProcCommunication.Specific.ComponentMessage>();
                    this.unprocessed_message_queue.Add(_msg);
                    break;
                case InterProcCommunication.Specific.MessagePositionInSeq.MESSAGE_INSIDE_SEQUENCE:
                    if (this.unprocessed_message_queue != null)
                        this.unprocessed_message_queue.Add(_msg);
                    break;
                case InterProcCommunication.Specific.MessagePositionInSeq.SEQUENCE_END_MESSAGE:
                    this.comm_can_send_next_msg.Value_NotifyOnTrue = false; // added 04.09.2017
                    if (this.unprocessed_message_queue != null)
                        this.unprocessed_message_queue.Add(_msg);
                    // send messages for processing by the CompRepManager
                    if (this.cp_display != null && this.cp_display.CompRepMANAGER != null)
                        this.cp_display.CompRepMANAGER.AddCompReps(this.unprocessed_message_queue);
                    this.unprocessed_message_queue = new List<InterProcCommunication.Specific.ComponentMessage>();
                    break;
                case InterProcCommunication.Specific.MessagePositionInSeq.SINGLE_MESSAGE:
                    this.comm_can_send_next_msg.Value_NotifyOnTrue = false; // added 04.09.2017
                    this.unprocessed_message_queue = new List<InterProcCommunication.Specific.ComponentMessage>();
                    // send message directly to CompRepManager for processing
                    if (this.cp_display != null && this.cp_display.CompRepMANAGER != null)
                        this.cp_display.CompRepMANAGER.AddCompReps(new List<InterProcCommunication.Specific.ComponentMessage> { _msg }); // changed from AddCompRep to AddCompReps: 04.09.2017
                    break;
                default:
                    break;
            }
        }

        internal void ConnectCompRepToZonedVolume(CompRepDescirbes _crd)
        {
            if (this.zp_display != null && this.zp_display.EManager != null)
            {
                this.zp_display.EManager.SelectGeometry((long)_crd.GR_Relationships[0].GrIds.X);
                this.zp_display.EManager.ReAssociateZonedVolumeWComp(_crd);
            }
        }

        internal void ConnectCompRepToMaterial(CompRepAlignedWith _cra)
        {
            if (this.zp_display != null && this.zp_display.MLManager != null)
            {
                this.zp_display.MLManager.ReAssociate(_cra);
                this.zp_display.UpdateMaterials();
            }
        }

        internal void UpdateMaterials()
        {
            if (this.zp_display != null && this.zp_display.MLManager != null)
            {
                this.zp_display.UpdateMaterials();
            }
        }

        private bool CanNOTSendUpdatesOverComm()
        {
            bool cannot_send = false;
            this.Dispatcher.Invoke(
                new Action(() =>
                {
                    cannot_send = this.SendNOUpdatesOverComm;
                })
            );
            return cannot_send;
        }

        #endregion

        #region EVENT HANDLERS: intercomm

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void comm_can_send_next_msg_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this.comm_can_send_next_msg == null || e == null) return;

            if (e.PropertyName == "Value_NotifyOnTrue")
            {
                // can send the next message, if there is one
                if (this.messages_to_send.Count > 0)
                {
                    this.comm_can_send_next_msg.Value_NotifyOnTrue = false;
                    System.Diagnostics.Debug.WriteLine("GV[{0}]>> sending next message...", Thread.CurrentThread.ManagedThreadId);
                    this.message_last_sent = this.messages_to_send.Dequeue();
                    this.CommUnit.CurrentUserInput = this.message_last_sent;
                    
                    this.Dispatcher.Invoke(
                        new Action(() =>
                        {
                            this.FinishedSendingMessages = (this.messages_to_send.Count == 0);
                        })
                    );
                }
                else
                {
                    // this causes cross-messaging -> turned into an explicit command
                    // this.SendDataToCompBuilderDeferred(this.comm_current_sender_cr_id);
                }

            }
            else if(e.PropertyName == "TrueAfterTimeout")
            {
                if (this.messages_to_send.Count > 0)
                {
                    // message got lost on the way -> resend
                    if (!(string.IsNullOrEmpty(this.message_last_sent)))
                    {
                        this.comm_can_send_next_msg.Value_NotifyOnTrue = false;
                        System.Diagnostics.Debug.WriteLine("GV[{0}]>> sending message AGAIN after loss ...", Thread.CurrentThread.ManagedThreadId);
                        this.CommUnit.CurrentUserInput = this.message_last_sent;
                        this.message_last_sent = string.Empty;

                        this.Dispatcher.Invoke(
                            new Action(() =>
                            {
                                this.FinishedSendingMessages = (this.messages_to_send.Count == 0);
                            })
                        );
                    }
                }
            }
        }

        #endregion

        #region LOGGERS

        protected void ls_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e != null)
            {
                if (e.PropertyName == "NewEntryToDisplay")
                {
                    this.Dispatcher.Invoke(
                        new Action(() =>
                        {
                            try
                            {
                                this.ExistingLoggerEntries = new List<LoggerEntry>(this.LoggingService.Entries);
                            }
                            catch { }
                        })
                    );
                }
            }
        }

        protected void CU_Logger_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e != null)
            {
                if (e.PropertyName == "NewEntryToDisplay")
                    this.LoggingService.MergeLast(this.CU_Logger);
            }
        }

        #endregion

        #region Mouse
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            switch(this.ActionMode)
            {
                case Communication.ActionType.NAVI_LOOK_AT:
                    // support for navigation
                    ChangeCamViewDir();
                    this.ActionMode = this.amManager.SetAction(ActionType.NO_ACTION);
                    break;
                default:
                    break;
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            switch(this.ActionMode)
            {
                case Communication.ActionType.ARCHITECTURE:
                    // deselect, if click not on a line in zp_display
                    if (this.arc_display != null && e.ChangedButton == MouseButton.Left)
                    {
                        if (this.arc_display.MouseSelectedNew)
                        {
                            this.arc_display.MouseSelectedNew = false;
                            this.Cursor = CURSORS[ActionType.ARCHITECTURE_SELECT];
                        }
                        else
                        {
                            this.arc_display.SelectNoneCmd.Execute(null);
                            this.Cursor = CURSORS[ActionType.ARCHITECTURE];
                        }
                    }
                    break;
                case Communication.ActionType.BUILDING_PHYSICS:
                    // deselect, if click not on a line in zp_display
                    if (this.zp_display != null && e.ChangedButton == MouseButton.Left)
                    {
                        if (this.zp_display.MouseSelectedNew)
                        {
                            this.zp_display.MouseSelectedNew = false;
                            this.Cursor = CURSORS[ActionType.BUILDING_PHYSICS_SELECT];
                        }
                        else
                        {
                            this.zp_display.SelectNoneCmd.Execute(null);
                            this.Cursor = CURSORS[ActionType.BUILDING_PHYSICS];
                        }
                    }
                    break;
                case Communication.ActionType.SPACES_OPENINGS: // corresponds to the component display
                    //// deselect
                    //if (this.cp_display != null && e.ChangedButton == MouseButton.Left &&
                    //    !this.cp_display.PathEditingModeOn)
                    //{
                    //    this.cp_display.SelectNoneCmd.Execute(null);
                    //}
                    break;
                default:
                    break;
            }
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            if (this.ActionMode != ActionType.LINE_DRAW)
                return;

            // support for drawing lines
            if (this.lg == null)
                this.GetActors();
            if (this.lg != null)
                this.lg_in_drawMode = lg.DrawMode;

        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            // support for drawing lines
            base.OnMouseLeave(e);
            this.lg_in_drawMode = false;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            GetActors();
            switch(this.ActionMode)
            {
                case ActionType.LINE_DRAW:
                    // support for drawing lines
                    if (this.lg != null && this.lg_in_drawMode)
                    {
                        Point currentHit = e.GetPosition(this);
                        this.lg.OnMouseMove(currentHit);

                        // ---------------------------------- OBJECT SNAP ------------------------------------ //
                        // on entering Action Mode LINE_DRAW:
                        // 1. we synchronize the view frustum with the camera
                        // ?2. we update the OcTree
                        // on entering LineGenerator3D.DrawMode within LINE_DRAW
                        // 3. we perform collision check in the OcTree
                        // 4. we perform visibility check using the OcTree and ViewFrustum
                        // --------------------------------- to ARC lines ------------------------------------ //
                        bool calcOSnap = this.SnapToEndPointsOn || this.SnapToMidPointsOn || this.SnapToIntesectionPointsOn;
                        if (this.arc_display != null && this.otm != null && calcOSnap)
                        {
                            Point3D snapPoint;
                            int index = this.otm.OnMouseMove(currentHit, this, out snapPoint,
                                this.SnapToEndPointsOn, this.SnapToMidPointsOn, this.SnapToIntesectionPointsOn, SNAP_MAGNET);
                            if (index > -1)
                            {
                                this.lg.Use_LastHitPos_SnapOverride = true;
                                this.lg.LastHitPos_SnapOverride = snapPoint;
                            }
                            else
                            {
                                this.lg.Use_LastHitPos_SnapOverride = false;
                            }
                        }
                        // ---------------------------------- OBJECT SNAP ------------------------------------ //
                    }
                    break;
                case ActionType.LINE_EDIT:
                    // support for editing lines
                    if (this.lmGr != null && this.lmGr.IsSelectedCopy)
                    {
                        Point currentHit = e.GetPosition(this);

                        // ---------------------------------- OBJECT SNAP ------------------------------------ //
                        // on entering Action Mode LINE_EDIT:
                        // 1. we synchronize the view frustum with the camera
                        // ?2. we update the OcTree
                        // on entering selecting a line in LINE_EDIT
                        // 3. we perform collision check in the OcTree
                        // 4. we perform visibility check using the OcTree and ViewFrustum
                        // --------------------------------- to ARC lines ------------------------------------ //
                        bool calcOSnap = this.SnapToEndPointsOn || this.SnapToMidPointsOn || this.SnapToIntesectionPointsOn;
                        if (this.arc_display != null && this.otm != null && calcOSnap)
                        {
                            Point3D snapPoint;
                            int index = this.otm.OnMouseMove(currentHit, this, out snapPoint,
                                this.SnapToEndPointsOn, this.SnapToMidPointsOn, this.SnapToIntesectionPointsOn, SNAP_MAGNET);
                            if (index > -1)
                            {
                                this.lmGr.Use_OSnapPoint = true;
                                this.lmGr.OSnapPoint = snapPoint;
                            }
                            else
                            {
                                this.lmGr.Use_OSnapPoint = false;
                            }
                        }
                        // ---------------------------------- OBJECT SNAP ------------------------------------ //
                    }
                    break;
            }

        }
        #endregion
    }
}
