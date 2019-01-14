using System;
using System.IO;
using SharpCompress.Compressors.LZMA.RangeCoder;
using SharpCompress.Compressors.PPMd.H;
using SharpCompress.Compressors.PPMd.I1;

namespace SharpCompress.Compressors.PPMd
{
    public class PpmdStream : Stream
    {
        private readonly PpmdProperties _properties;
        private readonly Stream _stream;
        private readonly bool _compress;
        private readonly Model _model;
        private readonly ModelPpm _modelH;
        private readonly Decoder _decoder;
        private long _position;
        private bool _isDisposed;

        public PpmdStream(PpmdProperties properties, Stream stream, bool compress)
        {
            _properties = properties;
            _stream = stream;
            _compress = compress;

            if (properties.Version == PpmdVersion.I1)
            {
                _model = new Model();
                if (compress)
                {
                    _model.EncodeStart(properties);
                }
                else
                {
                    _model.DecodeStart(stream, properties);
                }
            }
            if (properties.Version == PpmdVersion.H)
            {
                _modelH = new ModelPpm();
                if (compress)
                {
                    throw new NotImplementedException();
                }
                _modelH.DecodeInit(stream, properties.ModelOrder, properties.AllocatorSize);
            }
            if (properties.Version == PpmdVersion.H7Z)
            {
                _modelH = new ModelPpm();
                if (compress)
                {
                    throw new NotImplementedException();
                }
                _modelH.DecodeInit(null, properties.ModelOrder, properties.AllocatorSize);
                _decoder = new Decoder();
                _decoder.Init(stream);
            }
        }

        public override bool CanRead => !_compress;

        public override bool CanSeek => false;

        public override bool CanWrite => _compress;

        public override void Flush()
        {
        }

        protected override void Dispose(bool isDisposing)
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            if (isDisposing)
            {
                if (_compress)
                {
                    _model.EncodeBlock(_stream, new MemoryStream(), true);
                }
            }
            base.Dispose(isDisposing);
        }

        public override long Length => throw new NotSupportedException();

        public override long Position { get => _position; set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_compress)
            {
                return 0;
            }
            int size = 0;
            if (_properties.Version == PpmdVersion.I1)
            {
                size = _model.DecodeBlock(_stream, buffer, offset, count);
            }
            if (_properties.Version == PpmdVersion.H)
            {
                int c;
                while (size < count && (c = _modelH.DecodeChar()) >= 0)
                {
                    buffer[offset++] = (byte)c;
                    size++;
                }
            }
            if (_properties.Version == PpmdVersion.H7Z)
            {
                int c;
                while (size < count && (c = _modelH.DecodeChar(_decoder)) >= 0)
                {
                    buffer[offset++] = (byte)c;
                    size++;
                }
            }
            _position += size;
            return size;
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
            if (_compress)
            {
                _model.EncodeBlock(_stream, new MemoryStream(buffer, offset, count), false);
            }
        }
    }
}