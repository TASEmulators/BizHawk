using System;

namespace BizHawk.Emulation.Common
{
	public interface IEmulatorServiceProvider
	{
		/// <summary>
		/// Returns whether or not T is available
		/// </summary>
		bool HasService<T>();
		
		/// <summary>
		/// Returns whether or not t is available
		/// </summary>
		bool HasService(Type t);

		/// <summary>
		/// Returns an instance of T if T is available
		/// Else returns null
		/// </summary>
		T GetService<T>();

		/// <summary>
		/// Returns an instance of t if t is available
		/// Else returns null
		/// </summary>
		object GetService(Type t);
	}
}
