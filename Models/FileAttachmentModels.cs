using System;
using System.Collections.Generic;
using System.Linq;
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

    public class PracticeFileAttachment
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
}