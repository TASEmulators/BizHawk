using System;

namespace SharpCompress.Common
{
    public class CryptographicException : Exception
    {
        public CryptographicException(string message)
            : base(message)
        {
        }
    }
}