using System;

namespace BizHawk.Emulation.Common
{
	public class MissingFirmwareException : Exception
	{
		public MissingFirmwareException(string message) : base(message)
		{

		}
	}

	public class UnsupportedMapperException : InvalidOperationException
	{
		public UnsupportedMapperException(string message)
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
}
