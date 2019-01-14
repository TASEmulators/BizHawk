namespace SharpCompress.Common.SevenZip
{
    internal struct CMethodId
    {
        public const ulong K_COPY_ID = 0;
        public const ulong K_LZMA_ID = 0x030101;
        public const ulong K_LZMA2_ID = 0x21;
        public const ulong K_AES_ID = 0x06F10701;

        public static readonly CMethodId K_COPY = new CMethodId(K_COPY_ID);
        public static readonly CMethodId K_LZMA = new CMethodId(K_LZMA_ID);
        public static readonly CMethodId K_LZMA2 = new CMethodId(K_LZMA2_ID);
        public static readonly CMethodId K_AES = new CMethodId(K_AES_ID);

        public readonly ulong _id;

        public CMethodId(ulong id)
        {
            _id = id;
        }

        public override int GetHashCode()
        {
            return _id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is CMethodId && (CMethodId)obj == this;
        }

        public bool Equals(CMethodId other)
        {
            return _id == other._id;
        }

        public static bool operator ==(CMethodId left, CMethodId right)
        {
            return left._id == right._id;
        }

        public static bool operator !=(CMethodId left, CMethodId right)
        {
            return left._id != right._id;
        }

        public int GetLength()
        {
            int bytes = 0;
            for (ulong value = _id; value != 0; value >>= 8)
            {
                bytes++;
            }
            return bytes;
        }
    }
}