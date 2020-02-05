using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComponentBuilder.WpfUtils
{
    public enum GuiModus
    {
        NEUTRAL = 1,                            // mode level 0
            
        PARAMETER_NEW = 2,                      // mode level 0
        PARAMETER_EDIT = 3,                     // mode level 0
        PARAMETER_INFO = 4,                     // mode level 0
        PARAMETER_PICK_VALUE_FIELD = 5,         // sub-mode of 2, 3: level 1

        CALC_NEW = 6,                           // mode level 0
        CALC_EDIT = 7,                          // mode level 0
        CALC_PICK_PARAMETER_IN = 8,             // sub-mode of 6, 7: level 1
        CALC_PICK_PARAMETER_OUT = 9,            // sub-mode of 6, 7: level 1

        COMPONENT_NEW = 10,                     // mode level 0
        COMPONENT_EDIT = 11,                    // mode level 0
        COMPONENT_INFO = 12,                    // mode level 0
        COMPONENT_PICK_PARAMETER = 13,          // sub-mode of 10, 11: level 1
        COMPONENT_EDIT_PARAMETER = 14,          // sub-mode of 10, 11: level 1
        COMPONENT_PICK_CALC = 15,               // sub-mode of 10, 11: level 1
        COMPONENT_EDIT_CALC = 16,               // sub-mode of 10, 11: level 1
        COMPONENT_CALC_PICK_PARAMETER_IN = 17,  // sub-mode of 15, 16: level 2
        COMPONENT_CALC_PICK_PARAMETER_OUT = 18, // sub-mode of 15, 16: level 2
        COMPONENT_PICK_COMPONENT = 19,          // sub-mode of 10, 11: level 1

        COMPONENT_PICK_SYMBOL = 20,             // sub-mode of 10, 11: level 1
        NETWORK_ELEM_PICK_COMP = 21,            // mode level 0
        COMP_COMPARER_PICK_COMP = 22,           // mode level 0
        COMP_TO_WS_MAPPER_PICK_COMP = 23,       // mode level 0
        COMP_TO_COMP_MAPPER_PICK_COMP = 24      // mode level 0
    }

    public static class GuiModusUtils
    {
        public static string GuiModusToString(GuiModus _gm)
        {
            switch(_gm)
            {
                case GuiModus.NEUTRAL:
                    return "NEUTRAL";
                case GuiModus.PARAMETER_NEW:
                    return "PARAMETER_NEW";
                case GuiModus.PARAMETER_EDIT:
                    return "PARAMETER_EDIT";
                case GuiModus.PARAMETER_INFO:
                    return "PARAMETER_INFO";
                case GuiModus.PARAMETER_PICK_VALUE_FIELD:
                    return "PARAMETER_PICK_VALUE_FIELD";
                case GuiModus.CALC_NEW:
                    return "CALC_NEW";
                case GuiModus.CALC_EDIT:
                    return "CALC_EDIT";
                case GuiModus.CALC_PICK_PARAMETER_IN:
                    return "CALC_PICK_PARAMETER_IN";
                case GuiModus.CALC_PICK_PARAMETER_OUT:
                    return "CALC_PICK_PARAMETER_OUT";
                case GuiModus.COMPONENT_NEW:
                    return "COMPONENT_NEW";
                case GuiModus.COMPONENT_EDIT:
                    return "COMPONENT_EDIT";
                case GuiModus.COMPONENT_INFO:
                    return "COMPONENT_INFO";
                case GuiModus.COMPONENT_PICK_PARAMETER:
                    return "COMPONENT_PICK_PARAMETER";
                case GuiModus.COMPONENT_EDIT_PARAMETER:
                    return "COMPONENT_EDIT_PARAMETER";
                case GuiModus.COMPONENT_PICK_CALC:
                    return "COMPONENT_PICK_CALC";
                case GuiModus.COMPONENT_EDIT_CALC:
                    return "COMPONENT_EDIT_CALC";
                case GuiModus.COMPONENT_CALC_PICK_PARAMETER_IN:
                    return "COMPONENT_CALC_PICK_PARAMETER_IN";
                case GuiModus.COMPONENT_CALC_PICK_PARAMETER_OUT:
                    return "COMPONENT_CALC_PICK_PARAMETER_OUT";
                case GuiModus.COMPONENT_PICK_COMPONENT:
                    return "COMPONENT_PICK_COMPONENT";
                case GuiModus.COMPONENT_PICK_SYMBOL:
                    return "COMPONENT_PICK_SYMBOL";
                case GuiModus.NETWORK_ELEM_PICK_COMP:
                    return "NETWORK_ELEM_PICK_COMP";
                case GuiModus.COMP_COMPARER_PICK_COMP:
                    return "COMP_COMPARER_PICK_COMP";
                case GuiModus.COMP_TO_WS_MAPPER_PICK_COMP:
                    return "COMP_TO_WS_MAPPER_PICK_COMP";
                case GuiModus.COMP_TO_COMP_MAPPER_PICK_COMP:
                    return "COMP_TO_COMP_MAPPER_PICK_COMP";
                default:
                    return "NEUTRAL";
            }
        }

        public static GuiModus StringToGuiModus(string _input)
        {
            if (string.IsNullOrEmpty(_input)) return GuiModus.NEUTRAL;

            switch(_input)
            {
                case "NEUTRAL":
                    return GuiModus.NEUTRAL;
                case "PARAMETER_NEW":
                    return GuiModus.PARAMETER_NEW;
                case "PARAMETER_EDIT":
                    return GuiModus.PARAMETER_EDIT;
                case "PARAMETER_INFO":
                    return GuiModus.PARAMETER_INFO;
                case "PARAMETER_PICK_VALUE_FIELD":
                    return GuiModus.PARAMETER_PICK_VALUE_FIELD;
                case "CALC_NEW":
                    return GuiModus.CALC_NEW;
                case "CALC_EDIT":
                    return GuiModus.CALC_EDIT;
                case "CALC_PICK_PARAMETER_IN":
                    return GuiModus.CALC_PICK_PARAMETER_IN;
                case "CALC_PICK_PARAMETER_OUT":
                    return GuiModus.CALC_PICK_PARAMETER_OUT;
                case "COMPONENT_NEW":
                    return GuiModus.COMPONENT_NEW;
                case "COMPONENT_EDIT":
                    return GuiModus.COMPONENT_EDIT;
                case "COMPONENT_INFO":
                    return GuiModus.COMPONENT_INFO;
                case "COMPONENT_PICK_PARAMETER":
                    return GuiModus.COMPONENT_PICK_PARAMETER;
                case "COMPONENT_EDIT_PARAMETER":
                    return GuiModus.COMPONENT_EDIT_PARAMETER;
                case "COMPONENT_PICK_CALC":
                    return GuiModus.COMPONENT_PICK_CALC;
                case "COMPONENT_EDIT_CALC":
                    return GuiModus.COMPONENT_EDIT_CALC;
                case "COMPONENT_CALC_PICK_PARAMETER_IN":
                    return GuiModus.COMPONENT_CALC_PICK_PARAMETER_IN;
                case "COMPONENT_CALC_PICK_PARAMETER_OUT":
                    return GuiModus.COMPONENT_CALC_PICK_PARAMETER_OUT;
                case "COMPONENT_PICK_COMPONENT":
                    return GuiModus.COMPONENT_PICK_COMPONENT;
                case "COMPONENT_PICK_SYMBOL":
                    return GuiModus.COMPONENT_PICK_SYMBOL;
                case "NETWORK_ELEM_PICK_COMP":
                    return GuiModus.NETWORK_ELEM_PICK_COMP;
                case "COMP_COMPARER_PICK_COMP":
                    return GuiModus.COMP_COMPARER_PICK_COMP;
                case "COMP_TO_WS_MAPPER_PICK_COMP":
                    return GuiModus.COMP_TO_WS_MAPPER_PICK_COMP;
                case "COMP_TO_COMP_MAPPER_PICK_COMP":
                    return GuiModus.COMP_TO_COMP_MAPPER_PICK_COMP;
                default:
                    return GuiModus.NEUTRAL;
            }
        }

        public static bool IsSubMode(GuiModus _mode)
        {
            return (_mode == GuiModus.PARAMETER_PICK_VALUE_FIELD ||
                    _mode == GuiModus.CALC_PICK_PARAMETER_IN ||
                    _mode == GuiModus.CALC_PICK_PARAMETER_OUT ||
                    _mode == GuiModus.COMPONENT_PICK_PARAMETER ||
                    _mode == GuiModus.COMPONENT_EDIT_PARAMETER ||
                    _mode == GuiModus.COMPONENT_PICK_CALC ||
                    _mode == GuiModus.COMPONENT_EDIT_CALC ||
                    _mode == GuiModus.COMPONENT_CALC_PICK_PARAMETER_IN ||
                    _mode == GuiModus.COMPONENT_CALC_PICK_PARAMETER_OUT ||
                    _mode == GuiModus.COMPONENT_PICK_COMPONENT ||
                    _mode == GuiModus.COMPONENT_PICK_SYMBOL);
        }

        public static bool IsSubModeOfSubMode(GuiModus _mode)
        {
            return (_mode == GuiModus.COMPONENT_CALC_PICK_PARAMETER_IN ||
                    _mode == GuiModus.COMPONENT_CALC_PICK_PARAMETER_OUT);
        }

        // works only for 3 Levels: Neutral -> Mode -> SubMode !!!
        public static void SwitchFromTo(ref GuiModus _old, ref GuiModus _current, GuiModus _new)
        {
            if (_current == _new)
            {
                if (GuiModusUtils.IsSubMode(_new))
                {
                    // returning from sub-mode
                    GuiModus tmp = _current;
                    _current = _old;
                    _old = tmp;
                }
                else
                {
                    // returning from mode
                    _old = _current;
                    _current = GuiModus.NEUTRAL;
                }
            }
            else
            {
                _old = _current;
                _current = _new;
            }
        }

        // works for an arbitrary depth
        // returns the current mode
        public static GuiModus SwitchMode(ref Stack<GuiModus> _prev, GuiModus _new)
        {
            if (_prev == null) return GuiModus.NEUTRAL;
            if (_prev.Count < 1) return GuiModus.NEUTRAL;

            GuiModus prev_l0 = _prev.Peek();
            if (prev_l0 == _new)
            {
                // switch to the mode 1 level deeper in the stack
                _prev.Pop();
                if (_prev.Count == 0)
                    return GuiModus.NEUTRAL;
                else
                    return _prev.Peek();
            }
            else
            {
                _prev.Push(_new);
                return _new;
            }
        }

    }
}
