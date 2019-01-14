#if !NO_FILE
using System.IO;
using SharpCompress.Common;
#endif

namespace SharpCompress.Readers
{
    public static class IReaderExtensions
    {
#if !NO_FILE
        public static void WriteEntryTo(this IReader reader, string filePath)
        {
            using (Stream stream = File.Open(filePath, FileMode.Create, FileAccess.Write))
            {
                reader.WriteEntryTo(stream);
            }
        }

        public static void WriteEntryTo(this IReader reader, FileInfo filePath)
        {
            using (Stream stream = filePath.Open(FileMode.Create))
            {
                reader.WriteEntryTo(stream);
            }
        }

        /// <summary>
        /// Extract all remaining unread entries to specific directory, retaining filename
        /// </summary>
        public static void WriteAllToDirectory(this IReader reader, string destinationDirectory,
                                               ExtractionOptions options = null)
        {
            while (reader.MoveToNextEntry())
            {
                reader.WriteEntryToDirectory(destinationDirectory, options);
            }
        }

        /// <summary>
        /// Extract to specific directory, retaining filename
        /// </summary>
        public static void WriteEntryToDirectory(this IReader reader, string destinationDirectory,
                                                 ExtractionOptions options = null)
        {
            ExtractionMethods.WriteEntryToDirectory(reader.Entry, destinationDirectory, options,
                                              reader.WriteEntryToFile);
        }

        /// <summary>
        /// Extract to specific file
        /// </summary>
        public static void WriteEntryToFile(this IReader reader, string destinationFileName,
                                            ExtractionOptions options = null)
        {
            ExtractionMethods.WriteEntryToFile(reader.Entry, destinationFileName, options,
                                               (x, fm) =>
                                               {
                                                   using (FileStream fs = File.Open(destinationFileName, fm))
                                                   {
                                                       reader.WriteEntryTo(fs);
                                                   }
                                               });
        }
#endif
    }
}