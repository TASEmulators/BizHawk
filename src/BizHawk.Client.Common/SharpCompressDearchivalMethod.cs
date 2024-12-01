#nullable enable

using System.Collections.Generic;
using System.IO;

using BizHawk.Common;
using BizHawk.Common.BufferExtensions;

using SharpCompress.Archives;
using SharpCompress.Common;

namespace BizHawk.Client.Common
{
	/// <summary>A <see cref="IFileDearchivalMethod{T}">dearchival method</see> for <see cref="HawkFile"/> implemented using <c>SharpCompress</c> from NuGet.</summary>
	public class SharpCompressDearchivalMethod : IFileDearchivalMethod<SharpCompressArchiveFile>
	{
		private SharpCompressDearchivalMethod() {}

		public bool CheckSignature(string fileName, out int offset, out bool isExecutable)
		{
			offset = 0;
			isExecutable = false;

			bool isArchive = ArchiveFactory.IsArchive(fileName, out var type);
			if (!isArchive) return false;
			if (type is not ArchiveType.Tar) return true; // not expecting false positives from anything but .tar for now

			// SharpCompress seems to overzealously flag files it thinks are the in original .tar format, so we'll check for false positives. This affects 0.24.0, and the latest at time of writing, 0.27.1.
			// https://github.com/adamhathcock/sharpcompress/issues/390

			using FileStream fs = new(fileName, FileMode.Open, FileAccess.Read); // initialising and using a FileStream can throw all sorts of exceptions, but I think if we've gotten to this point and the file isn't readable, it makes sense to throw --yoshi
			if (!fs.CanRead || !fs.CanSeek || fs.Length < 512) return false;

			// looking for magic bytes
			fs.Seek(0x101, SeekOrigin.Begin);
			var buffer = new byte[8];
			_ = fs.Read(buffer, offset: 0, count: buffer.Length); // if stream is too short, the next check will catch it
			var s = buffer.BytesToHexString();
			if (s == "7573746172003030" || s == "7573746172202000") return true; // "ustar\000" (libarchive's bsdtar) or "ustar  \0" (GNU Tar)

			Console.WriteLine($"SharpCompress identified file as original .tar format, probably a false positive, ignoring. Filename: {fileName}");
			return false;
		}

		public bool CheckSignature(Stream fileStream, string? filenameHint)
		{
			if (!fileStream.CanRead || !fileStream.CanSeek) return false;
			long initialPosition = fileStream.Position;

			bool isArchive = ArchiveFactory.IsArchive(fileStream, out var type);
			fileStream.Seek(initialPosition, SeekOrigin.Begin);
			if (!isArchive) return false;
			if (type is not ArchiveType.Tar) return true; // not expecting false positives from anything but .tar for now

			// as above, SharpCompress seems to overzealously flag files it thinks are the in original .tar format, so we'll check for false positives

			if (fileStream.Length < 512) return false;
			// looking for magic bytes
			fileStream.Seek(0x101, SeekOrigin.Begin);
			var buffer = new byte[8];
			_ = fileStream.Read(buffer, offset: 0, count: buffer.Length); // if stream is too short, the next check will catch it
			fileStream.Seek(initialPosition, SeekOrigin.Begin);
			var s = buffer.BytesToHexString();
			if (s == "7573746172003030" || s == "7573746172202000") return true; // "ustar\000" (libarchive's bsdtar) or "ustar  \0" (GNU Tar)

			Console.WriteLine($"SharpCompress identified file in stream as original .tar format, probably a false positive, ignoring. Filename hint: {filenameHint}");
			return false;
		}

		public SharpCompressArchiveFile Construct(string path) => new(path);

		public SharpCompressArchiveFile Construct(Stream fileStream) => new(fileStream);

		public static readonly SharpCompressDearchivalMethod Instance = new();

		public IReadOnlyCollection<string> AllowedArchiveExtensions { get; } = new[]
		{
			".7z",
			".gz",
			".rar",
			".tar",
//			/*.tar*/".bz2", ".tb2", ".tbz", ".tbz2", ".tz2",
//			/*.tar.gz,*/ ".taz", ".tgz",
//			/*.tar*/".lz",
			".zip",
		};
	}
}
