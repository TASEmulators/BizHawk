#nullable enable

using System.Diagnostics;
using System.IO;

namespace BizHawk.Client.Common
{
	public enum FileWriteEnum
	{
		Success,
		FailedToOpen,
		FailedDuringWrite,
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
		public readonly string WritePath;

		public bool IsError => Error != FileWriteEnum.Success;

		public FileWriteResult(FileWriteEnum error, string path, Exception? exception)
		{
			Error = error;
			Exception = exception;
			WritePath = path;
		}

		/// <summary>
		/// Converts this instance to a different generic type.
		/// The new instance will take the value given only if this instance has no error.
		/// </summary>
		/// <param name="value">The value of the new instance. Ignored if this instance has an error.</param>
		public FileWriteResult<T> Convert<T>(T value) where T : class
		{
			if (Error == FileWriteEnum.Success) return new(value, WritePath);
			else return new(this);
		}

		public FileWriteResult(FileWriteResult other) : this(other.Error, other.WritePath, other.Exception) { }

		public string UserFriendlyErrorMessage()
		{
			switch (Error)
			{
				case FileWriteEnum.Success:
					return $"The file {WritePath} was written successfully.";
				case FileWriteEnum.FailedToOpen:
					if (WritePath.Contains(".saving"))
					{
						return $"The temporary file {WritePath} already exists and could not be deleted.";
					}
					return $"The file {WritePath} could not be created.";
				case FileWriteEnum.FailedDuringWrite:
					return $"An error occurred while writing the file."; // No file name here; it should be deleted.
				case FileWriteEnum.FailedToDeleteOldFile:
					string fileWithoutPath = Path.GetFileName(WritePath);
					return $"The file {WritePath} was created successfully, but the old file could not be deleted. You may manually rename the temporary file {fileWithoutPath}.";
				case FileWriteEnum.FailedToRename:
					fileWithoutPath = Path.GetFileName(WritePath);
					return $"The file {WritePath} was created successfully, but could not be renamed. You may manually rename the temporary file {fileWithoutPath}.";
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

		public FileWriteResult(FileWriteEnum error, string path, Exception? exception) : base(error, path, exception) { }

		public FileWriteResult(T value, string path) : base(FileWriteEnum.Success, path, null)
		{
			Debug.Assert(value != null, "Should not give a null value on success. Use the non-generic type if there is no value.");
			Value = value;
		}

		public FileWriteResult(FileWriteResult other) : base(other.Error, other.WritePath, other.Exception) { }
	}
}
