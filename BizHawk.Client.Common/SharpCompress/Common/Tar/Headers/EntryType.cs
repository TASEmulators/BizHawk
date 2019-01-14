namespace SharpCompress.Common.Tar.Headers
{
    internal enum EntryType : byte
    {
        File = 0,
        OldFile = (byte)'0',
        HardLink = (byte)'1',
        SymLink = (byte)'2',
        CharDevice = (byte)'3',
        BlockDevice = (byte)'4',
        Directory = (byte)'5',
        Fifo = (byte)'6',
        LongLink = (byte)'K',
        LongName = (byte)'L',
        SparseFile = (byte)'S',
        VolumeHeader = (byte)'V',
        GlobalExtendedHeader = (byte)'g'
    }
}