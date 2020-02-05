using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;

using GeometryViewer.SpatialOrganization;
using GeometryViewer.EntityGeometry;
using GeometryViewer.ComponentReps;

namespace GeometryViewer.Utils
{
    class CommandExt : GeometryModel3D
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // =================================================== INIT =============================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region CLASS MEMBERS

        // ---------- LineGenerator(Selectable) LineGroupDisplay(DXF Import and Storage) TRANSFER ----------------- //

        public LineGenerator3D LineGen { get; private set; }
        public ArchitectureDisplay ArcDisp { get; private set; }

        private Viewport3DXext viewPort;
        public ICommand TransferSingleFromEditToArcCmd { get; private set; }
        public ICommand TransferSingleFromArcToEditCmd { get; private set; }       

        // -------- ZoneGroupDispay(Zone Management) LineGroupDisplay(DXF Import and Storage) TRANSFER ------------ //

        public ZoneGroupDisplay ZoneDisp { get; private set; }
        public ICommand TransferSingleFromArcToZoneDisplayCmd { get; private set; }

        // ---------------------------------- Compoenent Representation Display ----------------------------------- //
        public ComponentDisplay CompDisp { get; private set; }
        public ICommand SwitchFromCompRepToZonedVolumeCmd { get; private set; }
        public ICommand SwitchFromZonedVolumeToCompRepCmd { get; private set; }
        public ICommand AssociateCompRepWZonedVolumeCmd { get; private set; }
        public ICommand PlaceCompRepInZonedVolumeCmd { get; private set; }
        public ICommand AlignCompRepWZonedVolumeCmd { get; private set; }
        
        // TODO: Associate with a surface in a ZonedVolume, with a Layer, place inside a ZonedVolume...

        // ------------------------------ OcTree and ViewFrustum TRANSFER ----------------------------------------- //

        public OcTreeManager OTManager { get; private set; }
        public ViewFrustumFunctions ViewFF { get; private set; }
        public ICommand DetermineOTVisibilityCmd { get; private set; }

        #endregion

        #region .CTOR
        public CommandExt()
        {
            // transfer btw the LineGenerator3D and the ArchitectureDisplay
            this.LineGen = null;
            this.ArcDisp = null;
            this.TransferSingleFromArcToEditCmd =
                new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnTransferSingleFromArcToEdit(),
                                               (x) => CanExecute_OnTransferSingleFromArcToEdit());
            this.TransferSingleFromEditToArcCmd =
                new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnTransferSingleFromEditToArc(),
                                               (x) => CanExecute_OnTransferSingleFromEditToArc());

            // transfer btw the ArchitectureDisplay and the ZoneGroupDisplay
            this.ZoneDisp = null;
            this.TransferSingleFromArcToZoneDisplayCmd =
                new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnTransferSingleFromArcToZoneDisplayCommand(),
                                               (x) => CanExecute_OnTransferSingleFromArcToZoneDisplayCommand());

            // transfer btw the ComponentDisplay and the ZoneGroupDisplay
            this.CompDisp = null;
            this.SwitchFromCompRepToZonedVolumeCmd =
                new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnSwitchFromCompRepToZonedVolumeCommand(x),
                                               (x) => CanExecute_OnSwitchFromCompRepToZonedVolumeCommand(x));
            this.SwitchFromZonedVolumeToCompRepCmd =
                new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnSwitchFromZonedVolumeToCompRepCommand(x),
                                               (x) => CanExecute_OnSwitchFromZonedVolumeToCompRepCommand(x));
            this.AssociateCompRepWZonedVolumeCmd =
                new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnAssociateCompRepWZonedVolumeCommand(),
                                               (x) => CanExecute_OnAssociateCompRepWZonedVolumeCommand());
            this.PlaceCompRepInZonedVolumeCmd =
                new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnAssociateCompRepWZonedVolumeCommand(),
                                               (x) => CanExecute_OnPlaceCompRepInZonedVolumeCommand());
            this.AlignCompRepWZonedVolumeCmd = new RelayCommand((x) => OnAlignCompRepWZonedVolumeCommand(),
                                                     (x) => CanExecute_OnAlignCompRepWZonedVolumeCommand());

            // transfer btw the OcTree Manager and the View Frustum
            this.OTManager = null;
            this.ViewFF = null;
            this.DetermineOTVisibilityCmd = new RelayCommand((x) => OnDetermineOTVisibilityCommand(),
                                                  (x) => CanExecute_OnDetermineOTVisibilityCommand());
        }

        #endregion

        #region ACTORS
        private void getActors()
        {
            var parent = this.Parent;
            if (parent == null)
                return;

            Viewport3DXext vp = parent as Viewport3DXext;
            if (vp == null)
                return;

            foreach(var item in vp.Items)
            {
                this.viewPort = vp;
                LineGenerator3D linGen = item as LineGenerator3D;
                ArchitectureDisplay arcDisp = item as ArchitectureDisplay;
                ZoneGroupDisplay zoneDisp = item as ZoneGroupDisplay;
                ComponentDisplay comprepDisp = item as ComponentDisplay;
                OcTreeManager otMan = item as OcTreeManager;
                ViewFrustumFunctions vfFunc = item as ViewFrustumFunctions;
                if (linGen != null)
                    this.LineGen = linGen;
                if (arcDisp != null)
                    this.ArcDisp = arcDisp;
                if (zoneDisp != null)
                {
                    this.ZoneDisp = zoneDisp;
                    if (zoneDisp != null)
                        this.ZoneDisp.PropertyChanged += ZoneDisp_PropertyChanged;
                }
                if (comprepDisp != null)
                    this.CompDisp = comprepDisp;
                if (otMan != null)
                    this.OTManager = otMan;
                if (vfFunc != null)
                    this.ViewFF = vfFunc;
            }
        }
        #endregion

        #region Event Handlers: Picking Zoned Volumes, Alignment with Zoned Volumes

        private CompRepInfo comp_waiting_to_be_assoc_w_volume;
        private bool comp_waiting_for_picked_volume;

        private CompRepInfo comp_waiting_to_be_aligned_w_volume_wall;
        private bool comp_waiting_for_alignment_w_picked_volume_wall;

        void ZoneDisp_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            ZoneGroupDisplay zgd = sender as ZoneGroupDisplay;
            if (zgd != null)
            {
                if (e != null && e.PropertyName == "PickedZonedVolume")
                {
                    if (this.comp_waiting_to_be_assoc_w_volume != null && this.comp_waiting_for_picked_volume)
                    {
                        // complete command OnAssociateCompRepWZonedVolumeCommand()
                        zgd.EManager.ReAssociateZonedVolumeWComp(this.comp_waiting_to_be_assoc_w_volume);

                        this.viewPort.SwitchActionModeCmd.Execute("SPACES_OPENINGS");
                        if (this.CompDisp != null)
                        {
                            this.CompDisp.CompRepMANAGER.UpdateFlatRecord();
                            this.CompDisp.UpdateCompRepList();
                        }
                        // this.viewPort.SendDataToCompBuilder(this.comp_waiting_to_be_assoc_w_volume); // not necessary, call is performed from the CompRep class
                        this.comp_waiting_for_picked_volume = false;
                    }
                    else if (this.comp_waiting_to_be_aligned_w_volume_wall != null && this.comp_waiting_for_alignment_w_picked_volume_wall)
                    {
                        // complete command OnAlignCompRepWZonedVolumeCommand()
                        ZonedVolume selected_vol = zgd.EManager.SelectedVolume;
                        CompRepContainedIn_Instance selected_comp = this.comp_waiting_to_be_aligned_w_volume_wall as CompRepContainedIn_Instance;
                        if (selected_vol != null && selected_comp != null && selected_comp.GR_Relationships[0].GrIds.X == selected_vol.ID)
                        {
                            bool successful_wall_hit = false;
                            Vector3 new_X_Axis = selected_vol.GetHorizontalAxisOfHitWall(zgd.HitPointOnVolumeMesh.ToVector3(), out successful_wall_hit);
                            if (successful_wall_hit)
                            {
                                selected_comp.AlignPlacement(new_X_Axis.ToVector3D());
                            }
                            this.viewPort.SwitchActionModeCmd.Execute("SPACES_OPENINGS");
                            if (this.CompDisp != null)
                            {
                                this.CompDisp.CompRepMANAGER.UpdateFlatRecord();
                                this.CompDisp.UpdateCompRepList();
                            }
                        }

                        this.comp_waiting_for_alignment_w_picked_volume_wall = false;
                    }
                }
            }
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // =========================================== COMMAND COMBINATIONS ======================================= //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region LineGenerator(Selectable) LineGroupDisplay(DXF Import and Storage) TRANSFER A SINGLE MODEL
        private void OnTransferSingleFromEditToArc()
        {
            this.LineGen.TransferToArcDisplayCmd.Execute(null);

            if (this.ArcDisp.UnpackSingleFromLineGeneratorCmd.CanExecute(null))
                this.ArcDisp.UnpackSingleFromLineGeneratorCmd.Execute(null);
        }

        private bool CanExecute_OnTransferSingleFromEditToArc()
        {
            getActors();
            return (this.LineGen != null && this.ArcDisp != null &&
                    this.LineGen.TransferToArcDisplayCmd.CanExecute(null));
        }


        private void OnTransferSingleFromArcToEdit()
        {
            this.ArcDisp.TransferSingleToLineGeneratorCmd.Execute(null);

            if (this.LineGen.UnpackFromArcDisplayCmd.CanExecute(null))
            {
                this.LineGen.UnpackFromArcDisplayCmd.Execute(null);
                this.viewPort.SwitchActionModeCmd.Execute("LINE_EDIT");
            }
        }

        private bool CanExecute_OnTransferSingleFromArcToEdit()
        {
            getActors();
            return (this.LineGen != null && this.ArcDisp != null &&
                    this.ArcDisp.TransferSingleToLineGeneratorCmd.CanExecute(null));
        }
        #endregion

        #region ZoneGroupDispay(Building Physics) ArchitecureDisplay(incl. DXF Import) TRANSFER A SINGLE MODEL

        private void OnTransferSingleFromArcToZoneDisplayCommand()
        {
            // transfer geometry
            this.ArcDisp.TransferSingleToZoneGroupDisplCmd.Execute(null);
            if (this.ZoneDisp.TransferPolygonDefinionCmd.CanExecute(null))
                this.ZoneDisp.TransferPolygonDefinionCmd.Execute(null);
        }

        private bool CanExecute_OnTransferSingleFromArcToZoneDisplayCommand()
        {
            getActors();
            return (this.ArcDisp != null && this.ZoneDisp != null &&
                    this.ArcDisp.TransferSingleToZoneGroupDisplCmd.CanExecute(null));
        }

        #endregion

        #region Component Representation -> Associated ZonedVolume

        private void OnSwitchFromCompRepToZonedVolumeCommand(object _o)
        {
            CompRepInfo comp = _o as CompRepInfo;
            if (comp == null)
                return;

            this.viewPort.SwitchActionModeCmd.Execute("BUILDING_PHYSICS");
            switch(comp.GR_State.Type)
            {
                case InterProcCommunication.Specific.Relation2GeomType.DESCRIBES:
                    CompRepDescirbes crd = comp as CompRepDescirbes;
                    if (crd != null)
                    {
                        this.ZoneDisp.SelectedEntity = crd.Geom_Zone;
                    }
                    break;
                default:
                    break;
            }
            
        }

        private bool CanExecute_OnSwitchFromCompRepToZonedVolumeCommand(object _o)
        {
            if (_o == null)
                return false;

            CompRepInfo comp = _o as CompRepInfo;
            if (comp == null)
                return false;
            if (comp.GR_State.Type != InterProcCommunication.Specific.Relation2GeomType.DESCRIBES &&
                comp.GR_State.Type != InterProcCommunication.Specific.Relation2GeomType.GROUPS)
                return false;

            if (this.ZoneDisp == null || this.CompDisp == null)
                return false;

            return true;
        }

        #endregion

        #region Associated ZonedVolume -> Component Representation

        private void OnSwitchFromZonedVolumeToCompRepCommand(object _o)
        {
            ZonedVolume zv = _o as ZonedVolume;
            if (zv == null)
                return;

            this.viewPort.SwitchActionModeCmd.Execute("SPACES_OPENINGS");
            this.CompDisp.SelectedCompRep = zv.GetDescribingCompOrFirst();
        }

        private bool CanExecute_OnSwitchFromZonedVolumeToCompRepCommand(object _o)
        {
            if (_o == null)
                return false;

            ZonedVolume zv = _o as ZonedVolume;
            if (zv == null)
                return false;
            if (!zv.AssociatedWComp)
                return false;

            if (this.ZoneDisp == null || this.CompDisp == null)
                return false;

            return true;
        }

        #endregion

        #region Component Representation / ZonedVolume interaction

        private void OnAssociateCompRepWZonedVolumeCommand()
        {
            CompRepInfo comp = this.CompDisp.SelectedCompRep;
            if (comp == null)
                return;

            this.comp_waiting_to_be_assoc_w_volume = comp;
            this.comp_waiting_for_picked_volume = true;
            this.viewPort.SwitchActionModeCmd.Execute("BUILDING_PHYSICS");
            this.ZoneDisp.SwitchZoneEditModeCmd.Execute("VOLUME_PICK");
        }

        private bool CanExecute_OnAssociateCompRepWZonedVolumeCommand()
        {
            if (this.ZoneDisp == null || this.CompDisp == null)
                return false;

            CompRepInfo comp = this.CompDisp.SelectedCompRep;
            if (comp == null) return false;
            if (!(comp is CompRepDescirbes)) return false;
            if (comp.GR_State.IsRealized) return false;

            return true;
        }

        private bool CanExecute_OnPlaceCompRepInZonedVolumeCommand()
        {
            if (this.ZoneDisp == null || this.CompDisp == null)
                return false;

            CompRepInfo comp = this.CompDisp.SelectedCompRep;
            if (comp == null) return false;
            if (!(comp is CompRepContainedIn_Instance)) return false;
            if (comp.GR_State.IsRealized) return false;

            return true;
        }

        private void OnAlignCompRepWZonedVolumeCommand()
        {
            CompRepInfo comp = this.CompDisp.SelectedCompRep;
            if (comp == null)
                return;

            this.comp_waiting_to_be_aligned_w_volume_wall = comp;
            this.comp_waiting_for_alignment_w_picked_volume_wall = true;
            this.viewPort.SwitchActionModeCmd.Execute("BUILDING_PHYSICS");
            this.ZoneDisp.SwitchZoneEditModeCmd.Execute("VOLUME_PICK");
        }

        private bool CanExecute_OnAlignCompRepWZonedVolumeCommand()
        {
            if (this.ZoneDisp == null || this.CompDisp == null)
                return false;
            
            CompRepInfo comp = this.CompDisp.SelectedCompRep;
            if (comp == null) return false;
            if (!(comp is CompRepContainedIn_Instance)) return false;
            if (!comp.GR_State.IsRealized) return false;

            return true;
        }

        #endregion

        #region OcTreeManager ViewFrustumFunctions
        private void OnDetermineOTVisibilityCommand()
        {
            this.OTManager.CheckForVisibilityCmd.Execute(this.ViewFF);
        }
        private bool CanExecute_OnDetermineOTVisibilityCommand()
        {
            getActors();
            return (this.OTManager != null && this.ViewFF != null &&
                    this.OTManager.CheckForCollisionsCmd.CanExecute(this.ViewFF));
        }
        #endregion
    }
}
