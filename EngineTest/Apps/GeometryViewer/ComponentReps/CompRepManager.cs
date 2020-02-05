using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading;

using InterProcCommunication.Specific;

namespace GeometryViewer.ComponentReps
{
    public class CompRepManager : INotifyPropertyChanged
    {
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

        #region PROPERTIES: Management

        private List<CompRep> comp_rep_record;
        public ReadOnlyCollection<CompRep> CompRepRecord { get { return this.comp_rep_record.AsReadOnly(); } }

        private bool record_changed;
        public bool RecordChanged
        {
            get { return this.record_changed; }
            set 
            { 
                this.record_changed = value;
                if (this.record_changed)
                    this.RegisterPropertyChanged("RecordChanged");
            }
        }

        private List<CompRep> comp_rep_record_flat;
        private Viewport3DXext communcation_manager;

        #endregion

        #region .CTOR

        public CompRepManager()
        {
            this.comp_rep_record = new List<CompRep>();
            this.comp_rep_record_flat = new List<CompRep>();
        }

        internal void SetCommunicationManager(Viewport3DXext _comm_manager)
        {
            if (this.communcation_manager == null && _comm_manager != null)
                this.communcation_manager = _comm_manager;
        }

        #endregion

        #region METHODS: Add, Remove

        // for a sequence of messages
        public void AddCompReps(List<ComponentMessage> _cmsgs)
        {
            if (_cmsgs == null) return;
            if (_cmsgs.Count == 0) return;

            // add all
            List<CompRep> added = new List<CompRep>();
            foreach(ComponentMessage cmsg in _cmsgs)
            {
                CompRep cr_new = AddCompRep(cmsg);
                if (cr_new != null)
                    added.Add(cr_new);
            }
            if (added.Count == 0)
            {
                // updates only of Component Ids, if any
                this.RecordChanged = false;
                this.RecordChanged = true;
                return;
            }

            // establish the relationships btw the component representations:
            // assign the parent ID
            // assing the representations of sub-components
            foreach(CompRep cr in added)
            {
                CompRepInfo cri = cr as CompRepInfo;
                if (cri == null) continue;

                if (cri.Comp_Parent > -1)
                {
                    CompRepInfo parent = this.FindByCompId(cri.Comp_Parent);
                    if (parent != null)
                    {
                        cri.CR_Parent = parent.CR_ID;
                        parent.AddSubCompRep(cri);
                        this.comp_rep_record.Remove(cri);
                    }
                }
            }
            this.UpdateFlatRecord();

            // establish connectivity
            List<CompRepContainedIn> connection_nodes =
                this.comp_rep_record_flat.Where(x => x is CompRepContainedIn).Select(x => x as CompRepContainedIn).ToList();
            if (connection_nodes.Count > 0)
            {
                foreach (CompRep cr in this.comp_rep_record_flat)
                {
                    if (cr is CompRepConnects)
                    {
                        CompRepConnects connection_edge = cr as CompRepConnects;
                        connection_edge.RetrieveConnectionPoints(connection_nodes);                       
                    }
                }
            }            

            // re-associate with existing geometry (this causes information flow back to the ComponentBuilder)
            // NOTE: after the hierarchy has been established in the previous step
            foreach(CompRep cr in this.comp_rep_record_flat)
            {
                if (cr is CompRepDescirbes)
                {
                    CompRepDescirbes crd = cr as CompRepDescirbes;
                    if (crd.GR_State.IsRealized)
                        this.communcation_manager.ConnectCompRepToZonedVolume(crd);
                }
                else if (cr is CompRepAlignedWith)
                {
                    CompRepAlignedWith cra = cr as CompRepAlignedWith;
                    this.communcation_manager.ConnectCompRepToMaterial(cra);
                }
            }
            
            // done
            this.RecordChanged = false;
            this.RecordChanged = true;
        }

        public CompRep AddCompRep(ComponentMessage _cmsg)
        {
            if (_cmsg == null) return null;
            if (this.communcation_manager == null) return null; // unable to communicate...

            // check for duplicate COMPONENT representations
            CompRep same_comp_id = this.FindByCompId(_cmsg.CompID);
            if (same_comp_id != null) return same_comp_id;

            // check for component representations with duplicate content -> take the one with the VALID COMPONENT ID
            CompRepInfo same_comp_content = this.FindSameStructureNoCompId(_cmsg);
            if (same_comp_content != null)
            {
                // just transfer the comp id
                same_comp_content.AdoptCompId(_cmsg);
                return null;
            }

            // creates a representation of a component that has been here before and looks for the referenced geometry
            // OR...
            // creates a representation of a component that is here for the first time
            switch(_cmsg.GeomType)
            {
                case InterProcCommunication.Specific.Relation2GeomType.DESCRIBES:
                    CompRepDescirbes created_D = new CompRepDescirbes(_cmsg, this.communcation_manager);
                    this.AddCompRepToRecord(created_D);
                    return created_D;
                case InterProcCommunication.Specific.Relation2GeomType.DESCRIBES_3D:
                    CompRepDescirbes3D created_D3d = new CompRepDescirbes3D(_cmsg, this.communcation_manager);
                    this.AddCompRepToRecord(created_D3d);
                    return created_D3d;
                case InterProcCommunication.Specific.Relation2GeomType.DESCRIBES_2DorLESS:
                    CompRepDescribes2DorLess created_D2d = new CompRepDescribes2DorLess(_cmsg, this.communcation_manager);
                    this.AddCompRepToRecord(created_D2d);
                    return created_D2d;
                case InterProcCommunication.Specific.Relation2GeomType.GROUPS:
                    return null;
                case InterProcCommunication.Specific.Relation2GeomType.CONTAINED_IN:
                    CompRepContainedIn created_CI = new CompRepContainedIn(_cmsg, this.communcation_manager);
                    this.AddCompRepToRecord(created_CI);
                    return created_CI;
                case InterProcCommunication.Specific.Relation2GeomType.CONNECTS:
                    CompRepConnects created_CO = new CompRepConnects(_cmsg, this.communcation_manager);
                    this.AddCompRepToRecord(created_CO);
                    return created_CO;
                case InterProcCommunication.Specific.Relation2GeomType.ALIGNED_WITH:
                    CompRepAlignedWith created_A = new CompRepAlignedWith(_cmsg, this.communcation_manager);
                    this.AddCompRepToRecord(created_A);
                    return created_A; 
                case InterProcCommunication.Specific.Relation2GeomType.NONE:
                    // this is for the parent of a geometry containing component
                    CompRepInfo created_N = new CompRepInfo(_cmsg, this.communcation_manager);
                    this.AddCompRepToRecord(created_N);
                    return created_N;
                default:
                    return null;
            }

        }

        private void AddCompRepToRecord(CompRep _cr)
        {
            this.comp_rep_record.Add(_cr);
            this.UpdateFlatRecord();
            this.RecordChanged = true;
            this.RecordChanged = false;
        }

        private bool RemoveCompRepFromRecord(CompRep _cr)
        {
            if (_cr == null) return false;

            bool success = this.comp_rep_record.Remove(_cr);
            this.UpdateFlatRecord();
            this.RecordChanged = true;
            this.RecordChanged = false;
            return success;
        }

        public void UpdateFlatRecord()
        {
            this.comp_rep_record_flat = new List<CompRep>();
            foreach(CompRep cr in this.comp_rep_record)
            {
                this.comp_rep_record_flat.Add(cr);
                if (cr is CompRepInfo)
                {
                    CompRepInfo cri = cr as CompRepInfo;
                    List<CompRepInfo> cri_subs = cri.GetFlatListOfSubCompReps();
                    this.comp_rep_record_flat.AddRange(cri_subs);
                }
            }
        }

        #endregion

        #region METHODS: Referencing

        private List<CompRepDescribes2DorLess> FindAllSurfaceRepsWMaterial(long _material_id)
        {
            List<CompRepDescribes2DorLess> found = new List<CompRepDescribes2DorLess>();
            if (_material_id < 0) return found;

            if (this.comp_rep_record_flat == null)
                this.UpdateFlatRecord();

            foreach(CompRep cr in this.comp_rep_record_flat)
            {
                if (!(cr is CompRepDescribes2DorLess)) continue;

                CompRepDescribes2DorLess surf_rep = cr as CompRepDescribes2DorLess;
                if (surf_rep.Material_ID == _material_id)
                    found.Add(surf_rep);
            }

            return found;
        }

        private CompRepAlignedWith FindCompRepAssociatedWith(long _material_id)
        {
            if (_material_id < 0) return null;

            if (this.comp_rep_record_flat == null)
                this.UpdateFlatRecord();

            foreach (CompRep cr in this.comp_rep_record_flat)
            {
                if (!(cr is CompRepAlignedWith)) continue;

                CompRepAlignedWith wall_constr_rep = cr as CompRepAlignedWith;
                if (wall_constr_rep.WallConstr == null) continue;

                if (wall_constr_rep.WallConstr.ID == _material_id)
                    return wall_constr_rep;
            }

            return null;
        }

        public long ConvertMaterialID2CompReference(long _material_id)
        {
            CompRepAlignedWith cra = this.FindCompRepAssociatedWith(_material_id);
            if (cra == null)
                return -1L;
            else
                return cra.Comp_ID;
        }


        public ReadOnlyCollection<CompRepDescribes2DorLess> AddMaterialReferenceToAffectedSurfaceReps(long _material_id, long _comp_id)
        {
            List<CompRepDescribes2DorLess> affected = this.FindAllSurfaceRepsWMaterial(_material_id);           
            if (affected.Count == 0) return affected.AsReadOnly();

            List<CompRepDescribes2DorLess> affected_and_updated = new List<CompRepDescribes2DorLess>();
            foreach(CompRepDescribes2DorLess surf_rep in affected)
            {
                bool updated = surf_rep.SetMaterialReference(_comp_id);
                if (updated)
                    affected_and_updated.Add(surf_rep);
            }

            return affected_and_updated.AsReadOnly();
        }

        #endregion

        #region METHODS: Message Generation

        public List<ComponentMessage> ExtractMessagesFrom(CompRep _cr)
        {
            List<ComponentMessage> extracted = new List<ComponentMessage>();
            if (_cr == null) return extracted;

            CompRepInfo cri = _cr as CompRepInfo;
            if (cri == null) return extracted;

            List<CompRepInfo> cr_subCRs = cri.GetFlatListOfSubCompReps();
            int nrSub = cr_subCRs.Count;
            if (nrSub == 0)
            {
                ComponentMessage msg = cri.ExtractMessage(MessagePositionInSeq.SINGLE_MESSAGE);
                if (msg != null)
                    extracted.Add(msg);
            }
            else
            {
                ComponentMessage msg = cri.ExtractMessage(MessagePositionInSeq.SEQUENCE_START_MESSAGE);
                if (msg != null)
                    extracted.Add(msg);
                for(int i = 0; i < nrSub; i++)
                {
                    MessagePositionInSeq pos_current = (i == (nrSub - 1)) ? MessagePositionInSeq.SEQUENCE_END_MESSAGE : MessagePositionInSeq.MESSAGE_INSIDE_SEQUENCE;
                    ComponentMessage msg_i = cr_subCRs[i].ExtractMessage(pos_current);
                    if (msg_i != null)
                        extracted.Add(msg_i);
                }
            }

            return extracted;
        }

        public List<ComponentMessage> ExtractMessagesForReferenceUpdateFrom(CompRep _cr)
        {
            List<ComponentMessage> extracted = new List<ComponentMessage>();
            if (_cr == null) return extracted;

            CompRepInfo cri = _cr as CompRepInfo;
            if (cri == null) return extracted;

            // call 'ExtractMessage' to force synchronization with geometry
            ComponentMessage msg1 = cri.ExtractMessage(MessagePositionInSeq.SINGLE_MESSAGE);
            // .............................................................................

            List<CompRepInfo> cr_subCRs = cri.GetFlatListOfSubCompReps();
            List<CompRepInfo> cr_CRs_w_refs = cr_subCRs.Where(x => x.Comp_RefCompIDs.Count > 0).ToList();
            if (cri.Comp_RefCompIDs.Count > 0)
                cr_CRs_w_refs.Insert(0, cri);

            int nrMsg = cr_CRs_w_refs.Count;
            if (nrMsg == 1)
            {
                ComponentMessage msg = cr_CRs_w_refs[0].ExtractMessage(MessagePositionInSeq.SINGLE_MESSAGE);
                if (msg != null)
                    extracted.Add(msg);
            }
            else if (nrMsg > 1)
            {
                for (int i = 0; i < nrMsg; i++)
                {
                    MessagePositionInSeq pos_current = MessagePositionInSeq.SEQUENCE_START_MESSAGE;
                    if (i > 0 && i < (nrMsg - 1))
                        pos_current = MessagePositionInSeq.MESSAGE_INSIDE_SEQUENCE;
                    else if (i == (nrMsg - 1))
                        pos_current = MessagePositionInSeq.SEQUENCE_END_MESSAGE;

                    ComponentMessage msg_i = cr_CRs_w_refs[i].ExtractMessage(pos_current);
                    if (msg_i != null)
                        extracted.Add(msg_i);
                }
            }

            return extracted;
        }

        #endregion

        #region METHODS: Finding CompReps by ...

        public CompRep FindById(long _id)
        {
            CompRep found = this.comp_rep_record_flat.FirstOrDefault(x => x.CR_ID == _id);
            return found;
        }

        public CompRepInfo FindByCompId(long _id)
        {
            CompRep found = this.comp_rep_record_flat.FirstOrDefault(x => (x is CompRepInfo) && (x as CompRepInfo).Comp_ID == _id);
            return found as CompRepInfo;
        }

        public CompRepInfo FindByGeomId(long _id)
        {
            foreach (CompRep record in this.comp_rep_record_flat)
            {
                CompRepInfo cri = record as CompRepInfo;
                if (cri == null) continue;

                GeometricRelationship geom_rel = cri.GR_Relationships.FirstOrDefault(x => x.GrIds.X == _id);
                if (geom_rel != null)
                {
                    return cri;
                }
            }

            return null;
        }

        public CompRepInfo FindByGeomId(long _id_volume, int _id_surface)
        {
            foreach (CompRep record in this.comp_rep_record_flat)
            {
                CompRepInfo cri = record as CompRepInfo;
                if (cri == null) continue;

                GeometricRelationship geom_rel = cri.GR_Relationships.FirstOrDefault(x => x.GrIds.X == _id_volume && x.GrIds.Y == _id_surface);
                if (geom_rel != null)
                {
                    return cri;
                }
            }

            return null;
        }

        public CompRepInfo FindByReferencedCompId(long _ref_id)
        {
            foreach (CompRep record in this.comp_rep_record_flat)
            {
                CompRepInfo cri = record as CompRepInfo;
                if (cri == null) continue;

                if (cri.Comp_RefCompIDs.Contains(_ref_id))
                    return cri;  
            }

            return null;
        }

        public CompRepInfo FindByParentCompRepId(long _parent_id)
        {
            CompRep found = this.comp_rep_record_flat.FirstOrDefault(x => (x is CompRepInfo) && (x as CompRepInfo).CR_Parent == _parent_id);
            return found as CompRepInfo;
        }

        public CompRepInfo FindByParentComponentId(long _parent_comp_id)
        {
            CompRep found = this.comp_rep_record_flat.FirstOrDefault(x => (x is CompRepInfo) && (x as CompRepInfo).Comp_Parent == _parent_comp_id);
            return found as CompRepInfo;
        }

        // NEW (07.08.2017)
        /// <summary>
        /// <para>Call in order to update the id of automatically generated components.</para>
        /// </summary>
        /// <param name="_msg_query"></param>
        /// <returns>A component representation with current component id -1 but with the same content.</returns>
        private CompRepInfo FindSameStructureNoCompId(ComponentMessage _msg_query)
        {
            if (_msg_query == null) return null;

            foreach (CompRep cr in this.comp_rep_record_flat)
            {
                CompRepInfo cri = cr as CompRepInfo;
                if (cri == null) continue;
                if (cri.Comp_ID > -1) continue;

                ComponentMessage msg_test = cri.Comp_Msg;
                if (msg_test == null)
                    msg_test = cri.ExtractMessage(MessagePositionInSeq.SINGLE_MESSAGE);
                
                bool found = ComponentMessage.HaveSameContent(_msg_query, msg_test);
                if (found)
                    return cri;
            }

            return null;
        }

        #endregion

        #region METHODS: Info Extraction By Type

        /// <summary>
        /// <para>The search involves all subcomponents. E.g.if a comp rep has a sub comp rep somewhere </para>
        /// <para>with a type contained in '_types', it will be included. For this reason the same component</para>
        /// <para>collected multiple times.</para>
        /// </summary>
        /// <param name="_types"></param>
        /// <returns></returns>
        public ReadOnlyCollection<CompRepInfo> PartialCompRepRecord(List<Relation2GeomType> _types)
        {
            List<CompRepInfo> found = new List<CompRepInfo>();
            if (_types == null) return found.AsReadOnly();
            if (_types.Count == 0) return found.AsReadOnly();

            List<long> added_parent = new List<long>();
            foreach (CompRep cr in this.comp_rep_record)
            {
                CompRepInfo cri = cr as CompRepInfo;
                if (cri == null) continue;

                if (_types.Contains(cri.GR_State.Type))
                    found.Add(cri);
                else
                    if (cri.HasCompRepOfType(_types))                    
                        found.Add(cri);                    
            }
            return found.AsReadOnly();
        }

        public ReadOnlyCollection<CompRepInfo> FullCompRepRecord()
        {
            return this.comp_rep_record.Where(x => x is CompRepInfo).Select(x => x as CompRepInfo).ToList().AsReadOnly();
        }

        public int GetNrOfLoaded(Relation2GeomType _type)
        {
            return this.comp_rep_record_flat.Where(x => x is CompRepInfo).Select(x => x as CompRepInfo).Count(x => x.GR_State.Type == _type);
        }

        #endregion

        #region METHODS: Selection

        public void Select(CompRepInfo _comp)
        {
            // expand
            if (_comp != null)
            {
                _comp.IsExpanded = true;
                List<CompRep> parent_chain = this.GetParentChain(_comp);
                foreach (CompRep cr in parent_chain)
                {
                    cr.IsExpanded = true;
                }
            }

            // reset selection
            foreach (CompRep cr in this.CompRepRecord)
            {
                cr.IsSelected = false;
                if (cr is CompRepInfo)
                {
                    CompRepInfo cri = cr as CompRepInfo;
                    List<CompRepInfo> cri_subs = cri.GetFlatListOfSubCompReps();
                    foreach (CompRepInfo cri_subs_i in cri_subs)
                    {
                        cri_subs_i.IsSelected = false;
                    }
                }
            }  

            // select
            if (_comp != null)
            {
                _comp.IsSelected = true;
            }
        }

        private List<CompRep> GetParentChain(CompRepInfo _cri)
        {
            if (_cri == null) return new List<CompRep>();

            List<CompRep> chain = new List<CompRep>();
            CompRep parent = (_cri.CR_Parent > -1) ? this.FindById(_cri.CR_Parent) : null;
            CompRepInfo parent_as_cri = (parent == null) ? null : (parent as CompRepInfo);

            while (parent_as_cri != null)
            {
                chain.Add(parent);
                parent = (parent_as_cri.CR_Parent > -1) ? this.FindById(parent_as_cri.CR_Parent) : null;
                parent_as_cri = (parent == null) ? null : (parent as CompRepInfo);
            }

            return chain;
        }

        #endregion
    }
}
