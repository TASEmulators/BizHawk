using System;

namespace SharpCompress.Common.SevenZip
{
    internal class CFileItem
    {
        public long Size { get; internal set; }
        public uint? Attrib { get; internal set; }
        public uint? Crc { get; internal set; }
        public string Name { get; internal set; }

        public bool HasStream { get; internal set; }
        public bool IsDir { get; internal set; }

        public bool CrcDefined => Crc != null;

        public bool AttribDefined => Attrib != null;

        public void SetAttrib(uint attrib)
        {
            Attrib = attrib;
        }

        public DateTime? CTime { get; internal set; }
        public DateTime? ATime { get; internal set; }
        public DateTime? MTime { get; internal set; }

        public long? StartPos { get; internal set; }
        public bool IsAnti { get; internal set; }

        internal CFileItem()
        {
            HasStream = true;
        }
    }
}