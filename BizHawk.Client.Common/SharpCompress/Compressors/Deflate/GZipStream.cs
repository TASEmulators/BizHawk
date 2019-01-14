// GZipStream.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2009 Dino Chiesa and Microsoft Corporation.
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
// Time-stamp: <2010-January-09 12:04:28>
//
// ------------------------------------------------------------------
//
// This module defines the GZipStream class, which can be used as a replacement for
// the System.IO.Compression.GZipStream class in the .NET BCL.  NB: The design is not
// completely OO clean: there is some intelligence in the ZlibBaseStream that reads the
// GZip header.
//
// ------------------------------------------------------------------

using System;
using System.IO;
using SharpCompress.Common;
using SharpCompress.Converters;
using System.Text;

namespace SharpCompress.Compressors.Deflate
{
    public class GZipStream : Stream
    {
        internal static readonly DateTime UNIX_EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public DateTime? LastModified { get; set; }

        private string _comment;
        private string _fileName;

        internal ZlibBaseStream BaseStream;
        private bool _disposed;
        private bool _firstReadDone;
        private int _headerByteCount;

        private readonly Encoding _encoding;

        public GZipStream(Stream stream, CompressionMode mode)
            : this(stream, mode, CompressionLevel.Default, Encoding.UTF8)
        {
        }

        public GZipStream(Stream stream, CompressionMode mode, CompressionLevel level)
            : this(stream, mode, level, Encoding.UTF8)
        {
        }

        public GZipStream(Stream stream, CompressionMode mode, CompressionLevel level, Encoding encoding)
        {
            BaseStream = new ZlibBaseStream(stream, mode, level, ZlibStreamFlavor.GZIP, encoding);
            _encoding = encoding;
        }

        #region Zlib properties

        public virtual FlushType FlushMode
        {
            get => (BaseStream._flushMode);
            set
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException("GZipStream");
                }
                BaseStream._flushMode = value;
            }
        }

        public int BufferSize
        {
            get => BaseStream._bufferSize;
            set
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException("GZipStream");
                }
                if (BaseStream._workingBuffer != null)
                {
                    throw new ZlibException("The working buffer is already set.");
                }
                if (value < ZlibConstants.WorkingBufferSizeMin)
                {
                    throw new ZlibException(
                                            String.Format("Don't be silly. {0} bytes?? Use a bigger buffer, at least {1}.", value,
                                                          ZlibConstants.WorkingBufferSizeMin));
                }
                BaseStream._bufferSize = value;
            }
        }

        internal virtual long TotalIn => BaseStream._z.TotalBytesIn;

        internal virtual long TotalOut => BaseStream._z.TotalBytesOut;

        #endregion

        #region Stream methods

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
                    throw new ObjectDisposedException("GZipStream");
                }
                return BaseStream._stream.CanRead;
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
                    throw new ObjectDisposedException("GZipStream");
                }
                return BaseStream._stream.CanWrite;
            }
        }

        /// <summary>
        /// Reading this property always throws a <see cref="NotImplementedException"/>.
        /// </summary>
        public override long Length => throw new NotSupportedException();

        /// <summary>
        ///   The position of the stream pointer.
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
                if (BaseStream._streamMode == ZlibBaseStream.StreamMode.Writer)
                {
                    return BaseStream._z.TotalBytesOut + _headerByteCount;
                }
                if (BaseStream._streamMode == ZlibBaseStream.StreamMode.Reader)
                {
                    return BaseStream._z.TotalBytesIn + BaseStream._gzipHeaderByteCount;
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
                    if (disposing && (BaseStream != null))
                    {
                        BaseStream.Dispose();
                        Crc32 = BaseStream.Crc32;
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
                throw new ObjectDisposedException("GZipStream");
            }
            BaseStream.Flush();
        }

        /// <summary>
        ///   Read and decompress data from the source stream.
        /// </summary>
        ///
        /// <remarks>
        ///   With a <c>GZipStream</c>, decompression is done through reading.
        /// </remarks>
        ///
        /// <example>
        /// <code>
        /// byte[] working = new byte[WORKING_BUFFER_SIZE];
        /// using (System.IO.Stream input = System.IO.File.OpenRead(_CompressedFile))
        /// {
        ///     using (Stream decompressor= new Ionic.Zlib.GZipStream(input, CompressionMode.Decompress, true))
        ///     {
        ///         using (var output = System.IO.File.Create(_DecompressedFile))
        ///         {
        ///             int n;
        ///             while ((n= decompressor.Read(working, 0, working.Length)) !=0)
        ///             {
        ///                 output.Write(working, 0, n);
        ///             }
        ///         }
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <param name="buffer">The buffer into which the decompressed data should be placed.</param>
        /// <param name="offset">the offset within that data array to put the first byte read.</param>
        /// <param name="count">the number of bytes to read.</param>
        /// <returns>the number of bytes actually read</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("GZipStream");
            }
            int n = BaseStream.Read(buffer, offset, count);

            // Console.WriteLine("GZipStream::Read(buffer, off({0}), c({1}) = {2}", offset, count, n);
            // Console.WriteLine( Util.FormatByteArray(buffer, offset, n) );

            if (!_firstReadDone)
            {
                _firstReadDone = true;
                FileName = BaseStream._GzipFileName;
                Comment = BaseStream._GzipComment;
            }
            return n;
        }

        /// <summary>
        ///   Calling this method always throws a <see cref="NotImplementedException"/>.
        /// </summary>
        /// <param name="offset">irrelevant; it will always throw!</param>
        /// <param name="origin">irrelevant; it will always throw!</param>
        /// <returns>irrelevant!</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///   Calling this method always throws a <see cref="NotImplementedException"/>.
        /// </summary>
        /// <param name="value">irrelevant; this method will always throw!</param>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///   Write data to the stream.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        ///   If you wish to use the <c>GZipStream</c> to compress data while writing,
        ///   you can create a <c>GZipStream</c> with <c>CompressionMode.Compress</c>, and a
        ///   writable output stream.  Then call <c>Write()</c> on that <c>GZipStream</c>,
        ///   providing uncompressed data as input.  The data sent to the output stream
        ///   will be the compressed form of the data written.
        /// </para>
        ///
        /// <para>
        ///   A <c>GZipStream</c> can be used for <c>Read()</c> or <c>Write()</c>, but not
        ///   both. Writing implies compression.  Reading implies decompression.
        /// </para>
        ///
        /// </remarks>
        /// <param name="buffer">The buffer holding data to write to the stream.</param>
        /// <param name="offset">the offset within that data array to find the first byte to write.</param>
        /// <param name="count">the number of bytes to write.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("GZipStream");
            }
            if (BaseStream._streamMode == ZlibBaseStream.StreamMode.Undefined)
            {
                //Console.WriteLine("GZipStream: First write");
                if (BaseStream._wantCompress)
                {
                    // first write in compression, therefore, emit the GZIP header
                    _headerByteCount = EmitHeader();
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            BaseStream.Write(buffer, offset, count);
        }

        #endregion Stream methods

        public String Comment
        {
            get => _comment;
            set
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException("GZipStream");
                }
                _comment = value;
            }
        }

        public string FileName
        {
            get => _fileName;
            set
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException("GZipStream");
                }
                _fileName = value;
                if (_fileName == null)
                {
                    return;
                }
                if (_fileName.IndexOf("/") != -1)
                {
                    _fileName = _fileName.Replace("/", "\\");
                }
                if (_fileName.EndsWith("\\"))
                {
                    throw new InvalidOperationException("Illegal filename");
                }

                var index = _fileName.IndexOf("\\");
                if (index != -1)
                {
                    // trim any leading path
                    int length = _fileName.Length;
                    int num = length;
                    while (--num >= 0)
                    {
                        char c = _fileName[num];
                        if (c == '\\')
                        {
                            _fileName = _fileName.Substring(num + 1, length - num - 1);
                        }
                    }
                }
            }
        }

        public int Crc32 { get; private set; }

        private int EmitHeader()
        {
            byte[] commentBytes = (Comment == null) ? null
                : _encoding.GetBytes(Comment);
            byte[] filenameBytes = (FileName == null) ? null
                : _encoding.GetBytes(FileName);

            int cbLength = (Comment == null) ? 0 : commentBytes.Length + 1;
            int fnLength = (FileName == null) ? 0 : filenameBytes.Length + 1;

            int bufferLength = 10 + cbLength + fnLength;
            var header = new byte[bufferLength];
            int i = 0;

            // ID
            header[i++] = 0x1F;
            header[i++] = 0x8B;

            // compression method
            header[i++] = 8;
            byte flag = 0;
            if (Comment != null)
            {
                flag ^= 0x10;
            }
            if (FileName != null)
            {
                flag ^= 0x8;
            }

            // flag
            header[i++] = flag;

            // mtime
            if (!LastModified.HasValue)
            {
                LastModified = DateTime.Now;
            }
            TimeSpan delta = LastModified.Value - UNIX_EPOCH;
            var timet = (Int32)delta.TotalSeconds;
            DataConverter.LittleEndian.PutBytes(header, i, timet);
            i += 4;

            // xflg
            header[i++] = 0; // this field is totally useless

            // OS
            header[i++] = 0xFF; // 0xFF == unspecified

            // extra field length - only if FEXTRA is set, which it is not.
            //header[i++]= 0;
            //header[i++]= 0;

            // filename
            if (fnLength != 0)
            {
                Array.Copy(filenameBytes, 0, header, i, fnLength - 1);
                i += fnLength - 1;
                header[i++] = 0; // terminate
            }

            // comment
            if (cbLength != 0)
            {
                Array.Copy(commentBytes, 0, header, i, cbLength - 1);
                i += cbLength - 1;
                header[i++] = 0; // terminate
            }

            BaseStream._stream.Write(header, 0, header.Length);

            return header.Length; // bytes written
        }
    }
}