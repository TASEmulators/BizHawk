namespace SharpCompress.Common.Rar.Headers
{
    internal enum HeaderType : byte
    {
        Null,
        Mark,
        Archive,
        File,
        Service,
        Comment,
        Av,
        Protect,
        Sign,
        NewSub,
        EndArchive,
        Crypt
    }

    internal static class HeaderCodeV
    {
        public const byte RAR4_MARK_HEADER = 0x72;
        public const byte RAR4_ARCHIVE_HEADER = 0x73;
        public const byte RAR4_FILE_HEADER = 0x74;
        public const byte RAR4_COMMENT_HEADER = 0x75;
        public const byte RAR4_AV_HEADER = 0x76;
        public const byte RAR4_SUB_HEADER = 0x77;
        public const byte RAR4_PROTECT_HEADER = 0x78;
        public const byte RAR4_SIGN_HEADER = 0x79;
        public const byte RAR4_NEW_SUB_HEADER = 0x7a;
        public const byte RAR4_END_ARCHIVE_HEADER = 0x7b;

        public const byte RAR5_ARCHIVE_HEADER = 0x01;
        public const byte RAR5_FILE_HEADER = 0x02;
        public const byte RAR5_SERVICE_HEADER = 0x03;
        public const byte RAR5_ARCHIVE_ENCRYPTION_HEADER = 0x04;
        public const byte RAR5_END_ARCHIVE_HEADER = 0x05;
    }

    internal static class HeaderFlagsV4
    {
        public const ushort HAS_DATA = 0x8000;
    }

    internal static class EncryptionFlagsV5
    {   
        // RAR 5.0 archive encryption header specific flags.
        public const uint CHFL_CRYPT_PSWCHECK = 0x01; // Password check data is present.
        
        public const uint FHEXTRA_CRYPT_PSWCHECK = 0x01; // Password check data is present.
        public const uint FHEXTRA_CRYPT_HASHMAC = 0x02;
    }

    internal static class HeaderFlagsV5
    {
        public const ushort HAS_EXTRA = 0x0001;
        public const ushort HAS_DATA = 0x0002;
        public const ushort KEEP = 0x0004;  // block must be kept during an update
        public const ushort SPLIT_BEFORE = 0x0008;
        public const ushort SPLIT_AFTER = 0x0010;
        public const ushort CHILD = 0x0020; // ??? Block depends on preceding file block.
        public const ushort PRESERVE_CHILD = 0x0040; // ???? Preserve a child block if host block is modified
    }

    internal static class ArchiveFlagsV4
    {
        public const ushort VOLUME = 0x0001;
        public const ushort COMMENT = 0x0002;
        public const ushort LOCK = 0x0004;
        public const ushort SOLID = 0x0008;
        public const ushort NEW_NUMBERING = 0x0010;
        public const ushort AV = 0x0020;
        public const ushort PROTECT = 0x0040;
        public const ushort PASSWORD = 0x0080;
        public const ushort FIRST_VOLUME = 0x0100;
        public const ushort ENCRYPT_VER = 0x0200;
    }

    internal static class ArchiveFlagsV5
    {
        public const ushort VOLUME = 0x0001;
        public const ushort HAS_VOLUME_NUMBER = 0x0002;
        public const ushort SOLID = 0x0004;
        public const ushort PROTECT = 0x0008;
        public const ushort LOCK = 0x0010;
    }

    internal static class HostOsV4
    {
        public const byte MS_DOS = 0;
        public const byte OS2 = 1;
        public const byte WIN32 = 2;
        public const byte UNIX = 3;
        public const byte MAC_OS = 4;
        public const byte BE_OS = 5;
    }

    internal static class HostOsV5
    {
        public const byte WINDOWS = 0;
        public const byte UNIX = 1;
    }

    internal static class FileFlagsV4
    {
        public const ushort SPLIT_BEFORE = 0x0001;
        public const ushort SPLIT_AFTER = 0x0002;
        public const ushort PASSWORD = 0x0004;
        public const ushort COMMENT = 0x0008;
        public const ushort SOLID = 0x0010;

        public const ushort WINDOW_MASK = 0x00e0;
        public const ushort WINDOW64 = 0x0000;
        public const ushort WINDOW128 = 0x0020;
        public const ushort WINDOW256 = 0x0040;
        public const ushort WINDOW512 = 0x0060;
        public const ushort WINDOW1024 = 0x0080;
        public const ushort WINDOW2048 = 0x00a0;
        public const ushort WINDOW4096 = 0x00c0;
        public const ushort DIRECTORY = 0x00e0;

        public const ushort LARGE = 0x0100;
        public const ushort UNICODE = 0x0200;
        public const ushort SALT = 0x0400;
        public const ushort VERSION = 0x0800;
        public const ushort EXT_TIME = 0x1000;
        public const ushort EXT_FLAGS = 0x2000;
    }

    internal static class FileFlagsV5
    {
        public const ushort DIRECTORY = 0x0001;
        public const ushort HAS_MOD_TIME = 0x0002;
        public const ushort HAS_CRC32 = 0x0004;
        public const ushort UNPACKED_SIZE_UNKNOWN = 0x0008;
    }

    internal static class EndArchiveFlagsV4
    {
        public const ushort NEXT_VOLUME = 0x0001;
        public const ushort DATA_CRC = 0x0002;
        public const ushort REV_SPACE = 0x0004;
        public const ushort VOLUME_NUMBER = 0x0008;
    }

    internal static class EndArchiveFlagsV5
    {
        public const ushort HAS_NEXT_VOLUME = 0x0001;
    }
}