using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Input;

using ParameterStructure.Values;
using ParameterStructure.Component;
using ParameterStructure.DXF;

namespace ParameterStructure.Parameter
{
    #region ENUMS

    // [1001 - 1100]
    public enum ParameterSaveCode
    {  
        NAME = 1001,
        UNIT = 1002,
        CATEGORY = 1003,
        PROPAGATION = 1004,

        VALUE_MIN = 1005,
        VALUE_MAX = 1006,
        VALUE_CURRENT = 1007,
        IS_WITHIN_BOUNDS = 1008,

        // VALUE_FIELD is saved in a separate file
        VALUE_FIELD_REF = 1009,
        // VALUE_FIELD_POINTER -> see MultiValueSaveCode

        VALUE_TEXT = 1010
    }

    public enum ComparisonResult
    {        
        UNIQUE = 0,
        SAMENAME_DIFFUNIT = 1,      // for calculations: same output
        SAMENAMEUNIT_DIFFVALUE = 2, // for calculations: same output and input
        SAME = 3
    }

    #endregion

    #region HELPER CLASSES

    public class ParameterDummy : INotifyPropertyChanged
    {
        #region STATIC

        public static readonly ParameterDummy DEFAULT;
        static ParameterDummy()
        {
            DEFAULT = new ParameterDummy()
            {
                Name = string.Empty,
                Unit = string.Empty,
                Category = Category.NoNe,
                Propagation = InfoFlow.MIXED,
                ValueMin = 0.0,
                ValueMax = 100.00,
                ValueCurrent = 0.00,
                ValueField = null,
                MValPointer = MultiValPointer.INVALID,
                TextValue = string.Empty
            };
        }
        #endregion

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

        #region PROPETIES

        private string name;
        public string Name 
        {
            get { return this.name; }
            set
            {
                this.name = value;
                this.RegisterPropertyChanged("Name");
            }
        }

        private string unit;
        public string Unit 
        {
            get { return this.unit; }
            set
            {
                this.unit = value;
                this.RegisterPropertyChanged("Unit");
            }
        }

        private Category cat;
        public Category Category 
        {
            get { return this.cat; } 
            set
            {
                this.cat = value;
                this.RegisterPropertyChanged("Category");
            }
        }

        private InfoFlow propagation;
        public InfoFlow Propagation 
        {
            get { return this.propagation; } 
            set
            {
                this.propagation = value;
                this.RegisterPropertyChanged("Propagation");
            }
        }

        private double value_min;
        public double ValueMin 
        {
            get { return this.value_min; }
            set
            {
                this.value_min = value;
                this.RegisterPropertyChanged("ValueMin");
            }
        }

        private double value_max;
        public double ValueMax 
        {
            get { return this.value_max; } 
            set
            {
                this.value_max = value;
                this.RegisterPropertyChanged("ValueMax");
            }
        }

        private double value_current;
        public double ValueCurrent 
        {
            get { return this.value_current; }
            set
            {
                this.value_current = value;
                if (this.ValueMin <= this.value_current && this.value_current <= this.ValueMax)
                    this.IsWithinLimits = true;
                else
                    this.IsWithinLimits = false;
                this.RegisterPropertyChanged("IsWithinLimits");
                this.RegisterPropertyChanged("ValueCurrent");
            }
        }
        public bool IsWithinLimits { get; private set; }

        private MultiValue value_field;
        public MultiValue ValueField 
        {
            get { return this.value_field; } 
            set
            {
                this.value_field = value;
                this.RegisterPropertyChanged("ValueField");
            }
        }

        private MultiValPointer mval_pointer;
        public MultiValPointer MValPointer 
        {
            get { return this.mval_pointer; } 
            set
            {
                this.mval_pointer = value;
                this.RegisterPropertyChanged("MValPointer");
            }
        }

        private string text_value;
        public string TextValue 
        {
            get { return this.text_value; } 
            set
            {
                this.text_value = value;
                this.RegisterPropertyChanged("TextValue");
            }
        }

        #endregion

        #region METHODS

        public void RevertToDefault()
        {
            this.Name = string.Empty;
            this.Unit = string.Empty;
            this.Category = Category.NoNe;
            this.Propagation = InfoFlow.MIXED;
            this.ValueMin = 0.0;
            this.ValueMax = 100.00;
            this.ValueCurrent = 0.00;
            this.ValueField = null;
            this.MValPointer = MultiValPointer.INVALID;
            this.TextValue = string.Empty;
        }

        #endregion
    }

    public class ParameterReservedNameRecord
    {
        public string Name { get; private set; }
        public string Definition { get; private set; }
        public Color PColor { get; private set; }

        public System.Windows.Visibility PNamevisible { get; private set; }

        public ParameterReservedNameRecord(string _name, string _definition, Color _color, bool _visible = true)
        {
            this.Name = _name;
            this.Definition = _definition;
            this.PColor = _color;
            this.PNamevisible = (_visible) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }
    }

    #endregion

    #region BASE DISPLAY CLASS

    public abstract class ComparableContent : INotifyPropertyChanged
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

        #region PROPERTIES: COMPARISON 

        private ComparisonResult cr;
        public ComparisonResult CompResult
        {
            get { return this.cr; }
            protected set 
            { 
                this.cr = value;
                this.RegisterPropertyChanged("CompResult");
            }
        }

        private bool cr_done;
        public bool CompResult_Done
        {
            get { return this.cr_done; }
            protected set
            { 
                this.cr_done = value;
                this.RegisterPropertyChanged("CompResult_Done");
            }
        }

        public ComparableContent Buddy { get; protected set; }

        private bool is_HL_by_buddy;
        public bool IsHLByBuddy
        {
            get { return this.is_HL_by_buddy; }
            set 
            { 
                this.is_HL_by_buddy = value;
                this.RegisterPropertyChanged("IsHLByBuddy");
            }
        }
        

        #endregion

        public ComparableContent()
        {
            this.RemoveComparisonMarking();
        }

        #region METHODS: Comparison

        public virtual void CompareWith<T>(List<T> _cc) where T : ComparableContent
        { }

        public virtual void AdoptPropertiesOfBuddy()
        { }

        public void RemoveComparisonMarking()
        {
            this.CompResult = ComparisonResult.UNIQUE;
            this.CompResult_Done = false;
            this.Buddy = null;
            this.IsHLByBuddy = false;
        }

        #endregion
    }

    #endregion

    #region PARAMETER
    public class Parameter : ComparableContent
    {
        #region STATIC

        #region GENERAL CONSTANTS

        public const string INFINITY = "\U0000221E";
        public const string NAN = "NaN";
        public static readonly double VALUE_TOLERANCE = 0.0001;

        #endregion

        #region RESERVED PARAMTER NAMES

        internal const string POINTER_X = "pointerX";
        internal const string POINTER_Y = "pointerY";
        internal const string POINTER_Z = "pointerZ";
        internal const string POINTER_STRING = "pointerS";

        internal const string POINTER_X_NAME_TAG = "xX";
        internal const string POINTER_Y_NAME_TAG = "yY";
        internal const string POINTER_Z_NAME_TAG = "zZ";
        internal const string POINTER_STRING_NAME_TAG = "sS";

        public const string RP_COST_POSITION = "Kᴇʜ";
        public const string RP_COST_NET = "Kɴᴇ";
        public const string RP_COST_TOTAL = "Kᴃʀ";

        public const string RP_COUNT = "NRᴛᴏᴛᴀʟ";
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

        public const string RP_LENGTH_MIN_TOTAL = "LᴍɪNᴛᴏᴛᴀʟ";
        public const string RP_AREA_MIN_TOTAL = "AᴍɪNᴛᴏᴛᴀʟ";
        public const string RP_VOLUME_MIN_TOTAL = "VᴍɪNᴛᴏᴛᴀʟ";
        public const string RP_LENGTH_MAX_TOTAL = "LᴍᴀXᴛᴏᴛᴀʟ";
        public const string RP_AREA_MAX_TOTAL = "AᴍᴀXᴛᴏᴛᴀʟ";
        public const string RP_VOLUME_MAX_TOTAL = "VᴍᴀXᴛᴏᴛᴀʟ";

        public const string RP_AREA_IN = "A1";
        public const string RP_AREA_OUT_MAIN = "A2";
        public const string RP_AREA_OUT_BRANCH = "A3";
        public const string RP_AREA_SUGGESTION = "Ax";
        public const string RP_FLOW = "V̇";
        public const string RP_SPEED = "w";
        public const string RP_SPEED_IN = "w1";
        public const string RP_SPEED_OUT_MAIN = "w2";
        public const string RP_SPEED_OUT_BRANCH = "w3";
        public const string RP_PRESS_IN = "ΔPin";
        public const string RP_PRESS_IN_MAIN = "ΔPin2";
        public const string RP_PRESS_IN_BRANCH = "ΔPin3";
        public const string RP_PRESS_OUT = "ΔPout";
        public const string RP_PRESS_OUT_MAIN = "ΔPout2";
        public const string RP_PRESS_OUT_BRANCH = "ΔPout3";
        public const string RP_RES_CORRECTION = "Zc";

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

        public static readonly Color RP_COLOR_GEOM = (Color)ColorConverter.ConvertFromString("#ff360063");
        public static readonly Color RP_COLOR_HVAC = (Color)ColorConverter.ConvertFromString("#ff570056");
        public static readonly Color RP_COLOR_BPH = (Color)ColorConverter.ConvertFromString("#ff21009a");
        public static readonly Color RP_COLOR_ARC = (Color)ColorConverter.ConvertFromString("#ff0000bd");

        public static readonly List<ParameterReservedNameRecord> RESERVED_NAMES = new List<ParameterReservedNameRecord>
        {
            // allgemein
            new ParameterReservedNameRecord(null, "-ALLGEMEIN-", Colors.Black, false),
            new ParameterReservedNameRecord("(Name)" + Parameter.POINTER_X_NAME_TAG, "Zeiger auf die X-Achse des Kennfeldes verknüpft mit dem Parameter \"Name\"", Colors.Black),
            new ParameterReservedNameRecord("(Name)" + Parameter.POINTER_Y_NAME_TAG, "Zeiger auf die Y-Achse des Kennfeldes verknüpft mit dem Parameter \"Name\"", Colors.Black),
            new ParameterReservedNameRecord("(Name)" + Parameter.POINTER_Z_NAME_TAG, "Zeiger auf die Registerkartennummer des Kennfeldes verknüpft mit dem Parameter \"Name\"", Colors.Black),
            new ParameterReservedNameRecord("(Name)" + Parameter.POINTER_STRING_NAME_TAG, "Zeiger auf benannte Elemente des Kennfeldes verknüpft mit dem Parameter \"Name\"", Colors.Black),
            new ParameterReservedNameRecord(Parameter.RP_COUNT, "Anzahl", Colors.Black),
            // KOSTEN
            new ParameterReservedNameRecord(null, "-KOSTEN-", Colors.Black, false),
            new ParameterReservedNameRecord(Parameter.RP_COST_POSITION, "Einheitskosten", Colors.Black),
            new ParameterReservedNameRecord(Parameter.RP_COST_NET, "Nettogesamtkosten", Colors.Black),
            new ParameterReservedNameRecord(Parameter.RP_COST_TOTAL, "Bruttogesamtkosten", Colors.Black),
            // GEOMETRIE
            new ParameterReservedNameRecord(null, "-GEOMETRIE-", Parameter.RP_COLOR_GEOM, false),            
            new ParameterReservedNameRecord(Parameter.RP_AREA, "Fläche", Parameter.RP_COLOR_GEOM),
            new ParameterReservedNameRecord(Parameter.RP_AREA_MAX, "Fläche Max", Parameter.RP_COLOR_GEOM),
            new ParameterReservedNameRecord(Parameter.RP_AREA_MIN, "Fläche Min", Parameter.RP_COLOR_GEOM),
            new ParameterReservedNameRecord(Parameter.RP_WIDTH, "Breite", Parameter.RP_COLOR_GEOM),
            new ParameterReservedNameRecord(Parameter.RP_WIDTH_MAX, "Breite Außenmaß", Parameter.RP_COLOR_GEOM),
            new ParameterReservedNameRecord(Parameter.RP_WIDTH_MIN, "Breite Innenmaß", Parameter.RP_COLOR_GEOM),
            new ParameterReservedNameRecord(Parameter.RP_HEIGHT, "Höhe", Parameter.RP_COLOR_GEOM),
            new ParameterReservedNameRecord(Parameter.RP_HEIGHT_MAX, "Höhe Außenmaß", Parameter.RP_COLOR_GEOM),
            new ParameterReservedNameRecord(Parameter.RP_HEIGHT_MIN, "Höhe Innenmaß", Parameter.RP_COLOR_GEOM),
            new ParameterReservedNameRecord(Parameter.RP_DIAMETER, "Diameter", Parameter.RP_COLOR_GEOM),
            new ParameterReservedNameRecord(Parameter.RP_DIAMETER_MAX, "Diameter Außenmaß", Parameter.RP_COLOR_GEOM),
            new ParameterReservedNameRecord(Parameter.RP_DIAMETER_MIN, "Diameter Innenmaß", Parameter.RP_COLOR_GEOM),
            new ParameterReservedNameRecord(Parameter.RP_LENGTH, "Länge", Parameter.RP_COLOR_GEOM),
            new ParameterReservedNameRecord(Parameter.RP_LENGTH_MAX, "Länge Max", Parameter.RP_COLOR_GEOM),
            new ParameterReservedNameRecord(Parameter.RP_LENGTH_MIN, "Länge Min", Parameter.RP_COLOR_GEOM),
            new ParameterReservedNameRecord(Parameter.RP_LENGTH_MIN_TOTAL, "Länge Gesamt Brutto", Parameter.RP_COLOR_GEOM),
            new ParameterReservedNameRecord(Parameter.RP_AREA_MIN_TOTAL, "Fläche Gesamt Brutto", Parameter.RP_COLOR_GEOM),
            new ParameterReservedNameRecord(Parameter.RP_VOLUME_MIN_TOTAL, "Volumen Gesamt Brutto", Parameter.RP_COLOR_GEOM),
            new ParameterReservedNameRecord(Parameter.RP_LENGTH_MAX_TOTAL, "Länge Gesamt Netto", Parameter.RP_COLOR_GEOM),
            new ParameterReservedNameRecord(Parameter.RP_AREA_MAX_TOTAL, "Fläche Gesamt Netto", Parameter.RP_COLOR_GEOM),
            new ParameterReservedNameRecord(Parameter.RP_VOLUME_MAX_TOTAL, "Volumen Gesamt Netto", Parameter.RP_COLOR_GEOM),

            new ParameterReservedNameRecord(null, "-----------", Parameter.RP_COLOR_ARC, false),
            new ParameterReservedNameRecord(Parameter.RP_K_FOK, "Kote Fußboden Oberkante", Parameter.RP_COLOR_ARC),
            new ParameterReservedNameRecord(Parameter.RP_K_FOK_ROH, "Kote Fußboden Oberkante roh", Parameter.RP_COLOR_ARC),
            new ParameterReservedNameRecord(Parameter.RP_K_F_AXES, "Kote Fußboden Bezugsebene", Parameter.RP_COLOR_ARC),
            new ParameterReservedNameRecord(Parameter.RP_K_DUK, "Kote Decke Unterkante", Parameter.RP_COLOR_ARC),
            new ParameterReservedNameRecord(Parameter.RP_K_DUK_ROH, "Kote Decke Unterkante roh", Parameter.RP_COLOR_ARC),
            new ParameterReservedNameRecord(Parameter.RP_K_D_AXES, "Kote Decke Bezugsebene", Parameter.RP_COLOR_ARC),
            new ParameterReservedNameRecord(Parameter.RP_H_NET, "Lichte Höhe", Parameter.RP_COLOR_ARC),
            new ParameterReservedNameRecord(Parameter.RP_H_GROSS, "Brutto-Höhe", Parameter.RP_COLOR_ARC),
            new ParameterReservedNameRecord(Parameter.RP_H_AXES, "Höhe zw. Bezugsebenen", Parameter.RP_COLOR_ARC),
            new ParameterReservedNameRecord(Parameter.RP_L_PERIMETER, "Perimeter", Parameter.RP_COLOR_ARC),
            new ParameterReservedNameRecord(Parameter.RP_AREA_BGF, "Fläche BGF", Parameter.RP_COLOR_ARC),
            new ParameterReservedNameRecord(Parameter.RP_AREA_NGF, "Fläche NGF", Parameter.RP_COLOR_ARC),
            new ParameterReservedNameRecord(Parameter.RP_AREA_NF, "Fläche NF", Parameter.RP_COLOR_ARC),
            new ParameterReservedNameRecord(Parameter.RP_AREA_AXES, "Fläche Bezugsebene", Parameter.RP_COLOR_ARC),
            new ParameterReservedNameRecord(Parameter.RP_VOLUME_BRI, "Volumen BRI", Parameter.RP_COLOR_ARC),
            new ParameterReservedNameRecord(Parameter.RP_VOLUME_NRI, "Volumen NRI", Parameter.RP_COLOR_ARC),
            new ParameterReservedNameRecord(Parameter.RP_VOLUME_NRI_NF, "Volumen NRI NF", Parameter.RP_COLOR_ARC),
            new ParameterReservedNameRecord(Parameter.RP_VOLUME_AXES, "Volumen Bezugsvolumen", Parameter.RP_COLOR_ARC),
            // BPH
            new ParameterReservedNameRecord(null, "-BAUPHYSIK-", Parameter.RP_COLOR_BPH, false),
            new ParameterReservedNameRecord(Parameter.RP_MATERIAL_COMPOSITE_D_OUT, "Abstand zw. Bezugsebene und Außenfläche", Parameter.RP_COLOR_BPH),
            new ParameterReservedNameRecord(Parameter.RP_MATERIAL_COMPOSITE_D_IN, "Abstand zw. Bezugsebene und Innenfläche", Parameter.RP_COLOR_BPH),
            // GT
            new ParameterReservedNameRecord(null, "-GEBÄUDETECHNIK-", Parameter.RP_COLOR_HVAC, false),            
            new ParameterReservedNameRecord(Parameter.RP_AREA_IN, "Fläche Ein", Parameter.RP_COLOR_HVAC),
            new ParameterReservedNameRecord(Parameter.RP_AREA_OUT_MAIN, "Fläche Aus Hauptleitung", Parameter.RP_COLOR_HVAC),
            new ParameterReservedNameRecord(Parameter.RP_AREA_OUT_BRANCH, "Fläche Aus Abzweigleitung", Parameter.RP_COLOR_HVAC),
            new ParameterReservedNameRecord(Parameter.RP_AREA_SUGGESTION, "Fläche als Vorschlag", Parameter.RP_COLOR_HVAC),
            new ParameterReservedNameRecord(Parameter.RP_FLOW, "Volumenstrom", Parameter.RP_COLOR_HVAC),
            new ParameterReservedNameRecord(Parameter.RP_SPEED, "Geschwindigkeit", Parameter.RP_COLOR_HVAC),
            new ParameterReservedNameRecord(Parameter.RP_SPEED_IN, "Geschwindigkeit Ein", Parameter.RP_COLOR_HVAC),
            new ParameterReservedNameRecord(Parameter.RP_SPEED_OUT_MAIN, "Geschwindigkeit Aus Hauptleitung", Parameter.RP_COLOR_HVAC),
            new ParameterReservedNameRecord(Parameter.RP_SPEED_OUT_BRANCH, "Geschwindigkeit Aus Abzweigleitung", Parameter.RP_COLOR_HVAC),            
            new ParameterReservedNameRecord(Parameter.RP_PRESS_IN, "Druckdifferenz Ein", Parameter.RP_COLOR_HVAC),
            new ParameterReservedNameRecord(Parameter.RP_PRESS_IN_MAIN, "Druckdifferenz Ein Hauptleitung", Parameter.RP_COLOR_HVAC),
            new ParameterReservedNameRecord(Parameter.RP_PRESS_IN_BRANCH, "Druckdifferenz Ein Abzweigleitung", Parameter.RP_COLOR_HVAC),
            new ParameterReservedNameRecord(Parameter.RP_PRESS_OUT, "Druckdifferenz Aus", Parameter.RP_COLOR_HVAC),
            new ParameterReservedNameRecord(Parameter.RP_PRESS_OUT_MAIN, "Druckdifferenz Aus Hauptleitung", Parameter.RP_COLOR_HVAC),
            new ParameterReservedNameRecord(Parameter.RP_PRESS_OUT_BRANCH, "Druckdifferenz Aus Abzweigleitung", Parameter.RP_COLOR_HVAC),
            new ParameterReservedNameRecord(Parameter.RP_RES_CORRECTION, "Widerstandskorrektur Vorschlag", Parameter.RP_COLOR_HVAC),
        };

        public static string GetReservedUnits(string _reserved_name)
        {
            if (string.IsNullOrEmpty(_reserved_name)) return "-";
            switch(_reserved_name)
            {
                case RP_COST_POSITION:
                case RP_COST_NET:
                case RP_COST_TOTAL:
                    return "€";
                case RP_WIDTH:
                case RP_HEIGHT:
                case RP_LENGTH:
                case RP_K_FOK:
                case RP_K_FOK_ROH:
                case RP_K_F_AXES:
                case RP_K_DUK:
                case RP_K_DUK_ROH:
                case RP_K_D_AXES:
                case RP_H_NET:
                case RP_H_GROSS:
                case RP_H_AXES:
                case RP_L_PERIMETER:
                    return "m";
                case RP_DIAMETER:
                    return "mm";
                case RP_AREA:
                case RP_AREA_BGF:
                case RP_AREA_NGF:
                case RP_AREA_NF:
                case RP_AREA_AXES:
                    return "m²";
                case RP_VOLUME_BRI:
                case RP_VOLUME_NRI:
                case RP_VOLUME_NRI_NF:
                case RP_VOLUME_AXES:
                    return "m³";
                default:
                    return "-";
            }
        }

        public static Category GetReservedCategoryFlag(string _reserved_name)
        {
            if (string.IsNullOrEmpty(_reserved_name)) return Category.NoNe;
            return Category.Geometry;
        }

        public static List<Parameter> GetSizeParametersForInstancing()
        {
            List<Parameter> parameters = new List<Parameter>();

            // size for instances
            Parameter p01 = new Parameter(Parameter.RP_WIDTH_MIN, "m", 0.0, 0.0, double.MaxValue);
            p01.TextValue = "min. Breite";
            p01.Category |= Category.Geometry;
            p01.Propagation = InfoFlow.INPUT;
            parameters.Add(p01);

            Parameter p02 = new Parameter(Parameter.RP_HEIGHT_MIN, "m", 0.0, 0.0, double.MaxValue);
            p02.TextValue = "min. Höhe";
            p02.Category |= Category.Geometry;
            p02.Propagation = InfoFlow.INPUT;
            parameters.Add(p02);

            Parameter p03 = new Parameter(Parameter.RP_LENGTH_MIN, "m", 0.0, 0.0, double.MaxValue);
            p03.TextValue = "min. Länge";
            p03.Category |= Category.Geometry;
            p03.Propagation = InfoFlow.INPUT;
            parameters.Add(p03);

            Parameter p04 = new Parameter(Parameter.RP_WIDTH_MAX, "m", 0.0, 0.0, double.MaxValue);
            p04.TextValue = "max. Breite";
            p04.Category |= Category.Geometry;
            p04.Propagation = InfoFlow.INPUT;
            parameters.Add(p04);

            Parameter p05 = new Parameter(Parameter.RP_HEIGHT_MAX, "m", 0.0, 0.0, double.MaxValue);
            p05.TextValue = "max. Höhe";
            p05.Category |= Category.Geometry;
            p05.Propagation = InfoFlow.INPUT;
            parameters.Add(p05);

            Parameter p06 = new Parameter(Parameter.RP_LENGTH_MAX, "m", 0.0, 0.0, double.MaxValue);
            p06.TextValue = "max. Länge";
            p06.Category |= Category.Geometry;
            p06.Propagation = InfoFlow.INPUT;
            parameters.Add(p06);

            Parameter p07 = new Parameter(Parameter.RP_AREA_MIN, "m²", 0.0, 0.0, double.MaxValue);
            p07.TextValue = "Fläche Netto";
            p07.Category |= Category.Geometry;
            p07.Propagation = InfoFlow.MIXED;
            parameters.Add(p07);

            Parameter p08 = new Parameter(Parameter.RP_AREA_MAX, "m²", 0.0, 0.0, double.MaxValue);
            p08.TextValue = "Fläche Brutto";
            p08.Category |= Category.Geometry;
            p08.Propagation = InfoFlow.MIXED;
            parameters.Add(p08);

            Parameter p09 = new Parameter(Parameter.RP_VOLUME_NRI, "m³", 0.0, 0.0, double.MaxValue);
            p09.TextValue = "Volumen Netto";
            p09.Category |= Category.Geometry;
            p09.Propagation = InfoFlow.MIXED;
            parameters.Add(p09);

            Parameter p10 = new Parameter(Parameter.RP_VOLUME_BRI, "m³", 0.0, 0.0, double.MaxValue);
            p10.TextValue = "Volumen Brutto";
            p10.Category |= Category.Geometry;
            p10.Propagation = InfoFlow.MIXED;
            parameters.Add(p10);

            return parameters;
        }

        public static List<Parameter> GetCumulativeParametersForInstancing()
        {
            List<Parameter> parameters = new List<Parameter>();

            // cumulative values over all instances
            Parameter p11 = new Parameter(Parameter.RP_LENGTH_MIN_TOTAL, "m", 0.0, 0.0, double.MaxValue);
            p11.TextValue = "Gesamtlänge Netto";
            p11.Category |= Category.Geometry | Category.Communication;
            p11.Propagation = InfoFlow.CALC_IN;
            parameters.Add(p11);

            Parameter p12 = new Parameter(Parameter.RP_AREA_MIN_TOTAL, "m²", 0.0, 0.0, double.MaxValue);
            p12.TextValue = "Gesamtfläche Netto";
            p12.Category |= Category.Geometry | Category.Communication;
            p12.Propagation = InfoFlow.CALC_IN;
            parameters.Add(p12);

            Parameter p13 = new Parameter(Parameter.RP_VOLUME_MIN_TOTAL, "m³", 0.0, 0.0, double.MaxValue);
            p13.TextValue = "Gesamtvolumen Netto";
            p13.Category |= Category.Geometry | Category.Communication;
            p13.Propagation = InfoFlow.CALC_IN;
            parameters.Add(p13);

            Parameter p14 = new Parameter(Parameter.RP_LENGTH_MAX_TOTAL, "m", 0.0, 0.0, double.MaxValue);
            p14.TextValue = "Gesamtlänge Brutto";
            p14.Category |= Category.Geometry | Category.Communication;
            p14.Propagation = InfoFlow.CALC_IN;
            parameters.Add(p14);

            Parameter p15 = new Parameter(Parameter.RP_AREA_MAX_TOTAL, "m²", 0.0, 0.0, double.MaxValue);
            p15.TextValue = "Gesamtfläche Brutto";
            p15.Category |= Category.Geometry | Category.Communication;
            p15.Propagation = InfoFlow.CALC_IN;
            parameters.Add(p15);

            Parameter p16 = new Parameter(Parameter.RP_VOLUME_MAX_TOTAL, "m³", 0.0, 0.0, double.MaxValue);
            p16.TextValue = "Gesamtvolument Brutto";
            p16.Category |= Category.Geometry | Category.Communication;
            p16.Propagation = InfoFlow.CALC_IN;
            parameters.Add(p16);

            Parameter p17 = new Parameter(Parameter.RP_COUNT, "-", 0.0, 0.0, 10000);
            p17.TextValue = "Gesamtanzahl Instanzen";
            p17.Category |= Category.Geometry | Category.Communication;
            p17.Propagation = InfoFlow.CALC_IN;
            parameters.Add(p17);

            return parameters;
        }

        #endregion

        public static System.IFormatProvider NR_FORMATTER = new System.Globalization.NumberFormatInfo();
        internal static long NR_PARAMS;

        static Parameter()
        {
            NR_PARAMS = 0;
        }

        #region COMMUNICATION W DUMMY
        // updates only things that NEED updating
        // to minimize the instances of recorded writing access
        public static void CopyDataFromDummy(ref Parameter _p, ParameterDummy _pd)
        {
            if (_p != null && _pd!= null)
            {
                //_p.TypeName = _pd.TypeName;
                if (_p.Unit != _pd.Unit)
                    _p.Unit = _pd.Unit;

                if (_p.Category != _pd.Category)
                    _p.Category = _pd.Category;

                if (_p.Propagation != _pd.Propagation)
                    _p.Propagation = _pd.Propagation;


                if (Math.Abs(_p.ValueMin - _pd.ValueMin) > Parameter.VALUE_TOLERANCE)
                    _p.ValueMin = _pd.ValueMin;

                if (Math.Abs(_p.ValueMax - _pd.ValueMax) > Parameter.VALUE_TOLERANCE)
                    _p.ValueMax = _pd.ValueMax;                    

                if (_p.Propagation != InfoFlow.REF_IN && 
                    (Math.Abs(_p.ValueCurrent - _pd.ValueCurrent) > Parameter.VALUE_TOLERANCE || double.IsNaN(_p.ValueCurrent)))
                    _p.ValueCurrent = _pd.ValueCurrent;


                if (_p.ValueField != _pd.ValueField && _p.Propagation != InfoFlow.OUPUT)
                    _p.ValueField = _pd.ValueField;

                if (_p.ValueField != null && _pd.MValPointer != MultiValPointer.INVALID) // added 26.10.2016
                {
                    if (!(MultiValPointer.AreEqual(_p.MValPointer, _pd.MValPointer))) // changed 19.08.2016
                        _p.MValPointer = new MultiValPointer(_pd.MValPointer);
                }

                if (_pd.ValueField == null && _p.Propagation != InfoFlow.REF_IN && Math.Abs(_p.ValueCurrent - _pd.ValueCurrent) > Parameter.VALUE_TOLERANCE)
                    _p.ValueCurrent = _pd.ValueCurrent;

                if (_p.Propagation != InfoFlow.REF_IN && _p.TextValue != _pd.TextValue)
                    _p.TextValue = _pd.TextValue;

                // last, in case a synchronitzation btw this and a reference is necessary
                // referenced parameters >> dummy values (user values)
                if (_p.Name != _pd.Name)
                    _p.Name = _pd.Name; 
            }
        }

        public static ParameterDummy CopyDataToDummy(Parameter _p)
        {
            if (_p != null)
            {
                ParameterDummy pd = new ParameterDummy()
                {
                    Name = _p.Name,
                    Unit = _p.Unit,
                    Category = _p.Category,
                    Propagation = _p.Propagation,

                    ValueMin = _p.ValueMin,
                    ValueMax = _p.ValueMax,
                    ValueCurrent = _p.ValueCurrent,

                    ValueField = _p.ValueField,
                    MValPointer = _p.MValPointer,

                    TextValue = _p.TextValue
                };
                return pd;
            }
            return null;
        }
        #endregion

        #region COMPARISON
        public static Dictionary<string, Parameter> ParamListToDict(List<Parameter> _ps)
        {
            Dictionary<string, Parameter> result = new Dictionary<string, Parameter>();
            if (_ps == null) return result;
            if (_ps.Count < 1) return result;

            foreach(Parameter p in _ps)
            {
                result.Add(p.Name, p);
            }

            return result;
        }

        internal static int CompareByName(Parameter _p1, Parameter _p2)
        {
            if (_p1 == null && _p2 == null) return 0;
            if (_p1 != null && _p2 == null) return 1;
            if (_p1 == null && _p2 != null) return -1;

            string p_name_1 = _p1.Name;
            string p_name_2 = _p2.Name;

            if (string.IsNullOrEmpty(p_name_1) && string.IsNullOrEmpty(p_name_2)) return 0;
            if (!string.IsNullOrEmpty(p_name_1) && string.IsNullOrEmpty(p_name_2)) return 1;
            if (string.IsNullOrEmpty(p_name_1) && !string.IsNullOrEmpty(p_name_2)) return -1;

            return p_name_1.CompareTo(p_name_2);
        }

        #endregion

        #region VALUE HANDLING
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
                return Parameter.NAN;
            else if (double.IsPositiveInfinity(_value) || _value == double.MaxValue)
                return "+" + Parameter.INFINITY;
            else if (double.IsNegativeInfinity(_value) || _value == double.MinValue)
                return "-" + Parameter.INFINITY;
            else
                return _value.ToString(number_format_string, Parameter.NR_FORMATTER);
        }

        public static double StringToDouble(string _input)
        {
            if (_input == null) return 0.0;
            switch(_input)
            {
                case Parameter.NAN:
                    return double.NaN;
                case "+" + Parameter.INFINITY:
                    return double.MaxValue;
                case "-" + Parameter.INFINITY:
                    return double.MinValue;
                default:
                    double f;
                    bool success = Double.TryParse(_input, NumberStyles.Float, Parameter.NR_FORMATTER, out f);
                    if (success)
                        return f;
                    else
                        return 0.0;
            }
        }
        #endregion

        #region PARSING, MERGING

        internal static void AdjustInstanceIds(ref List<Parameter> _instances)
        {
            if (_instances == null || _instances.Count == 0)
                return;

            // existing: 0 1 2 3 -> Parameter.NR_PARAMS = 4
            // new: 6 7 12 14
            List<long> all_ids = _instances.Select(x => x.ID).ToList();
            if (all_ids.Min() > Parameter.NR_PARAMS - 1)
                return;

            // existing: 0 1 2 3 -> Parameter.NR_PARAMS = 4
            // new: 2 3 6 7
            // shift all ids by an offset (4 - 2) = 2 -> 2 3 6 7 --> 4 5 8 9
            long offset = Parameter.NR_PARAMS - all_ids.Min();
            Parameter.NR_PARAMS = all_ids.Max() + offset + 1; // 10

            foreach (Parameter p in _instances)
            {
                p.ID += offset;
            }
        }

        #endregion

        #endregion

        #region .CTOR

        internal Parameter()
            : this("Parameter", "unit", 0.0, 0.0, 100.0)
        {
            this.Name = "Parameter_" + this.ID.ToString();
        }

        internal Parameter(string _name, string _unit, double _value)
            :this(_name, _unit, _value, 0.0, 100.0)
        { }

        protected Parameter(string _name, string _unit, double _value, double _value_min, double _value_max)
        {
            this.ID = (++Parameter.NR_PARAMS);
            this.Name = _name;
            this.Unit = _unit;
            this.Category = Component.Category.NoNe;
            this.Propagation = InfoFlow.MIXED;
          
            this.ValueMin = Math.Min(_value_min, _value_max);
            this.ValueMax = Math.Max(_value_min, _value_max);
            this.ValueCurrent = _value;
            this.TextValue = string.Empty;

            this.ValueField = null;

            this.IsSelected = false;
            this.OwnerIsSelected = false;
            this.IsExpanded = false;

            this.ShowInCompInstDisplay = true;
            this.ToggleShowInCompInstDisplay = new Utils.PS_RelayCommand((x) => this.ShowInCompInstDisplay = !this.ShowInCompInstDisplay);
        }

        #endregion

        #region.CTOR for Parsing

        internal Parameter(long _id, string _name, string _unit, Category _category, InfoFlow _propagation,
                           double _value_min, double _value_max, double _value_current, bool _is_within_bounds,
                           string _value_text, long _value_field_ref, MultiValPointer _value_field_pointer)
        {
            this.id = _id;
            this.name = _name;
            this.unit = _unit;
            this.category = _category;
            this.propagation = _propagation;

            this.value_min = _value_min;
            this.value_max = _value_max;
            this.value_current = _value_current;
            this.is_within_bounds = _is_within_bounds;

            this.value_field_ref = _value_field_ref;
            this.mval_pointer = _value_field_pointer;
            // the value field will be set later (DO NOT FORGET TO OVERRIDE THE VALUE FIELD POINTER !!!)
            // the time stamp will also be set later

            this.text_value = _value_text;

            this.IsSelected = false;
            this.IsExpanded = false;
            this.OwnerIsSelected = false;

            this.ShowInCompInstDisplay = true;
            this.ToggleShowInCompInstDisplay = new Utils.PS_RelayCommand((x) => this.ShowInCompInstDisplay = !this.ShowInCompInstDisplay);
        }


        #endregion

        #region .CTOR: COPYING

        protected Parameter(Parameter _original)
        {            
            this.ID = (++Parameter.NR_PARAMS);
            this.Name = _original.Name;
            this.Unit = _original.Unit;
            this.Category = _original.Category;
            this.Propagation = _original.Propagation;

            this.ValueMin = _original.ValueMin;
            this.ValueMax = _original.ValueMax;
            this.ValueCurrent = _original.ValueCurrent;
            this.TextValue = _original.TextValue;

            this.ValueField = _original.ValueField; // DO NOT copy, just pass the REFERENCE!
            this.MValPointer = new MultiValPointer(_original.MValPointer); // DO copy the pointer
            // if the field is empty (i.e. NULL), copy the current value
            if (this.ValueField == null)
                this.ValueCurrent = _original.ValueCurrent;

            this.IsSelected = false;
            this.IsExpanded = false;
            this.OwnerIsSelected = false;

            this.ShowInCompInstDisplay = true;
            this.ToggleShowInCompInstDisplay = new Utils.PS_RelayCommand((x) => this.ShowInCompInstDisplay = !this.ShowInCompInstDisplay);
        }

        internal virtual Parameter Clone()
        {
            return new Parameter(this);
        }

        #endregion

        #region PROPERTIES: General (ID, TypeName, Unit, Category, Propagation)

        private long id;
        public long ID
        {
            get { return this.id; }
            private set 
            { 
                this.id = value;
                this.RegisterPropertyChanged("ID");
            }
        }

        protected string name;
        public string Name
        {
            get { return this.name; }
            set 
            { 
                this.name = value;
                this.RegisterPropertyChanged("Name");
            }
        }

        protected string unit;
        public string Unit
        {
            get { return this.unit; }
            set
            {
                this.unit = value;
                this.RegisterPropertyChanged("Unit");
            }
        }

        protected Category category;
        public Category Category
        {
            get { return this.category; }
            set
            {
                this.category = value;
                this.RegisterPropertyChanged("Category");
            }
        }

        private InfoFlow propagation;
        public InfoFlow Propagation
        {
            get { return this.propagation; }
            set
            {
                this.propagation = value;
                this.RegisterPropertyChanged("Propagation");
            }
        }

        #endregion

        #region PROPERTIES: Value Management

        protected double value_max;
        public double ValueMax
        {
            get { return this.value_max; }
            set 
            { 
                this.value_max = value;
                this.DetermineIfWithinBounds();
                this.RegisterPropertyChanged("ValueMax");
            }
        }

        protected double value_min;
        public double ValueMin
        {
            get { return this.value_min; }
            set 
            { 
                this.value_min = value;
                this.DetermineIfWithinBounds();
                this.RegisterPropertyChanged("ValueMin");
            }
        }

        private bool is_within_bounds;
        public bool IsWithinBounds
        {
            get { return this.is_within_bounds; }
            protected set 
            { 
                this.is_within_bounds = value;
                this.RegisterPropertyChanged("IsWithinBounds");
            }
        }

        private double value_current;
        public double ValueCurrent
        {
            get { return this.value_current; }
            set 
            { 
                if (this.value_current != value)
                    this.TimeStamp = DateTime.Now;

                this.value_current = value;
                this.DetermineIfWithinBounds();               
                this.RegisterPropertyChanged("ValueCurrent");
            }
        }       

        protected void DetermineIfWithinBounds()
        {
            if (this.ValueMin <= this.value_current && this.value_current <= this.ValueMax)
                this.IsWithinBounds = true;
            else
                this.IsWithinBounds = false;
        }

        #endregion

        #region PROPERTIES: Value Field Management

        private MultiValue value_field;
        public MultiValue ValueField
        {
            get { return this.value_field; }
            set 
            {
                this.value_field = value;
                if (this.value_field != null)
                {
                    this.ValueFieldRef = this.value_field.MVID;
                    this.MValPointer = new MultiValPointer(this.value_field.MVDisplayVector);
                }
                else
                {
                    this.ValueFieldRef = -1;
                    this.MValPointer = MultiValPointer.INVALID;
                }

                this.RegisterPropertyChanged("ValueField");
            }
        }

        private long value_field_ref;
        public long ValueFieldRef
        {
            get { return this.value_field_ref; }
            protected set 
            { 
                this.value_field_ref = value;
                this.RegisterPropertyChanged("ValueFieldRef");
            }
        }

        private MultiValPointer mval_pointer;
        public MultiValPointer MValPointer
        {
            get { return this.mval_pointer; }
            set 
            { 
                this.mval_pointer = value;
                if (this.mval_pointer != null && this.mval_pointer != MultiValPointer.INVALID)
                    this.ValueCurrent = this.mval_pointer.Value;
                this.RegisterPropertyChanged("MValPointer");
            }
        }

        #endregion

        #region PROPERTIES: Time Stamp

        private DateTime time_stamp;
        public DateTime TimeStamp
        {
            get { return this.time_stamp; }
            internal set 
            {
                this.time_stamp = value;
                this.RegisterPropertyChanged("TimeStamp");
            }
        }

        #endregion

        #region PROPERTIES: String Value

        private string text_value;
        public string TextValue
        {
            get { return this.text_value; }
            set 
            {

                if (this.text_value != value)
                    this.TimeStamp = DateTime.Now;

                this.text_value = value;

                this.RegisterPropertyChanged("TextValue");
            }
        }

        #endregion

        #region PROPERTIES for Display (IsSelected, IsExpanded, OwnerIsSelected, ShowInCompInstDisplay)

        private bool isExpanded;
        public bool IsExpanded
        {
            get { return this.isExpanded; }
            set
            {
                this.isExpanded = value;
                this.RegisterPropertyChanged("IsExpanded");
            }
        }

        private bool isSelected;
        public bool IsSelected
        {
            get { return this.isSelected; }
            set
            {
                this.isSelected = value;
                this.RegisterPropertyChanged("IsSelected");
            }
        }

        private bool owner_is_selected;
        public bool OwnerIsSelected
        {
            get { return this.owner_is_selected; }
            set
            {
                this.owner_is_selected = value;
                this.RegisterPropertyChanged("OwnerIsSelected");
            }
        }

        private bool show_in_comp_inst_display;
        public bool ShowInCompInstDisplay
        {
            get { return this.show_in_comp_inst_display; }
            set 
            { 
                this.show_in_comp_inst_display = value;
                this.RegisterPropertyChanged("ShowInCompInstDisplay");
            }
        }
        public ICommand ToggleShowInCompInstDisplay { get; protected set; }

        #endregion

        #region METHODS : To and From String

        public override string ToString()
        {
            string output = this.ID + ": " + this.Name + " [" + this.Unit + "]: " + this.ValueCurrent.ToString("F2", Parameter.NR_FORMATTER);

            output += " in [" + ((this.ValueMin == double.MinValue) ? "-" + INFINITY : this.ValueMin.ToString("F2", Parameter.NR_FORMATTER)) + ", " +
                                   ((this.ValueMax == double.MaxValue) ? "+" + INFINITY : this.ValueMax.ToString("F2", Parameter.NR_FORMATTER)) + "] ";
            output += "[" + ComponentUtils.CategoryToString(this.Category) + "] ";
            output += "[" + ComponentUtils.InfoFlowToString(this.Propagation) + "]\n";
            output += (this.ValueField == null) ? "I" + this.ValueFieldRef + " F{ } " : "I" + this.ValueFieldRef + " F{" + this.ValueField.ToString() + "}\n";
            output += (this.MValPointer == null) ? "P{ } " : "P{" + this.MValPointer.ToString() + "}";

            return output;
        }

        public string ToShortString()
        {
            return this.Name + " = " + this.ValueCurrent.ToString("F2", Parameter.NR_FORMATTER);
        }

        public string ToInfoString()
        {
            return "{" + this.ID + "}" + this.Name + " " + this.ValueCurrent.ToString("F2", Parameter.NR_FORMATTER);
        }

        public string ToLongString()
        {
            return "{" + this.ID + "}" + this.Name + " = " + this.ValueCurrent.ToString("F2", Parameter.NR_FORMATTER) + " | " + this.TextValue;
        }

        public virtual void AddToExport(ref StringBuilder _sb)
        {
            if (_sb == null) return;
            string tmp = null;

            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.PARAMETER);                               // PARAMETER

            _sb.AppendLine(((int)ParamStructCommonSaveCode.CLASS_NAME).ToString());
            _sb.AppendLine(this.GetType().ToString());

            // general
            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_ID).ToString());
            _sb.AppendLine(this.ID.ToString());

            _sb.AppendLine(((int)ParameterSaveCode.NAME).ToString());
            _sb.AppendLine(this.Name);

            _sb.AppendLine(((int)ParameterSaveCode.UNIT).ToString());
            _sb.AppendLine(this.Unit);

            _sb.AppendLine(((int)ParameterSaveCode.CATEGORY).ToString());
            _sb.AppendLine(ComponentUtils.CategoryToString(this.Category));

            _sb.AppendLine(((int)ParameterSaveCode.PROPAGATION).ToString());
            _sb.AppendLine(ComponentUtils.InfoFlowToString(this.Propagation));

            // value management (changed 26.10.2016)
            _sb.AppendLine(((int)ParameterSaveCode.VALUE_MIN).ToString());
            _sb.AppendLine(Parameter.ValueToString(this.ValueMin, "F8"));

            _sb.AppendLine(((int)ParameterSaveCode.VALUE_MAX).ToString());
            _sb.AppendLine(Parameter.ValueToString(this.ValueMax, "F8"));

            _sb.AppendLine(((int)ParameterSaveCode.VALUE_CURRENT).ToString());
            _sb.AppendLine(Parameter.ValueToString(this.ValueCurrent, "F8"));

            _sb.AppendLine(((int)ParameterSaveCode.IS_WITHIN_BOUNDS).ToString());
            tmp = (this.IsWithinBounds) ? "1" : "0";
            _sb.AppendLine(tmp);

            // timestamp
            _sb.AppendLine(((int)ParamStructCommonSaveCode.TIME_STAMP).ToString());
            _sb.AppendLine(this.TimeStamp.ToString(ParamStructTypes.DT_FORMATTER));

            // text value
            _sb.AppendLine(((int)ParameterSaveCode.VALUE_TEXT).ToString());
            _sb.AppendLine(this.TextValue);

            // value field management
            _sb.AppendLine(((int)ParameterSaveCode.VALUE_FIELD_REF).ToString());
            _sb.AppendLine(this.ValueFieldRef.ToString());

            _sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_NUMDIM).ToString());
            _sb.AppendLine(this.MValPointer.NrDim.ToString());

            _sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_CELL_INDEX_X).ToString());
            tmp = (this.MValPointer.NrDim > 0) ? this.MValPointer.CellIndices[0].ToString() : "0";
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_CELL_INDEX_Y).ToString());
            tmp = (this.MValPointer.NrDim > 1) ? this.MValPointer.CellIndices[1].ToString() : "0";
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_CELL_INDEX_Z).ToString());
            tmp = (this.MValPointer.NrDim > 2) ? this.MValPointer.CellIndices[2].ToString() : "0";
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_CELL_INDEX_W).ToString());
            tmp = (this.MValPointer.NrDim > 3) ? this.MValPointer.CellIndices[3].ToString() : "0";
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_CELL_SIZE_W).ToString());
            tmp = double.IsNaN(this.MValPointer.CellSize.X) ? Parameter.NAN : this.MValPointer.CellSize.X.ToString("F8", Parameter.NR_FORMATTER);
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_CELL_SIZE_H).ToString());
            tmp = double.IsNaN(this.MValPointer.CellSize.Y) ? Parameter.NAN : this.MValPointer.CellSize.Y.ToString("F8", Parameter.NR_FORMATTER);
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_POS_IN_CELL_REL_X).ToString());
            tmp = double.IsNaN(this.MValPointer.PosInCell_Relative.X) ? Parameter.NAN : this.MValPointer.PosInCell_Relative.X.ToString("F8", Parameter.NR_FORMATTER);
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_POS_IN_CELL_REL_Y).ToString());
            tmp = double.IsNaN(this.MValPointer.PosInCell_Relative.Y) ? Parameter.NAN : this.MValPointer.PosInCell_Relative.Y.ToString("F8", Parameter.NR_FORMATTER);
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_POS_IN_CELL_REL_Z).ToString());
            _sb.AppendLine("0");

            _sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_POS_IN_CELL_ABS_X).ToString());
            tmp = double.IsNaN(this.MValPointer.PosInCell_AbsolutePx.X) ? Parameter.NAN : this.MValPointer.PosInCell_AbsolutePx.X.ToString("F8", Parameter.NR_FORMATTER);
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_POS_IN_CELL_ABS_Y).ToString());
            tmp = double.IsNaN(this.MValPointer.PosInCell_AbsolutePx.Y) ? Parameter.NAN : this.MValPointer.PosInCell_AbsolutePx.Y.ToString("F8", Parameter.NR_FORMATTER);
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_POS_IN_CELL_ABS_Z).ToString());
            _sb.AppendLine("0");

            _sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_VALUE).ToString());
            tmp = double.IsNaN(this.MValPointer.Value) ? Parameter.NAN : this.MValPointer.Value.ToString("F8", Parameter.NR_FORMATTER);
            _sb.AppendLine(tmp);
        }

        #endregion

        #region METHODS: Comparison

        public override void CompareWith<T>(List<T> _cc)
        {
            if (this.CompResult_Done) return;
            if (_cc == null) return;

            ComparisonResult tmp = this.CompResult;
            foreach(ComparableContent entry in _cc)
            {
                if (!(entry is Parameter))
                    continue;
                if (entry.CompResult_Done)
                    continue;

                Parameter p = entry as Parameter;
                ComparisonResult tmp_p = p.CompResult;

                if (tmp == ComparisonResult.UNIQUE)
                {
                    if (this.Name == p.Name)
                    {
                        tmp = ComparisonResult.SAMENAME_DIFFUNIT;
                        this.Buddy = p;
                        if (tmp_p <= ComparisonResult.SAMENAME_DIFFUNIT)
                        {
                            tmp_p = ComparisonResult.SAMENAME_DIFFUNIT;
                            p.Buddy = this;
                        }
                    }
                }
                if (tmp == ComparisonResult.SAMENAME_DIFFUNIT)
                {
                    if (this.Name == p.Name && this.Unit == p.Unit)
                    {
                        tmp = ComparisonResult.SAMENAMEUNIT_DIFFVALUE;
                        this.Buddy = p;
                        if (tmp_p <= ComparisonResult.SAMENAMEUNIT_DIFFVALUE)
                        {
                            tmp_p = ComparisonResult.SAMENAMEUNIT_DIFFVALUE;
                            p.Buddy = this;
                        }
                    }
                }
                if (tmp == ComparisonResult.SAMENAMEUNIT_DIFFVALUE)
                {
                    if (this.Name == p.Name && this.Unit == p.Unit && 
                        this.Propagation == p.Propagation && this.Category == p.Category && 
                        this.ValueCurrent == p.ValueCurrent && this.TextValue == p.TextValue)
                    {
                        this.CompResult = ComparisonResult.SAME;
                        this.CompResult_Done = true;
                        this.Buddy = p;
                        if (tmp_p <= ComparisonResult.SAME)
                        {
                            p.CompResult = ComparisonResult.SAME;
                            p.CompResult_Done = true;
                            p.Buddy = this;
                        }
                        return;
                    }
                }
                p.CompResult = tmp_p;
            }

            this.CompResult = tmp;
            this.CompResult_Done = true;
        }

        public override void AdoptPropertiesOfBuddy()
        {
            if (this.Buddy == null) return;
            Parameter p = this.Buddy as Parameter;
            if (p == null) return;

            this.Name = p.Name;
            this.Unit = p.Unit;
            this.Propagation = p.Propagation;
            this.Category = p.Category;
            this.ValueCurrent = p.ValueCurrent;
            this.TextValue = p.TextValue;
        }

        #endregion

        #region EVENT HANDLERS: Value Field Change

        // currently not in use
        protected void value_field_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            MultiValue mv = sender as MultiValue;
            if (mv == null || e == null) return;

            if (e.PropertyName == "MVDisplayVector")
            {
                this.ValueCurrent = mv.MVDisplayVector.Value;
            }
        }

        #endregion
    }

    #endregion
}
