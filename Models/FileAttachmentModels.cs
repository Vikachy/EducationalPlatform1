using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace EducationalPlatform.Models
{
    public class TheoryFileAttachment
    {
        public int AttachmentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public byte[] FileBytes { get; set; } = Array.Empty<byte>();
        public long FileSize { get; set; }
        public DateTime UploadDate { get; set; }
        public string FileType { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;

        public string SizeFormatted
        {
            get
            {
                if (FileSize < 1024) return $"{FileSize} Б";
                if (FileSize < 1024 * 1024) return $"{FileSize / 1024.0:0.0} КБ";
                return $"{FileSize / (1024.0 * 1024.0):0.0} МБ";
            }
        }
    }
        public class PracticeFileAttachment : INotifyPropertyChanged
        {
            private int _attachmentId;
            public int AttachmentId
            {
                get => _attachmentId;
                set { _attachmentId = value; OnPropertyChanged(); }
            }

            private int _practiceId;
            public int PracticeId
            {
                get => _practiceId;
                set { _practiceId = value; OnPropertyChanged(); }
            }

            private string _fileName = string.Empty;
            public string FileName
            {
                get => _fileName;
                set { _fileName = value; OnPropertyChanged(); }
            }

            private string _filePath = string.Empty;
            public string FilePath
            {
                get => _filePath;
                set { _filePath = value; OnPropertyChanged(); }
            }

            private string _fileType = string.Empty;
            public string FileType
            {
                get => _fileType;
                set { _fileType = value; OnPropertyChanged(); OnPropertyChanged(nameof(FileIcon)); }
            }

            private string _fileIcon = "📄";
            public string FileIcon
            {
                get => _fileIcon;
                set { _fileIcon = value; OnPropertyChanged(); }
            }

            private long _fileSize;
            public long FileSize
            {
                get => _fileSize;
                set
                {
                    _fileSize = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SizeFormatted));
                    OnPropertyChanged(nameof(FileSizeFormatted));
                }
            }

            private string _sizeFormatted = string.Empty;
            public string SizeFormatted
            {
                get
                {
                    if (string.IsNullOrEmpty(_sizeFormatted) && _fileSize > 0)
                    {
                        return FormatFileSize(_fileSize);
                    }
                    return _sizeFormatted;
                }
                set
                {
                    _sizeFormatted = value;
                    OnPropertyChanged();
                }
            }

            public string FileSizeFormatted
            {
                get => FormatFileSize(_fileSize);
            }

            private DateTime _uploadDate;
            public DateTime UploadDate
            {
                get => _uploadDate;
                set { _uploadDate = value; OnPropertyChanged(); }
            }

            private string FormatFileSize(long bytes)
            {
                if (bytes < 1024) return $"{bytes} Б";
                if (bytes < 1024 * 1024) return $"{bytes / 1024.0:0.0} КБ";
                return $"{bytes / (1024.0 * 1024.0):0.0} МБ";
            }

            public event PropertyChangedEventHandler? PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string? name = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
    }
