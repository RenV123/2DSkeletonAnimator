using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Xceed.Wpf.Toolkit.Core.Converters;

namespace _2DSkeletonAnimator.Helpers
{
    [ValueConversion(typeof(bool?), typeof(System.Windows.Thickness))]
    internal class BoolToBorderThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var boolVal = value as bool?;
            if (boolVal.HasValue && boolVal.Value)
            {
                return new Thickness(2);
            }
           return new Thickness(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Thickness thickness = (Thickness)value;
            if (thickness.Bottom > 0 && thickness.Top > 0 && thickness.Left > 0 && thickness.Right > 0)
                return true;

            return false;
        }
    }
    [ValueConversion(typeof(bool?), typeof(SolidColorBrush))]
    internal class BoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var boolVal = value as bool?;
            var blueBackgroundColor = (Color)ColorConverter.ConvertFromString("#B21D86B5");
            var greyBackgroundColor = (Color)ColorConverter.ConvertFromString("#B2424242");
            if (greyBackgroundColor == null || blueBackgroundColor == null) return Brushes.Gray;
            if (boolVal.HasValue && boolVal.Value)
                return new SolidColorBrush(blueBackgroundColor);
            else
                return new SolidColorBrush(greyBackgroundColor);

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    [ValueConversion(typeof(double?), typeof(string))]
    internal class DoubleToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            var str = (value as double?).ToString();
            str = str.Replace(',', '.');
            return str;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return 0;
            if (string.IsNullOrEmpty(value.ToString())) return 0;
            double number;
            var str = value.ToString();
            str = str.Replace(',', '.');
            return Double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out number) ? number : 0.0;
        }
    }
}