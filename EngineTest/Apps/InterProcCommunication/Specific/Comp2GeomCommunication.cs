using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

// =============================== NOTE: BAD DESIGN ================================= //
//       these classes and enums have to correspond to ParameterStructure.Geometry    //
// ================================================================================== //
namespace InterProcCommunication.Specific
{
    public static class Comp2GeomCommunication
    {
        #region RESERVED PARAMTER NAMES

        public const string RP_AREA = "A";
        public const string RP_WIDTH = "b";
        public const string RP_HEIGHT = "h";
        public const string RP_DIAMETER = "d";
        public const string RP_LENGTH = "L";

        public const string RP_AREA_MAX = "AᴍᴀX";
        public const string RP_WIDTH_MAX = "BᴍᴀX";
        public const string RP_HEIGHT_MAX = "HᴍᴀX";
        public const string RP_DIAMETER_MAX = "DᴍᴀX";
        public const string RP_LENGTH_MAX = "LᴍᴀX";

        public const string RP_AREA_MIN = "AᴍɪN";
        public const string RP_WIDTH_MIN = "BᴍɪN";
        public const string RP_HEIGHT_MIN = "HᴍɪN";
        public const string RP_DIAMETER_MIN = "DᴍɪN";
        public const string RP_LENGTH_MIN = "LᴍɪN";

        public const string RP_K_FOK = "Kꜰᴏᴋ";
        public const string RP_K_FOK_ROH = "Kꜰᴏᴋʀ";
        public const string RP_K_F_AXES = "Kꜰᴀ";
        public const string RP_K_DUK = "Kᴅᴜᴋ";
        public const string RP_K_DUK_ROH = "Kᴅᴜᴋʀ";
        public const string RP_K_D_AXES = "Kᴅᴀ";
        public const string RP_H_NET = "Hʟɪᴄʜᴛ";
        public const string RP_H_GROSS = "Hʀᴏʜ";
        public const string RP_H_AXES = "Hᴀ";
        public const string RP_L_PERIMETER = "Lᴘᴇʀ";
        public const string RP_AREA_BGF = "Aᴃɢꜰ";
        public const string RP_AREA_NGF = "Aɴɢꜰ";
        public const string RP_AREA_NF = "Aɴꜰ";
        public const string RP_AREA_AXES = "Aᴀ";
        public const string RP_VOLUME_BRI = "Vᴃʀɪ";
        public const string RP_VOLUME_NRI = "Vɴʀɪ";
        public const string RP_VOLUME_NRI_NF = "Vɴʀɪɴꜰ";
        public const string RP_VOLUME_AXES = "Vᴀ";

        public const string RP_MATERIAL_COMPOSITE_D_OUT = "Δdout";
        public const string RP_MATERIAL_COMPOSITE_D_IN = "Δdin";

        public const string RP_COUNT = "NRᴛᴏᴛᴀʟ";

        public static Dictionary<string, double> GetReservedParamDictionary(Relation2GeomType _type)
        {
            Dictionary<string, double> output = new Dictionary<string, double>();

            switch(_type)
            {
                case Relation2GeomType.DESCRIBES_3D:
                case Relation2GeomType.GROUPS:
                    output.Add(RP_K_FOK, 0.0);
                    output.Add(RP_K_FOK_ROH, 0.0);
                    output.Add(RP_K_F_AXES, 0.0);
                    output.Add(RP_K_DUK, 0.0);
                    output.Add(RP_K_DUK_ROH, 0.0);
                    output.Add(RP_K_D_AXES, 0.0);
                    output.Add(RP_H_NET, 0.0);
                    output.Add(RP_H_GROSS, 0.0);
                    output.Add(RP_H_AXES, 0.0);
                    output.Add(RP_L_PERIMETER, 0.0);
                    output.Add(RP_AREA_BGF, 0.0);
                    output.Add(RP_AREA_NGF, 0.0);
                    output.Add(RP_AREA_NF, 0.0);
                    output.Add(RP_AREA_AXES, 0.0);
                    output.Add(RP_VOLUME_BRI, 0.0);
                    output.Add(RP_VOLUME_NRI, 0.0);
                    output.Add(RP_VOLUME_NRI_NF, 0.0);
                    output.Add(RP_VOLUME_AXES, 0.0);
                    break;
                case Relation2GeomType.DESCRIBES_2DorLESS:
                    output.Add(RP_AREA, 0.0);
                    output.Add(RP_AREA_MIN, 0.0);
                    output.Add(RP_AREA_MAX, 0.0);
                    output.Add(RP_WIDTH, 0.0);
                    output.Add(RP_WIDTH_MIN, 0.0);
                    output.Add(RP_WIDTH_MAX, 0.0);
                    output.Add(RP_HEIGHT, 0.0);                    
                    output.Add(RP_HEIGHT_MIN, 0.0);
                    output.Add(RP_HEIGHT_MAX, 0.0);                   
                    break;
                case Relation2GeomType.ALIGNED_WITH:                    
                    output.Add(RP_AREA, 0.0);
                    output.Add(RP_MATERIAL_COMPOSITE_D_OUT, 0.0);
                    output.Add(RP_MATERIAL_COMPOSITE_D_IN, 0.0);
                    output.Add(RP_COUNT, 0.0);
                    break;
                case Relation2GeomType.CONTAINED_IN:
                case Relation2GeomType.CONNECTS:
                default:
                    break;
            }

            return output;
        }

        #endregion

    }

    public enum Relation2GeomType
    {
        NONE = 0,                   // components not dependent on geometry (DEFUALT VALUE IN .NET)
        DESCRIBES = 1,              // for Architectural Spaces (Zones) as a master component
        DESCRIBES_3D = 2,           // AUTOMATIC: for Volumes
        DESCRIBES_2DorLESS = 3,     // AUTOMATIC: for Surfaces, Edges or Points
        ALIGNED_WITH = 4,           // for Building Physics (Aufbauten: do not forget position of axis plane!)
        CONTAINED_IN = 5,           // for HVAC MEP components (Verortung)
        CONNECTS = 6,               // for HVAC ducts and pipes
        GROUPS = 7,                 // for Architectural Functions (e.g. Floor) or Building Physics Zones (Thermal Hull)
    }

    public struct Relation2GeomState
    {
        public bool IsRealized { get; set; } // default value = false
        public Relation2GeomType Type { get; set; } // default value = NONE

        public Relation2GeomState(Relation2GeomState _state)
            :this()
        {
            this.IsRealized = _state.IsRealized;
            this.Type = _state.Type;
        }
    }


    public static class GeometryUtils
    {
        #region Relation2GeomType

        public static Relation2GeomType StringToRelationship2Geometry(string _input)
        {
            if (string.IsNullOrEmpty(_input)) return Relation2GeomType.NONE;

            switch (_input)
            {
                case "DESCRIBES":
                    return Relation2GeomType.DESCRIBES;
                case "DESCRIBES_3D":
                    return Relation2GeomType.DESCRIBES_3D;
                case "DESCRIBES_2DorLESS":
                    return Relation2GeomType.DESCRIBES_2DorLESS;
                case "ALIGNED_WITH":
                    return Relation2GeomType.ALIGNED_WITH;
                case "CONTAINED_IN":
                    return Relation2GeomType.CONTAINED_IN;
                case "CONNECTS":
                    return Relation2GeomType.CONNECTS;
                case "GROUPS":
                    return Relation2GeomType.GROUPS;
                default:
                    return Relation2GeomType.NONE;
            }
        }

        public static string Relationship2GeometryToString(Relation2GeomType _rel)
        {
            switch (_rel)
            {
                case Relation2GeomType.DESCRIBES:
                    return "DESCRIBES";
                case Relation2GeomType.DESCRIBES_3D:
                    return "DESCRIBES_3D";
                case Relation2GeomType.DESCRIBES_2DorLESS:
                    return "DESCRIBES_2DorLESS";
                case Relation2GeomType.ALIGNED_WITH:
                    return "ALIGNED_WITH";
                case Relation2GeomType.CONTAINED_IN:
                    return "CONTAINED_IN";
                case Relation2GeomType.CONNECTS:
                    return "CONNECTS";
                case Relation2GeomType.GROUPS:
                    return "GROUPS";
                default:
                    return "NONE";
            }
        }

        public static string Relationship2GeometryToDescrDE(Relation2GeomType _rel)
        {
            switch (_rel)
            {
                case Relation2GeomType.DESCRIBES:
                    return "Beschreibt Raum";
                case Relation2GeomType.DESCRIBES_3D:
                    return "Beschreibt Volumen";
                case Relation2GeomType.DESCRIBES_2DorLESS:
                    return "Beschreibt Fläche / Kante";
                case Relation2GeomType.ALIGNED_WITH:
                    return "Begrenzend";
                case Relation2GeomType.CONTAINED_IN:
                    return "Verortet";
                case Relation2GeomType.CONNECTS:
                    return "Verbindend";
                case Relation2GeomType.GROUPS:
                    return "Gruppierend";
                default:
                    return "keine Geometrie";
            }
        }

        public static string Relationship2GeometryToCompNameDE(Relation2GeomType _rel)
        {
            switch (_rel)
            {
                case Relation2GeomType.DESCRIBES:
                    return "Raum";
                case Relation2GeomType.DESCRIBES_3D:
                    return "Volumen";
                case Relation2GeomType.DESCRIBES_2DorLESS:
                    return "Fläche";
                case Relation2GeomType.ALIGNED_WITH:
                    return "Aufbau";
                case Relation2GeomType.CONTAINED_IN:
                    return "Verortung";
                case Relation2GeomType.CONNECTS:
                    return "Verbindung";
                case Relation2GeomType.GROUPS:
                    return "Gruppe";
                default:
                    return "---";
            }
        }

        #endregion

        #region METHODS: Matrix3D Extension 

        public static Matrix3D Copy(this Matrix3D _original)
        {
            if (_original == null) return Matrix3D.Identity;

            Matrix3D copy = Matrix3D.Identity;

            copy.M11 = _original.M11;
            copy.M12 = _original.M12;
            copy.M13 = _original.M13;
            copy.M14 = _original.M14;

            copy.M21 = _original.M21;
            copy.M22 = _original.M22;
            copy.M23 = _original.M23;
            copy.M24 = _original.M24;

            copy.M31 = _original.M31;
            copy.M32 = _original.M32;
            copy.M33 = _original.M33;
            copy.M34 = _original.M34;

            copy.OffsetX = _original.OffsetX;
            copy.OffsetY = _original.OffsetY;
            copy.OffsetZ = _original.OffsetZ;
            copy.M44 = _original.M44;

            return copy;
        }

        #endregion
    }
}
