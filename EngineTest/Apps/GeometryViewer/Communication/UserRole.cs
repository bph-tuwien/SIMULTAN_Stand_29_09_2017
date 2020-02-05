using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;

namespace GeometryViewer.Communication
{
    // corresponds to the ComponentManagerType Enum

    public enum UserRole
    {
        ALL_RIGHTS = 0,         // corresponds to ADMINISTRATOR (@)
        DRAWING_RIGHTS = 1,     // corresponds to ARCHITECTURE (F)
        ZONING_RIGHTS = 2,      // corresponds to FIRE_SAFETY(G) or BUILDING_PHYSICS (H)
        INSERTION_RIGHTS = 3,   // corresponds to MEP_HVAC (I) or PROCESS_MEASURING_CONTROL (J)
        LAYER_VIS_RIGHTS = 4,   // corresponds to MODERATOR (A) or ENERGY_NETWORK_OPERATOR (B) or EENERGY_SUPPLIER (C)
                                // BUILDING_DEVELOPER (D) or BUILDING_OPERATOR (E)
        NO_RIGHTS = 5,          // corresponds to BUILDING_CONTRACTOR (K) or GUEST (0)
    }

    public class UserRoleUtils
    {
        public static UserRole TranslateUserRole(string _calling_user)
        {
            if (string.IsNullOrEmpty(_calling_user)) return UserRole.ALL_RIGHTS;

            switch(_calling_user)
            {
                case "@":
                    return UserRole.ALL_RIGHTS;
                case "A":
                case "B":
                case "C":
                case "D":
                case "E":
                    return UserRole.LAYER_VIS_RIGHTS;
                case "F":
                    return UserRole.DRAWING_RIGHTS;
                case "G":
                case "H":
                    return UserRole.ZONING_RIGHTS;
                case "I":
                case "J":
                    return UserRole.INSERTION_RIGHTS;
                case "K":
                case "0":
                    return UserRole.NO_RIGHTS;
                default:
                    // for starting as a stand-alone app
                    return UserRole.ALL_RIGHTS;
            }
        }

        public static UserRole StringtoUserRole(string _role)
        {
            if (string.IsNullOrEmpty(_role)) return UserRole.NO_RIGHTS;

            switch(_role)
            {
                case "ALL_RIGHTS":
                    return UserRole.ALL_RIGHTS;
                case "DRAWING_RIGHTS":
                    return UserRole.DRAWING_RIGHTS;
                case "ZONING_RIGHTS":
                    return UserRole.ZONING_RIGHTS;
                case "INSERTION_RIGHTS":
                    return UserRole.INSERTION_RIGHTS;
                case "LAYER_VIS_RIGHTS":
                    return UserRole.LAYER_VIS_RIGHTS;
                default:
                    return UserRole.NO_RIGHTS;
            }
        }

        public static string UserRoleToString(UserRole _role)
        {
            switch(_role)
            {
                case UserRole.ALL_RIGHTS:
                    return "ALL_RIGHTS";
                case UserRole.DRAWING_RIGHTS:
                    return "DRAWING_RIGHTS";
                case UserRole.ZONING_RIGHTS:
                    return "ZONING_RIGHTS";
                case UserRole.INSERTION_RIGHTS:
                    return "INSERTION_RIGHTS";
                case UserRole.LAYER_VIS_RIGHTS:
                    return "LAYER_VIS_RIGHTS";
                default:
                    return "NO_RIGHTS";
            }
        }


    }

    [ValueConversion(typeof(UserRole), typeof(Boolean))]
    public class UserRoleToBooleanConverter : IValueConverter
    {
        // in order to react to more than one zone edit mode at once
        // use + as OR
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            UserRole ur = UserRole.NO_RIGHTS;
            if (value is UserRole)
                ur = (UserRole)value;

            string str_param = parameter.ToString();
            string[] str_params_OR = str_param.Split(new char[] { '+' });

            if (str_params_OR.Count() < 2)
                return (ur == UserRoleUtils.StringtoUserRole(str_param));
            else
            {
                foreach (string p in str_params_OR)
                {
                    if (ur == UserRoleUtils.StringtoUserRole(p))
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
}
