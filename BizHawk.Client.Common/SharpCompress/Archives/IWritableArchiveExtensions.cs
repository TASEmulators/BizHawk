#if !NO_FILE
using System;
#endif
using System.IO;
using SharpCompress.Writers;

namespace SharpCompress.Archives
{
    public static class IWritableArchiveExtensions
    {
#if !NO_FILE

        public static void AddEntry(this IWritableArchive writableArchive,
                                                     string entryPath, string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException("Could not AddEntry: " + filePath);
            }
            writableArchive.AddEntry(entryPath, new FileInfo(filePath).OpenRead(), true, fileInfo.Length,
                                     fileInfo.LastWriteTime);
        }

        public static void SaveTo(this IWritableArchive writableArchive, string filePath, WriterOptions options)
        {
            writableArchive.SaveTo(new FileInfo(filePath), options);
        }

        public static void SaveTo(this IWritableArchive writableArchive, FileInfo fileInfo, WriterOptions options)
        {
            using (var stream = fileInfo.Open(FileMode.Create, FileAccess.Write))
            {
                writableArchive.SaveTo(stream, options);
            }
        }

        public static void AddAllFromDirectory(
            this IWritableArchive writableArchive,
            string filePath, string searchPattern = "*.*", SearchOption searchOption = SearchOption.AllDirectories)
        {
#if NET35
            foreach (var path in Directory.GetFiles(filePath, searchPattern, searchOption))
#else
            foreach (var path in Directory.EnumerateFiles(filePath, searchPattern, searchOption))
#endif
            {
                var fileInfo = new FileInfo(path);
                writableArchive.AddEntry(path.Substring(filePath.Length), fileInfo.OpenRead(), true, fileInfo.Length,
                                         fileInfo.LastWriteTime);
            }
        }
        public static IArchiveEntry AddEntry(this IWritableArchive writableArchive, string key, FileInfo fileInfo)
        {
            if (!fileInfo.Exists)
            {
                throw new ArgumentException("FileInfo does not exist.");
            }
            return writableArchive.AddEntry(key, fileInfo.OpenRead(), true, fileInfo.Length, fileInfo.LastWriteTime);
        }
#endif
    }
}