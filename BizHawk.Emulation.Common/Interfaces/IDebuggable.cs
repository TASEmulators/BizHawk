using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	public interface IDebuggable : IEmulator, ICoreService
	{
		/// <summary>
		/// Returns a list of Cpu registers and their current state
		/// </summary>
		/// <returns></returns>
		IDictionary<string, int> GetCpuFlagsAndRegisters();

		/// <summary>
		/// Sets a given Cpu register to the given value
		/// </summary>
		/// <param name="register"></param>
		/// <param name="value"></param>
		void SetCpuRegister(string register, int value);
	}
}
