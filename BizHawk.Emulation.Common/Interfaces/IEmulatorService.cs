using System;
using System.Collections.Generic;
using System.Linq;

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
	/// Should be added to any field of an ICoreService that is not implemented.  By Convention it should also throw a NotImplementedException
	/// Any feature that does not have this attribute is assumed to be implemented
	/// </summary>
	public class FeatureNotImplemented : Attribute
	{
		public FeatureNotImplemented() { }
	}

	/// <summary>
	/// This represents a service that would not apply to every core, instead it is a specialized service specific to a core or group of cores
	/// This service is merely intended to define semantics and expectations of a service
	/// </summary>
	public interface ISpecializedEmulatorService : IEmulatorService
	{
	}

	[AttributeUsage(AttributeTargets.Class)]
	public class ServiceNotApplicable : Attribute
	{
		public ServiceNotApplicable(params Type[] types)
		{
			if (types != null)
			{
				NotApplicableTypes = types.ToList();
			}
			else
			{
				NotApplicableTypes = new List<Type>();
			}
		}

		public IEnumerable<Type> NotApplicableTypes { get; private set; }
	}
}
