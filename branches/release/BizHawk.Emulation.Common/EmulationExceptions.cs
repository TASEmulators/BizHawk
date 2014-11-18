using System;

namespace BizHawk.Emulation.Common
{
	public class MissingFirmwareException : Exception
	{
		public MissingFirmwareException(string message) : base(message)
		{

		}
	}

	/// <summary>
	/// indicates that this core does not support the game, but it may be valid
	/// </summary>
	public class UnsupportedGameException : InvalidOperationException
	{
		public UnsupportedGameException(string message)
			: base(message)
		{

		}
	}

	public class CGBNotSupportedException : Exception
	{
		public CGBNotSupportedException()
			: base("Core does not support CGB only games!")
		{
		}

		public CGBNotSupportedException(string message)
			: base(message)
		{
		}
	}

	public class SavestateSizeMismatchException : InvalidOperationException
	{
		public SavestateSizeMismatchException(string message)
			: base(message)
		{

		}
	}
}
