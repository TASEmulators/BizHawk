#nullable enable

using System.IO;

namespace BizHawk.Client.Common
{
	public interface IZipWriter : IDisposable
	{
		void WriteItem(string name, Action<Stream> callback, bool zstdCompress);

		/// <summary>
		/// This method must be called after writing has finished and must not be called twice.
		/// Dispose will be called regardless of the result.
		/// </summary>
		/// <param name="backupPath">If not null, renames the original file to this path.</param>
		FileWriteResult CloseAndDispose(string? backupPath = null);

		/// <summary>
		/// Closes and deletes the file. Use if there was an error while writing.
		/// Do not call <see cref="CloseAndDispose"/> after this.
		/// </summary>
		void Abort();
	}
}
