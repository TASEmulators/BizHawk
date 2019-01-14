
#if !NO_CRYPTO
using System;
using System.Collections.Generic;
using System.IO;

namespace SharpCompress.Common.Rar
{
    internal class RarCryptoWrapper : Stream
    {
        private readonly Stream _actualStream;
        private readonly byte[] _salt;
        private RarRijndael _rijndael;
        private readonly Queue<byte> _data = new Queue<byte>();

        public RarCryptoWrapper(Stream actualStream, string password, byte[] salt)
        {
            _actualStream = actualStream;
            _salt = salt;
            _rijndael = RarRijndael.InitializeFrom(password, salt);
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_salt == null)
            {
                return _actualStream.Read(buffer, offset, count);
            }
            return ReadAndDecrypt(buffer, offset, count);
        }

        public int ReadAndDecrypt(byte[] buffer, int offset, int count)
        {
            int queueSize = _data.Count;
            int sizeToRead = count - queueSize;

            if (sizeToRead > 0)
            {
                int alignedSize = sizeToRead + ((~sizeToRead + 1) & 0xf);
                for (int i = 0; i < alignedSize / 16; i++)
                {
                    //long ax = System.currentTimeMillis();
                    byte[] cipherText = new byte[RarRijndael.CRYPTO_BLOCK_SIZE];
                    _actualStream.Read(cipherText, 0, RarRijndael.CRYPTO_BLOCK_SIZE);

                    var readBytes = _rijndael.ProcessBlock(cipherText);
                    foreach (var readByte in readBytes)
                        _data.Enqueue(readByte);

                }

                for (int i = 0; i < count; i++)
                    buffer[offset + i] = _data.Dequeue();
            }
            return count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get; set; }

        protected override void Dispose(bool disposing)
        {
            if (_rijndael != null)
            {
                _rijndael.Dispose();
                _rijndael = null;
            }
            base.Dispose(disposing);
        }
    }
}
#endif