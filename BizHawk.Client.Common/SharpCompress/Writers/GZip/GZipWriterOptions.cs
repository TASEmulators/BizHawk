using SharpCompress.Common;
using SharpCompress.Compressors.Deflate;

namespace SharpCompress.Writers.GZip
{
    public class GZipWriterOptions : WriterOptions
    {
        public GZipWriterOptions()
            : base(CompressionType.GZip)
        {
        }
        
        internal GZipWriterOptions(WriterOptions options)
            : base(options.CompressionType)
        {
            LeaveStreamOpen = options.LeaveStreamOpen;
            ArchiveEncoding = options.ArchiveEncoding;

            var writerOptions = options as GZipWriterOptions;
            if (writerOptions != null)
            {
                CompressionLevel = writerOptions.CompressionLevel;
            }
        }
        
        public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Default;
    }
}