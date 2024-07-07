using System.IO;

namespace BizHawk.Client.Common
{
	public interface IZipWriter : IDisposable
	{
		void WriteItem(string name, Action<Stream> callback, bool zstdCompress);
	}
}
