using System;
using System.IO;
using System.Linq;
using SharpCompress.IO;

namespace SharpCompress.Common.SevenZip
{
    internal class SevenZipFilePart : FilePart
    {
        private CompressionType? _type;
        private readonly Stream _stream;
        private readonly ArchiveDatabase _database;

        internal SevenZipFilePart(Stream stream, ArchiveDatabase database, int index, CFileItem fileEntry, ArchiveEncoding archiveEncoding)
           : base(archiveEncoding)
        {
            _stream = stream;
            _database = database;
            Index = index;
            Header = fileEntry;
            if (Header.HasStream)
            {
                Folder = database._folders[database._fileIndexToFolderIndexMap[index]];
            }
        }

        internal CFileItem Header { get; }
        internal CFolder Folder { get; }
        internal int Index { get; }

        internal override string FilePartName => Header.Name;

        internal override Stream GetRawStream()
        {
            return null;
        }

        internal override Stream GetCompressedStream()
        {
            if (!Header.HasStream)
            {
                return null;
            }
            var folderStream = _database.GetFolderStream(_stream, Folder, _database.PasswordProvider);

            int firstFileIndex = _database._folderStartFileIndex[_database._folders.IndexOf(Folder)];
            int skipCount = Index - firstFileIndex;
            long skipSize = 0;
            for (int i = 0; i < skipCount; i++)
            {
                skipSize += _database._files[firstFileIndex + i].Size;
            }
            if (skipSize > 0)
            {
                folderStream.Skip(skipSize);
            }
            return new ReadOnlySubStream(folderStream, Header.Size);
        }

        public CompressionType CompressionType
        {
            get
            {
                if (_type == null)
                {
                    _type = GetCompression();
                }
                return _type.Value;
            }
        }

        //copied from DecoderRegistry
        private const uint K_COPY = 0x0;
        private const uint K_DELTA = 3;
        private const uint K_LZMA2 = 0x21;
        private const uint K_LZMA = 0x030101;
        private const uint K_PPMD = 0x030401;
        private const uint K_BCJ = 0x03030103;
        private const uint K_BCJ2 = 0x0303011B;
        private const uint K_DEFLATE = 0x040108;
        private const uint K_B_ZIP2 = 0x040202;

        internal CompressionType GetCompression()
        {
            var coder = Folder._coders.First();
            switch (coder._methodId._id)
            {                
                case K_LZMA:
                case K_LZMA2:
                {
                    return CompressionType.LZMA;
                }
                case K_PPMD:
                {
                    return CompressionType.PPMd;
                }
                case K_B_ZIP2:
                {
                    return CompressionType.BZip2;
                }
                default:
                    throw new NotImplementedException();
            }
        }
    }
}