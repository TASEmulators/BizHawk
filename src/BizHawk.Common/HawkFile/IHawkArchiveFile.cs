using System;
using System.Collections.Generic;
using System.IO;

namespace BizHawk.Common
{
	/// <seealso cref="IFileDearchivalMethod"/>
	public interface IHawkArchiveFile : IDisposable
	{
		void ExtractFile(int index, Stream stream);

		List<HawkArchiveFileItem> Scan();
	}
}
