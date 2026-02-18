#nullable enable

using System.Diagnostics;
using System.IO;
using System.Threading;

using BizHawk.Common.StringExtensions;

namespace BizHawk.Client.Common
{
	public class FileWritePaths(string final, string temp)
	{
		public readonly string Final = final;
		public readonly string Temp = temp;
		public string? Backup;
	}

	/// <summary>
	/// Provides a mechanism for safely overwriting files, by using a temporary file that only replaces the original after writing has been completed.
	/// Optionally makes a backup of the original file.
	/// </summary>
	public class FileWriter : IDisposable
	{

		private FileStream? _stream; // is never null until this.Dispose()
		public FileStream Stream
		{
			get => _stream ?? throw new ObjectDisposedException("Cannot access a disposed FileStream.");
		}
		public FileWritePaths Paths;

		public bool UsingTempFile => Paths.Temp != Paths.Final;

		private bool _finished = false;

		private FileWriter(FileWritePaths paths, FileStream stream)
		{
			Paths = paths;
			_stream = stream;
		}

		public static FileWriteResult Write(string path, byte[] bytes, string? backupPath = null)
		{
			FileWriteResult<FileWriter> createResult = Create(path);
			if (createResult.IsError) return createResult;

			try
			{
				createResult.Value!.Stream.Write(bytes);
			}
			catch (Exception ex)
			{
				return new(FileWriteEnum.FailedDuringWrite, createResult.Value!.Paths, ex);
			}

			return createResult.Value.CloseAndDispose(backupPath);
		}

		public static FileWriteResult Write(string path, Action<Stream> writeCallback, string? backupPath = null)
		{
			FileWriteResult<FileWriter> createResult = Create(path);
			if (createResult.IsError) return createResult;

			try
			{
				writeCallback(createResult.Value!.Stream);
			}
			catch (Exception ex)
			{
				return new(FileWriteEnum.FailedDuringWrite, createResult.Value!.Paths, ex);
			}

			return createResult.Value.CloseAndDispose(backupPath);
		}

		/// <summary>
		/// Create a FileWriter instance, or return an error if unable to access the file.
		/// </summary>
		public static FileWriteResult<FileWriter> Create(string path)
		{
			string writePath = path;
			// If the file already exists, we will write to a temporary location first and preserve the old one until we're done.
			if (File.Exists(path))
			{
				writePath = path.InsertBeforeLast('.', ".saving", out bool inserted);
				if (!inserted) writePath = $"{path}.saving";

				// The file might already exist, if a prior file write failed.
				// Maybe the user should have dealt with this on the previously failed save.
				// But we want to support plain old "try again", so let's ignore that.
			}
			FileWritePaths paths = new(path, writePath);
			try
			{
				var parentDir = Path.GetDirectoryName(path);
				if (!string.IsNullOrWhiteSpace(parentDir)) Directory.CreateDirectory(parentDir);
				FileStream fs = new(writePath, FileMode.Create, FileAccess.Write);
				return new(new FileWriter(paths, fs), paths);
			}
			catch (Exception ex) // There are many exception types that file operations might raise.
			{
				return new(FileWriteEnum.FailedToOpen, paths, ex);
			}
		}

		/// <summary>
		/// This method must be called after writing has finished and must not be called twice.
		/// Dispose will be called regardless of the result.
		/// </summary>
		/// <param name="backupPath">If not null, renames the original file to this path.</param>
		/// <exception cref="InvalidOperationException">If called twice.</exception>
		public FileWriteResult CloseAndDispose(string? backupPath = null)
		{
			// In theory it might make sense to allow the user to try again if we fail inside this method.
			// If we implement that, it is probably best to make a static method that takes a FileWriteResult.
			// So even then, this method should not ever be called twice.
			if (_finished) throw new InvalidOperationException("Cannot close twice.");

			_finished = true;
			Dispose();

			Paths.Backup = backupPath;
			if (!UsingTempFile)
			{
				// The chosen file did not already exist, so there is nothing to back up and nothing to rename.
				return new(FileWriteEnum.Success, Paths, null);
			}

			try
			{
				// When everything goes right, this is all we need.
				File.Replace(Paths.Temp, Paths.Final, backupPath);
				return new(FileWriteEnum.Success, Paths, null);
			}
			catch
			{
				// When things go wrong, we have to do a lot of work in order to
				// figure out what went wrong and tell the user.
				return FindTheError();
			}
		}

		private FileWriteResult FindTheError()
		{
			// It is an unfortunate reality that .NET provides horrible exception messages
			// when using File.Replace(source, destination, backup). They are not only
			// unhelpful by not telling which file operation failed, but can also be a lie.
			// File.Move isn't great either.
			// So, we will split this into multiple parts and subparts.

			// 1) Handle backup file, if necessary
			//    a) Delete the old backup, if it exists. We check existence here to avoid DirectoryNotFound errors.
			//       If this fails, return that failure.
			//       If it succeeded but the file somehow still exists, report that error.
			//    b) Ensure the target directory exists.
			//       Rename the original file, and similarly report any errors.
			// 2) Handle renaming of temp file, the same way renaming of original for backup was done.

			if (Paths.Backup != null)
			{
				try { DeleteIfExists(Paths.Backup); }
				catch (Exception ex) { return new(FileWriteEnum.FailedToDeleteOldBackup, Paths, ex); }
				if (!TryWaitForFileToVanish(Paths.Backup)) return new(FileWriteEnum.FailedToDeleteOldBackup, Paths, new Exception("The file was supposedly deleted but is still there."));

				try { MoveFile(Paths.Final, Paths.Backup); }
				catch (Exception ex) { return new(FileWriteEnum.FailedToMakeBackup, Paths, ex); }
				if (!TryWaitForFileToVanish(Paths.Final)) return new(FileWriteEnum.FailedToMakeBackup, Paths, new Exception("The file was supposedly moved but is still in the orignal location."));
			}

			try { DeleteIfExists(Paths.Final); }
			catch (Exception ex) { return new(FileWriteEnum.FailedToDeleteOldFile, Paths, ex); }
			if (!TryWaitForFileToVanish(Paths.Final)) return new(FileWriteEnum.FailedToDeleteOldFile, Paths, new Exception("The file was supposedly deleted but is still there."));

			try { MoveFile(Paths.Temp, Paths.Final); }
			catch (Exception ex) { return new(FileWriteEnum.FailedToRename, Paths, ex); }
			if (!TryWaitForFileToVanish(Paths.Temp)) return new(FileWriteEnum.FailedToRename, Paths, new Exception("The file was supposedly moved but is still in the orignal location."));

			return new(FileWriteEnum.Success, Paths, null);
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
				File.Delete(Paths.Temp);
			}
			catch { /* eat? this is probably not very important */ }
		}

		private bool _dispoed;
		public void Dispose()
		{
			if (_dispoed) return;
			_dispoed = true;

			_stream!.Dispose();
			_stream = null;

			// The caller should call CloseAndDispose and handle potential failure.
			Debug.Assert(_finished, $"{nameof(FileWriteResult)} should not be disposed before calling {nameof(CloseAndDispose)}");
		}


		private static void DeleteIfExists(string path)
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}

		private static void MoveFile(string source, string destination)
		{
			FileInfo file = new(destination);
			file.Directory.Create();
			File.Move(source, destination);
		}

		/// <summary>
		/// Supposedly it is possible for File.Delete to return before the file has actually been deleted.
		/// And File.Move too, I guess.
		/// </summary>
		private static bool TryWaitForFileToVanish(string path)
		{
			for (var i = 25; i != 0; i--)
			{
				if (!File.Exists(path)) return true;
				Thread.Sleep(10);
			}
			return false;
		}
	}
}
