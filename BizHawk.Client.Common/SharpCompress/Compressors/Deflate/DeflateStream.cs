// DeflateStream.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2009-2010 Dino Chiesa.
// All rights reserved.
//
// This code module is part of DotNetZip, a zipfile class library.
//
// ------------------------------------------------------------------
//
// This code is licensed under the Microsoft Public License.
// See the file License.txt for the license details.
// More info on: http://dotnetzip.codeplex.com
//
// ------------------------------------------------------------------
//
// last saved (in emacs):
// Time-stamp: <2010-February-05 08:49:04>
//
// ------------------------------------------------------------------
//
// This module defines the DeflateStream class, which can be used as a replacement for
// the System.IO.Compression.DeflateStream class in the .NET BCL.
//
// ------------------------------------------------------------------

using System;
using System.IO;
using System.Text;

namespace SharpCompress.Compressors.Deflate
{
    public class DeflateStream : Stream
    {
        private readonly ZlibBaseStream _baseStream;
        private bool _disposed;

        public DeflateStream(Stream stream, CompressionMode mode,
                             CompressionLevel level = CompressionLevel.Default,
                             Encoding forceEncoding = null)
        {
            _baseStream = new ZlibBaseStream(stream, mode, level, ZlibStreamFlavor.DEFLATE, forceEncoding);
        }

        #region Zlib properties

        /// <summary>
        /// This property sets the flush behavior on the stream.
        /// </summary>
        /// <remarks> See the ZLIB documentation for the meaning of the flush behavior.
        /// </remarks>
        public virtual FlushType FlushMode
        {
            get => (_baseStream._flushMode);
            set
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException("DeflateStream");
                }
                _baseStream._flushMode = value;
            }
        }

        /// <summary>
        ///   The size of the working buffer for the compression codec.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        ///   The working buffer is used for all stream operations.  The default size is
        ///   1024 bytes.  The minimum size is 128 bytes. You may get better performance
        ///   with a larger buffer.  Then again, you might not.  You would have to test
        ///   it.
        /// </para>
        ///
        /// <para>
        ///   Set this before the first call to <c>Read()</c> or <c>Write()</c> on the
        ///   stream. If you try to set it afterwards, it will throw.
        /// </para>
        /// </remarks>
        public int BufferSize
        {
            get => _baseStream._bufferSize;
            set
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException("DeflateStream");
                }
                if (_baseStream._workingBuffer != null)
                {
                    throw new ZlibException("The working buffer is already set.");
                }
                if (value < ZlibConstants.WorkingBufferSizeMin)
                {
                    throw new ZlibException(
                                            String.Format("Don't be silly. {0} bytes?? Use a bigger buffer, at least {1}.", value,
                                                          ZlibConstants.WorkingBufferSizeMin));
                }
                _baseStream._bufferSize = value;
            }
        }

        /// <summary>
        ///   The ZLIB strategy to be used during compression.
        /// </summary>
        ///
        /// <remarks>
        ///   By tweaking this parameter, you may be able to optimize the compression for
        ///   data with particular characteristics.
        /// </remarks>
        public CompressionStrategy Strategy
        {
            get => _baseStream.Strategy;
            set
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException("DeflateStream");
                }
                _baseStream.Strategy = value;
            }
        }

        /// <summary> Returns the total number of bytes input so far.</summary>
        public virtual long TotalIn => _baseStream._z.TotalBytesIn;

        /// <summary> Returns the total number of bytes output so far.</summary>
        public virtual long TotalOut => _baseStream._z.TotalBytesOut;

        #endregion

        #region System.IO.Stream methods

        /// <summary>
        /// Indicates whether the stream can be read.
        /// </summary>
        /// <remarks>
        /// The return value depends on whether the captive stream supports reading.
        /// </remarks>
        public override bool CanRead
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException("DeflateStream");
                }
                return _baseStream._stream.CanRead;
            }
        }

        /// <summary>
        /// Indicates whether the stream supports Seek operations.
        /// </summary>
        /// <remarks>
        /// Always returns false.
        /// </remarks>
        public override bool CanSeek => false;

        /// <summary>
        /// Indicates whether the stream can be written.
        /// </summary>
        /// <remarks>
        /// The return value depends on whether the captive stream supports writing.
        /// </remarks>
        public override bool CanWrite
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException("DeflateStream");
                }
                return _baseStream._stream.CanWrite;
            }
        }

        /// <summary>
        /// Reading this property always throws a <see cref="NotImplementedException"/>.
        /// </summary>
        public override long Length => throw new NotSupportedException();

        /// <summary>
        /// The position of the stream pointer.
        /// </summary>
        ///
        /// <remarks>
        ///   Setting this property always throws a <see
        ///   cref="NotImplementedException"/>. Reading will return the total bytes
        ///   written out, if used in writing, or the total bytes read in, if used in
        ///   reading.  The count may refer to compressed bytes or uncompressed bytes,
        ///   depending on how you've used the stream.
        /// </remarks>
        public override long Position
        {
            get
            {
                if (_baseStream._streamMode == ZlibBaseStream.StreamMode.Writer)
                {
                    return _baseStream._z.TotalBytesOut;
                }
                if (_baseStream._streamMode == ZlibBaseStream.StreamMode.Reader)
                {
                    return _baseStream._z.TotalBytesIn;
                }
                return 0;
            }
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// Dispose the stream.
        /// </summary>
        /// <remarks>
        /// This may or may not result in a <c>Close()</c> call on the captive stream.
        /// </remarks>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!_disposed)
                {
                    if (disposing)
                    {
                        _baseStream?.Dispose();
                    }
                    _disposed = true;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// Flush the stream.
        /// </summary>
        public override void Flush()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("DeflateStream");
            }
            _baseStream.Flush();
        }

        /// <summary>
        /// Read data from the stream.
        /// </summary>
        /// <remarks>
        ///
        /// <para>
        ///   If you wish to use the <c>DeflateStream</c> to compress data while
        ///   reading, you can create a <c>DeflateStream</c> with
        ///   <c>CompressionMode.Compress</c>, providing an uncompressed data stream.
        ///   Then call Read() on that <c>DeflateStream</c>, and the data read will be
        ///   compressed as you read.  If you wish to use the <c>DeflateStream</c> to
        ///   decompress data while reading, you can create a <c>DeflateStream</c> with
        ///   <c>CompressionMode.Decompress</c>, providing a readable compressed data
        ///   stream.  Then call Read() on that <c>DeflateStream</c>, and the data read
        ///   will be decompressed as you read.
        /// </para>
        ///
        /// <para>
        ///   A <c>DeflateStream</c> can be used for <c>Read()</c> or <c>Write()</c>, but not both.
        /// </para>
        ///
        /// </remarks>
        /// <param name="buffer">The buffer into which the read data should be placed.</param>
        /// <param name="offset">the offset within that data array to put the first byte read.</param>
        /// <param name="count">the number of bytes to read.</param>
        /// <returns>the number of bytes actually read</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("DeflateStream");
            }
            return _baseStream.Read(buffer, offset, count);
        }

        public override int ReadByte()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("DeflateStream");
            }
            return _baseStream.ReadByte();
        }

        /// <summary>
        /// Calling this method always throws a <see cref="NotImplementedException"/>.
        /// </summary>
        /// <param name="offset">this is irrelevant, since it will always throw!</param>
        /// <param name="origin">this is irrelevant, since it will always throw!</param>
        /// <returns>irrelevant!</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Calling this method always throws a <see cref="NotImplementedException"/>.
        /// </summary>
        /// <param name="value">this is irrelevant, since it will always throw!</param>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///   Write data to the stream.
        /// </summary>
        /// <remarks>
        ///
        /// <para>
        ///   If you wish to use the <c>DeflateStream</c> to compress data while
        ///   writing, you can create a <c>DeflateStream</c> with
        ///   <c>CompressionMode.Compress</c>, and a writable output stream.  Then call
        ///   <c>Write()</c> on that <c>DeflateStream</c>, providing uncompressed data
        ///   as input.  The data sent to the output stream will be the compressed form
        ///   of the data written.  If you wish to use the <c>DeflateStream</c> to
        ///   decompress data while writing, you can create a <c>DeflateStream</c> with
        ///   <c>CompressionMode.Decompress</c>, and a writable output stream.  Then
        ///   call <c>Write()</c> on that stream, providing previously compressed
        ///   data. The data sent to the output stream will be the decompressed form of
        ///   the data written.
        /// </para>
        ///
        /// <para>
        ///   A <c>DeflateStream</c> can be used for <c>Read()</c> or <c>Write()</c>,
        ///   but not both.
        /// </para>
        ///
        /// </remarks>
        ///
        /// <param name="buffer">The buffer holding data to write to the stream.</param>
        /// <param name="offset">the offset within that data array to find the first byte to write.</param>
        /// <param name="count">the number of bytes to write.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("DeflateStream");
            }
            _baseStream.Write(buffer, offset, count);
        }

        public override void WriteByte(byte value)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("DeflateStream");
            }
            _baseStream.WriteByte(value);
        }

        #endregion

        public MemoryStream InputBuffer => new MemoryStream(_baseStream._z.InputBuffer, _baseStream._z.NextIn,
                                                            _baseStream._z.AvailableBytesIn);
    }
}