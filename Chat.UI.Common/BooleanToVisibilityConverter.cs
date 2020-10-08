using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Chat.UI.Common
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public class BooleanToVisibilityConverter : MarkupExtension, IValueConverter
    {
        public bool Invert { get; set; }

        public BooleanToVisibilityConverter()
        {
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(Visibility))
                throw new InvalidOperationException("The target must be a boolean");

            var realValue = (bool)value;
            if (this.Invert)
                realValue = !realValue;

            return realValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The source must be a boolean");

            bool rawValue;
            switch ((Visibility)value)
            {
                case Visibility.Visible: rawValue = true; break;
                case Visibility.Hidden: rawValue = false; break;
                case Visibility.Collapsed: rawValue = false; break;
                default:
                    throw new NotImplementedException();
            }

            if (this.Invert)
                rawValue = !rawValue;

            return rawValue;
        }

        #endregion
    }
}
