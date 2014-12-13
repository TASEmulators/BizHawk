using System;
namespace BizHawk.Emulation.Common
{
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
		IEmulatorService GetService<T>() where T : IEmulatorService;

		/// <summary>
		/// Returns an instance of t if t is available
		/// Else returns null
		/// </summary>
		IEmulatorService GetService(Type t);
	}
}
