using System;
using System.Collections.Generic;
using System.IO;
using SharpCompress.Common;
using SharpCompress.Common.Rar;

namespace SharpCompress.Compressors.Rar
{
    internal class MultiVolumeReadOnlyStream : Stream
    {
        private long currentPosition;
        private long maxPosition;

        private IEnumerator<RarFilePart> filePartEnumerator;
        private Stream currentStream;

        private readonly IExtractionListener streamListener;

        private long currentPartTotalReadBytes;
        private long currentEntryTotalReadBytes;

        internal MultiVolumeReadOnlyStream(IEnumerable<RarFilePart> parts, IExtractionListener streamListener)
        {
            this.streamListener = streamListener;

            filePartEnumerator = parts.GetEnumerator();
            filePartEnumerator.MoveNext();
            InitializeNextFilePart();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (filePartEnumerator != null)
                {
                    filePartEnumerator.Dispose();
                    filePartEnumerator = null;
                }
                currentStream = null;
            }
        }

        private void InitializeNextFilePart()
        {
            maxPosition = filePartEnumerator.Current.FileHeader.CompressedSize;
            currentPosition = 0;
            currentStream = filePartEnumerator.Current.GetCompressedStream();

            currentPartTotalReadBytes = 0;

            CurrentCrc = filePartEnumerator.Current.FileHeader.FileCrc;

            streamListener.FireFilePartExtractionBegin(filePartEnumerator.Current.FilePartName,
                                                       filePartEnumerator.Current.FileHeader.CompressedSize,
                                                       filePartEnumerator.Current.FileHeader.UncompressedSize);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            int currentOffset = offset;
            int currentCount = count;
            while (currentCount > 0)
            {
                int readSize = currentCount;
                if (currentCount > maxPosition - currentPosition)
                {
                    readSize = (int)(maxPosition - currentPosition);
                }

                int read = currentStream.Read(buffer, currentOffset, readSize);
                if (read < 0)
                {
                    throw new EndOfStreamException();
                }

                currentPosition += read;
                currentOffset += read;
                currentCount -= read;
                totalRead += read;
                if (((maxPosition - currentPosition) == 0)
                    && filePartEnumerator.Current.FileHeader.IsSplitAfter)
                {
                    if (filePartEnumerator.Current.FileHeader.R4Salt != null)
                    {
                        throw new InvalidFormatException("Sharpcompress currently does not support multi-volume decryption.");
                    }
                    string fileName = filePartEnumerator.Current.FileHeader.FileName;
                    if (!filePartEnumerator.MoveNext())
                    {
                        throw new InvalidFormatException(
                                                         "Multi-part rar file is incomplete.  Entry expects a new volume: " + fileName);
                    }
                    InitializeNextFilePart();
                }
                else
                {
                    break;
                }
            }
            currentPartTotalReadBytes += totalRead;
            currentEntryTotalReadBytes += totalRead;
            streamListener.FireCompressedBytesRead(currentPartTotalReadBytes, currentEntryTotalReadBytes);
            return totalRead;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public uint CurrentCrc { get; private set; }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

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