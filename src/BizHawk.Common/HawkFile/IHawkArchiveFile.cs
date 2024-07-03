using System.Collections.Generic;
using System.IO;

namespace BizHawk.Common
{
	/// <seealso cref="IFileDearchivalMethod{T}"/>
	public interface IHawkArchiveFile : IDisposable
	{
		void ExtractFile(int index, Stream stream);

		/// <returns><see langword="null"/> on failure</returns>
		List<HawkArchiveFileItem>? Scan();
	}
}
