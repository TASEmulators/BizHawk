// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using SharpCompress.Common.Zip;
using SharpCompress.Compressors.Deflate;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace SharpCompress.Compressors.Deflate64
{
    public sealed class Deflate64Stream : Stream
    {
        private const int DEFAULT_BUFFER_SIZE = 8192;

        private Stream _stream;
        private CompressionMode _mode;
        private InflaterManaged _inflater;
        private byte[] _buffer;

        public Deflate64Stream(Stream stream, CompressionMode mode)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (mode != CompressionMode.Decompress)
                throw new NotImplementedException("Deflate64: this implementation only supports decompression");
            if (!stream.CanRead)
                throw new ArgumentException("Deflate64: input stream is not readable", nameof(stream));

            InitializeInflater(stream, ZipCompressionMethod.Deflate64);
        }

        /// <summary>
        /// Sets up this DeflateManagedStream to be used for Inflation/Decompression
        /// </summary>
        private void InitializeInflater(Stream stream, ZipCompressionMethod method = ZipCompressionMethod.Deflate)
        {
            Debug.Assert(stream != null);
            Debug.Assert(method == ZipCompressionMethod.Deflate || method == ZipCompressionMethod.Deflate64);
            if (!stream.CanRead)
                throw new ArgumentException("Deflate64: input stream is not readable", nameof(stream));

            _inflater = new InflaterManaged(method == ZipCompressionMethod.Deflate64);

            _stream = stream;
            _mode = CompressionMode.Decompress;
            _buffer = new byte[DEFAULT_BUFFER_SIZE];
        }

        public override bool CanRead
        {
            get
            {
                if (_stream == null)
                {
                    return false;
                }

                return (_mode == CompressionMode.Decompress && _stream.CanRead);
            }
        }

        public override bool CanWrite
        {
            get
            {
                if (_stream == null)
                {
                    return false;
                }

                return (_mode == CompressionMode.Compress && _stream.CanWrite);
            }
        }

        public override bool CanSeek => false;

        public override long Length
        {
            get { throw new NotSupportedException("Deflate64: not supported"); }
        }

        public override long Position
        {
            get { throw new NotSupportedException("Deflate64: not supported"); }
            set { throw new NotSupportedException("Deflate64: not supported"); }
        }

        public override void Flush()
        {
            EnsureNotDisposed();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("Deflate64: not supported");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("Deflate64: not supported");
        }

        public override int Read(byte[] array, int offset, int count)
        {
            EnsureDecompressionMode();
            ValidateParameters(array, offset, count);
            EnsureNotDisposed();

            int bytesRead;
            int currentOffset = offset;
            int remainingCount = count;

            while (true)
            {
                bytesRead = _inflater.Inflate(array, currentOffset, remainingCount);
                currentOffset += bytesRead;
                remainingCount -= bytesRead;

                if (remainingCount == 0)
                {
                    break;
                }

                if (_inflater.Finished())
                {
                    // if we finished decompressing, we can't have anything left in the outputwindow.
                    Debug.Assert(_inflater.AvailableOutput == 0, "We should have copied all stuff out!");
                    break;
                }

                int bytes = _stream.Read(_buffer, 0, _buffer.Length);
                if (bytes <= 0)
                {
                    break;
                }
                else if (bytes > _buffer.Length)
                {
                    // The stream is either malicious or poorly implemented and returned a number of
                    // bytes larger than the buffer supplied to it.
                    throw new InvalidDataException("Deflate64: invalid data");
                }

                _inflater.SetInput(_buffer, 0, bytes);
            }

            return count - remainingCount;
        }

        private void ValidateParameters(byte[] array, int offset, int count)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (array.Length - offset < count)
                throw new ArgumentException("Deflate64: invalid offset/count combination");
        }

        private void EnsureNotDisposed()
        {
            if (_stream == null)
                ThrowStreamClosedException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowStreamClosedException()
        {
            throw new ObjectDisposedException(null, "Deflate64: stream has been disposed");
        }

        private void EnsureDecompressionMode()
        {
            if (_mode != CompressionMode.Decompress)
                ThrowCannotReadFromDeflateManagedStreamException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowCannotReadFromDeflateManagedStreamException()
        {
            throw new InvalidOperationException("Deflate64: cannot read from this stream");
        }

        private void EnsureCompressionMode()
        {
            if (_mode != CompressionMode.Compress)
                ThrowCannotWriteToDeflateManagedStreamException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowCannotWriteToDeflateManagedStreamException()
        {
            throw new InvalidOperationException("Deflate64: cannot write to this stream");
        }

        public override void Write(byte[] array, int offset, int count)
        {
            ThrowCannotWriteToDeflateManagedStreamException();
        }

        // This is called by Dispose:
        private void PurgeBuffers(bool disposing)
        {
            if (!disposing)
                return;

            if (_stream == null)
                return;

            Flush();
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                PurgeBuffers(disposing);
            }
            finally
            {
                // Close the underlying stream even if PurgeBuffers threw.
                // Stream.Close() may throw here (may or may not be due to the same error).
                // In this case, we still need to clean up internal resources, hence the inner finally blocks.
                try
                {
                    if (disposing)
                    {
                        _stream?.Dispose();
                    }
                }
                finally
                {
                    _stream = null;

                    try
                    {
                        _inflater?.Dispose();
                    }
                    finally
                    {
                        _inflater = null;
                        base.Dispose(disposing);
                    }
                }
            }
        }
    }
}
