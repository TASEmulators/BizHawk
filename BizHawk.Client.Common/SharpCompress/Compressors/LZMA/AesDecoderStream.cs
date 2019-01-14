
#if !NO_CRYPTO
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using SharpCompress.Compressors.LZMA.Utilites;

namespace SharpCompress.Compressors.LZMA
{
    internal class AesDecoderStream : DecoderStream2
    {
        #region Variables

        private readonly Stream mStream;
        private readonly ICryptoTransform mDecoder;
        private readonly byte[] mBuffer;
        private long mWritten;
        private readonly long mLimit;
        private int mOffset;
        private int mEnding;
        private int mUnderflow;
        private bool isDisposed;

        #endregion

        #region Stream Methods

        public AesDecoderStream(Stream input, byte[] info, IPasswordProvider pass, long limit)
        {
            mStream = input;
            mLimit = limit;

            if (((uint) input.Length & 15) != 0)
                throw new NotSupportedException("AES decoder does not support padding.");

            int numCyclesPower;
            byte[] salt, seed;
            Init(info, out numCyclesPower, out salt, out seed);

            byte[] password = Encoding.Unicode.GetBytes(pass.CryptoGetTextPassword());
            byte[] key = InitKey(numCyclesPower, salt, password);

            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;
                mDecoder = aes.CreateDecryptor(key, seed);
            }

            mBuffer = new byte[4 << 10];
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (isDisposed)
                {
                    return;
                }
                isDisposed = true;
                if (disposing)
                {
                    mStream.Dispose();
                    mDecoder.Dispose();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public override long Position
        {
            get
            {
                return mWritten;
            }
        }

        public override long Length
        {
            get
            {
                return mLimit;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count == 0
                || mWritten == mLimit)
                return 0;

            if (mUnderflow > 0)
                return HandleUnderflow(buffer, offset, count);

            // Need at least 16 bytes to proceed.
            if (mEnding - mOffset < 16)
            {
                Buffer.BlockCopy(mBuffer, mOffset, mBuffer, 0, mEnding - mOffset);
                mEnding -= mOffset;
                mOffset = 0;

                do
                {
                    int read = mStream.Read(mBuffer, mEnding, mBuffer.Length - mEnding);
                    if (read == 0)
                    {
                        // We are not done decoding and have less than 16 bytes.
                        throw new EndOfStreamException();
                    }

                    mEnding += read;
                }
                while (mEnding - mOffset < 16);
            }

            // We shouldn't return more data than we are limited to.
            // Currently this is handled by forcing an underflow if
            // the stream length is not a multiple of the block size.
            if (count > mLimit - mWritten)
                count = (int) (mLimit - mWritten);

            // We cannot transform less than 16 bytes into the target buffer,
            // but we also cannot return zero, so we need to handle this.
            // We transform the data locally and use our own buffer as cache.
            if (count < 16)
                return HandleUnderflow(buffer, offset, count);

            if (count > mEnding - mOffset)
                count = mEnding - mOffset;

            // Otherwise we transform directly into the target buffer.
            int processed = mDecoder.TransformBlock(mBuffer, mOffset, count & ~15, buffer, offset);
            mOffset += processed;
            mWritten += processed;
            return processed;
        }

        #endregion

        #region Private Methods

        private void Init(byte[] info, out int numCyclesPower, out byte[] salt, out byte[] iv)
        {
            byte bt = info[0];
            numCyclesPower = bt & 0x3F;

            if ((bt & 0xC0) == 0)
            {
                salt = new byte[0];
                iv = new byte[0];
                return;
            }

            int saltSize = (bt >> 7) & 1;
            int ivSize = (bt >> 6) & 1;
            if (info.Length == 1)
                throw new InvalidOperationException();

            byte bt2 = info[1];
            saltSize += (bt2 >> 4);
            ivSize += (bt2 & 15);
            if (info.Length < 2 + saltSize + ivSize)
                throw new InvalidOperationException();

            salt = new byte[saltSize];
            for (int i = 0; i < saltSize; i++)
                salt[i] = info[i + 2];

            iv = new byte[16];
            for (int i = 0; i < ivSize; i++)
                iv[i] = info[i + saltSize + 2];

            if (numCyclesPower > 24)
                throw new NotSupportedException();
        }

        private byte[] InitKey(int mNumCyclesPower, byte[] salt, byte[] pass)
        {
            if (mNumCyclesPower == 0x3F)
            {
                var key = new byte[32];

                int pos;
                for (pos = 0; pos < salt.Length; pos++)
                    key[pos] = salt[pos];
                for (int i = 0; i < pass.Length && pos < 32; i++)
                    key[pos++] = pass[i];

                return key;
            }
            else
            {
#if NETSTANDARD1_3
                using (IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
                {
                    byte[] counter = new byte[8];
                    long numRounds = 1L << mNumCyclesPower;
                    for (long round = 0; round < numRounds; round++)
                    {
                        sha.AppendData(salt, 0, salt.Length);
                        sha.AppendData(pass, 0, pass.Length);
                        sha.AppendData(counter, 0, 8);

                        // This mirrors the counter so we don't have to convert long to byte[] each round.
                        // (It also ensures the counter is little endian, which BitConverter does not.)
                        for (int i = 0; i < 8; i++)
                            if (++counter[i] != 0)
                                break;
                    }
                    return sha.GetHashAndReset();
                }
#else
                using (var sha = SHA256.Create())
                {
                    byte[] counter = new byte[8];
                    long numRounds = 1L << mNumCyclesPower;
                    for (long round = 0; round < numRounds; round++)
                    {
                        sha.TransformBlock(salt, 0, salt.Length, null, 0);
                        sha.TransformBlock(pass, 0, pass.Length, null, 0);
                        sha.TransformBlock(counter, 0, 8, null, 0);

                        // This mirrors the counter so we don't have to convert long to byte[] each round.
                        // (It also ensures the counter is little endian, which BitConverter does not.)
                        for (int i = 0; i < 8; i++)
                            if (++counter[i] != 0)
                                break;
                    }

                    sha.TransformFinalBlock(counter, 0, 0);
                    return sha.Hash;
                }
#endif
            }
        }

        private int HandleUnderflow(byte[] buffer, int offset, int count)
        {
            // If this is zero we were called to create a new underflow buffer.
            // Just transform as much as possible so we can feed from it as long as possible.
            if (mUnderflow == 0)
            {
                int blockSize = (mEnding - mOffset) & ~15;
                mUnderflow = mDecoder.TransformBlock(mBuffer, mOffset, blockSize, mBuffer, mOffset);
            }

            if (count > mUnderflow)
                count = mUnderflow;

            Buffer.BlockCopy(mBuffer, mOffset, buffer, offset, count);
            mWritten += count;
            mOffset += count;
            mUnderflow -= count;
            return count;
        }

        #endregion
    }
}

#endif