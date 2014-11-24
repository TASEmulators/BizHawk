using System;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// This interface specifies that an interface or implementation is a emulator core service, such as IDebuggable,
	/// but is an optional part of the core functionality
	/// Clients should gracefully handle an IEmulator that has a missing or partial implementation of one of these services
	/// </summary>
	public interface ICoreService
	{
	}

	/// <summary>
	/// Should be added to any field of an ICoreService that is not implemented.  By Convention it should also throw a NotImplementedException
	/// Any feature that does not have this attribute is assumed to be implemented
	/// </summary>
	public class FeatureNotImplemented : Attribute
	{
		public FeatureNotImplemented() { }
	}
}
