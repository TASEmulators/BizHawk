using System.Collections.Generic;
using System.IO;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	/// <summary>A <see cref="IFileDearchivalMethod{T}">dearchival method</see> for <see cref="HawkFile"/> implemented using <c>libarchive</c> (via <c>LibArchive.Net</c> bindings).</summary>
	public class LibarchiveDearchivalMethod : IFileDearchivalMethod<LibarchiveArchiveFile>
	{
		public static readonly LibarchiveDearchivalMethod Instance = new();

		public IReadOnlyCollection<string> AllowedArchiveExtensions { get; } = [
			".7z",
			".gz",
			".rar",
			".tar",
			/*.tar*/".bz2", ".tb2", ".tbz", ".tbz2", ".tz2",
			/*.tar.gz,*/ ".taz", ".tgz",
			/*.tar*/".lz",
			".zip",
		];

		private LibarchiveDearchivalMethod() {}

		public bool CheckSignature(string fileName)
		{
			LibarchiveArchiveFile? file = null;
			try
			{
				file = new(fileName);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
			finally
			{
				file?.Dispose();
			}
		}

		public bool CheckSignature(Stream fileStream, string? filenameHint)
		{
			if (!fileStream.CanRead || !fileStream.CanSeek) return false;
			var initialPosition = fileStream.Position;
			LibarchiveArchiveFile? file = null;
			try
			{
				file = new(fileStream);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
			finally
			{
				file?.Dispose();
				fileStream.Seek(initialPosition, SeekOrigin.Begin);
			}
		}

		public LibarchiveArchiveFile Construct(string path)
			=> new(path);

		public LibarchiveArchiveFile Construct(Stream fileStream)
			=> new(fileStream);
	}
}
