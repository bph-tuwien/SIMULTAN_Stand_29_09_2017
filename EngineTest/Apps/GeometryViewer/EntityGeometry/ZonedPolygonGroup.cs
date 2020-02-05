using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Point3D = System.Windows.Media.Media3D.Point3D;
using System.Windows.Data;
using System.Globalization;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;

using GeometryViewer.Utils;
using GeometryViewer.EntityDXF;

namespace GeometryViewer.EntityGeometry
{
    public class ZonedPolygonGroup : Entity
    {

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ======================================== PROPERTIES AND CLASS MEMBERS ================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region PROPERTIES: Geometrical

        private MeshGeometry3D fill;
        public MeshGeometry3D Fill
        {
            get { return this.fill; }
            private set 
            { 
                this.fill = value;
                RegisterPropertyChanged("Fill");
            }
        }

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

        private bool openingsDirty;
        public bool OpeningsDirty
        {
            get { return this.openingsDirty; }
            private set
            {
                this.openingsDirty = value;
                RegisterPropertyChanged("OpeningsDirty");
            }
        }

        private float area;
        public float Area
        {
            get { return this.area; }
            private set 
            { 
                this.area = value;
                RegisterPropertyChanged("Area");
            }
        }

        private float height;
        public float Height
        {
            get { return this.height; }
            private set
            {
                this.height = value;
                RegisterPropertyChanged("Height");
            }
        }

        #endregion

        #region CLASS MEMBERS

        private int outer_polygon_index;
        public int OuterPolygonIndex { get { return this.outer_polygon_index; } }

        private List<Point3D> polygon;
        private List<List<Point3D>> holes;
        private double avg_height;
        public double Avg_Height { get { return this.avg_height; } }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ================================================ INITIALIZERS ========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region .CTOR

        public ZonedPolygonGroup(string _name, List<ZonedPolygon> _zps) : base(_name)
        {
            this.ContainedEntities = new List<Entity>();
            foreach(ZonedPolygon zp in _zps)
            {
                zp.PropertyChanged += polygon_PropertyChanged;
                this.ContainedEntities.Add(zp);
            }
            ContainedEnditiesToGeometryDef();
            CalculateAvgHeight();
            this.IsDirty = true;
            this.OpeningsDirty = true;
        }

        public override string GetDafaultName()
        {
            return "Zoned Polygon Group";
        }

        #endregion

        #region PARSING .CTOR

        internal ZonedPolygonGroup(long _id, string _name, SharpDX.Color _color, EntityVisibility _vis, bool _is_valid, bool _assoc_w_comp,
                                    float _line_thickness_GUI, List<string> _mLtext, bool _is_top_closure, bool _is_bottom_closure,
                                    List<ZonedPolygon> _zps)
            :base(_id, _name, _color, _vis, _is_valid, _assoc_w_comp, _line_thickness_GUI,_mLtext, _is_top_closure, _is_bottom_closure)
        {
            this.ContainedEntities = new List<Entity>();
            foreach (ZonedPolygon zp in _zps)
            {
                zp.PropertyChanged += polygon_PropertyChanged;
                this.ContainedEntities.Add(zp);
            }
            ContainedEnditiesToGeometryDef();
            CalculateAvgHeight();
            this.IsDirty = true;
            this.OpeningsDirty = true;
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // =============================================== CLASS METHODS ========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region EVENT HANDLER: Polygons

        void polygon_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ZonedPolygon zp = sender as ZonedPolygon;
            if (e == null || zp == null)
                return;

            if (e.PropertyName == "EditModeType")
            {
                if (zp.EditModeType == ZonePolygonEditModeType.NONE)
                {
                    // polygon returning from editing
                    if (zp.GeometryChanged)
                    {
                        // reset geometry
                        this.Fill = null;
                        this.IsDirty = true;
                    }

                    this.OpeningsDirty = zp.OpeningsChanged;
                }
                else if (zp.EditModeType == ZonePolygonEditModeType.OPENING_ADD ||
                    zp.EditModeType == ZonePolygonEditModeType.OPENING_REMOVE)
                {
                    this.OpeningsDirty = false;
                }
                else if (zp.EditModeType == ZonePolygonEditModeType.ISBEING_DELETED)
                {
                    // remove polygon from the definition
                    this.ContainedEntities.Remove(zp);
                    if (this.ContainedEntities.Count < 1)
                        this.IsValid = false;
                    // reset geometry
                    this.Fill = null;
                    this.IsDirty = true;
                }
                
            }

        }

        #endregion

        #region CLEAN-UP BEFORE DELETING

        public void ReleasePolygons()
        {
            int n = this.ContainedEntities.Count;
            for (int i = 0; i < n; i++ )
            {
                ZonedPolygon zp = this.ContainedEntities[i] as ZonedPolygon;
                if (zp == null)
                    continue;

                zp.ResetAllOpeningPolygonNormals();
                zp.PropertyChanged -= polygon_PropertyChanged;
            }
            this.ContainedEntities = null;
        }

        #endregion

        #region GEOMETRY: Extract Zoned Polygons and Average Height

        public List<ZonedPolygon> ExtractZonedPolygons()
        {
            List<ZonedPolygon> polygons = new List<ZonedPolygon>();
            int n = this.ContainedEntities.Count;
            for (int i = 0; i < n; i++ )
            {
                ZonedPolygon zp = this.ContainedEntities[i] as ZonedPolygon;
                if (zp == null)
                    continue;

                polygons.Add(zp);
            }
            
            return polygons;
        }

        public void CalculateAvgHeight()
        {
            this.avg_height = 0.0;
            if (this.ContainedEntities == null)
                return;

            int n = this.ContainedEntities.Count;
            foreach (Entity e in this.ContainedEntities)
            {
                ZonedPolygon zp = e as ZonedPolygon;
                if (zp == null)
                    continue;

                this.avg_height += zp.GetPivot().Y;
            }
            this.avg_height /= n;

        }

        #endregion

        #region GEOMETRY: Determine outer-most polygon and holes

        private void ContainedEnditiesToGeometryDef()
        {
            // reset
            this.outer_polygon_index = -1;
            this.polygon = null;
            this.holes = null;

            // convert the contained entities into a list of coordinates
            float level_height = 0f;
            List<List<Point3D>> coordinates = new List<List<Point3D>>();
            foreach(Entity e in this.ContainedEntities)
            {
                ZonedPolygon zp = e as ZonedPolygon;
                if (zp != null && zp.Polygon_Coords.Count > 0)
                {
                    level_height = Math.Max(level_height, zp.Height);
                    coordinates.Add(zp.Polygon_Coords);
                }
            }

            this.Height = level_height;
            MeshesCustom.ToPolygonWithHoles(coordinates, Orientation.XZ, 
                                    out this.outer_polygon_index, out this.polygon, out this.holes);
        }

        public void ExtractGeometryDef(out List<Vector3> polygon, out List<List<Vector3>> holes)
        {
            // force update
            if (this.polygon == null || this.holes == null)
                this.ContainedEnditiesToGeometryDef();

            polygon = Utils.CommonExtensions.ConvertPoints3DListToVector3List(this.polygon);
            if (this.holes == null || this.holes.Count == 0)
                holes = new List<List<Vector3>>();
            else
                holes = Utils.CommonExtensions.ConvertPoints3DListListToVector3ListList(this.holes);
        }

        #endregion

        #region GEOMETRY: Build the Triangulation of the resulting floor plan

        public void Update()
        {
            if (this.polygon == null || this.IsDirty)
            {
                this.ContainedEnditiesToGeometryDef();
                this.CalculateArea();
            }
        }

        public void BuildLevelFill(bool _top)
        {
            this.Update();

            // calculate the fill geometry and the resulting area
            if (IsDirty)
            {
                this.Fill = MeshesCustom.PolygonComplexFill(polygon, holes, _top);
                this.IsDirty = false;
            }
        }

        private void CalculateArea()
        {
            this.Area = (float) MeshesCustom.CalculateAreaOfPolygonWHoles(this.polygon, this.holes);
        }

        #endregion

        #region GEOMETRY: for external use

        public void AddDisplayLines(ref LineBuilder _b)
        {
            if (_b == null) return;

            this.Update();

            int nrP = this.polygon.Count;
            for(int i = 0; i < nrP; i++)
            {
                _b.AddLine(this.polygon[i].ToVector3(), this.polygon[(i + 1) % nrP].ToVector3());
            }
            if (this.holes != null)
            {
                foreach (List<Point3D> hole in this.holes)
                {
                    int nrPH = hole.Count;
                    for (int j = 0; j < nrPH; j++)
                    {
                        _b.AddLine(hole[j].ToVector3(), hole[(j + 1) % nrPH].ToVector3());
                    }
                }
            }

        }

        public MeshGeometry3D GetDisplayMesh()
        {
            this.Update();
            return MeshesCustom.PolygonComplexFill(this.polygon, this.holes, false);
        }

        public double GetAreaWoHoles()
        {
            return MeshesCustom.CalculateAreaOfPolygonWHoles(this.polygon, this.holes);
        }

        public void GetSize(out double height, out double width)
        {
            Utils.CommonExtensions.GetObjAlignedSizeOf(Utils.CommonExtensions.ConvertPoints3DListToVector3List(this.polygon), out width, out height);
        }

        public Vector3 GetUnitMajorOrientation()
        {
            this.Update();

            // determine the longest side of the outer polygon
            return MeshesCustom.GetUnitMajorOrientation(this.polygon);
        }

        #endregion

        #region DXF Export

        public override void AddToExport(ref StringBuilder _sb, bool _with_contained_entites)
        {
            if (_sb == null) return;

            _sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());      // 0
            _sb.AppendLine(DXFUtils.GV_ZONEDLEVEL);                             // GV_ZONED_POLYGON_GROUP

            _sb.AppendLine(((int)EntitySaveCode.CLASS_NAME).ToString());        // 100 (subclass marker)
            _sb.AppendLine(this.GetType().ToString());                          // GeometryViewer.EntityGeometry.ZonedPolygonGroup

            if (this.ContainedEntities != null)
            {
                _sb.AppendLine(((int)ZonedVolumeSaveCode.POLYGON_REFERENCE).ToString());    // 1202
                _sb.AppendLine(this.ContainedEntities.Count.ToString());

                List<ZonedPolygon> polygons = this.ExtractZonedPolygons();
                foreach (ZonedPolygon zp in polygons)
                {
                    _sb.AppendLine(((int)EntitySaveCode.X_VALUE).ToString());               // 910
                    _sb.AppendLine(zp.ID.ToString());
                }
            }

            // also signifies end of complex entity (ends w SEQEND)
            base.AddToExport(ref _sb, _with_contained_entites);

            // the ZonedPolygons commprising the Group are in ContainedEntities
            // they were added to the export in the method call in their respective layers
            // here we only need to export each REFERENCE (ID) so the polygon can be found and re-attached at parsing time 
        }

        #endregion

    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // ================================= CUSTOM COMPARERS FOR ZONED POLYGON GROUPS ============================ //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region HEIGHT COMPARER
    public class ZonedPolygonGroupHeightComparer : IComparer<ZonedPolygonGroup>
    {
        private double tolerance;
        public ZonedPolygonGroupHeightComparer(double _tolerance = CommonExtensions.GENERAL_CALC_TOLERANCE)
        {
            this.tolerance = _tolerance;
        }

        public int Compare(ZonedPolygonGroup _zpg1, ZonedPolygonGroup _zpg2)
        {
            double h1 = _zpg1.Avg_Height;
            double h2 = _zpg2.Avg_Height;
            bool sameHeight = Math.Abs(h1 - h2) <= this.tolerance;

            if (sameHeight)
                return 0;
            else if (h1 > h2)
                return 1;
            else
                return -1;
        }
    }
    #endregion

}
