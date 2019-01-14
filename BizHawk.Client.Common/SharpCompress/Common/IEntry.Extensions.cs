
#if !NO_FILE
using System.IO;
using SharpCompress.Readers;

namespace SharpCompress.Common
{
    internal static class EntryExtensions
    {
        internal static void PreserveExtractionOptions(this IEntry entry, string destinationFileName,
                                                        ExtractionOptions options)
        {
            if (options.PreserveFileTime || options.PreserveAttributes)
            {
                FileInfo nf = new FileInfo(destinationFileName);
                if (!nf.Exists)
                {
                    return;
                }

                // update file time to original packed time
                if (options.PreserveFileTime)
                {
                    if (entry.CreatedTime.HasValue)
                    {
                        nf.CreationTime = entry.CreatedTime.Value;
                    }

                    if (entry.LastModifiedTime.HasValue)
                    {
                        nf.LastWriteTime = entry.LastModifiedTime.Value;
                    }

                    if (entry.LastAccessedTime.HasValue)
                    {
                        nf.LastAccessTime = entry.LastAccessedTime.Value;
                    }
                }

                if (options.PreserveAttributes)
                {
                    if (entry.Attrib.HasValue)
                    {
                        nf.Attributes = (FileAttributes)System.Enum.ToObject(typeof(FileAttributes), entry.Attrib.Value);
                    }
                }
            }
        }
    }
}
#endif