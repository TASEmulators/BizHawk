using System;

namespace SharpCompress.Common.Rar.Headers
{
    internal class NewSubHeaderType : IEquatable<NewSubHeaderType>
    {
        internal static readonly NewSubHeaderType SUBHEAD_TYPE_CMT = new NewSubHeaderType('C', 'M', 'T');

        //internal static final NewSubHeaderType SUBHEAD_TYPE_ACL = new NewSubHeaderType(new byte[]{'A','C','L'});

        //internal static final NewSubHeaderType SUBHEAD_TYPE_STREAM = new NewSubHeaderType(new byte[]{'S','T','M'});

        //internal static final NewSubHeaderType SUBHEAD_TYPE_UOWNER = new NewSubHeaderType(new byte[]{'U','O','W'});

        //internal static final NewSubHeaderType SUBHEAD_TYPE_AV = new NewSubHeaderType(new byte[]{'A','V'});

        internal static readonly NewSubHeaderType SUBHEAD_TYPE_RR = new NewSubHeaderType('R', 'R');

        //internal static final NewSubHeaderType SUBHEAD_TYPE_OS2EA = new NewSubHeaderType(new byte[]{'E','A','2'});

        //internal static final NewSubHeaderType SUBHEAD_TYPE_BEOSEA = new NewSubHeaderType(new byte[]{'E','A','B','E'});

        private readonly byte[] _bytes;

        private NewSubHeaderType(params char[] chars)
        {
            _bytes = new byte[chars.Length];
            for (int i = 0; i < chars.Length; ++i)
            {
                _bytes[i] = (byte)chars[i];
            }
        }

        internal bool Equals(byte[] bytes)
        {
            if (_bytes.Length != bytes.Length)
            {
                return false;
            }
            for (int i = 0; i < bytes.Length; ++i)
            {
                if (_bytes[i] != bytes[i])
                {
                    return false;
                }
            }
            return true;
        }

        public bool Equals(NewSubHeaderType other)
        {
            return Equals(other._bytes);
        }
    }
}