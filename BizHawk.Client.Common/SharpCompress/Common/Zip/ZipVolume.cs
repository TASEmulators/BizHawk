using System.IO;
using SharpCompress.Readers;

namespace SharpCompress.Common.Zip
{
    public class ZipVolume : Volume
    {
        public ZipVolume(Stream stream, ReaderOptions readerOptions)
            : base(stream, readerOptions)
        {
        }

        public string Comment { get; internal set; }
    }
}