using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace ParameterStructure.DXF
{
    // ALL SAVE CODES
    // [0    - 1000]: DXF Specs and general custom codes
    // [1001 - 1100]: Parameter
    // [1101 - 1300]: MultiValue
    // [1301 - 1400]: Calculation
    // [1401 - 1500]: Component
    //      [[1421 - 1430]]: Component -> AccessTracker
    //      [[1431 - 1440]]: Component -> AccessProfile
    // [1501 - 1600]: FlowNetwork
    // [1601 - 1700]: GeometricRelationship

    // general codes for all types
    // the more specific codes are saved with their respective types (i.e. Parameter)
    public enum ParamStructCommonSaveCode
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

    // entity names (to be placed after code ENTITY_START)
    public static class ParamStructTypes
    {
        public const string FLOWNETWORK = "FLOWNETWORK";        // custom
        public const string FLOWNETWORK_NODE = "FLOWNETWORK_NODE";// custom
        public const string FLOWNETWORK_EDGE = "FLOWNETWORK_EDGE";// custom
        public const string COMPONENT = "COMPONENT";            // custom
        public const string CALCULATION = "CALCULATION";        // custom
        public const string PARAMETER = "PARAMETER";            // custom
        public const string VALUE_FIELD = "VALUE_FIELD";        // custom
        public const string FUNCTION_FIELD = "FUNCTION_FIELD";  // custom
        public const string BIG_TABLE = "BIG_TABLE";            // custom
        public const string GEOM_RELATION = "GEOM_RELATIONSHIP";// custom
        public const string MAPPING_TO_COMP = "MAPPING2COMPONENT"; // custom

        public const string ACCESS_TRACKER = "ACCESS_TRACKER";  // custom helper
        public const string ACCESS_PROFILE = "ACCESS_PROFILE";  // custom helper

        public const string ENTITY_SECTION = "ENTITIES";        // DXF specs
        public const string NETWORK_SECTION = "NETWORKS";       // custom
        public const string SECTION_START = "SECTION";          // DXF specs
        public const string SECTION_END = "ENDSEC";             // DXF specs
        public const string SEQUENCE_END = "SEQEND";            // DXF specs
        public const string EOF = "EOF";                        // DXF specs

        public const string ENTITY_SEQUENCE = "ENTSEQ";         // custom
        public const string ENTITY_CONTINUE = "ENTCTN";         // custom

        // public const string DOUBLE_NAN = "NaN";                 // custom
        public const int NOT_END_OF_LIST = 1;                   // custom
        public const int END_OF_LIST = -1;                      // custom
        public const int END_OF_SUBLIST = -11;                  // custom

        public static readonly DateTimeFormatInfo DT_FORMATTER = new DateTimeFormatInfo();
    }

    public static class ParamStructFileExtensions
    {
        public const string FILE_EXT_COMPONENTS = "codxf";
        public const string FILE_EXT_PARAMETERS = "padxf";
        public const string FILE_EXT_MULTIVALUES = "mvdxf";
        public const string FILE_EXT_PROJECT = "smn";
    }
}
