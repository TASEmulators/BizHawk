using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	public interface IDebuggable : IEmulatorService
	{
		/// <summary>
		/// Returns a list of Cpu registers and their current state
		/// </summary>
		/// <returns></returns>
		IDictionary<string, Register> GetCpuFlagsAndRegisters();

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

	public class Register
	{
		public ulong Value { get; set; }
		public byte BitSize { get; set; }

		public static implicit operator Register(bool val)
		{
			return new Register
			{
				Value = (ulong)(val ? 1 : 0),
				BitSize = 1
			};
		}

		public static implicit operator Register(byte val)
		{
			return new Register
			{
				Value = val,
				BitSize = 8
			};
		}

		public static implicit operator Register(ushort val)
		{
			return new Register
			{
				Value = val,
				BitSize = 16
			};
		}

		public static implicit operator Register(int val)
		{
			return new Register
			{
				Value = (ulong)val,
				BitSize = 32
			};
		}

		public static implicit operator Register(uint val)
		{
			return new Register
			{
				Value = val,
				BitSize = 32
			};
		}

		public static implicit operator Register(long val)
		{
			return new Register
			{
				Value = (ulong)val,
				BitSize = 64
			};
		}

		public static implicit operator Register(ulong val)
		{
			return new Register
			{
				Value = val,
				BitSize = 64
			};
		}
	}
}
