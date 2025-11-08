using System.Globalization;

namespace EducationalPlatform.Converters
{
    public class FileTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string fileType)
            {
                return fileType.ToLower() switch
                {
                    ".pdf" => "📄",
                    ".doc" or ".docx" => "📝",
                    ".zip" or ".rar" => "📦",
                    ".jpg" or ".png" or ".gif" => "🖼️",
                    ".mp4" or ".avi" => "🎬",
                    ".xls" or ".xlsx" => "📊",
                    ".ppt" or ".pptx" => "📑",
                    ".txt" => "📄",
                    _ => "📎"
                };
            }
            return "📎";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}