using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Controls;
using System.ComponentModel;
using System.Data;

namespace DataStructVisualizer.WpfUtils
{
    [ValueConversion(typeof(Boolean), typeof(Boolean))]
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
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

    [ValueConversion(typeof(TreeViewItem),typeof(Thickness))]
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
                return 0;

            var test = InitLength + Factor * item.GetDepth();
            return InitLength + Factor * item.GetDepth();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }

    [ValueConversion(typeof(System.Int32), typeof(Boolean))]
    public class SortModeToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            int sort_mode = 0;
            int match = 0;
            bool success_parsing_value = int.TryParse(value.ToString(), out sort_mode);
            bool success_parsing_param = int.TryParse(parameter.ToString(), out match);
            
            if (success_parsing_value && success_parsing_param)
                return (sort_mode == match);
            else
                return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(System.Collections.IList), typeof(ListCollectionView))]
    public class TreeViewSortingEntityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            System.Collections.IList collection = value as System.Collections.IList;
            if (collection == null || parameter == null)
                return null;

            ListCollectionView view = new ListCollectionView(collection);
            SortDescription sort = new SortDescription(parameter.ToString(), ListSortDirection.Ascending);
            view.SortDescriptions.Add(sort);

            return view;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

    }

    [ValueConversion(typeof(System.Windows.Rect), typeof(System.Windows.Thickness))]
    public class RectToThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Thickness t = new Thickness(0, 0, 0, 0);
            if (value != null)
            {
                if (value is Rect)
                {
                    Rect rec = (Rect)value;
                    if (parameter == null)
                    {
                        t.Top = rec.Top;
                        t.Bottom = rec.Bottom;
                        t.Left = rec.Left;
                        t.Right = rec.Right;
                    }
                }
            }
            return t;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
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

    public class TreeViewSortingAccToParameterConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Count() == 0) return null;
            if (values.Count() != 3) return values[0];

            // retrieve collection to sort
            System.Collections.IList collection = values[0] as System.Collections.IList;
            if (collection == null || collection.Count < 2) return values[0];
            
            // retrieve property name according to which to sort
            string sorting_param = values[1].ToString();
            if (string.IsNullOrEmpty(sorting_param)) return values[0];

            // retrieve the sorting direction
            int sorting_dir_int = 0;
            bool succes_parsing_dir = int.TryParse(values[2].ToString(), out sorting_dir_int);
            if (!succes_parsing_dir || sorting_dir_int == 0) return values[0];

            ListSortDirection sorting_dir = (sorting_dir_int == 1) ? ListSortDirection.Ascending : ListSortDirection.Descending;

            // perform the sorting
            ListCollectionView view = new ListCollectionView(collection);
            SortDescription sort = new SortDescription(sorting_param, sorting_dir);
            view.SortDescriptions.Add(sort);

            return view;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class StringListToSmallStringListConverter : IMultiValueConverter
    {
        private static int MAX_LIST_LEN = 10;
        private static int MIN_SEARCH_TEXT_LEN = 3;
        public static readonly string EMPTY = "-";
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            List<string> some_strings = new List<string>();

            if (values == null || values.Count() == 0) return null;
            if (values.Count() < 2) return some_strings;

            // retrieve all strings
            List<string> all_strings = values[0] as List<string>;
            if (all_strings == null || all_strings.Count == 0)
                return some_strings;

            // retrieve the search text
            string search_string = values[1].ToString();
            if (search_string.Count() < MIN_SEARCH_TEXT_LEN)
                return some_strings;

            // extract the relevant strings
            int counter = 0;
            foreach (string s in all_strings)
            {
                if (string.IsNullOrEmpty(s)) continue;
                if (s.StartsWith(search_string))
                {
                    some_strings.Add(s);
                    counter++;
                    if (counter >= MAX_LIST_LEN)
                        break;
                }
            }
            some_strings.Add(StringListToSmallStringListConverter.EMPTY);

            return some_strings;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class RectAndContextToThicknessConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Thickness t = new Thickness(0, 0, 0, 0);
            if (values == null) return t;
            if (values.Count() < 3) return t;

            Rect rect_IN = new Rect(0,0,0,0);
            if (values[0] is Rect)
                rect_IN = (Rect)values[0];
            
            double context_H = 0;
            double.TryParse(values[1].ToString(), out context_H);

            Thickness t_add = new Thickness(0, 0, 0, 0);
            if (values[2] is Thickness)
                t_add = (Thickness)values[2];

            bool above = true;
            if (parameter != null && parameter is bool)
                above = (bool)parameter;

            // calculate the Thickness
            t.Left = t_add.Left;
            t.Right = t_add.Right;
            if (above)
            {
                t.Top = t_add.Top;
                t.Bottom = context_H - rect_IN.Top;
            }
            else
            {
                t.Top = rect_IN.Bottom;
                t.Bottom = t_add.Bottom;
            }

            return t;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
