using System;
using System.IO;

namespace SharpCompress.Common.Zip.Headers
{
    internal class SplitHeader : ZipHeader
    {
        public SplitHeader()
            : base(ZipHeaderType.Split)
        {
        }

        internal override void Read(BinaryReader reader)
        {
            throw new NotImplementedException();
        }
    }
}