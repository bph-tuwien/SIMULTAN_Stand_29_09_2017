using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows.Data;
using System.Globalization;

namespace GeometryViewer.Communication
{
    public enum ZoneEditType
    { 
        NO_EDIT = 0,
        POLYGON_VERTEX = 1,
        POLYGON_OPENING = 2,
        VOLUME_CREATE = 3,
        VOLUME_CREATE_COMPLEX = 4,
        VOLUME_SURFACE = 5,
        VOLUME_EDGE = 6,
        VOLUME_VERTEX = 7,
        VOLUME_PICK = 8,
        POLYGON_SPLIT = 9,
    }

    public class ZoneEditModeManager
    {
        private static int NR_EDIT_MODES = 10; // BAD DESING: but do not forget!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!1

        private List<bool> setModes;

        public ZoneEditModeManager()
        {
            this.setModes = Enumerable.Repeat(false, NR_EDIT_MODES).ToList();
        }

        public ZoneEditType SetEditMode(ZoneEditType _zet)
        {
            int ind_mode = (int)_zet;

            if (ind_mode == 0)
            {
                this.setModes = Enumerable.Repeat(false, NR_EDIT_MODES).ToList();
                return ZoneEditType.NO_EDIT;
            }

            bool mode_New = !(this.setModes[ind_mode]);
            this.setModes = Enumerable.Repeat(false, NR_EDIT_MODES).ToList();
            this.setModes[ind_mode] = mode_New;
            if (mode_New)
                return (ZoneEditType)ind_mode;
            else
                return ZoneEditType.NO_EDIT;
        }

        public ZoneEditType SetEditMode(string _str_zet)
        {
            if (_str_zet == null)
                return ZoneEditType.NO_EDIT;

            ZoneEditType zet = GetEditModeType(_str_zet);
            return SetEditMode(zet);
        }

        public static ZoneEditType GetEditModeType(string _type)
        {
            if (_type == null)
                return ZoneEditType.NO_EDIT;

            switch (_type)
            {
                case "POLYGON_VERTEX":
                    return ZoneEditType.POLYGON_VERTEX;
                case "POLYGON_OPENING":
                    return ZoneEditType.POLYGON_OPENING;
                case "VOLUME_CREATE":
                    return ZoneEditType.VOLUME_CREATE;
                case "VOLUME_CREATE_COMPLEX":
                    return ZoneEditType.VOLUME_CREATE_COMPLEX;
                case "VOLUME_SURFACE":
                    return ZoneEditType.VOLUME_SURFACE;
                case "VOLUME_EDGE":
                    return ZoneEditType.VOLUME_EDGE;
                case "VOLUME_VERTEX":
                    return ZoneEditType.VOLUME_VERTEX;
                case "VOLUME_PICK":
                    return ZoneEditType.VOLUME_PICK;
                case "POLYGON_SPLIT":
                    return ZoneEditType.POLYGON_SPLIT;
                default:
                    return ZoneEditType.NO_EDIT;
            }
        }

    }

    [ValueConversion(typeof(ZoneEditType), typeof(Boolean))]
    public class ZoneEditTypeToBooleanConverter : IValueConverter
    {
        // in order to react to more than one zone edit mode at once
        // use + as OR
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            ZoneEditType zet = ZoneEditType.NO_EDIT;
            if (value is ZoneEditType)
                zet = (ZoneEditType)value;

            string str_param = parameter.ToString();
            string[] str_params_OR = str_param.Split(new char[] { '+' });

            if (str_params_OR.Count() < 2)
                return (zet == ZoneEditModeManager.GetEditModeType(str_param));
            else
            {
                foreach (string p in str_params_OR)
                {
                    if (zet == ZoneEditModeManager.GetEditModeType(p))
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
