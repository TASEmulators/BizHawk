#nullable enable

using System.Diagnostics;
using System.IO;

using BizHawk.Common.StringExtensions;

namespace BizHawk.Client.Common
{
	public class FileWriter : IDisposable
	{
		private FileStream? _stream; // is never null until this.Dispose()
		public FileStream Stream
		{
			get => _stream ?? throw new ObjectDisposedException("Cannot access a disposed FileStream.");
		}
		public string FinalPath;
		public string TempPath;

		public bool UsingTempFile => TempPath != FinalPath;

		private bool _finished = false;

		private FileWriter(string final, string temp, FileStream stream)
		{
			FinalPath = final;
			TempPath = temp;
			_stream = stream;
		}

		// There is no public constructor. This is the only way to create an instance.
		public static FileWriteResult<FileWriter> Create(string path)
		{
			string writePath = path;
			try
			{
				// If the file already exists, we will write to a temporary location first and preserve the old one until we're done.
				if (File.Exists(path))
				{
					writePath = path.InsertBeforeLast('.', ".saving", out bool inserted);
					if (!inserted) writePath = $"{path}.saving";

					if (File.Exists(writePath))
					{
						// The user should probably have dealt with this on the previously failed save.
						// But maybe we should support plain old "try again", so let's delete it.
						File.Delete(writePath);
					}
				}
				FileStream fs = new(writePath, FileMode.Create, FileAccess.Write);
				return new(new FileWriter(path, writePath, fs), writePath);
			}
			catch (Exception ex) // There are many exception types that file operations might raise.
			{
				return new(FileWriteEnum.FailedToOpen, writePath, ex);
			}
		}

		/// <summary>
		/// This method must be called after writing has finished and must not be called twice.
		/// Dispose will be called regardless of the result.
		/// </summary>
		/// <exception cref="InvalidOperationException">If called twice.</exception>
		public FileWriteResult CloseAndDispose()
		{
			// In theory it might make sense to allow the user to try again if we fail inside this method.
			// If we implement that, it is probably best to make a static method that takes a FileWriteResult.
			// So even then, this method should not ever be called twice.
			if (_finished) throw new InvalidOperationException("Cannot close twice.");

			_finished = true;
			Dispose();

			if (!UsingTempFile) return new(FileWriteEnum.Success, FinalPath, null);

			if (File.Exists(FinalPath))
			{
				try
				{
					File.Delete(FinalPath);
				}
				catch (Exception ex)
				{
					return new(FileWriteEnum.FailedToDeleteOldFile, TempPath, ex);
				}
			}
			try
			{
				File.Move(TempPath, FinalPath);
			}
			catch (Exception ex)
			{
				return new(FileWriteEnum.FailedToRename, TempPath, ex);
			}

			return new(FileWriteEnum.Success, FinalPath, null);
		}

		/// <summary>
		/// Closes and deletes the file. Use if there was an error while writing.
		/// Do not call <see cref="CloseAndDispose"/> after this.
		/// </summary>
		public void Abort()
		{
			if (_dispoed) throw new ObjectDisposedException("Cannot use a disposed file stream.");
			_finished = true;
			Dispose();

			try
			{
				// Delete because the file is almost certainly useless and just clutter.
				File.Delete(TempPath);
			}
			catch { /* eat? this is probably not very important */ }
		}

		private bool _dispoed;
		public void Dispose()
		{
			if (_dispoed) return;
			_dispoed = true;

			_stream!.Flush(flushToDisk: true);
			_stream.Dispose();
			_stream = null;

			// The caller should call CloseAndDispose and handle potential failure.
			Debug.Assert(_finished, $"{nameof(FileWriteResult)} should not be disposed before calling {nameof(CloseAndDispose)}");
		}
	}
}
