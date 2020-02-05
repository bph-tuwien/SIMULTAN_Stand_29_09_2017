using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Data;
using System.Globalization;
using System.Collections.ObjectModel;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;

using GeometryViewer.Utils;
using GeometryViewer.EntityDXF;

namespace GeometryViewer.EntityGeometry
{
    public enum ZonePolygonEditModeType { NONE, VERTEX_EDIT, VERTEX_ADD, VERTEX_REMOVE, 
                                          POLY_REVERSE, POLY_LABELS_EDIT, POLY_LABELS_DEFAULT,
                                          OPENING_EDIT, OPENING_ADD, OPENING_REMOVE,
                                          ISBEING_DELETED}

    public class ZonedPolygon : GeometricEntity
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ========================================== PRIVATE NESTED CLASSES ====================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region DEFINITION OF Opening
        private sealed class ZoneOpening : IEquatable<ZoneOpening>
        {
            public static int NR_ZONE_OPENINGS = 0;

            // ----------------------------------------- CLASS MEMBERS AND PROPERTIES --------------------------------- //

            #region PROPERTIES & CLASS MEMBERS
            // idenitfication
            private readonly int id;
            public int ID { get { return this.id; } }

            // info about the parent polygon
            private int indInPolygon;
            public int IndInPolygon
            {
                get { return this.indInPolygon; }
                set
                {
                    this.indInPolygon = value;
                    if (this.vis != null)
                        this.vis.IndInOwner = value;
                }
            }

            public Vector3 vertPrev;
            public Vector3 vertNext;
            public Vector3 polygonNormal; // unit vector
            
            private string name;
            public string Name
            {
                get { return this.name; }
                set
                {
                    this.name = value;
                    if (this.vis != null)
                        this.vis.Label = value;
                }
            }

            // values derived from the info above
            private Vector3 vectOfPolygonSegment; // unit vector
            private float lengthOfPolygonSegment;

            // position and size of the opening relative to parent polygon
            private float distFromPrevVertex;
            public float DistFromPrevVertex
            {
                get { return this.distFromPrevVertex; }
                set
                {
                    this.distFromPrevVertex = value;
                    if (this.vis != null)
                        this.vis.DistFromVertex = value;
                }
            }

            private float lengthAlongPolygonSegment;
            public float LengthAlongPolygonSegment
            {
                get { return this.lengthAlongPolygonSegment; }
                set
                {
                    this.lengthAlongPolygonSegment = value;
                    if (this.vis != null)
                        this.vis.Length = value;
                }
            }

            private float distFromPolygonAlongNormal;
            public float DistFromPolygonAlongNormal
            {
                get { return this.distFromPolygonAlongNormal; }
                set
                {
                    this.distFromPolygonAlongNormal = value;
                    if (this.vis != null)
                        this.vis.DistFromFloorPolygon = value;
                }
            }

            private float heightAlongNormal;
            public float HeightAlongNormal
            {
                get { return this.heightAlongNormal; }
                set
                {
                    this.heightAlongNormal = value;
                    if (this.vis != null)
                        this.vis.Height = value;
                }
            }

            // additional info for 3D, depends on the next polygon of the parent zone
            public Vector3 adjustedPolygonNormal; // unit vector

            // object for visualization
            private ZoneOpeningVis vis;
            public ZoneOpeningVis Vis
            {
                get { return this.vis; }
            }
            #endregion

            // ------------------------------------------------- CONSTRUCTOR ------------------------------------------ //

            #region .CTOR
            public ZoneOpening(ZonedPolygon _polygon, int _indInPolygon, 
                               Vector3 _vertPrev, Vector3 _vertNext, Vector3 _polygonNormal,
                               float _distFromPrevVertex, float _lengthAlongPolygonSegment,
                               float _distFromPolygonAlongNormal, float _heightAlongNormal)
            {
                this.id = ++NR_ZONE_OPENINGS;
                this.name = string.Empty;

                this.indInPolygon = _indInPolygon;
                this.vertPrev = _vertPrev;
                this.vertNext = _vertNext;
                this.polygonNormal = _polygonNormal;                

                this.vectOfPolygonSegment = _vertNext - _vertPrev;
                this.lengthOfPolygonSegment = this.vectOfPolygonSegment.Length();
                this.vectOfPolygonSegment.Normalize();

                this.distFromPrevVertex = _distFromPrevVertex;
                this.lengthAlongPolygonSegment = _lengthAlongPolygonSegment;
                this.distFromPolygonAlongNormal = _distFromPolygonAlongNormal;
                this.heightAlongNormal = _heightAlongNormal;

                // visualization
                this.vis = new ZoneOpeningVis(_polygon, _indInPolygon, this.id, null,
                                              _distFromPrevVertex, lengthAlongPolygonSegment,
                                              _distFromPolygonAlongNormal, _heightAlongNormal);

            }
            #endregion

            #region COPY .CTOR

            public ZoneOpening(ZonedPolygon _polygon, ZoneOpening _original)
            {
                this.id = ++NR_ZONE_OPENINGS;
                this.name = string.Empty;

                this.indInPolygon = _original.IndInPolygon;
                this.vertPrev = _original.vertPrev;
                this.vertNext = _original.vertNext;
                this.polygonNormal = _original.polygonNormal;
                this.adjustedPolygonNormal = _original.adjustedPolygonNormal;

                this.vectOfPolygonSegment = _original.vectOfPolygonSegment;
                this.lengthOfPolygonSegment = _original.lengthOfPolygonSegment;
                this.vectOfPolygonSegment.Normalize();

                this.distFromPrevVertex = _original.distFromPrevVertex;
                this.lengthAlongPolygonSegment = _original.lengthAlongPolygonSegment;
                this.distFromPolygonAlongNormal = _original.distFromPolygonAlongNormal;
                this.heightAlongNormal = _original.heightAlongNormal;

                // visualization
                this.vis = new ZoneOpeningVis(_polygon, this.indInPolygon, this.id, null,
                                              this.distFromPrevVertex, this.lengthAlongPolygonSegment,
                                              this.distFromPolygonAlongNormal, this.heightAlongNormal);
            }

            public static List<ZoneOpening> Copy(ZonedPolygon _owner, List<ZoneOpening> _orignals)
            {
                if (_orignals == null) return null;
                if (_orignals.Count == 0) return new List<ZoneOpening>();

                List<ZoneOpening> copies = new List<ZoneOpening>();
                foreach(ZoneOpening zo in _orignals)
                {
                    copies.Add(new ZoneOpening(_owner, zo));
                }

                return copies;
            }

            #endregion

            #region PARSE .CTOR

            public ZoneOpening(ZonedPolygon _owner, int _id, string _name, int _ind_in_poly, Vector3D _v_prev, Vector3D _v_next, Vector3D _poly_wallT, Vector3D _poly_wallT_adj, 
                                float _dist_from_v_prev, float _len_along_segm, float _dist_from_poly, float _height_along_wallT)
            {
                this.id = _id;
                this.name = _name;

                this.IndInPolygon = _ind_in_poly;
                this.vertPrev = new Vector3((float)_v_prev.X, (float)_v_prev.Y, (float)_v_prev.Z);
                this.vertNext = new Vector3((float)_v_next.X, (float)_v_next.Y, (float)_v_next.Z);
                this.polygonNormal = new Vector3((float)_poly_wallT.X, (float)_poly_wallT.Y, (float)_poly_wallT.Z);
                this.adjustedPolygonNormal = new Vector3((float)_poly_wallT_adj.X, (float)_poly_wallT_adj.Y, (float)_poly_wallT_adj.Z);

                this.vectOfPolygonSegment = this.vertNext - this.vertPrev;
                this.lengthOfPolygonSegment = this.vectOfPolygonSegment.Length();
                this.vectOfPolygonSegment.Normalize();

                this.distFromPrevVertex = _dist_from_v_prev;
                this.lengthAlongPolygonSegment = _len_along_segm;
                this.distFromPolygonAlongNormal = _dist_from_poly;
                this.heightAlongNormal = _height_along_wallT;

                // visualization
                this.vis = new ZoneOpeningVis(_owner, _ind_in_poly, this.id, null,
                                              _dist_from_v_prev, _len_along_segm,
                                              _dist_from_poly, _height_along_wallT);
            }

            #endregion

            // ----------------------------------------- ADAPTING TO A CHANGED VERTEX --------------------------------- //

            #region UPDATE
            public float GetDistFromNextVertex()
            {
                return (this.lengthOfPolygonSegment - this.distFromPrevVertex - this.lengthAlongPolygonSegment);
            }
            public void AdaptZoneOpening(int _indInPolygon, Vector3 _vertPrev, Vector3 _vertNext, float _distFromPrevVertex)
            {
                this.IndInPolygon = _indInPolygon;
                this.vertPrev = _vertPrev;
                this.vertNext = _vertNext;

                this.vectOfPolygonSegment = _vertNext - _vertPrev;
                this.lengthOfPolygonSegment = this.vectOfPolygonSegment.Length();
                this.vectOfPolygonSegment.Normalize();

                this.DistFromPrevVertex = _distFromPrevVertex;
            }
            #endregion

            // ------------------------------------ CALCULATING THE GEOMETRY OF THE OPENING --------------------------- //

            #region GEOMETRY CALCULATION
            public Vector3 GetBottomFirstPointOnPolygon()
            {
                return (this.vertPrev + this.distFromPrevVertex * this.vectOfPolygonSegment);
            }

            public Vector3 GetBottomSecondPointOnPolygon()
            {
                return (this.vertPrev + (this.distFromPrevVertex + this.lengthAlongPolygonSegment)* this.vectOfPolygonSegment);
            }

            private Vector3 MoveAlongAvailableNormal(Vector3 _point, float _dist)
            {
                Vector3 point = _point;
                if (this.adjustedPolygonNormal == Vector3.Zero)
                {
                    point += _dist * this.polygonNormal;
                }
                else
                {
                    float adjDist = _dist;
                    float adjFact = Vector3.Dot(this.polygonNormal, this.adjustedPolygonNormal);
                    if (Math.Abs(adjFact) > CommonExtensions.GENERAL_CALC_TOLERANCE)
                        adjDist /= adjFact;

                    point += adjDist * this.adjustedPolygonNormal;
                }
                return point;
            }

            public Vector3 GetBottomFirstPoint3D()
            {
                Vector3 point = this.GetBottomFirstPointOnPolygon();
                Vector3 point3D = this.MoveAlongAvailableNormal(point, this.distFromPolygonAlongNormal);
                return point3D;
            }

            public Vector3 GetBottomSecondPoint3D()
            {
                Vector3 point = this.GetBottomSecondPointOnPolygon();
                Vector3 point3D = this.MoveAlongAvailableNormal(point, this.distFromPolygonAlongNormal);
                return point3D;
            }

            public Vector3 GetTopFirstPoint3D()
            {
                Vector3 point = this.GetBottomFirstPointOnPolygon();
                Vector3 point3D = this.MoveAlongAvailableNormal(point, this.distFromPolygonAlongNormal + this.heightAlongNormal);
                return point3D;
            }

            public Vector3 GetTopSecondPoint3D()
            {
                Vector3 point = this.GetBottomSecondPointOnPolygon();
                Vector3 point3D = this.MoveAlongAvailableNormal(point, this.distFromPolygonAlongNormal + this.heightAlongNormal);
                return point3D;
            }

            #endregion

            #region GEOMETRY COMPARISON

            public Tuple<int, Vector3, Vector3, float, float, float, float> CreateRecord()
            {
                return Tuple.Create(this.IndInPolygon, this.vertPrev, this.vertNext, this.DistFromPrevVertex, this.LengthAlongPolygonSegment, this.DistFromPolygonAlongNormal, this.HeightAlongNormal);
            }

            public static bool HaveSameGeometry(ZoneOpening _zo1, ZoneOpening _zo2)
            {
                if (_zo1.IndInPolygon != _zo2.IndInPolygon) return false;

                Vector3Comparer vCmp = new Vector3Comparer();
                if (!vCmp.Equals(_zo1.vertPrev, _zo2.vertPrev)) return false;
                if (!vCmp.Equals(_zo1.vertNext, _zo2.vertNext)) return false;

                float diff1 = Math.Abs(_zo1.DistFromPrevVertex - _zo2.DistFromPrevVertex);
                float diff2 = Math.Abs(_zo1.LengthAlongPolygonSegment - _zo2.LengthAlongPolygonSegment);
                float diff3 = Math.Abs(_zo1.DistFromPolygonAlongNormal - _zo2.DistFromPolygonAlongNormal);
                float diff4 = Math.Abs(_zo1.HeightAlongNormal - _zo2.HeightAlongNormal);

                if (diff1 >= CommonExtensions.GENERAL_CALC_TOLERANCE) return false;
                if (diff2 >= CommonExtensions.GENERAL_CALC_TOLERANCE) return false;
                if (diff3 >= CommonExtensions.GENERAL_CALC_TOLERANCE) return false;
                if (diff4 >= CommonExtensions.GENERAL_CALC_TOLERANCE) return false;

                return true;
            }

            public static bool HaveSameGeometry(ZoneOpening _zo1, Tuple<int, Vector3, Vector3, float, float, float, float> _zo2_record)
            {
                if (_zo1.IndInPolygon != _zo2_record.Item1) return false;

                Vector3Comparer vCmp = new Vector3Comparer();
                if (!vCmp.Equals(_zo1.vertPrev, _zo2_record.Item2)) return false;
                if (!vCmp.Equals(_zo1.vertNext, _zo2_record.Item3)) return false;

                float diff1 = Math.Abs(_zo1.DistFromPrevVertex - _zo2_record.Item4);
                float diff2 = Math.Abs(_zo1.LengthAlongPolygonSegment - _zo2_record.Item5);
                float diff3 = Math.Abs(_zo1.DistFromPolygonAlongNormal - _zo2_record.Item6);
                float diff4 = Math.Abs(_zo1.HeightAlongNormal - _zo2_record.Item7);

                if (diff1 >= CommonExtensions.GENERAL_CALC_TOLERANCE) return false;
                if (diff2 >= CommonExtensions.GENERAL_CALC_TOLERANCE) return false;
                if (diff3 >= CommonExtensions.GENERAL_CALC_TOLERANCE) return false;
                if (diff4 >= CommonExtensions.GENERAL_CALC_TOLERANCE) return false;

                return true;
            }

            #endregion

            // --------------------------------------------------- DXF EXPORT ----------------------------------------- //

            #region DXF EXPORT (SIMULTAN)
            public void AddToExport(ref StringBuilder _sb)
            {
                if (_sb == null) return;

                _sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());  // 0
                _sb.AppendLine(DXFUtils.GV_ZONEDPOLY_OPENING);                  // GV_ZONED_POLYGON_OPENING

                _sb.AppendLine(((int)EntitySaveCode.CLASS_NAME).ToString());    // 100 (subclass marker)
                _sb.AppendLine(this.GetType().ToString());                      // GeometryViewer.EntityGeometry.ZonedPolygon.ZoneOpening

                _sb.AppendLine(((int)EntitySaveCode.ENTITY_ID).ToString());     // 900
                _sb.AppendLine(this.ID.ToString());                             // ID

                _sb.AppendLine(((int)ZonedPolygonOpeningSaveCode.ID).ToString());    // 1151
                _sb.AppendLine(this.ID.ToString());                                  // ID

                _sb.AppendLine(((int)ZonedPolygonOpeningSaveCode.NAME).ToString());  // 1152
                _sb.AppendLine(this.Name);                                           // NAME

                _sb.AppendLine(((int)ZonedPolygonOpeningSaveCode.IND_IN_POLYGON).ToString());  // 1153
                _sb.AppendLine(this.IndInPolygon.ToString());                                  // IND_IN_POLYGON

                _sb.AppendLine(((int)ZonedPolygonOpeningSaveCode.VERTEX_PREV).ToString());      // 1154
                _sb.AppendLine("1");                                                            // VERTEX_PREV
                _sb.AppendLine(((int)EntitySaveCode.X_VALUE).ToString());                       // 910
                _sb.AppendLine(DXFUtils.ValueToString(this.vertPrev.X, "F8"));                  // X
                _sb.AppendLine(((int)EntitySaveCode.Y_VALUE).ToString());                       // 920
                _sb.AppendLine(DXFUtils.ValueToString(this.vertPrev.Y, "F8"));                  // Y
                _sb.AppendLine(((int)EntitySaveCode.Z_VALUE).ToString());                       // 930
                _sb.AppendLine(DXFUtils.ValueToString(this.vertPrev.Z, "F8"));                  // Z

                _sb.AppendLine(((int)ZonedPolygonOpeningSaveCode.VERTEX_NEXT).ToString());      // 1155
                _sb.AppendLine("1");                                                            // VERTEX_NEXT
                _sb.AppendLine(((int)EntitySaveCode.X_VALUE).ToString());                       // 910
                _sb.AppendLine(DXFUtils.ValueToString(this.vertNext.X, "F8"));                  // X
                _sb.AppendLine(((int)EntitySaveCode.Y_VALUE).ToString());                       // 920
                _sb.AppendLine(DXFUtils.ValueToString(this.vertNext.Y, "F8"));                  // Y
                _sb.AppendLine(((int)EntitySaveCode.Z_VALUE).ToString());                       // 930
                _sb.AppendLine(DXFUtils.ValueToString(this.vertNext.Z, "F8"));                  // Z

                _sb.AppendLine(((int)ZonedPolygonOpeningSaveCode.POLYGON_WALL_TANGENT).ToString());     // 1156
                _sb.AppendLine("1");                                                                    // POLYGON_WALL_TANGENT
                _sb.AppendLine(((int)EntitySaveCode.X_VALUE).ToString());                               // 910
                _sb.AppendLine(DXFUtils.ValueToString(this.polygonNormal.X, "F8"));                     // X
                _sb.AppendLine(((int)EntitySaveCode.Y_VALUE).ToString());                               // 920
                _sb.AppendLine(DXFUtils.ValueToString(this.polygonNormal.Y, "F8"));                     // Y
                _sb.AppendLine(((int)EntitySaveCode.Z_VALUE).ToString());                               // 930
                _sb.AppendLine(DXFUtils.ValueToString(this.polygonNormal.Z, "F8"));                     // Z

                _sb.AppendLine(((int)ZonedPolygonOpeningSaveCode.ADJUSTED_POLYGON_WALL_TANGENT).ToString());    // 1157
                _sb.AppendLine("1");                                                                            // ADJUSTED_POLYGON_WALL_TANGENT
                _sb.AppendLine(((int)EntitySaveCode.X_VALUE).ToString());                                       // 910
                _sb.AppendLine(DXFUtils.ValueToString(this.adjustedPolygonNormal.X, "F8"));                     // X
                _sb.AppendLine(((int)EntitySaveCode.Y_VALUE).ToString());                                       // 920
                _sb.AppendLine(DXFUtils.ValueToString(this.adjustedPolygonNormal.Y, "F8"));                     // Y
                _sb.AppendLine(((int)EntitySaveCode.Z_VALUE).ToString());                                       // 930
                _sb.AppendLine(DXFUtils.ValueToString(this.adjustedPolygonNormal.Z, "F8"));                     // Z

                _sb.AppendLine(((int)ZonedPolygonOpeningSaveCode.DIST_FROM_VERTEX_PREV).ToString());            // 1158
                _sb.AppendLine(DXFUtils.ValueToString(this.DistFromPrevVertex, "F8"));                          // DIST_FROM_VERTEX_PREV
                _sb.AppendLine(((int)ZonedPolygonOpeningSaveCode.LENGTH_ALONG_POLYGON_SEGMENT).ToString());     // 1159
                _sb.AppendLine(DXFUtils.ValueToString(this.LengthAlongPolygonSegment, "F8"));                   // LENGTH_ALONG_POLYGON_SEGMENT

                _sb.AppendLine(((int)ZonedPolygonOpeningSaveCode.DIST_FROM_POLYGON_ALONG_WALL_TANGENT).ToString()); // 1160
                _sb.AppendLine(DXFUtils.ValueToString(this.DistFromPolygonAlongNormal, "F8"));                      // DIST_FROM_POLYGON_ALONG_WALL_TANGENT
                _sb.AppendLine(((int)ZonedPolygonOpeningSaveCode.HEIGHT_ALONG_WALL_TANGENT).ToString());            // 1161
                _sb.AppendLine(DXFUtils.ValueToString(this.HeightAlongNormal, "F8"));                               // HEIGHT_ALONG_WALL_TANGENT
            }

            #endregion

            #region DXF EXPORT ACAD

            public void AddToACADExport(ref StringBuilder _sb, string _layer_name, Color _color)
            {
                if (_sb == null) return;
                
                // get 3d polyline vertices
                Vector3 v1 = this.GetBottomFirstPoint3D();
                Vector3 v2 = this.GetBottomSecondPoint3D();
                Vector3 v3 = this.GetTopSecondPoint3D();
                Vector3 v4 = this.GetTopFirstPoint3D();

                DXFUtils.Add3dPolylineToExport(ref _sb, _layer_name, _color, new List<Vector3> { v1, v2, v3, v4 }, true);
            }

            #endregion

            // ------------------------------------- IEquatable<ZoneOpening> IMPLEMENTATION --------------------------- //

            #region INTERFACE IMPL
            public bool Equals(ZoneOpening _zo)
            {
                //Check whether the compared objects reference the same data. 
                if (Object.ReferenceEquals(this, _zo)) return true;

                //Check whether the object is null. 
                if (Object.ReferenceEquals(_zo, null)) return false;

                return (this.ID == _zo.ID);
            }

            public override bool Equals(object obj)
            {
                //Check whether the compared objects reference the same data. 
                if (Object.ReferenceEquals(this, obj)) return true;

                //Check whether the object is null. 
                if (Object.ReferenceEquals(obj, null)) return false;

                ZoneOpening zo = obj as ZoneOpening;
                if (zo == null)
                    return false;
                else
                    return (this.ID == zo.ID);
            }

            public override int GetHashCode()
            {
                return this.ID.GetHashCode();
            }

            public static bool operator== (ZoneOpening _zo1, ZoneOpening _zo2)
            {
                if (Object.ReferenceEquals(_zo1, null) || Object.ReferenceEquals(_zo2, null))
                    return Object.Equals(_zo1, _zo2);

                return _zo1.Equals(_zo2);
            }

            public static bool operator!= (ZoneOpening _zo1, ZoneOpening _zo2)
            {
                if (Object.ReferenceEquals(_zo1, null) || Object.ReferenceEquals(_zo2, null))
                    return !Object.Equals(_zo1, _zo2);

                return !(_zo1.Equals(_zo2));
            }
            #endregion
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ======================================== PROPERTIES AND CLASS MEMBERS ================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region PROPERTIES: General Geometry

        private List<ZonedPolygonVertexVis> polygon_coords_vis;
        private List<Point3D> polygon_coords;
        public List<Point3D> Polygon_Coords 
        {
            get { return this.polygon_coords; }
            private set
            {
                if (value != null && value.Count > 0)
                    this.polygon_coords = value;
                else
                    this.polygon_coords = new List<Point3D> { new Point3D(0, 0, 0) };

                // check the winding direction of the polygon
                bool isValid = false;
                this.IsClockWise = this.CalculateIfClockWise(CommonExtensions.GENERAL_CALC_TOLERANCE, out isValid);

                RegisterPropertyChanged("Polygon_Coords");
            }
        }

        private List<Vector3> polygon_line_normals;
        public ReadOnlyCollection<Vector3> Polygon_LineNormals { get { return this.polygon_line_normals.AsReadOnly(); } }

        private bool isClockWise;
        public bool IsClockWise
        {
            get { return this.isClockWise; }
            private set
            { 
                this.isClockWise = value;
                RegisterPropertyChanged("IsClockWise");
            }
        }

        private List<int> zones_inds;
        public List<int> Zones_Inds
        {
            get { return this.zones_inds; }
            private set
            {
                this.zones_inds = value;
                RegisterPropertyChanged("Zones_Inds");
            }
        }

        private List<ZoneOpening> polygon_openings;

        #endregion

        #region PROPERTIES: Editing Feedback
        
        private List<Point3D> polygon_coords_prev;
        private List<int> zoned_inds_prev;
        private List<Tuple<int, Vector3, Vector3, float, float, float, float>> polygon_openings_prev;
        private ZonePolygonEditModeType editModeType;
        public ZonePolygonEditModeType EditModeType
        {
            get { return this.editModeType; }
            set 
            {
                if (value != ZonePolygonEditModeType.NONE)
                {
                    this.polygon_coords_prev = new List<Point3D>(this.Polygon_Coords);
                    this.zoned_inds_prev = new List<int>(this.Zones_Inds);
                    this.polygon_openings_prev = this.polygon_openings.Select(x => x.CreateRecord()).ToList();
                    this.GeometryChanged = false;
                    this.OpeningsChanged = false;
                }
                else
                {
                    this.GeometryChanged = this.IsStructurallyChanged(this.polygon_coords_prev, this.zoned_inds_prev);
                    this.OpeningsChanged = this.AreOpeningsChanged(this.polygon_openings_prev);
                    this.polygon_coords_prev = new List<Point3D>(this.Polygon_Coords);
                    this.zoned_inds_prev = new List<int>(this.Zones_Inds);
                    this.polygon_openings_prev = this.polygon_openings.Select(x => x.CreateRecord()).ToList();
                }
                this.editModeType = value;
                RegisterPropertyChanged("EditModeType");
            }
        }


        public bool GeometryChanged { get; private set; }
        public bool OpeningsChanged { get; private set; }

        #endregion

        #region PROPERTIES: Other

        private float height;
        public float Height
        {
            get { return this.height; }
            set 
            { 
                this.height = value;
                RegisterPropertyChanged("Height");
            }
        }


        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ================================================ INITIALIZERS ========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region .CTOR
        public ZonedPolygon(Layer _layer, List<Point3D> _polygon) : base(_layer)
        {
            this.HasGeometryType = EnityGeometryType.LINE;
            this.polygon_line_normals = new List<Vector3>();
            this.Polygon_Coords = this.CleanUpPolyline(_polygon);
            ClearZones();
            ClearOpenings();
            this.EditModeType = ZonePolygonEditModeType.NONE;
            this.Height = 1f;
            this.polygon_coords_prev = new List<Point3D>(this.Polygon_Coords);
            this.zoned_inds_prev = new List<int>(this.Zones_Inds);
            
            
        }

        public ZonedPolygon(string _name, Layer _layer, List<Point3D> _polygon) : base(_name, _layer)
        {
            this.HasGeometryType = EnityGeometryType.LINE;
            this.polygon_line_normals = new List<Vector3>();
            this.Polygon_Coords = this.CleanUpPolyline(_polygon);
            ClearZones();
            ClearOpenings();
            this.EditModeType = ZonePolygonEditModeType.NONE;
            this.Height = 1f;
            this.polygon_coords_prev = new List<Point3D>(this.Polygon_Coords);
            this.zoned_inds_prev = new List<int>(this.Zones_Inds);
        }

        public ZonedPolygon(ZonedPolygon _original, Vector3 _offset, bool _copyOpenings = false)
            : base("Copy of " + _original.EntityName, _original.EntityLayer)
        {
            // entity (w/o ID, Name, IsValid, IsSelected, LineThicknessGUI)
            this.EntityColor = _original.EntityColor;
            this.Visibility = _original.Visibility;
            this.ContainedEntities = _original.ContainedEntities;
            this.ShowZones = _original.ShowZones;
            this.ShowCtrlPoints = _original.ShowCtrlPoints;
            this.IsExpanded = _original.IsExpanded;

            // geometric entity
            this.HasGeometry = true;
            this.ColorByLayer = _original.ColorByLayer;

            // zoned polygon
            this.HasGeometryType = EnityGeometryType.LINE;          
            List<Point3D> pcoords_tmp = new List<Point3D>();
            foreach(Point3D p in _original.Polygon_Coords)
            {
                Point3D pO = p + new Vector3D(_offset.X, _offset.Y, _offset.Z);
                pcoords_tmp.Add(pO);
            }
            this.polygon_line_normals = new List<Vector3>();
            this.Polygon_Coords = new List<Point3D>(pcoords_tmp);

            this.Zones_Inds = new List<int>(_original.Zones_Inds);
            if (_copyOpenings)
            {
                this.polygon_openings = ZoneOpening.Copy(_original, _original.polygon_openings);
                this.polygon_openings_prev = this.polygon_openings.Select(x => x.CreateRecord()).ToList();
            }                
            else
            {
                this.ClearOpenings();
            }
                
            this.EditModeType = ZonePolygonEditModeType.NONE;
            this.Height = _original.Height;

            this.polygon_coords_prev = new List<Point3D>(this.Polygon_Coords);
            this.zoned_inds_prev = new List<int>(this.Zones_Inds);
        }

        public override string GetDafaultName()
        {
            return "Zoned Polygon";
        }
        #endregion

        #region PARSING .CTOR & METHODS

        internal ZonedPolygon(long _id, string _name, SharpDX.Color _color, EntityVisibility _vis, bool _is_valid, bool _assoc_w_comp, 
                        float _line_thickness_GUI, List<string> _mLtext, bool _is_top_closure, bool _is_bottom_closure,
                        Layer _layer, bool _color_by_layer, List<Point3D> _coords, float _height)
            :base(_id, _name, _color, _vis, _is_valid, _assoc_w_comp, _line_thickness_GUI,_mLtext, _is_top_closure, _is_bottom_closure, _layer, _color_by_layer)
        {
            this.HasGeometryType = EnityGeometryType.LINE;
            this.polygon_line_normals = new List<Vector3>();
            this.Polygon_Coords = new List<Point3D>(_coords);// this.CleanUpPolyline(_coords); // causes degeneration if two points overlap intentionally!
            ClearZones();
            ClearOpenings();
            this.EditModeType = ZonePolygonEditModeType.NONE;
            this.Height = _height;
            this.polygon_coords_prev = new List<Point3D>(this.Polygon_Coords);
            this.zoned_inds_prev = new List<int>(this.Zones_Inds);

            // add the openings later...
            // zones are set by the entity manager
        }

        internal void AddParsedOpening(int _id, string _name, int _ind_in_poly, Vector3D _v_prev, Vector3D _v_next, Vector3D _poly_wallT, Vector3D _poly_wallT_adj, 
                                        float _dist_from_v_prev, float _len_along_segm, float _dist_from_poly, float _height_along_wallT)
        {
            int n = this.Polygon_Coords.Count;
            if (_ind_in_poly < 0 || _ind_in_poly > (n - 1))
                return;

            ZoneOpening zo = new ZoneOpening(this, _id, _name, _ind_in_poly, _v_prev, _v_next, _poly_wallT, _poly_wallT_adj, 
                                                _dist_from_v_prev, _len_along_segm, _dist_from_poly, _height_along_wallT);
            ZoneOpening.NR_ZONE_OPENINGS = Math.Max(ZoneOpening.NR_ZONE_OPENINGS, _id);
            this.polygon_openings.Add(zo);
            this.polygon_openings_prev = this.polygon_openings.Select(x => x.CreateRecord()).ToList();
        }

        internal static void ResetZoneOpeningCounter()
        {
            ZoneOpening.NR_ZONE_OPENINGS = 0;
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // =============================================== CLASS METHODS ========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region ZONE DEFINITION
        public bool DefineZone(int _indexStart, int _label)
        {           
            // check that the indices are within range and form a contiguous segment
            int nrVertices = this.Polygon_Coords.Count;
            if (_indexStart < 0 || _indexStart > nrVertices - 1)
                return false;

            // check if this zone label exists already
            if (this.Zones_Inds.Contains(_label))
                return false;

            this.Zones_Inds[_indexStart] = _label;
            return true;
        }

        public bool ReDefineAllZones(List<int> _labels)
        {
            int n = this.Polygon_Coords.Count;
            if (_labels == null || _labels.Count != n)
                return false;

            for(int i = 0; i < n; i++)
            {
                this.Zones_Inds[i] = _labels[i];
            }

            this.RegisterPropertyChanged("EditModeType");
            return true;
        }

        public void ClearZones()
        {
            this.Zones_Inds = new List<int>();
            int n = this.Polygon_Coords.Count;
            for(int i = 0; i < n; i++)
            {
                this.Zones_Inds.Add(i);
            }
        }

        public String GetLabelsAsString(string _separator = ",")
        {
            int n = this.Polygon_Coords.Count;
            string all_labels = string.Empty;
            for (int i = 0; i < n; i++)
            {
                all_labels += this.Zones_Inds[i].ToString();
                if (i < (n - 1))
                    all_labels += _separator;
            }

            return all_labels;
        }

        #endregion

        #region GEOMETRY: Comparison

        private bool IsStructurallyChanged(List<Point3D> _zp_old_polygon_coords, List<int> _zp_old_zone_inds)
        {
            if (_zp_old_polygon_coords == null || _zp_old_zone_inds == null)
                return true;

            if (_zp_old_polygon_coords.Count != _zp_old_zone_inds.Count)
                return true;

            // compare the coordinates and lables
            Vector3Comparer vCmp = new Vector3Comparer();
            int nP_old = _zp_old_polygon_coords.Count;
            int nP_new = this.Polygon_Coords.Count;

            if (nP_old != nP_new)
                return true;

            for (int i = 0; i < nP_old; i++)
            {
                if (!vCmp.Equals(_zp_old_polygon_coords[i].ToVector3(), this.Polygon_Coords[i].ToVector3()))
                    return true;

                if (_zp_old_zone_inds[i] != this.Zones_Inds[i])
                    return true;
            }

            return false;
        }

        private bool AreOpeningsChanged(List<Tuple<int, Vector3, Vector3, float, float, float, float>> _openings_old)
        {
            if (this.polygon_openings == null && _openings_old == null) return false;
            if (this.polygon_openings == null && _openings_old != null) return true;
            if (this.polygon_openings != null && _openings_old == null) return true;

            int nrOp_new = this.polygon_openings.Count;
            int nrOp_old = _openings_old.Count;
            if (nrOp_new != nrOp_old) return true;

            for(int i = 0; i < nrOp_new; i++)
            {
                bool same_geom = ZoneOpening.HaveSameGeometry(this.polygon_openings[i], _openings_old[i]);
                if (!same_geom)
                    return true;
            }

            return false;
        }

        #endregion

        #region GEOMETRY: Extraction
        public override LineGeometry3D Build(double _startMarkerSize = 0.1)
        {
            LineBuilder b = new LineBuilder();
            int n = this.Polygon_Coords.Count;
            // lines
            for (int i = 0; i < n; i++ )
            {
                b.AddLine(this.Polygon_Coords[i % n].ToVector3(), this.Polygon_Coords[(i + 1) % n].ToVector3());
            }
            // start marker
            b.AddBox(this.Polygon_Coords[0].ToVector3(), _startMarkerSize, 0, _startMarkerSize);
            
            // direction marker
            Vector3 v01 = this.Polygon_Coords[1].ToVector3() - this.Polygon_Coords[0].ToVector3();
            v01.Normalize();
            Vector3 point1 = this.Polygon_Coords[0].ToVector3() + v01 * 2f * (float)_startMarkerSize;
            Vector3 point2 = this.Polygon_Coords[0].ToVector3() + v01 * 3f * (float)_startMarkerSize;
            Matrix Tr = CommonExtensions.CalcAlignmentTransform(point1, point2);

            float arSpan = (float)_startMarkerSize * 6f;
            Vector3[] arrow = new Vector3[] 
            { 
                new Vector3(-arSpan, 0f, arSpan), 
                new Vector3(0f, 0f, 0f), 
                new Vector3(-arSpan, 0f, -arSpan) 
            };
            Vector4[] arrowH = CommonExtensions.ConvertVector3ArToVector4Ar(arrow);
            Vector4.Transform(arrowH, ref Tr, arrowH);
            Vector3[] arrowT = CommonExtensions.ConvertVector4ArToVector3Ar(arrowH);

            b.AddLine(arrowT[0], arrowT[1]);
            b.AddLine(arrowT[2], arrowT[1]);

            return b.ToLineGeometry3D();    
        }

        public LineGeometry3D BuildZoneDescriptors(double _textHeight = 0.25)
        {
            LineBuilder b = new LineBuilder();
            List<Vector3> normals = GetLineNormals();
            // uniform offset
            List<Point3D> offset = OffsetPolygon((float)_textHeight * 2f);
            // non-uniform offset
            List<int> pOffsets = Enumerable.Range(1, this.Polygon_Coords.Count).ToList();
            List<float> pfOffsets = pOffsets.Select<int, float>(i => i * 0.1f).ToList();
            List<bool> pOut = Enumerable.Repeat(true,this.Polygon_Coords.Count).ToList();
            //pOut[1] = false;
            //pOut[2] = false;
            List<Point3D> offsetNU = OffsetPolygon(pfOffsets, pOut);
         
            int n = this.Polygon_Coords.Count;
            for (int i = 0; i < n; i++ )
            {
                //// for testing - show the normals and offsets
                //b.AddLine(this.Polygon_Coords[i].ToVector3(),
                //       this.Polygon_Coords[i].ToVector3() + normals[i] * (float)_textHeight * 2f);

                //b.AddLine(this.Polygon_Coords[i].ToVector3() + normals[i] * (float)_textHeight * 2f,
                //      this.Polygon_Coords[(i + 1) % n].ToVector3() + normals[i] * (float)_textHeight * 2f);

                // for testing - uniform polygon offset
                b.AddLine(offset[i % n].ToVector3(), offset[(i + 1) % n].ToVector3());

                //// for testing - non-uniform polygon offset
                //b.AddLine(offsetNU[i % n].ToVector3(), offsetNU[(i + 1) % n].ToVector3());


                // get TRANSFORMATION MATRIX for the zone description
                Vector3 point1 = this.Polygon_Coords[i].ToVector3() + normals[i] * (float)_textHeight * 0.1f;
                Vector3 point2 = this.Polygon_Coords[i].ToVector3() + normals[i] * (float)_textHeight * 1.1f;
                Matrix Tr = CommonExtensions.CalcAlignmentTransform(point1, point2);

                float windingF = (this.IsClockWise) ? 1 : -1;
                Matrix RoX = Matrix.RotationX(-(float)Math.PI * windingF / 2f);
                if(normals[i] == -Vector3.UnitX)
                    RoX = Matrix.RotationX((float)Math.PI * windingF / 2f);

                Matrix MirX = Matrix.Scaling(new Vector3(-1f, 1f, 1f));

                Matrix TrComplete = (this.IsClockWise) ? RoX * Tr : MirX * RoX * Tr;

                // get CHARACTER for the zone description
                List<List<Vector3>> descr = new List<List<Vector3>>();
                FontAssembler.ConvertTextToPointChains(this.Zones_Inds[i].ToString(), TrComplete, ref descr, 0, false);
                foreach(var chain in descr)
                {
                    int m = chain.Count;
                    for(int j = 0; j < m; j++)
                    {
                        b.AddLine(chain[j], chain[(j + 1) % m]);
                    }
                }
            }

            // the openings
            double oMarkerSize = _textHeight * 0.25;
            int nrO = this.polygon_openings.Count;
            for (int j = 0; j < nrO; j++)
            {
                List<Vector3> coords = this.ExtractOpeningGeometry(this.polygon_openings[j].ID);
                if (coords.Count > 1)
                {
                    b.AddBox(coords[0], oMarkerSize, oMarkerSize, oMarkerSize);
                    b.AddBox(coords[1], oMarkerSize, oMarkerSize, oMarkerSize);

                    // b.AddLine(coords[0], coords[2]);
                    ZonedPolygon.CreateDashedLine(ref b, coords[0], coords[2], (float)oMarkerSize);
                    b.AddLine(coords[2], coords[4]);

                    // b.AddLine(coords[1], coords[3]);
                    ZonedPolygon.CreateDashedLine(ref b, coords[1], coords[3], (float)oMarkerSize);
                    b.AddLine(coords[3], coords[5]);

                    b.AddLine(coords[2], coords[3]);
                    b.AddLine(coords[4], coords[5]);
                }
            }

            return b.ToLineGeometry3D();
        }

        public LineGeometry3D BuildCtrlPoints(double _size = 0.1)
        {
            LineBuilder b = new LineBuilder();
            int n = this.Polygon_Coords.Count;
            for(int i = 0; i < n; i++)
            {
                b.AddBox(this.Polygon_Coords[i].ToVector3(), _size, _size, _size);
            }

            //Vector3 pivot = GetPivot();
            //b.AddBox(pivot, _size, _size, _size);

            return b.ToLineGeometry3D();
        }

        #endregion

        #region VERTEX EDITING

        public List<ZonedPolygonVertexVis> ExtractVerticesForDisplay()
        {
            if (this.polygon_coords_vis == null)
                this.polygon_coords_vis = new List<ZonedPolygonVertexVis>();

            int nrV = this.polygon_coords_vis.Count;
            int nrC = this.Polygon_Coords.Count;
            for (int i = 0; i < nrC; i++)
            {
                if (i < nrV)
                {
                    // use an already existing vertex visualizer
                    this.polygon_coords_vis[i].AdjustToPolygonChange(i, this.Polygon_Coords[i].ToVector3(),
                                                                        this.Polygon_Coords[(i + 1) % nrC].ToVector3(),
                                                                        this.Polygon_Coords[(nrC + i - 1) % nrC].ToVector3());
                    this.polygon_coords_vis[i].Label = "Vertex " + (i + 1).ToString();
                    this.polygon_coords_vis[i].ZoneInOwner = this.Zones_Inds[i];
                }
                else
                {
                    // create a new vertex visualizer
                    ZonedPolygonVertexVis vertex = new ZonedPolygonVertexVis(this, i, null,
                                                                this.Polygon_Coords[i].ToVector3(),
                                                                this.Polygon_Coords[(i + 1) % nrC].ToVector3(),
                                                                this.Polygon_Coords[(nrC + i - 1) % nrC].ToVector3());
                    vertex.Label = "Vertex " + (i + 1).ToString();
                    vertex.ZoneInOwner = this.Zones_Inds[i];
                    this.polygon_coords_vis.Add(vertex);
                }
            }

            return this.polygon_coords_vis;

            // OLD:
            //List<ZonedPolygonVertexVis> vertices = new List<ZonedPolygonVertexVis>();
            //int n = this.Polygon_Coords.Count;
            //for (int i = 0; i < n; i++)
            //{
            //    ZonedPolygonVertexVis vertex = new ZonedPolygonVertexVis(this, i, null, this.Polygon_Coords[i].ToVector3(),
            //                this.Polygon_Coords[(i + 1) % n].ToVector3(), this.Polygon_Coords[(n + i - 1) % n].ToVector3());
            //    vertex.Label = "Vertex " + (i + 1).ToString();
            //    vertex.ZoneInOwner = this.Zones_Inds[i];
            //    vertices.Add(vertex);
            //}
            //return vertices;
        }

        public void Reverse()
        {
            List<Point3D> newCoords = new List<Point3D>();
            int n = this.Polygon_Coords.Count;
            newCoords.Add(this.Polygon_Coords[0]);
            for(int i = n - 1; i > 0; i--)
            {
                newCoords.Add(this.Polygon_Coords[i]);
            }
            this.Polygon_Coords = new List<Point3D>(newCoords);

            // reset the line normals
            this.polygon_line_normals = new List<Vector3>();

            // adapt all openings
            int m = this.polygon_openings.Count;
            for(int j = 0; j < m; j++)
            {
                int newInd = n - this.polygon_openings[j].IndInPolygon - 1;
                float distFromNext = this.polygon_openings[j].GetDistFromNextVertex();
                this.polygon_openings[j].AdaptZoneOpening(newInd, 
                                                          this.Polygon_Coords[newInd].ToVector3(),
                                                          this.Polygon_Coords[(n + newInd + 1) % n].ToVector3(),
                                                          distFromNext);
            }
        }

        public bool AddVertex(Point3D _vCandidate, int _afterIndex)
        {
            int n = this.Polygon_Coords.Count;
            if (_afterIndex < 0 || _afterIndex > n - 1)
                return false;

            this.Polygon_Coords.Insert(_afterIndex + 1, _vCandidate);
            this.Zones_Inds.Insert(_afterIndex + 1, this.Zones_Inds.Max() + 1);

            // reset the line normals
            this.polygon_line_normals = new List<Vector3>();

            // adapt affected openings in the segment
            List<ZoneOpening> zos = this.polygon_openings.FindAll(x => x.IndInPolygon == _afterIndex);
            if (zos != null && zos.Count > 0) 
            {
                foreach(ZoneOpening zo in zos)
                {
                    zo.AdaptZoneOpening(zo.IndInPolygon, zo.vertPrev, _vCandidate.ToVector3(), zo.DistFromPrevVertex);
                }
            }
            // adapt affectedopenings after the segment
            List<ZoneOpening> zos_after = this.polygon_openings.FindAll(x => x.IndInPolygon > _afterIndex);
            if (zos_after != null && zos_after.Count > 0)
            {
                foreach(ZoneOpening zo in zos_after)
                {
                    zo.IndInPolygon += 1;
                }
            }

            return true;
        }

        public bool RemoveVertex(int _atIndex)
        {
            int n = this.Polygon_Coords.Count;
            if (_atIndex < 0 || _atIndex > (n - 1))
                return false;

            this.Polygon_Coords.RemoveAt(_atIndex);
            this.Zones_Inds.RemoveAt(_atIndex);

            // reset the line normals
            this.polygon_line_normals = new List<Vector3>();

            // remove affected openings in this segment or the previous one
            List<ZoneOpening> zos = this.polygon_openings.FindAll(x => (x.IndInPolygon == _atIndex || x.IndInPolygon == (n + _atIndex - 1) % n));
            if (zos != null && zos.Count > 0)
            {
                foreach (ZoneOpening zo in zos)
                {
                    this.polygon_openings.Remove(zo);
                }
            }
            // adapt affectedopenings after the segment
            List<ZoneOpening> zos_after = this.polygon_openings.FindAll(x => x.IndInPolygon > _atIndex);
            if (zos_after != null && zos_after.Count > 0)
            {
                foreach (ZoneOpening zo in zos_after)
                {
                    zo.IndInPolygon -= 1;
                }
            }

            return true;
        }

        public bool ModifyVertex(int _atIndex, Vector3 _newPos)
        {
            int n = this.Polygon_Coords.Count;
            if (_atIndex < 0 || _atIndex > (n - 1))
                return false;

            this.Polygon_Coords[_atIndex] = _newPos.ToPoint3D();

            // reset the line normals
            this.polygon_line_normals = new List<Vector3>();

            // modify affected openings
            List<ZoneOpening> zos = this.polygon_openings.FindAll(x => x.IndInPolygon == _atIndex);
            List<ZoneOpening> zos_prev = this.polygon_openings.FindAll(x => x.IndInPolygon == (n + _atIndex - 1) % n);
            if (zos != null && zos.Count > 0)
            {
                foreach (ZoneOpening zo in zos)
                {
                    zo.AdaptZoneOpening(zo.IndInPolygon, _newPos, zo.vertNext, zo.DistFromPrevVertex);
                }
            }
            if (zos_prev != null && zos_prev.Count > 0)
            {
                foreach (ZoneOpening zo in zos_prev)
                {
                    zo.AdaptZoneOpening(zo.IndInPolygon, zo.vertPrev, _newPos, zo.DistFromPrevVertex);
                }
            }

            return true;
        }

        #endregion

        #region OPENING DEFINITION AND EDITING

        public void ClearOpenings()
        {
            this.polygon_openings = new List<ZoneOpening>();
            this.polygon_openings_prev = new List<Tuple<int, Vector3, Vector3, float, float, float, float>>();
        }

        public int AddOpening(int _indInPolygon, float _distFromPrevVertex, float _lengthAlongPolygonSegment,
                                                     float _distFromPolygonAlongNormal, float _heightAlongNormal)
        {
            int n = this.Polygon_Coords.Count;
            if (_indInPolygon < 0 || _indInPolygon > (n - 1))
                return -1;

            ZoneOpening zo = new ZoneOpening(this, _indInPolygon, 
                                             this.Polygon_Coords[_indInPolygon].ToVector3(), 
                                             this.Polygon_Coords[(_indInPolygon + 1) % n].ToVector3(), 
                                             this.GetPolygonNormalNewell(),
                                             _distFromPrevVertex, _lengthAlongPolygonSegment,
                                             _distFromPolygonAlongNormal, _heightAlongNormal);
            this.polygon_openings.Add(zo);
            return zo.ID;
        }

        public bool RemoveOpening(int _id)
        {
            ZoneOpening zo = this.polygon_openings.Find(x => x.ID == _id);
            if (zo != null)
                return this.polygon_openings.Remove(zo);
            else
                return false;
        }

        public bool ModifyOpeningDimensions(int _id, float _distFromPrevVertex, float _lengthAlongPolygonSegment,
                                           float _distFromPolygonAlongNormal, float _heightAlongNormal, string _name = "")
        {
            ZoneOpening zo = this.polygon_openings.Find(x => x.ID == _id);
            if (zo != null)
            {
                zo.DistFromPrevVertex = _distFromPrevVertex;
                zo.LengthAlongPolygonSegment = _lengthAlongPolygonSegment;
                zo.DistFromPolygonAlongNormal = _distFromPolygonAlongNormal;
                zo.HeightAlongNormal = _heightAlongNormal;
                zo.Name = _name;
                return true;
            }
            else
                return false;
        }

        public bool ModifyOpeningPolygonNormal(int _indInOwner, Vector3 _adjustedPolygonNormal)
        {
            List<ZoneOpening> zos = this.polygon_openings.FindAll(x => x.IndInPolygon == _indInOwner);
            if (zos != null && zos.Count > 0)
            {
                foreach (ZoneOpening zo in zos)
                {
                    zo.adjustedPolygonNormal = _adjustedPolygonNormal;
                }
                return true;
            }
            else
                return false;
        }

        public void ResetAllOpeningPolygonNormals()
        {
            foreach(ZoneOpening zo in this.polygon_openings)
            {
                zo.adjustedPolygonNormal = Vector3.Zero;
            }
        }

        public List<ZoneOpeningVis> ExtractOpeningsForDisplay()
        {
            List<ZoneOpeningVis> list = new List<ZoneOpeningVis>();
            int nro = this.polygon_openings.Count;
            for (int i = 0; i < nro; i++)
            {
                ZoneOpening zo = this.polygon_openings[i];
                if (zo.Name == null || zo.Name == string.Empty)
                    zo.Name = "Opening " + (i + 1).ToString(); 

                list.Add(zo.Vis);
            }
            return list;
        }

        public List<Vector3> ExtractOpeningGeometry(int _id)
        {
            ZoneOpening zo = this.polygon_openings.Find(x => x.ID == _id);

            List<Vector3> points = new List<Vector3>();
            if (zo != null)
            {
                points.Add(zo.GetBottomFirstPointOnPolygon());
                points.Add(zo.GetBottomSecondPointOnPolygon());
                points.Add(zo.GetBottomFirstPoint3D());
                points.Add(zo.GetBottomSecondPoint3D());
                points.Add(zo.GetTopFirstPoint3D());
                points.Add(zo.GetTopSecondPoint3D());
            }

            return points;
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ========================================== CLASS UTILITY METHODS ======================================= //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region UTILITIES: Geometry
        private List<Vector3> GetLineNormals()
        {
            if (this.polygon_line_normals != null && this.polygon_line_normals.Count == this.Polygon_Coords.Count)
                return this.polygon_line_normals;

            Vector3 pivot = GetPivot();
            Vector3 pNormal = GetPolygonNormalNewell();
            List<Vector3> normals = new List<Vector3>();
            int n = this.Polygon_Coords.Count;
            for (int i = 0; i < n; i++ )
            {
                Vector3 v0 = this.Polygon_Coords[(i + 1) % n].ToVector3() - this.Polygon_Coords[i].ToVector3();
                Vector3 v1 = pivot - this.Polygon_Coords[i].ToVector3();

                Vector3 e0 = v0.Normalized();
                Vector3 e1 = v1.Normalized();

                // if the pivot is lying on or nearly on the [i,i+1] segment...
                if (v0.Length() < CommonExtensions.GENERAL_CALC_TOLERANCE || Math.Abs(Vector3.Dot(e0, e1)) > (1 - CommonExtensions.GENERAL_CALC_TOLERANCE))
                {
                    pivot = pivot * 0.5f + this.Polygon_Coords[(i + 1 + n / 2) % n].ToVector3() * 0.5f;
                    v0 = this.Polygon_Coords[(i + 1) % n].ToVector3() - this.Polygon_Coords[i].ToVector3();
                    v1 = pivot - this.Polygon_Coords[i].ToVector3();

                    e0 = v0.Normalized();
                    e1 = v1.Normalized();
                }

                if (v0.Length() < CommonExtensions.GENERAL_CALC_TOLERANCE || Math.Abs(Vector3.Dot(e0, e1)) > (1 - CommonExtensions.GENERAL_CALC_TOLERANCE))
                {
                    if (i > 1)
                        normals.Add(normals[i - 1]);
                    else
                        normals.Add(Vector3.Zero);
                    continue;
                }

                // project v1 onto v0
                Vector3 p1 = this.Polygon_Coords[i].ToVector3() + Vector3.Dot(e0, e1) * v1.Length() * e0;

                // build normal
                Vector3 normal = p1 - pivot;
                normal.Normalize();

                // test if it points OUT(required) or INTO the polygon (for XZ - polygons)
                Vector3 testN = Vector3.Cross(v0, normal);
                if (Vector3.Dot(testN, pNormal) > 0)
                    normal *= -1f;

                //// for polygons that define a hole in a level (i.e. are interior)
                //if (!this.IsExterior)
                //    normal *= -1f;

                normals.Add(normal);
            }

            this.polygon_line_normals = new List<Vector3>(normals);
            return normals;
        }

        public Vector3 GetLineNormal(int _atIndex)
        {
            List<Vector3> lNormals = this.GetLineNormals();
            if (_atIndex > -1 && _atIndex < lNormals.Count)
                return lNormals[_atIndex];
            else
                return Vector3.Zero;
        }

        public Vector3 GetPivot()
        {
            Vector3 pivot = Vector3.Zero;
            int n = this.Polygon_Coords.Count;
            for(int i = 0; i < n; i++)
            {
                pivot += this.Polygon_Coords[i].ToVector3();
            }
            pivot /= n;

            return pivot;
        }

        
        private bool CalculateIfClockWise(double _tolerance, out bool isValid)
        {
            int n = this.Polygon_Coords.Count;
            if (n < 3)
            {
                isValid = false;
                return true;
            }

            double test = 0;

            // projection onto XZ - plane
            for(int i = 0; i < n ; i++)
            {
                test += (this.Polygon_Coords[(i + 1) % n].Z + this.Polygon_Coords[i].Z)*
                        (this.Polygon_Coords[(i + 1) % n].X - this.Polygon_Coords[i].X);
            }

            if (Math.Abs(test) > _tolerance)
            {
                isValid = true;
                return (test > 0);
            }

            // projection onto XY - plane
            test = 0;
            for (int i = 0; i < n; i++)
            {
                test += (this.Polygon_Coords[(i + 1) % n].Y + this.Polygon_Coords[i].Y) *
                        (this.Polygon_Coords[(i + 1) % n].X - this.Polygon_Coords[i].X);
            }

            if (Math.Abs(test) > _tolerance)
            {
                isValid = true;
                return (test > 0);
            }

            // projection onto YZ - plane
            test = 0;
            for (int i = 0; i < n; i++)
            {
                test += (this.Polygon_Coords[(i + 1) % n].Y + this.Polygon_Coords[i].Y) *
                        (this.Polygon_Coords[(i + 1) % n].Z - this.Polygon_Coords[i].Z);
            }

            if (Math.Abs(test) > _tolerance)
                isValid = true;
            else
                isValid = false;

            return (test > 0);
        }

        private Vector3 GetPolygonNormal()
        {
            int n = this.Polygon_Coords.Count;
            if (n < 3)
                return Vector3.Zero;

            Vector3 v1 = this.Polygon_Coords[1].ToVector3() - this.Polygon_Coords[0].ToVector3();
            v1.Normalize();

            Vector3 v2 = v1;
            for(int i = 2; i < n; i++)
            {
                v2 = this.Polygon_Coords[i].ToVector3() - this.Polygon_Coords[0].ToVector3();
                v2.Normalize();

                float dot = Vector3.Dot(v1, v2);
                if (Math.Abs(dot) < 1 - CommonExtensions.GENERAL_CALC_TOLERANCE)
                    break;
            }

            Vector3 normal = Vector3.Cross(v1, v2);
            normal.Normalize();

            return normal;
        }

        // implements Newell's method
        public Vector3 GetPolygonNormalNewell()
        {
            int n = this.Polygon_Coords.Count;
            if (n < 3)
                return Vector3.Zero;

            Vector3 normal = Vector3.Zero;
            for (int i = 0; i < n; i++ )
            {
                //Nx += (Vny - V(n+1)y) * (Vnz + V(n+1)z);
                //Ny += (Vnz - V(n+1)z) * (Vnx + V(n+1)x);
                //Nz += (Vnx - V(n+1)x) * (Vny + V(n+1)y);

                normal.X -= (float) ((this.Polygon_Coords[i].Z - this.Polygon_Coords[(i + 1) % n].Z) *
                                     (this.Polygon_Coords[i].Y + this.Polygon_Coords[(i + 1) % n].Y));
                normal.Y -= (float) ((this.Polygon_Coords[i].X - this.Polygon_Coords[(i + 1) % n].X) *
                                     (this.Polygon_Coords[i].Z + this.Polygon_Coords[(i + 1) % n].Z));
                normal.Z -= (float) ((this.Polygon_Coords[i].Y - this.Polygon_Coords[(i + 1) % n].Y) *
                                     (this.Polygon_Coords[i].X + this.Polygon_Coords[(i + 1) % n].X)); 
            }

            normal.Normalize();

            if (this.IsClockWise)
                return normal;
            else
                return -normal;
        }

        #endregion

        #region UTILITIES: Opening Geometry

        public List<List<Vector3>> GetOpeningGeometryForLabel(int _label)
        {
            List<List<Vector3>> openings = new List<List<Vector3>>();
            foreach(ZoneOpening zo in this.polygon_openings)
            {
                if (zo.IndInPolygon != _label) continue;
                // get 3d polyline vertices
                Vector3 v1 = zo.GetBottomFirstPoint3D();
                Vector3 v2 = zo.GetBottomSecondPoint3D();
                Vector3 v3 = zo.GetTopSecondPoint3D();
                Vector3 v4 = zo.GetTopFirstPoint3D();
                openings.Add(new List<Vector3> { v1, v2, v3, v4 });
            }

            return openings;
        }

        public int GetNrOfOpeningsForLabel(int _label)
        {
            return this.polygon_openings.Where(x => x.IndInPolygon == _label).Count();
        }

        #endregion

        #region UTILITIES: Geometry (Offset)

        // UNIFORM
        private List<Point3D> OffsetPolygon(float _offset, bool _out = true)
        {
            List<Vector3> normals = GetLineNormals(); // normalized
            List<Vector3> vNormals = new List<Vector3>();

            // calculate the displacement per vertex
            int n = this.Polygon_Coords.Count;
            for (int i = 0; i < n; i++)
            {
                // get the vector
                Vector3 interpolN;
                if (normals[i % n] == Vector3.Zero)
                    interpolN = normals[(n + i - 1) % n];
                else if (normals[(n + i - 1) % n] == Vector3.Zero)
                    interpolN = normals[i % n];
                else
                    interpolN = normals[i % n] * 0.5f + normals[(n + i - 1) % n] * 0.5f;
                
                float interpolLen = interpolN.Length();
                interpolN.Normalize();

                // calculate the magnitude
                if (interpolLen >= CommonExtensions.GENERAL_CALC_TOLERANCE)
                {
                    float magnitude = 1 / interpolLen;
                    interpolN *= magnitude;
                }
                else
                {
                    interpolN = normals[i % n];
                }

                if (_out)
                    interpolN *= _offset;
                else
                    interpolN *= _offset * -1f;

                vNormals.Add(interpolN);
            }

            // perform offset
            List<Point3D> polygon_offset = new List<Point3D>();
            for (int i = 0; i < n; i++)
            {
                Point3D vertex_displaced = (this.Polygon_Coords[i].ToVector3() + vNormals[i]).ToPoint3D();
                polygon_offset.Add(vertex_displaced);
            }

            return polygon_offset;
        }

        // NON-UNIFORM
        public List<Point3D> OffsetPolygon(List<float> _offsets, List<bool> _out)
        {
            if (_offsets == null || _out == null)
                return new List<Point3D>(this.Polygon_Coords);

            int n = this.Polygon_Coords.Count;
            if (_offsets.Count != n || _out.Count != n)
                return new List<Point3D>(this.Polygon_Coords);

            List<Vector3> normals = GetLineNormals(); // normalized
            List<Vector3> vNormals = new List<Vector3>();

            // calculate the displacement per vertex
            for (int i = 0; i < n; i++)
            {

                // calculate the displacement vector
                Vector3 normalPrev = (_out[(n + i - 1) % n]) ? normals[(n + i - 1) % n] : -normals[(n + i - 1) % n];
                Vector3 normalNext = (_out[i % n]) ? normals[i % n] : -normals[i % n];
                Vector3 interpolN;
                if (normals[i % n] == Vector3.Zero)
                    interpolN = normalPrev * _offsets[(n + i - 1) % n];
                else if (normals[(n + i - 1) % n] == Vector3.Zero)
                    interpolN = normalNext * _offsets[i % n];
                else
                {
                    // calculate the interpolating factors (notebook 1 PP.72-76)
                    float cosN1N2 = Vector3.Dot(normalNext, normalPrev);
                    float sinSqN1N2 = 1f - cosN1N2 * cosN1N2;
                    float n1L = _offsets[(n + i - 1) % n];
                    float n2L = _offsets[i % n];

                    float p1, p2;
                    if (sinSqN1N2 < CommonExtensions.GENERAL_CALC_TOLERANCE * 100)
                    {
                        p1 = 0;
                        p2 = n2L;                       
                    }
                    else
                    {
                        p1 = ((n1L - n2L * cosN1N2) / sinSqN1N2);
                        p2 = ((n2L - n1L * cosN1N2) / sinSqN1N2);
                    }

                    // interpolate
                    interpolN = normalPrev * p1 + normalNext * p2;
                }

                vNormals.Add(interpolN);
            }

            // perform offset
            List<Point3D> polygon_offset = new List<Point3D>();
            for (int i = 0; i < n; i++)
            {
                Point3D vertex_displaced = (this.Polygon_Coords[i].ToVector3() + vNormals[i]).ToPoint3D();
                polygon_offset.Add(vertex_displaced);
            }

            return polygon_offset;

        }

        #endregion

        #region UTILITIES: EditMode
        public static ZonePolygonEditModeType GetEditModeType(string _type)
        {
            if (_type == null)
                return ZonePolygonEditModeType.NONE;

            switch(_type)
            {
                case "VERTEX_EDIT":
                    return ZonePolygonEditModeType.VERTEX_EDIT;
                case "VERTEX_ADD":
                    return ZonePolygonEditModeType.VERTEX_ADD;
                case "VERTEX_REMOVE":
                    return ZonePolygonEditModeType.VERTEX_REMOVE;
                case "POLY_REVERSE":
                    return ZonePolygonEditModeType.POLY_REVERSE;
                case "POLY_LABELS_EDIT":
                    return ZonePolygonEditModeType.POLY_LABELS_EDIT;
                case "POLY_LABELS_DEFAULT":
                    return ZonePolygonEditModeType.POLY_LABELS_DEFAULT;
                case "OPENING_EDIT":
                    return ZonePolygonEditModeType.OPENING_EDIT;
                case"OPENING_ADD":
                    return ZonePolygonEditModeType.OPENING_ADD;
                case "OPENING_REMOVE":
                    return ZonePolygonEditModeType.OPENING_REMOVE;
                case "ISBEING_DELETED":
                    return ZonePolygonEditModeType.ISBEING_DELETED;
                default:
                    return ZonePolygonEditModeType.NONE;
            }
        }       
        #endregion

        #region UTILITIES: Drawing
        private static void CreateDashedLine(ref LineBuilder b, Vector3 _p0, Vector3 _p1, float _dashLength)
        {
            if (b == null)
                return;

            Vector3 v01 = _p1 - _p0;
            float totalLen = v01.Length();
            v01.Normalize();
            if (v01 == Vector3.Zero)
                return;

            if (_dashLength * 2f >= totalLen)
            {
                b.AddLine(_p0, _p1);
                return;
            }

            int nrLines = (int)Math.Ceiling((totalLen - _dashLength) / _dashLength);
            for (int i = 0; i < nrLines; i += 2)
            {
                float current = _dashLength * i;
                float next = current + _dashLength;

                if (current >= totalLen)
                {
                    break;
                }
                else
                {
                    if (next > totalLen)
                        next = totalLen;
                }

                Vector3 start = _p0 + v01 * current; // _dashLength * i
                Vector3 end = _p0 + v01 * next;      // _dashLength * (i + 1)
                b.AddLine(start, end);
            }

        }
        #endregion

        #region UTILITIES: Checking polyline before using it in the .ctor

        private List<Point3D> CleanUpPolyline(List<Point3D> _polyline)
        {
            if (_polyline == null) return null;
            if (_polyline.Count < 2) return new List<Point3D>();

            // check if start and end points overlap (as in closed polylines from CAD apps)
            Point3D pS = _polyline[0];
            Point3D pE = _polyline[_polyline.Count - 1];
            Vector3D dist = pE - pS;
            if (dist.LengthSquared > CommonExtensions.LINEDISTCALC_TOLERANCE)
            {
                return _polyline;
            }
            else
            {
                return _polyline.Take(_polyline.Count - 1).ToList();
            }
        }


        #endregion

        #region TO_STRING, DXF Export

        public override string ToString()
        {
            return "ZP: " + this.EntityName;
        }

        public override void AddToExport(ref StringBuilder _sb, bool _with_contained_entites)
        {
            if (_sb == null) return;

            _sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());      // 0
            _sb.AppendLine(DXFUtils.GV_ZONEDPOLY);                              // GV_ZONED_POLYGON

            _sb.AppendLine(((int)EntitySaveCode.CLASS_NAME).ToString());        // 100 (subclass marker)
            _sb.AppendLine(this.GetType().ToString());                          // GeometryViewer.EntityGeometry.ZonedPolygon

            string tmp = string.Empty;

            // layer
            _sb.AppendLine(((int)ZonedPolygonSaveCode.LAYER_NAME).ToString());       // 1106
            _sb.AppendLine(this.EntityLayer.EntityName);

            // color
            _sb.AppendLine(((int)EntitySpecificSaveCode.COLOR_BY_LAYER).ToString());    // 1008
            tmp = (this.ColorByLayer) ? "1" : "0";
            _sb.AppendLine(tmp);

            // vertex coordinates
            _sb.AppendLine(((int)ZonedPolygonSaveCode.VERTICES).ToString());    // 1101
            _sb.AppendLine(this.Polygon_Coords.Count.ToString());

            foreach(Point3D p in this.Polygon_Coords)
            {
                _sb.AppendLine(((int)EntitySaveCode.X_VALUE).ToString());    // 910
                _sb.AppendLine(DXFUtils.ValueToString(p.X, "F8"));
                _sb.AppendLine(((int)EntitySaveCode.Y_VALUE).ToString());    // 920
                _sb.AppendLine(DXFUtils.ValueToString(p.Y, "F8"));
                _sb.AppendLine(((int)EntitySaveCode.Z_VALUE).ToString());    // 930
                _sb.AppendLine(DXFUtils.ValueToString(p.Z, "F8"));
            }

            // zone indices
            _sb.AppendLine(((int)ZonedPolygonSaveCode.ZONE_INDICES).ToString());    // 1103
            _sb.AppendLine(this.Zones_Inds.Count.ToString());

            foreach(int index in this.Zones_Inds)
            {
                _sb.AppendLine(((int)EntitySaveCode.X_VALUE).ToString());   // 910
                _sb.AppendLine(index.ToString());
            }

            // zone openings
            if (this.polygon_openings.Count == 0)
            {
                _sb.AppendLine(((int)ZonedPolygonSaveCode.ZONE_OPENINGS).ToString());    // 1104
                _sb.AppendLine("0");
            }
            else
            {
                _sb.AppendLine(((int)ZonedPolygonSaveCode.ZONE_OPENINGS).ToString());    // 1104
                _sb.AppendLine(this.polygon_openings.Count.ToString());

                _sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());           // 0
                _sb.AppendLine(DXFUtils.ENTITY_SEQUENCE);                                // ENTSEQ

                foreach(ZoneOpening zo in this.polygon_openings)
                {
                    if (zo != null)
                        zo.AddToExport(ref _sb);
                }

                _sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());           // 0
                _sb.AppendLine(DXFUtils.SEQUENCE_END);                                   // SEQEND
                _sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());           // 0
                _sb.AppendLine(DXFUtils.ENTITY_CONTINUE);                                // ENTCTN
            }

            // polygon height
            _sb.AppendLine(((int)ZonedPolygonSaveCode.HEIGHT).ToString());              // 1105
            _sb.AppendLine(DXFUtils.ValueToString(this.Height, "F8"));                  // HEIGHT

            // also signifies end of complex entity (ends w SEQEND)
            base.AddToExport(ref _sb, _with_contained_entites);
        }

        public override void AddToACADExport(ref StringBuilder _sb, string _layer_name_visible, string _layer_name_hidden)
        {
            base.AddToACADExport(ref _sb, _layer_name_visible, _layer_name_hidden);

            // add the polygon to the export
            List<Vector3> coords_as_vectors = new List<Vector3>();
            foreach(Point3D p in this.Polygon_Coords)
            {
                Vector3 v = new Vector3((float)p.X, (float)p.Y, (float)p.Z);
                coords_as_vectors.Add(v);
            }
            DXFUtils.Add3dPolylineToExport(ref _sb, _layer_name_visible, this.EntityColor, coords_as_vectors, true);

            // add the openings to the export
            foreach(ZoneOpening zo in this.polygon_openings)
            {
                zo.AddToACADExport(ref _sb, _layer_name_visible, this.EntityColor);
            }
        }

        #endregion

    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // ==================================== VALUE CONVERTER FOR EDIT MODE TYPES =============================== //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region VALUE CONVERTER: ZonePolygonEditModeType

    [ValueConversion(typeof(ZonePolygonEditModeType), typeof(Boolean))]
    public class PolygonEditModeTypeToBooleanConverter : IValueConverter
    {
        // in order to react to more than one action mode at once
        // use + as OR; * as AND
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            ZonePolygonEditModeType zpet = ZonePolygonEditModeType.NONE;
            if (value is ZonePolygonEditModeType)
                zpet = (ZonePolygonEditModeType)value;

            string str_param = parameter.ToString();

            string[] str_params_OR = str_param.Split(new char[] { '+' });

            if (str_params_OR.Count() < 2)
                return (zpet == ZonedPolygon.GetEditModeType(str_param));
            else
            {
                foreach (string p in str_params_OR)
                {
                    if (zpet == ZonedPolygon.GetEditModeType(p))
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

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // ==================================== CUSTOM COMPARERS FOR ZONED POLYGONS =============================== //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region HEIGHT AND COMPLEX POLYGON COMPARER
    public class ZonedPolygonHeightComparer : IComparer<ZonedPolygon>
    {
        private double tolerance;
        public ZonedPolygonHeightComparer(double _tolerance = CommonExtensions.GENERAL_CALC_TOLERANCE)
        {
            this.tolerance = _tolerance;
        }

        public int Compare(ZonedPolygon _zp1, ZonedPolygon _zp2)
        {
            float h1 = _zp1.GetPivot().Y;
            float h2 = _zp2.GetPivot().Y;
            bool sameY = Math.Abs(h1 - h2) <= this.tolerance;

            if (sameY)
                return 0;
            else if (h1 > h2)
                return 1;
            else
                return -1;

        }
    }

    public class ZonedPolygonComplexComparer : IComparer<ZonedPolygon>
    {
        private double tolerance;
        public ZonedPolygonComplexComparer(double _tolerance = CommonExtensions.GENERAL_CALC_TOLERANCE)
        {
            this.tolerance = _tolerance;
        }

        public int Compare(ZonedPolygon _zp1, ZonedPolygon _zp2)
        {
            Vector3 pivot_1 = _zp1.GetPivot();
            Vector3 pivot_2 = _zp2.GetPivot();
            
            // 1. Compare heights
            bool sameY = Math.Abs(pivot_1.Y - pivot_2.Y) <= this.tolerance;
            if (sameY)
            {
                // 2. Compare positions along the Z-axis
                bool sameZ = Math.Abs(pivot_1.Z - pivot_2.Z) <= this.tolerance;
                if (sameZ)
                {
                    // 3. Compare position along the X-axis
                    bool sameX = Math.Abs(pivot_1.X - pivot_2.X) <= this.tolerance;
                    if(sameX)
                    {
                        // 4. Compare nr of vertices
                        bool sameNrV = (_zp1.Polygon_Coords.Count == _zp2.Polygon_Coords.Count);
                        if (sameNrV)
                        {
                            // 5. Compare sum of labels
                            int sumL1 = _zp1.Zones_Inds.Sum();
                            int sumL2 = _zp2.Zones_Inds.Sum();
                            bool sameLabelSum = (sumL1 == sumL2);
                            if (sameLabelSum)
                            {
                                // 6. Compare area
                                double area_1 = MeshesCustom.CalculatePolygonLargestSignedProjectedArea(CommonExtensions.ConvertPoints3DListToVector3List(_zp1.Polygon_Coords));
                                double area_2 = MeshesCustom.CalculatePolygonLargestSignedProjectedArea(CommonExtensions.ConvertPoints3DListToVector3List(_zp2.Polygon_Coords));
                                bool sameArea = Math.Abs(area_1 - area_2) <= this.tolerance;
                                if (sameArea)
                                {
                                    // 7. Compare perimeter
                                    double per1 = MeshesCustom.CalculatePolygonPerimeter(_zp1.Polygon_Coords);
                                    double per2 = MeshesCustom.CalculatePolygonPerimeter(_zp2.Polygon_Coords);
                                    bool samePerim = Math.Abs(per1 - per2) <= this.tolerance;
                                    if (samePerim)
                                    {
                                        return 0;
                                    }
                                    else
                                    {
                                        if (per1 > per2)
                                            return 1;
                                        else
                                            return -1;
                                    }
                                }
                                else
                                {
                                    if (area_1 > area_2)
                                        return 1;
                                    else
                                        return -1;
                                }
                            }
                            else
                            {
                                if (sumL1 > sumL2)
                                    return 1;
                                else
                                    return -1;
                            }
                        }
                        else
                        {
                            if (_zp1.Polygon_Coords.Count > _zp2.Polygon_Coords.Count)
                                return 1;
                            else
                                return -1;
                        }
                    }
                    else
                    {
                        if (pivot_1.X > pivot_2.X)
                            return 1;
                        else
                            return -1;
                    }                  
                }
                else
                {
                    if (pivot_1.Z > pivot_2.Z)
                        return 1;
                    else
                        return -1;
                }
            }
            else
            {
                if (pivot_1.Y > pivot_2.Y)
                    return 1;
                else
                    return -1;
            }
        }


    }


    #endregion

}
