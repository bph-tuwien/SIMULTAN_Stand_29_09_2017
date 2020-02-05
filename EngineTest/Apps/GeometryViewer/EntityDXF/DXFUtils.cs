using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

using SharpDX;

namespace GeometryViewer.EntityDXF
{

    #region SAVE CODES

    #region DXF Specs Codes
    public enum DXFSpecSaveCodes
    {
        // common
        ENTITY_TYPE         =   0,
        HANDLE              =   5,
        LINETYPE_NAME       =   6,
        LAYER_NAME          =   8,
        LINETYPE_SCALE      =  48,
        OBJ_VISIBILITY      =  60,
        COLOR_INDEX         =  62,
        DESIGN_SAPCE        =  67,
        NR_BYTES_IN_IMAGE   =  92,
        SUBCLASS_MARKER     = 100,
        START_END_APP_GROUP = 102,
        PREVIEW_IMAGE_DATA  = 310,
        TRUE_COLOR          = 420,

        // additional POINT (location in WCS)
        POINT_LOCATION_X    =  10, // for a LWPOLYLINE one entry / vertex, center of circle
        POINT_LOCATION_Y    =  20, // for a LWPOLYLINE one entry / vertex
        POINT_LOCATION_Z    =  30,
        THICKNESS           =  39,
        X_AXIS_ANGLE        =  50,
        EXTRUSION_DIR_X     = 210,
        EXTRUSION_DIR_Y     = 220,
        EXTRUSION_DIR_Z     = 230,

        // additional LINE
        END_POINT_X         =  11,
        END_POINT_Y         =  21,
        END_POINT_Z         =  31,

        // additional LWPOLYLINE
        ELEVATION           =  38,
        WIDTH_START         =  40,
        WIDTH_END           =  41,
        BULGE               =  42,
        WIDTH_CONST         =  43,
        NR_VERTICES         =  90,
        VERTEX_ID           =  91, // ACAD 2012

        // additional POLYLINE
        ENT_FLOW_FLAG       =  66, // obsolete since ACAD 2012
        // 0 = no vertices, default; 1 = vertices, followed by SEQEND
        POLYLINE_FLAG       =  70,
        // bit-coded: 0 = default, 1 = closed, 
        // 2 = curve for vertices added, 4 = spline-fit vertices added, 
        // 8 = 3d, 16 = 3d polygon mesh
        // 32 = polygon mesh closed in N direction, 64 = polyface mesh
        // 128 = linetype generated continuously around the vertices of the poly
        POLYGMESH_M_VERT_COUNT = 71,
        POLYGMESH_N_VERT_COUNT = 72,
        SMOOTHSURF_M_DENSITY   = 73,
        SMOOTHSURF_N_DENSITY   = 74,
        SMOOTHSURF_M_TYPE      = 75,
        // int coded: 0 = none, 5 = quadratic B-spline surface
        // 6 = cubic B-spline surface, 8 = Bezier surface

        // additional CIRCLE, ARC
        RADIUS                  = 40,
        ANGLE_START             = 50,
        ANGLE_END               = 51

    }
    #endregion

    // general codes for all types
    public enum EntitySaveCode
    {
        INVALID_CODE = -11, // random, has to be negative (DXF convention)
        ENTITY_START = 0,   // DXF specs
        ENTITY_NAME = 2,    // DXF specs
        CLASS_NAME = 100,   // AutoCAD specs
        ENTITY_ID = 900,    // custom
        NUMBER_OF = 901,    // ...
        TIME_STAMP = 902,
        ENTITY_REF = 903,   // saves the ID of a referenced entity (can be in another file)
        ENTITY_KEY = 904,   // for saving dictionaries

        VISIBILITY = 60,    // DXF specs (0 = visible, 1 = invisible)
        COLOR_INDEX = 62,   // DXF specs (default = BYLAYER, 0 = BYBLOCK, 256 = BYLAYER, neg. = layer off)
        TRUECOLOR = 420,    // DXF specs ((mask 0xIIRRGGBB), II = 194: index color, II = 195: true color)
        SPACE = 67,         // DXF specs (absent or 0 = entity in model space, 1 = entity in paper space, default = 0)

        STRING_VALUE = 909,
        X_VALUE = 910,
        Y_VALUE = 920,
        Z_VALUE = 930,
        W_VALUE = 940,
        V5_VALUE = 950,
        V6_VALUE = 960,
        V7_VALUE = 970,
        V8_VALUE = 980,
        V9_VALUE = 990,
        V10_VALUE = 1000
    }

    public enum EntitySpecificSaveCode
    {
        VALIDITY = 1001,
        ASSOC_W_OTHER = 1002, // e.g. components of other program modules
        CONTAINED_ENTITIES = 1003,

        // ARC
        VISLINE_THICKNESS = 1004,
        TEXT_LINE = 1005,

        // BPH
        IS_TOP_CLOSURE = 1006,
        IS_BOTTOM_CLOSURE = 1007,

        // GeometricEntity
        COLOR_BY_LAYER = 1008
    }


    public enum ZonedPolygonSaveCode
    {
        VERTICES = 1101,
        VERTICES_ACAD = 1102,        // to use when saving the polygon as an AutoCAD Enitiy POLYLINE
        ZONE_INDICES = 1103,
        ZONE_OPENINGS = 1104,
        HEIGHT = 1105,
        LAYER_NAME = 1106
    }

    public enum ZonedPolygonOpeningSaveCode
    {
        ID = 1151,
        NAME = 1152,
        IND_IN_POLYGON = 1153,
        VERTEX_PREV = 1154,
        VERTEX_NEXT = 1155,
        POLYGON_WALL_TANGENT = 1156,
        ADJUSTED_POLYGON_WALL_TANGENT = 1157,

        DIST_FROM_VERTEX_PREV = 1158,
        LENGTH_ALONG_POLYGON_SEGMENT = 1159,
        DIST_FROM_POLYGON_ALONG_WALL_TANGENT = 1160,
        HEIGHT_ALONG_WALL_TANGENT = 1161
    }

    public enum ZonedVolumeSaveCode
    {
        LEVELS = 1201,
        POLYGON_REFERENCE = 1202,
        MATERIALS_PER_LEVEL = 1203,
        MATERIALS_PER_LABEL = 1204
    }

    public enum MaterialSaveCode
    {
        ID = 1301,
        Name = 1302,
        THICKNESS = 1303,
        POSITION = 1304,
        ACC_AREA = 1305,
        IS_BOUND_2CR = 1306,
        BOUND_CRID = 1307,
        NR_ASSOC_SURFACES = 1308
    }

    #endregion

    public static class DXFUtils
    {
        // CONST
        public const string GV_ENTITY = "GV_ENTITY";                                    // custom
        public const string GV_LAYER = "GV_LAYER";                                      // custom
        public const string GV_ZONEDPOLY = "GV_ZONED_POLYGON";                          // custom
        public const string GV_ZONEDPOLY_OPENING = "GV_ZONED_POLYGON_OPENING";          // custom
        public const string GV_ZONEDLEVEL = "GV_ZONED_POLYGON_GROUP";                   // custom
        public const string GV_ZONEDVOL = "GV_ZONED_VOLUME";                            // custom
        public const string GV_MATERIAL = "GV_MATERIAL";                                // custom

        public const string LAYER = "LAYER";                            // DXF specs
        public const string LTYPE = "LTYPE";                            // DXF specs

        public const string TABLE = "TABLE";                            // DXF specs
        public const string TABLE_END = "ENDTAB";                       // DXF specs

        public const string MATERIAL_SECTION = "MATERIALS";             // custom
        public const string ENTITY_SECTION = "ENTITIES";                // DXF specs
        public const string TABLES_SECTION = "TABLES";                  // DXF specs
        public const string SECTION_START = "SECTION";                  // DXF specs
        public const string SECTION_END = "ENDSEC";                     // DXF specs
        public const string SEQUENCE_END = "SEQEND";                    // DXF specs
        public const string EOF = "EOF";                                // DXF specs

        public const string ENTITY_SEQUENCE = "ENTSEQ";                 // custom
        public const string ENTITY_CONTINUE = "ENTCTN";                 // custom

        public const string INFINITY = "\U0000221E";
        public const string NAN = "NaN";

        public const string LAYER_HIDDEN_SUFFIX = "_hidden";

        // FORMATTERS
        public static readonly DateTimeFormatInfo DT_FORMATTER = new DateTimeFormatInfo();
        public static IFormatProvider NR_FORMATTER = new NumberFormatInfo();

        // FILE EXTENSIONS
        public const string FILE_EXT_GEOMETRY = "geodxf";

        #region FLOATING POINT NUMBER VALUE HANDLING
        public static string ValueToString(double _value, string _format_string)
        {
            string number_format_string = "F2";
            if (_format_string != null)
            {
                Regex format_detector = new Regex(@"\bF[0-9]{1}\b");
                if (format_detector.IsMatch(_format_string))
                {
                    number_format_string = _format_string;
                }
            }

            if (double.IsNaN(_value))
                return DXFUtils.NAN;
            else if (double.IsPositiveInfinity(_value) || _value == double.MaxValue)
                return "+" + DXFUtils.INFINITY;
            else if (double.IsNegativeInfinity(_value) || _value == double.MinValue)
                return "-" + DXFUtils.INFINITY;
            else
                return _value.ToString(number_format_string, DXFUtils.NR_FORMATTER);
        }

        public static double StringToDouble(string _input)
        {
            if (_input == null) return 0.0;
            switch (_input)
            {
                case DXFUtils.NAN:
                    return double.NaN;
                case "+" + DXFUtils.INFINITY:
                    return double.MaxValue;
                case "-" + DXFUtils.INFINITY:
                    return double.MinValue;
                default:
                    double f;
                    bool success = Double.TryParse(_input, NumberStyles.Float, DXFUtils.NR_FORMATTER, out f);
                    if (success)
                        return f;
                    else
                        return 0.0;
            }
        }
        #endregion

        #region EXPORTING FOR ACAD: GEOMETRY

        public static void Add3dPolylineToExport(ref StringBuilder _sb, string _layer_name, SharpDX.Color _color, 
                                                    List<Vector3> _vertices, bool _is_closed = true)
        {
            if (_sb == null) return;
            if (_vertices == null || _vertices.Count == 0) return;
            string tmp = string.Empty;

            _sb.AppendLine(((int)DXFSpecSaveCodes.ENTITY_TYPE).ToString());     // 0
            _sb.AppendLine("POLYLINE");

            // common
            _sb.AppendLine(((int)DXFSpecSaveCodes.SUBCLASS_MARKER).ToString()); // 100
            _sb.AppendLine("AcDbEntity");

            _sb.AppendLine(((int)DXFSpecSaveCodes.LAYER_NAME).ToString());      // 8
            tmp = (string.IsNullOrEmpty(_layer_name)) ? "0" : _layer_name;
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)DXFSpecSaveCodes.COLOR_INDEX).ToString());     // 62
            DXFImportExport.DXFColor dxf_color = new DXFImportExport.DXFColor((float)_color.R, (float)_color.G, (float)_color.B, (float)_color.A, true);
            int ic = 256; // DXFImportExport.DXFColor.DXFColor2Index(dxf_color);
            _sb.AppendLine(ic.ToString());                                      // 256 = ByLayer

            //_sb.AppendLine(((int)DXFSpecSaveCodes.TRUE_COLOR).ToString());      // 420
            long tc = DXFImportExport.DXFColor.DXFColor2TrueColor(dxf_color);
            //_sb.AppendLine(tc.ToString());

            // 3d polyline
            _sb.AppendLine(((int)DXFSpecSaveCodes.SUBCLASS_MARKER).ToString()); // 100
            _sb.AppendLine("AcDb3dPolyline");

            _sb.AppendLine(((int)DXFSpecSaveCodes.ENT_FLOW_FLAG).ToString());    // 66
            _sb.AppendLine("1");

            _sb.AppendLine(((int)DXFSpecSaveCodes.POINT_LOCATION_X).ToString()); // 10
            _sb.AppendLine("0");
            _sb.AppendLine(((int)DXFSpecSaveCodes.POINT_LOCATION_Y).ToString()); // 20
            _sb.AppendLine("0");
            _sb.AppendLine(((int)DXFSpecSaveCodes.POINT_LOCATION_Z).ToString()); // 30
            _sb.AppendLine("0");

            _sb.AppendLine(((int)DXFSpecSaveCodes.POLYLINE_FLAG).ToString());     // 70: closed(1) 3d polyline(8)
            tmp = (_is_closed) ? "9" : "8";
            _sb.AppendLine(tmp);

            // 3d polyline vertices
            foreach (Vector3 v in _vertices)
            {
                Vector3 v_corrected = new Vector3(v.X, -v.Z, v.Y);
                DXFUtils.Add3dPolylineVertexToExport(ref _sb, v_corrected, _layer_name, 256, tc);
            }

            // mark end of vertex sequence
            _sb.AppendLine(((int)DXFSpecSaveCodes.ENTITY_TYPE).ToString());     // 0
            _sb.AppendLine("SEQEND");

            _sb.AppendLine(((int)DXFSpecSaveCodes.SUBCLASS_MARKER).ToString()); // 100
            _sb.AppendLine("AcDbEntity");

            _sb.AppendLine(((int)DXFSpecSaveCodes.LAYER_NAME).ToString());      // 8
            tmp = (string.IsNullOrEmpty(_layer_name)) ? "0" : _layer_name;
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)DXFSpecSaveCodes.COLOR_INDEX).ToString());     // 62
            _sb.AppendLine(ic.ToString());

            //_sb.AppendLine(((int)DXFSpecSaveCodes.TRUE_COLOR).ToString());      // 420
            //_sb.AppendLine(tc.ToString());
        }

        public static void Add3dPolylineVertexToExport(ref StringBuilder _sb, Vector3 _coords, string _layer_name, int _index_color, long _true_color)
        {
            if (_sb == null) return;
            string tmp = string.Empty;

            _sb.AppendLine(((int)DXFSpecSaveCodes.ENTITY_TYPE).ToString());     // 0
            _sb.AppendLine("VERTEX");

            // common
            _sb.AppendLine(((int)DXFSpecSaveCodes.SUBCLASS_MARKER).ToString()); // 100
            _sb.AppendLine("AcDbEntity");

            _sb.AppendLine(((int)DXFSpecSaveCodes.LAYER_NAME).ToString());      // 8
            tmp = (string.IsNullOrEmpty(_layer_name)) ? "0" : _layer_name;
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)DXFSpecSaveCodes.COLOR_INDEX).ToString());     // 62
            _sb.AppendLine(_index_color.ToString());                            // 0 = ByBlock, 256 = ByLayer

            //_sb.AppendLine(((int)DXFSpecSaveCodes.TRUE_COLOR).ToString());      // 420
            //_sb.AppendLine(_true_color.ToString());

            // vertex
            _sb.AppendLine(((int)DXFSpecSaveCodes.SUBCLASS_MARKER).ToString()); // 100
            _sb.AppendLine("AcDbVertex");
            _sb.AppendLine(((int)DXFSpecSaveCodes.SUBCLASS_MARKER).ToString()); // 100
            _sb.AppendLine("AcDb3dPolylineVertex");

            _sb.AppendLine(((int)DXFSpecSaveCodes.POINT_LOCATION_X).ToString()); // 10
            _sb.AppendLine(DXFUtils.ValueToString(_coords.X, "F8"));
            _sb.AppendLine(((int)DXFSpecSaveCodes.POINT_LOCATION_Y).ToString()); // 20
            _sb.AppendLine(DXFUtils.ValueToString(_coords.Y, "F8"));
            _sb.AppendLine(((int)DXFSpecSaveCodes.POINT_LOCATION_Z).ToString()); // 30
            _sb.AppendLine(DXFUtils.ValueToString(_coords.Z, "F8"));

            _sb.AppendLine(((int)DXFSpecSaveCodes.POLYLINE_FLAG).ToString());     // 70
            _sb.AppendLine("32");                                                 // 3d polyline vertex
        }

        #endregion

        #region EXPORTING FOR ACAD: LINETYPES and LAYERS

        public static void AddLineTypeAndLayerDefinitionsToExport(ref StringBuilder _sb, List<string> _layer_names, List<SharpDX.Color> _layer_colors)
        {
            if (_sb == null) return;

            // ---------------------- START TABLE SECTION -------------------------------------
            _sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());           // 0
            _sb.AppendLine(DXFUtils.SECTION_START);                                  // SECTION
            _sb.AppendLine(((int)EntitySaveCode.ENTITY_NAME).ToString());            // 2
            _sb.AppendLine(DXFUtils.TABLES_SECTION);                                 // TABLES

            // ======================= START LTYPE TABLE  =====================================
            _sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());           // 0
            _sb.AppendLine(DXFUtils.TABLE);                                          // TABLE
            _sb.AppendLine(((int)EntitySaveCode.ENTITY_NAME).ToString());            // 2
            _sb.AppendLine(DXFUtils.LTYPE);                                          // LTYPE
            _sb.AppendLine(((int)DXFSpecSaveCodes.SUBCLASS_MARKER).ToString());      // 100
            _sb.AppendLine("AcDbSymbolTable");
            _sb.AppendLine("70");                                                    // maximum nr of entries in table
            _sb.AppendLine("2");

            DXFUtils.AddLineTypeDefinitionToExport(ref _sb, "Continuous", "Solid line", "");
            //DXFUtils.AddLineTypeDefinitionToExport(ref _sb, "AM_ISO02W050x2", " _ _ _ _ _ _ _ _ _ _ _ _", "3.0|-0.75");

            _sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());           // 0
            _sb.AppendLine(DXFUtils.TABLE_END);                                      // ENDTAB
            // ======================== END LTYPE TABLE  ======================================


            // ======================= START LAYER TABLE  =====================================
            _sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());           // 0
            _sb.AppendLine(DXFUtils.TABLE);                                          // TABLE
            _sb.AppendLine(((int)EntitySaveCode.ENTITY_NAME).ToString());            // 2
            _sb.AppendLine(DXFUtils.LAYER);                                          // LAYER
            _sb.AppendLine(((int)DXFSpecSaveCodes.SUBCLASS_MARKER).ToString());      // 100
            _sb.AppendLine("AcDbSymbolTable");
            _sb.AppendLine("70");                                                    // maximum nr of entries in table
            _sb.AppendLine((_layer_names.Count * 2).ToString());

            // add the default layer 0:
            DXFUtils.AddLayerDefinitionToExport(ref  _sb, "0", "Continuous", SharpDX.Color.Black, false);
            // add all other layers
            if (_layer_names != null && _layer_colors != null)
            {
                int nrL = _layer_names.Count;
                if (nrL == _layer_colors.Count)
                {
                    for(int i = 0; i < nrL; i++)
                    {
                        DXFUtils.AddLayerDefinitionToExport(ref  _sb, _layer_names[i], "Continuous", _layer_colors[i], false);
                        DXFUtils.AddLayerDefinitionToExport(ref  _sb, _layer_names[i] + DXFUtils.LAYER_HIDDEN_SUFFIX, "Continuous", _layer_colors[i], true);
                    }
                }
            }

            _sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());           // 0
            _sb.AppendLine(DXFUtils.TABLE_END);                                      // ENDTAB
            // ======================== END LAYER TABLE  ======================================


            _sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());           // 0
            _sb.AppendLine(DXFUtils.SECTION_END);                                    // ENDSEC
            // ----------------------- END TABLE SECTION --------------------------------------
        }

        private static void AddLineTypeDefinitionToExport(ref StringBuilder _sb, string _name, string _description, string _pattern = "")
        {
            if (_sb == null || string.IsNullOrEmpty(_name)) return;
            string descr = (string.IsNullOrEmpty(_description)) ? "Solid line" : _description;

            _sb.AppendLine(((int)DXFSpecSaveCodes.ENTITY_TYPE).ToString());          // 0
            _sb.AppendLine(DXFUtils.LTYPE);                                          // LTYPE
            _sb.AppendLine(((int)DXFSpecSaveCodes.SUBCLASS_MARKER).ToString());      // 100
            _sb.AppendLine("AcDbSymbolTableRecord");
            _sb.AppendLine(((int)DXFSpecSaveCodes.SUBCLASS_MARKER).ToString());      // 100
            _sb.AppendLine("AcDbLinetypeTableRecord");

            _sb.AppendLine(((int)EntitySaveCode.ENTITY_NAME).ToString());            // 2
            _sb.AppendLine(_name);                                                   // ENTITY_NAME
            _sb.AppendLine("70");                                                    // 70 flag for XREFS and EDITING MARKER in AutoCAD
            _sb.AppendLine("0");                                                     // set no flags
            _sb.AppendLine("3");                                                     // 3 descriptive text for the linetype
            _sb.AppendLine(descr);                                                   // description
            _sb.AppendLine("72");                                                    // alignment code; value is always 65, the ASCII code for A
            _sb.AppendLine("65");                                                    // A

            // analyse the pattern
            double[] pattern_element_length;
            double total_pattern_length = 0.0;
            int nr_pattern_elements = 0;
            string[] parts = _pattern.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts != null && parts.Length > 1)
            {
                nr_pattern_elements = parts.Length;
                pattern_element_length = new double[nr_pattern_elements];
                int c = 0;
                foreach(string x in parts)
                {
                    pattern_element_length[c] = DXFUtils.StringToDouble(x);
                    c++;
                }
                total_pattern_length = pattern_element_length.Select(x => Math.Abs(x)).Sum();

                _sb.AppendLine("73");                                                    // the number of linetype elements
                _sb.AppendLine(nr_pattern_elements.ToString());
                _sb.AppendLine("40");                                                    // total pattern length
                _sb.AppendLine(DXFUtils.ValueToString(total_pattern_length, "F15"));

                foreach (double d in pattern_element_length)
                {
                    _sb.AppendLine("49");                                                    // dash, dot or space length
                    _sb.AppendLine(DXFUtils.ValueToString(d, "F15"));
                    //_sb.AppendLine("74");                                                    // line element type flags (AutoCAD dosen't recognize it)
                    //_sb.AppendLine("0");
                }
            }
            else
            {
                _sb.AppendLine("73");                                                    // the number of linetype elements
                _sb.AppendLine("0");
                _sb.AppendLine("40");                                                    // total pattern length
                _sb.AppendLine("0.0");
            }

        }

        private static void AddLayerDefinitionToExport(ref StringBuilder _sb, string _layer_name, string _ltype_name, SharpDX.Color _color, bool _marked_as_hidden)
        {
            if (_sb == null || string.IsNullOrEmpty(_layer_name)) return;
            string ltype = (string.IsNullOrEmpty(_ltype_name)) ? "Continuous" : _ltype_name;

            _sb.AppendLine(((int)DXFSpecSaveCodes.ENTITY_TYPE).ToString());          // 0
            _sb.AppendLine(DXFUtils.LAYER);                                          // LAYER
            _sb.AppendLine(((int)DXFSpecSaveCodes.SUBCLASS_MARKER).ToString());      // 100
            _sb.AppendLine("AcDbSymbolTableRecord");
            _sb.AppendLine(((int)DXFSpecSaveCodes.SUBCLASS_MARKER).ToString());      // 100
            _sb.AppendLine("AcDbLayerTableRecord");

            _sb.AppendLine(((int)EntitySaveCode.ENTITY_NAME).ToString());            // 2
            _sb.AppendLine(_layer_name);                                             // ENTITY_NAME
            /*
                70: Standard flags (bit-coded values)
                1 = Layer is frozen; otherwise layer is thawed.
                2 = Layer is frozen by default in new viewports.
                4 = Layer is locked.
                16 = If set, table entry is externally dependent on an xref.
                32 = If this bit and bit 16 are both set, the externally dependent xref has been successfully resolved.
                64 = If set, the table entry was referenced by at least one entity in the drawing the last time the drawing was edited.
             */
            _sb.AppendLine("70");                                                    // 70
            _sb.AppendLine("0");
            _sb.AppendLine(((int)DXFSpecSaveCodes.COLOR_INDEX).ToString());          // 62
            DXFImportExport.DXFColor dxf_color = new DXFImportExport.DXFColor((float)_color.R, (float)_color.G, (float)_color.B, (float)_color.A, true);
            int ic = DXFImportExport.DXFColor.DXFColor2Index(dxf_color);
            if (_marked_as_hidden)
            {
                ic = (ic / 10) * 10 + 1;
            }
            _sb.AppendLine(ic.ToString());
            //_sb.AppendLine(((int)DXFSpecSaveCodes.TRUE_COLOR).ToString());         // 420
            //long tc = DXFImportExport.DXFColor.DXFColor2TrueColor(new DXFImportExport.DXFColor((float)_color.R, (float)_color.G, (float)_color.B, (float)_color.A, true));
            //_sb.AppendLine(tc.ToString());
            _sb.AppendLine(((int)DXFSpecSaveCodes.LINETYPE_NAME).ToString());        // 6
            _sb.AppendLine(ltype);

        }

        #endregion
    }
}
