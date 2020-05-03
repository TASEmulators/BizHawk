#nullable enable

using System.IO;
using System.Linq;

using BizHawk.Common;

using SharpCompress.Archives;
using SharpCompress.Common;

namespace BizHawk.Client.Common
{
	/// <summary>A <see cref="IFileDearchivalMethod">dearchival method</see> for <see cref="HawkFile"/> implemented using <c>SharpCompress</c> from NuGet.</summary>
	public class SharpCompressDearchivalMethod : IFileDearchivalMethod<SharpCompressArchiveFile>
	{
		private SharpCompressDearchivalMethod() {}

		public bool CheckSignature(string fileName, out int offset, out bool isExecutable)
		{
			offset = 0;
			isExecutable = false;

			if (!ArchiveExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant())) return false;

			try
			{
				using var arcTest = ArchiveFactory.Open(fileName);
				switch (arcTest.Type)
				{
					case ArchiveType.Zip:
					case ArchiveType.SevenZip:
						return true;
				}
			}
			catch
			{
				// ignored
			}
			return false;
		}

		public SharpCompressArchiveFile Construct(string path) => new SharpCompressArchiveFile(path);

		/// <remarks>whitelist as to avoid exceptions</remarks>
		private static readonly string[] ArchiveExtensions = { ".zip", ".gz", ".gzip", ".tar", ".rar", ".7z" };

		public static readonly SharpCompressDearchivalMethod Instance = new SharpCompressDearchivalMethod();
	}
}