using System;
using System.IO;
using SharpCompress.IO;

namespace SharpCompress.Common.Rar.Headers
{
    // http://www.forensicswiki.org/w/images/5/5b/RARFileStructure.txt
    // https://www.rarlab.com/technote.htm
    internal class RarHeader : IRarHeader
    {
        private readonly HeaderType _headerType;
        private readonly bool _isRar5;

        internal static RarHeader TryReadBase(RarCrcBinaryReader reader, bool isRar5, ArchiveEncoding archiveEncoding)
        {
            try
            {
                return new RarHeader(reader, isRar5, archiveEncoding);
            }
            catch (EndOfStreamException)
            {
                return null;
            }
        }

        private RarHeader(RarCrcBinaryReader reader, bool isRar5, ArchiveEncoding archiveEncoding) 
        {
            _headerType = HeaderType.Null;
            _isRar5 = isRar5;
            ArchiveEncoding = archiveEncoding;
            if (IsRar5) 
            {
                HeaderCrc = reader.ReadUInt32();
                reader.ResetCrc();
                HeaderSize = (int)reader.ReadRarVIntUInt32(3);
                reader.Mark();
                HeaderCode = reader.ReadRarVIntByte();
                HeaderFlags = reader.ReadRarVIntUInt16(2);

                if (HasHeaderFlag(HeaderFlagsV5.HAS_EXTRA))
                {
                    ExtraSize = reader.ReadRarVIntUInt32();
                }
                if (HasHeaderFlag(HeaderFlagsV5.HAS_DATA))
                {
                    AdditionalDataSize = (long)reader.ReadRarVInt();
                }
            } else {
                reader.Mark();
                HeaderCrc = reader.ReadUInt16();
                reader.ResetCrc();
                HeaderCode = reader.ReadByte();
                HeaderFlags = reader.ReadUInt16();
                HeaderSize = reader.ReadInt16();
                if (HasHeaderFlag(HeaderFlagsV4.HAS_DATA))
                {
                    AdditionalDataSize = reader.ReadUInt32();
                }
            }
        }

        protected RarHeader(RarHeader header, RarCrcBinaryReader reader, HeaderType headerType) {
            _headerType = headerType;
            _isRar5 = header.IsRar5;
            HeaderCrc = header.HeaderCrc;
            HeaderCode = header.HeaderCode;
            HeaderFlags = header.HeaderFlags;
            HeaderSize = header.HeaderSize;
            ExtraSize = header.ExtraSize;
            AdditionalDataSize = header.AdditionalDataSize;
            ArchiveEncoding = header.ArchiveEncoding;
            ReadFinish(reader);

            int n = RemainingHeaderBytes(reader);
            if (n > 0)
            {
                reader.ReadBytes(n);
            }

            VerifyHeaderCrc(reader.GetCrc32());
        }

        protected int RemainingHeaderBytes(MarkingBinaryReader reader) {
            return checked(HeaderSize - (int)reader.CurrentReadByteCount);
        }

        protected virtual void ReadFinish(MarkingBinaryReader reader)
        {
            throw new NotImplementedException();
        }

        private void VerifyHeaderCrc(uint crc32)
        {
            var b = (IsRar5 ? crc32 : (ushort)crc32) == HeaderCrc;
            if (!b)
            {
                throw new InvalidFormatException("rar header crc mismatch");
            }
        }

        public HeaderType HeaderType => _headerType;

        protected bool IsRar5 => _isRar5;

        protected uint HeaderCrc { get; }

        internal byte HeaderCode { get; }

        protected ushort HeaderFlags { get; }

        protected bool HasHeaderFlag(ushort flag) 
        {
            return (HeaderFlags & flag) == flag;
        }

        protected int HeaderSize { get; }

        internal ArchiveEncoding ArchiveEncoding { get; }

        /// <summary>
        /// Extra header size.
        /// </summary>
        protected uint ExtraSize { get; }

        /// <summary>
        /// Size of additional data (eg file contents)
        /// </summary>
        protected long AdditionalDataSize { get; }
    }
}