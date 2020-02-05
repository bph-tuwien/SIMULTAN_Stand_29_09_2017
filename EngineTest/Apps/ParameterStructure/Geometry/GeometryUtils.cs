using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParameterStructure.Geometry
{
    public enum Relation2GeomType
    {
        NONE = 0,                   // components not dependent on geometry (DEFAULT VALUE IN .NET)
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

        public static Relation2GeomState Next(Relation2GeomState _prev_state)
        {
            if (_prev_state.IsRealized)
                // no change
                return new Relation2GeomState { IsRealized = true, Type = _prev_state.Type };
            else
            {
                switch(_prev_state.Type)
                {
                    case Relation2GeomType.NONE:
                        return new Relation2GeomState { IsRealized = false, Type = Relation2GeomType.DESCRIBES };
                    case Relation2GeomType.DESCRIBES:
                        return new Relation2GeomState { IsRealized = false, Type = Relation2GeomType.ALIGNED_WITH };
                    case Relation2GeomType.ALIGNED_WITH:
                        return new Relation2GeomState { IsRealized = false, Type = Relation2GeomType.CONTAINED_IN };
                    case Relation2GeomType.CONTAINED_IN:
                        return new Relation2GeomState { IsRealized = false, Type = Relation2GeomType.CONNECTS };
                    case Relation2GeomType.CONNECTS:
                        return new Relation2GeomState { IsRealized = false, Type = Relation2GeomType.GROUPS };
                    case Relation2GeomType.DESCRIBES_3D:
                    case Relation2GeomType.DESCRIBES_2DorLESS:
                    case Relation2GeomType.GROUPS:
                        return new Relation2GeomState { IsRealized = false, Type = Relation2GeomType.NONE };
                    default:
                        return new Relation2GeomState { IsRealized = false, Type = Relation2GeomType.NONE };
                }
            }
        }
    }

    public static class GeometryUtils
    {
        #region Relation2GeomType

        public static Relation2GeomType StringToRelationship2Geometry(string _input)
        {
            if (string.IsNullOrEmpty(_input)) return Relation2GeomType.NONE;

            switch(_input)
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
            switch(_rel)
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

        #endregion
    }
}
