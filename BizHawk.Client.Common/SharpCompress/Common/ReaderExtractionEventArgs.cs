using System;
using SharpCompress.Readers;

namespace SharpCompress.Common
{
    public class ReaderExtractionEventArgs<T> : EventArgs
    {
        internal ReaderExtractionEventArgs(T entry, ReaderProgress readerProgress = null)
        {
            Item = entry;
            ReaderProgress = readerProgress;
        }

        public T Item { get; }
        public ReaderProgress ReaderProgress { get; }
    }
}