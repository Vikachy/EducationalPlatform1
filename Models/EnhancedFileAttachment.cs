using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EducationalPlatform.Models
{
    public class EnhancedFileAttachment : INotifyPropertyChanged
    {
        private int _attachmentId;
        private string _fileName = string.Empty;
        private string _filePath = string.Empty;
        private long _fileSize;
        private DateTime _uploadDate;
        private string _fileType = string.Empty;
        private string _fileIcon = "📄";
        private bool _isUploading;
        private string _statusIcon = "⏳";
        private byte[]? _fileBytes;
        private string _fileSizeFormatted = "0 Б";

        public int AttachmentId
        {
            get => _attachmentId;
            set => SetProperty(ref _attachmentId, value);
        }

        public string FileName
        {
            get => _fileName;
            set => SetProperty(ref _fileName, value);
        }

        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }

        public long FileSize
        {
            get => _fileSize;
            set
            {
                if (SetProperty(ref _fileSize, value))
                {
                    UpdateSizeFormatted();
                    OnPropertyChanged(nameof(SizeFormatted));
                }
            }
        }

        public DateTime UploadDate
        {
            get => _uploadDate;
            set => SetProperty(ref _uploadDate, value);
        }

        public string FileType
        {
            get => _fileType;
            set => SetProperty(ref _fileType, value);
        }

        public string FileIcon
        {
            get => _fileIcon;
            set => SetProperty(ref _fileIcon, value);
        }

        public bool IsUploading
        {
            get => _isUploading;
            set => SetProperty(ref _isUploading, value);
        }

        public string StatusIcon
        {
            get => _statusIcon;
            set => SetProperty(ref _statusIcon, value);
        }

        public byte[]? FileBytes
        {
            get => _fileBytes;
            set => SetProperty(ref _fileBytes, value);
        }

        // Добавлено свойство для форматированного размера файла
        public string FileSizeFormatted
        {
            get => _fileSizeFormatted;
            set => SetProperty(ref _fileSizeFormatted, value);
        }

        // Свойство для обратной совместимости
        public string SizeFormatted
        {
            get
            {
                if (_fileSize < 1024) return $"{_fileSize} Б";
                if (_fileSize < 1024 * 1024) return $"{_fileSize / 1024.0:0.0} КБ";
                return $"{_fileSize / (1024.0 * 1024.0):0.0} МБ";
            }
        }

        private void UpdateSizeFormatted()
        {
            if (_fileSize < 1024)
            {
                _fileSizeFormatted = $"{_fileSize} Б";
            }
            else if (_fileSize < 1024 * 1024)
            {
                _fileSizeFormatted = $"{_fileSize / 1024.0:0.0} КБ";
            }
            else
            {
                _fileSizeFormatted = $"{_fileSize / (1024.0 * 1024.0):0.0} МБ";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}