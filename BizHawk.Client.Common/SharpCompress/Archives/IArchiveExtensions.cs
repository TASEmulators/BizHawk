#if !NO_FILE
using System.Linq;
using SharpCompress.Common;

#endif

namespace SharpCompress.Archives
{
    public static class IArchiveExtensions
    {
#if !NO_FILE

/// <summary>
/// Extract to specific directory, retaining filename
/// </summary>
        public static void WriteToDirectory(this IArchive archive, string destinationDirectory,
                                            ExtractionOptions options = null)
        {
            foreach (IArchiveEntry entry in archive.Entries.Where(x => !x.IsDirectory))
            {
                entry.WriteToDirectory(destinationDirectory, options);
            }
        }
#endif
    }
}