using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace EducationalPlatform.Converters
{
    public class BoolToTextConverter : IValueConverter
    {
        public string TrueText { get; set; } = "Да";
        public string FalseText { get; set; } = "Нет";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? TrueText : FalseText;
            return FalseText;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToButtonTextConverter : IValueConverter
    {
        public string TrueText { get; set; } = "Выключить";
        public string FalseText { get; set; } = "Включить";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? TrueText : FalseText;
            return FalseText;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToColorConverter : IValueConverter
    {
        public Color TrueColor { get; set; } = Color.FromArgb("#4CAF50");
        public Color FalseColor { get; set; } = Color.FromArgb("#F44336");

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TrueColor : FalseColor;
            }
            return FalseColor;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}