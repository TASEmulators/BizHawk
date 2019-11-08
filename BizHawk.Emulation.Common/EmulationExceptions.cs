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

	public class NoAvailableCoreException : Exception
	{
		public NoAvailableCoreException()
			: base("System is currently NOT emulated")
		{
		}

		public NoAvailableCoreException(string message)
			: base($"System is currently NOT emulated: {message}")
		{

		}
	}

	public class CGBNotSupportedException : Exception
	{
		public CGBNotSupportedException()
			: base("Core does not support CGB only games!")
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
