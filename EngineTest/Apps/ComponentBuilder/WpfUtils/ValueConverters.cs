using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Controls;
using System.ComponentModel;

using ParameterStructure.Values;
using ParameterStructure.Parameter;
using ParameterStructure.Component;
using ParameterStructure.Geometry;

namespace ComponentBuilder.WpfUtils
{
    [ValueConversion(typeof(double), typeof(string))]
    public class DoubleToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            System.IFormatProvider format;
            if (culture != null)
                format = culture.NumberFormat;
            else
            {
                System.Globalization.NumberFormatInfo f_inv = new System.Globalization.NumberFormatInfo();
                f_inv.NumberGroupSeparator = " ";
                format = f_inv;               
            }

            string number_format_string = "### ### ##0.00";
            
            if (parameter != null)
            {
                Regex format_detector = new Regex(@"\bF[0-9]{1}\b");
                if (format_detector.IsMatch(parameter.ToString()))
                {
                    number_format_string = parameter.ToString();
                }
            }

            double d = 0.0;
            if (value is double)
                d = (double)value;
            if (double.IsNaN(d))
                return "NaN";
            else if (double.IsPositiveInfinity(d) || d == double.MaxValue)
                return "+\U0000221E";
            else if (double.IsNegativeInfinity(d) || d == double.MinValue)
                return "-\U0000221E";
            else
                return d.ToString(number_format_string, format);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string input = value.ToString();
            System.IFormatProvider format;
            if (culture != null)
                format = culture.NumberFormat;
            else
                format = new System.Globalization.NumberFormatInfo();

            double output = 0.0;
            bool success = Double.TryParse(input, System.Globalization.NumberStyles.Float, format, out output);

            return output;
        }
    }

    // this converter is for double input, where the textbox does not show 0.00 as initial value
    [ValueConversion(typeof(string), typeof(string))]
    public class DoubleStringToStringConverter : IValueConverter
    {
        private static readonly string DEFAULT_FORMAT = "F2";
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            System.IFormatProvider format;
            if (culture != null)
                format = culture.NumberFormat;
            else
                format = new System.Globalization.NumberFormatInfo();

            string valAsString = "0";
            if (parameter == null)
                valAsString = string.Format(format, "{0:" + DEFAULT_FORMAT + "}", value);
            else
                valAsString = string.Format(format, "{0:" + parameter.ToString() + "}", value);

            double input = 0.0;
            bool success = Double.TryParse(valAsString, NumberStyles.Float, format, out input);
            if (success)
                if (parameter == null)
                    return input.ToString(DEFAULT_FORMAT, format);
                else
                    return input.ToString(parameter.ToString(), format);
            else
                return "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            System.IFormatProvider format;
            if (culture != null)
                format = culture.NumberFormat;
            else
                format = new System.Globalization.NumberFormatInfo();

            string valAsString = "0";
            if (parameter == null)
                valAsString = string.Format(format, "{0:" + DEFAULT_FORMAT + "}", value);
            else
                valAsString = string.Format(format, "{0:" + parameter.ToString() + "}", value);

            double output = 0.0;
            bool success = Double.TryParse(valAsString, System.Globalization.NumberStyles.Float, format, out output);
            if (success)
                if (parameter == null)
                    return output.ToString(DEFAULT_FORMAT, format);
                else
                    return output.ToString(parameter.ToString(), format);
            else
                return "0";
        }
    }

    [ValueConversion(typeof(string), typeof(string))]
    public class StringToStringWoSpacesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            return this.RemoveSpacesAndUnderlines(value.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            return this.RemoveSpacesAndUnderlines(value.ToString());
        }

        private string RemoveSpacesAndUnderlines(string _input)
        {
            if (string.IsNullOrEmpty(_input)) return string.Empty;

            string output = _input.Replace(" ", string.Empty);
            output = output.Replace("_", string.Empty);
            return output;
        }
    }

    [ValueConversion(typeof(string), typeof(int))]
    public class InputToIntConverter : IValueConverter
    {
        private static readonly Regex INT = new Regex("^[0-9]{1,32}$");
        private static readonly Regex TO_REMOVE = new Regex(@"[^0-9.,]");
        private static readonly NumberFormatInfo NR_FORMATTER = new System.Globalization.NumberFormatInfo();
        private static readonly string DEFAULT_FORMAT = "F2";
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return this.HandleConversion(value, parameter, culture);
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return this.HandleConversion(value, parameter, culture);
        }

        private object HandleConversion(object value, object parameter, CultureInfo culture)
        {
            if (value == null) return 0;

            System.IFormatProvider format;
            if (culture != null)
                format = culture.NumberFormat;
            else
                format = NR_FORMATTER;

            string valAsString = "0";
            if (parameter == null)
                valAsString = string.Format(format, "{0:" + DEFAULT_FORMAT + "}", value);
            else
                valAsString = string.Format(format, "{0:" + parameter.ToString() + "}", value);

            string adapted = TO_REMOVE.Replace(valAsString, string.Empty);

            double input = 0.0;
            bool success = Double.TryParse(adapted, NumberStyles.Float, format, out input);
            if (success)
                return (int)input;
            else
                return 0;
        }
    }

    [ValueConversion(typeof(Boolean), typeof(System.Windows.Media.Color))]
    public class BoolToWinMediaColorWOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return (Color)ColorConverter.ConvertFromString("#00aaaaaa");

            if (value is bool)
            {
                if ((bool)value)
                    return (Color)ColorConverter.ConvertFromString("#00aaaaaa");
                else
                    return (Color)ColorConverter.ConvertFromString("#ffff4500");
            }
            else
            {
                return (Color)ColorConverter.ConvertFromString("#00aaaaaa");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }

    [ValueConversion(typeof(Boolean), typeof(System.Windows.Media.Color))]
    public class BoolToWinMediaColorWOpacityConverter1 : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return (Color)ColorConverter.ConvertFromString("#aaffffff");

            if (value is bool)
            {
                if ((bool)value)
                    return (Color)ColorConverter.ConvertFromString("#aaffffff");
                else
                    return (Color)ColorConverter.ConvertFromString("#66ff4500");
            }
            else
            {
                return (Color)ColorConverter.ConvertFromString("#aaffffff");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }

    [ValueConversion(typeof(Boolean), typeof(System.Windows.Media.Color))]
    public class BoolToWinMediaColorWOpacityConverter2 : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return (Color)ColorConverter.ConvertFromString("#ff000000");

            if (value is bool)
            {
                if ((bool)value)
                    return (Color)ColorConverter.ConvertFromString("#ff666666");
                else
                    return (Color)ColorConverter.ConvertFromString("#ff000000");
            }
            else
            {
                return (Color)ColorConverter.ConvertFromString("#ff000000");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }

    [ValueConversion(typeof(Boolean), typeof(System.Windows.Media.Color))]
    public class BoolToWinMediaColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return (Color)ColorConverter.ConvertFromString("#ff555555");

            if (value is bool)
            {
                if ((bool)value)
                    return (Color)ColorConverter.ConvertFromString("#ff000000");
                else
                    return (Color)ColorConverter.ConvertFromString("#ffff4500");
            }
            else
            {
                return (Color)ColorConverter.ConvertFromString("#ff555555");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }

    [ValueConversion(typeof(Boolean), typeof(double))]
    public class BoolToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return 0.0;

            if (value is bool)
            {
                if ((bool)value)
                    return 1.0;
                else
                    return 0.0;
            }
            else
            {
                return 0.0;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }

    [ValueConversion(typeof(Boolean), typeof(double))]
    public class BoolToOpacityConverter1 : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return 0.0;

            if (value is bool)
            {
                if ((bool)value)
                    return 0.1;
                else
                    return 0.4;
            }
            else
            {
                return 0.0;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }

    [ValueConversion(typeof(int), typeof(System.Windows.Media.Color))]
    public class IntToWinMediaColorFormCompSlotsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return (Color)ColorConverter.ConvertFromString("#33ffffff");

            if (value is int)
            {
                if ((int)value == 0)
                    return (Color)ColorConverter.ConvertFromString("#330000ff");
                else
                    return (Color)ColorConverter.ConvertFromString("#66e88522");
            }
            else
            {
                return (Color)ColorConverter.ConvertFromString("#33ffffff");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }

    [ValueConversion(typeof(Boolean), typeof(Boolean))]
    public class InverseBooleanConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

    }

    [ValueConversion(typeof(Boolean), typeof(System.Windows.Visibility))]
    public class BooleanToVisibiltyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                if (value is bool)
                {
                    bool visible = (bool)value;
                    if (visible)
                        return System.Windows.Visibility.Visible;
                    else
                        return System.Windows.Visibility.Collapsed;
                }
            }
            return System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                if (value is System.Windows.Visibility)
                {
                    System.Windows.Visibility vis = (System.Windows.Visibility)value;
                    if (vis == System.Windows.Visibility.Visible)
                        return true;
                    else
                        return false;
                }
            }
            return false;
        }
    }

    [ValueConversion(typeof(Boolean), typeof(double))]
    public class BooleanToRadiusSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                if (value is bool)
                {
                    if ((bool)value)
                        return 10.0; // 7
                    else
                        return 2.0;
                }
            }
            return 2.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(Type), typeof(Boolean))]
    public class TypeToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;

            if (parameter == null)
                return true;

            string str_param = parameter.ToString();
            string[] str_params_OR = str_param.Split(new char[] { '+' });

            if (str_params_OR.Count() < 2)
            {
                if (value.GetType().ToString() == str_param)
                    return true;
                else
                    return false;
            }
            else
            {
                foreach (string strType in str_params_OR)
                {
                    if (value.GetType().ToString() == strType)
                        return true;
                }
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new NotImplementedException();
        }
    }

    [ValueConversion(typeof(int), typeof(Boolean))]
    public class IntToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return false;
            if (!(value is int)) return false;

            int value_as_int = (int)value;
            if (value_as_int == 0)
                return false;
            else
                return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return 0;
            if (!(value is bool)) return 0;

            bool value_as_bool = (bool)value;

            if (value_as_bool)
                return 1;
            else
                return 0;
        }
    }

    public class BoolAndConverter : IMultiValueConverter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="values">boolean input</param>
        /// <param name="targetType"></param>
        /// <param name="parameter">true = inverse input, false = keep input as is</param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values != null && values.Count() > 0)
            {
                bool inverse = false;
                if (parameter != null)
                    inverse = (bool)parameter;

                int n = values.Count();
                bool result = true;
                for (int i = 0; i < n; i++)
                {
                    if (!(values[i] is bool))
                    {
                        result &= false;
                        continue; 
                    }

                    if (inverse)
                        result &= !(bool)values[i];
                    else
                        result &= (bool)values[i];
                }
                return result;
            }
            else
            {
                return null;
            }
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if (value != null && targetTypes != null && targetTypes.Count() > 0)
            {
                bool inverse = false;
                if (parameter != null)
                    inverse = (bool)parameter;

                int n = targetTypes.Count();
                object[] result = new object[n];
                for (int i = 0; i < n; i++)
                {
                    if (!(value is bool))
                    {
                        result[i] = false;
                        continue; 
                    }

                    if (inverse)
                        result[i] = !(bool)value;
                    else
                        result[i] = (bool)value;
                }
                return result;
            }
            else
            {
                return null;
            }
        }
    }

    [ValueConversion(typeof(object), typeof(Boolean))]
    public class ObjectToIsNullAsBoolenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;
            else
                return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }

    // tree view 
    [ValueConversion(typeof(TreeViewItem), typeof(Thickness))]
    public class TreeDepthToOffsetConverter : IValueConverter
    {
        public double Factor { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as TreeViewItem;
            if (item == null)
                return new Thickness(0);

            return new Thickness(0, 0, Factor * item.GetDepth(), 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }

    [ValueConversion(typeof(TreeViewItem), typeof(Double))]
    public class TreeDepthToLengthConverter : IValueConverter
    {
        public double InitLength { get; set; }
        public double Factor { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as TreeViewItem;
            if (item == null)
                return 2;

            var test = InitLength + Factor * item.GetDepth();
            return Math.Max(2, InitLength + Factor * item.GetDepth());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }

    public class TreeViewGroupingAccToParameterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            if (parameter == null) return value;

            // retrieve collection to sort
            System.Collections.IList collection = value as System.Collections.IList;
            if (collection == null || collection.Count < 2) return value;

            // retrieve property name according to which to group
            string grouping_param = parameter.ToString();
            if (string.IsNullOrEmpty(grouping_param)) return value;

            // perform the grouping
            ListCollectionView view = new ListCollectionView(collection);
            PropertyGroupDescription pd = new PropertyGroupDescription(grouping_param);
            view.GroupDescriptions.Add(pd);
            view.SortDescriptions.Add(new SortDescription("CurrentSlot", ListSortDirection.Ascending));
            view.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            view.SortDescriptions.Add(new SortDescription("Description", ListSortDirection.Ascending));

            return view;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }

    public class ListViewGroupingAccToMulitValueTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;

            // retrieve collection to sort
            System.Collections.IList collection = value as System.Collections.IList;
            if (collection == null || collection.Count < 2) return value;

            // perform the grouping
            ListCollectionView view = new ListCollectionView(collection);
            PropertyGroupDescription pd = new PropertyGroupDescription("MVType");
            view.GroupDescriptions.Add(pd);
            view.SortDescriptions.Add(new SortDescription("MVType", ListSortDirection.Ascending));
            view.SortDescriptions.Add(new SortDescription("MVID", ListSortDirection.Ascending));

            return view;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }

    // app specific
    [ValueConversion(typeof(GuiModus), typeof(Boolean))]
    public class GuiModusToBooleanConverter : IValueConverter
    {
        // in order to react to more than one zone edit mode at once
        // use + as OR
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            GuiModus guim = GuiModus.NEUTRAL;
            if (value is GuiModus)
                guim = (GuiModus)value;

            string str_param = parameter.ToString();
            string[] str_params_OR = str_param.Split(new char[] { '+' });

            if (str_params_OR.Count() < 2)
                return (guim == GuiModusUtils.StringToGuiModus(str_param));
            else
            {
                foreach (string p in str_params_OR)
                {
                    if (guim == GuiModusUtils.StringToGuiModus(p))
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

    [ValueConversion(typeof(GuiModus), typeof(string))]
    public class GuiModusToStringConverter : IValueConverter
    {
        // in order to react to more than one zone edit mode at once
        // use + as OR
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;

            GuiModus guim = GuiModus.NEUTRAL;
            if (value is GuiModus)
                guim = (GuiModus)value;

            return GuiModusUtils.GuiModusToString(guim);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;

            string guim_str = value.ToString();
            return GuiModusUtils.StringToGuiModus(guim_str);
        }

    }

    [ValueConversion(typeof(Relation2GeomState), typeof(Boolean))]
    public class Relation2GeomTypeToBooleanConverter : IValueConverter
    {
        // in order to react to more than one type mode at once
        // use + as OR
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            Relation2GeomType r2gt = Relation2GeomType.NONE;
            if (value is Relation2GeomState)
                r2gt = ((Relation2GeomState)value).Type;

            string str_param = parameter.ToString();
            string[] str_params_OR = str_param.Split(new char[] { '+' });

            if (str_params_OR.Count() < 2)
                return (r2gt == GeometryUtils.StringToRelationship2Geometry(str_param));
            else
            {
                foreach (string p in str_params_OR)
                {
                    if (r2gt == GeometryUtils.StringToRelationship2Geometry(p))
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

    [ValueConversion(typeof(Relation2GeomState), typeof(string))]
    public class Relation2GeomTypeToStringConverter : IValueConverter
    {
        // in order to react to more than one type at once
        // use + as OR
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;

            Relation2GeomType r2gt = Relation2GeomType.NONE;
            if (value is Relation2GeomState)
                r2gt = ((Relation2GeomState)value).Type;

            return GeometryUtils.Relationship2GeometryToDescrDE(r2gt);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;

            string r2gt_str = value.ToString();
            return GeometryUtils.StringToRelationship2Geometry(r2gt_str);
        }

    }

    // VALUE specific
    [ValueConversion(typeof(MultiValue), typeof(string))]
    public class MultiValueToStringConverter : IValueConverter
    {
        // in order to react to more than one zone edit mode at once
        // use + as OR
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return ";";
            if (!(value is MultiValue)) return ";";

            MultiValue mv = value as MultiValue;
            return mv.GetSymbol();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }

    // PARAMETER specific
    [ValueConversion(typeof(Type), typeof(Boolean))]
    public class IsParameterToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;

            return (value is ParameterStructure.Parameter.Parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new NotImplementedException();
        }
    }

    [ValueConversion(typeof(Type), typeof(Boolean))]
    public class IsCalculationToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;

            return (value is ParameterStructure.Parameter.Calculation);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new NotImplementedException();
        }
    }

    [ValueConversion(typeof(DateTime), typeof(System.Windows.Media.Color))]
    public class DateTimeToWinMediaColorConverter : IValueConverter
    {
        public double MinutesFromNow { get; set; }
        public double MinutesFromNow_Fast { get; set; }
        public DateTimeToWinMediaColorConverter()
        {
            this.MinutesFromNow = 1;
            this.MinutesFromNow_Fast = 0.1;
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return (Color)ColorConverter.ConvertFromString("#33000000");
            if (!(value is DateTime)) return (Color)ColorConverter.ConvertFromString("#33000000");

            DateTime dt = (DateTime)value;
            TimeSpan x = DateTime.Now - dt;
            if (x.TotalMinutes > this.MinutesFromNow)
                return (Color)ColorConverter.ConvertFromString("#33000000");
            else if (x.TotalMinutes > this.MinutesFromNow_Fast)
                return (Color)ColorConverter.ConvertFromString("#55FF4500");
            else
                return (Color)ColorConverter.ConvertFromString("#FFFF4500");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }

    [ValueConversion(typeof(Category), typeof(string))]
    public class CategoryToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return ComponentUtils.CATEGORY_NONE_AS_STR;

            if (!(value is Category)) return ComponentUtils.CATEGORY_NONE_AS_STR;
            Category val_as_cat = (Category)value;
            return ComponentUtils.CategoryToString(val_as_cat);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return Category.NoNe;

            string val_as_str = value.ToString();
            if (string.IsNullOrEmpty(val_as_str)) return Category.NoNe;

            return ComponentUtils.StringToCategory(val_as_str);
        }

    }

    [ValueConversion(typeof(Category), typeof(string))]
    public class CategoryToSparseStringConverter : IValueConverter
    {
        private static Regex SMALL_LETTERS = new Regex("[a-z]");
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            if (!(value is Category)) return string.Empty;

            Category val_as_cat = (Category)value;
            string full = ComponentUtils.CategoryToString(val_as_cat);
            string sparse = SMALL_LETTERS.Replace(full, string.Empty);

            return sparse;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }

    [ValueConversion(typeof(Category), typeof(string))]
    public class CategoryToLetterConverter : IValueConverter
    {        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return "a";

            if (!(value is Category)) return "a";
            Category val_as_cat = (Category)value;
            string val_as_cat_str = ComponentUtils.CategoryToString(val_as_cat);

            int index = -1;
            int nrCat = Enum.GetNames(typeof(Category)).Length - 2;
            bool success = int.TryParse(parameter.ToString(), out index);
            if (!success) return "a";

            if (index < 0 || (nrCat - 1) < index) return "a";
            
            return val_as_cat_str[index];            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }

    public class LetterCollectionToCategoryConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            string category_NaN_as_str = ComponentUtils.CATEGORY_NONE_AS_STR;
            int nrCat = Enum.GetNames(typeof(Category)).Length - 2;

            if (values == null) return ComponentUtils.StringToCategory(category_NaN_as_str);
            if (values.Count() != nrCat) return ComponentUtils.StringToCategory(category_NaN_as_str);

            string cat_str_from_values = string.Empty;
            foreach(object v in values)
            {
                if (v == null) 
                    return ComponentUtils.StringToCategory(category_NaN_as_str);

                string v_as_str = v.ToString();
                if (string.IsNullOrEmpty(v_as_str) || v_as_str.Length != 1) 
                    return ComponentUtils.StringToCategory(category_NaN_as_str);

                cat_str_from_values += v_as_str;
            }

            return ComponentUtils.StringToCategory(cat_str_from_values);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(InfoFlow), typeof(string))]
    public class InfoFlowToLetterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "@";
            if (!(value is InfoFlow)) return "@";

            return ComponentUtils.InfoFlowToString((InfoFlow)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return InfoFlow.MIXED;
            return ComponentUtils.StringToInfoFlow(value.ToString());
        }

    }

    [ValueConversion(typeof(InfoFlow), typeof(System.Windows.Media.Color))]
    public class InfoFlowToWMColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return (Color)ColorConverter.ConvertFromString("#ff000000");
            if (!(value is InfoFlow)) return (Color)ColorConverter.ConvertFromString("#ff000000");

            string vLetter = ComponentUtils.InfoFlowToString((InfoFlow)value);
            string pLetter = parameter.ToString();

            if(vLetter == pLetter)
                return (Color)ColorConverter.ConvertFromString("#ff0000ff");
            else
                return (Color)ColorConverter.ConvertFromString("#ff000000");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new System.NotSupportedException();
        }

    }

    [ValueConversion(typeof(InfoFlow), typeof(System.Windows.Media.Color))]
    public class InfoFlowToMultiWMColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return (Color)ColorConverter.ConvertFromString("#ff000000");
            if (!(value is InfoFlow)) return (Color)ColorConverter.ConvertFromString("#ff000000");

            InfoFlow infl = (InfoFlow)value;

            switch (infl)
            {
                case InfoFlow.INPUT:
                    return (Color)ColorConverter.ConvertFromString("#ff0000ff");
                case InfoFlow.CALC_IN:
                    return (Color)ColorConverter.ConvertFromString("#ff88004b"); // #ffb35900 (deep orange) #ff88004b (light purple) #ff570056 (deep purple)
                default:
                    return (Color)ColorConverter.ConvertFromString("#ff000000");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new System.NotSupportedException();
        }

    }

    [ValueConversion(typeof(ComparisonResult), typeof(System.Windows.Media.Color))]
    public class CompResultToMultiWMColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return (Color)ColorConverter.ConvertFromString("#00ffffff");
            if (!(value is ComparisonResult)) return (Color)ColorConverter.ConvertFromString("#00ffffff");

            ComparisonResult cr = (ComparisonResult)value;

            switch (cr)
            {
                case ComparisonResult.UNIQUE:
                    return (Color)ColorConverter.ConvertFromString("#00ffffff");
                case ComparisonResult.SAMENAME_DIFFUNIT:
                    return (Color)ColorConverter.ConvertFromString("#33b35900");
                case ComparisonResult.SAMENAMEUNIT_DIFFVALUE:
                    return (Color)ColorConverter.ConvertFromString("#33ff0000");
                case ComparisonResult.SAME:
                    return (Color)ColorConverter.ConvertFromString("#3300ff00");
                default:
                    return (Color)ColorConverter.ConvertFromString("#00ffffff");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new System.NotSupportedException();
        }

    }

    public class ObjectCollectionToParameterConverter : IMultiValueConverter
    {
        // 0: Parameter in edit mode, represented by a ParameterDummy
        // 1: Category
        // 2: Propagation
        // 3: Name
        // 4: Unit
        // 5: ValueMin
        // 6: ValueMax
        // 7: ValueCurrent
        // 8: TextValue
        // 9: ValueField   
        //10: The Current Mode of the Application
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null) return null;
            int nrVals = values.Count();
            if (nrVals < 11) return null;

            // 10. Mode
            if (!(values[10] is GuiModus)) return null;

            GuiModus guim = (GuiModus)values[10];
            bool guim_valid = true;
            if (parameter != null)
            {
                string str_param = parameter.ToString();
                string[] str_params_OR = str_param.Split(new char[] { '+' });

                if (str_params_OR.Count() < 2)
                    guim_valid = (guim == GuiModusUtils.StringToGuiModus(str_param));
                else
                {
                    guim_valid = false;
                    foreach (string p in str_params_OR)
                    {
                        if (guim == GuiModusUtils.StringToGuiModus(p))
                        {
                            guim_valid = true;
                            break;
                        }
                    }                 
                }
            }
            if (!guim_valid) return null;

            System.IFormatProvider format = new System.Globalization.NumberFormatInfo();

            // 0: ParameterDummy
            if (values[0] == null) return null;
            ParameterDummy param = values[0] as ParameterDummy;
            if (param == null) return null;

            // 1: Category
            if (values[1] is Category)
                param.Category = (Category)values[1];

            // 2: Propagation
            if (values[2] == null) return null; // added 24.08.2016
            param.Propagation = ComponentUtils.StringToInfoFlow(values[2].ToString());

            // 3: Name
            if (values[3] != null)
                param.Name = values[3].ToString();

            // 4: Unit
            if (values[4] != null)
                param.Unit = values[4].ToString();

            // 5: ValueMin
            double val_min = double.NaN;
            if (values[5] == null) return null; // added 24.08.2016
            bool success = double.TryParse(values[5].ToString(), NumberStyles.Float, format, out val_min);
            if (success)
                param.ValueMin = val_min;

            // 6: ValueMax
            double val_max = double.NaN;
            if (values[6] == null) return null; // added 24.08.2016
            success = double.TryParse(values[6].ToString(), NumberStyles.Float, format, out val_max);
            if (success)
                param.ValueMax = val_max;

            // 7: ValueCurrent
            double val = double.NaN;
            if (values[7] == null) return null; // added 24.08.2016
            success = double.TryParse(values[7].ToString(), NumberStyles.Float, format, out val);
            if (success)
                param.ValueCurrent = val;

            // 8: TextValue
            if (values[8] != null)
                param.TextValue = values[8].ToString();

            // 9: ValueField
            MultiValue field = values[9] as MultiValue;
            if (field != null)
                param.ValueField = field;

            return param;           
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    // COMPONENT specific
    [ValueConversion(typeof(Type), typeof(Boolean))]
    public class IsComponentToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;

            return (value is ParameterStructure.Component.Component);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new NotImplementedException();
        }
    }

    [ValueConversion(typeof(Type), typeof(Boolean))]
    public class IsVisibleComponentToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;

            if (value is ParameterStructure.Component.Component)
            {
                ParameterStructure.Component.Component comp = value as ParameterStructure.Component.Component;
                return !(comp.IsHidden || comp.HideDetails);
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new NotImplementedException();
        }
    }

    [ValueConversion(typeof(Type), typeof(Boolean))]
    public class IsNotCalculationToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;

            return (value is ParameterStructure.Parameter.Parameter || value is ParameterStructure.Component.Component || 
                    value is ParameterStructure.Geometry.Point3DContainer);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new NotImplementedException();
        }
    }

    [ValueConversion(typeof(ComponentManagerType), typeof(string))]
    public class CompManagerTypeToLetterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return ComponentUtils.ComponentManagerTypeToLetter(ComponentManagerType.GUEST);

            if (!(value is ComponentManagerType)) return ComponentUtils.ComponentManagerTypeToLetter(ComponentManagerType.GUEST);
            ComponentManagerType val_as_cmt = (ComponentManagerType)value;
            return ComponentUtils.ComponentManagerTypeToLetter(val_as_cmt);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }

    [ValueConversion(typeof(ComponentManagerType), typeof(bool))]
    public class CompManagerTypeToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            if (!(value is ComponentManagerType)) return false;
            ComponentManagerType val_as_cmt = (ComponentManagerType)value;
            return ComponentUtils.ComponentManagerTypeToLetter(val_as_cmt) == parameter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }

    [ValueConversion(typeof(ComponentManagerType), typeof(string))]
    public class ComponentManagerTypeToDescrConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return ComponentUtils.ComponentManagerTypeToDescrDE(ComponentManagerType.GUEST);

            if (!(value is ComponentManagerType)) ComponentUtils.ComponentManagerTypeToDescrDE(ComponentManagerType.GUEST);
            ComponentManagerType val_as_cmt = (ComponentManagerType)value;
            return ComponentUtils.ComponentManagerTypeToDescrDE(val_as_cmt);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }

    [ValueConversion(typeof(ComponentValidity), typeof(string))]
    public class ComponentValidityToDescrConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return ComponentUtils.ComponentValidityToDescrDE(ComponentValidity.NOT_CALCULATED);

            if (!(value is ComponentValidity)) ComponentUtils.ComponentValidityToDescrDE(ComponentValidity.NOT_CALCULATED);
            ComponentValidity val_as_CV = (ComponentValidity)value;
            return ComponentUtils.ComponentValidityToDescrDE(val_as_CV);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }

    [ValueConversion(typeof(ComponentValidity), typeof(System.Windows.Media.Color))]
    public class ComponentValidityToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return ComponentUtils.ComponentValidityToColor(ComponentValidity.NOT_CALCULATED);

            if (!(value is ComponentValidity)) ComponentUtils.ComponentValidityToColor(ComponentValidity.NOT_CALCULATED);
            ComponentValidity val_as_CV = (ComponentValidity)value;
            return ComponentUtils.ComponentValidityToColor(val_as_CV);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }

    [ValueConversion(typeof(bool), typeof(int))]
    public class SlotSizeToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool))
                return 1;

            bool is_large = (bool)value;
            if (is_large)
                return 6;
            else
                return 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new NotImplementedException();
        }
    }

    // NETWORK specific
    [ValueConversion(typeof(Type), typeof(Boolean))]
    public class IsNetworkToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;

            return (value is ParameterStructure.Component.FlowNetwork);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new NotImplementedException();
        }
    }

    [ValueConversion(typeof(Point), typeof(string))]
    public class PointToStringConverter : IValueConverter
    {
        private static System.IFormatProvider NR_FORMATTER = new System.Globalization.NumberFormatInfo();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            if (value is Point)
            {
                Point p = (Point)value;
                return "(" + p.X.ToString("F2", NR_FORMATTER) + ", " + p.Y.ToString("F2", NR_FORMATTER) + ")";
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new NotImplementedException();
        }
    }

    [ValueConversion(typeof(Boolean), typeof(System.Windows.Media.Color))]
    public class ValidityToWinMediaColorConverter : IValueConverter
    {
        private static Color VALID = (Color)ColorConverter.ConvertFromString("#ff000000");
        private static Color INVALID = (Color)ColorConverter.ConvertFromString("#ff888888");
        private static Color UNKNOWN = (Color)ColorConverter.ConvertFromString("#ffcccccc");
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return UNKNOWN;

            if (value is bool)
            {
                if ((bool)value)
                    return VALID;
                else
                    return INVALID;
            }
            else
            {
                return UNKNOWN;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }

}
