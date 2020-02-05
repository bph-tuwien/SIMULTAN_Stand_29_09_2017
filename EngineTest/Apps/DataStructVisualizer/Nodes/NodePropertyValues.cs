using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;

namespace DataStructVisualizer.Nodes
{
    public enum EditMode { NONE, CONNECT, DISCONNECT, SHOW_CONN, TOUR_RUNNING }
    
    public enum NodeManagerType
    {
        NONE = 0, // light grey
        REGULATOR = 1, // red
        MODERATOR = 2, // dark red
        ENERGY_NETWORK_OPERATOR = 3, // dark orange
        EENERGY_SUPPLIER = 4, // orange
        BUILDING_DEVELOPER = 5, // dark green
        BUILDING_OPERATOR = 6, // green
        ARCHITECTURE = 7, // light blue
        FIRE_SAFETY = 8, // blue
        BUILDING_PHYSICS = 9, // dark blue
        MEP_HVAC = 10, // darker blue
        PROCESS_MEASURING_CONTROL = 11, // lilac
        BUILDING_CONTRACTOR = 12, // grey
        MANAGER_OF_SUPERIOR_NODE = 13 // dark grey
    }

    public enum ParameterType { NONE, IN, IN_DONE, OUT, OUT_DONE, DEFINITION, REFERENCE, METHOD }

    #region COST
    public enum NodeCostType_NORM
    {
        GRD = 0, // 0 Grund
        AUF = 1, // 1 Aufschliessung
        BWR = 2, // 2 Bauwerk - Rohbau
        BWT = 3, // 3 Bauwerk - Technik
        BWA = 4, // 4 Bauwerk - Ausbau
        EIR = 5, // 5 Einrichtung
        AAN = 6, // 6 Aussenanlagen
        PLL = 7, // 7 Planungsleistungen
        NEL = 8, // 8 Nebenleistungen
        RES = 9 //  9 Reserven
    }

    public enum NodeCostType_BD
    {
        GEST = 0, // Gestehungskosten
        NEKO = 1, // Nebenkosten
        BEKO = 2, // Betriebskosten
        WAKO = 3 // Wartungskosten
    }

    public enum NodeCostType_Frequency
    {
        EINMAL = 0, // Einmalkosten
        MEHRFACH = 1 // Mehrfachkosten
    }

    #endregion

    #region AREAS VOLUMES
    public enum NodeAreaType_NORM
    {
        BGF = 0, // Brutto-Grundfl
        UGF = 1, // unverwendbare Grundfl
        
        BGF_IGF = 2, // Innen-Grundfl
        BGF_AKF = 3, // Aussenwand-Kostruktionsgrundfl
        
        BGF_IGF_NGF = 4, // Netto-Grundfl
        BGF_IGF_IKG = 5, // Innenwand-Kosntruktionsgrundfl

        BGF_IGF_NGF_NRF = 6, // Netto-Raumfl
        BGF_IGF_NGF_TGF = 7, // Trennwand-Grundfl

        BGF_IGF_NGF_NRF_NF = 8, // Nutzfl
        BGF_IGF_NGF_NRF_SF = 9, // Sanitaerfl
        BGF_IGF_NGF_NRF_TF = 10, // Technikfl
        BGF_IGF_NGF_NRF_VF = 11 // Verkehrsfl
    }

    public enum NodeVolumeType_NORM
    {
        BRI = 0, // Brutto-Rauminhalt

        BRI_NRI = 1, // Netto-Rauminhalt
        BRI_KRI = 2, // Konstruktionsrauminhalt

        BRI_NRI_NF = 3, // Netto-Rauminhalt Nutzfl
        BRI_NRI_SF = 4, // Netto-Rauminhalt Sanitaerfl
        BRI_NRI_TF = 5, // Netto-Rauminhalt Technikfl
        BRI_NRI_VF = 6 // Netto-Rauminhalt Verkehrsfl
    }

    public enum NodeEnvelopeAreaType_NORM
    {
        AF = 0, // Aussenwand- u. Aussendeckenfl
        GF = 1, // Aussengrundfl
        DF = 2, // Dachfl
    }

    public enum NodeAreaType_BD
    {
        BGF = 0, // Brutto-Grundfl
        NGF = 1, // Netto-Grundfl

        NGF_gF = 2, // gefoerderte Fl
        NGF_aVF = 3, // allgemeine Verkehrsfl
        NGF_FF = 4, // Funktionsfl
        NGF_GaNGF = 5, // Garagen-Netto-Grundfl
        NGF_RNF = 6, // Rest-Nutzfl

        bIF = 7, // bewertete Infrastrukturfl
        Gesfl = 8, // Geschaeftsfl
    }

    public enum NodeVolumeType_BD
    {
        BRI = 0, // Brutto-Rauminhalt
        GaBRI = 1 // Garagen-Brutto-Rauminhalt
    }

    public enum NodeEnvelopeAreaType_BD
    {
        FAF = 0, // Fassadenfl
        FeTu = 1, // Fenster- u. Tuerenfl
        SoA = 2 // sonst. transp. Abschluesse
    }

    public enum NodeLengthType
    {
        PER = 0, // perimeter (im Raum)
        LWB = 1, // Laenge Waermebruecken
        LUA = 5, // Laenge d. Unterzuege u. Auskragungen
    }

    #endregion

    public static class NodePropertyValues
    {
        #region NodeManagerType Info

        public static List<NodeManagerType> GetListOfAllNodeManagerTypes()
        {
            return Enum.GetValues(typeof(NodeManagerType)).Cast<NodeManagerType>().ToList();
        }

        #endregion

        #region NodeManagerType to / from STRING Converters
        public static string NodeManagerTypeToString(NodeManagerType _type)
        {
            switch(_type)
            {
                case NodeManagerType.REGULATOR:
                    return "REGULATOR";
                case NodeManagerType.MODERATOR:
                    return "MODERATOR";
                case NodeManagerType.ENERGY_NETWORK_OPERATOR:
                    return "ENERGY_NETWORK_OPERATOR";
                case NodeManagerType.EENERGY_SUPPLIER:
                    return "EENERGY_SUPPLIER";
                case NodeManagerType.BUILDING_DEVELOPER:
                    return "BUILDING_DEVELOPER";
                case NodeManagerType.BUILDING_OPERATOR:
                    return "BUILDING_OPERATOR";
                case NodeManagerType.ARCHITECTURE:
                    return "ARCHITECTURE";
                case NodeManagerType.FIRE_SAFETY:
                    return "FIRE_SAFETY";
                case NodeManagerType.BUILDING_PHYSICS:
                    return "BUILDING_PHYSICS";
                case NodeManagerType.MEP_HVAC:
                    return "MEP_HVAC";
                case NodeManagerType.PROCESS_MEASURING_CONTROL:
                    return "PROCESS_MEASURING_CONTROL";
                case NodeManagerType.BUILDING_CONTRACTOR:
                    return "BUILDING_CONTRACTOR";
                case NodeManagerType.MANAGER_OF_SUPERIOR_NODE:
                    return "MANAGER_OF_SUPERIOR_NODE";
                default:
                    return "NONE";
            }
        }

        public static NodeManagerType StringToNodeManagerType(string _stype)
        {
            string str_type = _stype.Trim();
            switch (str_type)
            {
                case "REGULATOR":
                    return NodeManagerType.REGULATOR;
                case "MODERATOR":
                    return NodeManagerType.MODERATOR;
                case "ENERGY_NETWORK_OPERATOR":
                    return NodeManagerType.ENERGY_NETWORK_OPERATOR;
                case "EENERGY_SUPPLIER":
                    return NodeManagerType.EENERGY_SUPPLIER;
                case "BUILDING_DEVELOPER":
                    return NodeManagerType.BUILDING_DEVELOPER;
                case "BUILDING_OPERATOR":
                    return NodeManagerType.BUILDING_OPERATOR;
                case "ARCHITECTURE":
                    return NodeManagerType.ARCHITECTURE;
                case "FIRE_SAFETY":
                    return NodeManagerType.FIRE_SAFETY;
                case "BUILDING_PHYSICS":
                    return NodeManagerType.BUILDING_PHYSICS;
                case "MEP_HVAC":
                    return NodeManagerType.MEP_HVAC;
                case "PROCESS_MEASURING_CONTROL":
                    return NodeManagerType.PROCESS_MEASURING_CONTROL;
                case "BUILDING_CONTRACTOR":
                    return NodeManagerType.BUILDING_CONTRACTOR;
                case "MANAGER_OF_SUPERIOR_NODE":
                    return NodeManagerType.MANAGER_OF_SUPERIOR_NODE;
                default:
                    return NodeManagerType.NONE;
            }
        }


        public static string NodeManagerTypeToDescrDE(NodeManagerType _type)
        {
            switch (_type)
            {
                case NodeManagerType.REGULATOR:
                    return "Gesetzgeber";
                case NodeManagerType.MODERATOR:
                    return "Moderator";
                case NodeManagerType.ENERGY_NETWORK_OPERATOR:
                    return "Netzbetreiber";
                case NodeManagerType.EENERGY_SUPPLIER:
                    return "Energieversorger";
                case NodeManagerType.BUILDING_DEVELOPER:
                    return "Bauherr";
                case NodeManagerType.BUILDING_OPERATOR:
                    return "Betreiber";
                case NodeManagerType.ARCHITECTURE:
                    return "Architektur";
                case NodeManagerType.FIRE_SAFETY:
                    return "Brandschutzplanung";
                case NodeManagerType.BUILDING_PHYSICS:
                    return "Bauphysik";
                case NodeManagerType.MEP_HVAC:
                    return "Geb\u00E4udetechnik";
                case NodeManagerType.PROCESS_MEASURING_CONTROL:
                    return "MSR";
                case NodeManagerType.BUILDING_CONTRACTOR:
                    return "Ausfuehrende Firma";
                case NodeManagerType.MANAGER_OF_SUPERIOR_NODE:
                    return "siehe \u00FCbergeordnet";
                default:
                    return "nicht zugewiesen";
            }
        }
        #endregion

        #region NodeManagerType To COLOR Converter

        public static Color NodeManagerTypeToColor(NodeManagerType _type)
        {
            switch (_type)
            {
                case NodeManagerType.REGULATOR:
                    return (Color)ColorConverter.ConvertFromString("#ffff3600");
                case NodeManagerType.MODERATOR:
                    return (Color)ColorConverter.ConvertFromString("#ff8e1e00");
                case NodeManagerType.ENERGY_NETWORK_OPERATOR:
                    return (Color)ColorConverter.ConvertFromString("#ffa75a00");
                case NodeManagerType.EENERGY_SUPPLIER:
                    return (Color)ColorConverter.ConvertFromString("#ffff8a00");
                case NodeManagerType.BUILDING_DEVELOPER:
                    return (Color)ColorConverter.ConvertFromString("#ff346100");
                case NodeManagerType.BUILDING_OPERATOR:
                    return (Color)ColorConverter.ConvertFromString("#ff72d200");
                case NodeManagerType.ARCHITECTURE:
                    return (Color)ColorConverter.ConvertFromString("#ff00d8ff");
                case NodeManagerType.FIRE_SAFETY:
                    return (Color)ColorConverter.ConvertFromString("#ff0099b5");
                case NodeManagerType.BUILDING_PHYSICS:
                    return (Color)ColorConverter.ConvertFromString("#ff006577");
                case NodeManagerType.MEP_HVAC:
                    return (Color)ColorConverter.ConvertFromString("#ff0000ff");
                case NodeManagerType.PROCESS_MEASURING_CONTROL:
                    return (Color)ColorConverter.ConvertFromString("#ff43006a");
                case NodeManagerType.BUILDING_CONTRACTOR:
                    return (Color)ColorConverter.ConvertFromString("#ff555555");
                case NodeManagerType.MANAGER_OF_SUPERIOR_NODE:
                    return (Color)ColorConverter.ConvertFromString("#ff333333");
                default:
                    return (Color)ColorConverter.ConvertFromString("#ff888888");
            }
        }


        #endregion

        #region Node Edit Mode

        public static string EditModeToString(EditMode _mode)
        {
            switch (_mode)
            {
                case EditMode.CONNECT:
                    return "CONNECT";
                case EditMode.DISCONNECT:
                    return "DISCONNECT";
                case EditMode.SHOW_CONN:
                    return "SHOW_CONN";
                case EditMode.TOUR_RUNNING:
                    return "TOUR_RUNNING";
                default:
                    return "NONE";
            }
        }

        public static EditMode StringToEditMode(string _mode)
        {
            switch (_mode)
            {
                case "CONNECT":
                    return EditMode.CONNECT;
                case "DISCONNECT":
                    return EditMode.DISCONNECT;
                case "SHOW_CONN":
                    return EditMode.SHOW_CONN;
                case "TOUR_RUNNING":
                    return EditMode.TOUR_RUNNING;
                default:
                    return EditMode.NONE;
            }
        }

        #endregion

        #region PARAMETER TYPE

        public static string ParameterTypeToString(ParameterType _type)
        {
            switch (_type)
            {
                case ParameterType.IN:
                    return "IN";
                case ParameterType.IN_DONE:
                    return "IN_DONE";
                case ParameterType.OUT:
                    return "OUT";
                case ParameterType.OUT_DONE:
                    return "OUT_DONE";
                case ParameterType.DEFINITION:
                    return "DEFINITION";
                case ParameterType.REFERENCE:
                    return "REFERENCE";
                case ParameterType.METHOD:
                    return "METHOD";
                default:
                    return "NONE";
            }
        }

        public static ParameterType StringToParameterType(string _type)
        {
            switch (_type)
            {
                case "IN":
                    return ParameterType.IN;
                case "IN_DONE":
                    return ParameterType.IN_DONE;
                case "OUT":
                    return ParameterType.OUT;
                case "OUT_DONE":
                    return ParameterType.OUT_DONE;
                case "DEFINITION":
                    return ParameterType.DEFINITION;
                case "REFERENCE":
                    return ParameterType.REFERENCE;
                case "METHOD":
                    return ParameterType.METHOD;
                default:
                    return ParameterType.NONE;
            }
        }

        #endregion

    }

    [ValueConversion(typeof(NodeManagerType), typeof(string))]
    public class NodeManagerTypeToDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is NodeManagerType)
            {
                NodeManagerType nmt = (NodeManagerType)value;
                return NodePropertyValues.NodeManagerTypeToDescrDE(nmt);
            }
            else
                return "unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(NodeManagerType), typeof(System.Windows.Media.Color))]
    public class NodeManagerTypeToWinMediaColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is NodeManagerType)
            {
                NodeManagerType nmt = (NodeManagerType)value;
                return NodePropertyValues.NodeManagerTypeToColor(nmt);
            }
            else
                return (Color)ColorConverter.ConvertFromString("#ff000000");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(NodeManagerType), typeof(System.Windows.Media.Color))]
    public class NodeManagerTypeToBackgrWinMediaColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is NodeManagerType)
            {
                NodeManagerType nmt = (NodeManagerType)value;
               if (nmt == NodeManagerType.MANAGER_OF_SUPERIOR_NODE)
                   return (Color)ColorConverter.ConvertFromString("#55aaaaaa");
               else
                   return (Color)ColorConverter.ConvertFromString("#00000000");
            }
            else
                return (Color)ColorConverter.ConvertFromString("#00000000");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(System.Windows.Media.Color), typeof(System.Windows.Media.SolidColorBrush))]
    public class WinMediaColorToSolidColorBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Windows.Media.Color)
            {
                SolidColorBrush scb = new SolidColorBrush((System.Windows.Media.Color)value);
                scb.Opacity = 1.0;
                return scb;
            }
            else
            {
                SolidColorBrush scb = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ff000000"));
                scb.Opacity = 1.0;
                return scb;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(EditMode), typeof(Boolean))]
    public class EditModeToBooleanConverter : IValueConverter
    {
        // in order to react to more than one edit mode at once
        // use + as OR; * as AND
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            EditMode em = EditMode.NONE;
            if (value is EditMode)
                em = (EditMode)value;

            // OR - split
            string str_param = parameter.ToString();
            string[] str_params_OR = str_param.Split(new char[] { '+' });

            if (str_params_OR.Count() < 2)
                return (em == NodePropertyValues.StringToEditMode(str_param));
            else
            {
                foreach (string p in str_params_OR)
                {
                    if (em == NodePropertyValues.StringToEditMode(p))
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

    [ValueConversion(typeof(ParameterType), typeof(Boolean))]
    public class ParameterTypeToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            ParameterType pt = ParameterType.NONE;
            if (value is ParameterType)
                pt = (ParameterType)value;

            string str_param = parameter.ToString();
            return (pt == NodePropertyValues.StringToParameterType(str_param));          
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class MultiCondToSolidColorBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values != null && values.Count() > 1)
            {
                if (values[0] is Color)
                {
                    Color default_color = (Color)values[0];
                    SolidColorBrush scb;
                    if (values[1] != null && values[1].ToString() == Node.REFERENCE && 
                        default_color != Node.HIGHLIGHT_DEFAULT)
                    {
                        // turn blue if reference nut not highlighted
                        scb = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ff888888"));
                    }
                    else
                    {
                        scb = new SolidColorBrush(default_color);
                    }
                    scb.Opacity = 1.0;
                    return scb;
                }
            }
            
            SolidColorBrush scb_final = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ff000000"));
            scb_final.Opacity = 1.0;
            return scb_final;
            
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

}
