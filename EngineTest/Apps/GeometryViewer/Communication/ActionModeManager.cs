using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Data;
using System.Globalization;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;

namespace GeometryViewer.Communication
{
    public enum ActionType 
    {
        NO_ACTION = 0,

        OPTIONS = 1,
        LINE_DRAW = 2, 
        LINE_EDIT = 3,

        MODERATOR = 4,
        ENERGY_NETWORK_OPERATOR = 5,
	    EENERGY_SUPPLIER = 6,
	    BUILDING_DEVELOPER = 7,
	    BUILDING_OPERATOR = 8,
        ARCHITECTURE = 9,
        ARCHITECTURE_SELECT = 10,
        FIRE_SAFETY = 11,
        BUILDING_PHYSICS = 12,
        BUILDING_PHYSICS_SELECT = 13,
        MEP_HVAC = 14,
	    PROCESS_MEASURING_CONTROL = 15,
        SPACES_OPENINGS = 16,

        NAVI_LOOK_AT = 17,
    }
    class ActionModeManager : GeometryModel3D
    {
        private static int NR_STATES;
        static ActionModeManager()
        {
            NR_STATES = Enum.GetNames(typeof(ActionType)).Length;
        }

        private List<bool> setActions;
        public ActionModeManager()
        {
            this.setActions = Enumerable.Repeat(false, NR_STATES).ToList();
        }

        public ActionType SetAction(ActionType _at)
        {
            int ind_action = (int)_at;

            if (ind_action == 0)
            {
                this.setActions = Enumerable.Repeat(false, NR_STATES).ToList();
                return ActionType.NO_ACTION;
            }

            bool state_New = !(this.setActions[ind_action]);
            this.setActions = Enumerable.Repeat(false, NR_STATES).ToList();
            this.setActions[ind_action] = state_New;
            if (state_New)
                return (ActionType)ind_action;
            else
                return ActionType.NO_ACTION;
        }

        public ActionType SetAction(string _str_at)
        {
            if (_str_at == null)
                return ActionType.NO_ACTION;

            ActionType at = GetActionType(_str_at);
            return SetAction(at);
        }

        public static ActionType GetActionType(string _type)
        {
            if (_type == null)
                return ActionType.NO_ACTION;

            switch(_type)
            {
                case "OPTIONS":
                    return ActionType.OPTIONS;
                case "LINE_DRAW":
                    return ActionType.LINE_DRAW;
                case "LINE_EDIT":
                    return ActionType.LINE_EDIT;
                case "MODERATOR":
                    return ActionType.MODERATOR;
                case "ENERGY_NETWORK_OPERATOR":
                    return ActionType.ENERGY_NETWORK_OPERATOR;
                case "EENERGY_SUPPLIER":
                    return ActionType.EENERGY_SUPPLIER;
                case "BUILDING_DEVELOPER":
                    return ActionType.BUILDING_DEVELOPER;
                case "BUILDING_OPERATOR":
                    return ActionType.BUILDING_OPERATOR;
                case "ARCHITECTURE":
                    return ActionType.ARCHITECTURE;
                case "ARCHITECTURE_SELECT":
                    return ActionType.ARCHITECTURE_SELECT;
                case "FIRE_SAFETY":
                    return ActionType.FIRE_SAFETY;
                case "BUILDING_PHYSICS":
                    return ActionType.BUILDING_PHYSICS;
                case "BUILDING_PHYSICS_SELECT":
                    return ActionType.BUILDING_PHYSICS_SELECT;
                case "MEP_HVAC":
                    return ActionType.MEP_HVAC;
                case "PROCESS_MEASURING_CONTROL":
                    return ActionType.PROCESS_MEASURING_CONTROL;
                case "SPACES_OPENINGS":
                    return ActionType.SPACES_OPENINGS;
                case "NAVI_LOOK_AT":
                    return ActionType.NAVI_LOOK_AT;              
                default:
                    return ActionType.NO_ACTION;
            }
        }
    }

    public class ActionToBooleanConverter : IValueConverter
    {
        // in order to react to more than one action mode at once
        // use + as OR; * as AND
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            ActionType at = ActionType.NO_ACTION;
            if (value is ActionType)
                at = (ActionType)value;

            string str_param = parameter.ToString();

            // debug
            //if (str_param == "LINE_DRAW+POINT_PLACE+NAVI_LOOK_AT")
            //{
            //    var test = str_param.Count();
            //}

            string[] str_params_OR = str_param.Split(new char[] { '+' });

            if(str_params_OR.Count() < 2)
                return (at == ActionModeManager.GetActionType(str_param));
            else
            {
                foreach(string p in str_params_OR)
                {
                    if (at == ActionModeManager.GetActionType(p))
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
