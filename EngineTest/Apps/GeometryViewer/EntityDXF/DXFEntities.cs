using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Vector3D = System.Windows.Media.Media3D.Vector3D;
using Point3D = System.Windows.Media.Media3D.Point3D;

using GeometryViewer.EntityGeometry;
using GeometryViewer.ComponentInteraction;

namespace GeometryViewer.EntityDXF
{
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // ============================================= BASE TYPE: ENTITY ======================================== //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region DXF_Entity
    public class DXFEntity
    {
        #region CLASS MEMBERS
        public string ENT_Name { get; protected set; }
        public string ENT_ClassName { get; protected set; }
        public long ENT_ID { get; protected set; }
        public bool ENT_HasEntites { get; protected set; }
        internal DXFDecoder Decoder { get; set; }

        internal bool deferred_execute_OnLoad;

        #endregion

        #region .CTOR

        public DXFEntity()
        {
            this.ENT_Name = null;
            this.ENT_ClassName = null;
            this.ENT_ID = -1;
            this.ENT_HasEntites = false;

            this.deferred_execute_OnLoad = false;
        }

        #endregion

        #region METHODS: Entity parsing

        public void ParseNext()
        {
            // start parsing next entity
            this.ReadProperties();
            // if it contains entities itself, parse them next
            if (this.ENT_HasEntites)
                this.ReadEntities();
        }

        protected void ReadEntities()
        {
            DXFEntity e;
            do
            {
                this.Decoder.PositionToOutputWindow("ReadEntities '" + this.ENT_Name + "'");
                if (this.Decoder.FValue == DXFUtils.EOF)
                {
                    // end of file
                    this.Decoder.ReleaseRessources();
                    return;
                }
                e = this.Decoder.CreateEntity(this.ENT_Name);
                if (e == null)
                {
                    // reached end of complex entity
                    this.Decoder.Next();
                    break;
                }
                if (e is DXFContinue)
                {
                    // carry on parsing the same entity
                    this.ParseNext();
                    break;
                }
                e.ParseNext();
                if (e.GetType().IsSubclassOf(typeof(DXFEntity)))
                {
                    // complete parsing
                    e.OnLoaded();
                    // add to list of entities of this entity
                    this.AddEntity(e);
                }
            }
            while (this.Decoder.HasNext());
        }

        #endregion

        #region METHODS: Property parsing

        protected void ReadProperties()
        {
            while (this.Decoder.HasNext())
            {
                this.Decoder.Next();
                switch (this.Decoder.FCode)
                {
                    case (int)EntitySaveCode.ENTITY_START:
                        // reached next entity
                        return;
                    default:
                        // otherwise continue parsing
                        this.ReadPoperty();
                        break;
                }
            }
        }

        public virtual void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)EntitySaveCode.CLASS_NAME:
                    this.ENT_ClassName = this.Decoder.FValue;
                    break;
                case (int)EntitySaveCode.ENTITY_ID:
                    this.ENT_ID = this.Decoder.LongValue();
                    break;
            }
        }

        #endregion

        #region METHODS: For Subtypes

        public virtual void OnLoaded() 
        { }

        public virtual bool AddEntity(DXFEntity _e)
        {
            return false;
        }

        #endregion

    }
    #endregion

    #region DXF_Dummy_Entity

    public class DXFDummy : DXFEntity
    {
        public DXFDummy()
        {
            this.ENT_Name = null;
            this.ENT_ClassName = null;
            this.ENT_ID = -1;
            this.ENT_HasEntites = false;

            this.deferred_execute_OnLoad = false;
        }
        public DXFDummy(string _name)
        {
            this.ENT_Name = _name;
            this.ENT_ClassName = null;
            this.ENT_ID = -1;
            this.ENT_HasEntites = false;

            this.deferred_execute_OnLoad = false;
        }

        public override string ToString()
        {
            string dxfS = base.ToString();
            if (this.ENT_Name != null)
                dxfS += "[" + this.ENT_Name + "]";

            return dxfS;
        }
    }

    #endregion

    #region DXF_CONTINUE

    public class DXFContinue : DXFEntity
    {
        public DXFContinue()
        {
            this.ENT_Name = DXFUtils.ENTITY_CONTINUE;
            this.ENT_ClassName = null;
            this.ENT_ID = -1;
            this.ENT_HasEntites = false;

            this.deferred_execute_OnLoad = false;
        }
    }
    #endregion

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // ============================================== CUSTOM ENTITIES ========================================= //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // ------------------------------------------------ DXFMaterial ------------------------------------------- //

    // wrapper class for class ComponentInteraction.Material
    #region DXF_Material

    public class DXFMaterial : DXFEntity
    {
        #region CLASS MEMBERS

        public long dxf_ID { get; protected set; }
        public string dxf_Name { get; protected set; }
        public double dxf_Thickness { get; protected set; }
        public MaterialPosToWallAxisPlane dxf_Position { get; protected set; }
        public double dxf_AccArea { get; protected set; }
        public int dxf_NrSurfaces { get; protected set; }
        public bool dxf_isBound2CR { get; protected set; }
        public long dxf_BoundCRID { get; protected set; }

        internal Material dxf_parsed;

        #endregion

        public DXFMaterial()
        {
            this.deferred_execute_OnLoad = false;

            this.dxf_ID = -1;
            this.dxf_Name = string.Empty;
            this.dxf_Thickness = 0.0;
            this.dxf_Position = MaterialPosToWallAxisPlane.MIDDLE;
            this.dxf_AccArea = 0.0;
            this.dxf_NrSurfaces = 0;
            this.dxf_isBound2CR = false;
            this.dxf_BoundCRID = -1;
        }

        #region OVERRIDES: Read Property

        public override void ReadPoperty()
        {
            this.Decoder.PositionToOutputWindow("ReadProperty MAT '" + this.dxf_Name + "'");
            switch(this.Decoder.FCode)
            {
                case (int)EntitySaveCode.ENTITY_NAME:
                    this.ENT_Name = this.Decoder.FValue;
                    break;
                case (int)MaterialSaveCode.Name:
                    this.dxf_Name = this.Decoder.FValue;
                    break;
                case (int)MaterialSaveCode.THICKNESS:
                    this.dxf_Thickness = this.Decoder.DoubleValue();
                    break;
                case (int)MaterialSaveCode.POSITION:
                    this.dxf_Position = Material.String2MPTWAP(this.Decoder.FValue);
                    break;
                case (int)MaterialSaveCode.ACC_AREA:
                    this.dxf_AccArea = this.Decoder.DoubleValue();
                    break;
                case (int)MaterialSaveCode.IS_BOUND_2CR:
                    this.dxf_isBound2CR = (this.Decoder.IntValue() == 1) ? true : false;
                    break;
                case (int)MaterialSaveCode.BOUND_CRID:
                    this.dxf_BoundCRID = this.Decoder.LongValue();
                    break;
                case (int)MaterialSaveCode.NR_ASSOC_SURFACES:
                    this.dxf_NrSurfaces = this.Decoder.IntValue();
                    break;
                default:
                    // DXFEntity: CLASS_NAME, ENT_ID
                    base.ReadPoperty();
                    this.dxf_ID = this.ENT_ID;
                    break;
            }
        }

        #endregion

        #region OVERRIDES: Post-Processing

        public override void OnLoaded()
        {
            base.OnLoaded();

            if (this.Decoder.MLManager == null) return;
            this.Decoder.PositionToOutputWindow("[OnLoaded MAT '" + this.dxf_Name + "']");

            this.dxf_parsed = this.Decoder.MLManager.ReconstructMaterial(this.dxf_ID, this.dxf_Name, (float)this.dxf_Thickness, this.dxf_Position, 
                                                                            (float)this.dxf_AccArea, this.dxf_NrSurfaces, this.dxf_isBound2CR, this.dxf_BoundCRID);
        }

        #endregion
    }

    #endregion

    // ---------------------------------------------- DXFZoneOpening ------------------------------------------ //
    
    // wrapper class for class EntityGeometry.ZonedPolygon.ZoneOpening
    #region DXF_ZoneOpening

    public class DXFZoneOpening : DXFEntity
    {
        #region CLASS MEMBERS

        public long dxf_ID { get; protected set; }
        public string dxf_Name { get; protected set; }
        public int dxf_ind_in_poly { get; protected set; }

        protected Vector3D dxf_v_prev;
        public Vector3D dxf_VPrev { get { return this.dxf_v_prev; } }

        protected Vector3D dxf_v_next;
        public Vector3D dxf_VNext { get { return this.dxf_v_next; } }

        protected Vector3D dxf_poly_wallT;
        public Vector3D dxf_PolyWallT { get { return this.dxf_poly_wallT; } }

        protected Vector3D dxf_poly_wallT_adj;
        public Vector3D dxf_PolyWallTAdj { get { return this.dxf_poly_wallT_adj; } }

        public double dxf_dist_from_v_prev { get; protected set; }
        public double dxf_len_along_segm { get; protected set; }
        public double dxf_dist_from_poly { get; protected set; }
        public double dxf_height_along_wallT { get; protected set; }

        private bool dxf_v_prev_in_progress;
        private bool dxf_v_next_in_progress;
        private bool dxf_poly_wallT_in_progress;
        private bool dxf_poly_wallT_adj_in_progress;

        #endregion

        public DXFZoneOpening()
        {
            this.deferred_execute_OnLoad = false;

            this.dxf_ID = -1;
            this.dxf_Name = string.Empty;
            this.dxf_ind_in_poly = -1;

            this.dxf_v_prev = new Vector3D(0, 0, 0);
            this.dxf_v_next = new Vector3D(0, 0, 0);
            this.dxf_poly_wallT = new Vector3D(0, 0, 0);
            this.dxf_poly_wallT_adj = new Vector3D(0, 0, 0);

            this.dxf_v_prev_in_progress = false;
            this.dxf_v_next_in_progress = false;
            this.dxf_poly_wallT_in_progress = false;
            this.dxf_poly_wallT_adj_in_progress = false;

            this.dxf_dist_from_v_prev = 0f;
            this.dxf_len_along_segm = 0f;
            this.dxf_dist_from_poly = 0f;
            this.dxf_height_along_wallT = 0f;          
        }

        #region OVERRIDES: Read Property

        public override void ReadPoperty()
        {
            this.Decoder.PositionToOutputWindow("ReadProperty zpOp '" + this.ENT_Name + "'");
            switch (this.Decoder.FCode)
            {
                case (int)EntitySaveCode.ENTITY_NAME:
                    this.ENT_Name = this.Decoder.FValue;
                    break;
                case (int)ZonedPolygonOpeningSaveCode.NAME:
                    this.dxf_Name = this.Decoder.FValue;
                    break;
                case (int)ZonedPolygonOpeningSaveCode.IND_IN_POLYGON:
                    this.dxf_ind_in_poly = this.Decoder.IntValue();
                    break;
                case (int)ZonedPolygonOpeningSaveCode.VERTEX_PREV:
                    this.dxf_v_prev_in_progress = true;
                    break;
                case (int)ZonedPolygonOpeningSaveCode.VERTEX_NEXT:
                    this.dxf_v_next_in_progress = true;
                    break;
                case (int)ZonedPolygonOpeningSaveCode.POLYGON_WALL_TANGENT:
                    this.dxf_poly_wallT_in_progress = true;
                    break;
                case (int)ZonedPolygonOpeningSaveCode.ADJUSTED_POLYGON_WALL_TANGENT:
                    this.dxf_poly_wallT_adj_in_progress = true;
                    break;
                case (int)ZonedPolygonOpeningSaveCode.DIST_FROM_VERTEX_PREV:
                    this.dxf_dist_from_v_prev = this.Decoder.DoubleValue();
                    break;
                case (int)ZonedPolygonOpeningSaveCode.LENGTH_ALONG_POLYGON_SEGMENT:
                    this.dxf_len_along_segm = this.Decoder.DoubleValue();
                    break;
                case (int)ZonedPolygonOpeningSaveCode.DIST_FROM_POLYGON_ALONG_WALL_TANGENT:
                    this.dxf_dist_from_poly = this.Decoder.DoubleValue();
                    break;
                case (int)ZonedPolygonOpeningSaveCode.HEIGHT_ALONG_WALL_TANGENT:
                    this.dxf_height_along_wallT = this.Decoder.DoubleValue();
                    break;
                case (int)EntitySaveCode.X_VALUE:
                    if (this.dxf_v_prev_in_progress)
                        this.dxf_v_prev.X = this.Decoder.DoubleValue();
                    else if (this.dxf_v_next_in_progress)
                        this.dxf_v_next.X = this.Decoder.DoubleValue();
                    else if (this.dxf_poly_wallT_in_progress)
                        this.dxf_poly_wallT.X = this.Decoder.DoubleValue();
                    else if (this.dxf_poly_wallT_adj_in_progress)
                        this.dxf_poly_wallT_adj.X = this.Decoder.DoubleValue();
                    break;
                case (int)EntitySaveCode.Y_VALUE:
                    if (this.dxf_v_prev_in_progress)
                        this.dxf_v_prev.Y = this.Decoder.DoubleValue();
                    else if (this.dxf_v_next_in_progress)
                        this.dxf_v_next.Y = this.Decoder.DoubleValue();
                    else if (this.dxf_poly_wallT_in_progress)
                        this.dxf_poly_wallT.Y = this.Decoder.DoubleValue();
                    else if (this.dxf_poly_wallT_adj_in_progress)
                        this.dxf_poly_wallT_adj.Y = this.Decoder.DoubleValue();
                    break;
                case (int)EntitySaveCode.Z_VALUE:
                    if (this.dxf_v_prev_in_progress)
                    {
                        this.dxf_v_prev.Z = this.Decoder.DoubleValue();
                        this.dxf_v_prev_in_progress = false;
                    }
                    else if (this.dxf_v_next_in_progress)
                    {
                        this.dxf_v_next.Z = this.Decoder.DoubleValue();
                        this.dxf_v_next_in_progress = false;
                    }
                    else if (this.dxf_poly_wallT_in_progress)
                    {
                        this.dxf_poly_wallT.Z = this.Decoder.DoubleValue();
                        this.dxf_poly_wallT_in_progress = false;
                    }
                    else if (this.dxf_poly_wallT_adj_in_progress)
                    {
                        this.dxf_poly_wallT_adj.Z = this.Decoder.DoubleValue();
                        this.dxf_poly_wallT_adj_in_progress = false;
                    }
                    break;
                default:
                    // DXFEntity: CLASS_NAME, ENT_ID
                    base.ReadPoperty();
                    this.dxf_ID = this.ENT_ID;
                    break;
            }
        }

        #endregion

    }

    #endregion

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // =============================== HIERARCHICAL COLLECTIONS OF ENTITIES =================================== //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region DXF_Entity_Container

    public class DXFEntityContainer : DXFEntity
    {
        #region CLASS MEMBERS

        internal List<DXFEntity> EC_Entities;

        #endregion

        #region .CTOR

        public DXFEntityContainer()
            : base()
        {
            this.ENT_HasEntites = true;
            this.EC_Entities = new List<DXFEntity>();
        }

        #endregion

        #region OVERRIDES

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)EntitySaveCode.ENTITY_NAME:
                    this.ENT_Name = this.Decoder.FValue;
                    break;
                default:
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    break;
            }
        }

        public override bool AddEntity(DXFEntity _e)
        {
            if (_e != null)
                this.EC_Entities.Add(_e);
            return (_e != null);
        }

        public override string ToString()
        {
            string dxfS = "DXF EXTITY CONTAINER:";
            dxfS += this.ContentToString();
            return dxfS;
        }

        protected string ContentToString()
        {
            string dxfS = string.Empty;
            if (this.ENT_Name != null && this.ENT_Name.Count() > 0)
                dxfS += ": " + this.ENT_Name;
            int n = this.EC_Entities.Count;
            dxfS += " has " + n.ToString() + " entities:\n";
            for (int i = 0; i < n; i++)
            {
                dxfS += "_[ " + i + "]_" + this.EC_Entities[i].ToString() + "\n";
            }
            dxfS += "\n";
            return dxfS;
        }

        #endregion
    }

    #endregion

    // ------------------------------------------------ DXFSection -------------------------------------------- //

    #region DXF_Section

    public class DXFSection : DXFEntityContainer
    {
        public override void ReadPoperty()
        {
            if ((this.ENT_Name == null) && (this.Decoder.FCode == (int)EntitySaveCode.ENTITY_NAME))
            {
                this.ENT_Name = this.Decoder.FValue;
            }
            switch (this.ENT_Name)
            {
                case DXFUtils.ENTITY_SECTION:
                    this.Decoder.FEntities = this;
                    break;
            }
        }

        public override string ToString()
        {
            string dxfS = "DXF SECTION:";
            dxfS += this.ContentToString();
            return dxfS;
        }
    }

    #endregion

    // ----------------------------------- DXF Helper for Hierarchical Containers ----------------------------- //

    #region DXF_Hierarchical_Container

    public class DXFHierarchicalContainer : DXFEntityContainer
    {
        public DXFHierarchicalContainer(string _name_prefix)
        {
            this.ENT_Name = "HC " + _name_prefix;
        }
        public override void ReadPoperty()
        {
            this.Decoder.PositionToOutputWindow("ReadProperty Container '" + this.ENT_Name + "'");
            base.ReadPoperty();
        }

        public override bool AddEntity(DXFEntity _e)
        {
            this.Decoder.PositionToOutputWindow("AE Container '" + this.ENT_Name + "'");
            return base.AddEntity(_e);
        }

        public override void OnLoaded()
        {
            this.Decoder.PositionToOutputWindow("[OnLoaded Container '" + this.ENT_Name + "']");
            base.OnLoaded();
        }
        public override string ToString()
        {
            string dxfS = "DXF HContainer";
            dxfS += this.ContentToString();
            return dxfS;
        }
    }

    
    #endregion

    // ------------------------------------------------- DXFEntity -------------------------------------------- //

    #region DXF_GV_Entity

    public abstract class DXFGVEntity : DXFEntityContainer
    {
        #region CLASS MEMBERS

        public long dxf_Eninty_ID { get; protected set; }
        public string dxf_Enitiy_Name { get; protected set; }
        public EntityVisibility dxf_Visiblity { get; protected set; }
        public SharpDX.Color dxf_Entity_Color { get; protected set; }
        public bool dxf_IsValid { get; protected set; }
        public bool dxf_AssociatedWComp { get; protected set; }
        public double dxf_LineThicknessGUI { get; protected set; }
        public List<string> dxf_Text { get; protected set; }
        public bool dxf_IsTopClosure { get; protected set; }
        public bool dxf_IsBottomClosure { get; protected set; }

        protected List<Entity> dxf_ContainedEntities;
        protected int dxf_nr_ContainedEntities;

        #endregion

        public DXFGVEntity()
        {
            this.deferred_execute_OnLoad = false;

            this.dxf_Eninty_ID = -1;
            this.dxf_Enitiy_Name = string.Empty;
            this.dxf_Visiblity = EntityVisibility.UNKNOWN;
            this.dxf_Entity_Color = SharpDX.Color.Black;
            this.dxf_IsValid = false;
            this.dxf_AssociatedWComp = false;
            this.dxf_LineThicknessGUI = 0.25;
            this.dxf_Text = new List<string>();
            this.dxf_IsTopClosure = false;
            this.dxf_IsBottomClosure = false;

            this.dxf_nr_ContainedEntities = 0;
        }

        #region OVERRIDES: Processing

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)EntitySaveCode.ENTITY_NAME:
                    this.ENT_Name = this.Decoder.FValue;
                    this.dxf_Enitiy_Name = this.ENT_Name;
                    break;
                case (int)EntitySaveCode.VISIBILITY:
                    this.dxf_Visiblity = Entity.SaveCode2Visibility(this.Decoder.IntValue());
                    break;
                case (int)DXFSpecSaveCodes.COLOR_INDEX:
                    DXFImportExport.DXFColor col_IND = DXFImportExport.DXFColor.Index2DXFColor(this.Decoder.IntValue());
                    this.dxf_Entity_Color = new SharpDX.Color(col_IND.R, col_IND.G, col_IND.B, col_IND.A);
                    break;
                case (int)EntitySaveCode.TRUECOLOR:
                    DXFImportExport.DXFColor col_TC = DXFImportExport.DXFColor.TrueColor2DXFColor(this.Decoder.IntValue());
                    this.dxf_Entity_Color = new SharpDX.Color(col_TC.R, col_TC.G, col_TC.B, col_TC.A);
                    break;
                case (int)EntitySpecificSaveCode.VALIDITY:
                    this.dxf_IsValid = (this.Decoder.IntValue() == 1);
                    break;
                case (int)EntitySpecificSaveCode.ASSOC_W_OTHER:
                    this.dxf_AssociatedWComp = (this.Decoder.IntValue() == 1);
                    break;
                case (int)EntitySpecificSaveCode.VISLINE_THICKNESS:
                    this.dxf_LineThicknessGUI = this.Decoder.DoubleValue();
                    break;
                case (int)EntitySpecificSaveCode.TEXT_LINE:
                    this.dxf_Text.Add(this.Decoder.FValue);
                    break;
                case (int)EntitySpecificSaveCode.IS_TOP_CLOSURE:
                    this.dxf_IsTopClosure = (this.Decoder.IntValue() == 1);
                    break;
                case (int)EntitySpecificSaveCode.IS_BOTTOM_CLOSURE:
                    this.dxf_IsBottomClosure = (this.Decoder.IntValue() == 1);
                    break;
                case (int)EntitySpecificSaveCode.CONTAINED_ENTITIES:
                    this.dxf_nr_ContainedEntities = this.Decoder.IntValue();
                    break;
                default:
                    // DXFEntity: CLASS_NAME, ENT_ID
                    base.ReadPoperty();
                    this.dxf_Eninty_ID = this.ENT_ID;
                    break;
            }
        }

        #endregion
    }

    #endregion

    // ------------------------------------------------- DXFLayer --------------------------------------------- //

    #region DXF_Layer

    public class DXFLayer : DXFGVEntity
    {
        #region CLASS MEMBERS

        protected List<Layer> dxf_ContainedLayers;
        protected List<GeometricEntity> dxf_ContainedGeometry;
        
        protected Layer dxf_parsed;

        private List<DXFEntity> for_deferred_adding;

        #endregion

        public DXFLayer()
        {
            this.deferred_execute_OnLoad = false;

            this.dxf_ContainedEntities = new List<Entity>();
            this.dxf_ContainedLayers = new List<Layer>();
            this.dxf_ContainedGeometry = new List<GeometricEntity>();

            this.for_deferred_adding = new List<DXFEntity>();
        }

        #region OVERRIDES: Processing

        public override void ReadPoperty()
        {
            this.Decoder.PositionToOutputWindow("ReadProperty Layer '" + this.ENT_Name + "'");
            base.ReadPoperty();
        }
        #endregion

        #region OVERRIDES: Adding Entities

        public override bool AddEntity(DXFEntity _e)
        {
            this.Decoder.PositionToOutputWindow("AE Layer '" + this.ENT_Name + "'");

            // handle according to type
            if (_e == null) return false;
            bool add_successful = false;

            DXFHierarchicalContainer container = _e as DXFHierarchicalContainer;
            if (container != null)
            {
                add_successful = true;
                foreach(DXFEntity sE in container.EC_Entities)
                {
                    if (this.dxf_nr_ContainedEntities <= (this.dxf_ContainedLayers.Count + this.dxf_ContainedGeometry.Count))
                        break;

                    if (sE.deferred_execute_OnLoad)
                    {
                        this.for_deferred_adding.Add(sE);
                        continue;
                    }

                    DXFLayer subLayer = sE as DXFLayer;
                    if (subLayer != null)
                    {
                        // remove the layer from the top hierarchy level
                        this.Decoder.EManager.RemoveEntity(subLayer.dxf_parsed);
                        // take the parsed layer
                        this.dxf_ContainedLayers.Add(subLayer.dxf_parsed);
                        add_successful &= true;
                    }

                    DXFZonedPolygon poly = sE as DXFZonedPolygon;
                    if (poly != null && poly.dxf_parsed != null)
                    {
                        // set the polygon Layer (the Setter adds it to the ContainedEntites of the Layer)
                        // poly.dxf_parsed.EntityLayer = this.dxf_parsed; // at this point NULL
                        this.dxf_ContainedGeometry.Add(poly.dxf_parsed);
                        add_successful &= (poly.dxf_LayerName == this.dxf_Enitiy_Name);
                    }

                    DXFZonedVolume vol = sE as DXFZonedVolume;
                    if (vol != null && vol.dxf_parsed != null)
                    {
                        // set the volume Layer (the Setter adds it to the ContainedEntites of the Layer)
                        // vol.dxf_parsed.EntityLayer = this.dxf_parsed; // at this point NULL
                        this.dxf_ContainedGeometry.Add(vol.dxf_parsed);
                        add_successful &= (vol.dxf_LayerName == this.dxf_Enitiy_Name);
                    }
                }
            }

            if (this.for_deferred_adding.Count > 0)
                this.Decoder.DeferAddEntityExecution(this);

            return add_successful;
        }

        #endregion

        #region OVERRIDES: Post-Processing

        public override void OnLoaded()
        {
            base.OnLoaded();
            if (this.Decoder.EManager == null) return;
            this.Decoder.PositionToOutputWindow("[OnLoaded Layer '" + this.ENT_Name + "']");

            // create the layer and add it temporarily to the top level in the manager
            // if it is a sub-Layer of another one, it will be removed again in the AddEntity Method
            this.dxf_parsed = this.Decoder.EManager.ReconstructLayer(this.dxf_Eninty_ID, this.dxf_Enitiy_Name, this.dxf_Entity_Color, 
                                                                     this.dxf_Visiblity, this.dxf_IsValid, this.dxf_AssociatedWComp,
                                                                     (float)this.dxf_LineThicknessGUI, this.dxf_Text, this.dxf_IsTopClosure, this.dxf_IsBottomClosure);
            // add the sub-Layers
            foreach(Layer lay in this.dxf_ContainedLayers)
            {
                this.dxf_parsed.AddEntity(lay);
            }
            // add the geometric content
            foreach(GeometricEntity ge in this.dxf_ContainedGeometry)
            {
                ge.EntityLayer = this.dxf_parsed; // (the Setter adds 'ge' to the ContainedEntites of the Layer)
            }
            
        }

        internal void AddDeferred()
        {
            // add the deferred entities
            int deferred_counter = 0;
            foreach (DXFEntity sE in this.for_deferred_adding)
            {
                DXFZonedVolume vol = sE as DXFZonedVolume;
                if (vol != null && vol.dxf_parsed != null && this.dxf_nr_ContainedEntities > (this.dxf_ContainedLayers.Count + this.dxf_ContainedGeometry.Count + deferred_counter))
                {
                    deferred_counter++;
                    vol.dxf_parsed.EntityLayer = this.dxf_parsed;
                }
            }
        }

        #endregion

        #region OVERRIDES: ToString

        public override string ToString()
        {
            string dxfS = "DXF LAYER";
            dxfS += this.ContentToString();
            return dxfS;
        }

        #endregion
    }

    #endregion

    // --------------------------------------------- DXFZonedPolygon ------------------------------------------ //

    #region DXF_Zoned_Polygon

    public class DXFZonedPolygon : DXFGVEntity
    {
        #region CLASS MEMBERS

        public string dxf_LayerName { get; protected set; } // for verfication
        public bool dxf_ColorByLayer { get; protected set; }
        public double dxf_Height { get; protected set; }

        public List<Point3D> dxf_PolygonCoords { get; protected set; }
        protected int dxf_nr_PolygonCoords;
        protected int dxf_nr_PolygonCoords_read;
        protected Point3D dxf_PC;

        public List<int> dxf_Zone_Inds { get; protected set; }
        protected int dxf_nr_Zone_Inds;
        protected int dxf_nr_Zone_Inds_read;

        protected int dxf_nr_Openings;
        public List<DXFZoneOpening> dxf_ZO_Representations { get; protected set; }

        public ZonedPolygon dxf_parsed { get; protected set; }

        #endregion

        public DXFZonedPolygon()
        {
            this.deferred_execute_OnLoad = false;

            this.dxf_LayerName = string.Empty;
            this.dxf_ColorByLayer = true;
            this.dxf_Height = 1.0;

            this.dxf_PolygonCoords = new List<Point3D>();
            this.dxf_nr_PolygonCoords = 0;
            this.dxf_nr_PolygonCoords_read = 0;
            this.dxf_PC = new Point3D(0, 0, 0);

            this.dxf_Zone_Inds = new List<int>();
            this.dxf_nr_Zone_Inds = 0;
            this.dxf_nr_Zone_Inds_read = 0;

            this.dxf_nr_Openings = 0;
            this.dxf_ZO_Representations = new List<DXFZoneOpening>();
        }

        #region OVERRIDES: Processing

        public override void ReadPoperty()
        {
            this.Decoder.PositionToOutputWindow("ReadProperty ZPoly '" + this.ENT_Name + "'");
            switch (this.Decoder.FCode)
            {
                case (int)ZonedPolygonSaveCode.LAYER_NAME:
                    this.dxf_LayerName = this.Decoder.FValue;
                    break;
                case (int)EntitySpecificSaveCode.COLOR_BY_LAYER:
                    this.dxf_ColorByLayer = (this.Decoder.IntValue() == 1);
                    break;
                case (int)ZonedPolygonSaveCode.VERTICES:
                    this.dxf_nr_PolygonCoords = this.Decoder.IntValue();
                    break;
                case (int)ZonedPolygonSaveCode.ZONE_INDICES:
                    this.dxf_nr_Zone_Inds = this.Decoder.IntValue();
                    break;
                case (int)ZonedPolygonSaveCode.ZONE_OPENINGS:
                    this.dxf_nr_Openings = this.Decoder.IntValue();
                    break;
                case (int)ZonedPolygonSaveCode.HEIGHT:
                    this.dxf_Height = this.Decoder.DoubleValue();
                    break;
                case (int)EntitySaveCode.X_VALUE:
                    if (this.dxf_nr_PolygonCoords_read < this.dxf_nr_PolygonCoords)
                    {
                        this.dxf_PC = new Point3D(this.Decoder.DoubleValue(), 0, 0);
                    }
                    else if (this.dxf_nr_Zone_Inds_read < this.dxf_nr_Zone_Inds)
                    {
                        this.dxf_Zone_Inds.Add(this.Decoder.IntValue());
                        this.dxf_nr_Zone_Inds_read++;
                    }
                    break;
                case (int)EntitySaveCode.Y_VALUE:
                    if (this.dxf_nr_PolygonCoords_read < this.dxf_nr_PolygonCoords)
                    {
                        this.dxf_PC.Y = this.Decoder.DoubleValue();
                    }
                    break;
                case (int)EntitySaveCode.Z_VALUE:
                    if (this.dxf_nr_PolygonCoords_read < this.dxf_nr_PolygonCoords)
                    {
                        this.dxf_PC.Z = this.Decoder.DoubleValue();
                        this.dxf_PolygonCoords.Add(this.dxf_PC);
                        this.dxf_nr_PolygonCoords_read++;
                    }
                    break;
                default:
                    // DXFGVEntity: ENT_Name, Visiblity, EntityColor, IsValid, AssociatedWComp, LineThicknessGUI, Text, IsTopClosure, IsBottomClosure, 
                    // dxf_nr_ContainedEntities
                    // DXFEntity: CLASS_NAME, ENT_ID
                    base.ReadPoperty();
                    this.dxf_Eninty_ID = this.ENT_ID;
                    break;
            }
        }

        #endregion

        #region OVERRIDES: Adding Entities

        public override bool AddEntity(DXFEntity _e)
        {
            this.Decoder.PositionToOutputWindow("AE ZPoly '" + this.ENT_Name + "'");

            // handle according to type
            if (_e == null) return false;
            bool add_successful = false;

            DXFHierarchicalContainer container = _e as DXFHierarchicalContainer;
            if (container != null)
            {
                add_successful = true;
                foreach(DXFEntity sE in container.EC_Entities)
                {
                    DXFZoneOpening zo = sE as DXFZoneOpening;
                    if (zo != null && this.dxf_nr_Openings > this.dxf_ZO_Representations.Count)
                    {
                        this.dxf_ZO_Representations.Add(zo);
                        add_successful &= true;
                    }
                }
            }

            return add_successful;
        }

        #endregion

        #region OVERRIDES: Post-Processing

        public override void OnLoaded()
        {
            base.OnLoaded();
            if (this.Decoder.EManager == null) return;
            this.Decoder.PositionToOutputWindow("[OnLoaded ZPoly '" + this.ENT_Name + "']");

            // call the parsing .ctor w/o Layer (the correct Layer will be assigned in the DXFLayer.AddEntity method)
            // this makes the ZonedPolygon temporarily invalid!
            this.dxf_parsed = this.Decoder.EManager.ReconstructZonedPolygon(this.dxf_Eninty_ID, this.dxf_Enitiy_Name, this.dxf_Entity_Color, this.dxf_Visiblity, 
                                                                this.dxf_IsValid, this.dxf_AssociatedWComp, 
                                                                (float)this.dxf_LineThicknessGUI, this.dxf_Text, this.dxf_IsTopClosure, this.dxf_IsBottomClosure,
                                                                null, this.dxf_ColorByLayer, this.dxf_PolygonCoords, (float)this.dxf_Height, this.dxf_Zone_Inds);
            // add the parsed ZoneOpenings
            foreach(DXFZoneOpening zo in this.dxf_ZO_Representations)
            {
                this.Decoder.EManager.AddReconstructedOpeningToZonedPolygon(this.dxf_parsed, (int)zo.dxf_ID, zo.dxf_Name, zo.dxf_ind_in_poly, 
                                                                            zo.dxf_VPrev, zo.dxf_VNext, zo.dxf_PolyWallT, zo.dxf_PolyWallTAdj, 
                                                                            (float)zo.dxf_dist_from_v_prev, (float)zo.dxf_len_along_segm, (float)zo.dxf_dist_from_poly, (float)zo.dxf_height_along_wallT);
            }
        }

        #endregion

        #region OVERRIDES: ToString

        public override string ToString()
        {
            string dxfS = "DXF ZONED POLY";
            dxfS += this.ContentToString();
            return dxfS;
        }

        #endregion
    }

    #endregion

    // ------------------------------------------ DXFZonedPolygonGroup ---------------------------------------- //

    #region DXF_Zoned_Polygon_Group

    public class DXFZonedPolygonGroup : DXFGVEntity
    {
        #region CLASS MEMBERS

        public List<long> dxf_Referenced_ZonedPolygons { get; protected set; }
        protected int dxf_nr_Referenced_ZonedPolygons;
        protected int dxf_nr_Referenced_ZonedPolygons_read;

        public ZonedPolygonGroup dxf_parsed { get; protected set; }

        #endregion

        public DXFZonedPolygonGroup()
        {
            this.deferred_execute_OnLoad = true;

            this.dxf_Referenced_ZonedPolygons = new List<long>();
            this.dxf_nr_Referenced_ZonedPolygons = 0;
            this.dxf_nr_Referenced_ZonedPolygons_read = 0;
        }

        #region OVERRIDES: Processing

        public override void ReadPoperty()
        {
            this.Decoder.PositionToOutputWindow("ReadProperty ZPGr '" + this.ENT_Name + "']");
            switch (this.Decoder.FCode)
            {
                case (int)ZonedVolumeSaveCode.POLYGON_REFERENCE:
                    this.dxf_nr_Referenced_ZonedPolygons = this.Decoder.IntValue();
                    break;
                case (int)EntitySaveCode.X_VALUE:
                    if (this.dxf_nr_Referenced_ZonedPolygons_read < this.dxf_nr_Referenced_ZonedPolygons)
                    {
                        dxf_Referenced_ZonedPolygons.Add(this.Decoder.LongValue());
                        this.dxf_nr_Referenced_ZonedPolygons_read++;
                    }
                    break;
                default:
                    // DXFGVEntity: ENT_Name, Visiblity, EntityColor, IsValid, AssociatedWComp, LineThicknessGUI, Text, IsTopClosure, IsBottomClosure, 
                    // dxf_nr_ContainedEntities
                    // DXFEntity: CLASS_NAME, ENT_ID
                    base.ReadPoperty();
                    this.dxf_Eninty_ID = this.ENT_ID;
                    break;
            }
        }

        #endregion

        #region OVERRIDES: Post-Processing

        public override void OnLoaded()
        {
            this.Decoder.PositionToOutputWindow("[OnLoaded ZPGr '" + this.ENT_Name + "']");
            if (this.deferred_execute_OnLoad)
            {
                this.Decoder.DeferOnLoadedExecution(this);
                return;
            }
            if (this.Decoder.EManager == null) return;

            this.dxf_parsed = this.Decoder.EManager.ReconstructZonedLevel(this.dxf_Eninty_ID, this.dxf_Enitiy_Name, this.dxf_Entity_Color, this.dxf_Visiblity, 
                                                                            this.dxf_IsValid, this.dxf_AssociatedWComp, 
                                                                            (float)this.dxf_LineThicknessGUI, this.dxf_Text, this.dxf_IsTopClosure, this.dxf_IsBottomClosure,
                                                                             this.dxf_Referenced_ZonedPolygons);
        }

        #endregion

        #region OVERRIDES: ToString

        public override string ToString()
        {
            string dxfS = "DXF ZPGroup";
            dxfS += this.ContentToString();
            return dxfS;
        }

        #endregion
    }

    #endregion

    // --------------------------------------------- DXFZonedVolume ------------------------------------------- //

    #region DXF_Zoned_Volume

    public class DXFZonedVolume : DXFGVEntity
    {
        #region CLASS MEMBERS

        public string dxf_LayerName { get; protected set; } // for verfication
        public bool dxf_ColorByLayer { get; protected set; }


        protected int dxf_nr_Levels;
        protected List<ZonedPolygonGroup> dxf_Levels;

        protected int dxf_nr_materials_per_lEVel;
        protected int dxf_nr_materials_per_lEVel_read;
        protected int dxf_current_lEVel_key;
        protected Dictionary<int, long> dxf_materials_per_lEVel_ids;

        protected int dxf_nr_materials_per_lABel;
        protected int dxf_nr_materials_per_lABel_read;
        protected Utils.Composite4IntKey dxf_current_lABel_key;
        protected Dictionary<Utils.Composite4IntKey, long> dxf_materials_per_lABel_ids;
        public ZonedVolume dxf_parsed { get; protected set; }

        protected List<DXFEntity> for_deferred_adding;

        #endregion

        public DXFZonedVolume()
        {
            this.deferred_execute_OnLoad = true;
            this.for_deferred_adding = new List<DXFEntity>();

            this.dxf_LayerName = string.Empty;
            this.dxf_ColorByLayer = true;

            this.dxf_nr_Levels = 0;
            this.dxf_Levels = new List<ZonedPolygonGroup>();

            this.dxf_nr_materials_per_lEVel = 0;
            this.dxf_nr_materials_per_lEVel_read = 0;
            this.dxf_current_lEVel_key = -1;
            this.dxf_materials_per_lEVel_ids = new Dictionary<int, long>();

            this.dxf_nr_materials_per_lABel = 0;
            this.dxf_nr_materials_per_lABel_read = 0;
            this.dxf_current_lABel_key = Utils.Composite4IntKey.GetInvalidKey();
            this.dxf_materials_per_lABel_ids = new Dictionary<Utils.Composite4IntKey, long>();
        }

        #region OVERRIDES: Processing

        public override void ReadPoperty()
        {
            this.Decoder.PositionToOutputWindow("ReadProperty ZVol '" + this.ENT_Name + "'");
            switch (this.Decoder.FCode)
            {
                case (int)ZonedPolygonSaveCode.LAYER_NAME:
                    this.dxf_LayerName = this.Decoder.FValue;
                    break;
                case (int)EntitySpecificSaveCode.COLOR_BY_LAYER:
                    this.dxf_ColorByLayer = (this.Decoder.IntValue() == 1);
                    break;
                case (int)ZonedVolumeSaveCode.LEVELS:
                    this.dxf_nr_Levels = this.Decoder.IntValue();
                    break;
                case (int)ZonedVolumeSaveCode.MATERIALS_PER_LEVEL:
                    this.dxf_nr_materials_per_lEVel = this.Decoder.IntValue();
                    break;
                case (int)ZonedVolumeSaveCode.MATERIALS_PER_LABEL:
                    this.dxf_nr_materials_per_lABel = this.Decoder.IntValue();
                    break;
                case (int)EntitySaveCode.X_VALUE:
                    if (this.dxf_nr_materials_per_lEVel_read < this.dxf_nr_materials_per_lEVel)
                    {
                        this.dxf_current_lEVel_key = this.Decoder.IntValue();                        
                    }
                    else if (this.dxf_nr_materials_per_lABel_read < this.dxf_nr_materials_per_lABel)
                    {
                        Utils.Composite4IntKey parse_attempt = Utils.Composite4IntKey.ParseKey(this.Decoder.FValue);
                        if (parse_attempt != null)
                            this.dxf_current_lABel_key = parse_attempt;
                    }
                    break;
                case (int)EntitySaveCode.Y_VALUE:
                    if (this.dxf_nr_materials_per_lEVel_read < this.dxf_nr_materials_per_lEVel)
                    {
                        this.dxf_materials_per_lEVel_ids.Add(this.dxf_current_lEVel_key, this.Decoder.LongValue());
                        this.dxf_current_lEVel_key = -1;
                        this.dxf_nr_materials_per_lEVel_read++;
                    }
                    else if (this.dxf_nr_materials_per_lABel_read < this.dxf_nr_materials_per_lABel)
                    {

                        if (this.dxf_current_lABel_key != Utils.Composite4IntKey.GetInvalidKey())
                            this.dxf_materials_per_lABel_ids.Add(this.dxf_current_lABel_key, this.Decoder.LongValue());
                        this.dxf_current_lABel_key = Utils.Composite4IntKey.GetInvalidKey();
                        this.dxf_nr_materials_per_lABel_read++;
                    }
                    break;
                default:
                    // DXFGVEntity: ENT_Name, Visiblity, EntityColor, IsValid, AssociatedWComp, LineThicknessGUI, Text, IsTopClosure, IsBottomClosure, 
                    // dxf_nr_ContainedEntities
                    // DXFEntity: CLASS_NAME, ENT_ID
                    base.ReadPoperty();
                    this.dxf_Eninty_ID = this.ENT_ID;
                    break;
            }
        }

        #endregion

        #region OVERRIDES: Adding Entities

        public override bool AddEntity(DXFEntity _e)
        {
            this.Decoder.PositionToOutputWindow("AE ZVol '" + this.ENT_Name + "'");

            // handle according to type
            if (_e == null) return false;
            bool add_successful = false;

            DXFHierarchicalContainer container = _e as DXFHierarchicalContainer;
            if (container != null)
            {
                add_successful = true;
                foreach (DXFEntity sE in container.EC_Entities)
                {
                    if (sE.deferred_execute_OnLoad)
                    {
                        this.for_deferred_adding.Add(sE);
                        continue;
                    }
                    DXFZonedPolygonGroup zpg = sE as DXFZonedPolygonGroup;
                    if (zpg != null && this.dxf_nr_Levels > this.dxf_Levels.Count)
                    {
                        this.dxf_Levels.Add(zpg.dxf_parsed);
                        add_successful &= true;
                    }
                }
            }

            return add_successful;
        }

        #endregion

        #region OVERRIDES: Post-Processing

        public override void OnLoaded()
        {
            this.Decoder.PositionToOutputWindow("[OnLoaded ZVol '" + this.ENT_Name + "'");
            if (this.deferred_execute_OnLoad)
            {
                this.Decoder.DeferOnLoadedExecution(this);
                return;
            }
            if (this.Decoder.EManager == null) return;

            // add the deferred entities
            foreach(DXFEntity sE in this.for_deferred_adding)
            {
                DXFZonedPolygonGroup zpg = sE as DXFZonedPolygonGroup;
                if (zpg != null && zpg.dxf_parsed != null && this.dxf_nr_Levels > this.dxf_Levels.Count)
                {
                    this.dxf_Levels.Add(zpg.dxf_parsed);
                }
            }

            // find the corresponding materials
            // in order for the Materials to be found they have to be first in the file
            if (this.Decoder.MLManager == null) return;

            Dictionary<int, Material> mat_per_lEVel = new Dictionary<int, Material>();
            foreach(var entry in this.dxf_materials_per_lEVel_ids)
            {
                Material corresp_mat = this.Decoder.MLManager.FindByID(entry.Value);
                if (corresp_mat != null)
                    mat_per_lEVel.Add(entry.Key, corresp_mat);
            }

            Dictionary<Utils.Composite4IntKey, Material> mat_per_lABel = new Dictionary<Utils.Composite4IntKey, Material>();
            foreach(var entry in this.dxf_materials_per_lABel_ids)
            {
                Material corresp_mat = this.Decoder.MLManager.FindByID(entry.Value);
                if (corresp_mat != null)
                    mat_per_lABel.Add(entry.Key, corresp_mat);
            }

            // call the parsing .ctor w/o Layer (the correct Layer will be assigned in the DXFLayer.AddEntity method)
            // this makes the ZonedVolume temporarily invalid!
            this.dxf_parsed = this.Decoder.EManager.ReconstructZonedVolume(this.dxf_Eninty_ID, this.dxf_Enitiy_Name, this.dxf_Entity_Color, this.dxf_Visiblity,
                                                                this.dxf_IsValid, this.dxf_AssociatedWComp,
                                                                (float)this.dxf_LineThicknessGUI, this.dxf_Text, this.dxf_IsTopClosure, this.dxf_IsBottomClosure,
                                                                null, this.dxf_ColorByLayer, this.dxf_Levels, mat_per_lEVel, mat_per_lABel);

        }

        #endregion

        #region OVERRIDES: ToString

        public override string ToString()
        {
            string dxfS = "DXF ZONED VOLUME";
            dxfS += this.ContentToString();
            return dxfS;
        }

        #endregion
    }

    #endregion


}
