using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Data;
using System.Globalization;
using Point3D = System.Windows.Media.Media3D.Point3D;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;

using GeometryViewer.Utils;
using GeometryViewer.EntityDXF;

namespace GeometryViewer.EntityGeometry
{
    public enum ZonedVolumeEditModeType { NONE, LEVEL_ADD, LEVEL_DELETE, MATERIAL_ASSIGN, ISBEING_DELETED, ISBEING_REASSOCIATED }
    public enum SegmentContainmentType { CONTAINED, OVERLAP_START, OVERLAP_END, DISJOINT}

    public class ZonedVolume : GeometricEntity
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ========================================== PRIVATE NESTED CLASSES ====================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region VOLUME QUAD INFO
        private class VolumeQuadInfo
        {
            private static long NR_VQI = 0;
            private long id;
            public long ID { get { return this.id; } }

            // relationship to the defining polygons
            private int label;
            public int Label { get { return this.label; } }
            
            private int polylow; // index of the lower polygon within the defining polygon list of the volume
            public int PolyLow { get { return this.polylow; } }
            
            private int polylowind; // index (NOT zone label) in the lower polygon
            public int PolyLowInd { get { return this.polylowind; } }

            private int polyhigh; // index of the upper polygon within the defining polygon list of the volume
            public int PolyHigh { get { return this.polyhigh; } }

            private int polyhighind; // index (NOT zone label) in the upper polygon
            public int PolyHighInd { get { return this.polyhighind; } }

            private int opening_index;
            public int OpeningIndex { get { return this.opening_index; } }

            private bool discrepancy_in_out;
            public bool DiscrepancyInOut { get { return this.discrepancy_in_out; } }

            // basic geometry
            private List<Vector3> quad;
            public ReadOnlyCollection<Vector3> Quad { get { return this.quad.AsReadOnly(); } }

            private double area;
            public double Area { get { return this.area; } }

            private Vector3 quadnormal;
            public Vector3 QuadNormal { get { return this.quadnormal; } }

            public float OffsetInner { get; set; }
            public float OffsetOuter { get; set; }

            // material
            private ComponentInteraction.Material assoc_material;
            public ComponentInteraction.Material AssocMaterial 
            {
                get { return this.assoc_material; }
                set 
                {
                    this.assoc_material = value;
                    this.OffsetInner = this.assoc_material.OffsetIn;
                    this.OffsetOuter = this.assoc_material.OffsetOut;
                } 
            }

            //.ctor
            public VolumeQuadInfo(int _label, int _polyLow, int _polyLow_ind, int _polyHigh, int _polyHigh_ind,
                bool _discrepancy_in_out, List<Vector3> _quad, double _area, int _opening_index = -1)
            {
                this.id = ++NR_VQI;
                this.label = _label;
                this.polylow = _polyLow;
                this.polylowind = _polyLow_ind;
                this.polyhigh = _polyHigh;
                this.polyhighind = _polyHigh_ind;
                this.opening_index = _opening_index;
                this.discrepancy_in_out = _discrepancy_in_out;
                this.quad = _quad;
                this.area = _area;
                this.quadnormal = MeshesCustom.GetPolygonNormalNewell(_quad);

                this.OffsetInner = 0.1f;
                this.OffsetOuter = 0.25f;
                this.AssocMaterial = ComponentInteraction.Material.Default;
            }

            public override string ToString()
            {
                string main_str = "VQI" + " polyL: " + this.PolyLow + " polyH: " + this.PolyHigh + " label: " + this.Label;
                if (this.opening_index > -1)
                    main_str += "opening: " + this.opening_index;
                
                return main_str;
            }
        }

        #endregion

        #region VOLUME LEVEL INFO
        private class VolumeLevelInfo
        {
            private static long NR_VLI = 1000;
            private long id;
            public long ID { get { return this.id; } }

            // level properties
            private int level_ind;
            public int LevelIndex { get { return this.level_ind; } }

            private bool is_top_closure;
            public bool IsTopClosure { get { return this.is_top_closure; } }

            private bool is_bottom_closure;
            public bool IsBottomClosure { get { return this.is_bottom_closure; } }

            private List<int> contained_poly_inds;
            public ReadOnlyCollection<int> ContainedPolyInds { get { return this.contained_poly_inds.AsReadOnly(); } }

            private int outermost_poly_ind;
            public int OutermostPolyInd { get { return this.outermost_poly_ind; } }

            private Vector3 levelnormal;
            public Vector3 LevelNormal { get { return this.levelnormal; } }

            // can be set after .ctor
            public float LevelArea { get; set; }

            // offsets
            private float offset_inner;
            public float OffsetInner
            {
                get { return this.offset_inner; }
                set
                {
                    if (this.is_top_closure)
                        this.offset_inner = -value;
                    else if (this.is_bottom_closure)
                        this.offset_inner = value;
                    else
                        this.offset_inner = 0f;
                }
            }

            private float offset_outer;
            public float OffsetOuter
            {
                get { return this.offset_outer; }
                set
                {
                    if (this.is_top_closure)
                        this.offset_outer = -value;
                    else if (this.is_bottom_closure)
                        this.offset_outer = value;
                    else
                        this.offset_outer = 0f;
                }
            }

            // material
            private ComponentInteraction.Material assoc_material;
            public ComponentInteraction.Material AssocMaterial
            {
                get { return this.assoc_material; }
                set
                {
                    this.assoc_material = value;
                    this.OffsetInner = this.assoc_material.OffsetIn;
                    this.OffsetOuter = this.assoc_material.OffsetOut;
                }
            }

            public VolumeLevelInfo(int _level_index, bool _is_top_closure, bool _is_bottom_closure, 
                                    List<int> _contained_poly_inds, int _outermost_poly_ind, Vector3 _normal)
            {
                this.id = ++NR_VLI;
                this.level_ind = _level_index;
                this.is_top_closure = _is_top_closure;
                this.is_bottom_closure = _is_bottom_closure;
                this.contained_poly_inds = _contained_poly_inds;
                this.outermost_poly_ind = _outermost_poly_ind;
                this.levelnormal = (_is_bottom_closure) ? _normal * -1f : _normal;

                this.OffsetInner = 0.1f;
                this.OffsetOuter = 0.25f;
                this.AssocMaterial = ComponentInteraction.Material.Default;
            }

        }

        #endregion

        #region SIMPLE POLYGON REPRESENTATION

        private class ZonedPolygonRep
        {
            private List<Vector4> coordinates; // [1-3]:X,Y,Z, 4: Label
            public List<Vector4> Coordinates { get { return this.coordinates; } }

            private int levelindex;
            public int LevelIndex { get { return this.levelindex; } }

            public ZonedPolygonRep(List<Vector4> _annotated_coords, int _level_ind)
            {
                this.coordinates = _annotated_coords;
                this.levelindex = _level_ind;
            }
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ======================================== PROPERTIES AND CLASS MEMBERS ================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region PROPERTIES: Structural

        private List<ZonedPolygonGroup> levels;
        public List<ZonedPolygonGroup> Levels
        {
            get { return this.levels; }
            set 
            {
                this.levels = value;
                this.IsValid = (this.levels.Count > 1);
                RegisterPropertyChanged("Levels");
            }
        }

        // if a ruling level changes, it is set to TRUE
        private bool isDirty;
        public bool IsDirty
        {
            get { return this.isDirty; }
            private set 
            { 
                this.isDirty = value;
                RegisterPropertyChanged("IsDirty");
            }
        }

        private int nr_changes_since_loading;
        public int NrChangesSinceLoading
        {
            get { return this.nr_changes_since_loading; }
            set 
            { 
                this.nr_changes_since_loading = value;
                this.RegisterPropertyChanged("NrChangesSinceLoading");
            }
        }


        private ZonedVolumeEditModeType editModeType;
        public ZonedVolumeEditModeType EditModeType
        {
            get { return this.editModeType; }
            set
            { 
                this.editModeType = value;
                RegisterPropertyChanged("EditModeType");
            }
        }

        #endregion

        #region PROPERTIES: MaxHeight, Perimeter, Area, Volume

        public float Elevation_NET { get; private set; }
        public float Elevation_GROSS { get; private set; }
        public float Elevation_AXES { get; private set; }
        public float Ceiling_NET { get; private set; }
        public float Ceiling_GROSS { get; private set; }
        public float Ceiling_AXES { get; private set; }
        public float MaxHeight_NET { get; private set; }
        public float MaxHeight_GROSS { get; private set; }
        public float MaxHeight_AXES { get; private set; }
        public float Perimeter { get; private set; }
        public float Area_NET { get; private set; }
        public float Area_GROSS { get; private set; }
        public float Area_AXES { get; private set; }
        public float Volume_NET { get; private set; }
        public float Volume_GROSS { get; private set; }
        public float Volume_AXES { get; private set; }

        #endregion

        #region PROPERTIES: Associated Components (multiple -> describing the volume, the construction of its surfaces and the HVAC/MEP components in it)

        private List<ComponentReps.CompRepInfo> associated_comp_reps;

        /// <summary>
        /// <para>Adds _cri to the list of associated representaions and alerts them, if an action needs to be taken.</para>
        /// <para>E.g. the space describing representation adds a placable representation to its references.</para>
        /// </summary>
        /// <param name="_cri"></param>
        public void AddCompRepAssociation(ComponentReps.CompRepInfo _cri)
        {
            if (_cri == null) return;

            // avoid duplicates
            ComponentReps.CompRepInfo duplicate = this.associated_comp_reps.FirstOrDefault(x => x.CR_ID == _cri.CR_ID);
            if (duplicate != null) return;

            // avoid duplicate volume descriptions
            ComponentReps.CompRepInfo duplicated_descr = this.associated_comp_reps.FirstOrDefault(x => x is ComponentReps.CompRepDescirbes);
            if ((_cri is ComponentReps.CompRepDescirbes) && (duplicated_descr != null)) return;

            this.associated_comp_reps.Add(_cri);
            if (this.associated_comp_reps.Count > 0)
                this.AssociatedWComp = true;
            this.RegisterPropertyChanged("CompRep");

            // announce the change to the describing component
            if (_cri is ComponentReps.CompRepContainedIn)
            {
                // CASE 1: a new placement
                List<ComponentReps.CompRepDescirbes> descr = this.associated_comp_reps.Where(x => x is ComponentReps.CompRepDescirbes).Select(x => x as ComponentReps.CompRepDescirbes).ToList();
                if (descr != null && descr.Count > 0)
                {
                    ComponentReps.CompRepDescirbes main_descriptor = descr[0];
                    main_descriptor.TakeReference(_cri);
                }
            }
            else if(_cri is ComponentReps.CompRepDescirbes)
            {
                // CASE 2: a new descriptor
                List<ComponentReps.CompRepInfo> content = this.associated_comp_reps.Where(x => x is ComponentReps.CompRepContainedIn).ToList();
                if (content != null && content.Count > 0)
                {
                    _cri.TakeReferences(content);
                }
            }
        }

        /// <summary>
        /// <para>Remove _cri from the list of associated representaions and alerts them, if an action needs to be taken.</para>
        /// <para>E.g. the space describing representation removes a placable representation from its references.</para>
        /// </summary>
        /// <param name="_cri"></param>
        public void RemoveCompRepAssociation(ComponentReps.CompRepInfo _cri)
        {
            if (_cri == null) return;

            bool success = this.associated_comp_reps.Remove(_cri);
            if (this.associated_comp_reps.Count == 0)
                this.AssociatedWComp = false;
            if (success)
                this.RegisterPropertyChanged("CompRep");

            // announce the change to the describing component
            if (_cri is ComponentReps.CompRepContainedIn)
            {
                // CASE 1: a placement is removed
                List<ComponentReps.CompRepDescirbes> descr = this.associated_comp_reps.Where(x => x is ComponentReps.CompRepDescirbes).Select(x => x as ComponentReps.CompRepDescirbes).ToList();
                if (descr != null && descr.Count > 0)
                {
                    ComponentReps.CompRepDescirbes main_descriptor = descr[0];
                    main_descriptor.DiscardReference(_cri);
                }
            }
            else if (_cri is ComponentReps.CompRepDescirbes)
            {
                // CASE 2: the descriptor is removed
                List<ComponentReps.CompRepInfo> content = this.associated_comp_reps.Where(x => x is ComponentReps.CompRepContainedIn).ToList();
                if (content != null && content.Count > 0)
                {
                    _cri.DiscradReferences(content);
                }
            }
        }

        public ComponentReps.CompRepInfo GetFirstAssocComp()
        {
            if (this.associated_comp_reps == null) return null;
            if (this.associated_comp_reps.Count == 0) return null;

            return this.associated_comp_reps[0];
        }

        public ComponentReps.CompRepInfo GetDescribingCompOrFirst()
        {
            if (this.associated_comp_reps == null) return null;
            if (this.associated_comp_reps.Count == 0) return null;

            foreach(ComponentReps.CompRepInfo cri in this.associated_comp_reps)
            {
                if (cri is ComponentReps.CompRepDescirbes)
                    return cri;
            }

            return this.associated_comp_reps[0];
        }

        #endregion

        #region PRIVATE CLASS MEMBERS

        // GEOMETRY DEFINITIONS
        private List<ZonedPolygon> defining_polygons;
        public List<ZonedPolygon> Defining_Polygons { get { return this.defining_polygons; } }
        private Vector3 volume_pivot;
        public Vector3 Volume_Pivot { get { return this.volume_pivot; } }
        // [[polygon 0 id, polygon 1 id, index of segment in polygon 0], quad coordiantes]
        private Dictionary<Vector3, List<Vector3>> wallQuads;
        private Dictionary<Vector3, List<List<Vector3>>> wallQuadsOpenings;
        private List<MeshGeometry3D> surfaces;
        private List<double> surface_opacity;

        // information for showing inner and outer surfaces and assigning components
        private List<VolumeLevelInfo> levels_info; // nr or sequence DO NOT CHANGE after a polygon update
        private List<VolumeQuadInfo> wallQuads_info; // nr or sequence DO CHANGE after a polygon update
        private List<VolumeQuadInfo> wallOpenings_info; // nr or sequence DO CHANGE after a polygon update

        private List<ZonedPolygonRep> defining_polygons_IN;
        private List<ZonedPolygonRep> defining_polygons_OUT;
        private Dictionary<Vector3, List<Vector3>> wallQuads_IN;
        private Dictionary<Vector3, List<Vector3>> wallQuads_OUT;

        // Material Editing
        private List<ZonedVolumeSurfaceVis> surface_vis;
        private List<ZonedVolumeSurfaceVis> opening_vis;
        private Dictionary<int, ComponentInteraction.Material> materials_per_level;
        // key components: Label, PolyLow, PolyHigh, OpeningInd or -1
        private Dictionary<Composite4IntKey, ComponentInteraction.Material> materials_per_label;

        // GEOMETRY VISUALIZATION: defining surfaces
        private MeshGeometry3D mesh;
        private MeshGeometry3D mesh_wO;
        private LineGeometry3D linesSelect;

        // GEOMETRY VISUALIZATION: inner and outer surfaces
        private MeshGeometry3D mesh_IN;
        private MeshGeometry3D mesh_OUT;
        private LineGeometry3D lines_offset_IN;
        private LineGeometry3D lines_offset_OUT;

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ================================================ INITIALIZERS ========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region .CTOR
        public ZonedVolume(Layer _layer, List<ZonedPolygonGroup> _rulingLevels) : base(_layer)
        {
            this.HasGeometryType = EnityGeometryType.MESH;
            this.Levels = new List<ZonedPolygonGroup>(_rulingLevels);
            this.Levels.Sort(new ZonedPolygonGroupHeightComparer());
            foreach (ZonedPolygonGroup zpg in this.Levels)
            {
                zpg.PropertyChanged += level_PropertyChanged;
            }

            this.associated_comp_reps = new List<ComponentReps.CompRepInfo>();

            this.NrChangesSinceLoading = 0;
            this.EditModeType = ZonedVolumeEditModeType.NONE;
            this.OnCreation();
        }

        public ZonedVolume(Layer _layer, List<ZonedPolygon> _rulingPolygons) : base(_layer)
        {
            this.HasGeometryType = EnityGeometryType.MESH;
            this.Levels = new List<ZonedPolygonGroup>();
            if (_rulingPolygons == null || _rulingPolygons.Count < 2)
            {
                this.IsValid = false;
                return;
            }

            int n = _rulingPolygons.Count;
            List<ZonedPolygonGroup> levels_tmp = new List<ZonedPolygonGroup>();
            for (int i = 0; i < n; i++ )
            {
                ZonedPolygonGroup zpg = new ZonedPolygonGroup("zpg_" + i, new List<ZonedPolygon> { _rulingPolygons[i] });
                zpg.PropertyChanged += level_PropertyChanged;
                levels_tmp.Add(zpg);
            }
            this.Levels = new List<ZonedPolygonGroup>(levels_tmp);
            this.Levels.Sort(new ZonedPolygonGroupHeightComparer());
            // make the bottom-most level the bottom closure and the top-most level the top closure:
            this.Levels[0].IsBottomClosure = true;
            this.Levels[this.Levels.Count - 1].IsTopClosure = true;

            this.associated_comp_reps = new List<ComponentReps.CompRepInfo>();

            this.NrChangesSinceLoading = 0;
            this.EditModeType = ZonedVolumeEditModeType.NONE;
            this.OnCreation();
        }

        public ZonedVolume(Layer _layer, ZonedPolygon _base, Vector3 _offset) : base(_layer)
        {
            this.HasGeometryType = EnityGeometryType.MESH;
            this.Levels = new List<ZonedPolygonGroup>();
            if (_base == null)
            {
                this.IsValid = false;
                return;
            }

            ZonedPolygonGroup zpg_0 = new ZonedPolygonGroup("zpg_0", new List<ZonedPolygon> { _base });
            zpg_0.PropertyChanged += level_PropertyChanged;

            ZonedPolygonGroup zpg_1 = new ZonedPolygonGroup("zpg_1", new List<ZonedPolygon> { new ZonedPolygon(_base, _offset) });
            zpg_1.PropertyChanged += level_PropertyChanged;

            this.Levels = new List<ZonedPolygonGroup> { zpg_0, zpg_1 };
            this.Levels.Sort(new ZonedPolygonGroupHeightComparer());
            // make the bottom-most level the bottom closure and the top-most level the top closure:
            this.Levels[0].IsBottomClosure = true;
            this.Levels[this.Levels.Count - 1].IsTopClosure = true;

            this.associated_comp_reps = new List<ComponentReps.CompRepInfo>();

            this.NrChangesSinceLoading = 0;
            this.EditModeType = ZonedVolumeEditModeType.NONE;
            this.OnCreation();
        }

        public ZonedVolume(string _name, Layer _layer, List<ZonedPolygonGroup> _rulingLevels)
            : base(_name, _layer)
        {
            this.HasGeometryType = EnityGeometryType.MESH;
            this.Levels = new List<ZonedPolygonGroup>(_rulingLevels);
            this.Levels.Sort(new ZonedPolygonGroupHeightComparer());
            foreach (ZonedPolygonGroup zpg in this.Levels)
            {
                zpg.PropertyChanged += level_PropertyChanged;
            }

            this.associated_comp_reps = new List<ComponentReps.CompRepInfo>();

            this.NrChangesSinceLoading = 0;
            this.EditModeType = ZonedVolumeEditModeType.NONE;
            this.OnCreation();
        }

        public override string GetDafaultName()
        {
            return "Zoned Volume";
        }

        #endregion

        #region PARSING .CTOR

        internal ZonedVolume(long _id, string _name, SharpDX.Color _color, EntityVisibility _vis, bool _is_valid, bool _assoc_w_comp,
                        float _line_thickness_GUI, List<string> _mLtext, bool _is_top_closure, bool _is_bottom_closure,
                        Layer _layer, bool _color_by_layer, List<ZonedPolygonGroup> _rulingLevels,
                        Dictionary<int, ComponentInteraction.Material> _material_per_level, Dictionary<Composite4IntKey, ComponentInteraction.Material> _material_per_label)
            : base(_id, _name, _color, _vis, _is_valid, _assoc_w_comp, _line_thickness_GUI, _mLtext, _is_top_closure, _is_bottom_closure, _layer, _color_by_layer)
        {
            this.HasGeometryType = EnityGeometryType.MESH;
            this.Levels = new List<ZonedPolygonGroup>(_rulingLevels);
            this.Levels.Sort(new ZonedPolygonGroupHeightComparer());
            foreach (ZonedPolygonGroup zpg in this.Levels)
            {
                zpg.PropertyChanged += level_PropertyChanged;
            }

            this.associated_comp_reps = new List<ComponentReps.CompRepInfo>();

            this.NrChangesSinceLoading = 0;
            this.EditModeType = ZonedVolumeEditModeType.NONE;
            this.OnCreation();

            this.ParseMaterialAssociations(_material_per_level, _material_per_label);
            // (re)calculate OFFSETS
            this.CalculatePolygonOffsets();
            this.CalculateWallOffsets(); 
        }
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // =============================================== CLASS METHODS ========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region EVENT HANDLER: Levels

        void level_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ZonedPolygonGroup zpg = sender as ZonedPolygonGroup;
            if (e == null || zpg == null)
                return;

            if (e.PropertyName == "IsDirty")
            {
                if (zpg.IsDirty)
                {
                    this.ResetAllGeometry();
                }
            }
            else if (e.PropertyName == "IsValid")
            {
                if (!zpg.IsValid)
                    this.IsValid = false;              
            }
            else if (e.PropertyName == "OpeningsDirty")
            {
                if (zpg.OpeningsDirty)
                {
                    // reset only the wall quads with opeings
                    this.wallQuadsOpenings = new Dictionary<Vector3, List<List<Vector3>>>();
                    this.mesh_wO = null;

                    this.IsDirty = true;
                }
            }

        }

        #endregion

        #region CLEAN-UP BEFORE DELETING

        public void ReleaseLevels()
        {
            foreach (ZonedPolygonGroup zpg in this.Levels)
            {
                zpg.ReleasePolygons(); // normalizes opening geometry
                zpg.PropertyChanged -= level_PropertyChanged;
            }
            this.Levels = new List<ZonedPolygonGroup>();
        }

        #endregion

        #region GEOMETRY (RE)GENERATION: on Creation OR On Adaptation to Change

        private void OnCreation()
        {
            if (!this.IsValid) return;

            // analyse the relationships btw the levels
            this.GetDefiningPolygonsAndLevelInfo(); // ordered ascending acc. to height (Y-axis)
            this.CalculateVolumePivot();

            // build the wall quads and adapt the polygon opening geometry of the defining polygons, 
            // if the wall quads are not vertical
            this.CalculateVolumeWalls();

            // (re)calculate OFFSETS
            this.CalculatePolygonOffsets();
            this.CalculateWallOffsets();
        }

        private void ResetAllGeometry()
        {
            // save materials before resetting geometry
            this.SaveMaterialAssociations();
            // reset geometry
            this.defining_polygons = new List<ZonedPolygon>();
            this.wallQuads = new Dictionary<Vector3, List<Vector3>>();
            this.wallQuadsOpenings = new Dictionary<Vector3, List<List<Vector3>>>();
            this.surfaces = new List<MeshGeometry3D>();
            this.surface_opacity = new List<double>();

            this.levels_info = new List<VolumeLevelInfo>();
            this.wallQuads_info = new List<VolumeQuadInfo>();
            this.wallOpenings_info = new List<VolumeQuadInfo>();
            // material_per_label DOES NOT GET RESET
            this.defining_polygons_IN = new List<ZonedPolygonRep>();
            this.defining_polygons_OUT = new List<ZonedPolygonRep>();
            this.wallQuads_IN = new Dictionary<Vector3, List<Vector3>>();
            this.wallQuads_OUT = new Dictionary<Vector3, List<Vector3>>();

            this.mesh = null;
            this.mesh_wO = null;
            this.linesSelect = null;

            this.mesh_IN = null;
            this.mesh_OUT = null;
            this.lines_offset_IN = null;
            this.lines_offset_OUT = null;

            this.IsDirty = true;
        }

        internal void ReCreateAllGeometry()
        {
            this.ResetAllGeometry();
            this.OnCreation();
        }

        #endregion

        #region GEOMETRY EXTRACTION (Defining Surfaces)

        public MeshGeometry3D BuildVolume()
        {
            if (this.mesh == null && this.IsValid)
            {
                // get all quads to be meshed
                if (this.wallQuads == null || this.wallQuads.Count < 1)
                    this.CalculateVolumeWalls();
                
                // create mesh
                MeshGeometry3D sides = MeshesCustom.MeshFromQuads(this.wallQuads.Values.ToList());

                this.Levels[0].BuildLevelFill(false);
                MeshGeometry3D bottom = this.Levels[0].Fill;

                int nrL = this.Levels.Count;
                this.Levels[nrL - 1].BuildLevelFill(true);
                MeshGeometry3D top = this.Levels[nrL - 1].Fill;

                this.mesh = MeshesCustom.CombineMeshes(new List<MeshGeometry3D> { sides, bottom, top });
            }

            this.IsDirty = false;
            return this.mesh;
        }

        public MeshGeometry3D BuildVolumeWOpenings()
        {
            if (this.mesh_wO == null && this.IsValid)
            {
                // get all quads to be meshed
                if (this.wallQuadsOpenings == null || this.wallQuadsOpenings.Count < 1)
                    this.CalculateVolumeWalls();

                // create mesh: the quads for each wall as a separate mesh for sorting
                List<MeshGeometry3D> surfaces = new List<MeshGeometry3D>();

                // SIMPLE ALGORITHM: relying on rectangular openings ---------------------------------|
                foreach (var entry in this.wallQuadsOpenings)
                {
                    surfaces.Add(MeshesCustom.MeshFromQuads(entry.Value));
                }
                // SIMPLE ALGORITHM ------------------------------------------------------------------|

                ////// COMPLEX ALGORITHM: relying on triangulation of arbitrary polygon groups (buggy) ---|
                ////foreach (var entry in this.wallQuads)
                ////{
                ////    Vector3 ids = entry.Key;
                ////    long id_poly_lower = (long)ids.X;
                ////    long id_poly_upper = (long)ids.Y;
                ////    int label = (int)ids.Z;

                ////    List<Vector3> coords = entry.Value;
                ////    List<List<Vector3>> opening_coords = null;

                ////    // get the opening coords, if any openings exist
                ////    ZonedPolygon zp_lower = this.defining_polygons.Find(x => (x.ID == id_poly_lower));
                ////    if (zp_lower != null)
                ////        opening_coords = zp_lower.GetOpeningGeometryForLabel(label); // if none: empty list, not NULL

                ////    List<Point3D> coords_P3D = Utils.CommonExtensions.ConvertVector3ListToPoint3DList(coords);
                ////    List<List<Point3D>> opening_coords_P3D = (opening_coords.Count == 0) ? null : Utils.CommonExtensions.ConvertVector3ListListToPoint3DListList(opening_coords);
                ////    bool reverse = (opening_coords.Count == 0) ? true : false;
                ////    MeshGeometry3D displayMesh = MeshesCustom.PolygonComplexFillAfterReOrientation(coords_P3D, opening_coords_P3D, reverse);

                ////    surfaces.Add(displayMesh);
                ////}
                ////// COMPLEX ALGORITHM -----------------------------------------------------------------|

                // show only the levels defined as a bottom or top closure
                foreach(VolumeLevelInfo vli in this.levels_info)
                {
                    if (vli.IsBottomClosure)
                    {
                        this.Levels[vli.LevelIndex].BuildLevelFill(false);
                        surfaces.Add(this.Levels[vli.LevelIndex].Fill);
                    }
                    else if (vli.IsTopClosure)
                    {
                        this.Levels[vli.LevelIndex].BuildLevelFill(true);
                        surfaces.Add(this.Levels[vli.LevelIndex].Fill);
                    }
                }

                this.mesh_wO = MeshesCustom.SortAndCombineMeshes(surfaces);
                
            }

            this.IsDirty = false;
            return this.mesh_wO;
        }

        public LineGeometry3D BuildSelectionGeometry()
        {
            if (this.linesSelect == null && this.IsValid)
            {
                // get all WALL quads               
                if (this.wallQuads == null || this.wallQuads.Count < 1)
                    this.CalculateVolumeWalls();

                ////// SIMPLE: full quads-----------------------------------------------------------------|
                ////if (this.wallQuads != null && this.wallQuads.Count >= 1)
                ////{
                ////    LineBuilder b = new LineBuilder();
                ////    foreach (List<Vector3> quad in this.wallQuads.Values)
                ////    {
                ////        int qn = quad.Count;
                ////        for (int j = 0; j < qn; j++)
                ////        {
                ////            b.AddLine(quad[j], quad[(j + 1) % qn]);
                ////        }
                ////    }

                ////    this.linesSelect = b.ToLineGeometry3D();
                ////}
                ////else
                ////    this.linesSelect = null;
                ////// SIMPLE ----------------------------------------------------------------------------|
                
                // COMPLEX: show wall subdivisions ---------------------------------------------------|
                if (this.wallQuadsOpenings != null && this.wallQuadsOpenings.Count >= 1)
                {
                    LineBuilder b = new LineBuilder();
                    foreach(List<List<Vector3>> quad_collection in this.wallQuadsOpenings.Values)
                    {
                        foreach (List<Vector3> quad in quad_collection)
                        {
                            int qn = quad.Count;
                            for (int j = 0; j < qn; j++)
                            {
                                b.AddLine(quad[j], quad[(j + 1) % qn]);
                            }
                        }
                    }
                    this.linesSelect = b.ToLineGeometry3D();
                }
                else
                    this.linesSelect = null;
                // COMPLEX ---------------------------------------------------------------------------|
            }

            return this.linesSelect;
        }

        public void AddSelectionGeometry(ref LineBuilder _lb)
        {
            if (_lb == null || !this.IsValid)
                return;

            // get all WALL quads               
            if (this.wallQuads == null || this.wallQuads.Count < 1)
                this.CalculateVolumeWalls();

            foreach (List<Vector3> quad in this.wallQuads.Values)
            {
                int qn = quad.Count;
                for (int j = 0; j < qn; j++)
                {
                    _lb.AddLine(quad[j], quad[(j + 1) % qn]);
                }
            }            
        }


        #endregion

        #region GEOMETRY EXTRACTION (Outer & Inner Surfaces)

        public MeshGeometry3D BuildOuterHull()
        {
            if (this.mesh_OUT == null)
            {
                // get all quads to be meshed
                if (this.wallQuads_OUT == null || this.wallQuads_OUT.Count < 1)
                    this.OnCreation();

                // create mesh of SIDES
                MeshGeometry3D sides = MeshesCustom.MeshFromQuads(this.wallQuads_OUT.Values.ToList());
                
                // create the meshes of the LOWEST and HIGHEST LEVELS
                int nrL = this.Levels.Count;
                List<List<Point3D>> level0_polys = new List<List<Point3D>>();
                List<List<Point3D>> levelN_polys = new List<List<Point3D>>();
                foreach(var entry in this.defining_polygons_OUT)
                {
                    if (entry.LevelIndex == 0)
                        level0_polys.Add(CommonExtensions.ConvertVector4ListToPoint3DList( entry.Coordinates));
                    else if (entry.LevelIndex == nrL - 1)
                        levelN_polys.Add(CommonExtensions.ConvertVector4ListToPoint3DList(entry.Coordinates));
                }

                // build the offset level meshes
                int indB0, indBN;
                List<Point3D> big0, bigN;
                List<List<Point3D>> holes0, holesN;

                MeshesCustom.ToPolygonWithHoles(level0_polys, Orientation.XZ, out indB0, out big0, out holes0);
                MeshesCustom.ToPolygonWithHoles(levelN_polys, Orientation.XZ, out indBN, out bigN, out holesN);

                MeshGeometry3D bottom = MeshesCustom.PolygonComplexFill(big0, holes0, false);
                MeshGeometry3D top = MeshesCustom.PolygonComplexFill(bigN, holesN, true);

                this.mesh_OUT = MeshesCustom.CombineMeshes(new List<MeshGeometry3D> { sides, bottom, top });
            }

            return this.mesh_OUT;
        }

        public MeshGeometry3D BuildInnerHull()
        {
            if (this.mesh_IN == null)
            {
                // get all quads to be meshed
                if (this.wallQuads_IN == null || this.wallQuads_IN.Count < 1)
                    this.OnCreation();

                // create mesh of SIDES
                MeshGeometry3D sides = MeshesCustom.MeshFromQuads(this.wallQuads_IN.Values.ToList());

                // create the meshes of the LOWEST and HIGHEST LEVELS
                int nrL = this.Levels.Count;
                List<List<Point3D>> level0_polys = new List<List<Point3D>>();
                List<List<Point3D>> levelN_polys = new List<List<Point3D>>();
                foreach (var entry in this.defining_polygons_IN)
                {
                    if (entry.LevelIndex == 0)
                        level0_polys.Add(CommonExtensions.ConvertVector4ListToPoint3DList(entry.Coordinates));
                    else if (entry.LevelIndex == nrL - 1)
                        levelN_polys.Add(CommonExtensions.ConvertVector4ListToPoint3DList(entry.Coordinates));
                }

                // build the offset level meshes
                int indB0, indBN;
                List<Point3D> big0, bigN;
                List<List<Point3D>> holes0, holesN;

                MeshesCustom.ToPolygonWithHoles(level0_polys, Orientation.XZ, out indB0, out big0, out holes0);
                MeshesCustom.ToPolygonWithHoles(levelN_polys, Orientation.XZ, out indBN, out bigN, out holesN);

                MeshGeometry3D bottom = MeshesCustom.PolygonComplexFill(big0, holes0, false);
                MeshGeometry3D top = MeshesCustom.PolygonComplexFill(bigN, holesN, true);

                this.mesh_IN = MeshesCustom.CombineMeshes(new List<MeshGeometry3D> { sides, bottom, top });
            }

            return this.mesh_IN;
        }

        public LineGeometry3D BuildOuterOffsetLines()
        {
            if (this.lines_offset_OUT == null)
            {
                // get all quads to be meshed
                if (this.wallQuads_OUT == null || this.wallQuads_OUT.Count < 1)
                    this.OnCreation();

                if (this.wallQuads_OUT != null && this.wallQuads_OUT.Count >= 1)
                {
                    LineBuilder b = new LineBuilder();
                    foreach (List<Vector3> quad in this.wallQuads_OUT.Values)
                    {
                        int qn = quad.Count;
                        for (int j = 0; j < qn; j++)
                        {
                            b.AddLine(quad[j], quad[(j + 1) % qn]);
                        }
                    }

                    this.lines_offset_OUT = b.ToLineGeometry3D();
                }
                else
                    this.lines_offset_OUT = null;

            }

            return this.lines_offset_OUT;
        }

        public LineGeometry3D BuildInnerOffsetLines()
        {
            if (this.lines_offset_IN == null)
            {
                // get all quads to be meshed
                if (this.wallQuads_IN == null || this.wallQuads_IN.Count < 1)
                    this.OnCreation();

                if (this.wallQuads_IN != null && this.wallQuads_IN.Count >= 1)
                {
                    LineBuilder b = new LineBuilder();
                    foreach (List<Vector3> quad in this.wallQuads_IN.Values)
                    {
                        int qn = quad.Count;
                        for (int j = 0; j < qn; j++)
                        {
                            b.AddLine(quad[j], quad[(j + 1) % qn]);
                        }
                    }

                    this.lines_offset_IN = b.ToLineGeometry3D();
                }
                else
                    this.lines_offset_IN = null;

            }

            return this.lines_offset_IN;
        }

        #endregion

        #region GEOMETRY EXTRACTION (For Single Surface Selection)

        /// <summary>
        /// Populates a transient internal list. Called only when the volume is selected.
        /// </summary>
        /// <returns></returns>
        public List<ZonedVolumeSurfaceVis> ExtractSurfacesForDisplay()
        {
            if (this.surface_vis == null)
                this.surface_vis = new List<ZonedVolumeSurfaceVis>();

            int nrSV = this.surface_vis.Count;
            int nrQ = this.wallQuads_info.Count;
            int nrL = this.levels_info.Count;

            // transfer the quads
            for(int i = 0; i < nrQ; i++)
            {
                if (i < nrSV)
                {
                    // use an existing surface visualizer
                    this.surface_vis[i].AdjustToVolumeChange(true, this.wallQuads_info[i].ID, this.wallQuads_info[i].Label, 
                                                                   this.wallQuads_info[i].Quad, (float) Math.Abs(this.wallQuads_info[i].Area));
                    this.surface_vis[i].Label = "Surface " + (i + 1).ToString();
                    this.surface_vis[i].AssocMaterial = this.wallQuads_info[i].AssocMaterial;
                }
                else
                {
                    // create a new surface visualizer
                    ZonedVolumeSurfaceVis zvsv = new ZonedVolumeSurfaceVis(this, true, this.wallQuads_info[i].ID, this.wallQuads_info[i].Label,
                                                                                       this.wallQuads_info[i].Quad, (float)Math.Abs(this.wallQuads_info[i].Area));
                    zvsv.Label = "Surface " + (i + 1).ToString();
                    zvsv.AssocMaterial = this.wallQuads_info[i].AssocMaterial;
                    this.surface_vis.Add(zvsv);
                }
            }
            nrSV = this.surface_vis.Count;

            // transfer the levels
            for (int i = 0; i < nrL; i++)
            {
                if (nrQ + i < nrSV)
                {
                    // use an existing surface visualizer
                    this.surface_vis[nrQ + i].AdjustToVolumeChange(false, this.levels_info[i].ID, this.levels_info[i].LevelIndex,
                        CommonExtensions.ConvertPoints3DListToVector3List(this.defining_polygons[this.levels_info[i].OutermostPolyInd].Polygon_Coords),
                        this.levels_info[i].LevelArea);
                    this.surface_vis[nrQ + i].Label = "Surface " + (nrQ + i + 1).ToString();
                    this.surface_vis[nrQ + i].AssocMaterial = this.levels_info[i].AssocMaterial;
                }
                else
                {
                    // create a new surface visualizer
                    ZonedVolumeSurfaceVis zvsv = new ZonedVolumeSurfaceVis(this, false, this.levels_info[i].ID, this.levels_info[i].LevelIndex,
                        CommonExtensions.ConvertPoints3DListToVector3List(this.defining_polygons[this.levels_info[i].OutermostPolyInd].Polygon_Coords),
                        this.levels_info[i].LevelArea);
                    zvsv.Label = "Surface " + (nrQ + i + 1).ToString();
                    zvsv.AssocMaterial = this.levels_info[i].AssocMaterial;
                    this.surface_vis.Add(zvsv);
                }
            }

            return this.surface_vis;
        }

        /// <summary>
        ///  Populates a transient internal list. Called only when the volume is selected.
        /// </summary>
        /// <returns></returns>
        public List<ZonedVolumeSurfaceVis> ExtractOpeningsForDisplay()
        {
            if (this.opening_vis == null)
                this.opening_vis = new List<ZonedVolumeSurfaceVis>();

            int nrOV = this.opening_vis.Count;
            int nrOP = this.wallOpenings_info.Count;

            // transfer the quads
            for (int i = 0; i < nrOP; i++)
            {
                if (i < nrOV)
                {
                    // use an existing surface visualizer
                    this.opening_vis[i].AdjustToVolumeChange(true, this.wallOpenings_info[i].ID, this.wallOpenings_info[i].Label,
                                                                   this.wallOpenings_info[i].Quad, (float)Math.Abs(this.wallOpenings_info[i].Area), this.wallOpenings_info[i].OpeningIndex);
                    this.opening_vis[i].Label = "Opening " + (i + 1).ToString();
                    this.opening_vis[i].AssocMaterial = this.wallOpenings_info[i].AssocMaterial;
                }
                else
                {
                    // create a new surface visualizer
                    ZonedVolumeSurfaceVis zvsv = new ZonedVolumeSurfaceVis(this, true, this.wallOpenings_info[i].ID, this.wallOpenings_info[i].Label,
                                                                                       this.wallOpenings_info[i].Quad, (float)Math.Abs(this.wallOpenings_info[i].Area), 
                                                                                       this.wallOpenings_info[i].OpeningIndex);
                    zvsv.Label = "Opening " + (i + 1).ToString();
                    zvsv.AssocMaterial = this.wallOpenings_info[i].AssocMaterial;
                    this.opening_vis.Add(zvsv);
                }
            }

            return this.opening_vis;
        }

        /// <summary>
        ///  Populates transient internal lists. Called only when the volume is selected.
        /// </summary>
        /// <returns></returns>
        public List<ZonedVolumeSurfaceVis> ExtractSurfacesAndOpeningsForDisplay()
        {
            List<ZonedVolumeSurfaceVis> surfaces = this.ExtractSurfacesForDisplay();
            List<ZonedVolumeSurfaceVis> openings = this.ExtractOpeningsForDisplay();

            List<ZonedVolumeSurfaceVis> all = new List<ZonedVolumeSurfaceVis>();
            all.AddRange(surfaces);
            all.AddRange(openings);
            return all;
        }

        #endregion

        #region INFO: Polygons, Levels, Pivot, Local Coordinate Sysem

        private void GetDefiningPolygonsAndLevelInfo()
        {
            List<ZonedPolygon> polygons = new List<ZonedPolygon>();
            List<List<ZonedPolygon>> polygons_per_level = new List<List<ZonedPolygon>>();
            List<long> outermost_polygon_id_per_level = new List<long>();
            List<bool> level_is_top_closure = new List<bool>();
            List<bool> level_is_bottom_closure = new List<bool>();
            foreach(ZonedPolygonGroup level in this.Levels)
            {
                List<ZonedPolygon> level_polys = level.ExtractZonedPolygons();
                polygons.AddRange(level_polys);
                polygons_per_level.Add(level_polys);
                outermost_polygon_id_per_level.Add(level_polys[level.OuterPolygonIndex].ID);
                level_is_top_closure.Add(level.IsTopClosure);
                level_is_bottom_closure.Add(level.IsBottomClosure);
            }

            // order the polygons according to position along the Y-axis 
            // (unstable sort!): order of equal elements is not preserved
            polygons.Sort(new ZonedPolygonComplexComparer());
            this.defining_polygons = new List<ZonedPolygon>(polygons);

            // record the level information
            this.levels_info = new List<VolumeLevelInfo>();
            int nrL = polygons_per_level.Count;
            for (int i = 0; i < nrL; i++ )
            {
                List<ZonedPolygon> poly_group = polygons_per_level[i];
                List<int> indices = new List<int>();
                int outermost_poly_index = -1;
                if (poly_group.Count > 0)
                {
                    outermost_poly_index = this.defining_polygons.FindIndex(x => x.ID == outermost_polygon_id_per_level[i]);
                    foreach (ZonedPolygon zp in poly_group)
                    {
                        int index = this.defining_polygons.FindIndex(x => x.ID == zp.ID);
                        if (index > -1 && index < this.defining_polygons.Count)
                            indices.Add(index);
                    }
                    this.levels_info.Add(new VolumeLevelInfo(i, level_is_top_closure[i], level_is_bottom_closure[i],
                                                                indices, outermost_poly_index, poly_group[0].GetPolygonNormalNewell()));
                }
            }

            
        }

        private void CalculateVolumePivot()
        {
            if (this.defining_polygons == null || this.defining_polygons.Count < 1)
                return;

            this.volume_pivot = Vector3.Zero;
            int n = this.defining_polygons.Count;
            for (int i = 0; i < n; i++)
            {
                this.volume_pivot += this.defining_polygons[i].GetPivot();
            }
            this.volume_pivot /= n;
        }

        public List<Vector3> GetAxes()
        {
            List<Vector3> axes = new List<Vector3>();
            // get the level with largest area and its longest side as the X-axis
            Vector3 axisX = Vector3.UnitX;
            double max_area = this.Levels.Max(x => x.GetAreaWoHoles());
            ZonedPolygonGroup zpg = this.Levels.FirstOrDefault(x => x.GetAreaWoHoles() == max_area);
            if (zpg != null)
            {
                axisX = zpg.GetUnitMajorOrientation();
            }
            axes.Add(axisX);

            // Y-axis is assumed to be vertical
            axes.Add(Vector3.UnitY);

            // Z-axis automatically calculated
            Vector3 axisZ = Vector3.Cross(Vector3.UnitY, axisX);
            axisZ.Normalize();
            axes.Add(axisZ);

            return axes;
        }

        #endregion

        #region INFO : Perimeter, Area, Volume

        // call at the end of CalculatePolygonOffsets()
        private void CalculatePerimeterAreaVolume()
        {
            // NET (NETTO) + GROSS (BRUTTO)
            this.Elevation_NET = 0f;
            this.Ceiling_NET = 0f;
            this.MaxHeight_NET = 0f;
            this.Perimeter = 0f;
            this.Area_NET = 0f;
            this.Volume_NET = 0f;

            this.Elevation_GROSS = 0f;
            this.Ceiling_GROSS = 0f;
            this.MaxHeight_GROSS = 0f;
            this.Area_GROSS = 0f;
            this.Volume_GROSS = 0f;

            List<Vector3> level_pivots_IN = new List<Vector3>();
            List<float> level_areas_NET = new List<float>();
            List<Vector3> level_pivots_OUT = new List<Vector3>();
            List<float> level_areas_GROSS = new List<float>();

            for (int i = 0; i < this.Levels.Count; i++)
            {
                // NET
                List<ZonedPolygonRep> level_polys_IN = this.defining_polygons_IN.FindAll(x => x.LevelIndex == i);
                if (level_polys_IN != null && level_polys_IN.Count > 0)
                {
                    // convert the polygon representations into coordinates:
                    List<List<Point3D>> an_coords = new List<List<Point3D>>();
                    foreach (ZonedPolygonRep zpr in level_polys_IN)
                    {
                        an_coords.Add(CommonExtensions.ConvertVector4ListToPoint3DList(zpr.Coordinates));
                    }
                    level_pivots_IN.Add(MeshesCustom.GetMultiPolygonPivot(an_coords));

                    // calculate perimeter:
                    this.Perimeter += (float)MeshesCustom.CalculateMultiPolygonPerimeter(an_coords);

                    // separate coordinates into polygon and holes:
                    int an_outer_polygon_index = -1;
                    List<Point3D> an_polygon = null;
                    List<List<Point3D>> an_holes = null;
                    MeshesCustom.ToPolygonWithHoles(an_coords, Orientation.XZ,
                                    out an_outer_polygon_index, out an_polygon, out an_holes);

                    // calculate resulting area:
                    float area_current = (float)MeshesCustom.CalculateAreaOfPolygonWHoles(an_polygon, an_holes);
                    level_areas_NET.Add(area_current);

                    if (this.Levels[i].IsBottomClosure)                        
                        this.Area_NET += area_current;
                }

                // GROSS
                List<ZonedPolygonRep> level_polys_OUT = this.defining_polygons_OUT.FindAll(x => x.LevelIndex == i);
                if (level_polys_OUT != null && level_polys_OUT.Count > 0)
                {
                    // convert the polygon representations into coordinates:
                    List<List<Point3D>> an_coords = new List<List<Point3D>>();
                    foreach (ZonedPolygonRep zpr in level_polys_OUT)
                    {
                        an_coords.Add(CommonExtensions.ConvertVector4ListToPoint3DList(zpr.Coordinates));
                    }
                    level_pivots_OUT.Add(MeshesCustom.GetMultiPolygonPivot(an_coords));

                    // separate coordinates into polygon and holes:
                    int an_outer_polygon_index = -1;
                    List<Point3D> an_polygon = null;
                    List<List<Point3D>> an_holes = null;
                    MeshesCustom.ToPolygonWithHoles(an_coords, Orientation.XZ,
                                    out an_outer_polygon_index, out an_polygon, out an_holes);

                    // calculate resulting area:
                    float area_current = (float)MeshesCustom.CalculateAreaOfPolygonWHoles(an_polygon, an_holes);
                    level_areas_GROSS.Add(area_current);

                    if (this.Levels[i].IsBottomClosure)
                        this.Area_GROSS += area_current;
                }
            }

            // calculate height and volume:
            for (int i = 0; i < this.Levels.Count; i++ )
            {
                float h_toPrev_IN, h_toNext_IN, h_toPrev_OUT, h_toNext_OUT;
                if (i == 0)
                {
                    h_toNext_IN = (float)(level_pivots_IN[i + 1].Y - level_pivots_IN[i].Y);
                    this.Volume_NET += level_areas_NET[i] * h_toNext_IN;
                    this.MaxHeight_NET -= level_pivots_IN[i].Y;
                    this.Elevation_NET = (float)(level_pivots_IN[i].Y);

                    h_toNext_OUT = (float)(level_pivots_OUT[i + 1].Y - level_pivots_OUT[i].Y);
                    this.Volume_GROSS += level_areas_GROSS[i] * h_toNext_OUT;
                    this.MaxHeight_GROSS -= level_pivots_OUT[i].Y;
                    this.Elevation_GROSS = (float)(level_pivots_OUT[i].Y);
                }
                else if (i == this.Levels.Count - 1)
                {
                    h_toPrev_IN = (float)(level_pivots_IN[i].Y - level_pivots_IN[i - 1].Y);
                    this.Volume_NET += level_areas_NET[i] * h_toPrev_IN;
                    this.MaxHeight_NET += level_pivots_IN[i].Y;
                    this.Ceiling_NET = (float)(level_pivots_IN[i].Y);

                    h_toPrev_OUT = (float)(level_pivots_OUT[i].Y - level_pivots_OUT[i - 1].Y);
                    this.Volume_GROSS += level_areas_GROSS[i] * h_toPrev_OUT;
                    this.MaxHeight_GROSS += level_pivots_OUT[i].Y;
                    this.Ceiling_GROSS = (float)(level_pivots_OUT[i].Y);
                }
                else
                {
                    h_toPrev_IN = (float)(level_pivots_IN[i].Y - level_pivots_IN[i - 1].Y);
                    h_toNext_IN = (float)(level_pivots_IN[i + 1].Y - level_pivots_IN[i].Y);
                    this.Volume_NET += level_areas_NET[i] * (h_toPrev_IN + h_toNext_IN);

                    h_toPrev_OUT = (float)(level_pivots_OUT[i].Y - level_pivots_OUT[i - 1].Y);
                    h_toNext_OUT = (float)(level_pivots_OUT[i + 1].Y - level_pivots_OUT[i].Y);
                    this.Volume_GROSS += level_areas_GROSS[i] * (h_toPrev_OUT + h_toNext_OUT);
                }
            }
            this.Volume_NET *= 0.5f;
            this.Volume_GROSS *= 0.5f;

            // AXES
            this.Elevation_AXES = 0f;
            this.MaxHeight_AXES = 0f;
            this.Area_AXES = 0f;
            this.Volume_AXES = 0f;

            int nrL = this.Levels.Count;
            for (int i = 0; i < nrL; i++ )
            {                
                // calculate level area and communiate to the level info
                this.Levels[i].Update();
                this.levels_info[i].LevelArea = this.Levels[i].Area;

                // AREA
                if (this.Levels[i].IsBottomClosure)                    
                    this.Area_AXES += this.Levels[i].Area;
                // VOLUME
                float h_toPrev, h_toNext;
                if (i == 0)
                {
                    h_toNext = (float)(this.Levels[i + 1].Avg_Height - this.Levels[i].Avg_Height);
                    this.Volume_AXES += this.Levels[i].Area * h_toNext;
                    this.Elevation_AXES = (float)(this.Levels[i].Avg_Height);
                }
                else if(i == (nrL - 1))
                {
                    h_toPrev = (float)(this.Levels[i].Avg_Height - this.Levels[i - 1].Avg_Height);
                    this.Volume_AXES += this.Levels[i].Area * h_toPrev;
                    this.Ceiling_AXES = (float)(this.Levels[i].Avg_Height);
                }
                else
                {
                    h_toPrev = (float)(this.Levels[i].Avg_Height - this.Levels[i - 1].Avg_Height);
                    h_toNext = (float)(this.Levels[i + 1].Avg_Height - this.Levels[i].Avg_Height);
                    this.Volume_AXES += this.Levels[i].Area * (h_toPrev + h_toNext);
                }
            }
            this.Volume_AXES *= 0.5f;
            this.MaxHeight_AXES = (float)(this.Levels[nrL - 1].Avg_Height - this.Levels[0].Avg_Height);

        }

        #endregion

        #region DISPLAY: Calculate Wall Quads (with AND without Openings)

        private void CalculateVolumeWalls()
        {
            this.wallQuads = new Dictionary<Vector3, List<Vector3>>();
            this.wallQuadsOpenings = new Dictionary<Vector3, List<List<Vector3>>>();

            this.surfaces = new List<MeshGeometry3D>();
            this.surface_opacity = new List<double>();

            this.wallQuads_info = new List<VolumeQuadInfo>();
            this.wallOpenings_info = new List<VolumeQuadInfo>();

            int n = this.defining_polygons.Count;
            for (int i = 0; i < n; i++)
            {
                List<int> upward_connection_woO = new List<int>();
                List<int> upward_connection_withO = new List<int>();
                for (int j = i + 1; j < n; j++)
                {
                    // WALLS W/O OPENINGS, OPENINGS separately
                    Dictionary<int, List<Vector3>> quads_current;
                    List<VolumeQuadInfo> quads_current_info;
                    List<VolumeQuadInfo> quads_current_openings_info;
                    ZonedVolume.GetWallQuadsToBeMeshed(this.defining_polygons[i], i, this.defining_polygons[j], j,
                                                        upward_connection_woO, out quads_current, out quads_current_info, out quads_current_openings_info);
                    upward_connection_woO.AddRange(quads_current.Keys.ToList());
                    if (quads_current != null && quads_current.Count > 0)
                    {
                        foreach (var entry in quads_current)
                        {
                            // save quads
                            int index = entry.Key;
                            List<Vector3> quad = entry.Value;
                            this.wallQuads.Add(new Vector3(this.defining_polygons[i].ID, this.defining_polygons[j].ID, index), quad);

                            // get the quad's height vector
                            Vector3 bQ3Pr = CommonExtensions.NormalProject(quad[3], quad[0], quad[1]);
                            Vector3 bQvh = quad[3] - bQ3Pr;
                            bQvh.Normalize();
                            // communicate it to the first (and lower) polygon in order to adjust the opening geometry
                            if (bQvh != Vector3.Zero && bQvh != Vector3.UnitY)
                                this.defining_polygons[i].ModifyOpeningPolygonNormal(index, bQvh);

                            // create the corresponding surface mesh for the quad
                            this.surfaces.Add(MeshesCustom.MeshFromQuads(new List<List<Vector3>> { quad }));
                            this.surface_opacity.Add(1.0);
                        }
                        this.wallQuads_info.AddRange(quads_current_info);
                        this.wallOpenings_info.AddRange(quads_current_openings_info);
                    }

                    // WALLS WITH OPENINGS
                    Dictionary<int, List<List<Vector3>>> quads_small_current =
                        ZonedVolume.GetQuadsToBeMeshed(this.defining_polygons[i], i, this.defining_polygons[j], j,
                        upward_connection_withO);
                    upward_connection_withO.AddRange(quads_small_current.Keys.ToList());
                    if (quads_small_current != null && quads_small_current.Count > 0)
                    {
                        foreach (var entry in quads_small_current)
                        {
                            // save quads
                            int index = entry.Key;
                            List<List<Vector3>> quads = entry.Value;
                            this.wallQuadsOpenings.Add(new Vector3(this.defining_polygons[i].ID, this.defining_polygons[j].ID, index), quads);

                        }
                    }
                }
            }

            // restore material associations, if any were saved at an earlier time
            this.RestoreMaterialAssociations();

            // done
            this.NrChangesSinceLoading++;
        }

        #endregion

        #region DISPLAY: Calculate Polygon Offsets

        // call in OnCreation() or after
        // calculates the polygons defining the inner and outer surfaces of the volume:
        // this.defining_polygons_OUT
        // this.defining_polygons_IN
        private void CalculatePolygonOffsets()
        {
            // 1. reset containers
            this.defining_polygons_IN = new List<ZonedPolygonRep>();
            this.defining_polygons_OUT = new List<ZonedPolygonRep>();

            // 2. calculate offset data
            int n = this.defining_polygons.Count;
            List<int> defining_polygon_owner_level = new List<int>();
            List<List<bool>> defining_polygon_offsets_DONE = new List<List<bool>>();

            List<List<float>> defining_polygon_offsets_IN = new List<List<float>>();
            List<List<bool>> defining_polygon_offsets_INdir = new List<List<bool>>();
            List<float> defining_polygon_offset_vIN = new List<float>();

            List<List<float>> defining_polygon_offsets_OUT = new List<List<float>>();
            List<List<bool>> defining_polygon_offsets_OUTdir = new List<List<bool>>();
            List<float> defining_polygon_offset_vOUT = new List<float>();

            foreach (ZonedPolygon zp in this.defining_polygons)
            {
                int nrP = zp.Polygon_Coords.Count;
                defining_polygon_owner_level.Add(-1);
                defining_polygon_offsets_DONE.Add(Enumerable.Repeat(false, nrP).ToList());
                
                defining_polygon_offsets_IN.Add(Enumerable.Repeat(0f, nrP).ToList());
                defining_polygon_offsets_INdir.Add(Enumerable.Repeat(false, nrP).ToList());
                defining_polygon_offset_vIN.Add(0f);

                defining_polygon_offsets_OUT.Add(Enumerable.Repeat(0f, nrP).ToList());
                defining_polygon_offsets_OUTdir.Add(Enumerable.Repeat(true, nrP).ToList());
                defining_polygon_offset_vOUT.Add(0f);
            }

           
            foreach(VolumeLevelInfo vli in this.levels_info)
            {
                ReadOnlyCollection<int> affected_polys = vli.ContainedPolyInds;
                foreach (int poly_index in affected_polys)
                {
                    defining_polygon_owner_level[poly_index] = vli.LevelIndex;
                    defining_polygon_offset_vIN[poly_index] = vli.OffsetInner;
                    defining_polygon_offset_vOUT[poly_index] = vli.OffsetOuter;
                    bool isExterior = (vli.OutermostPolyInd == poly_index);

                    foreach (VolumeQuadInfo vqi in this.wallQuads_info)
                    {
                        // perform calculation
                        if (vqi.PolyHigh == poly_index)
                        {
                            float cosN1N2 = Vector3.Dot(vli.LevelNormal, vqi.QuadNormal);
                            float sinN1N2 = (float) Math.Sqrt(1 - cosN1N2 * cosN1N2);
                            if (sinN1N2 >= CommonExtensions.GENERAL_CALC_TOLERANCE * 10)
                            {
                                float offsetP_IN = (vqi.OffsetInner - Math.Abs(vli.OffsetInner) * cosN1N2) / sinN1N2;
                                float offsetP_OUT = (vqi.OffsetOuter - Math.Abs(vli.OffsetOuter) * cosN1N2) / sinN1N2;

                                bool swapInOut = vqi.DiscrepancyInOut && vli.IsBottomClosure;
                                
                                defining_polygon_offsets_IN[vqi.PolyHigh][vqi.PolyHighInd] = offsetP_IN;
                                defining_polygon_offsets_INdir[vqi.PolyHigh][vqi.PolyHighInd] = swapInOut ? (isExterior) : (!isExterior); // Default:false
                                
                                defining_polygon_offsets_OUT[vqi.PolyHigh][vqi.PolyHighInd] = offsetP_OUT;
                                defining_polygon_offsets_OUTdir[vqi.PolyHigh][vqi.PolyHighInd] = swapInOut ? (!isExterior) : (isExterior); // Default:true

                                defining_polygon_offsets_DONE[vqi.PolyHigh][vqi.PolyHighInd] = true;
                            }
                        }
                        else if (vqi.PolyLow == poly_index && (!defining_polygon_offsets_DONE[vqi.PolyLow][vqi.PolyLowInd] || vqi.DiscrepancyInOut))
                        {
                            float cosN1N2 = Vector3.Dot(vli.LevelNormal, vqi.QuadNormal);
                            float sinN1N2 = (float)Math.Sqrt(1 - cosN1N2 * cosN1N2);
                            if (sinN1N2 >= CommonExtensions.GENERAL_CALC_TOLERANCE * 10)
                            {
                                float offsetP_IN = (vqi.OffsetInner - Math.Abs(vli.OffsetInner) * cosN1N2) / sinN1N2;
                                float offsetP_OUT = (vqi.OffsetOuter - Math.Abs(vli.OffsetOuter) * cosN1N2) / sinN1N2;

                                bool swapInOut = vqi.DiscrepancyInOut && vli.IsTopClosure;

                                defining_polygon_offsets_IN[vqi.PolyLow][vqi.PolyLowInd] = offsetP_IN;
                                defining_polygon_offsets_INdir[vqi.PolyLow][vqi.PolyLowInd] = swapInOut ? (isExterior) : (!isExterior); // (!isExterior);

                                defining_polygon_offsets_OUT[vqi.PolyLow][vqi.PolyLowInd] = offsetP_OUT;
                                defining_polygon_offsets_OUTdir[vqi.PolyLow][vqi.PolyLowInd] = swapInOut ? (!isExterior) : (isExterior); // (isExterior);

                                defining_polygon_offsets_DONE[vqi.PolyLow][vqi.PolyLowInd] = true;
                            }
                        }
                    }
                }
            }

            // 3. perform the offsets and save
            for(int i = 0; i < n; i++)
            {
                int nrP = this.defining_polygons[i].Polygon_Coords.Count;

                //List<bool> dir_in = Enumerable.Repeat(false, nrP).ToList();
                List<Point3D> poly_in = this.defining_polygons[i].OffsetPolygon(defining_polygon_offsets_IN[i], defining_polygon_offsets_INdir[i]);
                List<Vector4> poly_in_V4 = new List<Vector4>();
                for (int j = 0; j < nrP; j++ )
                {
                    poly_in_V4.Add(new Vector4((float)poly_in[j].X, (float)poly_in[j].Y + defining_polygon_offset_vIN[i], (float)poly_in[j].Z,
                                               (float)this.defining_polygons[i].Zones_Inds[j]));                    
                }
                this.defining_polygons_IN.Add(new ZonedPolygonRep(poly_in_V4, defining_polygon_owner_level[i]));

                //List<bool> dir_out = Enumerable.Repeat(true, nrP).ToList();
                List<Point3D> poly_out = this.defining_polygons[i].OffsetPolygon(defining_polygon_offsets_OUT[i], defining_polygon_offsets_OUTdir[i]);
                List<Vector4> poly_out_V4 = new List<Vector4>();
                for (int j = 0; j < nrP; j++)
                {
                    poly_out_V4.Add(new Vector4((float)poly_out[j].X, (float)poly_out[j].Y - defining_polygon_offset_vOUT[i], (float)poly_out[j].Z,
                                               (float)this.defining_polygons[i].Zones_Inds[j]));                    
                }
                this.defining_polygons_OUT.Add(new ZonedPolygonRep(poly_out_V4, defining_polygon_owner_level[i]));
            }

            // 4. calculate resulting PERIMETER and AREAS (only of levels that are bottom closures)
            this.CalculatePerimeterAreaVolume();

        }

        #endregion

        #region DISPLAY: (Re)Calculate Offset Wall Quads

        // fills the following lists:
        // this.wallQuads_OUT
        // this.wallQuads_IN
        private void CalculateWallOffsets()
        {
            int n = this.defining_polygons.Count;
            // define the quads of the OFFSET surfaces
            this.wallQuads_IN = new Dictionary<Vector3, List<Vector3>>();
            this.wallQuads_OUT = new Dictionary<Vector3, List<Vector3>>();
            for (int i = 0; i < n; i++)
            {
                List<int> upward_connection_offset_IN = new List<int>();
                List<int> upward_connection_offset_OUT = new List<int>();
                for (int j = i + 1; j < n; j++)
                {
                    // offset IN
                    Dictionary<int, List<Vector3>> quads_current_IN =
                        ZonedVolume.GetOffsetWallQuadsToBeMeshed(this.defining_polygons_IN[i].Coordinates,
                                                                 this.defining_polygons_IN[j].Coordinates,
                                                                 upward_connection_offset_IN);
                    upward_connection_offset_IN.AddRange(quads_current_IN.Keys.ToList());
                    if (quads_current_IN != null && quads_current_IN.Count > 0)
                    {
                        foreach (var entry in quads_current_IN)
                        {
                            // save quads
                            int index = entry.Key;
                            List<Vector3> quad = entry.Value;
                            this.wallQuads_IN.Add(new Vector3(this.defining_polygons[i].ID, this.defining_polygons[j].ID, index), quad);
                        }
                    }

                    // offset OUT
                    Dictionary<int, List<Vector3>> quads_current_OUT =
                        ZonedVolume.GetOffsetWallQuadsToBeMeshed(this.defining_polygons_OUT[i].Coordinates,
                                                                 this.defining_polygons_OUT[j].Coordinates,
                                                                 upward_connection_offset_OUT);
                    upward_connection_offset_OUT.AddRange(quads_current_OUT.Keys.ToList());
                    if (quads_current_OUT != null && quads_current_OUT.Count > 0)
                    {
                        foreach (var entry in quads_current_OUT)
                        {
                            // save quads
                            int index = entry.Key;
                            List<Vector3> quad = entry.Value;
                            this.wallQuads_OUT.Add(new Vector3(this.defining_polygons[i].ID, this.defining_polygons[j].ID, index), quad);
                        }
                    }
                }
            }
        }

        #endregion

        #region MATERIALS

        // call before resetting geometry
        private void SaveMaterialAssociations()
        {
            this.materials_per_level = new Dictionary<int, ComponentInteraction.Material>();
            this.materials_per_label = new Dictionary<Composite4IntKey, ComponentInteraction.Material>();

            foreach(VolumeQuadInfo vqi in this.wallQuads_info)
            {
                Composite4IntKey query_key = new Composite4IntKey(vqi.Label, vqi.PolyLow, vqi.PolyHigh, -1);
                if (!this.materials_per_label.ContainsKey(query_key))
                    this.materials_per_label.Add(query_key, vqi.AssocMaterial);
            }
            foreach(VolumeQuadInfo vqi in this.wallOpenings_info)
            {
                Composite4IntKey query_key = new Composite4IntKey(vqi.Label, vqi.PolyLow, vqi.PolyHigh, vqi.OpeningIndex);
                if (!this.materials_per_label.ContainsKey(query_key))
                    this.materials_per_label.Add(query_key, vqi.AssocMaterial);
            }

            foreach(VolumeLevelInfo vli in this.levels_info)
            {
                if (!this.materials_per_level.ContainsKey(vli.LevelIndex))
                    this.materials_per_level.Add(vli.LevelIndex, vli.AssocMaterial);
            }
        }

        private void RestoreMaterialAssociations()
        {
            if (this.materials_per_label == null || this.materials_per_level == null)
                return;

            foreach (VolumeQuadInfo vqi in this.wallQuads_info)
            {
                Composite4IntKey query_key = new Composite4IntKey(vqi.Label, vqi.PolyLow, vqi.PolyHigh, -1);
                if (this.materials_per_label.ContainsKey(query_key))
                    vqi.AssocMaterial = this.materials_per_label[query_key];
            }

            foreach (VolumeQuadInfo vqi in this.wallOpenings_info)
            {
                Composite4IntKey query_key = new Composite4IntKey(vqi.Label, vqi.PolyLow, vqi.PolyHigh, vqi.OpeningIndex);
                if (this.materials_per_label.ContainsKey(query_key))
                    vqi.AssocMaterial = this.materials_per_label[query_key];
            }

            foreach (VolumeLevelInfo vli in this.levels_info)
            {
                if (this.materials_per_level.ContainsKey(vli.LevelIndex))
                    vli.AssocMaterial = this.materials_per_level[vli.LevelIndex];
            }
        }
        
        // updated with comp rep association behaviour: 05.09.2017
        public void UpdateMaterialAssociation(ZonedVolumeSurfaceVis _input)
        {
            if (_input == null)
                return;

            // prepare for passing info to the main descriptor, if it exists
            int nr_additional_changes = 0;
            ComponentReps.CompRepDescirbes main_descriptor = null;
            ComponentReps.CompRepInfo cr1 = this.GetDescribingCompOrFirst();
            if (cr1 != null && cr1 is ComponentReps.CompRepDescirbes)
                main_descriptor = cr1 as ComponentReps.CompRepDescirbes;

            int update_label = -1;
            long update_lower_poly = -1;
            int update_opening_index = -1;

            if (_input.IsWall)
            {
                if (_input.OpeningIndex == -1)
                {
                    // adapt wall quad
                    VolumeQuadInfo vqi = this.wallQuads_info.Find(x => x.ID == _input.IDInOwner);
                    int vqi_index = (vqi == null) ? -1 : this.wallQuads_info.IndexOf(vqi);
                    if (vqi != null)
                    {
                        // A. pass the info to the main descriptor (step A HAS to be BEFORE step B for correct info propagation)
                        // this step ensures that the reference to the comp rep associated w the material is passed back to the CB
                        if (main_descriptor != null)
                        {
                            update_label = vqi.Label;
                            update_lower_poly = this.defining_polygons[vqi.PolyLow].ID;
                            main_descriptor.PassMaterialAssignmentToSurfaceRep(this.ID, update_label, update_opening_index, update_lower_poly, _input.AssocMaterial.ID);
                        }
                        
                        // added accumulation 24.12.2016
                        if (vqi.AssocMaterial.Name != GeometryViewer.ComponentInteraction.Material.MAT_DEF_NAME)
                            vqi.AssocMaterial.SubtractSurface(_input.AssocArea); // B. triggers change in the material -> in the associated comp rep

                        //vqi.AssocMaterial = _input.AssocMaterial;
                        this.wallQuads_info[vqi_index].AssocMaterial = _input.AssocMaterial; // replaced 29.09.2017
                        this.SaveMaterialAssociations(); // placed here 29.09.2017
                        nr_additional_changes++;

                        if (vqi.AssocMaterial.Name != GeometryViewer.ComponentInteraction.Material.MAT_DEF_NAME)
                            vqi.AssocMaterial.AddSurface(_input.AssocArea); // B. triggers change in the material -> in the associated comp rep
                    }
                }               
                else
                {
                    // adapt opening quad
                    VolumeQuadInfo vqi_op = this.wallOpenings_info.Find(x => x.ID == _input.IDInOwner && x.OpeningIndex == _input.OpeningIndex);
                    int vqi_op_index = (vqi_op == null) ? -1 : this.wallOpenings_info.IndexOf(vqi_op);
                    if (vqi_op != null)
                    {
                        // A. pass the info to the main descriptor (step A HAS to be BEFORE step B for correct info propagation)
                        // this step ensures that the reference to the comp rep associated w the material is passed back to the CB
                        if (main_descriptor != null)
                        {
                            update_label = vqi_op.Label;
                            update_lower_poly = this.defining_polygons[vqi_op.PolyLow].ID;
                            update_opening_index = vqi_op.OpeningIndex;
                            main_descriptor.PassMaterialAssignmentToSurfaceRep(this.ID, update_label, update_opening_index, update_lower_poly, _input.AssocMaterial.ID);
                        }
                        // added accumulation 24.12.2016
                        if (vqi_op.AssocMaterial.Name != GeometryViewer.ComponentInteraction.Material.MAT_DEF_NAME)
                            vqi_op.AssocMaterial.SubtractSurface(_input.AssocArea); // B. triggers change in the material -> in the associated comp rep

                        //vqi_op.AssocMaterial = _input.AssocMaterial;
                        this.wallOpenings_info[vqi_op_index].AssocMaterial = _input.AssocMaterial; // replaced 29.09.2017
                        this.SaveMaterialAssociations(); // placed here 29.09.2017
                        nr_additional_changes++;

                        if (vqi_op.AssocMaterial.Name != GeometryViewer.ComponentInteraction.Material.MAT_DEF_NAME)
                            vqi_op.AssocMaterial.AddSurface(_input.AssocArea); // B. triggers change in the material -> in the associated comp rep
                    }
                }                
            }
            else
            {
                // adapt level
                VolumeLevelInfo vli = this.levels_info.Find(x => x.ID == _input.IDInOwner);
                int vli_index = (vli == null) ? -1 : this.levels_info.IndexOf(vli);
                if (vli != null)
                {
                    // A. pass the info to the main descriptor (step A HAS to be BEFORE step B for correct info propagation)
                    // this step ensures that the reference to the comp rep associated w the material is passed back to the CB
                    if (main_descriptor != null)
                    {
                        update_label = vli.LevelIndex;
                        main_descriptor.PassMaterialAssignmentToSurfaceRep(this.ID, update_label, update_opening_index, update_lower_poly, _input.AssocMaterial.ID);
                    }
                    // added accumulation 24.12.2016
                    if (vli.AssocMaterial.Name != GeometryViewer.ComponentInteraction.Material.MAT_DEF_NAME)
                        vli.AssocMaterial.SubtractSurface(_input.AssocArea); // B. triggers change in the material -> in the associated comp rep

                    //vli.AssocMaterial = _input.AssocMaterial;
                    this.levels_info[vli_index].AssocMaterial = _input.AssocMaterial; // replaced 29.09.2017
                    this.SaveMaterialAssociations(); // placed here 29.09.2017
                    nr_additional_changes++;

                    if (vli.AssocMaterial.Name != GeometryViewer.ComponentInteraction.Material.MAT_DEF_NAME)
                        vli.AssocMaterial.AddSurface(_input.AssocArea); // B. triggers change in the material -> in the associated comp rep
                }
            }           

            // (re)calculate OFFSETS
            this.mesh_IN = null;
            this.mesh_OUT = null;
            this.lines_offset_IN = null;
            this.lines_offset_OUT = null;

            this.CalculatePolygonOffsets();
            this.CalculateWallOffsets();

            // save material associations
            //this.SaveMaterialAssociations();
            this.NrChangesSinceLoading += nr_additional_changes; // triggers re-association of the volume and the main descriptor
        }

        #endregion

        #region MATERIAL PARSING

        // call only when parsing
        private void ParseMaterialAssociations(Dictionary<int, ComponentInteraction.Material> _mat_per_level, 
                                               Dictionary<Composite4IntKey, ComponentInteraction.Material> _mat_per_label)
        {
            //// debug
            //string debug_mat = string.Empty;
            //foreach(var entry in _mat_per_label)
            //{
            //    debug_mat += entry.Key.ToString() + "->" + entry.Value.ID.ToString() + "\n"; ;
            //}
            //string debug_quads = string.Empty;
            //foreach (VolumeQuadInfo vqi in this.wallQuads_info)
            //{
            //    debug_quads += vqi.Label + "|" + vqi.PolyLow + "|" + vqi.PolyHigh + "|-1->...\n";
            //}

            this.materials_per_level = new Dictionary<int, ComponentInteraction.Material>();
            this.materials_per_label = new Dictionary<Composite4IntKey, ComponentInteraction.Material>();

            foreach (VolumeQuadInfo vqi in this.wallQuads_info)
            {
                Composite4IntKey query_key = new Composite4IntKey(vqi.Label, vqi.PolyLow, vqi.PolyHigh, -1);
                if (!this.materials_per_label.ContainsKey(query_key) && _mat_per_label.ContainsKey(query_key))
                {
                    vqi.AssocMaterial = _mat_per_label[query_key];
                    this.materials_per_label.Add(query_key, vqi.AssocMaterial);
                }
            }

            foreach(VolumeQuadInfo vqi in this.wallOpenings_info)
            {
                Composite4IntKey query_key = new Composite4IntKey(vqi.Label, vqi.PolyLow, vqi.PolyHigh, vqi.OpeningIndex);
                if (!this.materials_per_label.ContainsKey(query_key) && _mat_per_label.ContainsKey(query_key))
                {
                    vqi.AssocMaterial = _mat_per_label[query_key];
                    this.materials_per_label.Add(query_key, vqi.AssocMaterial);
                }
            }

            foreach (VolumeLevelInfo vli in this.levels_info)
            {
                if (!this.materials_per_level.ContainsKey(vli.LevelIndex) && _mat_per_level.ContainsKey(vli.LevelIndex))
                {
                    vli.AssocMaterial = _mat_per_level[vli.LevelIndex];
                    this.materials_per_level.Add(vli.LevelIndex, vli.AssocMaterial);
                }
            }
        }

        #endregion

        #region MATERIAL Info and Reference Transfer to / from Comp Reps (not in use)

        private List<Composite4IntKey> GetAllSurfaceIDsForMaterial(ComponentInteraction.Material _mat)
        {
            List<Composite4IntKey> found = new List<Composite4IntKey>();
            if (_mat == null) return found;

            if (this.materials_per_level == null || this.materials_per_label == null)
                this.SaveMaterialAssociations();
            if (this.materials_per_level.Count == 0 || this.materials_per_label.Count == 0)
                this.SaveMaterialAssociations();

            foreach(var entry in this.materials_per_level)
            {
                ComponentInteraction.Material m = entry.Value;
                if (m == null) continue;

                if (m.ID == _mat.ID)
                    found.Add(new Composite4IntKey(entry.Key, -1, -1, -1));
            }

            foreach(var entry in this.materials_per_label)
            {
                ComponentInteraction.Material m = entry.Value;
                if (m == null) continue;

                if (m.ID == _mat.ID)
                    found.Add(entry.Key);
            }

            return found;
        }

        public int GetNrSurfacesWithMaterial(ComponentInteraction.Material _mat)
        {
            List<Composite4IntKey> surfaces = this.GetAllSurfaceIDsForMaterial(_mat);
            return surfaces.Count;
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ================================================= UTILITIES ============================================ //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region UTILITIES: Preparing Polygon Openings for Mesh Geometry

        private static Dictionary<int,List<List<Vector3>>> GetQuadsToBeMeshed(ZonedPolygon _zp0, int _zp0_ind ,ZonedPolygon _zp1, int _zp1_ind, List<int> _excluded)
        {
            Dictionary<int, List<List<Vector3>>> allQuads = new Dictionary<int, List<List<Vector3>>>();

            if (_zp0 == null || _zp1 == null)
                return allQuads;

            // the quads are indexed according to _zp0's segment indices
            Dictionary<int, List<Vector3>> bigQuads;
            List<VolumeQuadInfo> bigQuads_info; // is ignored here
            List<VolumeQuadInfo> bigQuadOpenings_info; // is ignored here
            ZonedVolume.GetWallQuadsToBeMeshed(_zp0, _zp0_ind, _zp1, _zp1_ind, _excluded, out bigQuads, out bigQuads_info, out bigQuadOpenings_info);
            List<ZoneOpeningVis> openings0 = _zp0.ExtractOpeningsForDisplay();
            for (int i = 0; i < _zp0.Polygon_Coords.Count; i++ )
            {
                if (!bigQuads.ContainsKey(i))
                    continue;

                List<ZoneOpeningVis> openings0_current = openings0.FindAll(x => x.IndInOwner == i);
                if (openings0_current == null || openings0_current.Count < 1)
                {
                    allQuads.Add(i, new List<List<Vector3>> { bigQuads[i] });
                    continue;
                }

                List<List<Vector3>> quads_current = new List<List<Vector3>> { bigQuads[i] };
                List<List<Vector3>> quads_current_modified = new List<List<Vector3>>();

                foreach(ZoneOpeningVis zov in openings0_current)
                {
                    List<Vector3> zov_geom = _zp0.ExtractOpeningGeometry(zov.IDInOwner);                   
                    // perform split
                    foreach(List<Vector3> quad in quads_current)
                    {
                        List<List<Vector3>> quad_after_split = ZonedVolume.SplitAtOpening(quad, zov_geom);
                        quads_current_modified.AddRange(quad_after_split);
                    }
                    quads_current = new List<List<Vector3>>(quads_current_modified);
                    quads_current_modified = new List<List<Vector3>>();
                }
                allQuads.Add(i, quads_current);
            }

            return allQuads;
        }

        #endregion

        #region UTILITIES: Quad Split in the presense of an Opening

        private static List<List<Vector3>> SplitAtOpening(List<Vector3> _quad, List<Vector3> _opening)
        {
            // 1. attempt a horizontal split
            List<List<Vector3>> quads_after_HSplit = ZonedVolume.SplitHorizontal(_quad, _opening);
            if (quads_after_HSplit.Count < 2)
            {
                // 1. attempt a vertical split
                List<List<Vector3>> quads_after_VSplit = ZonedVolume.SplitVertical(_quad, _opening);
                return quads_after_VSplit;
            }
            else
            {
                return quads_after_HSplit;
            }
        }

        private static List<List<Vector3>> SplitVertical(List<Vector3> _quad, List<Vector3> _opening)
        {
            if (_quad == null || _opening == null)
                return new List<List<Vector3>>();

            List<List<Vector3>> quads_after_split = new List<List<Vector3>> { _quad };

            if (_quad.Count < 3 || _opening.Count != 6)
                return quads_after_split;

            // perform intersection of opening 'verticals' with quad 'horizontals':
            //q3___x_______x_____q2
            //  |  :       :     |
            //  |o4:_______:o5   |
            //  |o2|_______|o3   |
            //q0|__:_______:_____|q1
            //     x       x
            // ....:.......:.......
            //     o0      o1

            Vector3 op24_int_q01, op35_int_q01, op24_int_q23, op35_int_q23;
            Vector3 B;

            CommonExtensions.LineToLineShortestLine3D(_quad[0], _quad[1], _opening[2], _opening[4], out op24_int_q01, out B);
            CommonExtensions.LineToLineShortestLine3D(_quad[0], _quad[1], _opening[3], _opening[5], out op35_int_q01, out B);
            CommonExtensions.LineToLineShortestLine3D(_quad[2], _quad[3], _opening[2], _opening[4], out op24_int_q23, out B);
            CommonExtensions.LineToLineShortestLine3D(_quad[2], _quad[3], _opening[3], _opening[5], out op35_int_q23, out B);

            Vector3 q03 = _quad[3] - _quad[0];
            float q03_lSq = q03.LengthSquared();
            Vector3 q12 = _quad[2] - _quad[1];
            float q12_lSq = q12.LengthSquared();

            // perform the actual VERTICAL split
            SegmentContainmentType containment =
                ZonedVolume.GetRelativePosition(_quad[0], _quad[1], op24_int_q01, op35_int_q01);

            if (containment == SegmentContainmentType.CONTAINED)
            {
                Vector3 op2_to_q01 = _opening[2] - op24_int_q01;
                Vector3 op3_to_q01 = _opening[3] - op35_int_q01;
                Vector3 op4_to_q23 = _opening[4] - op24_int_q23;
                Vector3 op5_to_q23 = _opening[5] - op35_int_q23;

                Vector3 q01_to_q23_along_op24 = op24_int_q01 - op24_int_q23;
                Vector3 q01_to_q23_along_op35 = op35_int_q01 - op35_int_q23;
                float q01_23_lSqLeft = q01_to_q23_along_op24.LengthSquared();
                float q01_23_lSqRight = q01_to_q23_along_op35.LengthSquared();

                if (op3_to_q01.LengthSquared() < q01_23_lSqRight && op2_to_q01.LengthSquared() < q01_23_lSqLeft &&
                    op5_to_q23.LengthSquared() < q01_23_lSqRight && op4_to_q23.LengthSquared() < q01_23_lSqLeft)
                {
                    // split the quad in 4 new ones and one opening: 
                    quads_after_split.Remove(_quad);
                    quads_after_split.Add(new List<Vector3> { _quad[0], op24_int_q01, op24_int_q23, _quad[3] });
                    quads_after_split.Add(new List<Vector3> { op24_int_q01, op35_int_q01, _opening[3], _opening[2] });
                    quads_after_split.Add(new List<Vector3> { _opening[4], _opening[5], op35_int_q23, op24_int_q23 });
                    quads_after_split.Add(new List<Vector3> { op35_int_q01, _quad[1], _quad[2], op35_int_q23 });
                }
            }
            else if (containment == SegmentContainmentType.OVERLAP_START)
            {
                // split the quad in 3 new ones and one opening:
                // A. overlap at END of opening (RIGHT)
                Vector3 op23_int_q12, op45_int_q12;
                CommonExtensions.LineToLineShortestLine3D(_quad[1], _quad[2], _opening[2], _opening[3], out op23_int_q12, out B);
                CommonExtensions.LineToLineShortestLine3D(_quad[1], _quad[2], _opening[4], _opening[5], out op45_int_q12, out B);
                Vector3 op23_to_q1 = _quad[1] - op23_int_q12;
                Vector3 op45_to_q2 = _quad[2] - op45_int_q12;
                if (op23_to_q1.LengthSquared() < q12_lSq && op45_to_q2.LengthSquared() < q12_lSq)
                {
                    quads_after_split.Remove(_quad);
                    quads_after_split.Add(new List<Vector3> { _quad[0], op24_int_q01, op24_int_q23, _quad[3] });
                    quads_after_split.Add(new List<Vector3> { op24_int_q01, _quad[1], op23_int_q12, _opening[2] });
                    quads_after_split.Add(new List<Vector3> { _opening[4], op45_int_q12, _quad[2], op24_int_q23 });
                }
            }
            else if (containment == SegmentContainmentType.OVERLAP_END)
            {
                // split the quad in 3 new ones and one opening:
                // B. overlap at START of opening (LEFT)
                Vector3 op45_int_q03, op23_int_q03;
                CommonExtensions.LineToLineShortestLine3D(_quad[0], _quad[3], _opening[4], _opening[5], out op45_int_q03, out B);
                CommonExtensions.LineToLineShortestLine3D(_quad[0], _quad[3], _opening[2], _opening[3], out op23_int_q03, out B);
                Vector3 op45_to_q3 = _quad[3] - op45_int_q03;
                Vector3 op23_to_q0 = _quad[0] - op23_int_q03;
                if (op45_to_q3.LengthSquared() < q03_lSq && op23_to_q0.LengthSquared() < q03_lSq)
                {
                    quads_after_split.Remove(_quad);
                    quads_after_split.Add(new List<Vector3> { _quad[0], op35_int_q01, _opening[3], op23_int_q03 });
                    quads_after_split.Add(new List<Vector3> { op35_int_q01, _quad[1], _quad[2], op35_int_q23});
                    quads_after_split.Add(new List<Vector3> { op45_int_q03, _opening[5], op35_int_q23, _quad[3] });
                }
            }
            else
            {
                // no overlap -> no change in the quad
                // DO NOTHING
            }


            return quads_after_split;
        }

        private static List<List<Vector3>> SplitHorizontal(List<Vector3> _quad, List<Vector3> _opening)
        {
            if (_quad == null || _opening == null)
                return new List<List<Vector3>>();

            List<List<Vector3>> quads_after_split = new List<List<Vector3>> { _quad };

            if (_quad.Count < 3 || _opening.Count != 6)
                return quads_after_split;


            // extend the opening 'horizontals' to the quad's 'verticals'
            //  q3__________________________q2
            //  |                           \
            // x|.......o4________o5.........\x
            // x|........|________|...........\x
            //  |      o2:        :o3          \
            //  |________:________:_____________\
            //  q0       :        :             q1
            //           :        :
            //  ........o0........o1......................

            Vector3 op23_int_q03, op45_int_q03, op23_int_q12, op45_int_q12;
            Vector3 B;

            CommonExtensions.LineToLineShortestLine3D(_quad[0], _quad[3], _opening[2], _opening[3], out op23_int_q03, out B);
            CommonExtensions.LineToLineShortestLine3D(_quad[0], _quad[3], _opening[4], _opening[5], out op45_int_q03, out B);
            CommonExtensions.LineToLineShortestLine3D(_quad[1], _quad[2], _opening[2], _opening[3], out op23_int_q12, out B);
            CommonExtensions.LineToLineShortestLine3D(_quad[1], _quad[2], _opening[4], _opening[5], out op45_int_q12, out B);

            Vector3 q23 = _quad[3] - _quad[2];
            float q23_lSq = q23.LengthSquared();
            Vector3 q01 = _quad[1] - _quad[0];
            float q01_lSq = q01.LengthSquared();

            // perform the actual HORIZONTAL split
            SegmentContainmentType containment =
                ZonedVolume.GetRelativePosition(_quad[0], _quad[3], op23_int_q03, op45_int_q03);

            if (containment == SegmentContainmentType.CONTAINED)
            {
                Vector3 op2_to_q03 = _opening[2] - op23_int_q03;
                Vector3 op4_to_q03 = _opening[4] - op45_int_q03;
                Vector3 op3_to_q12 = _opening[3] - op23_int_q12;
                Vector3 op5_to_q12 = _opening[5] - op45_int_q12;

                Vector3 q03_to_q12_along_op23 = op23_int_q03 - op23_int_q12;
                Vector3 q03_to_q12_along_op45 = op45_int_q03 - op45_int_q12;
                float q03_12_lSqTop = q03_to_q12_along_op45.LengthSquared();
                float q03_12_lSqBottom = q03_to_q12_along_op23.LengthSquared();

                if (op2_to_q03.LengthSquared() < q03_12_lSqBottom && op4_to_q03.LengthSquared() < q03_12_lSqTop &&
                    op3_to_q12.LengthSquared() < q03_12_lSqBottom && op5_to_q12.LengthSquared() < q03_12_lSqTop)
                {
                    // split the quad in 4 new ones and one opening: 
                    quads_after_split.Remove(_quad);
                    quads_after_split.Add(new List<Vector3> { _quad[0], _quad[1], op23_int_q12, op23_int_q03 });
                    quads_after_split.Add(new List<Vector3> { op23_int_q03, _opening[2], _opening[4], op45_int_q03 });
                    quads_after_split.Add(new List<Vector3> { _opening[3], op23_int_q12, op45_int_q12, _opening[5] });
                    quads_after_split.Add(new List<Vector3> { op45_int_q03, op45_int_q12, _quad[2], _quad[3] });
                }
            }
            else if (containment == SegmentContainmentType.OVERLAP_START)
            {
                // split the quad in 3 new ones and one opening:
                // A. overlap at TOP of opening
                Vector3 op24_int_q23, op35_int_q23;
                CommonExtensions.LineToLineShortestLine3D(_quad[2], _quad[3], _opening[2], _opening[4], out op24_int_q23, out B);
                CommonExtensions.LineToLineShortestLine3D(_quad[2], _quad[3], _opening[3], _opening[5], out op35_int_q23, out B);
                Vector3 op24_to_q3 = _quad[3] - op24_int_q23;
                Vector3 op35_to_q2 = _quad[2] - op35_int_q23;
                if (op24_to_q3.LengthSquared() < q23_lSq && op35_to_q2.LengthSquared() < q23_lSq)
                {
                    quads_after_split.Remove(_quad);
                    quads_after_split.Add(new List<Vector3> { _quad[0], _quad[1], op23_int_q12, op23_int_q03 });
                    quads_after_split.Add(new List<Vector3> { op23_int_q03, _opening[2], op24_int_q23, _quad[3] });
                    quads_after_split.Add(new List<Vector3> { _opening[3], op23_int_q12, _quad[2], op35_int_q23 });
                } 
            }
            else if (containment == SegmentContainmentType.OVERLAP_END)
            {
                // split the quad in 3 new ones and one opening:
                // B. overlap at BOTTOM of opening
                Vector3 op24_int_q01, op35_int_q01;
                CommonExtensions.LineToLineShortestLine3D(_quad[0], _quad[1], _opening[2], _opening[4], out op24_int_q01, out B);
                CommonExtensions.LineToLineShortestLine3D(_quad[0], _quad[1], _opening[3], _opening[5], out op35_int_q01, out B);
                Vector3 op24_to_q0 = _quad[0] - op24_int_q01;
                Vector3 op35_to_q1 = _quad[1] - op35_int_q01;
                if (op24_to_q0.LengthSquared() < q01_lSq && op35_to_q1.LengthSquared() < q01_lSq)
                {
                    quads_after_split.Remove(_quad);
                    quads_after_split.Add(new List<Vector3> { _quad[0], op24_int_q01, _opening[4], op45_int_q03 });
                    quads_after_split.Add(new List<Vector3> { op35_int_q01, _quad[1], op45_int_q12, _opening[5] });
                    quads_after_split.Add(new List<Vector3> { op45_int_q03, op45_int_q12, _quad[2], _quad[3] });
                }                
            }
            else
            {
                // no overlap -> no change in the quad
                // DO NOTHING
            }

            return quads_after_split;

        }

        #endregion

        #region WALL QUAD EXTRACTION
        private static void GetWallQuadsToBeMeshed(ZonedPolygon _zp0, int _zp0_ind, ZonedPolygon _zp1, int _zp1_ind,
            List<int> _excluded, out Dictionary<int, List<Vector3>> bigQuads, out List<VolumeQuadInfo> bigQuads_info, out List<VolumeQuadInfo> bigQuads_Opening_info)
        {
            bigQuads = new Dictionary<int, List<Vector3>>();
            bigQuads_info = new List<VolumeQuadInfo>();
            bigQuads_Opening_info = new List<VolumeQuadInfo>();

            if (_zp0 == null || _zp1 == null)
                return;

            bool perform_exclusion = (_excluded != null && _excluded.Count > 0);

            // determine the WALL QUADS: connecing the same labels in both zoned polygons
            // and save them along with the polygon segment INDEX
            int n0 = _zp0.Polygon_Coords.Count;
            int n1 = _zp1.Polygon_Coords.Count;

            for (int i = 0; i < n0; i++)
            {
                for (int j = 0; j < n1; j++)
                {
                    if (perform_exclusion && _excluded.Contains(i))
                        continue;

                    bool discrepancy_in_out = false;
                    
                    if (_zp0.Zones_Inds[i] == _zp1.Zones_Inds[j])
                    {
                        // get the corrsponding line segment normals
                        Vector3 pn0 = _zp0.GetLineNormal(i);
                        Vector3 pn1 = _zp1.GetLineNormal(j);
                        discrepancy_in_out = Vector3.Dot(pn0, pn1) < 0;

                        List<Vector3> quad = new List<Vector3>
                        {
                            _zp0.Polygon_Coords[i].ToVector3(), 
                            _zp0.Polygon_Coords[(i + 1) % n0].ToVector3(),
                            _zp1.Polygon_Coords[(j + 1) % n1].ToVector3(), 
                            _zp1.Polygon_Coords[j].ToVector3()
                        };

                        double area = MeshesCustom.CalculatePolygonLargestSignedProjectedArea(quad);
                        if (Math.Abs(area) < CommonExtensions.GENERAL_CALC_TOLERANCE)
                        {
                            discrepancy_in_out = true;
                            quad = new List<Vector3>
                            {
                                _zp0.Polygon_Coords[i].ToVector3(), 
                                _zp0.Polygon_Coords[(i + 1) % n0].ToVector3(),
                                _zp1.Polygon_Coords[j].ToVector3(), 
                                _zp1.Polygon_Coords[(j + 1) % n1].ToVector3()
                            };
                            area = MeshesCustom.CalculatePolygonLargestSignedProjectedArea(quad);
                        }

                        bigQuads.Add(i, quad);
                        bigQuads_info.Add(new VolumeQuadInfo(_zp0.Zones_Inds[i], _zp0_ind, i, _zp1_ind, j, discrepancy_in_out, quad, area));
                        
                        List<List<Vector3>> opening_coords = _zp0.GetOpeningGeometryForLabel(_zp0.Zones_Inds[i]);
                        for (int op = 0; op < opening_coords.Count; op++)
                        {
                            List<Vector3> op_quad = opening_coords[op];
                            // get area and size
                            double op_area = 0.0, op_h = 0.0, op_w = 0.0;
                            ZonedVolume.GetMeasurementsOfQuad(op_quad, out op_area, out op_h, out op_w);
                            // define
                            bigQuads_Opening_info.Add(new VolumeQuadInfo(_zp0.Zones_Inds[i], _zp0_ind, i, _zp1_ind, j, discrepancy_in_out, op_quad, op_area, op));
                        }
                    }
                }
            }

        }

        #endregion

        #region OFFSET WALL QUAD EXTRACTION

        private static Dictionary<int, List<Vector3>> GetOffsetWallQuadsToBeMeshed(List<Vector4> _offsetPoly0, List<Vector4> _offsetPoly1, List<int> _excluded)
        {
            if (_offsetPoly0 == null || _offsetPoly1 == null)
                return new Dictionary<int, List<Vector3>>();

            int n0 = _offsetPoly0.Count;
            int n1 = _offsetPoly1.Count;

            if (n0 == 0 || n1 == 0)
                return new Dictionary<int, List<Vector3>>();

            bool perform_exclusion = (_excluded != null && _excluded.Count > 0);

            // determine the WALL QUADS: connecing the same labels in both zoned polygons
            // and save them along with the polygon segment INDEX
            Dictionary<int, List<Vector3>> bigQuads = new Dictionary<int, List<Vector3>>();
            for (int i = 0; i < n0; i++)
            {
                for (int j = 0; j < n1; j++)
                {
                    if (perform_exclusion && _excluded.Contains(i))
                        continue;

                    if (_offsetPoly0[i].W == _offsetPoly1[j].W)
                    {
                        List<Vector3> quad_A = new List<Vector3>
                        {
                            new Vector3(_offsetPoly0[i].X, _offsetPoly0[i].Y, _offsetPoly0[i].Z),
                            new Vector3(_offsetPoly0[(i + 1) % n0].X, _offsetPoly0[(i + 1) % n0].Y, _offsetPoly0[(i + 1) % n0].Z),
                            new Vector3(_offsetPoly1[(j + 1) % n1].X, _offsetPoly1[(j + 1) % n1].Y, _offsetPoly1[(j + 1) % n1].Z),
                            new Vector3(_offsetPoly1[j].X, _offsetPoly1[j].Y, _offsetPoly1[j].Z)
                        };
                        double area_A = Math.Abs(MeshesCustom.CalculatePolygonLargestSignedProjectedArea(quad_A));

                        List<Vector3> quad_B = new List<Vector3>
                        {
                            new Vector3(_offsetPoly0[i].X, _offsetPoly0[i].Y, _offsetPoly0[i].Z),
                            new Vector3(_offsetPoly0[(i + 1) % n0].X, _offsetPoly0[(i + 1) % n0].Y, _offsetPoly0[(i + 1) % n0].Z),
                            new Vector3(_offsetPoly1[j].X, _offsetPoly1[j].Y, _offsetPoly1[j].Z),
                            new Vector3(_offsetPoly1[(j + 1) % n1].X, _offsetPoly1[(j + 1) % n1].Y, _offsetPoly1[(j + 1) % n1].Z)
                        };
                        double area_B = Math.Abs(MeshesCustom.CalculatePolygonLargestSignedProjectedArea(quad_B));

                        if (area_A > area_B)
                            bigQuads.Add(i, quad_A);
                        else
                            bigQuads.Add(i, quad_B);
                    }
                }
            }

            return bigQuads;

        }


        #endregion

        #region UTILITIES: Containment Type

        // determine the relative positions of segment P:[p0 before p1] and Q:[q0 before q1]
        // all lying on the same line in 3D
        private static SegmentContainmentType GetRelativePosition(Vector3 _p0, Vector3 _p1, Vector3 _q0, Vector3 _q1)
        {
            // SEGMENT P
            Vector3 p0p1 = _p1 - _p0;
            float length_p0p1 = p0p1.LengthSquared();

            // START of P to Q
            Vector3 p0q0 = _q0 - _p0;
            float length_p0q0 = p0q0.LengthSquared();
            bool p0_before_q0 = Math.Sign(Vector3.Dot(p0p1, p0q0)) > 0;

            Vector3 p0q1 = _q1 - _p0;
            float length_p0q1 = p0q1.LengthSquared();
            bool p0_before_q1 = Math.Sign(Vector3.Dot(p0p1, p0q1)) > 0;

            // END of P to Q
            Vector3 p1q0 = _q0 - _p1;
            float length_p1q0 = p1q0.LengthSquared();
            bool p1_after_q0 = Math.Sign(Vector3.Dot(-p0p1, p1q0)) > 0;

            Vector3 p1q1 = _q1 - _p1;
            float length_p1q1 = p1q1.LengthSquared();
            bool p1_after_q1 = Math.Sign(Vector3.Dot(-p0p1, p1q1)) > 0;

            // evaluate
            if (p0_before_q0 && p1_after_q1)
            {
                return SegmentContainmentType.CONTAINED;
            }
            else if ((p0_before_q0 && !p1_after_q0) || (!p0_before_q1 && p1_after_q1))
            {
                return SegmentContainmentType.DISJOINT;
            }
            else if (p0_before_q0 && p1_after_q0 && !p1_after_q1)
            {
                return SegmentContainmentType.OVERLAP_START; // start of P
            }
            else if (!p0_before_q0 && p0_before_q1 && p1_after_q1)
            {
                return SegmentContainmentType.OVERLAP_END; // end of P
            }

            return SegmentContainmentType.DISJOINT;

        }

        

        #endregion

        #region UTILITIES: EditMode
        public static ZonedVolumeEditModeType GetEditModeType(string _type)
        {
            if (_type == null)
                return ZonedVolumeEditModeType.NONE;

            switch (_type)
            {
                case "LEVEL_ADD":
                    return ZonedVolumeEditModeType.LEVEL_ADD;
                case "LEVEL_DELETE":
                    return ZonedVolumeEditModeType.LEVEL_DELETE;
                case "MATERIAL_ASSIGN":
                    return ZonedVolumeEditModeType.MATERIAL_ASSIGN;
                case "ISBEING_DELETED":
                    return ZonedVolumeEditModeType.ISBEING_DELETED;
                case "ISBEING_REASSOCIATED":
                    return ZonedVolumeEditModeType.ISBEING_REASSOCIATED;
                default:
                    return ZonedVolumeEditModeType.NONE;
            }
        }
        #endregion

        #region ToString, DXF Export

        public override string ToString()
        {
            return "ZV {" + this.ID + "} " + this.EntityName;
        }

        public override void AddToExport(ref StringBuilder _sb, bool _with_contained_entites)
        {
            if (_sb == null) return;

            _sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());      // 0
            _sb.AppendLine(DXFUtils.GV_ZONEDVOL);                               // GV_ZONED_VOLUME

            _sb.AppendLine(((int)EntitySaveCode.CLASS_NAME).ToString());        // 100 (subclass marker)
            _sb.AppendLine(this.GetType().ToString());                          // GeometryViewer.EntityGeometry.ZonedVolume

            string tmp = string.Empty;

            // layer
            _sb.AppendLine(((int)ZonedPolygonSaveCode.LAYER_NAME).ToString());              // 1106
            _sb.AppendLine(this.EntityLayer.EntityName);

            // color
            _sb.AppendLine(((int)EntitySpecificSaveCode.COLOR_BY_LAYER).ToString());        // 1008
            tmp = (this.ColorByLayer) ? "1" : "0";
            _sb.AppendLine(tmp);

            // levels
            if (this.Levels.Count < 2) return;

            _sb.AppendLine(((int)ZonedVolumeSaveCode.LEVELS).ToString());                   // 1201
            _sb.AppendLine(this.Levels.Count.ToString());

            _sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());                  // 0
            _sb.AppendLine(DXFUtils.ENTITY_SEQUENCE);                                       // ENTSEQ

            foreach (ZonedPolygonGroup zpg in this.Levels)
            {
                if (zpg != null)
                    zpg.AddToExport(ref _sb, false);
            }

            _sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());                  // 0
            _sb.AppendLine(DXFUtils.SEQUENCE_END);                                          // SEQEND
            _sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());                  // 0
            _sb.AppendLine(DXFUtils.ENTITY_CONTINUE);                                       // ENTCTN

            // materials
            if (this.materials_per_level == null || this.materials_per_label == null)
                this.SaveMaterialAssociations();

            _sb.AppendLine(((int)ZonedVolumeSaveCode.MATERIALS_PER_LEVEL).ToString());      // 1203
            _sb.AppendLine(this.materials_per_level.Count.ToString());
            
            foreach(var entry in this.materials_per_level)
            {
                _sb.AppendLine(((int)EntitySaveCode.X_VALUE).ToString());                   // 910
                _sb.AppendLine(entry.Key.ToString());                                       // level index in this.Levels
                _sb.AppendLine(((int)EntitySaveCode.Y_VALUE).ToString());                   // 920
                _sb.AppendLine(entry.Value.ID.ToString());                                  // Material ID
            }

            _sb.AppendLine(((int)ZonedVolumeSaveCode.MATERIALS_PER_LABEL).ToString());      // 1204
            _sb.AppendLine(this.materials_per_label.Count.ToString());

            //// debug
            //string debug_mat = string.Empty;
            foreach (var entry in this.materials_per_label)
            {
                _sb.AppendLine(((int)EntitySaveCode.X_VALUE).ToString());                   // 910
                _sb.AppendLine(entry.Key.ToString());                                       // Composite Key consisting of: label, lower poly id, upper poly id, opening index or -1
                _sb.AppendLine(((int)EntitySaveCode.Y_VALUE).ToString());                   // 920
                _sb.AppendLine(entry.Value.ID.ToString());                                  // Material ID

                //// debug
                //debug_mat += entry.Key.ToString() + "->" + entry.Value.ID.ToString() + "\n";
            }

            // also signifies end of complex entity (ends w SEQEND)
            base.AddToExport(ref _sb, _with_contained_entites);
        }

        public override void AddToACADExport(ref StringBuilder _sb, string _layer_name_visible, string _layer_name_hidden)
        {
            base.AddToACADExport(ref _sb, _layer_name_visible, _layer_name_hidden);

            // force update
            if (this.wallQuads_IN == null || this.wallQuads_IN.Count < 1)
                this.OnCreation();

            // levels
            foreach(ZonedPolygon zp in this.defining_polygons)
            {
                zp.AddToACADExport(ref _sb, _layer_name_hidden, _layer_name_hidden); // incuding openings
            }
            foreach(ZonedPolygonRep zpR in this.defining_polygons_IN)
            {
                List<Vector3> vertices = zpR.Coordinates.Select(x => new Vector3(x.X, x.Y, x.Z)).ToList();
                DXFUtils.Add3dPolylineToExport(ref _sb, _layer_name_visible, this.EntityColor, vertices, true);
            }
            foreach (ZonedPolygonRep zpR in this.defining_polygons_OUT)
            {
                List<Vector3> vertices = zpR.Coordinates.Select(x => new Vector3(x.X, x.Y, x.Z)).ToList();
                DXFUtils.Add3dPolylineToExport(ref _sb, _layer_name_visible, this.EntityColor, vertices, true);
            }

            // walls
            foreach(var entry in this.wallQuads)
            {
                DXFUtils.Add3dPolylineToExport(ref _sb, _layer_name_hidden, this.EntityColor, entry.Value, true);
            }
            foreach (var entry in this.wallQuads_IN)
            {
                DXFUtils.Add3dPolylineToExport(ref _sb, _layer_name_visible, this.EntityColor, entry.Value, true);
            }
            foreach (var entry in this.wallQuads_OUT)
            {
                DXFUtils.Add3dPolylineToExport(ref _sb, _layer_name_visible, this.EntityColor, entry.Value, true);
            }

            // opening PROJECTIONS on the inner and out wall qauds
            for (int i = 0; i < this.wallQuads.Count; i++ )
            {
                var entry_IN = this.wallQuads_IN.ElementAt(i);
                // check for openings               
                ZonedPolygon zp_lower = this.defining_polygons.Find(x => (x.ID == entry_IN.Key.X));
                if (zp_lower == null) continue;

                int label = (int)entry_IN.Key.Z;
                List<List<Vector3>> opening_coords = zp_lower.GetOpeningGeometryForLabel(label);
                if (opening_coords.Count > 0)
                {
                    // define the wall quad planes
                    Vector3 v0_IN = entry_IN.Value[0];
                    Vector3 v1_IN = entry_IN.Value[1];
                    Vector3 v2_IN = entry_IN.Value[3];

                    var entry_OUT = this.wallQuads_OUT.ElementAt(i);
                    Vector3 v0_OUT = entry_OUT.Value[0];
                    Vector3 v1_OUT = entry_OUT.Value[1];
                    Vector3 v2_OUT = entry_OUT.Value[3];

                    foreach (List<Vector3> xs in opening_coords)
                    {
                        // project onto the wall quads
                        List<Vector3> xs_projected_OUT = xs.Select(x => CommonExtensions.ProjectPointOnPlane(x, v0_OUT, v1_OUT, v2_OUT)).ToList(); // CCW
                        List<Vector3> xs_projected_IN = xs.Select(x => CommonExtensions.ProjectPointOnPlane(x, v0_IN, v2_IN, v1_IN)).ToList(); // CW
                        DXFUtils.Add3dPolylineToExport(ref _sb, _layer_name_visible, this.EntityColor, xs_projected_IN, true);
                        DXFUtils.Add3dPolylineToExport(ref _sb, _layer_name_visible, this.EntityColor, xs_projected_OUT, true);
                    }
                }
            }
        }

        #endregion

        #region EXPORT Detailed Info to Component Representations

        /*
         * for each surface we need:
         * * line geometry for DISPLAY
         * * mesh geometry for DISPLAY
         * * area, height, width of surface (gross, axis, net)
         * * area, height, width of each opening in the surface (gross = axis = net at this stage, since no info on door or window construction)
         */
        public List<ComponentReps.SurfaceInfo> ExportInfoToCompRep()
        {
            // force update
            if (this.wallQuads_IN == null || this.wallQuads_IN.Count < 1)
                this.OnCreation();

            List<ComponentReps.SurfaceInfo> surface_infos = new List<ComponentReps.SurfaceInfo>();

            // ================================================== levels ============================================================
            #region LEVELS (holes in them are not transferred as openings)
            int nrL = this.Levels.Count;
            for (int i = 0; i < nrL; i++ )
            {
                ZonedPolygonGroup zpg = this.Levels[i];

                LineBuilder b = new LineBuilder();
                
                // -------------------------------------------- axes display -----------------------------------------------------
                // add to the line display
                zpg.AddDisplayLines(ref b);
                // generate the mesh display
                MeshGeometry3D displayMesh = zpg.GetDisplayMesh();
                // get the area
                double area_i_AXES = zpg.GetAreaWoHoles();
                // get the size
                double h_i_AXES, w_i_AXES;
                zpg.GetSize(out h_i_AXES, out w_i_AXES);
                // get the defining geometry
                List<Vector3> polygon;
                List<List<Vector3>> polygon_openings;
                zpg.ExtractGeometryDef(out polygon, out polygon_openings);
                
                // ---------------------------------------- inner surface display ------------------------------------------------
                double area_i_NET = 0.0;
                double h_i_NET = 0.0, w_i_NET = 0.0;

                List<ZonedPolygonRep> zprs_in = this.defining_polygons_IN.FindAll(x => x.LevelIndex == i).ToList();
                if (zprs_in != null && zprs_in.Count > 0)
                {
                    List<List<Point3D>> coordinates_IN = new List<List<Point3D>>();
                    foreach (ZonedPolygonRep zpr_in in zprs_in)
                    {
                        // add to the line geometry
                        int nrP_in = zpr_in.Coordinates.Count;
                        for (int j = 0; j < nrP_in; j++)
                        {
                            Vector3 p1 = new Vector3(zpr_in.Coordinates[j].X, zpr_in.Coordinates[j].Y, zpr_in.Coordinates[j].Z);
                            Vector3 p2 = new Vector3(zpr_in.Coordinates[(j + 1) % nrP_in].X, zpr_in.Coordinates[(j + 1) % nrP_in].Y, zpr_in.Coordinates[(j + 1) % nrP_in].Z);
                            b.AddLine(p1, p2);
                        }

                        coordinates_IN.Add(CommonExtensions.ConvertVector4ListToPoint3DList(zpr_in.Coordinates));
                    }

                    // get the area
                    int outer_polygon_index_IN = -1;
                    List<Point3D> polygon_IN = null;
                    List<List<Point3D>> holes_IN = null;
                    MeshesCustom.ToPolygonWithHoles(coordinates_IN, Orientation.XZ, out outer_polygon_index_IN, out polygon_IN, out holes_IN);
                    area_i_NET = MeshesCustom.CalculateAreaOfPolygonWHoles(polygon_IN, holes_IN);
                    // get the size
                    Utils.CommonExtensions.GetObjAlignedSizeOf(Utils.CommonExtensions.ConvertPoints3DListToVector3List(polygon_IN), out w_i_NET, out h_i_NET);
                }

                // ---------------------------------------- outer surface display ------------------------------------------------
                double area_i_GROSS = 0.0;
                double h_i_GROSS = 0.0 , w_i_GROSS = 0.0;

                List<ZonedPolygonRep> zprs_out = this.defining_polygons_OUT.FindAll(x => x.LevelIndex == i).ToList();
                if (zprs_out != null && zprs_out.Count > 0)
                {
                    List<List<Point3D>> coordinates_OUT = new List<List<Point3D>>();
                    foreach (ZonedPolygonRep zpr_out in zprs_out)
                    {
                        // add to the line geometry
                        int nrP_in = zpr_out.Coordinates.Count;
                        for (int j = 0; j < nrP_in; j++)
                        {
                            Vector3 p1 = new Vector3(zpr_out.Coordinates[j].X, zpr_out.Coordinates[j].Y, zpr_out.Coordinates[j].Z);
                            Vector3 p2 = new Vector3(zpr_out.Coordinates[(j + 1) % nrP_in].X, zpr_out.Coordinates[(j + 1) % nrP_in].Y, zpr_out.Coordinates[(j + 1) % nrP_in].Z);
                            b.AddLine(p1, p2);
                        }

                        coordinates_OUT.Add(CommonExtensions.ConvertVector4ListToPoint3DList(zpr_out.Coordinates));
                    }

                    // get the area
                    int outer_polygon_index_OUT = -1;
                    List<Point3D> polygon_OUT = null;
                    List<List<Point3D>> holes_OUT = null;
                    MeshesCustom.ToPolygonWithHoles(coordinates_OUT, Orientation.XZ, out outer_polygon_index_OUT, out polygon_OUT, out holes_OUT);
                    area_i_GROSS = MeshesCustom.CalculateAreaOfPolygonWHoles(polygon_OUT, holes_OUT);
                    // get the size
                    Utils.CommonExtensions.GetObjAlignedSizeOf(Utils.CommonExtensions.ConvertPoints3DListToVector3List(polygon_OUT), out w_i_GROSS, out h_i_GROSS);
                }

                // --------------------------------------------- gather info -----------------------------------------------------
                ComponentReps.SurfaceInfo si_level = new ComponentReps.SurfaceInfo()
                {
                    DisplayLabel = "L " + i.ToString(),
                    DisplayLines = b.ToLineGeometry3D(),
                    DisplayMesh = displayMesh,
                    VolumeID = this.ID,
                    IsWall = false,
                    LevelOrLabel = i,
                    LabelForOpenings = -1,
                    WallLowerPoly = -1,
                    WallUpperPoly = -1,
                    Area_AXES = area_i_AXES,
                    Area_NET = area_i_NET,
                    Area_GROSS = area_i_GROSS,
                    Height_AXES = h_i_AXES,
                    Height_NET = h_i_NET,
                    Height_GROSS = h_i_GROSS,
                    Width_AXES = w_i_AXES,
                    Width_NET = w_i_NET,
                    Width_GROSS = w_i_GROSS,
                    Geometry = Utils.CommonExtensions.ConvertVector3ListToPoint3DList(polygon),
                    Material_ID = this.materials_per_level[i].ID,
                    Openings = null,
                    Consumed = false
                };
                surface_infos.Add(si_level);
            }
            #endregion

            // ================================================ wall quads ==========================================================
            #region WALLS
            int nrW = this.wallQuads.Count;
            for (int i = 0; i < nrW; i++ )
            {
                Vector3 ids = this.wallQuads.ElementAt(i).Key;
                long id_poly_lower = (long)ids.X;
                long id_poly_upper = (long)ids.Y;
                int segment_ind_in_poly_lower = (int)ids.Z;
                int label = this.wallQuads_info[i].Label;
                int index_poly_lower = this.wallQuads_info[i].PolyLow; // in this.defining_polygons
                int index_poly_upper = this.wallQuads_info[i].PolyHigh; // in this.defining_polygons

                LineBuilder b = new LineBuilder();

                // -------------------------------------------- axes display -----------------------------------------------------
                double area_i_AXES = 0.0;
                double h_i_AXES = 0.0, w_i_AXES = 0.0;

                // get the coordinates
                List<Vector3> coords = this.wallQuads.ElementAt(i).Value;
                List<List<Vector3>> opening_coords = null;

                // get the opening coords, if any openings exist
                ZonedPolygon zp_lower = this.defining_polygons.Find(x => (x.ID == id_poly_lower));
                if (zp_lower != null)
                    opening_coords = zp_lower.GetOpeningGeometryForLabel(segment_ind_in_poly_lower); // if none: empty list, not NULL

                // add to the line display
                ZonedVolume.AddCoordsToLineGeometry3D(coords, ref b);
                ZonedVolume.AddMultiCoordsToLineGeometry3D(opening_coords, ref b);

                // generate the mesh display
                List<Point3D> coords_P3D = Utils.CommonExtensions.ConvertVector3ListToPoint3DList(coords);
                List<List<Point3D>> opening_coords_P3D = (opening_coords.Count == 0) ? null : Utils.CommonExtensions.ConvertVector3ListListToPoint3DListList(opening_coords);
                MeshGeometry3D displayMesh = MeshesCustom.PolygonComplexFillAfterRotation(coords_P3D, opening_coords_P3D, true);
                                 
                // get the area
                area_i_AXES = MeshesCustom.CalculateAreaOfPolygonWHoles(coords_P3D, opening_coords_P3D);
                // get the size
                Utils.CommonExtensions.GetObjAlignedSizeOf(coords, out w_i_AXES, out h_i_AXES);


                // ---------------------------------------- inner surface display ------------------------------------------------
                double area_i_NET = 0.0;
                double h_i_NET = 0.0, w_i_NET = 0.0;

                // get the coordinates
                List<Vector3> coords_NET = this.wallQuads_IN.ElementAt(i).Value;

                // PROJECT the opening coords, if any openings exist
                List<List<Vector3>> opening_coords_IN = new List<List<Vector3>>();
                if (coords_NET.Count > 2 && opening_coords.Count > 0)
                {
                    // define the wall quad planes
                    Vector3 v0_IN = coords_NET[0];
                    Vector3 v1_IN = coords_NET[1];
                    Vector3 v2_IN = coords_NET[3];

                    foreach (List<Vector3> xs in opening_coords)
                    {
                        // project onto the wall quad (CW)
                        List<Vector3> xs_projected_IN = xs.Select(x => CommonExtensions.ProjectPointOnPlane(x, v0_IN, v2_IN, v1_IN)).ToList();
                        opening_coords_IN.Add(xs_projected_IN);
                    }
                }

                // add to the line display
                ZonedVolume.AddCoordsToLineGeometry3D(coords_NET, ref b);
                ZonedVolume.AddMultiCoordsToLineGeometry3D(opening_coords_IN, ref b);

                // get the area
                area_i_NET = MeshesCustom.CalculateAreaOfPolygonWHoles(Utils.CommonExtensions.ConvertVector3ListToPoint3DList(coords_NET),
                                                                        Utils.CommonExtensions.ConvertVector3ListListToPoint3DListList(opening_coords_IN));
                // get the size
                Utils.CommonExtensions.GetObjAlignedSizeOf(coords_NET, out w_i_NET, out h_i_NET);

                // ---------------------------------------- inner surface display ------------------------------------------------
                double area_i_GROSS = 0.0;
                double h_i_GROSS = 0.0, w_i_GROSS = 0.0;

                // get the coordinates
                List<Vector3> coords_GROSS = this.wallQuads_OUT.ElementAt(i).Value;

                // PROJECT the opening coords, if any openings exist
                List<List<Vector3>> opening_coords_OUT = new List<List<Vector3>>();
                if (coords_GROSS.Count > 2 && opening_coords.Count > 0)
                {
                    // define the wall quad planes
                    Vector3 v0_OUT = coords_GROSS[0];
                    Vector3 v1_OUT = coords_GROSS[1];
                    Vector3 v2_OUT = coords_GROSS[3];

                    foreach (List<Vector3> xs in opening_coords)
                    {
                        // project onto the wall quad (CCW)
                        List<Vector3> xs_projected_OUT = xs.Select(x => CommonExtensions.ProjectPointOnPlane(x, v0_OUT, v1_OUT, v2_OUT)).ToList();
                        opening_coords_OUT.Add(xs_projected_OUT);
                    }
                }

                // add to the line display
                ZonedVolume.AddCoordsToLineGeometry3D(coords_GROSS, ref b);
                ZonedVolume.AddMultiCoordsToLineGeometry3D(opening_coords_OUT, ref b);

                // get the area
                area_i_GROSS = MeshesCustom.CalculateAreaOfPolygonWHoles(Utils.CommonExtensions.ConvertVector3ListToPoint3DList(coords_GROSS),
                                                                         Utils.CommonExtensions.ConvertVector3ListListToPoint3DListList(opening_coords_OUT));
                // get the size
                Utils.CommonExtensions.GetObjAlignedSizeOf(coords_GROSS, out w_i_GROSS, out h_i_GROSS);

                // ----------------------------------------- gather OPENINGS info ------------------------------------------------
                
                List<ComponentReps.SurfaceInfo> si_openings = new List<ComponentReps.SurfaceInfo>();
                if (opening_coords.Count > 0)
                {
                    // ASSUMPTION: all openings are rectangles
                    int counter = 0;
                    foreach(List<Vector3> op_c in opening_coords)
                    {
                        // get the lines to display
                        LineBuilder op_b = new LineBuilder();
                        ZonedVolume.AddCoordsToLineGeometry3D(op_c, ref op_b);

                        // get area and size
                        double op_area = 0.0, op_h = 0.0, op_w = 0.0;
                        ZonedVolume.GetMeasurementsOfQuad(op_c, out op_area, out op_h, out op_w);

                        Composite4IntKey mat_o_key = new Composite4IntKey(label, index_poly_lower, index_poly_upper, counter);
                        ComponentReps.SurfaceInfo si_wall_op = new ComponentReps.SurfaceInfo()
                        {
                            DisplayLabel = "O " + counter.ToString() + "in W " + label.ToString(),
                            DisplayLines = op_b.ToLineGeometry3D(),
                            DisplayMesh = MeshesCustom.PolygonFill(op_c, false),
                            VolumeID = this.ID,
                            IsWall = true,
                            LevelOrLabel = label,
                            LabelForOpenings = counter,
                            WallLowerPoly = id_poly_lower,
                            WallUpperPoly = id_poly_upper,
                            Area_AXES = op_area,
                            Area_NET = op_area,
                            Area_GROSS = op_area,
                            Height_AXES = op_h,
                            Height_NET = op_h,
                            Height_GROSS = op_h,
                            Width_AXES = op_w,
                            Width_NET = op_w,
                            Width_GROSS = op_w,
                            Geometry = Utils.CommonExtensions.ConvertVector3ListToPoint3DList(op_c),
                            Material_ID = (this.materials_per_label.ContainsKey(mat_o_key)) ? this.materials_per_label[mat_o_key].ID : -1, // tmp, should be replaced by door or window ID
                            Openings = null,
                            Consumed = false
                        };
                        si_openings.Add(si_wall_op);
                        counter++;
                    }
                }

                // --------------------------------------------- gather info -----------------------------------------------------
                Composite4IntKey mat_key = new Composite4IntKey(label, index_poly_lower, index_poly_upper, -1);
                ComponentReps.SurfaceInfo si_wall = new ComponentReps.SurfaceInfo()
                {
                    DisplayLabel = "W " + label.ToString(),
                    DisplayLines = b.ToLineGeometry3D(),
                    DisplayMesh = displayMesh,
                    VolumeID = this.ID,
                    IsWall = true,
                    LevelOrLabel = label,
                    LabelForOpenings = -1,
                    WallLowerPoly = id_poly_lower,
                    WallUpperPoly = id_poly_upper,
                    Area_AXES = area_i_AXES,
                    Area_NET = area_i_NET,
                    Area_GROSS = area_i_GROSS,
                    Height_AXES = h_i_AXES,
                    Height_NET = h_i_NET,
                    Height_GROSS = h_i_GROSS,
                    Width_AXES = w_i_AXES,
                    Width_NET = w_i_NET,
                    Width_GROSS = w_i_GROSS,
                    Geometry = Utils.CommonExtensions.ConvertVector3ListToPoint3DList(coords),
                    Material_ID = (this.materials_per_label.ContainsKey(mat_key)) ? this.materials_per_label[mat_key].ID : -1,
                    Openings = si_openings,
                    Consumed = false
                };
                surface_infos.Add(si_wall);
                
            }
            #endregion

            return surface_infos;
        }

        private static void AddCoordsToLineGeometry3D(List<Vector3> _coords, ref LineBuilder _b)
        {
            if (_coords == null) return;
            if (_coords.Count < 2) return;
            if (_b == null) return;

            int nrC = _coords.Count;
            for(int i = 0; i < nrC; i++)
            {
                _b.AddLine(_coords[i], _coords[(i + 1) % nrC]);
            }
        }

        private static void AddMultiCoordsToLineGeometry3D(List<List<Vector3>> _coords, ref LineBuilder _b)
        {
            if (_coords == null) return;
            if (_coords.Count < 1) return;
            if (_b == null) return;

            foreach(List<Vector3> coords_i in _coords)
            {
                ZonedVolume.AddCoordsToLineGeometry3D(coords_i, ref _b);
            }
        }

        private static void AddCoordsToLineGeometry3D(List<Point3D> _coords, ref LineBuilder _b)
        {
            if (_coords == null) return;
            if (_coords.Count < 2) return;
            if (_b == null) return;

            int nrC = _coords.Count;
            for (int i = 0; i < nrC; i++)
            {
                Vector3 v1 = new Vector3((float)_coords[i].X, (float)_coords[i].Y, (float)_coords[i].Z);
                Vector3 v2 = new Vector3((float)_coords[(i + 1) % nrC].X, (float)_coords[(i + 1) % nrC].Y, (float)_coords[(i + 1) % nrC].Z);
                _b.AddLine(v1, v2);
            }
        }

        private static void AddMultiCoordsToLineGeometry3D(List<List<Point3D>> _coords, ref LineBuilder _b)
        {
            if (_coords == null) return;
            if (_coords.Count < 1) return;
            if (_b == null) return;

            foreach (List<Point3D> coords_i in _coords)
            {
                ZonedVolume.AddCoordsToLineGeometry3D(coords_i, ref _b);
            }
        }

        private static void GetMeasurementsOfQuad(List<Vector3> _quad, out double area, out double h, out double w)
        {
            area = 0.0;
            h = 0.0;
            w = 0.0;

            if (_quad == null) return;
            if (_quad.Count < 4) return;

            w = Vector3.Distance(_quad[0], _quad[1]);
            h = Vector3.Distance(_quad[1], _quad[2]);
            area = w * h;
        }

        #endregion

        #region EXPORT Detailed Into for Neighborhood Search

        public List<SpatialOrganization.SurfaceBasicInfo> ExportBasicInfoForNeighborhoodTest()
        {
            // force update
            if (this.wallQuads_IN == null || this.wallQuads_IN.Count < 1)
                this.OnCreation();

            List<SpatialOrganization.SurfaceBasicInfo> surface_infos = new List<SpatialOrganization.SurfaceBasicInfo>();

            // ================================================== levels ============================================================
            #region LEVELS
            int nrL = this.Levels.Count;
            for (int i = 0; i < nrL; i++)
            {
                // leave out intermediate levels as they do not represent surfaces
                if (!this.levels_info[i].IsBottomClosure && !this.levels_info[i].IsTopClosure) continue;

                ZonedPolygonGroup zpg = this.Levels[i];

                LineBuilder b = new LineBuilder();

                // ---------------------------------------- axes info and display ------------------------------------------------
                // add to the line display
                zpg.AddDisplayLines(ref b);
                // generate the mesh display
                MeshGeometry3D displayMesh = zpg.GetDisplayMesh();
                // get the area
                double area_i_AXES = zpg.GetAreaWoHoles();
                // get the size
                double h_i_AXES, w_i_AXES;
                zpg.GetSize(out h_i_AXES, out w_i_AXES);
                // get the defining geometry
                List<Vector3> polygon;
                List<List<Vector3>> polygon_openings;
                zpg.ExtractGeometryDef(out polygon, out polygon_openings);

                // ----------------------------------------- gather OPENINGS info ------------------------------------------------

                List<SpatialOrganization.SurfaceBasicInfo> sbi_openings = new List<SpatialOrganization.SurfaceBasicInfo>();
                if (polygon_openings.Count > 0)
                {
                    // ASSUMPTION: all openings are rectangles
                    int counter = 0;
                    foreach (List<Vector3> op_c in polygon_openings)
                    {
                        // get the lines to display
                        LineBuilder op_b = new LineBuilder();
                        ZonedVolume.AddCoordsToLineGeometry3D(op_c, ref op_b);

                        // get area and size
                        double op_area = 0.0, op_h = 0.0, op_w = 0.0;
                        ZonedVolume.GetMeasurementsOfQuad(op_c, out op_area, out op_h, out op_w);

                        SpatialOrganization.SurfaceBasicInfo sbi_wall_op = new SpatialOrganization.SurfaceBasicInfo(op_b.ToLineGeometry3D(), MeshesCustom.PolygonFill(op_c, false), op_c,
                                                                                            this.ID, false, i, counter, -1, -1, op_area, op_h, op_w, this.materials_per_level[i].ID);

                        sbi_openings.Add(sbi_wall_op);
                        counter++;
                    }
                }
                // --------------------------------------------- gather info -----------------------------------------------------
                SpatialOrganization.SurfaceBasicInfo sbi_level = new SpatialOrganization.SurfaceBasicInfo(b.ToLineGeometry3D(), displayMesh, polygon,
                                                                                    this.ID, false, i, -1, -1, -1, area_i_AXES, h_i_AXES, w_i_AXES, this.materials_per_level[i].ID);

                sbi_level.Openings = sbi_openings;
                surface_infos.Add(sbi_level);
            }
            #endregion

            // ================================================ wall quads ==========================================================
            #region WALLS
            int nrW = this.wallQuads.Count;
            for (int i = 0; i < nrW; i++)
            {
                Vector3 ids = this.wallQuads.ElementAt(i).Key;
                long id_poly_lower = (long)ids.X;
                long id_poly_upper = (long)ids.Y;
                int label = this.wallQuads_info[i].Label;

                LineBuilder b = new LineBuilder();

                // -------------------------------------------- axes display -----------------------------------------------------
                double area_i_AXES = 0.0;
                double h_i_AXES = 0.0, w_i_AXES = 0.0;

                // get the coordinates
                List<Vector3> coords = this.wallQuads.ElementAt(i).Value;
                List<List<Vector3>> opening_coords = null;

                // get the opening coords, if any openings exist
                ZonedPolygon zp_lower = this.defining_polygons.Find(x => (x.ID == id_poly_lower));
                if (zp_lower != null)
                    opening_coords = zp_lower.GetOpeningGeometryForLabel(label); // if none: empty list, not NULL

                // add to the line display
                ZonedVolume.AddCoordsToLineGeometry3D(coords, ref b);
                ZonedVolume.AddMultiCoordsToLineGeometry3D(opening_coords, ref b);

                // generate the mesh display
                List<Point3D> coords_P3D = Utils.CommonExtensions.ConvertVector3ListToPoint3DList(coords);
                List<List<Point3D>> opening_coords_P3D = (opening_coords.Count == 0) ? null : Utils.CommonExtensions.ConvertVector3ListListToPoint3DListList(opening_coords);
                MeshGeometry3D displayMesh = MeshesCustom.PolygonComplexFillAfterRotation(coords_P3D, opening_coords_P3D, true);

                // get the area
                area_i_AXES = MeshesCustom.CalculateAreaOfPolygonWHoles(coords_P3D, opening_coords_P3D);
                // get the size
                Utils.CommonExtensions.GetObjAlignedSizeOf(coords, out w_i_AXES, out h_i_AXES);


                // ----------------------------------------- gather OPENINGS info ------------------------------------------------

                List<SpatialOrganization.SurfaceBasicInfo> sbi_openings = new List<SpatialOrganization.SurfaceBasicInfo>();
                if (opening_coords.Count > 0)
                {
                    // ASSUMPTION: all openings are rectangles
                    int counter = 0;
                    foreach (List<Vector3> op_c in opening_coords)
                    {
                        // get the lines to display
                        LineBuilder op_b = new LineBuilder();
                        ZonedVolume.AddCoordsToLineGeometry3D(op_c, ref op_b);

                        // get area and size
                        double op_area = 0.0, op_h = 0.0, op_w = 0.0;
                        ZonedVolume.GetMeasurementsOfQuad(op_c, out op_area, out op_h, out op_w);

                        Composite4IntKey mat_o_key = new Composite4IntKey(label, (int)id_poly_lower, (int)id_poly_upper, counter);
                        long mat_o_ID = (this.materials_per_label.ContainsKey(mat_o_key)) ? this.materials_per_label[mat_o_key].ID : -1;
                        SpatialOrganization.SurfaceBasicInfo sbi_wall_op = new SpatialOrganization.SurfaceBasicInfo(op_b.ToLineGeometry3D(), MeshesCustom.PolygonFill(op_c, false), op_c,
                                                                                            this.ID, true, label, counter, id_poly_lower, id_poly_upper,
                                                                                            op_area, op_h, op_w, mat_o_ID);


                        sbi_openings.Add(sbi_wall_op);
                        counter++;
                    }
                }

                // --------------------------------------------- gather info -----------------------------------------------------

                Composite4IntKey mat_key = new Composite4IntKey(label, (int)id_poly_lower, (int)id_poly_upper, -1);
                long mat_ID = (this.materials_per_label.ContainsKey(mat_key)) ? this.materials_per_label[mat_key].ID : -1;
                SpatialOrganization.SurfaceBasicInfo sbi_wall = new SpatialOrganization.SurfaceBasicInfo(b.ToLineGeometry3D(), displayMesh, coords,
                                                                                 this.ID, true, label, -1, id_poly_lower, id_poly_upper,
                                                                                 area_i_AXES, h_i_AXES, w_i_AXES, mat_ID);
                sbi_wall.Openings = sbi_openings;
                surface_infos.Add(sbi_wall);
            }
            #endregion

            return surface_infos;
        }

        public int GetNrOfEnclosingSurfaces()
        {
            // force update
            if (this.wallQuads_IN == null || this.wallQuads_IN.Count < 1)
                this.OnCreation();

            return this.Levels.Count + this.wallQuads.Count;
        }



        #endregion

        #region HIT-TEST: Detect User-Selected Quad

        /// <summary>
        /// Performs the hit test only for the walls.
        /// </summary>
        /// <param name="_hit_point"></param>
        /// <returns></returns>
        public List<Vector3> GetHitQuad(Vector3 _hit_point)
        {
            // test the walls only
            for(int i = 0; i < this.wallQuads_info.Count; i++)
            {
                List<Vector3> quad = new List<Vector3>(this.wallQuads_info[i].Quad);
                Vector3 normal = this.wallQuads_info[i].QuadNormal;
                normal.Normalize();

                // test if _hit_point lies on the plane of the quad
                bool hit_point_in_quad = false;
                foreach(Vector3 qP in quad)
                {
                    Vector3 v_to_hit_point = _hit_point - qP;
                    if (v_to_hit_point.Length() > CommonExtensions.LINEDISTCALC_TOLERANCE * 100)
                    {
                        v_to_hit_point.Normalize();
                        if (Math.Abs(Vector3.Dot(v_to_hit_point, normal)) <= CommonExtensions.GENERAL_CALC_TOLERANCE)
                        {
                            hit_point_in_quad = true;
                            break;
                        }   
                    }
                }
                if (hit_point_in_quad)
                    return quad;
            }
            return new List<Vector3>();
        }

        public Vector3 GetHorizontalAxisOfHitWall(Vector3 _hit_point, out bool _a_wall_was_hit)
        {
            _a_wall_was_hit = false;
            List<Vector3> hit_quad = this.GetHitQuad(_hit_point);
            if (hit_quad.Count > 0)
            {
                _a_wall_was_hit = true;
                return MeshesCustom.GetHorizontalAxisOf(hit_quad);
            }
            else
                return Vector3.UnitX;
        }

        #endregion
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // ==================================== VALUE CONVERTER FOR EDIT MODE TYPES =============================== //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region VALUE CONVERTER: ZonedVolumeEditModeType

    [ValueConversion(typeof(ZonedVolumeEditModeType), typeof(Boolean))]
    public class VolumeEditModeTypeToBooleanConverter : IValueConverter
    {
        // in order to react to more than one action mode at once
        // use + as OR; * as AND
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            ZonedVolumeEditModeType zvet = ZonedVolumeEditModeType.NONE;
            if (value is ZonedVolumeEditModeType)
                zvet = (ZonedVolumeEditModeType)value;

            string str_param = parameter.ToString();

            string[] str_params_OR = str_param.Split(new char[] { '+' });

            if (str_params_OR.Count() < 2)
                return (zvet == ZonedVolume.GetEditModeType(str_param));
            else
            {
                foreach (string p in str_params_OR)
                {
                    if (zvet == ZonedVolume.GetEditModeType(p))
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
