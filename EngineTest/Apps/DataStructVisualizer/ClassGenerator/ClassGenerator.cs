using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Globalization;
using System.IO;

using DataStructVisualizer.Nodes;
using ClassGenerator.CodeSnippets;

namespace DataStructVisualizer.ClassGenerator
{
    [Flags]
    public enum TypeFlags 
    { 
        IsNotValid = 0,
        IsEnum = 1,
        CanAssignValue = 2,
        CanAssignNull = 4,
        CanAddValue = 8, // ->  has also Insert(),  Count , Clear(), Contains()
        CanPushValue = 16, // -> has also Pop(),     Count , Clear(), Contains()
        CanEnqueue = 32, // ->  has also Dequeue(), Count , Clear(), Contains()
        CanAccessItem = 64, // [] operator
        CanAccessSubItem = 128, // classinstance.propertyname
        ContainedType_CanAccessSubItem = 256, // z.B: [0].propertyname

        IsSimpleValueType = CanAssignValue,
        IsSimpleNullableType = CanAssignValue | CanAssignNull,
        IsList = CanAssignValue | CanAssignNull | CanAddValue | CanAccessItem,
        IsStack = CanAssignValue | CanAssignNull | CanPushValue,
        IsQueue = CanAssignValue | CanAssignNull | CanEnqueue
    }

    
    public class ClassGenerator : INotifyPropertyChanged
    {
        #region STATIC INIT

        public static readonly string[] RESERVED_STRINGS = new string[] { "System", "INotifyPropertyChanged", "ClassGenerator" };
        public static readonly string DEFAULT_NAMESPACE = "ClassGenerator";
        public static readonly string LOG_FILE_NAME = "class_records.txt";
        
        private static readonly string NOTHING, EMPTY_STRING;

        static ClassGenerator()
        { 
            char empty_set = '\u2205';
            NOTHING = empty_set.ToString();
            char quad = '\u2395';
            EMPTY_STRING = quad.ToString();
        }

        
        
        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RegisterPropertyChanged(string _propName)
        {
            if (_propName == null)
                return;

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(_propName));
        }

        #endregion     

        #region PROPERTIES: Text

        // whole text for saving as file
        private string class_text;
        public string ClassText
        {
            get { return this.class_text; }
            private set 
            { 
                this.class_text = value;
                this.RegisterPropertyChanged("ClassText");
            }
        }

        // text snippets for displaying
        public List<string> Snippets { get; private set; }
        public string ClassNameFull { get { return this.class_candidate.TypeInfo.TWD_Type_Name; } }

        #endregion

        #region INTERNAL CLASSES

        #region CLASS: for Type Description
        private class TypeWithDescription
        {
            #region STATIC: Init

            public static readonly Dictionary<string, TypeWithDescription> TYPES_CONTAINER_DEFAULT;
            public static readonly Dictionary<string, TypeWithDescription> TYPES_SIMPLE_DEFAULT;
            
            private static readonly string FLAG_0 = "\U000026AC";
            private static readonly string FLAG_1 = "\U000025CF";
            private static readonly Regex BINARY = new Regex("^[01]{1,32}$");

            private static readonly string SYMB_DOUBLE, SYMB_INT, SYMB_LONG, SYMB_STRING;
            
            static TypeWithDescription()
            {
                TYPES_CONTAINER_DEFAULT = new Dictionary<string, TypeWithDescription>();
                TYPES_CONTAINER_DEFAULT.Add("list", new TypeWithDescription(typeof(List<>), TypeFlags.IsList));
                TYPES_CONTAINER_DEFAULT.Add("List", new TypeWithDescription(typeof(List<>), TypeFlags.IsList));
                TYPES_CONTAINER_DEFAULT.Add("stack", new TypeWithDescription(typeof(Stack<>), TypeFlags.IsStack));
                TYPES_CONTAINER_DEFAULT.Add("Stack", new TypeWithDescription(typeof(Stack<>), TypeFlags.IsStack));
                TYPES_CONTAINER_DEFAULT.Add("queue", new TypeWithDescription(typeof(Queue<>), TypeFlags.IsQueue));
                TYPES_CONTAINER_DEFAULT.Add("Queue", new TypeWithDescription(typeof(Queue<>), TypeFlags.IsQueue));

                TYPES_SIMPLE_DEFAULT = new Dictionary<string, TypeWithDescription>();
                TYPES_SIMPLE_DEFAULT.Add("int", new TypeWithDescription(typeof(int), TypeFlags.IsSimpleValueType));
                TYPES_SIMPLE_DEFAULT.Add("Int", new TypeWithDescription(typeof(int), TypeFlags.IsSimpleValueType));
                TYPES_SIMPLE_DEFAULT.Add("Int32", new TypeWithDescription(typeof(int), TypeFlags.IsSimpleValueType));
                TYPES_SIMPLE_DEFAULT.Add("int32", new TypeWithDescription(typeof(int), TypeFlags.IsSimpleValueType));
                TYPES_SIMPLE_DEFAULT.Add("long", new TypeWithDescription(typeof(long), TypeFlags.IsSimpleValueType));
                TYPES_SIMPLE_DEFAULT.Add("Long", new TypeWithDescription(typeof(long), TypeFlags.IsSimpleValueType));
                TYPES_SIMPLE_DEFAULT.Add("Int64", new TypeWithDescription(typeof(long), TypeFlags.IsSimpleValueType));
                TYPES_SIMPLE_DEFAULT.Add("int64", new TypeWithDescription(typeof(long), TypeFlags.IsSimpleValueType));
                TYPES_SIMPLE_DEFAULT.Add("double", new TypeWithDescription(typeof(double), TypeFlags.IsSimpleValueType));
                TYPES_SIMPLE_DEFAULT.Add("Double", new TypeWithDescription(typeof(double), TypeFlags.IsSimpleValueType));
                TYPES_SIMPLE_DEFAULT.Add("float", new TypeWithDescription(typeof(float), TypeFlags.IsSimpleValueType));
                TYPES_SIMPLE_DEFAULT.Add("Float", new TypeWithDescription(typeof(float), TypeFlags.IsSimpleValueType));
                TYPES_SIMPLE_DEFAULT.Add("string", new TypeWithDescription(typeof(string), TypeFlags.IsSimpleNullableType));
                TYPES_SIMPLE_DEFAULT.Add("String", new TypeWithDescription(typeof(string), TypeFlags.IsSimpleNullableType));
                TYPES_SIMPLE_DEFAULT.Add("Coords2D", new TypeWithDescription(typeof(Coords2D), TypeFlags.IsSimpleNullableType));
                TYPES_SIMPLE_DEFAULT.Add("object", new TypeWithDescription(typeof(object), TypeFlags.IsSimpleNullableType));
                TYPES_SIMPLE_DEFAULT.Add("Object", new TypeWithDescription(typeof(object), TypeFlags.IsSimpleNullableType));                
                TYPES_SIMPLE_DEFAULT.Add("enum", new TypeWithDescription(typeof(object), TypeFlags.IsEnum));
                TYPES_SIMPLE_DEFAULT.Add("Enum", new TypeWithDescription(typeof(object), TypeFlags.IsEnum));
                TYPES_SIMPLE_DEFAULT.Add("Invalid", new TypeWithDescription(typeof(object), TypeFlags.IsNotValid));
                TYPES_SIMPLE_DEFAULT.Add("invalid", new TypeWithDescription(typeof(object), TypeFlags.IsNotValid));

                SYMB_DOUBLE = "\U0001D53B\U0001D560\U0001D566\U0001D553\U0001D55D\U0001D556";
                SYMB_INT = "\U0001D540\U0001D55F\U0001D565";
                SYMB_LONG = "\U0001D543\U0001D560\U0001D55F\U0001D558";
                SYMB_STRING = "\U0001D54A\U0001D565\U0001D563\U0001D55A\U0001D55F\U0001D558";
                //SYMB_TYPE = "\U0001D54B";
            }

            private static string GetTypeSymbol(string _type)
            {
                if (string.IsNullOrEmpty(_type)) return string.Empty;

                if (_type == typeof(double).ToString())
                    return SYMB_DOUBLE;
                else if (_type == typeof(int).ToString())
                    return SYMB_INT;
                else if (_type == typeof(long).ToString())
                    return SYMB_LONG;
                else if (_type == typeof(string).ToString())
                    return SYMB_STRING;
                else
                    return _type;
            }

            #endregion

            public Type TWD_Type { get; private set; }
            public TypeWithDescription TWD_Type_Custom_Contained { get; private set; }
            public List<string> TWD_Values { get; private set; } 
            public string TWD_Type_Name { get; private set; } // only name
            public string TWD_Type_Namespace { get; private set; } // only namespace
            public string TWD_Type_FullName { get; private set; } // namespace.name
            public TypeFlags TWD_Flags {get; private set;}
            public TypeWithDescription(Type _type, TypeFlags _descr)
            {
                this.TWD_Type = _type;
                this.TWD_Type_Custom_Contained = null;
                this.TWD_Values = new List<string>();
                this.TWD_Type_Name = TypeWithDescription.CSharpName(this);
                this.TWD_Type_Namespace = _type.Namespace;
                this.TWD_Type_FullName = TypeWithDescription.CSharpName(this, true);
                this.TWD_Flags = _descr;
            }
            public TypeWithDescription(Type _type, TypeWithDescription _twd_cont, TypeFlags _descr)
            {
                this.TWD_Type = _type;
                this.TWD_Type_Custom_Contained = _twd_cont;
                this.TWD_Values = new List<string>();
                this.TWD_Type_Name = TypeWithDescription.CSharpName(this);
                this.TWD_Type_Namespace = _type.Namespace;
                this.TWD_Type_FullName = TypeWithDescription.CSharpName(this, true);
                this.TWD_Flags = _descr;
            }
            public TypeWithDescription(string _type_as_str, string _namespace, TypeFlags _descr)
            {
                this.TWD_Type = typeof(object);
                this.TWD_Type_Custom_Contained = null;
                this.TWD_Values = new List<string>();
                this.TWD_Type_Name = _type_as_str;
                this.TWD_Type_Namespace = _namespace;
                this.TWD_Type_FullName = TWD_Type_Namespace + "." + TWD_Type_Name;
                this.TWD_Flags = _descr;
            }
            public void AddFlags(TypeFlags _add)
            {
                this.TWD_Flags = this.TWD_Flags | _add;
            }
            public void AddValues(string _value)
            {
                // consistency checks in the caller method!
                this.TWD_Values.Add(_value);
            }
            public override string ToString()
            {
                string output = Convert.ToString((int)this.TWD_Flags, 2);
                output = output.PadLeft(9, '0');
                output = output.Replace("1", TypeWithDescription.FLAG_1);
                output = output.Replace("0", TypeWithDescription.FLAG_0);
                output += " " + this.TWD_Type_FullName;
                return output;
            }

            #region PARSING: Type from String

            public static TypeWithDescription ParseStringWFlags(string _input, List<TypeWithDescription> _custom_types)
            {
                if (string.IsNullOrEmpty(_input)) return null;

                TypeWithDescription type_parsed = TypeWithDescription.TYPES_SIMPLE_DEFAULT["object"];

                string[] components_top = _input.Split(new string[] { " ", "<", ">" }, StringSplitOptions.RemoveEmptyEntries);
                string[] components_top_1 = components_top.Select(x => x.Trim()).ToArray();
                string[] components_top_clean = components_top_1.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                if (components_top_clean == null || components_top_clean.Length < 2) return type_parsed;
                
                // extract FLAG string
                string flags_as_str = components_top_clean[0];
                // extract the NAME string of the (FIRST) type
                string type_name_1 = components_top_clean[1];
                string[] type_name_1_comps = type_name_1.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                if (type_name_1_comps == null || type_name_1_comps.Length < 2) return type_parsed;
                // extract the NAME string of the (SECOND / NESTED) type
                string type_name_2 = (components_top_clean.Length > 2) ? components_top_clean[2] : string.Empty;
                string[] type_name_2_comps;
                if (!string.IsNullOrEmpty(type_name_2))
                    type_name_2_comps = type_name_2.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                else
                    type_name_2_comps = new string[] { };
                // assemble NAME SEARCH string
                string search_name = type_name_1_comps[type_name_1_comps.Length - 1];
                string search_nsp = string.Join(".", type_name_1_comps.Take(type_name_1_comps.Length - 1));
                if (type_name_2_comps != null && type_name_2_comps.Length > 1)
                    search_name += "<" + type_name_2_comps[type_name_2_comps.Length - 1] + ">";
                
               
                // parse flags
                TypeFlags flags = 0;
                flags_as_str = flags_as_str.Replace(TypeWithDescription.FLAG_1, "1");
                flags_as_str = flags_as_str.Replace(TypeWithDescription.FLAG_0, "0");
                if (TypeWithDescription.BINARY.IsMatch(flags_as_str))
                {
                    int flags_as_int = 0;
                    try
                    {
                        flags_as_int = Convert.ToInt32(flags_as_str, 2);
                    }
                    catch
                    { }
                    flags = (TypeFlags)flags_as_int;
                }
                // parse type name
                type_parsed = TypeWithDescription.ExtractSingleTypeDescr(search_nsp, search_name, _custom_types);
                type_parsed.AddFlags(flags);
                type_parsed.TWD_Type_Namespace = search_nsp;                

                return type_parsed;
            }

            public static List<TypeWithDescription> ExtractTypeDescr(string _namespace, string _type_as_str,
                                                            List<TypeWithDescription> _custom_types)
            {
                if (string.IsNullOrEmpty(_type_as_str)) return new List<TypeWithDescription>();
                _type_as_str = _type_as_str.Trim();
                if (string.IsNullOrEmpty(_type_as_str)) return new List<TypeWithDescription>();
                
                // split incoming string
                int ind_semicolon = _type_as_str.LastIndexOf(":");
                string[] type_names = _type_as_str.Split(new string[] { ":", ",", " " }, StringSplitOptions.RemoveEmptyEntries);
                string[] type_names_1 = type_names.Select(x => ClassGenerator.CleanString(x)).ToArray();
                string[] type_names_clean = type_names_1.Where(x => !string.IsNullOrEmpty(x)).ToArray();

                List<TypeWithDescription> extracted_types = new List<TypeWithDescription>();
                if (ind_semicolon == 0)
                {
                    // inheritance (supports multiple types, if at most one is not an interface)
                    foreach (string entry in type_names_clean)
                        extracted_types.Add(TypeWithDescription.ExtractSingleTypeDescr(_namespace, entry, _custom_types));
                }
                else
                {
                    // type declaration (supports only one type)               
                    if (type_names_clean.Length > 0)
                        extracted_types.Add(TypeWithDescription.ExtractSingleTypeDescr(_namespace, type_names_clean[0], _custom_types));
                }
                
                return extracted_types;
            }

            private static TypeWithDescription ExtractSingleTypeDescr(string _namespace, string _single_type_as_str, 
                                                                      List<TypeWithDescription> _custom_types)
            {
                // ASSUMES THE INPUT IS NOT NULL OR EMPTY                
                // try custom type extraction
                if (_custom_types != null && _custom_types.Count > 0)
                {
                    TypeWithDescription found = _custom_types.Find(x => x.TWD_Type_Name == _single_type_as_str);
                    if (found != null)
                        return found;
                }
                // try standard type extraction
                TypeWithDescription type_standard = TypeWithDescription.ExtractStandardTypeDescr(_namespace, _single_type_as_str, _custom_types);
                if (type_standard.TWD_Flags == TypeFlags.IsNotValid)
                {
                    // the type is new
                    TypeWithDescription type_new = new TypeWithDescription(_single_type_as_str, _namespace, TypeFlags.IsSimpleNullableType);
                    return type_new;
                }
                else
                {
                    return type_standard;
                }
            }

            private static TypeWithDescription ExtractSingleCustomTypeDescr(string _namespace, string _single_type_as_str,
                                                                            List<TypeWithDescription> _custom_types)
            {
                // ASSUMES THE INPUT IS NOT NULL OR EMPTY                
                // try custom type extraction
                if (_custom_types != null && _custom_types.Count > 0)
                {
                    TypeWithDescription found = _custom_types.Find(x => x.TWD_Type_Name == _single_type_as_str);
                    if (found != null)
                        return found;
                }
                
                // the type is unknown
                return TypeWithDescription.TYPES_SIMPLE_DEFAULT["Invalid"];
            }

            private static TypeWithDescription ExtractStandardTypeDescr(string _namespace, string _type_as_str, List<TypeWithDescription> _custom_types)
            {
                if (string.IsNullOrEmpty(_type_as_str)) return null;

                TypeWithDescription type = TypeWithDescription.TYPES_SIMPLE_DEFAULT["Invalid"];
                
                // remove superfluous whitespaces
                _type_as_str = _type_as_str.Trim();

                // try simple types
                if (TypeWithDescription.TYPES_SIMPLE_DEFAULT.ContainsKey(_type_as_str))
                    type = TypeWithDescription.TYPES_SIMPLE_DEFAULT[_type_as_str];

                // try complex types
                if (type.TWD_Flags == TypeFlags.IsNotValid)
                {
                    string[] type_comps = _type_as_str.Split(new string[] { "<", ">" }, StringSplitOptions.RemoveEmptyEntries);
                    if (type_comps == null || type_comps.Length < 2)
                        return type;

                    Type container_type, contained_type;
                    if (TypeWithDescription.TYPES_CONTAINER_DEFAULT.ContainsKey(type_comps[0]))
                    {
                        container_type = TypeWithDescription.TYPES_CONTAINER_DEFAULT[type_comps[0]].TWD_Type;
                        if (TypeWithDescription.TYPES_SIMPLE_DEFAULT.ContainsKey(type_comps[1]))
                        {
                            // both container and contained types are STANDARD
                            contained_type = TypeWithDescription.TYPES_SIMPLE_DEFAULT[type_comps[1]].TWD_Type;
                            if (container_type != null && contained_type != null)
                            {
                                return new TypeWithDescription(container_type.MakeGenericType(contained_type),
                                    TypeWithDescription.TYPES_CONTAINER_DEFAULT[type_comps[0]].TWD_Flags);
                            }
                        }
                        else
                        {
                            // container type STANDARD, contained CUSTOM
                            contained_type = typeof(object);
                            TypeWithDescription contained_twd = TypeWithDescription.ExtractSingleCustomTypeDescr(_namespace, type_comps[1], _custom_types);
                            if (container_type != null && contained_twd != null)
                            {
                                TypeWithDescription constructed_type = new TypeWithDescription(container_type.MakeGenericType(contained_type), contained_twd,
                                    TypeWithDescription.TYPES_CONTAINER_DEFAULT[type_comps[0]].TWD_Flags);
                                if (contained_twd.TWD_Flags.HasFlag(TypeFlags.CanAccessSubItem))
                                    constructed_type.AddFlags(TypeFlags.ContainedType_CanAccessSubItem);
                                return constructed_type;
                            }
                        }
                    }
                }

                return type;
            }
            #endregion

            #region UTILS: Complex Type Names

            public static string CSharpName(TypeWithDescription _twd, bool _full_name = false)
            {
                if (_twd == null) return string.Empty;

                if (_twd.TWD_Type_Custom_Contained == null)
                    return TypeWithDescription.CSharpName(_twd.TWD_Type, _full_name);
                else if (_twd.TWD_Type_Custom_Contained != null && _twd.TWD_Type.IsGenericType)
                {
                    var sb = new StringBuilder();
                    string name = (_full_name) ? _twd.TWD_Type.FullName : _twd.TWD_Type.Name;
                    string name_custom_contained = (_full_name) ? _twd.TWD_Type_Custom_Contained.TWD_Type_FullName : _twd.TWD_Type_Custom_Contained.TWD_Type_Name;
                    sb.Append(name.Substring(0, name.IndexOf('`')));
                    sb.Append("<");
                    sb.Append(name_custom_contained);
                    sb.Append(">");
                    return sb.ToString();
                }

                return string.Empty;
            }

            private static string CSharpName(Type _type, bool _full_name = false)
            {
                var sb = new StringBuilder();
                var name = (_full_name) ? _type.FullName : _type.Name;
                if (!_type.IsGenericType) return name;

                sb.Append(name.Substring(0, name.IndexOf('`')));
                sb.Append("<");
                sb.Append(string.Join(", ", _type.GetGenericArguments().Select(t => TypeWithDescription.CSharpName(t, _full_name))));
                sb.Append(">");
                return sb.ToString();
            }

            #endregion
        }

        #endregion

        #region CLASS: For Saving Calculations
        private class CalculationRecord
        {
            private static readonly string RP = "RP";
            private static readonly Regex PARAM_PATTERN = new Regex("p[0-9]+");
            private static readonly string[] PARAM_SYMBOLS = new string[] { "p0", "p1", "p2", "p3", "p4", "p5", "p6", "p7", "p8", "p9" };

            public string ReturnProp; // corresponds to the property, for which the calculation is meant
            public List<string> Parameters; // can be empty, namespace.name
            public string CalcExpression; // default: assignment

            public CalculationRecord(string _return_prop_name, string _default_val)
            {
                this.ReturnProp = _return_prop_name;
                this.Parameters = new List<string> { _default_val };
                this.CalcExpression = CalculationRecord.RP + " = " + CalculationRecord.PARAM_SYMBOLS[0] + ";"; // RT = p0;
            }

            public string AssembleExpression()
            {
                string expression = this.CalcExpression;
                expression = expression.Replace(CalculationRecord.RP, this.ReturnProp);
                for(int i = 0; i < CalculationRecord.PARAM_SYMBOLS.Length && i < this.Parameters.Count; i++)
                {
                    expression = expression.Replace(CalculationRecord.PARAM_SYMBOLS[i], this.Parameters[i]);
                }
                return expression;
            }

        }
        #endregion

        #region CLASS: For Saving Calss Properties
        private class ClassRecord
        {
            #region CLASS MEMBERS / PROPERTIES
            public TypeWithDescription TypeInfo;
            public long ID { get; private set; }
            
            // only for CLASS
            private List<TypeWithDescription> base_TWD;
            public IReadOnlyCollection<TypeWithDescription> BaseTWD { get { return this.base_TWD.AsReadOnly(); } }


            private List<long> property_ids;
            public IReadOnlyCollection<long> PropertyIds { get { return this.property_ids.AsReadOnly(); } }

            private List<string> property_names;
            public IReadOnlyCollection<string> PropertyNames { get { return this.property_names.AsReadOnly(); } }

            private List<TypeWithDescription> property_TWD;
            public IReadOnlyCollection<TypeWithDescription> PropertyTWD { get { return this.property_TWD.AsReadOnly(); } }

            private List<string> property_init_values;
            public IReadOnlyCollection<string> PropertyInitVals { get { return this.property_init_values.AsReadOnly(); } }



            // only for ENUM
            private List<string> enum_values;
            public IReadOnlyCollection<string> EnumValues { get { return this.enum_values.AsReadOnly(); } }

            #endregion

            public ClassRecord(long _id, string _type_as_str, string _namespace, TypeFlags _descr)
            {
                this.TypeInfo = new TypeWithDescription(_type_as_str, _namespace, _descr);
                this.ID = _id;

                this.base_TWD = new List<TypeWithDescription>();
                this.property_ids = new List<long>();
                this.property_names = new List<string>();
                this.property_TWD = new List<TypeWithDescription>();
                this.property_init_values = new List<string>();

                this.enum_values = new List<string>();               
            }

            #region SETTER

            public void AddBaseType(TypeWithDescription _base_type)
            {
                if (_base_type == null) return;
                this.base_TWD.Add(_base_type);
                this.TypeInfo.AddFlags(_base_type.TWD_Flags);                
            }

            public void AddProperty(long _id, string _name, TypeWithDescription _type, string _value)
            {
                if (_id < 0 || _name == null || _type == null || _value == null) return;
                if (this.property_ids.Contains(_id)) return;

                this.property_ids.Add(_id);
                this.property_names.Add(_name);
                this.property_TWD.Add(_type);
                this.property_init_values.Add(_value);
                this.TypeInfo.AddFlags(TypeFlags.CanAccessSubItem);
            }

            public void AddEnumValue(string _value)
            {
                if (this.TypeInfo.TWD_Flags != TypeFlags.IsEnum) return;
                if (string.IsNullOrEmpty(_value)) return;
                if (this.enum_values.Contains(_value)) return;

                this.enum_values.Add(_value);
                this.TypeInfo.AddValues(_value);
            }

            #endregion

            #region TOSTRING
            public override string ToString()
            {
                string output = this.ID + "|";
                if (this.TypeInfo.TWD_Flags == TypeFlags.IsEnum)
                {
                    output += "ENUM  " + this.TypeInfo.TWD_Type_FullName + " [ ] {";
                    int nrV = this.EnumValues.Count;
                    for(int i = 0; i < nrV; i++)
                    {
                        output += this.enum_values[i] + ", ";
                    }
                    output = output.Substring(0, output.Length - 2);
                    output += "}";
                }
                else
                {
                    output += "CLASS " + this.TypeInfo.TWD_Type_FullName + " [";
                    foreach(TypeWithDescription entry in this.base_TWD)
                    {
                        output += entry.ToString() + ", ";
                    }
                    output = output.Substring(0, output.Length - 2);
                    output += "] [";
                    int nrP = this.property_names.Count;
                    for (int i = 0; i < nrP; i++)
                    {
                        output +=   this.property_ids[i] + "|" +
                                    this.property_names[i] + ":" + 
                                    this.property_TWD[i].ToString() + "(" + 
                                    this.property_init_values[i] + "), ";
                    }
                    output = output.Substring(0, output.Length - 2);
                    output += "]";
                }

                return output;
            }

            #endregion

            #region STATIC: Parsing Class Records from String

            private static readonly string SYMB_ENUM = "ENUM  ";
            private static readonly string SYMB_CLASS = "CLASS ";
            private static readonly string[] SECTION_DELIMITERS =
                new string[] { "[", "{", "][", "] [", "]{", "] {", "}[", "} [" };
            private static readonly Regex SPACES = new Regex(@"^\s+$");

            public static ClassRecord ReadFromString(string _record, List<TypeWithDescription> _cutom_types)
            {
                if (_record == null || _record.Length < SYMB_CLASS.Length) return null;

                // extract the ID
                int ind_id_1 = _record.IndexOf('|');
                if (ind_id_1 < 0 || ind_id_1 > _record.Length - 1) return null;
                string[] record_split1 = new string[2];
                record_split1[0] = _record.Substring(0, ind_id_1);
                record_split1[1] = _record.Substring(ind_id_1 + 1);
                
                long id = -1;
                bool success_parsing_id = long.TryParse(record_split1[0], out id);
                if (!success_parsing_id) return null;

                // determine TYPE: ENUM or CLASS
                string start = record_split1[1].Substring(0, SYMB_CLASS.Length);
                string body = record_split1[1].Substring(SYMB_CLASS.Length);
                bool is_enum;
                if (start == SYMB_CLASS)
                    is_enum = false;
                else if (start == SYMB_ENUM)
                    is_enum = true;
                else
                    return null;

                // split into SECTIONS
                string[] body_sections = body.Split(SECTION_DELIMITERS, StringSplitOptions.RemoveEmptyEntries);
                if (body_sections.Length < 3) return null;
                string section_name = body_sections[0];
                string section_base = body_sections[1];
                string section_props_vals = body_sections[2].Replace("]", string.Empty).Replace("}", string.Empty);

                // extract NAME and NAMESPACE
                string[] name_components = section_name.Split(new string[] {".", " "}, StringSplitOptions.RemoveEmptyEntries);
                if (name_components == null) return null;
                if (name_components.Length < 2) return null;

                TypeFlags flags = (is_enum) ? TypeFlags.IsEnum : TypeFlags.IsSimpleValueType;
                ClassRecord cr = new ClassRecord(id, name_components[1], name_components[0], flags);

                // extract BASE TYPES
                string[] base_components = section_base.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                if (base_components != null && base_components.Length > 0 && cr.TypeInfo.TWD_Flags != TypeFlags.IsEnum)
                {
                    foreach (string entry in base_components)
                    {
                        TypeWithDescription base_type = TypeWithDescription.ParseStringWFlags(entry, _cutom_types);
                        if (base_type != null)
                            cr.AddBaseType(base_type);
                    }
                }

                // extract PROPERTIES (class) or VALUES (enum)
                string[] props_vals_comp = section_props_vals.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                if (props_vals_comp != null && props_vals_comp.Length > 0)
                {
                    // extract NAMES, TYPES and VALUES
                    List<long> prop_ids = new List<long>();
                    List<string> prop_names = new List<string>();
                    List<TypeWithDescription> prop_types = new List<TypeWithDescription>();
                    List<string> prop_vals = new List<string>();
                    foreach(var entry in props_vals_comp)
                    {
                        string[] entry_comps = entry.Split(new string[] { "|", ":", "(", ")" }, StringSplitOptions.RemoveEmptyEntries);
                        if (entry_comps == null) continue;

                        if (entry_comps.Length == 4)
                        {
                            long prop_id = -1;
                            bool success_parsing_prop_id = long.TryParse(entry_comps[0], out prop_id);
                            if (!success_parsing_id) continue;

                            prop_ids.Add(prop_id);
                            prop_names.Add(entry_comps[1].Trim());
                            prop_types.Add(TypeWithDescription.ParseStringWFlags(entry_comps[2], _cutom_types));
                            prop_vals.Add(entry_comps[3].Trim());
                        }
                        else
                        {
                            prop_vals.Add(entry_comps[0].Trim());
                        }
                    }

                    // add PROPERTIES
                    int nrV = prop_vals.Count;
                    if (cr.TypeInfo.TWD_Flags == TypeFlags.IsEnum)
                    {
                        for (int i = 0; i < nrV; i++)
                        {
                            cr.AddEnumValue(prop_vals[i]);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < nrV; i++)
                        {
                            cr.AddProperty(prop_ids[i], prop_names[i], prop_types[i], prop_vals[i]);
                        }
                    }
                }

                // extract DEPENDENCIES btw PROPERTIES
                // TODO ...
                
                return cr;
            }

            #endregion
        }
        #endregion

        #endregion

        #region CLASS MEMBERS

        private ClassRecord class_candidate;
        private Dictionary<string, ClassRecord> created_classes;
        public List<string> ClassRecords { get { return this.created_classes.Values.Select(x => x.ToString()).ToList(); } }

        private List<TypeWithDescription> custom_types;

        #endregion

        #region .CTOR
        public ClassGenerator()
        {
            this.created_classes = new Dictionary<string, ClassRecord>();
            this.custom_types = new List<TypeWithDescription>();
        }

        #endregion

        #region CLASS Generation

        // simple node w 1 layer of subnodes
        public bool GenerateClassText(Node _n, string _namespace)
        {
            if (_n == null || string.IsNullOrEmpty(_namespace)) return false;

            // get class name
            string class_name = ClassGenerator.ExtractName(_n, false);
            if (string.IsNullOrEmpty(class_name)) return false;
            if (this.created_classes.ContainsKey(class_name)) return false;

            // start the class record
            this.class_candidate = new ClassRecord(_n.ID, class_name, _namespace, TypeFlags.IsNotValid);

            List<TypeWithDescription> base_types = TypeWithDescription.ExtractTypeDescr(_namespace, _n.NodeUnit, this.custom_types);
            if (base_types.Count < 1)
                base_types.Add(TypeWithDescription.TYPES_SIMPLE_DEFAULT["object"]);            
            foreach (var entry in base_types)
                this.class_candidate.AddBaseType(entry);           

            // assemble text
            if (this.class_candidate.TypeInfo.TWD_Flags == TypeFlags.IsEnum)
                this.GenerateClassText_Enum(_n);
            else
                this.GenerateClassText_Class(_n, _namespace);

            return true;
        }

        private void GenerateClassText_Class(Node _n, string _namespace)
        {
            this.Snippets = new List<string>();

            string file_start = ClassGenerator.AssembleClassStart(this.class_candidate.TypeInfo.TWD_Type_Name, 
                                                                  this.class_candidate.TypeInfo.TWD_Type_Namespace,
                                                                  this.class_candidate.BaseTWD);
            this.ClassText += file_start;
            this.Snippets.Add(file_start);

            string impl_INPC = ClassGenerator.AssembleINotifyPropertyChanged();
            this.ClassText += impl_INPC;
            this.Snippets.Add(impl_INPC);

            // assemble property text
            if (_n.ContainedNodes.Count > 0)
            {
                foreach (Node sN in _n.ContainedNodes)
                {
                    string prop = this.GenerateProperty(sN, _namespace);
                    if (string.IsNullOrEmpty(prop)) continue;

                    this.ClassText += prop;
                    this.Snippets.Add(prop);
                }
            }

            string impl_Ctor0 = ClassGenerator.AssembleClassDefaultCtor(this.class_candidate);
            this.ClassText += impl_Ctor0;
            this.Snippets.Add(impl_Ctor0);

            string file_end = ClassGenerator.AssembleClassEnd();
            this.ClassText += file_end;
            this.Snippets.Add(file_end);
        }

        private void GenerateClassText_Enum(Node _n)
        {
            this.Snippets = new List<string>();
            List<string> enum_candidate_values = ClassGenerator.ExtractValueList(_n);
            if (enum_candidate_values != null && enum_candidate_values.Count > 0)
            {
                foreach(string val in enum_candidate_values)
                {
                    this.class_candidate.AddEnumValue(val);
                }
            }
            string text = ClassGenerator.AssembleEnum(this.class_candidate.TypeInfo.TWD_Type_Name, 
                                                      this.class_candidate.TypeInfo.TWD_Type_Namespace, 
                                                      this.class_candidate.EnumValues.ToList());
            this.ClassText += text;
            this.Snippets.Add(text);
        }

        #endregion

        #region CLASS RECORD MANAGEMENT

        public bool SaveClassRecord()
        {
            if (!this.created_classes.ContainsKey(this.class_candidate.TypeInfo.TWD_Type_FullName))
            {
                this.created_classes.Add(this.class_candidate.TypeInfo.TWD_Type_FullName, this.class_candidate);
                
                // save the resulting type
                if (!this.custom_types.Contains(this.class_candidate.TypeInfo))
                    this.custom_types.Add(this.class_candidate.TypeInfo);

                return true;
            }

            return false;
        }

        public void WriteClassRecords(FileStream _fs)
        {
            if (_fs == null || !_fs.CanWrite) return;

            foreach (ClassRecord record in this.created_classes.Values)
            {
                byte[] content_B = System.Text.Encoding.UTF8.GetBytes(record.ToString() + Environment.NewLine);
                _fs.Write(content_B, 0, content_B.Length);
                _fs.Flush();
            }

        }

        public bool ReadClassRecord(string _record)
        {
            ClassRecord record_new = ClassRecord.ReadFromString(_record, this.custom_types);
            if (!this.created_classes.ContainsKey(record_new.TypeInfo.TWD_Type_FullName))
            {
                this.created_classes.Add(record_new.TypeInfo.TWD_Type_FullName, record_new);

                // save the resulting type
                if (!this.custom_types.Contains(record_new.TypeInfo))
                    this.custom_types.Add(record_new.TypeInfo);

                return true;
            }

            return false;
        }

        public void ClearClassRecords()
        {
            this.created_classes.Clear();
            this.custom_types.Clear();
        }

        #endregion

        #region Propery Generation

        public string GenerateProperty(Node _n, string _namespace)
        {
            string prop_name = ClassGenerator.ExtractName(_n);
            if (string.IsNullOrEmpty(prop_name)) return string.Empty;

            List<TypeWithDescription> types = TypeWithDescription.ExtractTypeDescr(_namespace, _n.NodeUnit, this.custom_types);
            if (types == null || types.Count != 1) return string.Empty;
            TypeWithDescription type = types[0];
            string value = ClassGenerator.ExtractSingleValue(_n);
            if (string.IsNullOrEmpty(value))
                value = this.GetDefaultValue(type);

            if (this.class_candidate != null && !this.class_candidate.PropertyNames.Contains(prop_name))
            {
                this.class_candidate.AddProperty(_n.ID, prop_name, type, value);
                return ClassGenerator.AssemblePropertySimple(prop_name, type.TWD_Type_FullName);
            }

            return string.Empty;
        }

        #endregion

        #region UTILS: Name extraction

        public static string ExtractClassName(Node _n)
        {
            return ClassGenerator.ExtractName(_n, false);
        }

        private static string ExtractName(Node _n, bool _property = true)
        {
            if (_n == null) return string.Empty;

            string name = (_n.NodeDescr.Count() == 0) ? _n.NodeName : _n.NodeName + "_" + _n.NodeDescr;
            name = name.Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue").Replace("ß", "ss").Replace(" ", "_").Replace("__", "_");
            name = ClassGenerator.CleanString(name);

            if (!_property)
            {
                string[] name_comps = name.Split(new string[]{" ", "_"}, StringSplitOptions.RemoveEmptyEntries);
                if (name_comps.Length > 0)
                {
                    string nameClass = "";
                    foreach(string entry in name_comps)
                    {
                        nameClass += CultureInfo.CurrentCulture.TextInfo.ToTitleCase(entry);
                    }
                    name = nameClass;
                }
            }

            return name;
        }

        private static string CleanString(string _input, bool _is_numeric = false)
        {
            if (string.IsNullOrEmpty(_input)) return string.Empty;

            Regex regex_start_symb, regex_other_symb;

            if (_is_numeric)
            {
                regex_start_symb = new Regex(@"^_");
                regex_other_symb = new Regex(@"[^a-zA-Z0-9<>_.-]");
            }
            else
            {
                regex_start_symb = new Regex(@"^_");
                regex_other_symb = new Regex(@"[^a-zA-Z0-9<>_]");
            }

            string output = regex_start_symb.Replace(_input, string.Empty);
            output = regex_other_symb.Replace(output, string.Empty);

            return output;
        }

        #endregion

        #region UTILS: Value extraction

        private static List<string> ExtractValueList(Node _n)
        {
            List<string> single_values = new List<string>();
            if (_n == null) return single_values;

            string values_as_str = _n.NodeDefaultVal;
            if (string.IsNullOrEmpty(values_as_str)) return single_values;

            string[] values = values_as_str.Split(new string[] { ",", " " }, StringSplitOptions.RemoveEmptyEntries);
            if (values != null && values.Length > 0)
            {
                foreach(string entry in values)
                {
                    string clean_enry = ClassGenerator.CleanString(entry, true);
                    if (!string.IsNullOrEmpty(clean_enry))
                        single_values.Add(clean_enry);
                }
            }

            return single_values;
        }

        private static string ExtractSingleValue(Node _n)
        {
            string value = string.Empty;
            if (_n == null) return value;

            string values_as_str = _n.NodeDefaultVal;
            if (string.IsNullOrEmpty(values_as_str)) return value;

            value = values_as_str.Trim();
            return value;
        }

        private string GetDefaultValue(TypeWithDescription _type)
        {
            string def_val = ClassGenerator.GetStandardDefaultValue(_type.TWD_Type);

            if (def_val == ClassGenerator.NOTHING)
            {
                // check the custom types...
                TypeWithDescription found = this.custom_types.Find(x => x.TWD_Type_FullName == _type.TWD_Type_FullName);
                if (found != null && found.TWD_Flags.HasFlag(TypeFlags.IsEnum))
                {

                }
            }

            return def_val;
        }

        private static string GetStandardDefaultValue(Type _type)
        {
            if (_type == null) return ClassGenerator.NOTHING;

            string def_val = string.Empty;

            if (_type == typeof(double))
                def_val = default(double).ToString();
            if (_type == typeof(float))
                def_val = default(float).ToString();
            else if (_type == typeof(int))
                def_val = default(int).ToString();
            else if (_type == typeof(long))
                def_val = default(long).ToString();
            else if (_type == typeof(string))
                def_val = ClassGenerator.EMPTY_STRING;
            else
                def_val = ClassGenerator.NOTHING;

            return def_val;
        }

        #endregion

        #region UTILS: Text Snippets

        private static string AssembleClassStart(string _class_name, string _namespace, IReadOnlyCollection<TypeWithDescription> _base_types)
        {
            if (string.IsNullOrEmpty(_class_name) || string.IsNullOrEmpty(_namespace) || _base_types == null)
                return string.Empty;

            string base_types = " : INotifyPropertyChanged";
            if (_base_types.Count > 0)
            {
                foreach(TypeWithDescription entry in _base_types)
                {
                    base_types += ", " + entry.TWD_Type_FullName;
                }
            }

            string text =
@"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace " + _namespace +
@"
{
    public class " + _class_name + base_types +
@"
    {
";
            return text;
        }

        private static string AssembleINotifyPropertyChanged()
        {
            string text =
@"
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RegisterPropertyChanged(string _propName)
        {
            if (_propName == null)
                return;

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(_propName));
        }

        #endregion
";
            return text;
        }

        private static string AssembleClassDefaultCtor(string _class_name)
        {
            if (string.IsNullOrEmpty(_class_name)) return string.Empty;

            string text =
@"
        public " + _class_name + @"()
        {
        }
";
            return text;
        }

        private static string AssembleClassDefaultCtor(ClassRecord _record)
        {
            if (_record == null) return string.Empty;
            if (_record.TypeInfo.TWD_Flags == TypeFlags.IsEnum) return string.Empty;

            List<string> property_names = _record.PropertyNames.ToList();
            List<TypeWithDescription> property_TWD = _record.PropertyTWD.ToList();
            List<string> property_init_values = _record.PropertyInitVals.ToList();


            string text =
@"
        public " + _record.TypeInfo.TWD_Type_Name + @"()
        {
";
            int n = property_names.Count;
            for (int i = 0; i < n; i++ )
            {
                if (property_TWD[i].TWD_Type == typeof(string))
                {
                    string actual_val = (property_init_values[i] == ClassGenerator.EMPTY_STRING) ? "" : property_init_values[i];
                    text +=
@"          this." + property_names[i] + @" = """ + actual_val + @""";
";
                }
                else
                {
                    string actual_val = string.Empty;
                    if (property_init_values[i] == ClassGenerator.NOTHING)
                    {
                        if ((property_TWD[i].TWD_Flags.HasFlag(TypeFlags.CanAddValue)) ||
                            (property_TWD[i].TWD_Flags.HasFlag(TypeFlags.CanPushValue)) ||
                            (property_TWD[i].TWD_Flags.HasFlag(TypeFlags.CanEnqueue)))
                        {
                            actual_val = "new " + TypeWithDescription.CSharpName(property_TWD[i], true) + "()";
                        }
                        else if (property_TWD[i].TWD_Flags.HasFlag(TypeFlags.CanAssignNull))
                        {
                            actual_val = "null";
                        }
                        else if (property_TWD[i].TWD_Flags.HasFlag(TypeFlags.CanAssignValue))
                        {
                            actual_val = "0";
                        }
                        else if (property_TWD[i].TWD_Flags.HasFlag(TypeFlags.IsEnum))
                        {
                            actual_val = (property_TWD[i].TWD_Values.Count > 0 ) ? property_TWD[i].TWD_Type_FullName + "." + property_TWD[i].TWD_Values[0] : "0";
                        }
                    }
                    else
                    {
                        if (property_TWD[i].TWD_Flags.HasFlag(TypeFlags.CanAddValue))
                        {
                            bool contiained_type_is_string = false;
                            if (property_TWD[i].TWD_Type.GenericTypeArguments.Length > 0)
                            {
                                Type contained_type = property_TWD[i].TWD_Type.GenericTypeArguments[0];
                                contiained_type_is_string = (contained_type == typeof(string));
                            }
                            if (contiained_type_is_string)
                                actual_val = "new " + TypeWithDescription.CSharpName(property_TWD[i], true) + "{ \"" + property_init_values[i] + "\" }";
                            else
                                actual_val = "new " + TypeWithDescription.CSharpName(property_TWD[i], true) + "{ " + property_init_values[i] + " }";                           
                        }
                        else if ((property_TWD[i].TWD_Flags.HasFlag(TypeFlags.CanPushValue)) ||
                                 (property_TWD[i].TWD_Flags.HasFlag(TypeFlags.CanEnqueue)))
                        {
                            bool contiained_type_is_string = false;
                            Type contained_type = typeof(int);
                            if (property_TWD[i].TWD_Type.GenericTypeArguments.Length > 0)
                            {
                                contained_type = property_TWD[i].TWD_Type.GenericTypeArguments[0];
                                contiained_type_is_string = (contained_type == typeof(string));
                            }
                            
                            if (contiained_type_is_string)
                                actual_val = "new " + TypeWithDescription.CSharpName(property_TWD[i], true) + "( new string[] { \"" + property_init_values[i] + "\" })";
                            else
                                actual_val = "new " + TypeWithDescription.CSharpName(property_TWD[i], true) + "( new " + contained_type.FullName + "[] { " + property_init_values[i] + " })";

                            // var test1 = new Queue<string>(new [] { "hi" });
                            // var test2 = new Stack<string>(new string[] { "hi" });
                        }
                        else if (property_TWD[i].TWD_Flags.HasFlag(TypeFlags.CanAssignValue))
                        {
                            actual_val = property_init_values[i];
                        }
                        else if (property_TWD[i].TWD_Flags.HasFlag(TypeFlags.IsEnum))
                        {
                            if (property_TWD[i].TWD_Values.Contains(property_init_values[i]))
                                actual_val = property_TWD[i].TWD_Type_FullName + "." + property_init_values[i];
                            else
                                actual_val = (property_TWD[i].TWD_Values.Count > 0) ? property_TWD[i].TWD_Type_FullName + "." + property_TWD[i].TWD_Values[0] : "0";                            
                        }
                    }

                    text +=
@"          this." + property_names[i] + " = " + actual_val + @";
";
                }
            }
            text +=
@"
        }";

            return text;
        }

        private static string AssembleClassEnd()
        {
            string text =
@"
    }
}
";
            return text;
        }

        private static string AssemblePropertySimple(string _prop_name, string _prop_type)
        {
            if (string.IsNullOrEmpty(_prop_name) || string.IsNullOrEmpty(_prop_type))
                return string.Empty;

            string content_prop =
@"
        protected " + _prop_type + " " + _prop_name.ToLower() + @";
        public " + _prop_type + " " + _prop_name + @"
        {
            get { return this." + _prop_name.ToLower() + @"; }
            protected set 
            {
                this." + _prop_name.ToLower() + @" = value;
                this.RegisterPropertyChanged(""" + _prop_name + @""");                
            }
        }
";
            return content_prop;
        }


        private static string AssembleEnum(string _enum_name, string _namespace, List<string> _values)
        {
            if (string.IsNullOrEmpty(_enum_name) || string.IsNullOrEmpty(_namespace) || _values == null)
                return string.Empty;

            string text =
@"
namespace " + _namespace +
@"
{
    public enum " + _enum_name + 
@"
    {";
            foreach(string entry in _values)
            {
                text += 
@"
        " + entry + ",";
            }
            text = text.Substring(0, text.Length - 1);

            text +=
@"
    }
}
";

            return text;
        }


        #endregion

    }
}
