using SharpCompress.Archives;
using SharpCompress.Common;

namespace SharpCompress.Writers.Tar
{
    public class TarWriterOptions : WriterOptions
    {
        /// <summary>
        /// Indicates if archive should be finalized (by 2 empty blocks) on close.
        /// </summary>
        public bool FinalizeArchiveOnClose { get; }

        public TarWriterOptions(CompressionType compressionType, bool finalizeArchiveOnClose)
            : base(compressionType)
        {
            FinalizeArchiveOnClose = finalizeArchiveOnClose;
        }

        internal TarWriterOptions(WriterOptions options) : this(options.CompressionType, true)
        {
        }
    }
}