#if !NO_FILE
using System;
#endif
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace SharpCompress.Writers
{
    public static class IWriterExtensions
    {
        public static void Write(this IWriter writer, string entryPath, Stream source)
        {
            writer.Write(entryPath, source, null);
        }

#if !NO_FILE
        public static void Write(this IWriter writer, string entryPath, FileInfo source)
        {
            if (!source.Exists)
            {
                throw new ArgumentException("Source does not exist: " + source.FullName);
            }
            using (var stream = source.OpenRead())
            {
                writer.Write(entryPath, stream, source.LastWriteTime);
            }
        }

        public static void Write(this IWriter writer, string entryPath, string source)
        {
            writer.Write(entryPath, new FileInfo(source));
        }

        public static void WriteAll(this IWriter writer, string directory, string searchPattern = "*", SearchOption option = SearchOption.TopDirectoryOnly)
        {
            writer.WriteAll(directory, searchPattern, null, option);
        }

        public static void WriteAll(this IWriter writer, string directory, string searchPattern = "*", Expression<Func<string, bool>> fileSearchFunc = null,
                                    SearchOption option = SearchOption.TopDirectoryOnly)
        {
            if (!Directory.Exists(directory))
            {
                throw new ArgumentException("Directory does not exist: " + directory);
            }

            if (fileSearchFunc == null)
            {
                fileSearchFunc = n => true;
            }
#if NET35
            foreach (var file in Directory.GetDirectories(directory, searchPattern, option).Where(fileSearchFunc.Compile()))
#else
            foreach (var file in Directory.EnumerateFiles(directory, searchPattern, option).Where(fileSearchFunc.Compile()))
#endif
            {
                writer.Write(file.Substring(directory.Length), file);
            }
        }

#endif
    }
}