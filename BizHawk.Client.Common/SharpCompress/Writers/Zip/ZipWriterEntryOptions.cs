using System;
using SharpCompress.Common;
using SharpCompress.Compressors.Deflate;

namespace SharpCompress.Writers.Zip
{
    public class ZipWriterEntryOptions
    {
        public CompressionType? CompressionType { get; set; }
        /// <summary>
        /// When CompressionType.Deflate is used, this property is referenced.  Defaults to CompressionLevel.Default.
        /// </summary>
        public CompressionLevel? DeflateCompressionLevel { get; set; }

        public string EntryComment { get; set; }

        public DateTime? ModificationDateTime { get; set; }

        /// <summary>
        /// Allocate an extra 20 bytes for this entry to store,
		/// 64 bit length values, thus enabling streams
		/// larger than 4GiB.
		/// This option is not supported with non-seekable streams.
        /// </summary>
        public bool? EnableZip64 { get; set; }
    }
}