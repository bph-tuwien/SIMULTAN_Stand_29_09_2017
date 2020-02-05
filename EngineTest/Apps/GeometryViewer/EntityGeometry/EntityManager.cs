using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;

using GeometryViewer.ComponentReps;
using GeometryViewer.EntityDXF;

namespace GeometryViewer.EntityGeometry
{
    public class EntityManager
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ==================================== CLASS MEMEBERS AND INITIALIZATION ================================= //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region STAIC

        public static readonly Layer NULL_LAYER;

        static EntityManager()
        {
            NULL_LAYER = new Layer("NULL", Color.Black);
        }

        #endregion

        #region PROPERTIES / CLASS MEMBERS
        // data carrying container, the rest is for easier calculations ONLY
        public List<Layer> Layers { get; private set; }
        private Layer defaultLayer;
        
        // selection management
        public int SelectedGeomIndex { get; private set; }
        public ZonedPolygon SelectedPolygon { get; private set; }
        public bool SelectedEntityIsPolygon { get; private set; }
        public ZonedVolume SelectedVolume { get; private set; }
        public bool SelectedEntityIsVolume { get; private set; }

        // for display and user input
        public List<ZonedPolygonVertexVis> VerticesOfSelectedPolygon { get; private set; }
        public List<ZoneOpeningVis> OpeningsOfSelectedPolygon { get; private set; }
        public List<ZonedVolumeSurfaceVis> SurfacesOfSelectedVolume { get; private set; }

        // search function
        private List<Entity> entities_flat;
        IEnumerator<Entity> matchingEntityEnumerator;
        private string prev_search_text;

        // parsing
        private List<GeometricEntity> parsed_GE_before_adding_to_Layer;

        #endregion

        #region .CTOR

        public EntityManager()
        {
            this.Reset(true);
        }

        internal void Reset(bool _w_default_layer)
        {
            this.Layers = new List<Layer>();
            if (_w_default_layer)
            {
                this.defaultLayer = new Layer("_Layer Default", Color.Black);
                this.defaultLayer.Visibility = EntityVisibility.ON;
                this.Layers.Add(this.defaultLayer);
            }
            this.ResetSelectionVaraibles();
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ================================== LAYER AND GEOMETRIC ENTITY MANAGEMENT =============================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region LAYER AND GEOMETRIC ENTITY MANAGEMENT
        public bool AddLayer(Layer _layer, bool _allowDuplicateNames = true)
        {
            if (_layer == null) return false;
            if (this.Layers.Contains(_layer)) return false;

            // no duplicate names
            if (!_allowDuplicateNames)
            {
                List<Layer> layers_w_same_name = this.Layers.FindAll(x => x.EntityName == _layer.EntityName);
                if (layers_w_same_name != null && layers_w_same_name.Count > 0)
                    return false;
            }

            this.Layers.Add(_layer);
            return true;
        }

        public Layer GetParentLayer(Layer _layer)
        {
            if (_layer == null) return null;

            // top level
            if (this.Layers.Contains(_layer)) return null;

            foreach(Layer item in this.Layers)
            {
                List<Layer> itemContent = item.GetFlatLayerList();
                if (itemContent.Contains(_layer)) 
                    return item;
            }

            return null;
        }

        // returns the ancestor chain starting at the highest level and ending with the input layer
        public List<Layer> GetParentLayerChain(Layer _layer)
        {
            if (_layer == null) return null;

            // if at top level
            if (this.Layers.Contains(_layer)) return new List<Layer> { _layer };

            List<Layer> chain = new List<Layer>();

            // get top-level parent
            Layer topParent = null;
            foreach (Layer item in this.Layers)
            {
                List<Layer> itemContent = item.GetFlatLayerList();
                if (itemContent.Contains(_layer))
                    topParent = item;
            }

            if (topParent == null)
                return new List<Layer> { _layer };

            // proceed from the top parent down
            chain.Add(topParent);
            Layer currentParent = topParent;
            while(currentParent.ID != _layer.ID)
            {
                foreach(Entity e in currentParent.ContainedEntities)
                {
                    Layer subLayer = e as Layer;
                    if (subLayer == null)
                        continue;

                    if (subLayer.ID == _layer.ID)
                    {
                        currentParent = subLayer;
                        break;
                    }

                    List<Layer> subLayerContent = subLayer.GetFlatLayerList();
                    if (subLayerContent.Contains(_layer))
                    {
                        currentParent = subLayer;
                        break;
                    }
                }
                chain.Add(currentParent);
            }

            return chain;
            
        }

        private bool RemoveLayer(Layer _layer)
        {
            if (_layer == null) return false;
            if (_layer.ID == this.defaultLayer.ID) return false; // changed (to ID comparison) 12.06.2017

            bool success_level0 = this.Layers.Remove(_layer);
            if (success_level0)
                return true;
            else
            {
                foreach(Layer sublayer in this.Layers)
                {
                    bool success_levelN = sublayer.RemoveEntity(_layer);
                    if (success_levelN)
                        return true;
                }
                return false;
            }
        }       

        public bool MoveLayer(Layer _layer, Layer _toLayer)
        {
            if (_layer == null || _toLayer == null) return false;
            if (_layer.ID == _toLayer.ID) return false;
            if (_toLayer.ContainedEntities.Contains(_layer)) return false;


            // remove layer from old parent layer or from the top of the hierarchy
            bool success = this.RemoveLayer(_layer);
            if (!success) return false;

            // add to the destination layer
            if (_toLayer != NULL_LAYER)
                success = _toLayer.AddEntity(_layer);
            else
                success = this.AddLayer(_layer);
            return success;
        }

        public bool RemoveEntity(Entity _e)
        {
            if (_e == null) return false;

            GeometricEntity ge = _e as GeometricEntity;
            Layer layer = _e as Layer;
            if (ge != null)
            {
                // if the entity is a volume, release the connection to its defining levels and polygons
                // trigger its PropertyChanged event to alert a Space that may use it
                ZonedVolume zv = ge as ZonedVolume;
                if (zv != null)
                {
                    zv.ReleaseLevels();
                    zv.EditModeType = ZonedVolumeEditModeType.ISBEING_DELETED;
                }

                // if the entity is a polygon, trigger its PropertyChanged event to alert a Volume that may use it
                ZonedPolygon zp = ge as ZonedPolygon;
                if (zp != null)
                    zp.EditModeType = ZonePolygonEditModeType.ISBEING_DELETED;

                // remove from the layer
                Layer geL = ge.EntityLayer;
                return geL.RemoveEntity(_e);
            }
            else if(layer != null)
            {
                return this.RemoveLayer(layer);
            }

            return false;
        }

        public Layer FindLayerByName(string _name)
        {
            if (_name == null || _name == string.Empty)
                return null;

            List<Layer> flatLayers = GetFlatLayerList();
            return flatLayers.FirstOrDefault(x => x.EntityName == _name);
        }

        public Layer FindLayerByNameAndParent(string _name, Layer _parent)
        {
            if (_name == null || _name == string.Empty || _parent == null)
                return null;

            List<Layer> flatLayers = GetFlatLayerList();
            List<Layer> found = flatLayers.FindAll(x => x.EntityName == _name);
            if (found == null || found.Count < 1)
                return null;

            foreach(Layer layer in found)
            {
                Layer parent = this.GetParentLayer(layer);
                if (parent != null && parent.ID == _parent.ID)
                    return layer;
            }
            return null;
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // =================================== MANIPULATION OF POLYGONS AND VOLUMES =============================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region POLYGON MANIPULATION
        public void ReverseSelectedPolygon()
        {
            this.SelectedPolygon.Reverse();
            this.VerticesOfSelectedPolygon = this.SelectedPolygon.ExtractVerticesForDisplay();
            this.OpeningsOfSelectedPolygon = this.SelectedPolygon.ExtractOpeningsForDisplay();
        }

        public void AddVertexToSelectedPolygon(Point3D _vCandidate, int _afterIndex)
        {
            bool success = this.SelectedPolygon.AddVertex(_vCandidate, _afterIndex);
            if (success)
            {
                this.VerticesOfSelectedPolygon = this.SelectedPolygon.ExtractVerticesForDisplay();
                this.OpeningsOfSelectedPolygon = this.SelectedPolygon.ExtractOpeningsForDisplay();
            }
        }

        public void RemoveVertexFromSelectedPolygon(ZonedPolygonVertexVis _zpvToRemove)
        {
            if (_zpvToRemove == null)
                return;

            int atIndex = _zpvToRemove.IndexInOwner;
            bool success = this.SelectedPolygon.RemoveVertex(atIndex);
            if (success)
            {
                this.VerticesOfSelectedPolygon = this.SelectedPolygon.ExtractVerticesForDisplay();
                this.OpeningsOfSelectedPolygon = this.SelectedPolygon.ExtractOpeningsForDisplay();
            }
        }

        public void MoveVertexFromSelectedPolygon(ZonedPolygonVertexVis _zpvToMove, bool _forward, float _distance)
        {
            if (_zpvToMove == null)
                return;

            bool success = _zpvToMove.SetAt(_forward, _distance);
            if (success)
            {
                // THIS CODE CAUSES A FEEDBACK LOOP: LEAVE IT COMMENTED
                // this.VerticesOfSelectedPolygon = this.SelectedPolygon.ExtractVerticesForDisplay();
                this.OpeningsOfSelectedPolygon = this.SelectedPolygon.ExtractOpeningsForDisplay();
            }

        }

        public void SetPolygonLabels()
        {
            foreach(ZonedPolygonVertexVis v in this.VerticesOfSelectedPolygon)
            {
                if (v.Owner.ID == this.SelectedPolygon.ID)
                    this.SelectedPolygon.DefineZone(v.IndexInOwner, v.ZoneInOwner);                       
            }
            this.VerticesOfSelectedPolygon = this.SelectedPolygon.ExtractVerticesForDisplay();
        }

        public void ResetPolygonLabels()
        {
            this.SelectedPolygon.ClearZones();
            this.VerticesOfSelectedPolygon = this.SelectedPolygon.ExtractVerticesForDisplay();
        }

        public void SetPolygonLabels(string _labels_new, string _separator = ",")
        {
            // extract the lables from the string
            if (string.IsNullOrEmpty(_labels_new)) return;

            string[] label_literals = _labels_new.Split(new string[] { _separator }, StringSplitOptions.RemoveEmptyEntries);
            if (label_literals == null || label_literals.Length < 1) return;

            List<int> labels = new List<int>();
            foreach (string lit in label_literals)
            {
                int label = -1;
                bool success = int.TryParse(lit, out label);
                if (success)
                    labels.Add(label);
            }

            // apply the extracted labels
            this.SelectedPolygon.ReDefineAllZones(labels);

            this.VerticesOfSelectedPolygon = this.SelectedPolygon.ExtractVerticesForDisplay();
        }

        public void AddOpeningToSelectedPolygon(Point3D _p1, Point3D _p2, int _atIndex, float _jambHeight, float _clearHeight)
        {
            int n = this.SelectedPolygon.Polygon_Coords.Count;
            if (_atIndex < 0 || _atIndex > (n - 1))
                return;

            Point3D vert = this.SelectedPolygon.Polygon_Coords[_atIndex];

            // check which point is closer to the vertex or the polygon
            float dist1 = (_p1.ToVector3() - vert.ToVector3()).Length();
            float dist2 = (_p2.ToVector3() - vert.ToVector3()).Length();
            float distFromPrevVertex = Math.Min(dist1, dist2);
            float lengthAlongPolygonSegment = Math.Abs(dist2 - dist1);

            int newid = this.SelectedPolygon.AddOpening(_atIndex, distFromPrevVertex, lengthAlongPolygonSegment, _jambHeight, _clearHeight);
            if (newid != -1)
            {
                this.OpeningsOfSelectedPolygon = this.SelectedPolygon.ExtractOpeningsForDisplay();
            }
        }

        public void RemoveOpeningFromSelectedPolygon(ZoneOpeningVis _zovToRemove)
        {
            if (_zovToRemove == null)
                return;

            bool success = this.SelectedPolygon.RemoveOpening(_zovToRemove.IDInOwner);
            if (success)
            {
                this.OpeningsOfSelectedPolygon = this.SelectedPolygon.ExtractOpeningsForDisplay();
            }
        }

        public void ModifyOpeningOfSelectedPolygon(ZoneOpeningVis _zovToModify, Point3D _p1, Point3D _p2, float _jambHeight, float _clearHeight)
        {
            if (_zovToModify == null)
                return;

            int n = this.SelectedPolygon.Polygon_Coords.Count;
            if (_zovToModify.IndInOwner < 0 || _zovToModify.IndInOwner > (n - 1))
                return;

            Point3D vert = this.SelectedPolygon.Polygon_Coords[_zovToModify.IndInOwner];
            float distFromPrevVertex = (_p1.ToVector3() - vert.ToVector3()).Length();
            float lengthAlongPolygonSegment = (_p2.ToVector3() - _p1.ToVector3()).Length();

            bool success = this.SelectedPolygon.ModifyOpeningDimensions(_zovToModify.IDInOwner, distFromPrevVertex, lengthAlongPolygonSegment, _jambHeight, _clearHeight);
            if (success)
            {
                this.OpeningsOfSelectedPolygon = this.SelectedPolygon.ExtractOpeningsForDisplay();
            }
        }

        public void ModifyAllOpeningsOfSelectedPolygon()
        {
            foreach(ZoneOpeningVis o in this.OpeningsOfSelectedPolygon)
            {
                if (o.OwnerPolygon.ID == this.SelectedPolygon.ID)
                    this.SelectedPolygon.ModifyOpeningDimensions(o.IDInOwner, o.DistFromVertex, o.Length, 
                                                                              o.DistFromFloorPolygon, o.Height,
                                                                              o.Label);
            }
            this.OpeningsOfSelectedPolygon = this.SelectedPolygon.ExtractOpeningsForDisplay();
        }

        #endregion

        #region VOLUME MANIPULATION

        public void AssignMaterialToSurface(ZonedVolumeSurfaceVis _input)
        {
            if (this.SurfacesOfSelectedVolume != null && this.SurfacesOfSelectedVolume.Count > 0)
            {
                long v_id = this.SurfacesOfSelectedVolume[0].OwnerVolume.ID;
                if (v_id == this.SelectedVolume.ID)
                    this.SelectedVolume.UpdateMaterialAssociation(_input);
            }
        }

        public void ReAssociateZonedVolumeWComp(CompRepInfo _comp)
        {
            if (_comp == null)
                return;

            if (this.SelectedVolume != null)
                this.SelectedVolume.EditModeType = ZonedVolumeEditModeType.ISBEING_REASSOCIATED;

            switch(_comp.GR_State.Type)
            {
                case InterProcCommunication.Specific.Relation2GeomType.DESCRIBES:
                    CompRepDescirbes crd = _comp as CompRepDescirbes;
                    if (crd != null)
                    {
                        // if the Volume is NULL, the relationship turns to 'not realized'
                        // (see CompRepDescirbes.Geom_Zone setter)
                        crd.Geom_Zone = this.SelectedVolume;
                    }
                    break;
                case InterProcCommunication.Specific.Relation2GeomType.CONTAINED_IN:
                    CompRepContainedIn_Instance crci = _comp as CompRepContainedIn_Instance;
                    if (crci != null)
                    {
                        // if the Volume is NULL, nothing happens
                        crci.PlaceIn(this.SelectedVolume);
                    }
                    break;
                default:
                    break;
            }
            
        }

        public ZonedVolume GetVolumeByID(long _id)
        {
            List<GeometricEntity> gList = GetFlatGeometryList();
            GeometricEntity found = gList.FirstOrDefault(x => x.ID == _id && x is ZonedVolume);
            if (found != null)
                return (found as ZonedVolume);
            else
                return null;
        }

        public void UpdateVolumesAfterMaterialChange(ComponentInteraction.Material _mat)
        {
            List<GeometricEntity> gList = GetFlatGeometryList();
            List<ZonedVolume> volumes = gList.Where(x => x is ZonedVolume).Select(x => x as ZonedVolume).ToList();
            if (volumes == null || volumes.Count == 0) return;

            List<ZonedVolume> volumes_to_update = new List<ZonedVolume>();
            foreach(ZonedVolume zv in volumes)
            {
                int nr_affected_surf = zv.GetNrSurfacesWithMaterial(_mat);
                if (nr_affected_surf > 0)
                    volumes_to_update.Add(zv);
            }

            foreach(ZonedVolume zv in volumes_to_update)
            {
                zv.ReCreateAllGeometry();
            }
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // =============================================== INFO EXTRACTION ======================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region INFO EXTRACTION
        public List<Layer> GetFlatLayerList()
        {
            List<Layer> list = new List<Layer>();
            list.Add(NULL_LAYER);
            foreach(Layer layer in this.Layers)
            {
                list.Add(layer);
                list.AddRange(layer.GetFlatLayerList());
            }
            return list;
        }

        public Dictionary<Layer, string> GetFlatLayerQualifiedNameList()
        {
            Dictionary<Layer, string> names = new Dictionary<Layer, string>();
            names.Add(NULL_LAYER, NULL_LAYER.EntityName);
            foreach(Layer layer in this.Layers)
            {
                names.Add(layer, layer.EntityName);
                Dictionary<Layer, string> names_sublayers = layer.GetFlatQualifiedLayerNameList();
                foreach(var entry in names_sublayers)
                {
                    names.Add(entry.Key, entry.Value);
                }
            }
            return names;

        }

        public List<GeometricEntity> GetFlatGeometryList()
        {
            List<GeometricEntity> list = new List<GeometricEntity>();
            foreach (Layer layer in this.Layers)
            {
                list.AddRange(layer.GetFlatGeometryList());
            }
            return list;
        }

        #endregion

        #region ENTITY SEARCH: by Name

        public Entity ProcessSearch(string _partName)
        {
            if (_partName == null || _partName == String.Empty)
                return null;

            if (this.entities_flat == null)
            {
                this.entities_flat = new List<Entity>();
                this.entities_flat.AddRange(this.GetFlatLayerList());
                this.entities_flat.AddRange(this.GetFlatGeometryList());
            }

            if (this.entities_flat.Count < 1)
                return null;

            if (this.matchingEntityEnumerator == null || !this.matchingEntityEnumerator.MoveNext() || prev_search_text != _partName)
                this.VerifyMatches(_partName);

            Entity ent = this.matchingEntityEnumerator.Current;
            // update GUI parameters
            if (ent != null)
            {
                GeometricEntity gent = ent as GeometricEntity;
                if (gent != null)
                    this.SelectGeometry(gent);
                Layer lent = ent as Layer;
                if (lent != null)
                    this.SelectLayer(lent);
            }

            this.prev_search_text = _partName;
            return ent;
        }

        private void VerifyMatches(string _text)
        {
            IEnumerable<Entity> matchingEntities = this.FindMatches(_text);
            this.matchingEntityEnumerator = matchingEntities.GetEnumerator();

            if (!matchingEntityEnumerator.MoveNext())
                MessageBox.Show("No matching names were found.", "Try Again", MessageBoxButton.OK, MessageBoxImage.Information);
        }


        private IEnumerable<Entity> FindMatches(string _text)
        {
            foreach(Entity e in this.entities_flat)
            {
                if (e.EntityName.Contains(_text))
                    yield return e;
            }
        }


        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ============================================== SELECTION HANDLING ====================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region SELECTION HANDLING

        private void ResetSelectionVaraibles()
        {
            this.SelectedGeomIndex = -1;
            this.SelectedPolygon = null;
            this.SelectedEntityIsPolygon = false;
            this.SelectedVolume = null;
            this.SelectedEntityIsVolume = false;
            this.VerticesOfSelectedPolygon = new List<ZonedPolygonVertexVis>();
            this.OpeningsOfSelectedPolygon = new List<ZoneOpeningVis>();
            this.SurfacesOfSelectedVolume = new List<ZonedVolumeSurfaceVis>();
        }

        private void SelectLayer(Layer _layer)
        {
            List<Layer> lList = GetFlatLayerList();
            foreach(Layer layer in lList)
            {
                if (layer == null || layer.ID != _layer.ID)
                {
                    layer.IsSelected = false;
                    this.ResetSelectionVaraibles();
                }
                else
                {
                    layer.IsSelected = true;
                    layer.IsExpanded = true;
                    Layer topParent = GetParentLayer(layer);
                    if (topParent != null)
                        topParent.IsExpanded = true;

                    break;
                }
            }
        }

        public void SelectGeometry(long _id)
        {
            List<GeometricEntity> gList = GetFlatGeometryList();
            this.AdaptSelectionState(gList, _id);
        }

        public void SelectGeometry(GeometricEntity _e)
        {
            List<GeometricEntity> gList = GetFlatGeometryList();
            // DEBUG
            //long[] ids = gList.Select(x => x.ID).ToArray();
            long id = (_e == null) ? -1 : _e.ID;
            this.AdaptSelectionState(gList, id);
        }

        private void AdaptSelectionState(List<GeometricEntity> _gList, long _gID)
        {
            if (_gList == null) return;

            this.ResetSelectionVaraibles();

            int n = _gList.Count;
            for (int i = 0; i < n; i++)
            {
                GeometricEntity ge = _gList[i];
                if (_gID != ge.ID)
                {
                    ge.IsSelected = false;                    
                }
                else
                {
                    ge.IsSelected = true;

                    // process state in GUI
                    ge.EntityLayer.IsExpanded = true;
                    Layer topParent = GetParentLayer(ge.EntityLayer);
                    if (topParent != null)
                        topParent.IsExpanded = true;

                    // process further, if selected entity is a ZonedPolygon or a ZonedVolume
                    ZonedPolygon zp = ge as ZonedPolygon;
                    ZonedVolume zv = ge as ZonedVolume;
                    if (zp != null)
                    {
                        this.SelectedPolygon = zp;
                        this.SelectedEntityIsPolygon = true;
                        this.VerticesOfSelectedPolygon = zp.ExtractVerticesForDisplay();
                        this.OpeningsOfSelectedPolygon = zp.ExtractOpeningsForDisplay();
                        this.SurfacesOfSelectedVolume = new List<ZonedVolumeSurfaceVis>();

                        this.SelectedVolume = null;
                        this.SelectedEntityIsVolume = false;
                    }
                    else if (zv != null)
                    {
                        this.SelectedVolume = zv;
                        this.SelectedEntityIsVolume = true;

                        this.SelectedPolygon = null;
                        this.SelectedEntityIsPolygon = false;
                        this.VerticesOfSelectedPolygon = new List<ZonedPolygonVertexVis>();
                        this.OpeningsOfSelectedPolygon = new List<ZoneOpeningVis>();
                        this.SurfacesOfSelectedVolume = zv.ExtractSurfacesAndOpeningsForDisplay(); // changed 30.08.2017
                    }
                    else
                    {
                        this.ResetSelectionVaraibles();
                    }
                    this.SelectedGeomIndex = i;
                    break;
                }
            }
        }

        public void SelectVertex(ZonedPolygonVertexVis _v)
        {
            if (this.VerticesOfSelectedPolygon == null || this.VerticesOfSelectedPolygon.Count < 1)
                return;

            foreach(var vert in this.VerticesOfSelectedPolygon)
            {
                if (_v == null || _v.ID != vert.ID)
                {
                    vert.IsSelected = false;
                }
                else
                {
                    vert.IsSelected = true;
                }
            }
        }


        public void SelectOpening(ZoneOpeningVis _zov)
        {
            if (this.OpeningsOfSelectedPolygon == null || this.OpeningsOfSelectedPolygon.Count < 1)
                return;

            foreach(var opening in this.OpeningsOfSelectedPolygon)
            {
                if (_zov == null || _zov.ID != opening.ID)
                {
                    opening.IsSelected = false;
                }
                else
                {
                    opening.IsSelected = true;
                }
            }
        }

        public void SelectSurface(ZonedVolumeSurfaceVis _zvsv)
        {
            if (this.SurfacesOfSelectedVolume == null || this.SurfacesOfSelectedVolume.Count < 1)
                return;

            foreach (var surf in this.SurfacesOfSelectedVolume)
            {
                if (_zvsv == null || _zvsv.ID != surf.ID)
                {
                    surf.IsSelected = false;
                }
                else
                {
                    surf.IsSelected = true;
                }
            }
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ================================================= EXPORT AS DXF ======================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region EXPORT AS DXF

        public StringBuilder ExportEntites(bool _finalize)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());           // 0
            sb.AppendLine(DXFUtils.SECTION_START);                                  // SECTION
            sb.AppendLine(((int)EntitySaveCode.ENTITY_NAME).ToString());            // 2
            sb.AppendLine(DXFUtils.ENTITY_SECTION);                                 // ENTITIES

            foreach (Layer layer in this.Layers)
            {
                layer.AddToExport(ref sb, true);
            }

            sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());           // 0
            sb.AppendLine(DXFUtils.SECTION_END);                                    // ENDSEC

            if (_finalize)
            {
                sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());       // 0
                sb.AppendLine(DXFUtils.EOF);                                        // EOF
            }

            return sb;
        }

        public StringBuilder ExportEntitiesForACAD()
        {
            StringBuilder sb = new StringBuilder();

            // save linetypes and layers in their respective tables
            Dictionary<Layer, string> layer_names = this.GetFlatLayerQualifiedNameList();
            Dictionary<Layer, string> layer_names_conform = new Dictionary<Layer, string>();
            foreach(var entry in layer_names)
            {
                string name = entry.Value.Replace('.', '_');
                string name_1 = name.Replace(" ", "__");
                layer_names_conform.Add(entry.Key, name_1);
            }

            List<string> names = layer_names_conform.Values.ToList();
            List<SharpDX.Color> colors = layer_names_conform.Keys.Select(x => x.EntityColor).ToList();
            DXFUtils.AddLineTypeAndLayerDefinitionsToExport(ref sb, names, colors);

            // save the entities
            sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());           // 0
            sb.AppendLine(DXFUtils.SECTION_START);                                  // SECTION
            sb.AppendLine(((int)EntitySaveCode.ENTITY_NAME).ToString());            // 2
            sb.AppendLine(DXFUtils.ENTITY_SECTION);                                 // ENTITIES

            List<GeometricEntity> GEs = this.GetFlatGeometryList();
            foreach(GeometricEntity ge in GEs)
            {
                if (ge.Visibility == EntityVisibility.ON)
                    ge.AddToACADExport(ref sb, layer_names_conform[ge.EntityLayer], layer_names_conform[ge.EntityLayer] + DXFUtils.LAYER_HIDDEN_SUFFIX);
            }

            sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());           // 0
            sb.AppendLine(DXFUtils.SECTION_END);                                    // ENDSEC

            // mark the end
            sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());       // 0
            sb.AppendLine(DXFUtils.EOF);                                        // EOF

            return sb;
        }

        public StringBuilder ExportSelectedForACAD()
        {
            StringBuilder sb = new StringBuilder();

            GeometricEntity selection;
            if (this.SelectedVolume != null)
                selection = this.SelectedVolume;
            else
                selection = this.SelectedPolygon;

            if (selection == null)
                return sb;

            // save linetypes and layers in their respective tables
            string selected_layer_name = selection.EntityLayer.EntityName.Replace('.', '_');
            selected_layer_name = selected_layer_name.Replace(" ", "__");
            List<string> names = new List<string> { selected_layer_name };
            List<SharpDX.Color> colors = new List<Color> { selection.EntityLayer.EntityColor };

            DXFUtils.AddLineTypeAndLayerDefinitionsToExport(ref sb, names, colors);

            // save the entities
            sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());           // 0
            sb.AppendLine(DXFUtils.SECTION_START);                                  // SECTION
            sb.AppendLine(((int)EntitySaveCode.ENTITY_NAME).ToString());            // 2
            sb.AppendLine(DXFUtils.ENTITY_SECTION);                                 // ENTITIES

            selection.AddToACADExport(ref sb, selected_layer_name, selected_layer_name + DXFUtils.LAYER_HIDDEN_SUFFIX);

            sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());           // 0
            sb.AppendLine(DXFUtils.SECTION_END);                                    // ENDSEC

            // mark the end
            sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());       // 0
            sb.AppendLine(DXFUtils.EOF);                                        // EOF

            return sb;
        }

        #endregion

        #region PARSING from DXF

        internal void ResetStaticCounters()
        {
            Entity.Nr_Entities = 0;
            ZonedPolygon.ResetZoneOpeningCounter();
            this.parsed_GE_before_adding_to_Layer = new List<GeometricEntity>();
        }

        internal void CleanUpAfterParsing()
        {
            this.parsed_GE_before_adding_to_Layer = new List<GeometricEntity>();
        }

        internal Layer ReconstructLayer(long _id, string _name, SharpDX.Color _color, EntityVisibility _vis, bool _is_valid, bool _assoc_w_comp,
                                        float _line_thickness_GUI, List<string> _mLtext, bool _is_top_closure, bool _is_bottom_closure)
        {
            // create the layer
            Layer created = new Layer(_id, _name, _color, _vis, _is_valid, _assoc_w_comp, _line_thickness_GUI, _mLtext,
                                      _is_top_closure, _is_bottom_closure);
            Entity.Nr_Entities = Math.Max(Entity.Nr_Entities, _id) + 1;
            // add (for the time being) to the manager
            this.AddLayer(created);
            // if it is a sub-Layer of another one, it will be removed later...
            return created;
        }

        internal ZonedPolygon ReconstructZonedPolygon(long _id, string _name, SharpDX.Color _color, EntityVisibility _vis, bool _is_valid, bool _assoc_w_comp, 
                        float _line_thickness_GUI, List<string> _mLtext, bool _is_top_closure, bool _is_bottom_closure,
                        Layer _layer, bool _color_by_layer, List<Point3D> _coords, float _height, List<int> _zone_inds)
        {
            // create the polygon
            ZonedPolygon zp = new ZonedPolygon(_id, _name, _color, _vis, _is_valid, _assoc_w_comp,
                                                _line_thickness_GUI, _mLtext, _is_top_closure, _is_bottom_closure,
                                                _layer, _color_by_layer, _coords, _height);
            if (_zone_inds != null && _zone_inds.Count == _coords.Count)
                zp.ReDefineAllZones(_zone_inds);
            Entity.Nr_Entities = Math.Max(Entity.Nr_Entities, _id) + 1;
            // setting the layer adds the ZonedPolygon to the ContainedEntities of its layer 
            // (see DXFLayer and Property GeometricEntity.EnitiyLayer)

            // add to temporary container
            this.parsed_GE_before_adding_to_Layer.Add(zp);

            return zp;
        }

        internal void AddReconstructedOpeningToZonedPolygon(ZonedPolygon _zp, int _id, string _name, int _ind_in_poly, 
                                                            Vector3D _v_prev, Vector3D _v_next, Vector3D _poly_wallT, Vector3D _poly_wallT_adj, 
                                                            float _dist_from_v_prev, float _len_along_segm, float _dist_from_poly, float _height_along_wallT)
        {
            if (_zp == null) return;
            _zp.AddParsedOpening(_id, _name, _ind_in_poly, _v_prev, _v_next, _poly_wallT, _poly_wallT_adj,
                                             _dist_from_v_prev, _len_along_segm, _dist_from_poly, _height_along_wallT);
        }

        internal ZonedPolygonGroup ReconstructZonedLevel(long _id, string _name, SharpDX.Color _color, EntityVisibility _vis, bool _is_valid, bool _assoc_w_comp,
                                    float _line_thickness_GUI, List<string> _mLtext, bool _is_top_closure, bool _is_bottom_closure,
                                    List<long> _zoned_polygon_ids)
        {
            // find all polygons contained in the group (level)
            if (_zoned_polygon_ids == null) return null;
            if (_zoned_polygon_ids.Count < 1) return null;

            List<ZonedPolygon> level_polygons = new List<ZonedPolygon>();
            foreach(long id in _zoned_polygon_ids)
            {
                GeometricEntity ge_found = this.parsed_GE_before_adding_to_Layer.FirstOrDefault(x => x.ID == id);
                if (ge_found == null) continue;

                ZonedPolygon zp_found = ge_found as ZonedPolygon;
                if (zp_found == null) continue;

                level_polygons.Add(zp_found);
            }

            if (level_polygons.Count < 1) return null;

            // reconstruct the grouop (level)
            ZonedPolygonGroup created = new ZonedPolygonGroup(_id, _name, _color, _vis, _is_valid, _assoc_w_comp, _line_thickness_GUI, _mLtext, 
                                                              _is_top_closure, _is_bottom_closure, level_polygons);
            Entity.Nr_Entities = Math.Max(Entity.Nr_Entities, _id) + 1;
            return created;
        }

        internal ZonedVolume ReconstructZonedVolume(long _id, string _name, SharpDX.Color _color, EntityVisibility _vis, bool _is_valid, bool _assoc_w_comp,
                        float _line_thickness_GUI, List<string> _mLtext, bool _is_top_closure, bool _is_bottom_closure,
                        Layer _layer, bool _color_by_layer, List<ZonedPolygonGroup> _rulingLevels,
                        Dictionary<int, ComponentInteraction.Material> _material_per_level, Dictionary<Utils.Composite4IntKey, ComponentInteraction.Material> _material_per_label)
        {
            // reconstruct the volume
            ZonedVolume created = new ZonedVolume(_id, _name, _color, _vis, _is_valid, _assoc_w_comp,
                                                _line_thickness_GUI, _mLtext, _is_top_closure, _is_bottom_closure,
                                                _layer, _color_by_layer, _rulingLevels, _material_per_level, _material_per_label);
            Entity.Nr_Entities = Math.Max(Entity.Nr_Entities, _id) + 1;
            // setting the layer adds the ZonedVolume to the ContainedEntities of the layer (see DXFLayer)

            return created;
        }

        #endregion
    }
}
