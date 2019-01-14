using System;
using System.IO;
using SharpCompress.Compressors.LZMA.LZ;
using SharpCompress.Converters;

namespace SharpCompress.Compressors.LZMA
{
    public class LzmaStream : Stream
    {
        private readonly Stream _inputStream;
        private readonly long _inputSize;
        private readonly long _outputSize;

        private readonly int _dictionarySize;
        private readonly OutWindow _outWindow = new OutWindow();
        private readonly RangeCoder.Decoder _rangeDecoder = new RangeCoder.Decoder();
        private Decoder _decoder;

        private long _position;
        private bool _endReached;
        private long _availableBytes;
        private long _rangeDecoderLimit;
        private long _inputPosition;

        // LZMA2
        private readonly bool _isLzma2;
        private bool _uncompressedChunk;
        private bool _needDictReset = true;
        private bool _needProps = true;

        private readonly Encoder _encoder;
        private bool _isDisposed;

        public LzmaStream(byte[] properties, Stream inputStream)
            : this(properties, inputStream, -1, -1, null, properties.Length < 5)
        {
        }

        public LzmaStream(byte[] properties, Stream inputStream, long inputSize)
            : this(properties, inputStream, inputSize, -1, null, properties.Length < 5)
        {
        }

        public LzmaStream(byte[] properties, Stream inputStream, long inputSize, long outputSize)
            : this(properties, inputStream, inputSize, outputSize, null, properties.Length < 5)
        {
        }

        public LzmaStream(byte[] properties, Stream inputStream, long inputSize, long outputSize,
                          Stream presetDictionary, bool isLzma2)
        {
            _inputStream = inputStream;
            _inputSize = inputSize;
            _outputSize = outputSize;
            _isLzma2 = isLzma2;

            if (!isLzma2)
            {
                _dictionarySize = DataConverter.LittleEndian.GetInt32(properties, 1);
                _outWindow.Create(_dictionarySize);
                if (presetDictionary != null)
                {
                    _outWindow.Train(presetDictionary);
                }

                _rangeDecoder.Init(inputStream);

                _decoder = new Decoder();
                _decoder.SetDecoderProperties(properties);
                Properties = properties;

                _availableBytes = outputSize < 0 ? long.MaxValue : outputSize;
                _rangeDecoderLimit = inputSize;
            }
            else
            {
                _dictionarySize = 2 | (properties[0] & 1);
                _dictionarySize <<= (properties[0] >> 1) + 11;

                _outWindow.Create(_dictionarySize);
                if (presetDictionary != null)
                {
                    _outWindow.Train(presetDictionary);
                    _needDictReset = false;
                }

                Properties = new byte[1];
                _availableBytes = 0;
            }
        }

        public LzmaStream(LzmaEncoderProperties properties, bool isLzma2, Stream outputStream)
            : this(properties, isLzma2, null, outputStream)
        {
        }

        public LzmaStream(LzmaEncoderProperties properties, bool isLzma2, Stream presetDictionary, Stream outputStream)
        {
            _isLzma2 = isLzma2;
            _availableBytes = 0;
            _endReached = true;

            if (isLzma2)
            {
                throw new NotImplementedException();
            }

            _encoder = new Encoder();
            _encoder.SetCoderProperties(properties._propIDs, properties._properties);
            MemoryStream propStream = new MemoryStream(5);
            _encoder.WriteCoderProperties(propStream);
            Properties = propStream.ToArray();

            _encoder.SetStreams(null, outputStream, -1, -1);
            if (presetDictionary != null)
            {
                _encoder.Train(presetDictionary);
            }
        }

        public override bool CanRead => _encoder == null;

        public override bool CanSeek => false;

        public override bool CanWrite => _encoder != null;

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
            if (disposing)
            {
                if (_encoder != null)
                {
                    _position = _encoder.Code(null, true);
                }
                _inputStream?.Dispose();
            }
            base.Dispose(disposing);
        }

        public override long Length => _position + _availableBytes;

        public override long Position { get => _position; set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_endReached)
            {
                return 0;
            }

            int total = 0;
            while (total < count)
            {
                if (_availableBytes == 0)
                {
                    if (_isLzma2)
                    {
                        DecodeChunkHeader();
                    }
                    else
                    {
                        _endReached = true;
                    }
                    if (_endReached)
                    {
                        break;
                    }
                }

                int toProcess = count - total;
                if (toProcess > _availableBytes)
                {
                    toProcess = (int)_availableBytes;
                }

                _outWindow.SetLimit(toProcess);
                if (_uncompressedChunk)
                {
                    _inputPosition += _outWindow.CopyStream(_inputStream, toProcess);
                }
                else if (_decoder.Code(_dictionarySize, _outWindow, _rangeDecoder)
                         && _outputSize < 0)
                {
                    _availableBytes = _outWindow.AvailableBytes;
                }

                int read = _outWindow.Read(buffer, offset, toProcess);
                total += read;
                offset += read;
                _position += read;
                _availableBytes -= read;

                if (_availableBytes == 0 && !_uncompressedChunk)
                {
                    _rangeDecoder.ReleaseStream();
                    if (!_rangeDecoder.IsFinished || (_rangeDecoderLimit >= 0 && _rangeDecoder._total != _rangeDecoderLimit))
                    {
                        throw new DataErrorException();
                    }
                    _inputPosition += _rangeDecoder._total;
                    if (_outWindow.HasPending)
                    {
                        throw new DataErrorException();
                    }
                }
            }

            if (_endReached)
            {
                if (_inputSize >= 0 && _inputPosition != _inputSize)
                {
                    throw new DataErrorException();
                }
                if (_outputSize >= 0 && _position != _outputSize)
                {
                    throw new DataErrorException();
                }
            }

            return total;
        }

        private void DecodeChunkHeader()
        {
            int control = _inputStream.ReadByte();
            _inputPosition++;

            if (control == 0x00)
            {
                _endReached = true;
                return;
            }

            if (control >= 0xE0 || control == 0x01)
            {
                _needProps = true;
                _needDictReset = false;
                _outWindow.Reset();
            }
            else if (_needDictReset)
            {
                throw new DataErrorException();
            }

            if (control >= 0x80)
            {
                _uncompressedChunk = false;

                _availableBytes = (control & 0x1F) << 16;
                _availableBytes += (_inputStream.ReadByte() << 8) + _inputStream.ReadByte() + 1;
                _inputPosition += 2;

                _rangeDecoderLimit = (_inputStream.ReadByte() << 8) + _inputStream.ReadByte() + 1;
                _inputPosition += 2;

                if (control >= 0xC0)
                {
                    _needProps = false;
                    Properties[0] = (byte)_inputStream.ReadByte();
                    _inputPosition++;

                    _decoder = new Decoder();
                    _decoder.SetDecoderProperties(Properties);
                }
                else if (_needProps)
                {
                    throw new DataErrorException();
                }
                else if (control >= 0xA0)
                {
                    _decoder = new Decoder();
                    _decoder.SetDecoderProperties(Properties);
                }

                _rangeDecoder.Init(_inputStream);
            }
            else if (control > 0x02)
            {
                throw new DataErrorException();
            }
            else
            {
                _uncompressedChunk = true;
                _availableBytes = (_inputStream.ReadByte() << 8) + _inputStream.ReadByte() + 1;
                _inputPosition += 2;
            }
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
            if (_encoder != null)
            {
                _position = _encoder.Code(new MemoryStream(buffer, offset, count), false);
            }
        }

        public byte[] Properties { get; } = new byte[5];
    }
}