using System.Collections.Generic;
using System.IO;
using SharpCompress.Common;
using SharpCompress.Common.Zip;
using SharpCompress.Common.Zip.Headers;

namespace SharpCompress.Readers.Zip
{
    public class ZipReader : AbstractReader<ZipEntry, ZipVolume>
    {
        private readonly StreamingZipHeaderFactory _headerFactory;

        private ZipReader(Stream stream, ReaderOptions options)
            : base(options, ArchiveType.Zip)
        {
            Volume = new ZipVolume(stream, options);
            _headerFactory = new StreamingZipHeaderFactory(options.Password, options.ArchiveEncoding);
        }

        public override ZipVolume Volume { get; }

        #region Open

        /// <summary>
        /// Opens a ZipReader for Non-seeking usage with a single volume
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static ZipReader Open(Stream stream, ReaderOptions options = null)
        {
            stream.CheckNotNull("stream");
            return new ZipReader(stream, options ?? new ReaderOptions());
        }

        #endregion Open

        protected override IEnumerable<ZipEntry> GetEntries(Stream stream)
        {
            foreach (ZipHeader h in _headerFactory.ReadStreamHeader(stream))
            {
                if (h != null)
                {
                    switch (h.ZipHeaderType)
                    {
                        case ZipHeaderType.LocalEntry:
                            {
                                yield return new ZipEntry(new StreamingZipFilePart(h as LocalEntryHeader,
                                                                                   stream));
                            }
                            break;
                        case ZipHeaderType.DirectoryEnd:
                            {
                                yield break;
                            }
                    }
                }
            }
        }
    }
}