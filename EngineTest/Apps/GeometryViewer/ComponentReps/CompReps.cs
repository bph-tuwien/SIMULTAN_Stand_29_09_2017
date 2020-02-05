using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using InterProcCommunication.Specific;
using GeometryViewer.EntityGeometry;
using GeometryViewer.ComponentInteraction;

using HelixToolkit.SharpDX.Wpf;

namespace GeometryViewer.ComponentReps
{
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // ======================== BASE TYPE: ABSTRACT REPRESENTATION OF A COMPONENT ============================= //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // --------------------------------------------- BASE TYPE ------------------------------------------------ //

    #region Abstract Component Representation
    public abstract class CompRep : INotifyPropertyChanged
    {
        internal static long NR_COMP_REPS = 0L;

        #region PROPERTIES: INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RegisterPropertyChanged(string _propName)
        {
            if (_propName == null)
                return;

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(_propName));
        }

        #endregion

        #region PROPERTIES: id, generated automatically

        protected long cr_id;
        public long CR_ID
        {
            get { return this.cr_id; }
            protected set 
            { 
                this.cr_id = value;
                this.RegisterPropertyChanged("CR_ID");
            }
        }

        protected bool cr_gen_automatically;
        public bool CR_GeneratedAutomatically
        {
            get { return this.cr_gen_automatically; }
            protected set 
            {
                this.cr_gen_automatically = value;
                this.RegisterPropertyChanged("CR_GeneratedAutomatically");
            }
        }

        #endregion

        #region PROPERTIES: display in a treeview

        protected bool isExpanded;
        public bool IsExpanded
        {
            get { return this.isExpanded; }
            set
            {
                this.isExpanded = value;
                RegisterPropertyChanged("IsExpanded");
            }
        }

        protected bool isSelected;
        public bool IsSelected
        {
            get { return this.isSelected; }
            set
            {
                this.isSelected = value;
                RegisterPropertyChanged("IsSelected");
            }
        }

        #endregion

        #region CLASS MEMBERS: the viewport playing the role of communication port

        // communication
        protected Viewport3DXext comm_manager;

        #endregion

        internal CompRep(bool _automatic, Viewport3DXext _manager)
        {
            // generic
            this.CR_ID = (++CompRep.NR_COMP_REPS);
            this.CR_GeneratedAutomatically = _automatic;

            // communication
            this.comm_manager = _manager;
        }

        #region VIRTUAL METHODS: Message Passing

        public virtual ComponentMessage ExtractMessage(MessagePositionInSeq _pos)
        {
            return null;
        }

        #endregion
  
    }

    #endregion

    // ------------------------ BASE TYPE FOR CARRYING INFORMATION ABOUT COMPONENTS --------------------------- //

    #region Information carrying component

    public class CompRepInfo : CompRep
    {
        #region PROPERTIES: component structure related containers

        public long CR_Parent { get; internal set; }

        // reflects the sub-component structure
        protected List<CompRepInfo> cr_sub_comp_reps;
        public ReadOnlyCollection<CompRepInfo> CR_SubCompReps { get { return this.cr_sub_comp_reps.AsReadOnly(); } }

        /// <summary>
        /// <para>Adds a subcomponent representation</para>
        /// <para>if the parent component id of _sub corresponds to the component id saved in this representation.</para>
        /// <para>Is to be called by the CompRepManager after receiving a sequence of messages.</para>
        /// </summary>
        /// <param name="_sub"></param>
        public void AddSubCompRep(CompRepInfo _sub)
        {
            if (_sub == null) return;

            // guarantee consistency!
            if (_sub.Comp_Parent != this.Comp_ID) return;

            // no duplicates!
            CompRep duplicate = this.cr_sub_comp_reps.FirstOrDefault(x => x.CR_ID == _sub.CR_ID);
            if (duplicate != null) return;

            // and finally ... add!
            this.cr_sub_comp_reps.Add(_sub);
        }

        #endregion

        #region PROPERTIES: component data

        public ComponentMessage Comp_Msg { get; protected set; } // probably does not need to be public
        public long Comp_ID { get; protected set; }
        public string Comp_Description { get; protected set; }

        protected List<long> comp_ref_comp_ids;
        public virtual ReadOnlyCollection<long> Comp_RefCompIDs { get { return this.comp_ref_comp_ids.AsReadOnly(); } }
        public long Comp_Parent { get; protected set; }

        // relationships to geometry
        public List<GeometricRelationship> GR_Relationships { get; set; }
        public Relation2GeomState GR_State { get; protected set; }

        // component parameters
        protected Dictionary<string, double> comp_param_values;

        #endregion

        #region PROPERTIES: Display
        // derived - used in the data template in App.xaml
        public string GR_RelationshipWith
        {
            get
            {
                if (this.GR_Relationships.Count == 0)
                    return "---";
                else
                    return "(" + this.GR_Relationships[0].GrIds.X + "," + this.GR_Relationships[0].GrIds.Y + "," + 
                                 this.GR_Relationships[0].GrIds.Z + "," + this.GR_Relationships[0].GrIds.W + ")";
            }
        }

        // derived - used in the data template in App.xaml
        public string ALL_IDS
        {
            get
            {
                // component ID, PARENT component ID; component representation ID, PARENT component representation ID
                return "C[" + this.Comp_ID + "] CP[" + this.Comp_Parent + "]| R[" + this.CR_ID + "] RP[" + this.CR_Parent + "]";
            }
        }

        // derived - used in the data template in App.xaml
        public string Comp_Description_Long
        {
            get
            {
                string descr = "represents \"" + this.Comp_Description + "\" with ID " + this.Comp_ID;
                
                if (this.Comp_Parent > -1)
                    descr += ", child of " + this.Comp_Parent;
                
                if (this.GR_Relationships.Count > 1)
                    descr += ", type of " + this.GR_Relationships.Count + " instances";
                
                if (this.comp_ref_comp_ids.Count > 0)
                {
                    descr += "/n, referencing";
                    foreach(long ref_id in this.comp_ref_comp_ids)
                    {
                        descr += " " + ref_id;
                    }
                }
                descr += ".";

                return descr;
            }
        }

        public string DisplayLabel { get; protected set; }
        public LineGeometry3D DisplayLines { get; protected set; }
        public LineGeometry3D DisplayLinesSecondary { get; protected set; }
        public HelixToolkit.SharpDX.Wpf.MeshGeometry3D DisplayMesh { get; protected set; }

        #endregion

        #region PROPERTIES: message passing

        public bool ExtractUpdateMessage { get; protected set; }

        #endregion

        #region .CTOR GENERIC
        public CompRepInfo(bool _automatic, Viewport3DXext _manager)
            :base(_automatic, _manager)
        {
            // component structure
            this.CR_Parent = -1;
            this.cr_sub_comp_reps = new List<CompRepInfo>();

            // component data
            this.Comp_Msg = null;
            this.Comp_ID = -1;
            this.Comp_Description = "Not a Component";
            this.comp_ref_comp_ids = new List<long>();
            this.Comp_Parent = -1;

            // connection btw component and geometry data
            this.GR_Relationships = new List<GeometricRelationship>();
            this.GR_State = new Relation2GeomState();
            
            // relevant component parameters
            this.comp_param_values = new Dictionary<string, double>();    
       
            // message transfer
            this.ExtractUpdateMessage = true;

        }
        #endregion

        #region .CTOR FROM EXISTING COMPONENT

        public CompRepInfo(ComponentMessage _cmsg, Viewport3DXext _manager)
            : base(_cmsg.CompAutomaticallyGenerated, _manager)
        {
            // component STRUCTURE
            this.CR_Parent = -1;                                // ------------- deferred to the CompRepManager
            this.cr_sub_comp_reps = new List<CompRepInfo>();    // ------------- deferred to the CompRepManager
            
            // component DATA
            this.Comp_Msg = _cmsg;
            if (_cmsg != null)
            {
                this.Comp_ID = _cmsg.CompID;
                this.Comp_Description = _cmsg.CompDescr;
                this.comp_ref_comp_ids = new List<long>(_cmsg.CompRefIds);
                this.Comp_Parent = _cmsg.CompParentID;
            }

            // connection btw component and GEOMETRY data
            if (_cmsg != null)
                this.GR_Relationships = new List<GeometricRelationship>(_cmsg.GeomRelationships);
            if (this.GR_Relationships.Count < 1)
            {
                // this should not happen!
                this.GR_State = new Relation2GeomState();
            }
            else
            {
                this.GR_State = new Relation2GeomState( this.GR_Relationships[0].GrState);
            }

            // transfer the relevant component PARAMETERS
            if (this.GR_State.Type != Relation2GeomType.NONE)
            {
                Dictionary<string, double> guide = Comp2GeomCommunication.GetReservedParamDictionary(this.GR_State.Type);
                this.comp_param_values = Comp2GeomCommunication.GetReservedParamDictionary(this.GR_State.Type);
                foreach (var entry in guide)
                {
                    this.comp_param_values[entry.Key] = _cmsg[entry.Key];
                }
            }

            // message transfer
            this.ExtractUpdateMessage = true;
        }

        #endregion

        #region METHODS: ToString

        public override string ToString()
        {
            string output = "CompRepInfo ";
            output += "[" + this.CR_ID + "][comp " + this.Comp_ID + "][gr " + this.GR_State.Type + "]";
            return output;
        }

        #endregion

        #region METHODS: Switch the contained component message

        /// <summary>
        /// <para>The switch is performed only if the current component id is -1.</para>
        /// <para>Use to update the component id of automatically created components.</para>
        /// </summary>
        /// <param name="_cmsg"></param>
        public void AdoptCompId(ComponentMessage _cmsg)
        {
            if (_cmsg == null) return;
            if (this.Comp_ID > -1) return;

            if (this.Comp_ID < 0 && _cmsg.CompID > -1)
                this.Comp_ID = _cmsg.CompID;
            if (this.Comp_Parent < 0 && _cmsg.CompParentID > -1)
                this.Comp_Parent = _cmsg.CompParentID;
            this.Comp_Description = _cmsg.CompDescr;
        }

        #endregion

        #region METHODS: Info about sub-CompReps

        /// <summary>
        /// Searches recursively in all sub-representations for any of the types.
        /// </summary>
        /// <param name="_types"></param>
        /// <returns></returns>
        public bool HasCompRepOfType(List<Relation2GeomType> _types)
        {
            if (_types == null || _types.Count == 0) return true;

            if (this.cr_sub_comp_reps == null)
                return false;
            else
            {
                foreach(CompRepInfo cri in this.cr_sub_comp_reps)
                {
                    if (_types.Contains(cri.GR_State.Type))
                        return true;
                }
                foreach(CompRepInfo cri in this.cr_sub_comp_reps)
                {
                    bool found__in_cri = cri.HasCompRepOfType(_types);
                    if (found__in_cri)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Recursive. Counts all levels of sub-representations.
        /// </summary>
        /// <returns></returns>
        protected int GetNrOfAllSubCompReps()
        {
            int nr = this.cr_sub_comp_reps.Count;
            if (nr == 0)
                return 0;

            foreach (CompRepInfo cri in this.cr_sub_comp_reps)
            {
                nr += cri.GetNrOfAllSubCompReps();
            }

            return nr;
        }

        /// <summary>
        /// Gathers sub-representations depth-first.
        /// </summary>
        /// <returns></returns>
        internal virtual List<CompRepInfo> GetFlatListOfSubCompReps()
        {
            List<CompRepInfo> found = new List<CompRepInfo>();
            if (this.cr_sub_comp_reps.Count == 0)
                return found;

            // depth-first
            foreach(CompRepInfo cri in this.cr_sub_comp_reps)
            {
                found.Add(cri);
                found.AddRange(cri.GetFlatListOfSubCompReps());
            }

            return found;
        }

        #endregion

        #region METHODS: referenced CompReps

        /// <summary>
        /// Saves a reference to a represented component using its ID. Sends update to calling module.
        /// </summary>
        /// <param name="_to_be_ref"></param>
        public void TakeReference(CompRepInfo _to_be_ref)
        {
            if (_to_be_ref == null) return;
            if (_to_be_ref.Comp_ID < 0) return;
            if (this.Comp_ID == _to_be_ref.Comp_ID) return;

            if (this.comp_ref_comp_ids.Contains(_to_be_ref.Comp_ID)) return;

            this.comp_ref_comp_ids.Add(_to_be_ref.Comp_ID);
            if (this.comm_manager != null)
                this.comm_manager.SendDataToCompBuilder(this, InterProcCommunication.CommMessageType.REF_UPDATE);
        }

        /// <summary>
        /// Saves references to all represented components using their ID. Sends update to calling module.
        /// </summary>
        /// <param name="_to_be_refs"></param>
        public void TakeReferences(List<CompRepInfo> _to_be_refs)
        {
            if (_to_be_refs == null) return;
            if (_to_be_refs.Count == 0) return;

            foreach(CompRepInfo cri in _to_be_refs)
            {
                if (cri == null) continue;
                if (cri.Comp_ID < 0) continue;
                if (this.Comp_ID == cri.Comp_ID) continue;

                if (this.comp_ref_comp_ids.Contains(cri.Comp_ID)) continue;
                this.comp_ref_comp_ids.Add(cri.Comp_ID);
            }

            if (this.comm_manager != null)
                this.comm_manager.SendDataToCompBuilder(this, InterProcCommunication.CommMessageType.REF_UPDATE);
        }

        /// <summary>
        /// Removes a reference to a represented component using its ID. Sends update to calling module.
        /// </summary>
        /// <param name="_to_be_discarded"></param>
        public void DiscardReference(CompRepInfo _to_be_discarded)
        {
            if (_to_be_discarded == null) return;
            if (_to_be_discarded.Comp_ID < 0) return;
            if (this.Comp_ID == _to_be_discarded.Comp_ID) return;

            if (!(this.comp_ref_comp_ids.Contains(_to_be_discarded.Comp_ID))) return;

            this.comp_ref_comp_ids.Remove(_to_be_discarded.Comp_ID);
            if (this.comm_manager != null)
                this.comm_manager.SendDataToCompBuilder(this, InterProcCommunication.CommMessageType.REF_UPDATE);
        }

        /// <summary>
        /// Removes the references to all represented components using their IDs. Sends update to calling module.
        /// </summary>
        /// <param name="_to_be_discarded"></param>
        public void DiscradReferences(List<CompRepInfo> _to_be_discarded)
        {
            if (_to_be_discarded == null) return;
            if (_to_be_discarded.Count == 0) return;

            List<long> ids_to_remove = new List<long>();
            foreach(CompRepInfo cri in _to_be_discarded)
            {
                if (cri == null) continue;
                if (cri.Comp_ID < 0) continue;
                if (this.Comp_ID == cri.Comp_ID) continue;

                if (!(this.comp_ref_comp_ids.Contains(cri.Comp_ID))) continue;
                ids_to_remove.Add(cri.Comp_ID);
            }

            if (ids_to_remove.Count == 0) return;

            foreach(long id in ids_to_remove)
            {
                this.comp_ref_comp_ids.Remove(id);
            }

            if (this.comm_manager != null)
                this.comm_manager.SendDataToCompBuilder(this, InterProcCommunication.CommMessageType.REF_UPDATE);
        }

        #endregion

        #region OVERRIDES: Message Passing

        public override ComponentMessage ExtractMessage(MessagePositionInSeq _pos)
        {
            return new ComponentMessage(_pos, this.Comp_Parent, this.Comp_ID, this.CR_GeneratedAutomatically, this.Comp_Description,
                                              this.comp_ref_comp_ids, this.comp_param_values, this.GR_Relationships,
                                              this.CR_ID, this.CR_Parent, MessageAction.UPDATE);
        }

        public void SetSubCompRepsTransferState(bool _transfer)
        {
            foreach (CompRepInfo cri in this.cr_sub_comp_reps)
            {
                cri.ExtractUpdateMessage = _transfer;
                cri.SetSubCompRepsTransferState(_transfer);
            }
        }

        #endregion

        #region METHODS: Synchronization W Content

        public virtual void SynchronizeWContent()
        { }

        public virtual bool ContentChanged()
        {
            return false;
        }

        /// <summary>
        /// <para>To be called if this representation is dis-associated from its geometry.</para>
        /// <para>Recursively sets the state of all sub-representaion instances to 'IsRealized = false'.</para>
        /// </summary>
        protected void PropagateNotRealized()
        {
            foreach(CompRepInfo cri in this.cr_sub_comp_reps)
            {
                foreach (GeometricRelationship gr in cri.GR_Relationships)
                {
                    gr.SetTypeToNotRealized();
                }
                if (cri.GR_Relationships.Count > 0)
                {
                    cri.GR_State = new Relation2GeomState { IsRealized = false, Type = cri.GR_Relationships[0].GrState.Type };
                }
                cri.PropagateNotRealized();
            }
        }

        #endregion

        #region METHODS: Visualisation of sub-representations

        internal virtual void GetDisplayables(ref LineBuilder _lb_all, ref LineBuilder _lb_all_2, ref List<HelixToolkit.SharpDX.Wpf.MeshGeometry3D> _boxes_all, long _id_to_exclude)
        { }

        /// <summary>
        /// Fills the DisplayLines, DisplayLinesSecondary, and DisplayMesh fields depending on the sub-representations.
        /// </summary>
        /// <param name="_id_cr">is the id of the comp representation whose geometry relationship _id_gr_to_exclude is to be excluded</param>
        /// <param name="_id_gr_to_exclude">is the id of the geometry relationship to be excluded from the display</param>
        public virtual void AdaptDisplay(long _id_cr, long _id_gr_to_exclude)
        {
            LineBuilder lb = new LineBuilder();
            LineBuilder lb_2 = new LineBuilder();
            List<HelixToolkit.SharpDX.Wpf.MeshGeometry3D> boxes = new List<HelixToolkit.SharpDX.Wpf.MeshGeometry3D>();

            foreach (CompRepInfo entry in this.cr_sub_comp_reps)
            {
                long gr_to_exclude = -1;
                if (entry.CR_ID == _id_cr)
                    gr_to_exclude = _id_gr_to_exclude;

                entry.GetDisplayables(ref lb, ref lb_2, ref boxes, gr_to_exclude);
            }

            LineGeometry3D lines = lb.ToLineGeometry3D();
            LineGeometry3D lines_2 = lb_2.ToLineGeometry3D();
            HelixToolkit.SharpDX.Wpf.MeshGeometry3D meshes = Utils.MeshesCustom.CombineMeshes(boxes);

            if (lines != null)
                this.DisplayLines = lines;
            if (lines_2 != null)
                this.DisplayLinesSecondary = lines_2;
            if (meshes != null)
                this.DisplayMesh = meshes;
        }

        #endregion
    }


    #endregion


    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // ================ SPECIALIZED TYPES: REPRESENTATION OF A COMPONENT CONNECTED TO A ZONE ================== //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // ---------------------------------------- DESCRIBES A SPACE --------------------------------------------- //

    #region TYPE: DESCRIBES -> e.g. Zone / Space

    public class CompRepDescirbes : CompRepInfo
    {
        #region PROPERTIES: local geometry

        protected ZonedVolume geom_zone;
        public ZonedVolume Geom_Zone
        {
            get { return this.geom_zone; }
            set
            {
                // check if anything should be done at all
                if (this.geom_zone == null && value == null) return;
                if (this.geom_zone != null && value != null && this.geom_zone.ID == value.ID) return;

                // release the previously associated ZonedVolume
                if (this.geom_zone != null)
                {
                    // remove the references to all geometric neighbors
                    if (this.comm_manager != null)
                    { 
                        List<long> ids_to_remove = this.comm_manager.ReleaseNeighborsFrom(this); // passes info to the neighbors
                        foreach(long id in ids_to_remove)
                        {
                            this.geometric_neighbors.Remove(id);
                        }
                    }
                    this.geom_zone.PropertyChanged -= geom_PropertyChanged;                  
                    this.geom_zone.RemoveCompRepAssociation(this);                    
                }

                // SET
                this.geom_zone = value;

                // bind the currently associated ZonedVolume
                if (this.geom_zone != null)
                {
                    this.geom_zone.PropertyChanged += geom_PropertyChanged;
                    this.geom_zone.AddCompRepAssociation(this);
                    this.nr_geom_changes_since_loading = this.geom_zone.NrChangesSinceLoading;
                    // covers different types of relationship
                    this.GR_Relationships[0].SetTypeToRealized(new Point4D(this.geom_zone.ID, -1, -1, -1));
                    this.GR_State = new Relation2GeomState { IsRealized = true, Type = this.GR_Relationships[0].GrState.Type };
                    // make sure that the subcomponents are generated before CompRepManager.ExtractMessagesFrom is called
                    // so that it functions correctly
                    this.SynchronizeWContent(); 
                }
                else
                {
                    this.nr_geom_changes_since_loading = 0;
                    this.GR_Relationships[0].SetTypeToNotRealized();
                    this.GR_State = new Relation2GeomState { IsRealized = false, Type = this.GR_Relationships[0].GrState.Type };
                    this.PropagateNotRealized();                   
                }
                
                if (this.comm_manager != null)
                    this.comm_manager.SendDataToCompBuilder(this); // calls this.ExtractMessage() which calls this.SynchronizeWContent()

                // announce change
                this.RegisterPropertyChanged("Geom_Zone");
            }
        }

        private int nr_geom_changes_since_loading;
        private List<long> geometric_neighbors;

        public override ReadOnlyCollection<long> Comp_RefCompIDs
        {
            get
            {
                List<long> all_ref = new List<long>(this.comp_ref_comp_ids);
                all_ref.AddRange(this.geometric_neighbors);
                return all_ref.AsReadOnly();
            }
        }
        #endregion

        // for EXISTING components sent by the ComponentBuilder
        public CompRepDescirbes(ComponentMessage _cmsg, Viewport3DXext _manager)
            :base(_cmsg, _manager)
        {
            // local geometry
            // NOTE: the '_manager' looks for the referenced geometry AFTER the constructor (see Viewport3DXext.cs)
            this.geom_zone = null;
            this.nr_geom_changes_since_loading = 0;
            this.geometric_neighbors = new List<long>();
        }

        #region METHODS: ToString

        public override string ToString()
        {
            string output = "CompRepDescirbes ";
            output += "[" + this.CR_ID + "][comp " + this.Comp_ID + "][gr " + this.GR_State.Type + "]";
            if (this.Geom_Zone != null)
                output += "[zv " + this.Geom_Zone.ID + "]";

            return output;
        }

        #endregion

        #region OVERRIDES: data transfer from geometry

        /// <summary>
        /// <para>Searches for the 3D representation in its sub-representations and synchs it w the volume.</para>
        /// <para>Searches for the 2D representations in its sub-representations and synchs them w the surfaces and openings.</para>
        /// <para>Searches for geometric neighbors and gathers referenes to thier describing components.</para>
        /// <para>To be called after changes in geometry or before sending data to the calling module.</para>
        /// </summary>
        public override void SynchronizeWContent()
        {
            if (this.geom_zone == null)
            {
                // reset all sub-components
                // WARNING: this does not stop the transfer of the sub-components back to CB
                // unless their property ExtractUpdateMessage is set to FALSE ->
                // see method OnDisAssociateCompRepWZonedVoume() in ComponentDisplay
                this.cr_sub_comp_reps = new List<CompRepInfo>();

                // reset the neighborhood relationships
                this.geometric_neighbors = new List<long>();
                return; 
            }
            else
            {
                // synch the 3D representation
                CompRepDescirbes3D cr_3d;
                if (this.cr_sub_comp_reps.Count > 0)
                {
                    cr_3d = this.cr_sub_comp_reps.Where(x => x is CompRepDescirbes3D).Select(x => x as CompRepDescirbes3D).ToList().First();
                    if (cr_3d != null)
                        cr_3d.SynchronizeWVolume(this.geom_zone);
                }
                else
                {
                    cr_3d = new CompRepDescirbes3D(this.comm_manager, this);
                }

                // synch the 2D representationS
                List<SurfaceInfo> info_2d = this.geom_zone.ExportInfoToCompRep();
                List<CompRepDescribes2DorLess> to_synch = this.cr_sub_comp_reps.Where(x => x is CompRepDescribes2DorLess).Select(x => x as CompRepDescribes2DorLess).ToList();
                CompRepDescribes2DorLess.SynchronizeCRListWSurfaceInfoList(this.comm_manager, this, ref to_synch, ref info_2d);

                // update the sub-CompReps
                this.cr_sub_comp_reps = new List<CompRepInfo>();
                this.cr_sub_comp_reps.Add(cr_3d);
                this.cr_sub_comp_reps.AddRange(to_synch);

                // update the neighborhood relationships
                if (this.comm_manager != null)
                {
                    this.geometric_neighbors = this.comm_manager.GetNeighborsFor(this);
                    foreach(long gn_id in this.geometric_neighbors)
                    {
                        this.comp_ref_comp_ids.Remove(gn_id);
                    }
                }                   
                else
                    this.geometric_neighbors = new List<long>();

            }            
        }

        /// <summary>
        /// <para>Returns 'true' if the associated volume metrics have changed beyond a threshold value.</para>
        /// <para>Use to avoid unnecessary synchronization calls.</para>
        /// </summary>
        /// <returns></returns>
        public override bool ContentChanged()
        {
            if (this.geom_zone == null) return false;
            // check the 3D representation
            if (this.cr_sub_comp_reps.Count > 0)
            {
                CompRepDescirbes3D cr_3d = this.cr_sub_comp_reps[0] as CompRepDescirbes3D;
                if (cr_3d != null)
                    return cr_3d.VolumeGeometryChanged(this.Geom_Zone);
            }

            return false;
        }

        public bool AddNeighborReferenceIfMissing(CompRepDescirbes _future_neighbor)
        {
            if (_future_neighbor == null) return false;
            if (this.comp_ref_comp_ids.Contains(_future_neighbor.Comp_ID) ||
                this.geometric_neighbors.Contains(_future_neighbor.Comp_ID)) return false;

            this.geometric_neighbors.Add(_future_neighbor.Comp_ID);
            return true;
        }

        public bool RemoveNeighborReferenceIfPresent(CompRepDescirbes _former_neighbor)
        {
            if (_former_neighbor == null) return false;
            if (this.geometric_neighbors.Contains(_former_neighbor.Comp_ID))
            {
                // in case the neighbor was added during THIS SESSION
                this.geometric_neighbors.Remove(_former_neighbor.Comp_ID);
                return true;
            }
            else if (this.comp_ref_comp_ids.Contains(_former_neighbor.Comp_ID))
            {
                // in case the neighbor was added during a PREVIOUS SESSION
                this.comp_ref_comp_ids.Remove(_former_neighbor.Comp_ID);
                return true;
            }

            return false;
        }

        public void PassMaterialAssignmentToSurfaceRep(long _vol_id, int _zone_label, int _opening_index, long _lower_poly_id,  long _material_id)
        {
            List<CompRepDescribes2DorLess> to_search = this.cr_sub_comp_reps.Where(x => x is CompRepDescribes2DorLess).Select(x => x as CompRepDescribes2DorLess).ToList();
            foreach (CompRepDescribes2DorLess cr_2d in to_search)
            {
                Point4D cr_2d_ids = (cr_2d.GR_Relationships.Count > 0) ? cr_2d.GR_Relationships[0].GrIds : new Point4D(-1, -1, -1, -1);
                if (cr_2d_ids.X == _vol_id && cr_2d_ids.Y == _zone_label &&
                    cr_2d_ids.Z == _opening_index && cr_2d_ids.W == _lower_poly_id)
                {
                    cr_2d.Material_ID = _material_id;
                    break;
                }                    
            }
        }

        #endregion

        #region OVERRIDE METHODS: Message Passing

        public override ComponentMessage ExtractMessage(MessagePositionInSeq _pos)
        {
            this.SynchronizeWContent();

            // return base.ExtractMessage(_pos);

            List<long> all_references = new List<long>(this.comp_ref_comp_ids);
            all_references.AddRange(this.geometric_neighbors);

            return new ComponentMessage(_pos, this.Comp_Parent, this.Comp_ID, this.CR_GeneratedAutomatically, this.Comp_Description,
                                              all_references, this.comp_param_values, this.GR_Relationships,
                                              this.CR_ID, this.CR_Parent, MessageAction.UPDATE);
        }

        #endregion

        #region OVERRIDE METHODS: Visualisation of sub-representations

        public override void AdaptDisplay(long _id_cr, long _id_gr_to_exclude)
        {       
        }

        #endregion

        #region EVENT HANDLER: Geometry

        protected void geom_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ZonedVolume zv = sender as ZonedVolume;
            if (e == null || zv == null)
                return;

            if (e.PropertyName == "IsDirty")
            {
                if (!zv.IsDirty)
                {
                    // check to see if there is eny real change!
                    if (this.ContentChanged() || this.nr_geom_changes_since_loading < this.geom_zone.NrChangesSinceLoading)
                    {
                        this.nr_geom_changes_since_loading = this.geom_zone.NrChangesSinceLoading;
                        if (this.comm_manager == null)
                            this.SynchronizeWContent();
                        else
                            this.comm_manager.SendDataToCompBuilder(this);  // calls this.ExtractMessage() which calls this.SynchronizeWContent()
                    }
                }
            }
            else if (e.PropertyName == "EditModeType")
            {
                if (zv.EditModeType == ZonedVolumeEditModeType.ISBEING_DELETED)
                {
                    this.Geom_Zone = null;
                }
            }

        }

        #endregion

    }

    #endregion

    // ---------------------------------- DESCRIBES A 3D OBJECT: VOLUME --------------------------------------- //

    #region TYPE DESCRIBES_3D -> the Volume of a Zone

    public class CompRepDescirbes3D : CompRepInfo
    {
        #region .CTOR w/o Component
        // for FUTURE components, hence no association with a ComponentMessage
        public CompRepDescirbes3D(Viewport3DXext _manager, CompRepDescirbes _parent)
            :base(true, _manager)
        {
            // component STRUCTURE
            this.CR_Parent = _parent.CR_ID;
            this.cr_sub_comp_reps = new List<CompRepInfo>(); // should remain empty

            // component DATA (none, since the component is yet to be created)            
            this.Comp_ID = -1;
            this.Comp_Description = "Volume";
            this.comp_ref_comp_ids = new List<long>(); // should remain empty
            this.Comp_Parent = _parent.Comp_ID;           

            // connection btw component and GEOMETRY data
            this.GR_State = new Relation2GeomState { Type = Relation2GeomType.DESCRIBES_3D, IsRealized = (_parent.Geom_Zone != null) };
            Point4D geom_ids = (_parent.Geom_Zone == null) ? new Point4D(-1, -1, -1, -1) : new Point4D(_parent.Geom_Zone.ID, -1, -1, -1);
            GeometricRelationship main = new GeometricRelationship(-1, "Describes 3D space", this.GR_State, geom_ids, Matrix3D.Identity, Matrix3D.Identity, Matrix3D.Identity);
            this.GR_Relationships = new List<GeometricRelationship>();
            this.GR_Relationships.Add(main);

            // initiate the relevant component PARAMETERS           
            this.comp_param_values = Comp2GeomCommunication.GetReservedParamDictionary(this.GR_State.Type);

            // synchronize parameters and display w the geometry
            this.SynchronizeWVolume(_parent.Geom_Zone);
        }
        #endregion

        #region .CTOR from Component
        // for EXISTING components sent by the ComponentBuilder
        public CompRepDescirbes3D(ComponentMessage _cmsg, Viewport3DXext _manager)
            :base(_cmsg, _manager)
        {
            // see base for details
        }
        #endregion

        #region METHODS: data transfer from geometry

        /// <summary>
        /// <para>The synchronization requires the presense of parameters with pre-defines reserved names.</para>
        /// <para>The size is NOT transferred to the instances contained in GR_Relationships!</para>
        /// </summary>
        /// <param name="_volume"></param>
        internal void SynchronizeWVolume(ZonedVolume _volume)
        {
            if (_volume == null) return;

            // DISPLAY
            this.DisplayLabel = "3D of " + _volume.EntityName;
            this.DisplayLines = _volume.BuildOuterOffsetLines();

            // PARAMETERS
            this.comp_param_values[Comp2GeomCommunication.RP_K_FOK] = _volume.Elevation_NET;
            this.comp_param_values[Comp2GeomCommunication.RP_K_FOK_ROH] = _volume.Elevation_GROSS;
            this.comp_param_values[Comp2GeomCommunication.RP_K_F_AXES] = _volume.Elevation_AXES;
            this.comp_param_values[Comp2GeomCommunication.RP_K_DUK] = _volume.Ceiling_NET;
            this.comp_param_values[Comp2GeomCommunication.RP_K_DUK_ROH] = _volume.Ceiling_GROSS;
            this.comp_param_values[Comp2GeomCommunication.RP_K_D_AXES] = _volume.Ceiling_AXES;
            this.comp_param_values[Comp2GeomCommunication.RP_H_NET] = _volume.MaxHeight_NET;
            this.comp_param_values[Comp2GeomCommunication.RP_H_GROSS] = _volume.MaxHeight_GROSS;
            this.comp_param_values[Comp2GeomCommunication.RP_H_AXES] = _volume.MaxHeight_AXES;
            this.comp_param_values[Comp2GeomCommunication.RP_L_PERIMETER] = _volume.Perimeter;
            this.comp_param_values[Comp2GeomCommunication.RP_AREA_BGF] = _volume.Area_GROSS;
            this.comp_param_values[Comp2GeomCommunication.RP_AREA_NGF] = _volume.Area_NET;
            this.comp_param_values[Comp2GeomCommunication.RP_AREA_NF] = _volume.Area_NET;
            this.comp_param_values[Comp2GeomCommunication.RP_AREA_AXES] = _volume.Area_AXES;
            this.comp_param_values[Comp2GeomCommunication.RP_VOLUME_BRI] = _volume.Volume_GROSS;
            this.comp_param_values[Comp2GeomCommunication.RP_VOLUME_NRI] = _volume.Volume_NET;
            this.comp_param_values[Comp2GeomCommunication.RP_VOLUME_NRI_NF] = _volume.Volume_NET;
            this.comp_param_values[Comp2GeomCommunication.RP_VOLUME_AXES] = _volume.Volume_AXES;

            if (this.GR_Relationships.Count > 0)
            {
                this.GR_Relationships[0].SetTypeToRealized(new Point4D(_volume.ID, -1, -1, -1));
                this.GR_State = new Relation2GeomState { IsRealized = true, Type = Relation2GeomType.DESCRIBES_3D };
            }
        }

        internal bool VolumeGeometryChanged(ZonedVolume _volume, double _tolerance = 0.001)
        {
            if (_volume == null) return false;
            double sumChange = 0.0;

            sumChange += Math.Abs(this.comp_param_values[Comp2GeomCommunication.RP_K_FOK] - _volume.Elevation_NET);
            sumChange += Math.Abs(this.comp_param_values[Comp2GeomCommunication.RP_K_FOK_ROH] - _volume.Elevation_GROSS);
            sumChange += Math.Abs(this.comp_param_values[Comp2GeomCommunication.RP_K_F_AXES] - _volume.Elevation_AXES);
            sumChange += Math.Abs(this.comp_param_values[Comp2GeomCommunication.RP_K_DUK] - _volume.Ceiling_NET);
            sumChange += Math.Abs(this.comp_param_values[Comp2GeomCommunication.RP_K_DUK_ROH] - _volume.Ceiling_GROSS);
            sumChange += Math.Abs(this.comp_param_values[Comp2GeomCommunication.RP_K_D_AXES] - _volume.Ceiling_AXES);
            sumChange += Math.Abs(this.comp_param_values[Comp2GeomCommunication.RP_H_NET] - _volume.MaxHeight_NET);
            sumChange += Math.Abs(this.comp_param_values[Comp2GeomCommunication.RP_H_GROSS] - _volume.MaxHeight_GROSS);
            sumChange += Math.Abs(this.comp_param_values[Comp2GeomCommunication.RP_H_AXES] - _volume.MaxHeight_AXES);
            sumChange += Math.Abs(this.comp_param_values[Comp2GeomCommunication.RP_L_PERIMETER] - _volume.Perimeter);
            sumChange += Math.Abs(this.comp_param_values[Comp2GeomCommunication.RP_AREA_BGF] - _volume.Area_GROSS);
            sumChange += Math.Abs(this.comp_param_values[Comp2GeomCommunication.RP_AREA_NGF] - _volume.Area_NET);
            sumChange += Math.Abs(this.comp_param_values[Comp2GeomCommunication.RP_AREA_NF] - _volume.Area_NET);
            sumChange += Math.Abs(this.comp_param_values[Comp2GeomCommunication.RP_AREA_AXES] - _volume.Area_AXES);
            sumChange += Math.Abs(this.comp_param_values[Comp2GeomCommunication.RP_VOLUME_BRI] - _volume.Volume_GROSS);
            sumChange += Math.Abs(this.comp_param_values[Comp2GeomCommunication.RP_VOLUME_NRI] - _volume.Volume_NET);
            sumChange += Math.Abs(this.comp_param_values[Comp2GeomCommunication.RP_VOLUME_NRI_NF] - _volume.Volume_NET);
            sumChange += Math.Abs(this.comp_param_values[Comp2GeomCommunication.RP_VOLUME_AXES] - _volume.Volume_AXES);

            return (sumChange > _tolerance);
        }

        #endregion

        #region OVERRIDE METHODS

        public override void AdaptDisplay(long _id_cr, long _id_gr_to_exclude)
        {
        }

        public override ComponentMessage ExtractMessage(MessagePositionInSeq _pos)
        {
            MessageAction action = (this.ExtractUpdateMessage) ? MessageAction.UPDATE : MessageAction.DELETE;

            return new ComponentMessage(_pos, this.Comp_Parent, this.Comp_ID, this.CR_GeneratedAutomatically, this.Comp_Description,
                                              this.comp_ref_comp_ids, this.comp_param_values, this.GR_Relationships,
                                              this.CR_ID, this.CR_Parent, action);
        }

        #endregion
    }

    #endregion

    // ----------------------- DESCRIBES A 2D OBJECT or LESS: Surface, Edge or Point -------------------------- //

    #region TYPE DESCRIBES_2DorLESS -> the Wall / Floor / Ceiling of a Zone

    #region HELPER CLASS: SurfaceInfo
    public class SurfaceInfo
    {
        // display
        public string DisplayLabel { get; set; }
        public LineGeometry3D DisplayLines { get; set; }
        public HelixToolkit.SharpDX.Wpf.MeshGeometry3D DisplayMesh { get; set; }
        // position within the volume
        public long VolumeID { get; set; }
        public bool IsWall { get; set; }
        public int LevelOrLabel { get; set; }
        public int LabelForOpenings { get; set; }
        public long WallLowerPoly { get; set; }
        public long WallUpperPoly { get; set; }
        // size
        public double Area_AXES { get; set; }
        public double Area_NET { get; set; }
        public double Area_GROSS { get; set; }
        public double Height_AXES { get; set; }
        public double Height_NET { get; set; }
        public double Height_GROSS { get; set; }
        public double Width_AXES { get; set; }
        public double Width_NET { get; set; }
        public double Width_GROSS { get; set; }
        // geometry
        public List<Point3D> Geometry { get; set; }
        // material
        public long Material_ID { get; set; }
        // openings
        public List<SurfaceInfo> Openings { get; set; }
        // processing
        public bool Consumed { get; set; }
    }
    #endregion

    public class CompRepDescribes2DorLess : CompRepInfo
    {
        #region STATIC: Synchronization btw lists of CompRep and SurfaceInfo

        protected static string NAME_TMP = "--Surface--";
        /// <summary>
        /// <para>To be called after change in the associated geometry.</para>
        /// </summary>
        internal static void SynchronizeCRListWSurfaceInfoList(Viewport3DXext _comm_manager, CompRepInfo _caller, ref List<CompRepDescribes2DorLess> _cr_list, ref List<SurfaceInfo> _si_list)
        {
            if (_si_list == null) return;
            if (_cr_list == null)
                _cr_list = new List<CompRepDescribes2DorLess>();

            int nrCR = _cr_list.Count;
            if (nrCR > 0)
            {
                // synch existing CompReps w existing Surfaces
                List<bool> cr_updated = Enumerable.Repeat(false, nrCR).ToList();
                for (int i = 0; i < nrCR; i++)
                {
                    CompRepDescribes2DorLess cr_2d = _cr_list[i];
                    Point4D cr_2d_ids = (cr_2d.GR_Relationships.Count > 0) ? cr_2d.GR_Relationships[0].GrIds : new Point4D(-1, -1, -1, -1);
                    SurfaceInfo corresponding_si = _si_list.FirstOrDefault(x => (x.IsWall == cr_2d.IsWall && 
                                                                                 x.VolumeID == cr_2d_ids.X && x.LevelOrLabel == cr_2d_ids.Y && 
                                                                                 x.LabelForOpenings == cr_2d_ids.Z && x.WallLowerPoly == cr_2d_ids.W &&
                                                                                 !x.Consumed));
                    if (corresponding_si != null)
                    {
                        cr_2d.SynchronizeWSurface(ref corresponding_si); // sets corresponding_si.Consumed = true
                        cr_updated[i] = true;
                    }
                    
                }

                // delete CompReps that were not updated
                if (cr_updated.Count(x => x == false) > 0)
                {
                    List<CompRepDescribes2DorLess> cr_list_new = new List<CompRepDescribes2DorLess>();
                    for (int i = 0; i < nrCR; i++)
                    {
                        if (cr_updated[i])
                            cr_list_new.Add(_cr_list[i]);
                    }
                    _cr_list = cr_list_new;
                }
            }

            // add CompReps for new surfaces
            if (_caller != null)
            {
                List<SurfaceInfo> info_2d_unprocessed = _si_list.Where(x => x.Consumed == false).ToList();
                if (info_2d_unprocessed != null && info_2d_unprocessed.Count > 0)
                {
                    foreach (SurfaceInfo si in info_2d_unprocessed)
                    {
                        CompRepDescribes2DorLess cr_2d = new CompRepDescribes2DorLess(_comm_manager, _caller, si);
                        _cr_list.Add(cr_2d);
                    }
                }
            }
        }


        #endregion

        #region PROPERTIES: Overrides

        public override ReadOnlyCollection<long> Comp_RefCompIDs
        {
            get
            {
                List<long> all_ref = new List<long>(this.comp_ref_comp_ids);
                if (this.other_side > -1)
                    all_ref.Add(this.other_side);
                if (this.surface_material_representation > -1)
                    all_ref.Add(this.surface_material_representation);
                return all_ref.AsReadOnly();
            }
        }

        #endregion

        #region PROPERTIES: for cross-referencing with volume surfaces

        public bool IsWall { get; protected set; }
        public long Material_ID { get; internal set; }

        private long other_side;
        private long surface_material_representation; // the CompRep visualised as a Material with Material_ID

        #endregion

        #region .CTOR w/o Component
        // for FUTURE components, hence no association with a ComponentMessage
        public CompRepDescribes2DorLess(Viewport3DXext _manager, CompRepInfo _parent, SurfaceInfo _surface)
            :base(true, _manager)
        {
            // component STRUCTURE
            this.CR_Parent = _parent.CR_ID;
            this.cr_sub_comp_reps = new List<CompRepInfo>(); // for openings

            // component DATA (none, since the component is yet to be created)            
            this.Comp_ID = -1;
            this.Comp_Description = CompRepDescribes2DorLess.NAME_TMP;
            this.comp_ref_comp_ids = new List<long>(); // for the component representing the surface construction (Material)
            this.Comp_Parent = _parent.Comp_ID;

            // connection btw component and GEOMETRY data
            ZonedVolume parent_geometry = null;
            CompRepDescirbes parent_as_descr = _parent as CompRepDescirbes;
            if (parent_as_descr != null)
                parent_geometry = parent_as_descr.Geom_Zone;

            this.GR_State = new Relation2GeomState { Type = Relation2GeomType.DESCRIBES_2DorLESS, IsRealized = (parent_geometry != null) };
            Point4D geom_ids = (parent_geometry == null) ? new Point4D(-1, -1, -1, -1) : new Point4D(parent_geometry.ID, _surface.LevelOrLabel, _surface.LabelForOpenings, _surface.WallLowerPoly);
            GeometricRelationship main = new GeometricRelationship(-1, "Describes 2D surface", this.GR_State, geom_ids, Matrix3D.Identity, Matrix3D.Identity, Matrix3D.Identity);
            this.GR_Relationships = new List<GeometricRelationship>();
            this.GR_Relationships.Add(main);

            // initiate the relevant component PARAMETERS           
            this.comp_param_values = Comp2GeomCommunication.GetReservedParamDictionary(this.GR_State.Type);

            // synchronize parameters, cross-references and display w the geometry
            this.SynchronizeWSurface(ref _surface);
        }
        #endregion

        #region .CTOR from Component
        // for EXISTING components sent by the ComponentBuilder
        public CompRepDescribes2DorLess(ComponentMessage _cmsg, Viewport3DXext _manager)
            :base(_cmsg, _manager)
        {
            // see base for details

            if (this.Comp_Description.Contains("-W- "))
                this.IsWall = true;
            else
                this.IsWall = false; 
        }
        #endregion

        #region METHODS: data transfer from 2d geometry

        /// <summary>
        /// <para>The synchronization requires the presense of parameters with pre-defines reserved names.</para>
        /// <para>The size is NOT transferred to the instances contained in GR_Relationships!</para>
        /// <para>The representations of openings are added as sub-representations and _si is marked as consumed.</para>
        /// <para>A search for the material of the surface and its associated component is performed. The reference to this component is deleted (if not found) or passed on.</para>
        /// </summary>
        /// <param name="_si"></param>
        internal void SynchronizeWSurface(ref SurfaceInfo _si)
        {
            // new description of the component
            string name = (_si.IsWall) ? "-W- " : "-L- ";
            name += _si.LevelOrLabel.ToString();
            if (_si.LabelForOpenings > -1)
                name += " -O- " + _si.LabelForOpenings.ToString();
            if (this.Comp_Description == CompRepDescribes2DorLess.NAME_TMP)
                this.Comp_Description = name;
            
            // cross-referencing
            this.IsWall = _si.IsWall;
            this.Material_ID = _si.Material_ID;   
         
            // geometry (relationship) transfer
            if (this.GR_Relationships.Count > 0)
            {
                this.GR_Relationships[0].SetTypeToRealized(new Point4D(_si.VolumeID, _si.LevelOrLabel, _si.LabelForOpenings, _si.WallLowerPoly));
                this.GR_State = new Relation2GeomState { IsRealized = true, Type = Relation2GeomType.DESCRIBES_2DorLESS };
                this.GR_Relationships[0].PathAsPolygonGeometry(new Point3D(_si.LevelOrLabel, _si.WallLowerPoly, _si.WallUpperPoly), _si.Geometry);
            }

            // DISPLAY
            this.DisplayLabel = _si.DisplayLabel;
            this.DisplayLines = _si.DisplayLines;
            this.DisplayMesh = _si.DisplayMesh;

            // PARAMETERS
            this.comp_param_values[Comp2GeomCommunication.RP_AREA] = _si.Area_AXES;
            this.comp_param_values[Comp2GeomCommunication.RP_AREA_MIN] = _si.Area_NET;
            this.comp_param_values[Comp2GeomCommunication.RP_AREA_MAX] = _si.Area_GROSS;
            this.comp_param_values[Comp2GeomCommunication.RP_WIDTH] = _si.Width_AXES;
            this.comp_param_values[Comp2GeomCommunication.RP_WIDTH_MIN] = _si.Width_NET;
            this.comp_param_values[Comp2GeomCommunication.RP_WIDTH_MAX] = _si.Width_GROSS;
            this.comp_param_values[Comp2GeomCommunication.RP_HEIGHT] = _si.Height_AXES;
            this.comp_param_values[Comp2GeomCommunication.RP_HEIGHT_MIN] = _si.Height_NET;
            this.comp_param_values[Comp2GeomCommunication.RP_HEIGHT_MAX] = _si.Height_GROSS;

            // OPENINGS
            if (_si.Openings != null && _si.Openings.Count > 0)
            {
                List<SurfaceInfo> si_openings = _si.Openings;
                List<CompRepDescribes2DorLess> to_synch = this.cr_sub_comp_reps.Where(x => x is CompRepDescribes2DorLess).Select(x => x as CompRepDescribes2DorLess).ToList();
                CompRepDescribes2DorLess.SynchronizeCRListWSurfaceInfoList(this.comm_manager, this, ref to_synch, ref si_openings);

                // update the sub-CompReps
                this.cr_sub_comp_reps = new List<CompRepInfo>();
                this.cr_sub_comp_reps.AddRange(to_synch);
            }

            // mark the SurfaceInfo
            _si.Consumed = true;

            // set the neighborhood relationship and the reference to the material
            if (this.comm_manager != null)
            {
                // remove the old references
                this.comp_ref_comp_ids.Remove(this.other_side);
                this.comp_ref_comp_ids.Remove(this.surface_material_representation);

                // discover the new references
                this.other_side = this.comm_manager.GetOtherSideOf(this);
                this.surface_material_representation = this.comm_manager.GetCompReferenceFromMaterialID(this.Material_ID);
                if (this.surface_material_representation < 0 && this.Material_ID > -1 && this.Material_ID != 1) // 1 is the ID of the default material
                {
                    // comp rep associated with the material could not be found -> remove the reference
                    long ref_to_remove = this.comm_manager.GetMissingCompReferenceFromMaterialID(this.Material_ID);
                    this.comp_ref_comp_ids.Remove(ref_to_remove);
                    this.comm_manager.UpdateRefsForSingleCompRep(this);
                }
                
                // remove them to avoid duplicates later during the message generation stage
                this.comp_ref_comp_ids.Remove(this.other_side);
                this.comp_ref_comp_ids.Remove(this.surface_material_representation);
            }               
            else
            {
                this.other_side = -1L;
                this.surface_material_representation = -1L;
            }               
        }

        #endregion

        #region METHODS: data transfer from material

        /// <summary>
        /// To be called by the comp rep of the material in a batch reference update.
        /// </summary>
        /// <param name="_comp_id"></param>
        /// <returns>false, if the reference was already set</returns>
        public bool SetMaterialReference(long _comp_id)
        {
            if (this.surface_material_representation == _comp_id)
                return false;
            else
            {
                this.surface_material_representation = _comp_id;
                return true;
            }            
        }

        #endregion

        #region OVERRIDE METHODS

        public override void AdaptDisplay(long _id_cr, long _id_gr_to_exclude)
        {
        }

        public override ComponentMessage ExtractMessage(MessagePositionInSeq _pos)
        {
            MessageAction action = (this.ExtractUpdateMessage) ? MessageAction.UPDATE : MessageAction.DELETE;

            List<long> all_references = new List<long>(this.comp_ref_comp_ids);
            if (this.other_side > -1)
                all_references.Add(this.other_side);
            if (this.surface_material_representation > -1)
                all_references.Add(this.surface_material_representation);

            return new ComponentMessage(_pos, this.Comp_Parent, this.Comp_ID, this.CR_GeneratedAutomatically, this.Comp_Description,
                                              all_references, this.comp_param_values, this.GR_Relationships,
                                              this.CR_ID, this.CR_Parent, action);
        }

        #endregion
    }

    #endregion

    // --------------------------------- DESCRIBES A GROUP OF 3D OBJECTS -------------------------------------- //

    #region TYPE: GROUPS -> TODO

    public class CompRepGroups : CompRepInfo
    {
        public CompRepGroups(bool _automatic, Viewport3DXext _manager, bool _realized)
            :base(_automatic, _manager)
        {
            
        }
    }

    #endregion

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // ================= SPECIALIZED TYPES: REPRESENTATION OF A COMPONENT IN OTHER POSITIONS ================== //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // ------------------------------- DESCRIBES AN ALIGNMENT WITH A SURFACE ---------------------------------- //

    #region TYPE: ALIGNED_WITH -> e.g. Wall Construction (here: Material)

    public class CompRepAlignedWith : CompRepInfo
    {
        #region PROPERTIES: local Material

        // derived: this is used in Material.BoundCR.set
        protected ComponentInteraction.Material wall_constr;
        public ComponentInteraction.Material WallConstr
        {
            get { return this.wall_constr; }
            set
            {
                this.wall_constr = value;
                if (this.wall_constr == null)
                {
                    this.GR_Relationships[0].SetTypeToNotRealized();
                    this.GR_State = new Relation2GeomState { Type = Relation2GeomType.ALIGNED_WITH, IsRealized = false };
                    // send the update to the component builder
                    if (this.comm_manager != null)
                        this.comm_manager.SendDataToCompBuilder(this);
                }
                else
                {
                    this.GR_Relationships[0].SetTypeToRealized(new Point4D(this.wall_constr.ID, -1, -1, -1));
                    this.GR_State = new Relation2GeomState { Type = Relation2GeomType.ALIGNED_WITH, IsRealized = true };
                    // synchronize with the material and send the update to the component builder
                    this.SynchronizeWMaterial(this.wall_constr);
                }

                this.RegisterPropertyChanged("WallConstr");
            }
        }

        #endregion

        public CompRepAlignedWith(ComponentMessage _cmsg, Viewport3DXext _manager)
            :base(_cmsg, _manager)
        {
            // local material
            // NOTE: the '_manager' looks for the referenced material AFTER the constructor (see Viewport3DXext.cs)
            this.wall_constr = null;
        }

        #region METHODS: create new material

        public ComponentInteraction.Material CreateMaterial()
        {
            bool component_params_changed = false;

            string name = this.Comp_Description;
            double d_out = double.NaN;
            double d_in = double.NaN;
            float d = float.NaN;

            if (this.comp_param_values.ContainsKey(Comp2GeomCommunication.RP_MATERIAL_COMPOSITE_D_OUT))
            {
                d_out = this.comp_param_values[Comp2GeomCommunication.RP_MATERIAL_COMPOSITE_D_OUT];
                if (d_out == 0)
                {
                    d_out = ComponentInteraction.Material.DEFAULT_HALF_THICKNESS;
                    this.comp_param_values[Comp2GeomCommunication.RP_MATERIAL_COMPOSITE_D_OUT] = d_out;
                }
            }               
            else
            {
                d_out = ComponentInteraction.Material.DEFAULT_HALF_THICKNESS;
                this.comp_param_values.Add(Comp2GeomCommunication.RP_MATERIAL_COMPOSITE_D_OUT, d_out);
                component_params_changed = true;
            }

            if (this.comp_param_values.ContainsKey(Comp2GeomCommunication.RP_MATERIAL_COMPOSITE_D_IN))
            {
                d_in = this.comp_param_values[Comp2GeomCommunication.RP_MATERIAL_COMPOSITE_D_IN];
                if (d_in == 0)
                {
                    d_in = ComponentInteraction.Material.DEFAULT_HALF_THICKNESS;
                    this.comp_param_values[Comp2GeomCommunication.RP_MATERIAL_COMPOSITE_D_IN] = d_in;
                }
            }
            else
            {
                d_in = ComponentInteraction.Material.DEFAULT_HALF_THICKNESS;
                this.comp_param_values.Add(Comp2GeomCommunication.RP_MATERIAL_COMPOSITE_D_IN, d_in);
                component_params_changed = true;
            }

            d = (float)(d_in + d_out);

            MaterialPosToWallAxisPlane pos = MaterialPosToWallAxisPlane.MIDDLE;
            if (d_in * 10 < d_out)
                pos = MaterialPosToWallAxisPlane.IN;
            else if (d_out * 10 < d_in)
                pos = MaterialPosToWallAxisPlane.OUT;

            // create material
            ComponentInteraction.Material mat = new ComponentInteraction.Material(name, d, pos);
            mat.BoundCR = this;

            // send the update to the component builder
            if (component_params_changed && this.comm_manager != null)
                this.comm_manager.SendDataToCompBuilder(this);

            return mat;
        }

        #endregion

        #region METHODS: data transfer to / from material

        /// <summary>
        /// Transfers the area and nr of associated surfaces from the material to the components and the thickness from the component to the material.
        /// </summary>
        /// <param name="_mat"></param>
        internal void SynchronizeWMaterial(ComponentInteraction.Material _mat)
        {
            if (_mat == null) return;

            // material -> comp rep: Area, NrSurfaces
            if (this.comp_param_values.ContainsKey(Comp2GeomCommunication.RP_AREA))
                this.comp_param_values[Comp2GeomCommunication.RP_AREA] = _mat.AccArea;
            else
                this.comp_param_values.Add(Comp2GeomCommunication.RP_AREA, _mat.AccArea);
            
            if (this.comp_param_values.ContainsKey(Comp2GeomCommunication.RP_COUNT))
                this.comp_param_values[Comp2GeomCommunication.RP_COUNT] = _mat.NrSurfaces;
            else
                this.comp_param_values.Add(Comp2GeomCommunication.RP_COUNT, _mat.NrSurfaces);

            // comp rep -> material : Thickness and Position of axis plane
            _mat.SetOffsets(this, this.comp_param_values[Comp2GeomCommunication.RP_MATERIAL_COMPOSITE_D_OUT],
                                  this.comp_param_values[Comp2GeomCommunication.RP_MATERIAL_COMPOSITE_D_IN]);
            if (_mat.OffsetsChanged && this.comm_manager != null)
            {
                this.comm_manager.PropagateMaterialOffsetChangeToVolumes(_mat);
            }

            // send the update to the component builder
            if (this.comm_manager != null)
            {
                this.comm_manager.UpdateMaterials();
                this.comm_manager.SendDataToCompBuilder(this);
                // updates to the affected surfaces are sent in a deferred manner
                this.comm_manager.PropagateCompReferenceToAllAffectedSurfaceReps(this);
            }
        }

        #endregion

        #region OVERRIDE METHODS: Visualisation of sub-representations

        public override void AdaptDisplay(long _id_cr, long _id_gr_to_exclude)
        {
        }

        #endregion
    }

    #endregion

    // ---------------------------- DESCRIBES A COMPONENT PLACED IN A SPACE ----------------------------------- //

    #region TYPE: CONTAINED_IN -> e.g. Ventilator
    public class CompRepContainedIn : CompRepInfo
    {
        #region STATIC

        internal const double BOX_THICKNESS = 0.025;
        internal static SharpDX.Matrix InterpretAs(Matrix3D _mat)
        {
            return new SharpDX.Matrix(  (float)_mat.M11, (float)_mat.M12, (float)_mat.M13, (float)_mat.M14,
                                        (float)_mat.M21, (float)_mat.M22, (float)_mat.M23, (float)_mat.M24,
                                        (float)_mat.M31, (float)_mat.M32, (float)_mat.M33, (float)_mat.M34,
                                        (float)_mat.OffsetX, (float)_mat.OffsetY, (float)_mat.OffsetZ, (float)_mat.M44);
        }

        internal static void AddBox(ref LineBuilder _lb, Matrix3D _ucs, List<double> _size, double _offset_scale)
        {
            if (_lb == null) return;
            if (_size == null || _size.Count < 3) return;

            List<double> half_size = _size.Select(x => x * 0.5).ToList();

            Point3D origin = new Point3D(_ucs.OffsetX * _offset_scale, _ucs.OffsetY * _offset_scale, _ucs.OffsetZ * _offset_scale);
            Vector3D axisX = new Vector3D(_ucs.M11, _ucs.M12, _ucs.M13);
            Vector3D axisY = new Vector3D(_ucs.M21, _ucs.M22, _ucs.M23);
            Vector3D axisZ = new Vector3D(_ucs.M31, _ucs.M32, _ucs.M33);

            // size[0] = h, size[1] = b, size[2] = L
            Point3D p01 = origin - half_size[2] * axisY - half_size[1] * axisX - half_size[0] * axisZ;
            Point3D p02 = origin - half_size[2] * axisY - half_size[1] * axisX + half_size[0] * axisZ;
            Point3D p03 = origin - half_size[2] * axisY + half_size[1] * axisX + half_size[0] * axisZ;
            Point3D p04 = origin - half_size[2] * axisY + half_size[1] * axisX - half_size[0] * axisZ;

            Point3D p11 = origin + half_size[2] * axisY - half_size[1] * axisX - half_size[0] * axisZ;
            Point3D p12 = origin + half_size[2] * axisY - half_size[1] * axisX + half_size[0] * axisZ;
            Point3D p13 = origin + half_size[2] * axisY + half_size[1] * axisX + half_size[0] * axisZ;
            Point3D p14 = origin + half_size[2] * axisY + half_size[1] * axisX - half_size[0] * axisZ;

            _lb.AddLine(new SharpDX.Vector3((float)p01.X, (float)p01.Y, (float)p01.Z), new SharpDX.Vector3((float)p02.X, (float)p02.Y, (float)p02.Z));
            _lb.AddLine(new SharpDX.Vector3((float)p02.X, (float)p02.Y, (float)p02.Z), new SharpDX.Vector3((float)p03.X, (float)p03.Y, (float)p03.Z));
            _lb.AddLine(new SharpDX.Vector3((float)p03.X, (float)p03.Y, (float)p03.Z), new SharpDX.Vector3((float)p04.X, (float)p04.Y, (float)p04.Z));
            _lb.AddLine(new SharpDX.Vector3((float)p04.X, (float)p04.Y, (float)p04.Z), new SharpDX.Vector3((float)p01.X, (float)p01.Y, (float)p01.Z));

            _lb.AddLine(new SharpDX.Vector3((float)p11.X, (float)p11.Y, (float)p11.Z), new SharpDX.Vector3((float)p12.X, (float)p12.Y, (float)p12.Z));
            _lb.AddLine(new SharpDX.Vector3((float)p12.X, (float)p12.Y, (float)p12.Z), new SharpDX.Vector3((float)p13.X, (float)p13.Y, (float)p13.Z));
            _lb.AddLine(new SharpDX.Vector3((float)p13.X, (float)p13.Y, (float)p13.Z), new SharpDX.Vector3((float)p14.X, (float)p14.Y, (float)p14.Z));
            _lb.AddLine(new SharpDX.Vector3((float)p14.X, (float)p14.Y, (float)p14.Z), new SharpDX.Vector3((float)p11.X, (float)p11.Y, (float)p11.Z));

            _lb.AddLine(new SharpDX.Vector3((float)p01.X, (float)p01.Y, (float)p01.Z), new SharpDX.Vector3((float)p11.X, (float)p11.Y, (float)p11.Z));
            _lb.AddLine(new SharpDX.Vector3((float)p02.X, (float)p02.Y, (float)p02.Z), new SharpDX.Vector3((float)p12.X, (float)p12.Y, (float)p12.Z));
            _lb.AddLine(new SharpDX.Vector3((float)p03.X, (float)p03.Y, (float)p03.Z), new SharpDX.Vector3((float)p13.X, (float)p13.Y, (float)p13.Z));
            _lb.AddLine(new SharpDX.Vector3((float)p04.X, (float)p04.Y, (float)p04.Z), new SharpDX.Vector3((float)p14.X, (float)p14.Y, (float)p14.Z));
        }

        internal static void AddDoubleBox(ref LineBuilder _lb, Matrix3D _ucs, List<double> _size1, double _thickness, double _offset_scale)
        {
            if (_size1 == null) return;
            if (_size1.Count < 3) return;
            List<double> size2 = _size1.Select(x => x - _thickness * 2).ToList();

            CompRepContainedIn.AddBox(ref _lb, _ucs, _size1, _offset_scale);
            CompRepContainedIn.AddBox(ref _lb, _ucs, size2, _offset_scale);
        }

        internal static HelixToolkit.SharpDX.Wpf.MeshGeometry3D ConstructBox(Matrix3D _ucs, List<double> _size, double _offset_scale)
        {
            if (_size == null || _size.Count < 3) return null;

            List<double> half_size = _size.Select(x => x * 0.5).ToList();

            Point3D origin = new Point3D(_ucs.OffsetX * _offset_scale, _ucs.OffsetY * _offset_scale, _ucs.OffsetZ * _offset_scale);
            Vector3D axisX = new Vector3D(_ucs.M11, _ucs.M12, _ucs.M13);
            Vector3D axisY = new Vector3D(_ucs.M21, _ucs.M22, _ucs.M23);
            Vector3D axisZ = new Vector3D(_ucs.M31, _ucs.M32, _ucs.M33);

            Point3D p01 = origin - half_size[2] * axisY - half_size[1] * axisX - half_size[0] * axisZ;
            Point3D p02 = origin - half_size[2] * axisY - half_size[1] * axisX + half_size[0] * axisZ;
            Point3D p03 = origin - half_size[2] * axisY + half_size[1] * axisX + half_size[0] * axisZ;
            Point3D p04 = origin - half_size[2] * axisY + half_size[1] * axisX - half_size[0] * axisZ;

            Point3D p11 = origin + half_size[2] * axisY - half_size[1] * axisX - half_size[0] * axisZ;
            Point3D p12 = origin + half_size[2] * axisY - half_size[1] * axisX + half_size[0] * axisZ;
            Point3D p13 = origin + half_size[2] * axisY + half_size[1] * axisX + half_size[0] * axisZ;
            Point3D p14 = origin + half_size[2] * axisY + half_size[1] * axisX - half_size[0] * axisZ;

            List<Point3D> poly_0 = new List<Point3D> { p01, p04, p03, p02 };
            List<Point3D> poly_1 = new List<Point3D> { p11, p14, p13, p12 };

            HelixToolkit.SharpDX.Wpf.MeshGeometry3D sides =  Utils.MeshesCustom.MeshFrom2Polygons(Utils.CommonExtensions.ConvertPoints3DListToVector3List(poly_0),
                                                                                                  Utils.CommonExtensions.ConvertPoints3DListToVector3List(poly_1));
            HelixToolkit.SharpDX.Wpf.MeshGeometry3D bottom = Utils.MeshesCustom.PolygonFill(Utils.CommonExtensions.ConvertPoints3DListToVector3List(poly_0), true);
            HelixToolkit.SharpDX.Wpf.MeshGeometry3D top = Utils.MeshesCustom.PolygonFill(Utils.CommonExtensions.ConvertPoints3DListToVector3List(poly_1));

            return Utils.MeshesCustom.CombineMeshes(new List<HelixToolkit.SharpDX.Wpf.MeshGeometry3D> { sides, bottom, top });
        }

        #endregion

        #region PROPERTIES: local geometry

        public List<CompRepDescirbes> InstancePlacements { get; protected set; }

        public List<LineGeometry3D> instance_display_lines;
        public List<HelixToolkit.SharpDX.Wpf.MeshGeometry3D> instance_display_mesh;

        // TODO: remove
        protected double placement_scale;
        public double PlacementScale 
        {
            get { return this.placement_scale; }
            set
            {
                this.placement_scale = value;
                this.AdaptDisplayToPlacement();
                this.RegisterPropertyChanged("PlacementScale");
            }
        }

        // if at least one of the instances cannot find the volume it was placed in
        public bool SendUpdateBackAfterCtor { get; internal set; }
        
        #endregion

        public CompRepContainedIn(ComponentMessage _cmsg, Viewport3DXext _manager)
            :base(_cmsg, _manager)
        {
            this.TranslateRefIdsToPlacements();
            this.instance_display_lines = new List<LineGeometry3D>();
            this.instance_display_mesh = new List<HelixToolkit.SharpDX.Wpf.MeshGeometry3D>();

            // generate the visualization out of the size parameters
            this.placement_scale = 1;
            this.SendUpdateBackAfterCtor = false;
            this.AdaptDisplayToPlacement();
            this.DisplayLabel = this.Comp_Description;

            // added 07.09.2017: check if all instances could find their volume
            if (this.SendUpdateBackAfterCtor)
            {
                bool all_instances_unrealised = (this.GR_Relationships.Count(x => x.GrState.IsRealized) == 0);
                this.GR_State = new Relation2GeomState { Type = Relation2GeomType.CONTAINED_IN, IsRealized = !all_instances_unrealised };
                if (this.comm_manager != null)
                    this.comm_manager.SendDataToCompBuilder(this);
                this.SendUpdateBackAfterCtor = false;
            }
        }

        #region METHODS: Info

        internal GeometricRelationship GetInstanceDescription(long _inst_id)
        {
            return this.GR_Relationships.FirstOrDefault(x => x.InstNWeId == _inst_id);
        }

        #endregion

        #region METHODS: manage placements

        private void TranslateRefIdsToPlacements()
        {
            this.InstancePlacements = new List<CompRepDescirbes>();
            if (this.comm_manager == null) return;

            foreach(long id in this.comp_ref_comp_ids)
            {
                CompRepDescirbes container_descriptor = this.comm_manager.GetPlacementContainer(id);
                if (container_descriptor != null)
                    this.InstancePlacements.Add(container_descriptor);
            }
        }

        internal void AddPlacement(ZonedVolume _container)
        {
            if (_container == null) return;
            if (this.comm_manager == null) return;

            CompRepDescirbes container_descriptor = this.comm_manager.GetPlacementContainer(_container.ID);
            if (container_descriptor == null) return;

            CompRepDescirbes duplicate = this.InstancePlacements.FirstOrDefault(x => x.CR_ID == container_descriptor.CR_ID);
            if (duplicate != null) return;

            this.InstancePlacements.Add(container_descriptor);
            this.comp_ref_comp_ids.Add(container_descriptor.Comp_ID);
        }

        internal void RemovePlacement(CompRepDescirbes _former_container)
        {
            if (_former_container == null) return;

            // notify the geometry, if there was ONLY ONE placement left            
            ZonedVolume geom = _former_container.Geom_Zone;
            if (geom != null)
            {
                int nr_placements_left = this.GR_Relationships.Count(x => x.GrIds.X == geom.ID);
                if (nr_placements_left < 1)
                    geom.RemoveCompRepAssociation(this); // notifies _former_container to drop the reference to this component 
            }               

            // notify the components
            this.InstancePlacements.Remove(_former_container);
            this.comp_ref_comp_ids.Remove(_former_container.Comp_ID);
        }

        internal bool HasPlacementIn(ZonedVolume _container)
        {
            if (_container == null) return false;
            if (this.comm_manager == null) return false;

            // check if the volume has a descriptor at all
            CompRepDescirbes container_descriptor = this.comm_manager.GetPlacementContainer(_container.ID);
            if (container_descriptor == null) return false;

            // check if the found descriptor is among the local placements
            CompRepDescirbes found = this.InstancePlacements.FirstOrDefault(x => x.CR_ID == container_descriptor.CR_ID);
            return (found != null);
        }

        /// <summary>
        /// <para>Regenerates the visible geometry NOT from the parameters of the components representation,</para>
        /// <para>but from the size data in its 'geometry relationships'. Those are represented as newly created</para>
        /// <para>sub-representations of type CompRepContainedIn_Instance.</para>
        /// </summary>
        internal void AdaptDisplayToPlacement()
        {            
            // geometric display
            this.instance_display_lines = new List<LineGeometry3D>();
            this.instance_display_mesh = new List<HelixToolkit.SharpDX.Wpf.MeshGeometry3D>();

            LineBuilder lb_all = new LineBuilder();
            List<HelixToolkit.SharpDX.Wpf.MeshGeometry3D> boxes = new List<HelixToolkit.SharpDX.Wpf.MeshGeometry3D>();
            
            foreach(GeometricRelationship gr in this.GR_Relationships)
            {
                // OUTER LINES: instance, ALL instances
                LineBuilder lb_instance = new LineBuilder();
                if (gr.GrIds.X < 0)
                {
                    CompRepContainedIn.AddBox(ref lb_instance, gr.GrUCS, gr.GetSizeInfo(true), this.PlacementScale);
                    CompRepContainedIn.AddBox(ref lb_all, gr.GrUCS, gr.GetSizeInfo(true), this.PlacementScale);
                }                    
                else
                {
                    CompRepContainedIn.AddDoubleBox(ref lb_instance, gr.GrUCS, gr.GetSizeInfo(true), CompRepContainedIn.BOX_THICKNESS, this.PlacementScale);
                    CompRepContainedIn.AddDoubleBox(ref lb_all, gr.GrUCS, gr.GetSizeInfo(true), CompRepContainedIn.BOX_THICKNESS, this.PlacementScale);
                }                   
                this.instance_display_lines.Add(lb_instance.ToLineGeometry3D());

                // INNER MESH: instance
                HelixToolkit.SharpDX.Wpf.MeshGeometry3D box_instance = 
                    CompRepContainedIn.ConstructBox(gr.GrUCS, gr.GetSizeInfo(false), this.PlacementScale);
                this.instance_display_mesh.Add(box_instance);
                // INNER MESH: ALL instances
                boxes.Add(box_instance);               
            }

            this.DisplayLines = lb_all.ToLineGeometry3D();
            this.DisplayMesh = Utils.MeshesCustom.CombineMeshes(boxes);

            // treeview display
            this.cr_sub_comp_reps = new List<CompRepInfo>();
            for (int i = 0; i < this.GR_Relationships.Count; i++ )
            {
                GeometricRelationship gr_i = this.GR_Relationships[i];
                CompRepContainedIn_Instance child_i = new CompRepContainedIn_Instance(this.comm_manager, this, gr_i,
                                                                this.instance_display_lines[i], this.instance_display_mesh[i]);
                child_i.PropertyChanged += child_i_PropertyChanged;
                this.cr_sub_comp_reps.Add(child_i);
            }

            this.RegisterPropertyChanged("AdaptedDisplayToPlacement");
        }

        /// <summary>
        /// <para>To be called by a containing representation (e.g. of a GT system) to show the entire system.</para>
        /// </summary>
        internal override void GetDisplayables(ref LineBuilder _lb_all , ref LineBuilder _lb_all_2, ref List<HelixToolkit.SharpDX.Wpf.MeshGeometry3D> _boxes_all, long _id_to_exclude)
        {
            if (_lb_all == null || _boxes_all == null) return;

            foreach (GeometricRelationship gr in this.GR_Relationships)
            {
                if (gr.GrID == _id_to_exclude)
                    continue;
                // OUTER LINES
                if (gr.GrIds.X < 0)
                    CompRepContainedIn.AddBox(ref _lb_all, gr.GrUCS, gr.GetSizeInfo(true), this.PlacementScale);
                else
                    CompRepContainedIn.AddDoubleBox(ref _lb_all, gr.GrUCS, gr.GetSizeInfo(true), CompRepContainedIn.BOX_THICKNESS, this.PlacementScale);

                // INNER MESH
                HelixToolkit.SharpDX.Wpf.MeshGeometry3D box_instance =
                    CompRepContainedIn.ConstructBox(gr.GrUCS, gr.GetSizeInfo(false), this.PlacementScale);
                _boxes_all.Add(box_instance);
            }
        }

        public override void AdaptDisplay(long _id_cr, long _id_gr_to_exclude)
        {
        }

        /// <summary>
        /// Adapts the graphical representation after movement in one of the instances.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void child_i_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            CompRepContainedIn_Instance child = sender as CompRepContainedIn_Instance;
            if (child == null || e == null) return;

            if (e.PropertyName == "Moved")
            {
                //// adopt to local change (necessary ONLY if the child contains a COPY of the geometric relationship)
                //int index = this.GR_Relationships.FindIndex(x => x.GrIds.X == child.GR_Relationships[0].GrIds.X);
                //if (index > -1)
                //{
                //    this.GR_Relationships[index] = child.GR_Relationships[0];
                //}

                // update display
                this.AdaptDisplayToPlacement();

                // communicate
                if (this.comm_manager != null)
                    this.comm_manager.SendDataToCompBuilder(this);
            }
        }

        #endregion

        #region OVERRIDES: SubcompReps

        /// <summary>
        /// <para>Does NOT return any sub-representations as they have a function to show individual instances ONLY.</para>
        /// <para>They are NOT transferred to the calling module. The information resides in their list of 'geometric relationships' instead.</para>
        /// </summary>
        /// <returns></returns>
        internal override List<CompRepInfo> GetFlatListOfSubCompReps()
        {
            return new List<CompRepInfo>();
        }

        #endregion

    }

    #endregion

    #region TYPE: CONTAINED_IN_INSTANCE -> e.g. a single placement of a Ventilator (not visible in ComponentBuilder)

    public class CompRepContainedIn_Instance : CompRepInfo
    {
        public CompRepContainedIn Parent { get; protected set; }
        public long ConnectionID { get; protected set; }
        public bool IsPlaced { get { return (this.GR_Relationships[0].GrIds.X > -1); } }

        #region .CTOR w/o Component
        
        public CompRepContainedIn_Instance(Viewport3DXext _manager, CompRepContainedIn _parent, GeometricRelationship _instance_info, 
                                            LineGeometry3D _lines, HelixToolkit.SharpDX.Wpf.MeshGeometry3D _mesh)
            :base(true, _manager)
        {
            // component STRUCTURE
            this.CR_Parent = _parent.CR_ID;
            this.Parent = _parent;
            this.cr_sub_comp_reps = new List<CompRepInfo>(); // should remain empty

            // component DATA (none, since the component is NOT to be created)            
            this.Comp_ID = -1;
            this.Comp_Description = "Instance " + _instance_info.InstNWeName;
            this.comp_ref_comp_ids = new List<long>(); // should remain empty
            this.Comp_Parent = _parent.Comp_ID;           

            // connection btw INSTANCE and GEOMETRY data
            this.GR_State = new Relation2GeomState { Type = Relation2GeomType.CONTAINED_IN, IsRealized = _instance_info.GrState.IsRealized };
            Point4D geom_ids = _instance_info.GrIds;           

            GeometricRelationship main = _instance_info; // new GeometricRelationship(_instance_info); // copying de-couples from parent and dependent connections!!!
            this.GR_Relationships = new List<GeometricRelationship>();
            this.GR_Relationships.Add(main);

            // initiate the relevant INSTANCE PARAMETERS           
            this.comp_param_values = new Dictionary<string, double>();

            // display
            this.DisplayLines = _lines;
            this.DisplayMesh = _mesh;
            this.DisplayLabel = this.Comp_Description;

            // connectivity
            this.ConnectionID = _instance_info.InstNWeId;

            // added 07.09.2017: check if the referenced volume exists
            if (geom_ids.X >= 0)
            {
                ZonedVolume placement_vol= null;
                if (this.comm_manager != null)
                    placement_vol = this.comm_manager.GetPlacementVolume((long)geom_ids.X);
                if (placement_vol == null)
                {
                    this.GR_Relationships[0].SetTypeToNotRealized();
                    this.Parent.SendUpdateBackAfterCtor = true;
                }
            }
        }
        #endregion

        #region METHODS: placement

        /// <summary>
        /// <para>The local coordinate system of the space volume is used as a placement reference.</para>
        /// <para>The placement creates a new 'geometric relationship' (i.e. a new instance) or populates the existing one,</para>
        /// <para>if this is the very first placement. The state of the representation is set to 'IsRealized = true'. Multiple placements in the same volume are allowed.</para>
        /// </summary>
        /// <param name="_geom"></param>
        public void PlaceIn(ZonedVolume _geom)
        {
            if (_geom == null) return;

            // allow duplicate placements (e.g. air vents)
            //bool duplicate_placement = this.Parent.HasPlacementIn(_geom);
            //if (duplicate_placement) return;

            // extract the transforms from the local geometry
            SharpDX.Vector3 pivot = _geom.Volume_Pivot;
            List<SharpDX.Vector3> geom_axes = _geom.GetAxes(); // x, y, z
            Point3D pivot_P3D = new Point3D(pivot.X, pivot.Y, pivot.Z);
            Vector3D axis_X_V3D = new Vector3D(geom_axes[0].X, geom_axes[0].Y, geom_axes[0].Z);
            Vector3D axis_Z_V3D = new Vector3D(geom_axes[1].X, geom_axes[1].Y, geom_axes[1].Z); // swapped 30.08.2017
            Vector3D axis_Y_V3D = new Vector3D(geom_axes[2].X, geom_axes[2].Y, geom_axes[2].Z); // swapped 30.08.2017

            Matrix3D geom_ucs = GeometricTransformations.PackUCS(pivot_P3D, axis_X_V3D, axis_Y_V3D, axis_Z_V3D);
            Matrix3D geom_trWC2LC = GeometricTransformations.GetTransformWC2LC(pivot_P3D, axis_X_V3D, axis_Y_V3D, axis_Z_V3D);
            Matrix3D geom_trLC2WC = GeometricTransformations.GetTransformLC2WC(pivot_P3D, axis_X_V3D, axis_Y_V3D, axis_Z_V3D); // to be changed!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            Point4D geom_ids = new Point4D(_geom.ID, -1, -1, -1); // TMP -> to refine later

            this.GR_Relationships[0].SetTypeToRealized(geom_ucs, geom_trWC2LC, geom_trLC2WC, geom_ids);

            // notify the parent geometry
            _geom.AddCompRepAssociation(this.Parent);
            this.Parent.AddPlacement(_geom);
            this.Parent.AdaptDisplayToPlacement();

            // communicate via the parent (the instance has NO corresponding component!)
            if (this.comm_manager != null)
                this.comm_manager.SendDataToCompBuilder(this.Parent);
        }

        /// <summary>
        /// <para>Removes only the association with geometry. Does not affect the instance in the scheme.</para>
        /// <para>The instance can be completely removed only in the Component Builder module.</para>
        /// </summary>
        public void RemovePlacement()
        {
            if (this.comm_manager == null) return;

            // identify the container
            long vol_id = (long)this.GR_Relationships[0].GrIds.X;
            CompRepDescirbes container = this.comm_manager.GetPlacementContainer(vol_id);
            if (container == null) return;

            // remove the corresponding 'relationship to geometry'
            this.GR_Relationships[0].SetTypeToNotRealized();

            // notify the container (it notifies the geometry)
            this.Parent.RemovePlacement(container);
            this.Parent.AdaptDisplayToPlacement();

            // communicate via the parent (the instance has NO corresponding component!)
            if (this.comm_manager != null)
                this.comm_manager.SendDataToCompBuilder(this.Parent);
        }

        public void AlignPlacement(Vector3D _new_y_axis)
        {
            // unpack old coordinate system
            Point3D pivot_P3D = GeometricTransformations.UnpackOrigin(this.GR_Relationships[0].GrUCS);
            Vector3D axis_X_V3D = GeometricTransformations.UnpackXAxis(this.GR_Relationships[0].GrUCS);
            Vector3D axis_Z_V3D = GeometricTransformations.UnpackZAxis(this.GR_Relationships[0].GrUCS);
            Vector3D axis_Y_V3D = GeometricTransformations.UnpackYAxis(this.GR_Relationships[0].GrUCS);

            // replace
            axis_Y_V3D = _new_y_axis;
            axis_X_V3D = Vector3D.CrossProduct(axis_Z_V3D, axis_Y_V3D);

            // update
            Matrix3D geom_ucs = GeometricTransformations.PackUCS(pivot_P3D, axis_X_V3D, axis_Y_V3D, axis_Z_V3D);
            Matrix3D geom_trWC2LC = GeometricTransformations.GetTransformWC2LC(pivot_P3D, axis_X_V3D, axis_Y_V3D, axis_Z_V3D);
            Matrix3D geom_trLC2WC = GeometricTransformations.GetTransformLC2WC(pivot_P3D, axis_X_V3D, axis_Y_V3D, axis_Z_V3D);
            Point4D geom_ids = this.GR_Relationships[0].GrIds;

            this.GR_Relationships[0].SetTypeToRealized(geom_ucs, geom_trWC2LC, geom_trLC2WC, geom_ids);

            // communicate via the parent (the instance has NO corresponding component!)
            if (this.comm_manager != null)
                this.comm_manager.SendDataToCompBuilder(this.Parent);
        }

        #endregion


        #region OVERRIDE METHODS: Visualisation of sub-representations

        public override void AdaptDisplay(long _id_cr, long _id_gr_to_exclude)
        {
            // OUTER LINES
            LineBuilder lb = new LineBuilder();
            if (this.IsPlaced)
                CompRepContainedIn.AddDoubleBox(ref lb, this.GR_Relationships[0].GrUCS, this.GR_Relationships[0].GetSizeInfo(true), CompRepContainedIn.BOX_THICKNESS, this.Parent.PlacementScale);
            else
                CompRepContainedIn.AddBox(ref lb, this.GR_Relationships[0].GrUCS, this.GR_Relationships[0].GetSizeInfo(true), this.Parent.PlacementScale);
            this.DisplayLines = lb.ToLineGeometry3D();

            // INNER MESH
            HelixToolkit.SharpDX.Wpf.MeshGeometry3D box_instance =
                CompRepContainedIn.ConstructBox(this.GR_Relationships[0].GrUCS, this.GR_Relationships[0].GetSizeInfo(false), this.Parent.PlacementScale);
            this.DisplayMesh = box_instance;
        }

        #endregion

        #region METHOD: Axis-aligned translation 

        public void MoveAlongX(double _amount)
        {
            this.GR_Relationships[0].MoveAlongUCSx(_amount);
            this.RegisterPropertyChanged("Moved");
        }

        public void MoveAlongY(double _amount)
        {
            this.GR_Relationships[0].MoveAlongUCSy(_amount);
            this.RegisterPropertyChanged("Moved");
        }

        public void MoveAlongZ(double _amount)
        {
            this.GR_Relationships[0].MoveAlongUCSz(_amount);
            this.RegisterPropertyChanged("Moved");
        }

        #endregion

        #region OVERRIDES

        public override ComponentMessage ExtractMessage(MessagePositionInSeq _pos)
        {
            return null;
        }

        internal override List<CompRepInfo> GetFlatListOfSubCompReps()
        {
            return new List<CompRepInfo>();
        }

        #endregion
    }

    #endregion

    // ---------------------------- DESCRIBES A COMPONENT CONNECTING TWO OTHERS ------------------------------- //

    #region TYPE: CONNECTS -> e.g. Pipe

    public class CompRepConnects : CompRepInfo
    {
        #region STATIC

        internal static void AddPolyline(ref LineBuilder _lb, List<Point3D> _vertices_in_order, Vector3D _offset_start, Vector3D _offset_end)
        {
            if (_lb == null) return;
            if (_vertices_in_order == null || _vertices_in_order.Count < 3) return;

            // skip first entry: it contains connectivity info, NOT an actual vertex
            for(int i = 1; i < _vertices_in_order.Count - 1; i++)
            {
                Point3D p0 = _vertices_in_order[i];
                if (i == 1)
                    p0 = _vertices_in_order[i] + _offset_start;

                Point3D p1 = _vertices_in_order[i + 1];
                if (i == _vertices_in_order.Count - 2)
                    p1 = _vertices_in_order[i + 1] + _offset_end;

                _lb.AddLine(new SharpDX.Vector3((float)p0.X, (float)p0.Y, (float)p0.Z), new SharpDX.Vector3((float)p1.X, (float)p1.Y, (float)p1.Z));
            }
        }

        internal static void AddOffsetPolyline(ref LineBuilder _lb, List<Point3D> _central_line, double _hrz_size, double _vert_size)
        {
            // assumption: up = Y-axis
            if (_lb == null) return;
            if (_central_line == null || _central_line.Count < 3) return;

            Vector3D upV = new Vector3D(0, 1, 0);

            // skip first entry: it contains connectivity info, NOT an actual vertex
            List<Utils.PolyLineOffsetContainer> offset_infos = new List<Utils.PolyLineOffsetContainer>();
            for (int i = 1; i < _central_line.Count - 1; i++)
            {
                Utils.PolyLineOffsetContainer info = Utils.PolyLineOffsetContainer.CreateOutOf(_central_line[i], _central_line[i + 1], upV, _hrz_size, _vert_size);
                if (info != null)
                    offset_infos.Add(info);
            }

            // process the invalid entries
            bool success_1 = Utils.PolyLineOffsetContainer.HandleInvalidEntries(ref offset_infos, _hrz_size, _vert_size);

            // find the intersection points
            bool success_2 = Utils.PolyLineOffsetContainer.ConnectSequence(ref offset_infos);

            // regardless of success of the operations above, display
            for (int i = 0; i < offset_infos.Count; i++ )
            {
                Utils.PolyLineOffsetContainer entry = offset_infos[i];
                _lb.AddLine(entry.OffsetLine_A_P0, entry.OffsetLine_A_P1);
                _lb.AddLine(entry.OffsetLine_B_P0, entry.OffsetLine_B_P1);
                _lb.AddLine(entry.OffsetLine_C_P0, entry.OffsetLine_C_P1);
                _lb.AddLine(entry.OffsetLine_D_P0, entry.OffsetLine_D_P1);

                if (i == 0)
                {
                    _lb.AddLine(entry.OffsetLine_A_P0, entry.OffsetLine_B_P0);
                    _lb.AddLine(entry.OffsetLine_B_P0, entry.OffsetLine_C_P0);
                    _lb.AddLine(entry.OffsetLine_C_P0, entry.OffsetLine_D_P0);
                    _lb.AddLine(entry.OffsetLine_D_P0, entry.OffsetLine_A_P0);
                }

                _lb.AddLine(entry.OffsetLine_A_P1, entry.OffsetLine_B_P1);
                _lb.AddLine(entry.OffsetLine_B_P1, entry.OffsetLine_C_P1);
                _lb.AddLine(entry.OffsetLine_C_P1, entry.OffsetLine_D_P1);
                _lb.AddLine(entry.OffsetLine_D_P1, entry.OffsetLine_A_P1);
            }
        }

        #endregion

        #region CLASS MEMBERS

        public List<LineGeometry3D> instance_display_lines;
        public List<LineGeometry3D> instance_display_lines_2;

        protected List<GeometricRelationship> instance_start_points;
        protected List<GeometricRelationship> instance_end_points;
        protected List<CompRepContainedIn> influencing_reps;

        #endregion

        public CompRepConnects(ComponentMessage _cmsg, Viewport3DXext _manager)
            : base(_cmsg, _manager)
        {
            this.instance_display_lines = new List<LineGeometry3D>();
            this.instance_display_lines_2 = new List<LineGeometry3D>();

            this.instance_start_points = new List<GeometricRelationship>();
            this.instance_end_points = new List<GeometricRelationship>();
            this.influencing_reps = new List<CompRepContainedIn>();

            this.AdaptDisplayToPlacement();
            this.DisplayLabel = this.Comp_Description;
        }

        #region METHODS: manage placements

        /// <summary>
        /// <para>To be called by the managing class for re-establishing connectivity.</para>
        /// <para>Once done, the resulting lists do not change to the end of the session</para>
        /// <para>because no new instances can be defined, they can only be placed. No deletion is possible either.</para>
        /// </summary>
        /// <param name="_point_types"></param>
        internal void RetrieveConnectionPoints(List<CompRepContainedIn> _point_types)
        {
            if (_point_types == null) return;
            if (_point_types.Count == 0) return;

            this.instance_start_points = new List<GeometricRelationship>();
            this.instance_end_points = new List<GeometricRelationship>();

            foreach(GeometricRelationship instance in this.GR_Relationships)
            {
                long start_point_id = -1;
                long end_point_id = -1;
                if (instance.InstPath != null && instance.InstPath.Count > 0)
                {
                    start_point_id = (long)instance.InstPath[0].X;
                    end_point_id = (long)instance.InstPath[0].Y;
                }

                if (start_point_id == -1 || end_point_id == -1)
                {
                    this.instance_start_points.Add(null);
                    this.instance_end_points.Add(null);
                    continue;
                }

                GeometricRelationship start = null;
                GeometricRelationship end = null;
                foreach(CompRepContainedIn p_t in _point_types)
                {
                    if (start == null)
                    {
                        start = p_t.GR_Relationships.FirstOrDefault(x => x.InstNWeId == start_point_id);
                        if (start != null)
                        {
                            p_t.PropertyChanged += point_PropertyChanged;
                            this.influencing_reps.Add(p_t);
                        }
                    }                        

                    if (end == null)
                    {
                        end = p_t.GR_Relationships.FirstOrDefault(x => x.InstNWeId == end_point_id);
                        if (end != null)
                        {
                            p_t.PropertyChanged += point_PropertyChanged;
                            this.influencing_reps.Add(p_t);
                        }
                    }
                        
                    if (start != null && end != null)
                        break;
                }
                this.instance_start_points.Add(start);
                this.instance_end_points.Add(end);
            }

            this.AdaptDisplayToPlacement();
        }

        

        protected void AdaptDisplayToPlacement()
        {
            if (this.GR_Relationships.Count != this.instance_start_points.Count || 
                this.GR_Relationships.Count != this.instance_end_points.Count) 
                return;

            // geometric display
            this.instance_display_lines = new List<LineGeometry3D>();
            
            LineBuilder lb_all = new LineBuilder();
            LineBuilder lb_all_2 = new LineBuilder();

            for (int i = 0; i < this.GR_Relationships.Count; i++ )
            {
                GeometricRelationship gr = this.GR_Relationships[i];

                // adjust start and end
                Vector3D offset_start = new Vector3D(0, 0, 0); // new Vector3D(this.instance_start_points[i].InstSize[0], this.instance_start_points[i].InstSize[1], this.instance_start_points[i].InstSize[2]);
                Vector3D offset_end = new Vector3D(0, 0, 0);

                // update path with connection information
                gr.PathConnectivityUpate(this.instance_start_points[i].GetOriginUCS(),
                                         this.instance_end_points[i].GetOriginUCS());
                List<Point3D> path = gr.InstPath.ToList();

                // update state with state of connecting points
                if (this.instance_start_points[i].GrState.IsRealized && this.instance_end_points[i].GrState.IsRealized)
                    gr.SetTypeToRealized(new Point4D(-2, -1, -1, -1));
                else
                    gr.SetTypeToNotRealized();

                // CENRTAL LINES: instance
                LineBuilder lb_instance = new LineBuilder();
                LineBuilder lb_instance_2 = new LineBuilder();
                CompRepConnects.AddPolyline(ref lb_instance, path, offset_start, offset_end);
                CompRepConnects.AddOffsetPolyline(ref lb_instance_2, path, gr.InstSize[1], gr.InstSize[0]);
                this.instance_display_lines.Add(lb_instance.ToLineGeometry3D());
                this.instance_display_lines_2.Add(lb_instance_2.ToLineGeometry3D());

                // CENTRAL LINES: ALL instances
                CompRepConnects.AddPolyline(ref lb_all, path, offset_start, offset_end);
                CompRepConnects.AddOffsetPolyline(ref lb_all_2, path, gr.InstSize[1], gr.InstSize[0]);
            }

            this.DisplayLines = lb_all.ToLineGeometry3D();
            this.DisplayLinesSecondary = lb_all_2.ToLineGeometry3D();

            // treeview display
            this.cr_sub_comp_reps = new List<CompRepInfo>();
            for (int i = 0; i < this.GR_Relationships.Count; i++)
            {
                GeometricRelationship gr_i = this.GR_Relationships[i];
                CompRepConnects_Instance child_i = new CompRepConnects_Instance(this.comm_manager, this, gr_i, this.instance_display_lines[i], this.instance_display_lines_2[i]);
                child_i.PropertyChanged += child_i_PropertyChanged;
                this.cr_sub_comp_reps.Add(child_i);
            }
        }

        /// <summary>
        /// <para>To be called by a containing representation (e.g. of a GT system) to show the entire system.</para>
        /// </summary>
        internal override void GetDisplayables(ref LineBuilder _lb_all, ref LineBuilder _lb_all_2, ref List<HelixToolkit.SharpDX.Wpf.MeshGeometry3D> _boxes_all, long _id_to_exclude)
        {
            if (_lb_all == null) return;

            for (int i = 0; i < this.GR_Relationships.Count; i++)
            {
                GeometricRelationship gr = this.GR_Relationships[i];
                if (gr.GrID == _id_to_exclude)
                    continue;

                Vector3D offset_start = new Vector3D(0, 0, 0); // new Vector3D(this.instance_start_points[i].InstSize[0], this.instance_start_points[i].InstSize[1], this.instance_start_points[i].InstSize[2]);
                Vector3D offset_end = new Vector3D(0, 0, 0);

                // update path with connection information
                gr.PathConnectivityUpate(this.instance_start_points[i].GetOriginUCS(),
                                         this.instance_end_points[i].GetOriginUCS());
                List<Point3D> path = gr.InstPath.ToList();

                // CENTRAL LINES
                CompRepConnects.AddPolyline(ref _lb_all, path, offset_start, offset_end);
                CompRepConnects.AddOffsetPolyline(ref _lb_all_2, path, gr.InstSize[1], gr.InstSize[0]);
            }
        }

        public override void AdaptDisplay(long _id_cr, long _id_gr_to_exclude)
        {
        }

        protected void child_i_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            CompRepConnects_Instance child = sender as CompRepConnects_Instance;
            if (child == null || e == null) return;

            if (e.PropertyName == "PathChanged")
            {
                // update display
                this.AdaptDisplayToPlacement();

                // communicate
                if (this.comm_manager != null)
                    this.comm_manager.SendDataToCompBuilder(this);
            }
        }

        protected void point_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            CompRepContainedIn influencing_point = sender as CompRepContainedIn;
            if (influencing_point == null || e == null) return;

            if (e.PropertyName == "AdaptedDisplayToPlacement")
            {
                // update display
                this.AdaptDisplayToPlacement();

                // communicate
                if (this.comm_manager != null)
                    this.comm_manager.SendDataToCompBuilder(this);
            }
        }

        #endregion

        #region OVERRIDES: SubcompReps

        /// <summary>
        /// <para>Does NOT return any sub-representations as they have a function to show individual instances ONLY.</para>
        /// <para>They are NOT transferred to the calling module. The information resides in their list of 'geometric relationships' instead.</para>
        /// </summary>
        /// <returns></returns>
        internal override List<CompRepInfo> GetFlatListOfSubCompReps()
        {
            return new List<CompRepInfo>();
        }

        #endregion
    }

    #endregion

    #region TYPE : CONNECTS_IN_SUB -> e.g. a single placement of a Pipe (not visible in ComponentBuilder)

    public class CompRepConnects_Instance : CompRepInfo
    {

        #region .CTOR w/o Component

        public CompRepConnects_Instance(Viewport3DXext _manager, CompRepConnects _parent, GeometricRelationship _instance_info, LineGeometry3D _lines, LineGeometry3D _lines_2)
            :base(true, _manager)
        {
            // component STRUCTURE
            this.CR_Parent = _parent.CR_ID;
            this.cr_sub_comp_reps = new List<CompRepInfo>(); // should remain empty

            // component DATA (none, since the component is NOT to be created)            
            this.Comp_ID = -1;
            this.Comp_Description = "Instance " + _instance_info.InstNWeName;
            this.comp_ref_comp_ids = new List<long>(); // should remain empty
            this.Comp_Parent = _parent.Comp_ID;

            // connection btw INSTANCE and GEOMETRY data
            this.GR_State = new Relation2GeomState { Type = Relation2GeomType.CONNECTS, IsRealized = _instance_info.GrState.IsRealized };
            Point4D geom_ids = _instance_info.GrIds;
            GeometricRelationship main = _instance_info; // new GeometricRelationship(_instance_info); // copying de-couples from parent and dependent connections!!!
            this.GR_Relationships = new List<GeometricRelationship>();
            this.GR_Relationships.Add(main);

            // initiate the relevant INSTANCE PARAMETERS           
            this.comp_param_values = new Dictionary<string, double>();

            // display
            this.DisplayLines = _lines;
            this.DisplayLinesSecondary = _lines_2;
            this.DisplayMesh = null;
            this.DisplayLabel = this.Comp_Description;
        }

        #endregion

        #region OVERRIDE METHODS: Visualisation of sub-representations

        public override void AdaptDisplay(long _id_cr, long _id_gr_to_exclude)
        {
            // adapt display
            LineBuilder lb = new LineBuilder();
            CompRepConnects.AddPolyline(ref lb, this.GR_Relationships[0].InstPath.ToList(),
                                            new Vector3D(0, 0, 0), new Vector3D(0, 0, 0));
            this.DisplayLines = lb.ToLineGeometry3D();

            LineBuilder lb_2 = new LineBuilder();
            CompRepConnects.AddOffsetPolyline(ref lb_2, this.GR_Relationships[0].InstPath.ToList(),
                                              this.GR_Relationships[0].InstSize[1], this.GR_Relationships[0].InstSize[0]);
            this.DisplayLinesSecondary = lb_2.ToLineGeometry3D();
        }

        #endregion

        #region OVERRIDES

        public override ComponentMessage ExtractMessage(MessagePositionInSeq _pos)
        {
            return null;
        }

        internal override List<CompRepInfo> GetFlatListOfSubCompReps()
        {
            return new List<CompRepInfo>();
        }

        #endregion

        #region METHODS: Path Editing

        public void TransferPathChange(List<Point3D> _internal_points)
        {
            if (this.GR_Relationships != null && this.GR_Relationships.Count > 0)
            {
                // communicate to the component represented
                this.GR_Relationships[0].PathTransfer(_internal_points);
                // adapt display
                LineBuilder lb = new LineBuilder();
                CompRepConnects.AddPolyline(ref lb, this.GR_Relationships[0].InstPath.ToList(),
                                                new Vector3D(0, 0, 0), new Vector3D(0, 0, 0));
                this.DisplayLines = lb.ToLineGeometry3D();
                
                LineBuilder lb_2 = new LineBuilder();
                CompRepConnects.AddOffsetPolyline(ref lb_2, this.GR_Relationships[0].InstPath.ToList(),
                                                  this.GR_Relationships[0].InstSize[1], this.GR_Relationships[0].InstSize[0]);
                this.DisplayLinesSecondary = lb_2.ToLineGeometry3D();
                // communicate to parent
                this.RegisterPropertyChanged("PathChanged");
            }            
        }

        #endregion
    }


    #endregion


    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // ============================================== UTILITIES =============================================== //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region VALUE CONVERTER

    [ValueConversion(typeof(System.Collections.IList), typeof(ListCollectionView))]
    public class TreeViewSortingCompRepConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            System.Collections.IList collection = value as System.Collections.IList;
            if (collection == null)
                return null;

            ListCollectionView view = new ListCollectionView(collection);
            view.CustomSort = new CompRepComparer();
            return view;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

    }

    // WORKS ONLY ON FLAT COLLECTIONS
    [ValueConversion(typeof(System.Collections.IList), typeof(ListCollectionView))]
    public class TreeViewFilterAccCompRepInfo : IValueConverter
    {
        private string filter_parameter;
        private bool FilterMethod(object _item)
        {
            if (_item == null) return false;
            if (string.IsNullOrEmpty(this.filter_parameter)) return false;

            ComponentReps.CompRepInfo cr = _item as ComponentReps.CompRepInfo;
            if (cr == null) return false;

            InterProcCommunication.Specific.Relation2GeomType type = cr.GR_State.Type;
            string type_str = InterProcCommunication.Specific.GeometryUtils.Relationship2GeometryToString(type);

            // filter
            string[] filter_components = this.filter_parameter.Split(new char[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string f in filter_components)
            {
                if (type_str == f)
                    return true;
            }

            return false;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            if (parameter == null) return value;

            // retrieve collection to sort
            System.Collections.IList collection = value as System.Collections.IList;
            if (collection == null || collection.Count < 2) return value;

            // retrieve property name according to which to filter
            string param_as_str = parameter.ToString();
            if (string.IsNullOrEmpty(param_as_str)) return value;

            // perform the filtering and sorting
            this.filter_parameter = param_as_str;
            ListCollectionView view = new ListCollectionView(collection);
            view.Filter = this.FilterMethod;
            view.CustomSort = new CompRepComparer();

            return view;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }

    [ValueConversion(typeof(Relation2GeomType), typeof(System.Windows.Media.Color))]
    public class Relation2GeomTypeToWinMediaColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            System.Windows.Media.Color col = (System.Windows.Media.Color)ColorConverter.ConvertFromString("Black");
            if (value == null)
                return col;

            if (value is Relation2GeomType)
            {
                Relation2GeomType r2gt = (Relation2GeomType)value;
                switch (r2gt)
                {
                    case Relation2GeomType.NONE:
                        col = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#ff666666");
                        break;                    
                    case Relation2GeomType.DESCRIBES:
                    case Relation2GeomType.GROUPS:
                        col = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#ff000000");
                        break;
                    case Relation2GeomType.DESCRIBES_3D:
                    case Relation2GeomType.DESCRIBES_2DorLESS:
                        col = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#ff440000");
                        break;
                    case Relation2GeomType.ALIGNED_WITH:
                        col = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#ff0000ff");
                        break;
                    case Relation2GeomType.CONTAINED_IN:
                    case Relation2GeomType.CONNECTS:
                        col = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#ff88004b");
                        break;
                    default:
                        break;
                }
            }
            return col;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }

    [ValueConversion(typeof(Relation2GeomType), typeof(Boolean))]
    public class Relation2GeomTypeToBooleanConverter : IValueConverter
    {
        // in order to react to more than one type mode at once
        // use + as OR
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            Relation2GeomType r2gt = Relation2GeomType.NONE;
            if (value is Relation2GeomType)
                r2gt = (Relation2GeomType)value;

            string str_param = parameter.ToString();
            string[] str_params_OR = str_param.Split(new char[] { '+' });

            if (str_params_OR.Count() < 2)
                return (r2gt == GeometryUtils.StringToRelationship2Geometry(str_param));
            else
            {
                foreach (string p in str_params_OR)
                {
                    if (r2gt == GeometryUtils.StringToRelationship2Geometry(p))
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

    [ValueConversion(typeof(Relation2GeomType), typeof(string))]
    public class Relation2GeomTypeToStringConverter : IValueConverter
    {
        // in order to react to more than one type at once
        // use + as OR
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;

            Relation2GeomType r2gt = Relation2GeomType.NONE;
            if (value is Relation2GeomType)
                r2gt = (Relation2GeomType)value;

            return GeometryUtils.Relationship2GeometryToDescrDE(r2gt);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;

            string r2gt_str = value.ToString();
            return GeometryUtils.StringToRelationship2Geometry(r2gt_str);
        }

    }

    #endregion

    #region COMPARER

    public class CompRepComparer : IComparer
    {
        public int Compare(object _crLHS, object _crRHS)
        {
            CompRep crLHS = _crLHS as CompRep;
            CompRep crRHS = _crRHS as CompRep;

            if (crLHS == null && crRHS == null)
                return 0;
            else if (crLHS == null && crRHS != null)
                return -1;
            else if (crLHS != null && crRHS == null)
                return 1;

            if (crLHS.CR_ID == crRHS.CR_ID)
                return 0;
            else if (crLHS.CR_ID < crRHS.CR_ID)
                return -1;
            else
                return 1;
        }
    }

    #endregion



}
