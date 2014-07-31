using System;

namespace BizHawk.Emulation.Common
{
	public class MissingFirmwareException : Exception
	{
		public MissingFirmwareException(string message) : base(message)
		{

		}
	}
}
