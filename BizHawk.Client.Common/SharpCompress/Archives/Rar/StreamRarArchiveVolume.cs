using System.Collections.Generic;
using System.IO;
using SharpCompress.Common.Rar;
using SharpCompress.Common.Rar.Headers;
using SharpCompress.IO;
using SharpCompress.Readers;

namespace SharpCompress.Archives.Rar
{
    internal class StreamRarArchiveVolume : RarVolume
    {
        internal StreamRarArchiveVolume(Stream stream, ReaderOptions options)
            : base(StreamingMode.Seekable, stream, options)
        {
        }

        internal override IEnumerable<RarFilePart> ReadFileParts()
        {
            return GetVolumeFileParts();
        }

        internal override RarFilePart CreateFilePart(MarkHeader markHeader, FileHeader fileHeader)
        {
            return new SeekableFilePart(markHeader, fileHeader, Stream, ReaderOptions.Password);
        }
    }
}