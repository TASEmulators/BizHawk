using System.IO;
using SharpCompress.Common;
using SharpCompress.Common.Rar.Headers;

namespace SharpCompress.Compressors.Rar
{
    internal class RarCrcStream : RarStream
    {
        private readonly MultiVolumeReadOnlyStream readStream;
        private uint currentCrc;

        public RarCrcStream(IRarUnpack unpack, FileHeader fileHeader, MultiVolumeReadOnlyStream readStream)
            : base(unpack, fileHeader, readStream)
        {
            this.readStream = readStream;
            ResetCrc();
        }

        public uint GetCrc()
        {
            return ~currentCrc;
        }

        public void ResetCrc()
        {
            currentCrc = 0xffffffff;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var result = base.Read(buffer, offset, count);
            if (result != 0)
            {
                currentCrc = RarCRC.CheckCrc(currentCrc, buffer, offset, result);
            }
            else if (GetCrc() != readStream.CurrentCrc && count != 0)
            {
                // NOTE: we use the last FileHeader in a multipart volume to check CRC
                throw new InvalidFormatException("file crc mismatch");
            }

            return result;
        }
    }
}