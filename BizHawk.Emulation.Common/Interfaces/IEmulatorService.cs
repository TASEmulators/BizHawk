namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// This interface specifies that an interface or implementation is a emulator core service, such as IDebuggable,
	/// but is an optional part of the core functionality
	/// Clients should gracefully handle an IEmulator that has a missing or partial implementation of one of these services
	/// </summary>
	public interface IEmulatorService
	{
	}

	/// <summary>
	/// This represents a service that would not apply to every core,
	/// instead it is a specialized service specific to a core or group of cores
	/// This service is merely intended to define semantics and expectations of a service
	/// Services of this type are not assumed to be "missing" from cores that fail to implement them
	/// </summary>
	public interface ISpecializedEmulatorService : IEmulatorService
	{
	}
}
