using System.IO;
using SharpCompress.Readers;

namespace SharpCompress.Common.Tar
{
    public class TarVolume : Volume
    {
        public TarVolume(Stream stream, ReaderOptions readerOptions)
            : base(stream, readerOptions)
        {
        }
    }
}