using System;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// Should be added to any field of an IEmulatorService that is not implemented.
	/// By Convention it should also throw a NotImplementedException
	/// Any feature that does not have this attribute is assumed to be implemented
	/// </summary>
	public class FeatureNotImplemented : Attribute { }

	/// <summary>
	/// Should be added to any implementation of IEmulator to document any
	/// IEmulatorService (that is not an ISpecializedEmulatorService) that
	/// by design, will not be implemented by the core
	/// Any service that is unimplemented and not marked with this attribute is
	/// assumed to be a "TODO" that needs to be done but hasn't been done yet
	/// </summary>
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
