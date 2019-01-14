using System;

namespace SharpCompress.Common
{
    public class ArchiveException : Exception
    {
        public ArchiveException(string message)
            : base(message)
        {
        }
    }
}