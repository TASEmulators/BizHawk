using System;
using System.Collections.Generic;
using SharpCompress.Compressors.LZMA;

namespace SharpCompress.Common.SevenZip
{
    internal struct CStreamSwitch : IDisposable
    {
        private ArchiveReader _archive;
        private bool _needRemove;
        private bool _active;

        public void Dispose()
        {
            if (_active)
            {
                _active = false;
#if DEBUG
                Log.WriteLine("[end of switch]");
#endif
            }

            if (_needRemove)
            {
                _needRemove = false;
                _archive.DeleteByteStream();
            }
        }

        public void Set(ArchiveReader archive, byte[] dataVector)
        {
            Dispose();
            _archive = archive;
            _archive.AddByteStream(dataVector, 0, dataVector.Length);
            _needRemove = true;
            _active = true;
        }

        public void Set(ArchiveReader archive, List<byte[]> dataVector)
        {
            Dispose();
            _active = true;

            byte external = archive.ReadByte();
            if (external != 0)
            {
                int dataIndex = archive.ReadNum();
                if (dataIndex < 0 || dataIndex >= dataVector.Count)
                {
                    throw new InvalidOperationException();
                }

#if DEBUG
                Log.WriteLine("[switch to stream {0}]", dataIndex);
#endif
                _archive = archive;
                _archive.AddByteStream(dataVector[dataIndex], 0, dataVector[dataIndex].Length);
                _needRemove = true;
                _active = true;
            }
            else
            {
#if DEBUG
                Log.WriteLine("[inline data]");
#endif
            }
        }
    }
}