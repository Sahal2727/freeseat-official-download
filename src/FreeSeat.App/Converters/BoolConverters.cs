using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;
using System.Windows.Media;

namespace FreeSeat.App.Converters;

/// <summary>
/// Converts boolean to Visibility (True = Visible, False = Collapsed).
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is System.Windows.Visibility.Visible;
    }
}

/// <summary>
/// Converts boolean to Visibility (True = Collapsed, False = Visible).
/// </summary>
public class BoolToVisibilityInverseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is System.Windows.Visibility.Collapsed;
    }
}

/// <summary>
/// Inverts a boolean value.
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b ? !b : true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b ? !b : true;
    }
}

/// <summary>
/// Converts boolean to monitoring button text.
/// </summary>
public class BoolToMonitorTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? "⏸ Stop Monitoring" : "▶ Start Monitoring";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is string s && s.Contains("Stop");
    }
}

/// <summary>
/// Converts boolean to On/Off text.
/// </summary>
public class BoolToOnOffConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? "ON" : "OFF";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is string s && string.Equals(s, "ON", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Converts boolean to Start/Stop text.
/// </summary>
public class BoolToStartStopConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? "Stop Monitoring" : "Start Monitoring";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is string s && s.Contains("Stop", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Converts an enum value to its Description attribute or ToString().
/// </summary>
public class EnumToDescriptionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return string.Empty;

        var type = value.GetType();
        if (!type.IsEnum) return value.ToString() ?? string.Empty;

        var name = Enum.GetName(type, value);
        if (name == null) return value.ToString() ?? string.Empty;

        var field = type.GetField(name);
        if (field == null) return name;

        var attr = field.GetCustomAttribute<DescriptionAttribute>();
        return attr?.Description ?? name;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts an integer count to Visibility (Count > 0 = Visible, Count = 0 = Collapsed).
/// </summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count > 0 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }
        if (value is long lcount)
        {
            return lcount > 0 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }
        if (value is ICollection collection)
        {
            return collection.Count > 0 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }
        return System.Windows.Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts an integer count to Visibility (Count = 0 = Visible, Count > 0 = Collapsed).
/// </summary>
public class CountToVisibilityInverseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count == 0 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }
        if (value is long lcount)
        {
            return lcount == 0 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }
        if (value is ICollection collection)
        {
            return collection.Count == 0 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }
        return System.Windows.Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts boolean to a specific Brush based on value.
/// </summary>
public class BoolToBrushConverter : IValueConverter
{
    public Brush TrueBrush { get; set; } = Brushes.Red;
    public Brush FalseBrush { get; set; } = Brushes.Black;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? TrueBrush : FalseBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}