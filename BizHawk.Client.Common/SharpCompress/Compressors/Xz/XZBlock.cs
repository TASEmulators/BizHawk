using System.Collections.Generic;
using System.IO;
using System.Linq;
using SharpCompress.Compressors.Xz.Filters;

namespace SharpCompress.Compressors.Xz
{
    internal sealed class XZBlock : XZReadOnlyStream
    {
        public int BlockHeaderSize => (_blockHeaderSizeByte + 1) * 4;
        public ulong? CompressedSize { get; private set; }
        public ulong? UncompressedSize { get; private set; }
        public Stack<BlockFilter> Filters { get; private set; } = new Stack<BlockFilter>();
        public bool HeaderIsLoaded { get; private set; }
        private CheckType _checkType;
        private readonly int _checkSize;
        private bool _streamConnected;
        private int _numFilters;
        private byte _blockHeaderSizeByte;
        private Stream _decomStream;
        private bool _endOfStream;
        private bool _paddingSkipped;
        private bool _crcChecked;
        private ulong _bytesRead;

        public XZBlock(Stream stream, CheckType checkType, int checkSize) : base(stream)
        {
            _checkType = checkType;
            _checkSize = checkSize;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = 0;
            if (!HeaderIsLoaded)
                LoadHeader();
            if (!_streamConnected)
                ConnectStream();
            if (!_endOfStream)
                bytesRead = _decomStream.Read(buffer, offset, count);
            if (bytesRead != count)
                _endOfStream = true;
            if (_endOfStream && !_paddingSkipped)
                SkipPadding();
            if (_endOfStream && !_crcChecked)
                CheckCrc();
            _bytesRead += (ulong)bytesRead;
            return bytesRead;
        }

        private void SkipPadding()
        {
            int bytes = (int)(BaseStream.Position % 4);
            if (bytes > 0)
            {
                byte[] paddingBytes = new byte[4 - bytes];
                BaseStream.Read(paddingBytes, 0, paddingBytes.Length);
                if (paddingBytes.Any(b => b != 0))
                    throw new InvalidDataException("Padding bytes were non-null");
            }
            _paddingSkipped = true;
        }

        private void CheckCrc()
        {
            byte[] crc = new byte[_checkSize];
            BaseStream.Read(crc, 0, _checkSize);
            // Actually do a check (and read in the bytes
            //   into the function throughout the stream read).
            _crcChecked = true;
        }

        private void ConnectStream()
        {
            _decomStream = BaseStream;
            while (Filters.Any())
            {
                var filter = Filters.Pop();
                filter.SetBaseStream(_decomStream);
                _decomStream = filter;
            }
            _streamConnected = true;
        }

        private void LoadHeader()
        {
            ReadHeaderSize();
            byte[] headerCache = CacheHeader();

            using (var cache = new MemoryStream(headerCache))
            using (var cachedReader = new BinaryReader(cache))
            {
                cachedReader.BaseStream.Position = 1; // skip the header size byte
                ReadBlockFlags(cachedReader);
                ReadFilters(cachedReader);
            }
            HeaderIsLoaded = true;
        }

        private void ReadHeaderSize()
        {
            _blockHeaderSizeByte = (byte)BaseStream.ReadByte();
            if (_blockHeaderSizeByte == 0)
                throw new XZIndexMarkerReachedException();
        }

        private byte[] CacheHeader()
        {
            byte[] blockHeaderWithoutCrc = new byte[BlockHeaderSize - 4];
            blockHeaderWithoutCrc[0] = _blockHeaderSizeByte;
            var read = BaseStream.Read(blockHeaderWithoutCrc, 1, BlockHeaderSize - 5);
            if (read != BlockHeaderSize - 5)
                throw new EndOfStreamException("Reached end of stream unexectedly");

            uint crc = BaseStream.ReadLittleEndianUInt32();
            uint calcCrc = Crc32.Compute(blockHeaderWithoutCrc);
            if (crc != calcCrc)
                throw new InvalidDataException("Block header corrupt");

            return blockHeaderWithoutCrc;
        }

        private void ReadBlockFlags(BinaryReader reader)
        {
            var blockFlags = reader.ReadByte();
            _numFilters = (blockFlags & 0x03) + 1;
            byte reserved = (byte)(blockFlags & 0x3C);

            if (reserved != 0)
                throw new InvalidDataException("Reserved bytes used, perhaps an unknown XZ implementation");

            bool compressedSizePresent = (blockFlags & 0x40) != 0;
            bool uncompressedSizePresent = (blockFlags & 0x80) != 0;

            if (compressedSizePresent)
                CompressedSize = reader.ReadXZInteger();
            if (uncompressedSizePresent)
                UncompressedSize = reader.ReadXZInteger();
        }

        private void ReadFilters(BinaryReader reader, long baseStreamOffset = 0)
        {
            int nonLastSizeChangers = 0;
            for (int i = 0; i < _numFilters; i++)
            {
                var filter = BlockFilter.Read(reader);
                if ((i + 1 == _numFilters && !filter.AllowAsLast)
                    || (i + 1 < _numFilters && !filter.AllowAsNonLast))
                    throw new InvalidDataException("Block Filters in bad order");
                if (filter.ChangesDataSize && i + 1 < _numFilters)
                    nonLastSizeChangers++;
                filter.ValidateFilter();
                Filters.Push(filter);
            }
            if (nonLastSizeChangers > 2)
                throw new InvalidDataException("More than two non-last block filters cannot change stream size");

            int blockHeaderPaddingSize = BlockHeaderSize -
                (4 + (int)(reader.BaseStream.Position - baseStreamOffset));
            byte[] blockHeaderPadding = reader.ReadBytes(blockHeaderPaddingSize);
            if (!blockHeaderPadding.All(b => b == 0))
                throw new InvalidDataException("Block header contains unknown fields");
        }
    }
}
