

using System;
using SharpCompress.Common;

namespace SharpCompress.Readers
{
    public class ReaderProgress
    {
        private readonly IEntry _entry;
        public long BytesTransferred { get; }
        public int Iterations { get; }

        public int PercentageRead => (int)Math.Round(PercentageReadExact);
        public double PercentageReadExact => (float)BytesTransferred / _entry.Size * 100;

        public ReaderProgress(IEntry entry, long bytesTransferred, int iterations)
        {
            _entry = entry;
            BytesTransferred = bytesTransferred;
            Iterations = iterations;
        }
    }
}
