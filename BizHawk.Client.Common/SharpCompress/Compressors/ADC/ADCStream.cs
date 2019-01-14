//
// ADC.cs
//
// Author:
//       Natalia Portillo <claunia@claunia.com>
//
// Copyright (c) 2016 © Claunia.com
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.IO;

namespace SharpCompress.Compressors.ADC
{
    /// <summary>
    /// Provides a forward readable only stream that decompresses ADC data
    /// </summary>
    public class ADCStream : Stream
    {
        /// <summary>
        /// This stream holds the compressed data
        /// </summary>
        private readonly Stream _stream;

        /// <summary>
        /// Is this instance disposed?
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// Position in decompressed data
        /// </summary>
        private long _position;

        /// <summary>
        /// Buffer with currently used chunk of decompressed data
        /// </summary>
        private byte[] _outBuffer;

        /// <summary>
        /// Position in buffer of decompressed data
        /// </summary>
        private int _outPosition;

        /// <summary>
        /// Initializates a stream that decompresses ADC data on the fly
        /// </summary>
        /// <param name="stream">Stream that contains the compressed data</param>
        /// <param name="compressionMode">Must be set to <see cref="CompressionMode.Decompress"/> because compression is not implemented</param>
        public ADCStream(Stream stream, CompressionMode compressionMode = CompressionMode.Decompress)
        {
            if (compressionMode == CompressionMode.Compress)
            {
                throw new NotSupportedException();
            }

            _stream = stream;
        }

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => _position; set => throw new NotSupportedException(); }

        public override void Flush()
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            base.Dispose(disposing);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count == 0)
            {
                return 0;
            }
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (offset < buffer.GetLowerBound(0))
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if ((offset + count) > buffer.GetLength(0))
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            int size = -1;

            if (_outBuffer == null)
            {
                size = ADCBase.Decompress(_stream, out _outBuffer);
                _outPosition = 0;
            }

            int inPosition = offset;
            int toCopy = count;
            int copied = 0;

            while (_outPosition + toCopy >= _outBuffer.Length)
            {
                int piece = _outBuffer.Length - _outPosition;
                Array.Copy(_outBuffer, _outPosition, buffer, inPosition, piece);
                inPosition += piece;
                copied += piece;
                _position += piece;
                toCopy -= piece;
                size = ADCBase.Decompress(_stream, out _outBuffer);
                _outPosition = 0;
                if (size == 0 || _outBuffer == null || _outBuffer.Length == 0)
                {
                    return copied;
                }
            }

            Array.Copy(_outBuffer, _outPosition, buffer, inPosition, toCopy);
            _outPosition += toCopy;
            _position += toCopy;
            copied += toCopy;
            return copied;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}