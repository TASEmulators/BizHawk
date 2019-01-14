using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SharpCompress.Converters;

namespace SharpCompress.Common.Zip.Headers
{
    internal abstract class ZipFileEntry : ZipHeader
    {
        protected ZipFileEntry(ZipHeaderType type, ArchiveEncoding archiveEncoding)
            : base(type)
        {
            Extra = new List<ExtraData>();
            ArchiveEncoding = archiveEncoding;
        }

        internal bool IsDirectory
        {
            get
            {
                if (Name.EndsWith("/"))
                {
                    return true;
                }

                //.NET Framework 4.5 : System.IO.Compression::CreateFromDirectory() probably writes backslashes to headers
                return CompressedSize == 0
                       && UncompressedSize == 0
                       && Name.EndsWith("\\");
            }
        }
        
        internal Stream PackedStream { get; set; }

        internal ArchiveEncoding ArchiveEncoding { get; }

        internal string Name { get; set; }

        internal HeaderFlags Flags { get; set; }

        internal ZipCompressionMethod CompressionMethod { get; set; }

        internal long CompressedSize { get; set; }

        internal long? DataStartPosition { get; set; }

        internal long UncompressedSize { get; set; }

        internal List<ExtraData> Extra { get; set; }

        public string Password { get; set; }

        internal PkwareTraditionalEncryptionData ComposeEncryptionData(Stream archiveStream)
        {
            if (archiveStream == null)
            {
                throw new ArgumentNullException(nameof(archiveStream));
            }

            var buffer = new byte[12];
            archiveStream.ReadFully(buffer);

            PkwareTraditionalEncryptionData encryptionData = PkwareTraditionalEncryptionData.ForRead(Password, this, buffer);

            return encryptionData;
        }

#if !NO_CRYPTO
        internal WinzipAesEncryptionData WinzipAesEncryptionData { get; set; }
#endif

        internal ushort LastModifiedDate { get; set; }

        internal ushort LastModifiedTime { get; set; }

        internal uint Crc { get; set; }

        protected void LoadExtra(byte[] extra)
        {
            for (int i = 0; i < extra.Length - 4;)
            {
                ExtraDataType type = (ExtraDataType)DataConverter.LittleEndian.GetUInt16(extra, i);
                if (!Enum.IsDefined(typeof(ExtraDataType), type))
                {
                    type = ExtraDataType.NotImplementedExtraData;
                }

                ushort length = DataConverter.LittleEndian.GetUInt16(extra, i + 2);
                byte[] data = new byte[length];
                Buffer.BlockCopy(extra, i + 4, data, 0, length);
                Extra.Add(LocalEntryHeaderExtraFactory.Create(type, length, data));

                i += length + 4;
            }
        }

        internal ZipFilePart Part { get; set; }

        internal bool IsZip64 => CompressedSize == uint.MaxValue;
    }
}