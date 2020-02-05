using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;
using System.Data;
using System.Windows.Controls;
using System.ComponentModel;

using GeometryViewer.EntityGeometry;
using GeometryViewer.UIElements;

namespace GeometryViewer.Utils
{
    /// <summary>
    /// For debugging the data binding
    /// </summary>
    [ValueConversion(typeof(System.Windows.Media.Media3D.Point3D), typeof(System.Windows.Media.Media3D.Point3D))]
    public class Point3dToPoint3DConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    [ValueConversion(typeof(string), typeof(System.Windows.Media.Color))]
    public class StringToARGBColorConeverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return (Color)ColorConverter.ConvertFromString("Black");

            string input = value.ToString();
            int firstBr = input.IndexOf("[");
            int lastBr = input.LastIndexOf("]");

            if (firstBr != -1 && lastBr != -1)
            {
                try
                {
                    string str_color = input.Substring(firstBr + 1, lastBr - firstBr - 1);
                    return (Color)ColorConverter.ConvertFromString(str_color);
                }
                catch
                {
                    return (Color)ColorConverter.ConvertFromString("#FF666666");
                }
            }
            else
            {
                return (Color)ColorConverter.ConvertFromString("#FF666666");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }

    public class SharpDXColorToWinMediaColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return (Color)ColorConverter.ConvertFromString("Black");

            SharpDX.Color4 color;
            if (value is SharpDX.Color4)
                color = (SharpDX.Color4)value;
            else if (value is SharpDX.Color)
                color = (SharpDX.Color)value;
            else
                color = SharpDX.Color.Black;

            System.Windows.Media.Color swm_color = new Color();
            swm_color.A = (byte)(color.Alpha * 255);
            swm_color.R = (byte)(color.Red * 255);
            swm_color.G = (byte)(color.Green * 255);
            swm_color.B = (byte)(color.Blue * 255);

            return swm_color;
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
                return (Color)ColorConverter.ConvertFromString("Black");

            if (value is bool)
            {
                if ((bool)value)
                    return (Color)ColorConverter.ConvertFromString("Black");
                else
                    return (Color)ColorConverter.ConvertFromString("Red");
            }
            else
            {
                return (Color)ColorConverter.ConvertFromString("Black");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }

    [ValueConversion(typeof(Boolean), typeof(System.Windows.Media.Color))]
    public class BoolToWinMediaColorWOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return (Color)ColorConverter.ConvertFromString("#00000000");

            if (value is bool)
            {
                if ((bool)value)
                    return (Color)ColorConverter.ConvertFromString("#00000000");
                else
                    return (Color)ColorConverter.ConvertFromString("#ff000000");
            }
            else
            {
                return (Color)ColorConverter.ConvertFromString("#00000000");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }

    [ValueConversion(typeof(Boolean), typeof(System.Windows.Media.LinearGradientBrush))]
    public class BoolToLinearGradientBrushConverter : IValueConverter
    {
        private static readonly Color COL_TRUE_LIGHT = (Color)ColorConverter.ConvertFromString("#fff5f5f5");
        private static readonly Color COL_TRUE_DARK =  (Color)ColorConverter.ConvertFromString("#ffd9d9d9");

        private static readonly Color COL_FALSE_LIGHT = (Color)ColorConverter.ConvertFromString("#fffcff00");
        private static readonly Color COL_FALSE_DARK =  (Color)ColorConverter.ConvertFromString("#ffdddd98");

        private static readonly LinearGradientBrush TRUE_BRUSH =
            new LinearGradientBrush(COL_TRUE_DARK, COL_TRUE_LIGHT, new System.Windows.Point(0.5, 0), new System.Windows.Point(0.5, 1));
        private static readonly LinearGradientBrush FALSE_BRUSH =
            new LinearGradientBrush(COL_FALSE_DARK, COL_FALSE_LIGHT, new System.Windows.Point(0.5, 0), new System.Windows.Point(0.5, 1));

        private static readonly GradientStopCollection GR_STOPS_FALSE = new GradientStopCollection( new List<GradientStop> {
            new GradientStop(COL_TRUE_DARK, 0.0),
            new GradientStop(COL_TRUE_LIGHT, 0.9),
            new GradientStop(COL_FALSE_LIGHT, 0.95),
            new GradientStop(COL_FALSE_LIGHT, 1.0),
        });
        private static readonly LinearGradientBrush FALSE_BRUSH_1 =
            new LinearGradientBrush(GR_STOPS_FALSE, new System.Windows.Point(0.5, 0), new System.Windows.Point(0.5, 1));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return FALSE_BRUSH;

            if (value is bool)
            {
                if ((bool)value)
                    return TRUE_BRUSH;
                else
                    return FALSE_BRUSH_1;
            }
            else
            {
                return FALSE_BRUSH;
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

    [ValueConversion(typeof(Boolean), typeof(System.Windows.FontWeight))]
    public class BoolToWinMediaFontWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return System.Windows.FontWeights.Normal;

            if (value is bool)
            {
                if ((bool)value)
                    return System.Windows.FontWeights.Normal;
                else
                    return System.Windows.FontWeights.Bold;
            }
            else
            {
                return System.Windows.FontWeights.Normal;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }

    [ValueConversion(typeof(SharpDX.Color4), typeof(System.Windows.Media.Color))]
    public class SharpDXColor4ToWinMediaColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return (Color)ColorConverter.ConvertFromString("#00000000");

            if (value is SharpDX.Color4)
            {
                SharpDX.Color4 sdx_color = (SharpDX.Color4)value;
                return new System.Windows.Media.Color 
                { 
                    A = (byte)(sdx_color.Alpha * 255), 
                    R = (byte)(sdx_color.Red * 255),
                    G = (byte)(sdx_color.Green * 255),
                    B = (byte)(sdx_color.Blue * 255)
                };
            }
            else
            {
                return (Color)ColorConverter.ConvertFromString("#00000000");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }

    [ValueConversion(typeof(System.Collections.IList), typeof(List<string>))]
    public class ItemListConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                var ol = value as System.Collections.IList;
                if (ol != null)
                {
                    List<string> list = new List<string>();
                    foreach(var item in ol)
                    {
                        list.Add(item.ToString());
                    }
                    return list;
                }
            }
            return new List<string>();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    [ValueConversion(typeof(Boolean), typeof(Boolean))]
    public class InverseBooleanConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // the code below casues problems in child Bindings of a MultiBinding
            //if (targetType != typeof(bool))
            //    throw new InvalidOperationException("The target must be a boolean");

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
                if(value is bool)
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

    [ValueConversion(typeof(EntityVisibility), typeof(Boolean))]
    public class EntityVisibilityCorrespondenceToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && parameter != null)
            {
                if (value is EntityVisibility)
                {
                    EntityVisibility vis1 = (EntityVisibility) value;
                    EntityVisibility vis2 = Entity.ConvertStringToVis(parameter.ToString());
                    if (vis1 == vis2)
                        return true;
                    else
                        return false;
                }
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(Entity), typeof(ZonedPolygon))]
    public class EntityToZonedPolygonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                if (value is ZonedPolygon)
                {
                    return (value as ZonedPolygon);
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                if (value is ZonedPolygon)
                    return value;
            }
            return null;
        }
    }

    [ValueConversion(typeof(double), typeof(string))]
    public class DoubleToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            System.IFormatProvider format;
            if (culture != null)
                format = culture.NumberFormat;
            else
                format = new System.Globalization.NumberFormatInfo();

            double d = 0.0;
            if (value is double)
                d = (double)value;
            return d.ToString("F3", format);
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


    [ValueConversion(typeof(Orientation), typeof(int))]
    public class IntToOrientationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Orientation or = (Orientation)value;
            if (or == Orientation.XZ)
                return 0;
            else if (or == Orientation.XY)
                return 1;
            else
                return 2;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int i = (int)value;
            if (i == 0)
                return Orientation.XZ;
            else if (i == 1)
                return Orientation.XY;
            else
                return Orientation.YZ;
        }
    }

    [ValueConversion(typeof(List<string>), typeof(string))]
    public class ListOfStringsToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            List<string> list = value as List<string>;
            if (list == null || list.Count < 1)
                return string.Empty;

            string result = string.Empty;
            foreach(string str in list)
            {
                result += str + "\n";
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            List<string> result = new List<string>();
            if (value == null)
                return result;

            System.IFormatProvider format;
            if (culture != null)
                format = culture.NumberFormat;
            else
                format = new System.Globalization.NumberFormatInfo();

            string str_val = string.Format(format, "{0}", value);

            string[] str_lines = str_val.Split(new char[] { '\n' });
            foreach(string line in str_lines)
            {
                result.Add(line);
            }

            return result;
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
                Type targetTypeP = Type.GetType(parameter.ToString());
                if (targetTypeP != null && value.GetType() == targetTypeP)
                    return true;
                else
                    return false;
            }
            else
            {
                foreach (string strType in str_params_OR)
                {
                    Type targetTypeP = Type.GetType(strType.ToString());
                    if (targetTypeP != null && value.GetType() == targetTypeP)
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

    public class CloneConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values != null && values.Count() > 0)
            {
                return values[0];
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
                int n = targetTypes.Count();
                object[] result = new object[n];
                for (int i = 0; i < n; i++)
                {
                    result[i] = value;
                }
                return result;
            }
            else
            {
                return null;
            }
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
                for (int i = 0; i < n; i++ )
                {
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

    public class MultiSelectionToSingleSelection : IMultiValueConverter
    {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values != null && values.Count() > 0)
            {
                int n = values.Count();
                string selection = "-1";
                for (int i = 0; i < n; i++)
                {
                    int selNext = -1;
                    bool success = Int32.TryParse(values[i].ToString(), out selNext);
                    if (success && selNext != -1)
                    {
                        selection = selNext.ToString();
                        break;
                    }
                }
                return selection;
            }
            else
            {
                return "-1";
            }
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class MultiColorToSingleColorString : IMultiValueConverter
    {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values != null && values.Count() > 0)
            {
                int n = values.Count();
                string selection = "- - -";
                for (int i = 0; i < n; i++)
                {
                    if (values[i] is Color)
                    {
                        Color col = (Color)values[i];
                        selection = col.R.ToString() + " " + col.G.ToString() + " " + col.B.ToString();
                     }
                }
                return selection;
            }
            else
            {
                return "- - -";
            }
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class MultiColorToSingleColor : IMultiValueConverter
    {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values != null && values.Count() > 0)
            {
                int n = values.Count();
                Color selection = Colors.White;
                for (int i = 0; i < n; i++)
                {
                    if (values[i] is Color)
                    {
                        Color col = (Color)values[i];
                        selection = col;
                    }
                }
                return selection;
            }
            else
            {
                return Colors.White;
            }
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    // UIElements
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

    // Style
    [ValueConversion(typeof(Boolean), typeof(System.Windows.Style))]
    public class BooleanToBorderStyleConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                if (value is bool)
                {
                    // load the styles
                    System.Windows.Style active_style = System.Windows.Application.Current.TryFindResource("ActiveActionBorder") as System.Windows.Style;
                    System.Windows.Style inactive_style = System.Windows.Application.Current.TryFindResource("InactiveActionBorder") as System.Windows.Style;

                    bool active = (bool)value;
                    if (active)
                        return active_style;
                    else
                        return inactive_style;
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }

}
