using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	public interface IDebuggable : IEmulatorService
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

		ITracer Tracer { get; }

		IMemoryCallbackSystem MemoryCallbacks { get; }

		// Advanced Navigation
		//void StepInto();
		//void StepOut();
		//void StepOver();

		void Step(StepType type);
	}

	public enum StepType { Into, Out, Over }
}
