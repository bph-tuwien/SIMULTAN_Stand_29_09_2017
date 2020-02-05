using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using System.ComponentModel;

using ParameterStructure.Utils;
using ParameterStructure.DXF;
using ParameterStructure.Geometry;

namespace ParameterStructure.Component
{
    #region ENUMS
    [Flags]
    public enum Category
    {
        NoNe = 0,
        General = 1,                // 0  Aa
        Geometry = 2,               // 1  Bb
        Costs = 4,                  // 2  Cc
        Regulations = 8,            // 3  Dd
        Heating = 16,               // 4  Ee
        Cooling = 32,               // 5  Ff
        Humidity = 64,              // 6  Gg
        Air = 128,                  // 7  Hh
        Acoustics = 256,            // 8  Ii
        Light_Natural = 512,        // 9  Jj
        Light_Artificial = 1024,    // 10 Kk
        Water = 2048,               // 11 Ll
        Waste = 4096,               // 12 Mm
        Electricity = 8192,         // 13 Nn
        FireSafety = 16384,         // 14 Oo
        MSR = 32768,                // 15 Pp
        Communication = 65536       // 16 Qq
    }

    public enum InfoFlow
    {
        INPUT = 0,      // pure input !
        OUPUT = 1,      // pure output ?
        MIXED = 2,      // can be both input and output @
        REF_IN = 3,     // takes input from a referenced component "
        CALC_IN = 4     // takes input from a network calculation and transfer &
    }

    public enum ComponentManagerType
    {
        ADMINISTRATOR = 0,              // white        @
        MODERATOR = 1,                  // black        A
        ENERGY_NETWORK_OPERATOR = 2,    // dark orange  B
        EENERGY_SUPPLIER = 3,           // orange       C
        BUILDING_DEVELOPER = 4,         // dark green   D
        BUILDING_OPERATOR = 5,          // green        E
        ARCHITECTURE = 6,               // light blue   F
        FIRE_SAFETY = 7,                // dark red     G
        BUILDING_PHYSICS = 8,           // blue         H
        MEP_HVAC = 9,                   // dark blue    I
        PROCESS_MEASURING_CONTROL = 10, // darker blue  J
        BUILDING_CONTRACTOR = 11,       // grey         K
        GUEST               = 12,       // white        0
    }

    [Flags]
    public enum ComponentAccessType
    {
        NO_ACCESS = 0,      // external user
        READ = 1,           // project member
        WRITE = 2,          // assiged planer
        SUPERVIZE = 4,      // supervizing planer
        RELEASE = 8         // building developer / another planer
    }

    public enum ComponentValidity
    {
        NOT_CALCULATED = 0,             // grey
        WRITE_AFTER_SUPERVIZE = 1,      // dark red
        WRITE_AFTER_RELEASE = 2,        // light red
        SUPERVIZE_AFTER_RELEASE = 3,    // yellow-orange
        VALID = 4                       // blue
    }

    #endregion

    #region HELPER CLASSES

    #region ComponentAccessTracker

    public enum ComponentAccessTrackerSaveCode
    {
        FLAGS = 1421,
        WRITE_PREV = 1422,
        WRITE_LAST = 1423,
        SUPERVIZE_PREV = 1424,
        SUPERVIZE_LAST = 1425,
        RELEASE_PREV = 1426,
        RELEASE_LAST = 1427
    }
    public class ComponentAccessTracker
    {
        #region PROPERTIES
        public ComponentAccessType AccessTypeFlags { get; set; }

        private DateTime prev_access_write = DateTime.MinValue;
        private DateTime last_access_write = DateTime.MinValue;
        public DateTime LastAccess_Write 
        {
            get { return this.last_access_write; } 
            set
            {
                if (this.AccessTypeFlags.HasFlag(ComponentAccessType.WRITE))
                {
                    this.prev_access_write = this.last_access_write;
                    this.last_access_write = value;
                }
            }
        }

        private DateTime prev_access_supervize = DateTime.MinValue;
        private DateTime last_access_supervize = DateTime.MinValue;
        public DateTime LastAccess_Supervize 
        {
            get { return this.last_access_supervize; }
            set
            {
                if (this.AccessTypeFlags.HasFlag(ComponentAccessType.SUPERVIZE))
                {
                    this.prev_access_supervize = this.last_access_supervize;
                    this.last_access_supervize = value;
                }
            }
        }

        public void UndoLastSupervize()
        {
            if (this.AccessTypeFlags.HasFlag(ComponentAccessType.SUPERVIZE))
                this.last_access_supervize = this.prev_access_supervize;
        }

        private DateTime prev_access_release = DateTime.MinValue;
        private DateTime last_access_release = DateTime.MinValue;
        public DateTime LastAccess_Release 
        {
            get { return this.last_access_release; }
            set
            {
                if (this.AccessTypeFlags.HasFlag(ComponentAccessType.RELEASE))
                {
                    this.prev_access_release = this.last_access_release;
                    this.last_access_release = value;
                }
            }
        }

        public void UndoLastRelease()
        {
            if (this.AccessTypeFlags.HasFlag(ComponentAccessType.RELEASE))
                this.last_access_release = this.prev_access_release;
        }

        #endregion

        #region .CTOR

        public ComponentAccessTracker()
        { }

        public ComponentAccessTracker(ComponentAccessTracker _original)
        {
            this.AccessTypeFlags = _original.AccessTypeFlags;

            this.prev_access_write = _original.prev_access_write;
            this.last_access_write = _original.last_access_write;

            this.prev_access_supervize = _original.prev_access_supervize;
            this.last_access_supervize = _original.last_access_supervize;

            this.prev_access_release = _original.prev_access_release;
            this.last_access_release = _original.last_access_release;
        }

        // use only for parsing
        internal ComponentAccessTracker(ComponentAccessType _flags,
                                        DateTime _prev_write, DateTime _last_write,
                                        DateTime _prev_supervize, DateTime _last_supervize,
                                        DateTime _prev_release, DateTime _last_release)
        {
            this.AccessTypeFlags = _flags;

            this.prev_access_write = _prev_write;
            this.last_access_write = _last_write;

            this.prev_access_supervize = _prev_supervize;
            this.last_access_supervize = _last_supervize;

            this.prev_access_release = _prev_release;
            this.last_access_release = _last_release;
        }

        #endregion

        #region METHODS
        public DateTime GetTimeStamp(int _index)
        {
            switch(_index)
            {
                case 1:
                    return this.last_access_write;
                case 2:
                    return this.last_access_supervize;
                case 3:
                    return this.last_access_release;
                default:
                    return DateTime.MinValue;
            }
        }

        public DateTime GetPrevTimeStamp(int _index)
        {
            switch (_index)
            {
                case 1:
                    return this.prev_access_write;
                case 2:
                    return this.prev_access_supervize;
                case 3:
                    return this.prev_access_release;
                default:
                    return DateTime.MinValue;
            }
        }

        public void SetTimeStamp(int _index, DateTime _dt)
        {
            switch (_index)
            {
                case 1:
                    this.last_access_write = _dt;
                    break;
                case 2:
                    this.last_access_supervize = _dt;
                    break;
                case 3:
                    this.last_access_release = _dt;
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region TO STRING

        public void AddToExport(ref StringBuilder _sb, string _key = null)
        {
            if (_sb == null) return;

            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.ACCESS_TRACKER);                          // ACCESS_TRACKER

            if (!(string.IsNullOrEmpty(_key)))
            {
                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_KEY).ToString());
                _sb.AppendLine(_key);
            }

            _sb.AppendLine(((int)ComponentAccessTrackerSaveCode.FLAGS).ToString());
            _sb.AppendLine(ComponentUtils.ComponentAccessTypeToString(this.AccessTypeFlags));

            _sb.AppendLine(((int)ComponentAccessTrackerSaveCode.WRITE_PREV).ToString());
            _sb.AppendLine(this.prev_access_write.ToString(ParamStructTypes.DT_FORMATTER));

            _sb.AppendLine(((int)ComponentAccessTrackerSaveCode.WRITE_LAST).ToString());
            _sb.AppendLine(this.last_access_write.ToString(ParamStructTypes.DT_FORMATTER));

            _sb.AppendLine(((int)ComponentAccessTrackerSaveCode.SUPERVIZE_PREV).ToString());
            _sb.AppendLine(this.prev_access_supervize.ToString(ParamStructTypes.DT_FORMATTER));

            _sb.AppendLine(((int)ComponentAccessTrackerSaveCode.SUPERVIZE_LAST).ToString());
            _sb.AppendLine(this.last_access_supervize.ToString(ParamStructTypes.DT_FORMATTER));

            _sb.AppendLine(((int)ComponentAccessTrackerSaveCode.RELEASE_PREV).ToString());
            _sb.AppendLine(this.prev_access_release.ToString(ParamStructTypes.DT_FORMATTER));

            _sb.AppendLine(((int)ComponentAccessTrackerSaveCode.RELEASE_LAST).ToString());
            _sb.AppendLine(this.last_access_release.ToString(ParamStructTypes.DT_FORMATTER));
        }

        #endregion

    }
    #endregion

    #region ComponentManagerAndAccessFlagDateTimeTriple

    public struct ComponentManagerAndAccessFlagDateTimeTriple
    {
        public ComponentManagerType ManagerType { get; set; }
        public int AccessFlagIndex { get; set; }
        public DateTime AccessTimeStamp_Current { get; set; }
        public DateTime AccessTimeStamp_Prev { get; set; }
    }

    #endregion

    #region ComponentAccessProfile

    public enum ComponentAccessProfileSaveCode
    {
        STATE = 1431,
        PROFILE = 1432,
        MANAGER = 1433
    }

    public class ComponentAccessProfile : INotifyPropertyChanged
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

        #region PROPERTIES

        private Dictionary<ComponentManagerType, ComponentAccessTracker> profile;
        public ComponentAccessTracker this[ComponentManagerType t]
        {
            get { return this.profile[t]; }
            set
            {
                this.profile[t] = value;
                this.AdjustWritingAccessAfterUserInput(t);
                this.ProfileState = ComponentUtils.ComponentAccessProfileToValidity(this.profile);
                this.RegisterPropertyChanged("ProfileState");
            }
        }

        public ComponentValidity ProfileState { get; private set; }

        #endregion

        #region .CTOR
        public ComponentAccessProfile(IDictionary<ComponentManagerType, ComponentAccessTracker> _input, ComponentManagerType _caller)
        {
            if (_input == null)
            {
                this.profile = ComponentUtils.GetStandardProfile(ComponentUtils.COMP_SLOT_OBJECT);
            }
            else
            {
                this.profile = new Dictionary<ComponentManagerType, ComponentAccessTracker>();
                foreach(var entry in _input)
                {
                    if (this.profile.ContainsKey(entry.Key)) continue;
                    this.profile.Add(entry.Key, entry.Value);
                }
            }

            // make sure that the administrator and the current caller have both reading and writing access 
            ComponentAccessTracker caller_tracker = this.profile[_caller];
            caller_tracker.AccessTypeFlags |= ComponentAccessType.READ;
            caller_tracker.AccessTypeFlags |= ComponentAccessType.WRITE;
            this.profile[_caller] = caller_tracker;
            
            ComponentAccessTracker admin_tracker = this.profile[ComponentManagerType.ADMINISTRATOR];
            admin_tracker.AccessTypeFlags |= ComponentAccessType.READ;
            admin_tracker.AccessTypeFlags |= ComponentAccessType.WRITE;
            this.profile[ComponentManagerType.ADMINISTRATOR] = admin_tracker;

            this.AdjustWritingAccessAfterUserInput(_caller);
        }

        public ComponentAccessProfile(ComponentAccessProfile _original)
        {
            this.profile = new Dictionary<ComponentManagerType, ComponentAccessTracker>();
            foreach(var entry in _original.profile)
            {
                this.profile.Add(entry.Key, new ComponentAccessTracker(entry.Value));
            }
            this.ProfileState = _original.ProfileState;
        }
        #endregion

        #region METHODS

        public void UpdateProfileState()
        {
            this.ProfileState = ComponentUtils.ComponentAccessProfileToValidity(this.profile);
            this.RegisterPropertyChanged("ProfileState");
        }

        #endregion

        #region METHODS: Make sure only two trackers have writing access

        private void AdjustWritingAccessAfterUserInput(ComponentManagerType _most_recent_change)
        {
            Dictionary<ComponentManagerType, ComponentAccessTracker> trackers_to_change = new Dictionary<ComponentManagerType, ComponentAccessTracker>();
            foreach(var entry in this.profile)
            {
                if (entry.Key == ComponentManagerType.ADMINISTRATOR)
                {
                    ComponentAccessTracker tracker = entry.Value;
                    tracker.AccessTypeFlags |= ComponentAccessType.WRITE;
                    trackers_to_change.Add(ComponentManagerType.ADMINISTRATOR, tracker);
                }
                else
                {
                    if (_most_recent_change != ComponentManagerType.ADMINISTRATOR && this.profile[_most_recent_change].AccessTypeFlags.HasFlag(ComponentAccessType.WRITE))
                    {
                        // make sure no one else can write
                        if (entry.Key != _most_recent_change && entry.Value.AccessTypeFlags.HasFlag(ComponentAccessType.WRITE))
                        {
                            ComponentAccessTracker tracker = entry.Value;
                            tracker.AccessTypeFlags &= ~ComponentAccessType.WRITE;
                            trackers_to_change.Add(entry.Key, tracker);
                        }
                    }
                }
            }

            // apply changes
            foreach(var entry in trackers_to_change)
            {
                this.profile[entry.Key] = entry.Value;
            }
        }

        #endregion

        #region TO STRING

        public void AddToExport(ref StringBuilder _sb, bool _is_last)
        {
            if (_sb == null) return;

            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.ACCESS_PROFILE);                          // ACCESS_PROFILE

            _sb.AppendLine(((int)ComponentAccessProfileSaveCode.STATE).ToString());
            _sb.AppendLine(ComponentUtils.ComponentValidityToString(this.ProfileState));

            _sb.AppendLine(((int)ComponentAccessProfileSaveCode.PROFILE).ToString());
            _sb.AppendLine(this.profile.Count.ToString());

            foreach(var entry in this.profile)
            {
                entry.Value.AddToExport(ref _sb, ComponentUtils.ComponentManagerTypeToLetter(entry.Key));
            }

            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.SEQUENCE_END);                            // SEQEND

            if(!_is_last)
            {
                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
                _sb.AppendLine(ParamStructTypes.ENTITY_CONTINUE);                         // ENTCTN
            }
        }

        #endregion

    }
    #endregion

    #region Empty Slot

    public class EmptyComponentSlot : INotifyPropertyChanged
    {
        #region STATIC

        internal static int CompareByName(EmptyComponentSlot _ecs1, EmptyComponentSlot _ecs2)
        {
            if (_ecs1 == null && _ecs2 == null) return 0;
            if (_ecs1 != null && _ecs2 == null) return 1;
            if (_ecs1 == null && _ecs2 != null) return -1;

            string slot_name_1 = _ecs1.SlotName;
            string slot_name_2 = _ecs2.SlotName;

            if (string.IsNullOrEmpty(slot_name_1) && string.IsNullOrEmpty(slot_name_2)) return 0;
            if (!string.IsNullOrEmpty(slot_name_1) && string.IsNullOrEmpty(slot_name_2)) return 1;
            if (string.IsNullOrEmpty(slot_name_1) && !string.IsNullOrEmpty(slot_name_2)) return -1;

            return slot_name_1.CompareTo(slot_name_2);
        }


        #endregion

        #region PROPERTIES: INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        private void RegisterPropertyChanged(string _propName)
        {
            if (_propName == null)
                return;

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(_propName));
        }

        #endregion 

        public int SlotType { get; set; } // 0 = subcomponent, 1 = referenced component
        public string SlotName { get; set; }

        private string slot_descr = string.Empty;
        public string SlotDescr
        {
            get { return this.slot_descr; }
            set 
            {
                this.slot_descr = value;
                this.RegisterPropertyChanged("SlotDescr");
            }
        }

        // properties for display
        public bool IsSelected { get; set; }

        private bool parent_is_selected;
        public bool ParentIsSelected 
        {
            get { return this.parent_is_selected; }
            set
            {
                this.parent_is_selected = value;
                this.RegisterPropertyChanged("ParentIsSelected");
            }
        }
        public bool IsExpanded { get; set; }
        public bool IsLarge { get; set; }
    }

    #endregion

    #endregion

    public static class ComponentUtils
    {
        #region CATEGORY

        public static readonly Regex LOWER_CASE_SINGLE = new Regex("^[a-z]{1}$");
        public static readonly Regex UPPER_CASE_SINGLE = new Regex("^[A-Z]{1}$");
        public const string CATEGORY_NONE_AS_STR = "bcdefghijklmnopq";
        public static string CategoryToString(Category _input)
        {
            string output = ComponentUtils.CATEGORY_NONE_AS_STR;

            if (_input.HasFlag(Category.Geometry))
                output = output.Replace('b', 'B');
            if (_input.HasFlag(Category.Costs))
                output = output.Replace('c', 'C');
            if (_input.HasFlag(Category.Regulations))
                output = output.Replace('d', 'D');
            if (_input.HasFlag(Category.Heating))
                output = output.Replace('e', 'E');
            if (_input.HasFlag(Category.Cooling))
                output = output.Replace('f', 'F');
            if (_input.HasFlag(Category.Humidity))
                output = output.Replace('g', 'G');
            if (_input.HasFlag(Category.Air))
                output = output.Replace('h', 'H');
            if (_input.HasFlag(Category.Acoustics))
                output = output.Replace('i', 'I');
            if (_input.HasFlag(Category.Light_Natural))
                output = output.Replace('j', 'J');
            if (_input.HasFlag(Category.Light_Artificial))
                output = output.Replace('k', 'K');
            if (_input.HasFlag(Category.Water))
                output = output.Replace('l', 'L');
            if (_input.HasFlag(Category.Waste))
                output = output.Replace('m', 'M');
            if (_input.HasFlag(Category.Electricity))
                output = output.Replace('n', 'N');
            if (_input.HasFlag(Category.FireSafety))
                output = output.Replace('o', 'O');
            if (_input.HasFlag(Category.MSR))
                output = output.Replace('p', 'P');
            if (_input.HasFlag(Category.Communication))
                output = output.Replace('q', 'Q');

            return output;
        }

        public static Category StringToCategory(string _input)
        {
            Category output = Category.NoNe;
            if (string.IsNullOrEmpty(_input)) return output;

            if (_input.Contains('B'))
                output |= Category.Geometry;
            if (_input.Contains('C'))
                output |= Category.Costs;
            if (_input.Contains('D'))
                output |= Category.Regulations;
            if (_input.Contains('E'))
                output |= Category.Heating;
            if (_input.Contains('F'))
                output |= Category.Cooling;
            if (_input.Contains('G'))
                output |= Category.Humidity;
            if (_input.Contains('H'))
                output |= Category.Air;
            if (_input.Contains('I'))
                output |= Category.Acoustics;
            if (_input.Contains('J'))
                output |= Category.Light_Natural;
            if (_input.Contains('K'))
                output |= Category.Light_Artificial;
            if (_input.Contains('L'))
                output |= Category.Water;
            if (_input.Contains('M'))
                output |= Category.Waste;
            if (_input.Contains('N'))
                output |= Category.Electricity;
            if (_input.Contains('O'))
                output |= Category.FireSafety;
            if (_input.Contains('P'))
                output |= Category.MSR;
            if (_input.Contains('Q'))
                output |= Category.Communication;

            return output;
        }

        public static string CategoryStringToDescription(string _letter)
        {
            if (string.IsNullOrEmpty(_letter)) return string.Empty;

            switch(_letter)
            {
                case "A":
                    return "Allgemein";
                case "B":
                    return "Geometrie";
                case "C":
                    return "Kosten";
                case "D":
                    return "Anforderungen";
                case "E":
                    return "Wärme";
                case "F":
                    return "Kälte";
                case "G":
                    return "Feuchtigkeit";
                case "H":
                    return "Luft";
                case "I":
                    return "Akustik";
                case "J":
                    return "Tageslicht";
                case "K":
                    return "Kunstlicht";
                case "L":
                    return "Wasser";
                case "M":
                    return "Abwasser";
                case "N":
                    return "Elektro";
                case "O":
                    return "Brandschutz";
                case "P":
                    return "MSR";
                case "Q":
                    return "Kommunikation";
                default:
                    return string.Empty;
            }
        }

        public static List<SelectableString> CATEGORIES_SELECTABLE = new List<SelectableString>
        {
            new SelectableString("A"),
            new SelectableString("B"),
            new SelectableString("C"),
            new SelectableString("D"),
            new SelectableString("E"),
            new SelectableString("F"),
            new SelectableString("G"),
            new SelectableString("H"),
            new SelectableString("I"),
            new SelectableString("J"),
            new SelectableString("K"),
            new SelectableString("L"),
            new SelectableString("M"),
            new SelectableString("N"),
            new SelectableString("O"),
            new SelectableString("P"),
            new SelectableString("Q")
        };

        #endregion

        #region InfoFlow

        public const string INPUT = "!";
        public const string OUPUT = "?";
        public const string MIXED = "@";
        public const string REF_IN = "\"";
        public const string CALC_IN = "&";

        public static string InfoFlowToString(InfoFlow _input)
        {
            switch(_input)
            {
                case InfoFlow.INPUT:
                    return ComponentUtils.INPUT;
                case InfoFlow.OUPUT:
                    return ComponentUtils.OUPUT;
                case InfoFlow.REF_IN:
                    return ComponentUtils.REF_IN;
                case InfoFlow.CALC_IN:
                    return ComponentUtils.CALC_IN;
                default:
                    return ComponentUtils.MIXED;
            }
        }

        public static InfoFlow StringToInfoFlow(string _input)
        {
            if (string.IsNullOrEmpty(_input)) return InfoFlow.MIXED;
            
            switch(_input)
            {
                case ComponentUtils.INPUT:
                    return InfoFlow.INPUT;
                case ComponentUtils.OUPUT:
                    return InfoFlow.OUPUT;
                case ComponentUtils.REF_IN:
                    return InfoFlow.REF_IN;
                case ComponentUtils.CALC_IN:
                    return InfoFlow.CALC_IN;
                default:
                    return InfoFlow.MIXED;
            }
        }

        public static string InfoFlowStringToDescription(string _letter)
        {
            if (string.IsNullOrEmpty(_letter)) return "In- und Output";

            switch (_letter)
            {
                case ComponentUtils.INPUT:
                    return "reiner Input";
                case ComponentUtils.OUPUT:
                    return "reiner Output";
                case ComponentUtils.REF_IN:
                    return "Input von Referenz";
                case ComponentUtils.CALC_IN:
                    return "Input aus Netzwerk";
                default:
                    return "In- und Output";
            }
        }

        #endregion

        #region COMPONENT MANAGEMENT

        public static string ComponentManagerTypeToLetter(ComponentManagerType _type)
        {
            switch(_type)
            {
                case ComponentManagerType.ADMINISTRATOR:
                    return "@";
                case ComponentManagerType.MODERATOR:
                    return "A";
                case ComponentManagerType.ENERGY_NETWORK_OPERATOR:
                    return "B";
                case ComponentManagerType.EENERGY_SUPPLIER:
                    return "C";
                case ComponentManagerType.BUILDING_DEVELOPER:
                    return "D";
                case ComponentManagerType.BUILDING_OPERATOR:
                    return "E";
                case ComponentManagerType.ARCHITECTURE:
                    return "F";
                case ComponentManagerType.FIRE_SAFETY:
                    return "G";
                case ComponentManagerType.BUILDING_PHYSICS:
                    return "H";
                case ComponentManagerType.MEP_HVAC:
                    return "I";
                case ComponentManagerType.PROCESS_MEASURING_CONTROL:
                    return "J";
                case ComponentManagerType.BUILDING_CONTRACTOR:
                    return "K";
                case ComponentManagerType.GUEST:
                    return "L";
                default:
                    return "L";
            }
        }


        public static ComponentManagerType StringToComponentManagerType(string _type)
        {
            if (_type == null) return ComponentManagerType.GUEST;

            switch(_type)
            {
                case "@":
                    return ComponentManagerType.ADMINISTRATOR;
                case "A":
                    return ComponentManagerType.MODERATOR;
                case "B":
                    return ComponentManagerType.ENERGY_NETWORK_OPERATOR;
                case "C":
                    return ComponentManagerType.EENERGY_SUPPLIER;
                case "D":
                    return ComponentManagerType.BUILDING_DEVELOPER;
                case "E":
                    return ComponentManagerType.BUILDING_OPERATOR;
                case "F":
                    return ComponentManagerType.ARCHITECTURE;
                case "G":
                    return ComponentManagerType.FIRE_SAFETY;
                case "H":
                    return ComponentManagerType.BUILDING_PHYSICS;
                case "I":
                    return ComponentManagerType.MEP_HVAC;
                case "J":
                    return ComponentManagerType.PROCESS_MEASURING_CONTROL;
                case "K":
                    return ComponentManagerType.BUILDING_CONTRACTOR;
                case "L":
                    return ComponentManagerType.GUEST;
                default:
                    return ComponentManagerType.GUEST;
            }
        }

        public static string ComponentManagerTypeToDescrDE(ComponentManagerType _type)
        {
            switch (_type)
            {
                case ComponentManagerType.ADMINISTRATOR:
                    return "Administrator";
                case ComponentManagerType.MODERATOR:
                    return "Moderator";
                case ComponentManagerType.ENERGY_NETWORK_OPERATOR:
                    return "Netzbetreiber";
                case ComponentManagerType.EENERGY_SUPPLIER:
                    return "Energieversorger";
                case ComponentManagerType.BUILDING_DEVELOPER:
                    return "Bauherr";
                case ComponentManagerType.BUILDING_OPERATOR:
                    return "Betreiber";
                case ComponentManagerType.ARCHITECTURE:
                    return "Architektur";
                case ComponentManagerType.FIRE_SAFETY:
                    return "Brandschutzplanung";
                case ComponentManagerType.BUILDING_PHYSICS:
                    return "Bauphysik";
                case ComponentManagerType.MEP_HVAC:
                    return "Geb\u00E4udetechnik";
                case ComponentManagerType.PROCESS_MEASURING_CONTROL:
                    return "MSR";
                case ComponentManagerType.BUILDING_CONTRACTOR:
                    return "Ausf\u00FChrende Firma";
                case ComponentManagerType.GUEST:
                    return "Gast";
                default:
                    return "Gast";
            }
        }

        public static string ComponentManagerTypeToAbbrevEN(ComponentManagerType _type)
        {
            switch (_type)
            {
                case ComponentManagerType.ADMINISTRATOR:
                    return "ADMIN";
                case ComponentManagerType.MODERATOR:
                    return "MOD";
                case ComponentManagerType.ENERGY_NETWORK_OPERATOR:
                    return "NETW";
                case ComponentManagerType.EENERGY_SUPPLIER:
                    return "EN";
                case ComponentManagerType.BUILDING_DEVELOPER:
                    return "BDEV";
                case ComponentManagerType.BUILDING_OPERATOR:
                    return "USER";
                case ComponentManagerType.ARCHITECTURE:
                    return "ARC";
                case ComponentManagerType.FIRE_SAFETY:
                    return "FS";
                case ComponentManagerType.BUILDING_PHYSICS:
                    return "BPH";
                case ComponentManagerType.MEP_HVAC:
                    return "MEP_HVAC";
                case ComponentManagerType.PROCESS_MEASURING_CONTROL:
                    return "MSR";
                case ComponentManagerType.BUILDING_CONTRACTOR:
                    return "BC";
                case ComponentManagerType.GUEST:
                    return "GUEST";
                default:
                    return "GUEST";
            }
        }

        public static List<SelectableString> MANAGER_TYPES_SELECTABLE = new List<SelectableString>
        {
            new SelectableString("@"),
            new SelectableString("A"),
            new SelectableString("B"),
            new SelectableString("C"),
            new SelectableString("D"),
            new SelectableString("E"),
            new SelectableString("F"),
            new SelectableString("G"),
            new SelectableString("H"),
            new SelectableString("I"),
            new SelectableString("J"),
            new SelectableString("K"),
            new SelectableString("L")
        };

        public static List<string> MANAGER_TYPES_STRING = new List<string>() { "@", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L" };

        public static List<int> MANAGER_TYPE_OPENING_SIGNATURE_NONE = Enumerable.Repeat(0, Enum.GetNames(typeof(ComponentManagerType)).Length).ToList();
        public static List<int> GetManagerTypeOpeningSignature(ComponentManagerType _user)
        {
            List<int> signature = new List<int>(ComponentUtils.MANAGER_TYPE_OPENING_SIGNATURE_NONE);
            signature[(int)_user] = 1;
            return signature;
        }

        #endregion

        #region COMPONENT ACCESS

        public static readonly Regex ACCESS_NOT_ALLOWED_SINGLE = new Regex("^[w-z]{1}$");
        public static readonly Regex ACCESS_ALLOWED_SINGLE = new Regex("^[W-Zs-v]{1}$");
        public static readonly Regex ACCESS_RECORDED_SINGLE = new Regex("^[S-V]{1}$");
        public static readonly Regex ACCESS_ALLOWED_READWRITE = new Regex("^[stWX]{1}$");
        public const string COMP_ACCESS_NONE = "wxyz";

        public static string ComponentAccessTypeToString(ComponentAccessType _input)
        {
            string output = ComponentUtils.COMP_ACCESS_NONE;

            if (_input.HasFlag(ComponentAccessType.READ))
                output = output.Replace('w', 'W');
            if (_input.HasFlag(ComponentAccessType.WRITE))
                output = output.Replace('x', 'X');
            if (_input.HasFlag(ComponentAccessType.SUPERVIZE))
                output = output.Replace('y', 'Y');
            if (_input.HasFlag(ComponentAccessType.RELEASE))
                output = output.Replace('z', 'Z');

            return output;
        }

        public static string ComponentAccessTypeInTrackerToString(ComponentAccessTracker _input)
        {
            if (_input == null) return string.Empty;

            string output = ComponentUtils.ComponentAccessTypeToString(_input.AccessTypeFlags);
            
            if (_input.LastAccess_Write > DateTime.MinValue)
                output = output.Replace('X', 'T');
            else
                output = output.Replace('X', 't');

            if (_input.LastAccess_Supervize > DateTime.MinValue)
                output = output.Replace('Y', 'U');
            else
                output = output.Replace('Y', 'u');

            if (_input.LastAccess_Release > DateTime.MinValue)
                output = output.Replace('Z', 'V');
            else
                output = output.Replace('Z', 'v');

            return output;
        }

        public static ComponentAccessType StringToComponentAccessType(string _input)
        {
            ComponentAccessType output = ComponentAccessType.NO_ACCESS;
            if (string.IsNullOrEmpty(_input)) return output;

            if (_input.Contains('W') || _input.Contains('s') || _input.Contains('S'))
                output |= ComponentAccessType.READ;
            if (_input.Contains('X') || _input.Contains('t') || _input.Contains('T'))
                output |= ComponentAccessType.WRITE;
            if (_input.Contains('Y') || _input.Contains('u') || _input.Contains('U'))
                output |= ComponentAccessType.SUPERVIZE;
            if (_input.Contains('Z') || _input.Contains('v') || _input.Contains('V'))
                output |= ComponentAccessType.RELEASE;

            return output;
        }

        public static string ComponentAccessTypeStringToDescriptionDE(string _letter, string _date = "")
        {
            if (string.IsNullOrEmpty(_letter)) return string.Empty;

            switch (_letter)
            {
                case "w":
                    return "KANN NICHT Lesen";
                case "W":
                case "s":
                    return "KANN Lesen";
                case "S":
                    return "Gelesen am " + _date;
                case "x":
                    return "KANN NICHT Schreiben";
                case "X":
                case "t":
                    return "KANN Schreiben";
                case "T":
                    return "Geschrieben am " + _date;
                case "y":
                    return "KANN NICHT Freigeben";
                case "Y":
                case "u":
                    return "KANN Freigeben";
                case "U":
                    return "Freigegeben am " + _date;
                case "z":
                    return "KANN NICHT Publizieren";
                case "Z":
                case "v":
                    return "KANN Publizieren";
                case "V":
                    return "Publiziert am " + _date;
                default:
                    return "unbekannte Berechtigung";
            }
        }

        #endregion

        #region COMPONENT ACCESS : Predefined Profiles

        private static readonly List<ComponentManagerType> COMP_MANAGER_ALL = new List<ComponentManagerType>
        {
            ComponentManagerType.ADMINISTRATOR,            
            ComponentManagerType.MODERATOR,
            ComponentManagerType.ENERGY_NETWORK_OPERATOR,
            ComponentManagerType.EENERGY_SUPPLIER,
            ComponentManagerType.BUILDING_DEVELOPER,
            ComponentManagerType.BUILDING_OPERATOR,
            ComponentManagerType.ARCHITECTURE,
            ComponentManagerType.FIRE_SAFETY,
            ComponentManagerType.BUILDING_PHYSICS,
            ComponentManagerType.MEP_HVAC,
            ComponentManagerType.PROCESS_MEASURING_CONTROL,
            ComponentManagerType.BUILDING_CONTRACTOR,
            ComponentManagerType.GUEST
        };

        // _type has all rights except PUBLISH (reserved mostly for the BUILDING_DEVELOPER)
        internal static Dictionary<ComponentManagerType, ComponentAccessTracker> 
            SetAccessProfile(   List<ComponentManagerType> _cannot_read, 
                                List<ComponentManagerType> _can_write, 
                                List<ComponentManagerType> _can_supervize, 
                                List<ComponentManagerType> _can_release)
        {
            Dictionary<ComponentManagerType, ComponentAccessTracker> profile = new Dictionary<ComponentManagerType, ComponentAccessTracker>();
            if (_cannot_read == null || _can_write == null || _can_supervize == null || _can_release == null) return profile;

            int nrRoles = Enum.GetNames(typeof(ComponentManagerType)).Length;
            for (int i = 0; i < nrRoles; i++)
            {
                ComponentManagerType role = (ComponentManagerType)i;
                ComponentAccessTracker tracker = new ComponentAccessTracker();
                tracker.AccessTypeFlags = ComponentAccessType.NO_ACCESS;

                // READ
                if (!_cannot_read.Contains(role))
                    tracker.AccessTypeFlags |= ComponentAccessType.READ;
                // WRITE
                if (_can_write.Contains(role))
                    tracker.AccessTypeFlags |= ComponentAccessType.WRITE;
                // SUPERVIZE
                if (_can_supervize.Contains(role))
                    tracker.AccessTypeFlags |= ComponentAccessType.SUPERVIZE;
                // RELEASE
                if (_can_release.Contains(role))
                    tracker.AccessTypeFlags |= ComponentAccessType.RELEASE;

                profile.Add(role, tracker);
            }
            return profile;
        }

        internal static Dictionary<ComponentManagerType, ComponentAccessTracker> GetStandardProfile(string _slot_type)
        {
            List<ComponentManagerType> cannot_read = new List<ComponentManagerType>();
            List<ComponentManagerType> can_write = new List<ComponentManagerType>();
            List<ComponentManagerType> can_supervize = new List<ComponentManagerType>();
            List<ComponentManagerType> can_release = new List<ComponentManagerType>();

            if (string.IsNullOrEmpty(_slot_type)) 
                return ComponentUtils.SetAccessProfile(cannot_read, can_write, can_supervize, can_release);

            if(_slot_type.StartsWith(COMP_SLOT_COMMUNICATION))
            {
                cannot_read.Add(ComponentManagerType.GUEST);

                can_write.Add(ComponentManagerType.ADMINISTRATOR);
                can_write.Add(ComponentManagerType.MODERATOR);

                can_supervize.AddRange(ComponentUtils.COMP_MANAGER_ALL);
                can_supervize.Remove(ComponentManagerType.GUEST);

                can_release.Add(ComponentManagerType.BUILDING_DEVELOPER);
            }

            if (_slot_type.StartsWith(COMP_SLOT_COST))
            {
                cannot_read.AddRange(ComponentUtils.COMP_MANAGER_ALL);
                cannot_read.Remove(ComponentManagerType.ADMINISTRATOR);
                cannot_read.Remove(ComponentManagerType.BUILDING_DEVELOPER);
                cannot_read.Remove(ComponentManagerType.BUILDING_PHYSICS);
                cannot_read.Remove(ComponentManagerType.ARCHITECTURE);
                cannot_read.Remove(ComponentManagerType.MEP_HVAC);

                can_write.Add(ComponentManagerType.BUILDING_DEVELOPER);
                can_write.Add(ComponentManagerType.ADMINISTRATOR);

                can_supervize.Add(ComponentManagerType.BUILDING_DEVELOPER);

                can_release.Add(ComponentManagerType.BUILDING_DEVELOPER);
            }

            if (_slot_type.StartsWith(COMP_SLOT_REGULATION))
            {
                can_write.Add(ComponentManagerType.ADMINISTRATOR);
                can_write.Add(ComponentManagerType.ARCHITECTURE);

                can_supervize.Add(ComponentManagerType.ARCHITECTURE);

                can_release.Add(ComponentManagerType.BUILDING_DEVELOPER);
            }

            if (_slot_type.StartsWith(COMP_SLOT_OBJECT) ||
                _slot_type.StartsWith(COMP_SLOT_SIZE) ||
                _slot_type.StartsWith(COMP_SLOT_LENGTHS) ||
                _slot_type.StartsWith(COMP_SLOT_AREAS) ||
                _slot_type.StartsWith(COMP_SLOT_VOLUMES) ||
                _slot_type.StartsWith(COMP_SLOT_POSITION))
            {
                cannot_read.Add(ComponentManagerType.GUEST);

                can_write.Add(ComponentManagerType.ADMINISTRATOR);
                can_write.Add(ComponentManagerType.ARCHITECTURE);
                // can_write can vary

                can_supervize.Add(ComponentManagerType.BUILDING_DEVELOPER);
                can_supervize.Add(ComponentManagerType.ARCHITECTURE);

                can_release.Add(ComponentManagerType.BUILDING_DEVELOPER);
            }

            if (_slot_type.StartsWith(COMP_SLOT_MATERIAL) ||
                _slot_type.StartsWith(COMP_SLOT_LAYER) ||
                _slot_type.StartsWith(COMP_SLOT_COMPOSITE) ||
                _slot_type.StartsWith(COMP_SLOT_JOINT) ||
                _slot_type.StartsWith(COMP_SLOT_OPENING))
            {
                cannot_read.Add(ComponentManagerType.GUEST);

                can_write.Add(ComponentManagerType.ADMINISTRATOR);
                can_write.Add(ComponentManagerType.BUILDING_PHYSICS);

                can_supervize.Add(ComponentManagerType.ARCHITECTURE);

                can_release.Add(ComponentManagerType.BUILDING_DEVELOPER);
            }

            if (_slot_type.StartsWith(COMP_SLOT_SYSTEM) ||
                _slot_type.StartsWith(COMP_SLOT_ERZEUGER) ||
                _slot_type.StartsWith(COMP_SLOT_VERTEILER) ||
                _slot_type.StartsWith(COMP_SLOT_VERTEILER_PIPE) ||
                _slot_type.StartsWith(COMP_SLOT_VERTEILER_PART) ||
                _slot_type.StartsWith(COMP_SLOT_ABGABE) ||
                _slot_type.StartsWith(COMP_SLOT_CONNECTED_TO) ||
                _slot_type.StartsWith(COMP_SLOT_SINGLE_HEATIG) ||
                _slot_type.StartsWith(COMP_SLOT_SINGLE_COOLING) ||
                _slot_type.StartsWith(COMP_SLOT_SINGLE_HUMIDITY) ||
                _slot_type.StartsWith(COMP_SLOT_SINGLE_ACOUSTICS) ||
                _slot_type.StartsWith(COMP_SLOT_SINGLE_LIGHT_NATURAL) ||
                _slot_type.StartsWith(COMP_SLOT_SINGLE_LIGHT_ARTIF) ||
                _slot_type.StartsWith(COMP_SLOT_SINGLE_WATER) ||
                _slot_type.StartsWith(COMP_SLOT_SINGLE_WASTE) ||
                _slot_type.StartsWith(COMP_SLOT_SINGLE_ELECTRICAL) ||
                _slot_type.StartsWith(COMP_SLOT_SINGLE_FIRE_SAFETY) ||
                _slot_type.StartsWith(COMP_SLOT_SINGLE_MSR))
            {
                cannot_read.Add(ComponentManagerType.GUEST);

                can_write.Add(ComponentManagerType.ADMINISTRATOR);
                can_write.Add(ComponentManagerType.MEP_HVAC);

                can_supervize.Add(ComponentManagerType.ARCHITECTURE);

                can_release.Add(ComponentManagerType.BUILDING_DEVELOPER);
            }

            return ComponentUtils.SetAccessProfile(cannot_read, can_write, can_supervize, can_release);
        }

        #endregion

        #region COMPONENT ACCESS: Component Separation acc. to Profile

        internal static Dictionary<ComponentManagerType, List<Component>> SplitAccToProfile(List<Component> _comp_in)
        {
            Dictionary<ComponentManagerType, List<Component>> separated = new Dictionary<ComponentManagerType, List<Component>>();
            
            int nr_cmt = Enum.GetNames(typeof(ComponentManagerType)).Length;
            for (int i = 0; i < nr_cmt; i++)
            {
                separated.Add((ComponentManagerType)i, new List<Component>());
            }
            if (_comp_in == null) return separated;
            if (_comp_in.Count == 0) return separated;

            foreach(Component c in _comp_in)
            {
                int counter = 0;
                ComponentManagerType save_type = ComponentManagerType.ADMINISTRATOR;
                for(int i = 0; i < nr_cmt; i++)
                {
                    ComponentManagerType manager = (ComponentManagerType)i;
                    // the administrator has permanent writing access 
                    // the guest         has no writing access
                    if (manager == ComponentManagerType.ADMINISTRATOR ||
                        manager == ComponentManagerType.GUEST) continue;

                    bool can_write = c.AccessLocal[manager].AccessTypeFlags.HasFlag(ComponentAccessType.WRITE);
                    if (can_write)
                    {
                        counter++;
                        if (counter == 1)
                            save_type = manager;
                        else if (counter > 1)
                            save_type = ComponentManagerType.GUEST;
                    }                   
                }
                // perform separation acc to type with writing access
                separated[save_type].Add(c);
                
            }

            return separated;
        }


        #endregion

        #region COMPONENT SLOTS: String definitions

        // usage: 
        // SYSTEM -> SYSTEM_Air_Conditioning, SYSTEM_Heating, etc. (i.e. begin the string with 'SYSTEM')
        public const string COMP_SLOT_LIST = "Liste";
        public const string COMP_SLOT_TUPLE = "Tupel";

        public const string COMP_SLOT_COMMUNICATION = "Kommunikation";
        public const string COMP_SLOT_COST = "Kosten";       
        public const string COMP_SLOT_REGULATION = "Anforderungen";
        public const string COMP_SLOT_SPECIFICATION = "Leistungsbeschr";
        public const string COMP_SLOT_CALCULATION = "Berechnung";

        public const string COMP_SLOT_OBJECT = "Geometrisches_Objekt";
        public const string COMP_SLOT_SIZE = "Geometrische_Maße";
        public const string COMP_SLOT_LENGTHS = "Geometrische_Längen";
        public const string COMP_SLOT_AREAS = "Geometrische_Flächen";
        public const string COMP_SLOT_VOLUMES = "Geometrische_Volumina";
        public const string COMP_SLOT_POSITION = "Verortung";

        public const string COMP_SLOT_MATERIAL = "Material";
        public const string COMP_SLOT_LAYER = "Schicht";
        public const string COMP_SLOT_COMPOSITE = "Aufbau";
        public const string COMP_SLOT_JOINT = "Anschluss";
        public const string COMP_SLOT_OPENING = "Öffnung";

        public const string COMP_SLOT_SYSTEM = "System";
        public const string COMP_SLOT_ERZEUGER = "Erzeuger";
        public const string COMP_SLOT_VERTEILER = "Verteiler";
        public const string COMP_SLOT_VERTEILER_PIPE = "Verteiler_Kanal";
        public const string COMP_SLOT_VERTEILER_PART = "Verteiler_Teil";
        public const string COMP_SLOT_ABGABE = "Abgabe";
        public const string COMP_SLOT_CONNECTED_TO = "Angeschlossen_an";

        public const string COMP_SLOT_SINGLE_HEATIG = "Heizung";
        public const string COMP_SLOT_SINGLE_COOLING = "Kühlung";
        public const string COMP_SLOT_SINGLE_HUMIDITY = "Feuchte";
        public const string COMP_SLOT_SINGLE_ACOUSTICS = "Akustik";
        public const string COMP_SLOT_SINGLE_LIGHT_NATURAL = "Naturlicht";
        public const string COMP_SLOT_SINGLE_LIGHT_ARTIF = "Kustlicht";
        public const string COMP_SLOT_SINGLE_WATER = "Wasser";
        public const string COMP_SLOT_SINGLE_WASTE = "Abwasser";
        public const string COMP_SLOT_SINGLE_ELECTRICAL = "Elektro";
        public const string COMP_SLOT_SINGLE_FIRE_SAFETY = "Brandschutz";
        public const string COMP_SLOT_SINGLE_MSR = "MSR";

        public const string COMP_SLOT_DELIMITER = "_0";
        public const string COMP_SLOT_UNDEFINED = "Undefined Slot";

        public static readonly List<string> COMP_SLOTS_ALL = new List<string>
        {
            COMP_SLOT_LIST,
            COMP_SLOT_TUPLE,

            COMP_SLOT_COMMUNICATION,
            COMP_SLOT_COST,
            COMP_SLOT_REGULATION,
            COMP_SLOT_SPECIFICATION,
            COMP_SLOT_CALCULATION,

            COMP_SLOT_OBJECT,
            COMP_SLOT_SIZE,
            COMP_SLOT_LENGTHS,
            COMP_SLOT_AREAS,
            COMP_SLOT_VOLUMES,
            COMP_SLOT_POSITION,

            COMP_SLOT_MATERIAL,
            COMP_SLOT_LAYER,
            COMP_SLOT_COMPOSITE,
            COMP_SLOT_JOINT,
            COMP_SLOT_OPENING,

            COMP_SLOT_SYSTEM,
            COMP_SLOT_ERZEUGER,
            COMP_SLOT_VERTEILER,
            COMP_SLOT_VERTEILER_PIPE,
            COMP_SLOT_VERTEILER_PART,
            COMP_SLOT_ABGABE,
            COMP_SLOT_CONNECTED_TO,

            COMP_SLOT_SINGLE_HEATIG,
            COMP_SLOT_SINGLE_COOLING,
            COMP_SLOT_SINGLE_HUMIDITY,
            COMP_SLOT_SINGLE_ACOUSTICS,
            COMP_SLOT_SINGLE_LIGHT_NATURAL,
            COMP_SLOT_SINGLE_LIGHT_ARTIF,
            COMP_SLOT_SINGLE_WATER,
            COMP_SLOT_SINGLE_WASTE,
            COMP_SLOT_SINGLE_ELECTRICAL,
            COMP_SLOT_SINGLE_FIRE_SAFETY,
            COMP_SLOT_SINGLE_MSR
        };

        public static readonly List<SelectableString> COMP_SLOTS_ALL_SELECTABLE = new List<SelectableString>
        {
            new SelectableString(COMP_SLOT_LIST),
            new SelectableString(COMP_SLOT_TUPLE),

            new SelectableString(COMP_SLOT_COMMUNICATION),
            new SelectableString(COMP_SLOT_COST),
            new SelectableString(COMP_SLOT_REGULATION),
            new SelectableString(COMP_SLOT_SPECIFICATION),
            new SelectableString(COMP_SLOT_CALCULATION),

            new SelectableString(COMP_SLOT_OBJECT),
            new SelectableString(COMP_SLOT_SIZE),
            new SelectableString(COMP_SLOT_LENGTHS),
            new SelectableString(COMP_SLOT_AREAS),
            new SelectableString(COMP_SLOT_VOLUMES),
            new SelectableString(COMP_SLOT_POSITION),

            new SelectableString(COMP_SLOT_MATERIAL),
            new SelectableString(COMP_SLOT_LAYER),
            new SelectableString(COMP_SLOT_COMPOSITE),
            new SelectableString(COMP_SLOT_JOINT),
            new SelectableString(COMP_SLOT_OPENING),

            new SelectableString(COMP_SLOT_SYSTEM),
            new SelectableString(COMP_SLOT_ERZEUGER),
            new SelectableString(COMP_SLOT_VERTEILER),
            new SelectableString(COMP_SLOT_VERTEILER_PIPE),
            new SelectableString(COMP_SLOT_VERTEILER_PART),
            new SelectableString(COMP_SLOT_ABGABE),
            new SelectableString(COMP_SLOT_CONNECTED_TO),

            new SelectableString(COMP_SLOT_SINGLE_HEATIG),
            new SelectableString(COMP_SLOT_SINGLE_COOLING),
            new SelectableString(COMP_SLOT_SINGLE_HUMIDITY),
            new SelectableString(COMP_SLOT_SINGLE_ACOUSTICS),
            new SelectableString(COMP_SLOT_SINGLE_LIGHT_NATURAL),
            new SelectableString(COMP_SLOT_SINGLE_LIGHT_ARTIF),
            new SelectableString(COMP_SLOT_SINGLE_WATER),
            new SelectableString(COMP_SLOT_SINGLE_WASTE),
            new SelectableString(COMP_SLOT_SINGLE_ELECTRICAL),
            new SelectableString(COMP_SLOT_SINGLE_FIRE_SAFETY),
            new SelectableString(COMP_SLOT_SINGLE_MSR)
        };

        public static Relation2GeomType SlotToRelation2GeomType(string _slot)
        {
            if (string.IsNullOrEmpty(_slot)) return Relation2GeomType.NONE;

            switch(_slot)
            {
                case COMP_SLOT_OBJECT:
                case COMP_SLOT_SIZE:
                case COMP_SLOT_LENGTHS:
                case COMP_SLOT_AREAS:
                case COMP_SLOT_VOLUMES:
                case COMP_SLOT_POSITION:
                    return Relation2GeomType.DESCRIBES_3D;
                case COMP_SLOT_COMPOSITE:
                case COMP_SLOT_JOINT:
                case COMP_SLOT_OPENING:
                    return Relation2GeomType.ALIGNED_WITH;
                case COMP_SLOT_SYSTEM:
                case COMP_SLOT_ERZEUGER:
                case COMP_SLOT_VERTEILER:
                case COMP_SLOT_VERTEILER_PIPE:
                case COMP_SLOT_VERTEILER_PART:
                case COMP_SLOT_ABGABE:
                    return Relation2GeomType.CONTAINED_IN;
                default:
                    return Relation2GeomType.NONE;
            }
        }

        public static string Relation2GeomTypeToSlot(Relation2GeomType _type)
        {
            switch(_type)
            {
                case Relation2GeomType.GROUPS:
                case Relation2GeomType.DESCRIBES:
                    return COMP_SLOT_OBJECT;
                case Relation2GeomType.DESCRIBES_3D:
                    return COMP_SLOT_VOLUMES;                    
                case Relation2GeomType.DESCRIBES_2DorLESS:
                    return COMP_SLOT_AREAS;
                case Relation2GeomType.ALIGNED_WITH:
                    return COMP_SLOT_COMPOSITE;
                case Relation2GeomType.CONTAINED_IN:
                case Relation2GeomType.CONNECTS:
                    return COMP_SLOT_POSITION;
                default:
                    return COMP_SLOT_UNDEFINED;
            }
        }

        #endregion

        #region COMPONENT VALIDITY

        public static ComponentValidity ComponentAccessProfileToValidity(IDictionary<ComponentManagerType, ComponentAccessTracker> _profile)
        {
            if (_profile == null) return ComponentValidity.NOT_CALCULATED;

            // gather info from the profile
            DateTime t_now = DateTime.Now;
            DateTime t_write_earliest = t_now;
            DateTime t_write_latest = DateTime.MinValue;
            DateTime t_supervize_earliest = t_now;
            DateTime t_supervize_latest = DateTime.MinValue;
            DateTime t_release_earliest = t_now;
            DateTime t_release_latest = DateTime.MinValue;

            foreach(var entry in _profile)
            {
                if (entry.Value.AccessTypeFlags.HasFlag(ComponentAccessType.WRITE))
                {
                    if (entry.Value.LastAccess_Write < t_write_earliest)
                        t_write_earliest = entry.Value.LastAccess_Write;
                    if (entry.Value.LastAccess_Write > t_write_latest)
                        t_write_latest = entry.Value.LastAccess_Write;
                }

                if (entry.Value.AccessTypeFlags.HasFlag(ComponentAccessType.SUPERVIZE))
                {
                    if (entry.Value.LastAccess_Supervize < t_supervize_earliest)
                        t_supervize_earliest = entry.Value.LastAccess_Supervize;
                    if (entry.Value.LastAccess_Supervize > t_supervize_latest)
                        t_supervize_latest = entry.Value.LastAccess_Supervize;
                }

                if (entry.Value.AccessTypeFlags.HasFlag(ComponentAccessType.RELEASE))
                {
                    if (entry.Value.LastAccess_Release < t_release_earliest)
                        t_release_earliest = entry.Value.LastAccess_Release;
                    if (entry.Value.LastAccess_Release > t_release_latest)
                        t_release_latest = entry.Value.LastAccess_Release;
                } 
            }

            // analyze info
            if (t_write_latest > t_supervize_earliest && t_supervize_earliest > DateTime.MinValue)
                return ComponentValidity.WRITE_AFTER_SUPERVIZE;
            else if (t_write_latest > t_release_earliest && t_release_earliest > DateTime.MinValue)
                return ComponentValidity.WRITE_AFTER_RELEASE;
            else if (t_supervize_latest > t_release_earliest && t_release_earliest > DateTime.MinValue)
                return ComponentValidity.SUPERVIZE_AFTER_RELEASE;
            else
                return ComponentValidity.VALID;
        }


        public static string ComponentValidityToString(ComponentValidity _validity)
        {
            switch(_validity)
            {
                case ComponentValidity.WRITE_AFTER_SUPERVIZE:
                    return "WRITE_AFTER_SUPERVIZE";
                case ComponentValidity.WRITE_AFTER_RELEASE:
                    return "WRITE_AFTER_RELEASE";
                case ComponentValidity.SUPERVIZE_AFTER_RELEASE:
                    return "SUPERVIZE_AFTER_RELEASE";
                case ComponentValidity.VALID:
                    return "VALID";
                default :
                    return "NOT_CALCULATED";
            }
        }

        public static ComponentValidity StringToComponentValidity(string _validity_as_str)
        {
            if (string.IsNullOrEmpty(_validity_as_str)) return ComponentValidity.NOT_CALCULATED;

            switch (_validity_as_str)
            {
                case "WRITE_AFTER_SUPERVIZE":
                    return ComponentValidity.WRITE_AFTER_SUPERVIZE;
                case "WRITE_AFTER_RELEASE":
                    return ComponentValidity.WRITE_AFTER_RELEASE;
                case "SUPERVIZE_AFTER_RELEASE":
                    return ComponentValidity.SUPERVIZE_AFTER_RELEASE;
                case "VALID":
                    return ComponentValidity.VALID;
                default:
                    return ComponentValidity.NOT_CALCULATED;
            }
        }

        public static string ComponentValidityToDescrDE(ComponentValidity _validity)
        {
            switch (_validity)
            {
                case ComponentValidity.WRITE_AFTER_SUPERVIZE:
                    return "Schreibvorgang nach Freigabe";
                case ComponentValidity.WRITE_AFTER_RELEASE:
                    return "Schreibvorgang nach Publikation";
                case ComponentValidity.SUPERVIZE_AFTER_RELEASE:
                    return "Freigabe nach Publikation";
                case ComponentValidity.VALID:
                    return "gültig";
                default:
                    return "nicht berechnet";
            }
        }

        public static Color ComponentValidityToColor(ComponentValidity _validity)
        {
            switch(_validity)
            {
                case ComponentValidity.WRITE_AFTER_SUPERVIZE:
                    return (Color)ColorConverter.ConvertFromString("#FF962a00");
                case ComponentValidity.WRITE_AFTER_RELEASE:
                    return (Color)ColorConverter.ConvertFromString("#FFff4500");
                case ComponentValidity.SUPERVIZE_AFTER_RELEASE:
                    return (Color)ColorConverter.ConvertFromString("#FFffaa00");
                case ComponentValidity.VALID:
                    return (Color)ColorConverter.ConvertFromString("#FF0000ff");
                default:
                    return (Color)ColorConverter.ConvertFromString("#FF888888");
            }
        }

        #endregion
    }
}
