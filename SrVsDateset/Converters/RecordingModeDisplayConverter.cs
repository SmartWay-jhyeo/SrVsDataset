using System;
using System.Globalization;
using System.Windows.Data;
using SrVsDataset.Models;

namespace SrVsDataset.Converters
{
    /// <summary>
    /// RecordingMode enum을 표시용 문자열로 변환하는 컨버터
    /// </summary>
    public class RecordingModeDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is RecordingMode mode)
            {
                return mode.GetDisplayName();
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // UI에서는 ConvertBack이 필요하지 않음 (직접 바인딩 사용)
            throw new NotImplementedException();
        }
    }
}