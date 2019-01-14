using System;
using System.IO;

namespace SharpCompress.Common.Zip.Headers
{
    internal class IgnoreHeader : ZipHeader
    {
        public IgnoreHeader(ZipHeaderType type)
            : base(type)
        {
        }

        internal override void Read(BinaryReader reader)
        {
        }
    }
}