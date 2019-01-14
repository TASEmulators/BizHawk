using System;
using System.Collections.Generic;
using System.IO;
using SharpCompress.Compressors.LZMA;
using SharpCompress.Compressors.LZMA.Utilites;

namespace SharpCompress.Common.SevenZip
{
    internal class ArchiveDatabase
    {
        internal byte _majorVersion;
        internal byte _minorVersion;
        internal long _startPositionAfterHeader;
        internal long _dataStartPosition;

        internal List<long> _packSizes = new List<long>();
        internal List<uint?> _packCrCs = new List<uint?>();
        internal List<CFolder> _folders = new List<CFolder>();
        internal List<int> _numUnpackStreamsVector;
        internal List<CFileItem> _files = new List<CFileItem>();

        internal List<long> _packStreamStartPositions = new List<long>();
        internal List<int> _folderStartFileIndex = new List<int>();
        internal List<int> _fileIndexToFolderIndexMap = new List<int>();
        
        internal IPasswordProvider PasswordProvider { get; }

        public ArchiveDatabase(IPasswordProvider passwordProvider)
        {
            PasswordProvider = passwordProvider;
        }

        internal void Clear()
        {
            _packSizes.Clear();
            _packCrCs.Clear();
            _folders.Clear();
            _numUnpackStreamsVector = null;
            _files.Clear();

            _packStreamStartPositions.Clear();
            _folderStartFileIndex.Clear();
            _fileIndexToFolderIndexMap.Clear();
        }

        internal bool IsEmpty()
        {
            return _packSizes.Count == 0
                   && _packCrCs.Count == 0
                   && _folders.Count == 0
                   && _numUnpackStreamsVector.Count == 0
                   && _files.Count == 0;
        }

        private void FillStartPos()
        {
            _packStreamStartPositions.Clear();

            long startPos = 0;
            for (int i = 0; i < _packSizes.Count; i++)
            {
                _packStreamStartPositions.Add(startPos);
                startPos += _packSizes[i];
            }
        }

        private void FillFolderStartFileIndex()
        {
            _folderStartFileIndex.Clear();
            _fileIndexToFolderIndexMap.Clear();

            int folderIndex = 0;
            int indexInFolder = 0;
            for (int i = 0; i < _files.Count; i++)
            {
                CFileItem file = _files[i];

                bool emptyStream = !file.HasStream;

                if (emptyStream && indexInFolder == 0)
                {
                    _fileIndexToFolderIndexMap.Add(-1);
                    continue;
                }

                if (indexInFolder == 0)
                {
                    // v3.13 incorrectly worked with empty folders
                    // v4.07: Loop for skipping empty folders
                    for (;;)
                    {
                        if (folderIndex >= _folders.Count)
                        {
                            throw new InvalidOperationException();
                        }

                        _folderStartFileIndex.Add(i); // check it

                        if (_numUnpackStreamsVector[folderIndex] != 0)
                        {
                            break;
                        }

                        folderIndex++;
                    }
                }

                _fileIndexToFolderIndexMap.Add(folderIndex);

                if (emptyStream)
                {
                    continue;
                }

                indexInFolder++;

                if (indexInFolder >= _numUnpackStreamsVector[folderIndex])
                {
                    folderIndex++;
                    indexInFolder = 0;
                }
            }
        }

        public void Fill()
        {
            FillStartPos();
            FillFolderStartFileIndex();
        }

        internal long GetFolderStreamPos(CFolder folder, int indexInFolder)
        {
            int index = folder._firstPackStreamId + indexInFolder;
            return _dataStartPosition + _packStreamStartPositions[index];
        }

        internal long GetFolderFullPackSize(int folderIndex)
        {
            int packStreamIndex = _folders[folderIndex]._firstPackStreamId;
            CFolder folder = _folders[folderIndex];

            long size = 0;
            for (int i = 0; i < folder._packStreams.Count; i++)
            {
                size += _packSizes[packStreamIndex + i];
            }

            return size;
        }

        internal Stream GetFolderStream(Stream stream, CFolder folder, IPasswordProvider pw)
        {
            int packStreamIndex = folder._firstPackStreamId;
            long folderStartPackPos = GetFolderStreamPos(folder, 0);
            List<long> packSizes = new List<long>();
            for (int j = 0; j < folder._packStreams.Count; j++)
            {
                packSizes.Add(_packSizes[packStreamIndex + j]);
            }

            return DecoderStreamHelper.CreateDecoderStream(stream, folderStartPackPos, packSizes.ToArray(), folder, pw);
        }

        private long GetFolderPackStreamSize(int folderIndex, int streamIndex)
        {
            return _packSizes[_folders[folderIndex]._firstPackStreamId + streamIndex];
        }

        private long GetFilePackSize(int fileIndex)
        {
            int folderIndex = _fileIndexToFolderIndexMap[fileIndex];
            if (folderIndex != -1)
            {
                if (_folderStartFileIndex[folderIndex] == fileIndex)
                {
                    return GetFolderFullPackSize(folderIndex);
                }
            }
            return 0;
        }
    }
}