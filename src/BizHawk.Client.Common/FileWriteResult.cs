#nullable enable

using System.Diagnostics;

namespace BizHawk.Client.Common
{
	public enum FileWriteEnum
	{
		Success,
		FailedToOpen,
		FailedDuringWrite,
		FailedToDeleteOldBackup,
		FailedToMakeBackup,
		FailedToDeleteOldFile,
		FailedToRename,
	}

	/// <summary>
	/// Provides information about the success or failure of an attempt to write to a file.
	/// </summary>
	public class FileWriteResult
	{
		public readonly FileWriteEnum Error = FileWriteEnum.Success;
		public readonly Exception? Exception;
		internal readonly FileWritePaths Paths;

		public bool IsError => Error != FileWriteEnum.Success;

		internal FileWriteResult(FileWriteEnum error, FileWritePaths writer, Exception? exception)
		{
			Error = error;
			Exception = exception;
			Paths = writer;
		}

		public FileWriteResult() : this(FileWriteEnum.Success, new("", ""), null) { }

		/// <summary>
		/// Converts this instance to a different generic type.
		/// The new instance will take the value given only if this instance has no error.
		/// </summary>
		/// <param name="value">The value of the new instance. Ignored if this instance has an error.</param>
		public FileWriteResult<T> Convert<T>(T value) where T : class
		{
			if (Error == FileWriteEnum.Success) return new(value, Paths);
			else return new(this);
		}

		public FileWriteResult(FileWriteResult other) : this(other.Error, other.Paths, other.Exception) { }

		public string UserFriendlyErrorMessage()
		{
			Debug.Assert(!IsError || (Exception != null), "FileWriteResult with an error should have an exception.");

			switch (Error)
			{
				// We include the full path since the user may not have explicitly given a directory and may not know what it is.
				case FileWriteEnum.Success:
					return $"The file \"{Paths.Final}\" was written successfully.";
				case FileWriteEnum.FailedToOpen:
					if (Paths.Final != Paths.Temp)
					{
						return $"The temporary file \"{Paths.Temp}\" could not be opened.";
					}
					return $"The file \"{Paths.Final}\" could not be created.";
				case FileWriteEnum.FailedDuringWrite:
					return $"An error occurred while writing the file."; // No file name here; it should be deleted.
			}

			string success = $"The file was created successfully at \"{Paths.Temp}\" but could not be moved";
			switch (Error)
			{
				case FileWriteEnum.FailedToDeleteOldBackup:
					return $"{success}. Unable to remove old backup file \"{Paths.Backup}\".";
				case FileWriteEnum.FailedToMakeBackup:
					return $"{success}. Unable to create backup. Failed to move \"{Paths.Final}\" to \"{Paths.Backup}\".";
				case FileWriteEnum.FailedToDeleteOldFile:
					return $"{success}. Unable to remove the old file \"{Paths.Final}\".";
				case FileWriteEnum.FailedToRename:
					return $"{success} to \"{Paths.Final}\".";
				default:
					return "unreachable";
			}
		}
	}

	/// <summary>
	/// Provides information about the success or failure of an attempt to write to a file.
	/// If successful, also provides a related object instance.
	/// </summary>
	public class FileWriteResult<T> : FileWriteResult where T : class // Note: "class" also means "notnull".
	{
		/// <summary>
		/// Value will be null if <see cref="FileWriteResult.IsError"/> is true.
		/// Otherwise, Value will not be null.
		/// </summary>
		public readonly T? Value = default;

		internal FileWriteResult(FileWriteEnum error, FileWritePaths paths, Exception? exception) : base(error, paths, exception) { }

		internal FileWriteResult(T value, FileWritePaths paths) : base(FileWriteEnum.Success, paths, null)
		{
			Debug.Assert(value != null, "Should not give a null value on success. Use the non-generic type if there is no value.");
			Value = value;
		}

		public FileWriteResult(FileWriteResult other) : base(other.Error, other.Paths, other.Exception) { }
	}
}
