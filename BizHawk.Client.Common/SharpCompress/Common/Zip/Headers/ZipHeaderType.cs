namespace SharpCompress.Common.Zip.Headers
{
    internal enum ZipHeaderType
    {
        Ignore,
        LocalEntry,
        DirectoryEntry,
        DirectoryEnd,
        Split,
        Zip64DirectoryEnd,
        Zip64DirectoryEndLocator
    }
}