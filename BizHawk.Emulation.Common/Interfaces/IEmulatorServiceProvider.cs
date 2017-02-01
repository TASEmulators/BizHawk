using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// This interface defines the mechanism by which clients can retrive IEmulatorServices
	/// from an IEmulator implementation
	/// An implementation should collect all available IEmulatorService instances.
	/// This interface defines only the client interaction.  This interface does not specify the means
	/// by which a service provider will be populated with available services.  However, an implementation
	/// by design must provide this mechanism
	/// </summary>
	/// <seealso cref="IEmulator" /> 
	/// <seealso cref="IEmulatorService"/> 
	public interface IEmulatorServiceProvider
	{
		/// <summary>
		/// Returns whether or not T is available
		/// </summary>
		bool HasService<T>() where T : IEmulatorService;
		
		/// <summary>
		/// Returns whether or not t is available
		/// </summary>
		bool HasService(Type t);

		/// <summary>
		/// Returns an instance of T if T is available
		/// Else returns null
		/// </summary>
		T GetService<T>() where T : IEmulatorService;

		/// <summary>
		/// Returns an instance of t if t is available
		/// Else returns null
		/// </summary>
		object GetService(Type t);

		/// <summary>
		/// A list of all currently registered services available to be retrieved
		/// </summary>
		IEnumerable<Type> AvailableServices { get; }
	}
}
