using System;
using System.IO;

namespace SharpCompress.Common.Zip
{
    internal enum CryptoMode
    {
        Encrypt,
        Decrypt
    }

    internal class PkwareTraditionalCryptoStream : Stream
    {
        private readonly PkwareTraditionalEncryptionData _encryptor;
        private readonly CryptoMode _mode;
        private readonly Stream _stream;
        private bool _isDisposed;

        public PkwareTraditionalCryptoStream(Stream stream, PkwareTraditionalEncryptionData encryptor, CryptoMode mode)
        {
            this._encryptor = encryptor;
            this._stream = stream;
            this._mode = mode;
        }

        public override bool CanRead => (_mode == CryptoMode.Decrypt);

        public override bool CanSeek => false;

        public override bool CanWrite => (_mode == CryptoMode.Encrypt);

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_mode == CryptoMode.Encrypt)
            {
                throw new NotSupportedException("This stream does not encrypt via Read()");
            }

            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            byte[] temp = new byte[count];
            int readBytes = _stream.Read(temp, 0, count);
            byte[] decrypted = _encryptor.Decrypt(temp, readBytes);
            Buffer.BlockCopy(decrypted, 0, buffer, offset, readBytes);
            return readBytes;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_mode == CryptoMode.Decrypt)
            {
                throw new NotSupportedException("This stream does not Decrypt via Write()");
            }

            if (count == 0)
            {
                return;
            }

            byte[] plaintext = null;
            if (offset != 0)
            {
                plaintext = new byte[count];
                Buffer.BlockCopy(buffer, offset, plaintext, 0, count);
            }
            else
            {
                plaintext = buffer;
            }

            byte[] encrypted = _encryptor.Encrypt(plaintext, count);
            _stream.Write(encrypted, 0, encrypted.Length);
        }

        public override void Flush()
        {
            //throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            base.Dispose(disposing);
            _stream.Dispose();
        }
    }
}