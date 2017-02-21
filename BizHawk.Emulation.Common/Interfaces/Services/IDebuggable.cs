using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// This service manages debugging capabilities from the core to the client.  Tools such as the debugger make use of this, as well as lua scripting
	/// This service specifically manages getting/setting cpu registers, managing breakpoints, and stepping through cpu instructions
	/// Providing a disassembly is managed by another service, these are aspects outside of the disassembler that are essential to debugging tools
	/// Tools like the debugger will gracefully degrade based on the availability of each component of this service,
	/// it is expected that any of these features will throw a NotImplementedException if not implemented, and the client will manage accordingly
	/// </summary>
	public interface IDebuggable : IEmulatorService
	{
		/// <summary>
		/// Returns a list of Cpu registers and their current state
		/// </summary>
		IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters();

		/// <summary>
		/// Sets a given Cpu register to the given value
		/// </summary>
		void SetCpuRegister(string register, int value);

		/// <summary>
		/// A memory callback implementation that manages memory callback functionality
		/// </summary>
		IMemoryCallbackSystem MemoryCallbacks { get; }

		/// <summary>
		/// Informs the calling code whether or not the given step type is implemented,
		/// if false, a NotImplementedException will be thrown if Step is called with the given value
		/// </summary>
		bool CanStep(StepType type);

		/// <summary>
		/// Advances the core based on the given Step type
		/// </summary>
		void Step(StepType type);

		/// <summary>
		/// Total number of cpu cycles since the beginning of the core's lifecycle
		/// Note that the cpu in this case is the "main" cpu, for some cores that may be somewhat subjective
		/// </summary>
		int TotalExecutedCycles { get; } // TODO: this should probably be a long, but most cores were using int, oh well
	}

	public class RegisterValue
	{
		public ulong Value { get; private set; }
		public byte BitSize { get; private set; }

		public RegisterValue(ulong val, byte bitSize)
		{
			if (bitSize == 64)
			{
				Value = val;
			}
			else if (bitSize > 64 || bitSize == 0)
			{
				throw new System.ArgumentOutOfRangeException("bitSize", "BitSize must be in 1..64");
			}
			else
			{
				Value = val & (1UL << bitSize) - 1;
			}

			BitSize = bitSize;
		}

		public RegisterValue(bool val)
		{
			Value = val ? 1UL : 0UL;
			BitSize = 1;
		}

		public RegisterValue(byte val)
		{
			Value = val;
			BitSize = 8;
		}

		public RegisterValue(sbyte val)
		{
			Value = (byte)val;
			BitSize = 8;
		}

		public RegisterValue(ushort val)
		{
			Value = val;
			BitSize = 16;
		}

		public RegisterValue(short val)
		{
			Value = (ushort)val;
			BitSize = 16;
		}

		public RegisterValue(uint val)
		{
			Value = val;
			BitSize = 32;
		}

		public RegisterValue(int val)
		{
			Value = (uint)val;
			BitSize = 32;
		}

		public RegisterValue(ulong val)
		{
			Value = val;
			BitSize = 64;
		}

		public RegisterValue(long val)
		{
			Value = (ulong)val;
			BitSize = 64;
		}


		public static implicit operator RegisterValue(bool val)
		{
			return new RegisterValue(val);
		}

		public static implicit operator RegisterValue(byte val)
		{
			return new RegisterValue(val);
		}

		public static implicit operator RegisterValue(sbyte val)
		{
			return new RegisterValue(val);
		}

		public static implicit operator RegisterValue(ushort val)
		{
			return new RegisterValue(val);
		}

		public static implicit operator RegisterValue(short val)
		{
			return new RegisterValue(val);
		}

		public static implicit operator RegisterValue(uint val)
		{
			return new RegisterValue(val);
		}

		public static implicit operator RegisterValue(int val)
		{
			return new RegisterValue(val);
		}

		public static implicit operator RegisterValue(ulong val)
		{
			return new RegisterValue(val);
		}

		public static implicit operator RegisterValue(long val)
		{
			return new RegisterValue(val);
		}
	}
}
