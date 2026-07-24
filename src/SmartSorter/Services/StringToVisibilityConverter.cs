using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SmartSorter.Services
{
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            string currentTab = value.ToString()!;
            string targetTab = parameter.ToString()!;

            return currentTab.Equals(targetTab, StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}