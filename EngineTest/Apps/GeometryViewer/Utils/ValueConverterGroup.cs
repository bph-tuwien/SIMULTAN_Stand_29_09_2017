using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;

namespace GeometryViewer.Utils
{
    [System.Windows.Markup.ContentProperty("Converters")]
    public class ValueConverterGroup : IValueConverter
    {
        public ObservableCollection<IValueConverter> Converters { get; set; }
        public ValueConverterGroup()
        {
            this.Converters = new ObservableCollection<IValueConverter>();
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // debug
            if (parameter != null && parameter.ToString() == "POLY_LABELS_EDIT")
            {
                var test = parameter;
            }
            // call first converter
            object output = ForwardConverterCall(this.Converters[0], value, parameter, culture);

            for(int i = 1; i < this.Converters.Count - 1; i++)
            {
                output = ForwardConverterCall(this.Converters[i], output, parameter, culture);
            }

            // call last converter
            return ForwardConverterCall(this.Converters[this.Converters.Count - 1], output, parameter, culture);

        }

        private object ForwardConverterCall(IValueConverter _converter, object _value, object _parameter, CultureInfo _culture)
        {
            if (_converter == null)
                return null;

            object[] attribs = _converter.GetType().GetCustomAttributes(typeof(ValueConversionAttribute), false);
            if (attribs != null && attribs.Length == 1)
            {
                ValueConversionAttribute vca = attribs[0] as ValueConversionAttribute;
                if (vca != null)
                {
                    Type currentTargetType = vca.TargetType;
                    return _converter.Convert(_value, currentTargetType, _parameter, _culture);
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // call last converter
            object output = BackwardConverterCall(this.Converters[this.Converters.Count - 1], value, parameter, culture);

            for (int i = this.Converters.Count - 2; i > 0; i--)
            {
                output = BackwardConverterCall(this.Converters[i], output, parameter, culture);
            }

            // call first converter
            return BackwardConverterCall(this.Converters[0], output, parameter, culture);
        }

        private object BackwardConverterCall(IValueConverter _converter, object _value, object _parameter, CultureInfo _culture)
        {
            if (_converter == null)
                return null;

            object[] attribs = _converter.GetType().GetCustomAttributes(typeof(ValueConversionAttribute), false);
            if (attribs != null && attribs.Length == 1)
            {
                ValueConversionAttribute vca = attribs[0] as ValueConversionAttribute;
                if (vca != null)
                {
                    Type currentSourceType = vca.SourceType;
                    return _converter.ConvertBack(_value, currentSourceType, _parameter, _culture);
                }
            }

            return null;
        }
    }
}
