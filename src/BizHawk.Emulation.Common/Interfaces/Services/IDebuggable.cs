using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// This service manages debugging capabilities from the core to the client.  Tools such as the debugger make use of this, as well as LUA scripting
	/// This service specifically manages getting/setting CPU registers, managing breakpoints, and stepping through CPU instructions
	/// Providing a disassembly is managed by another service, these are aspects outside of the disassembler that are essential to debugging tools
	/// Tools like the debugger will gracefully degrade based on the availability of each component of this service,
	/// it is expected that any of these features will throw a NotImplementedException if not implemented, and the client will manage accordingly
	/// </summary>
	public interface IDebuggable : IEmulatorService
	{
		/// <summary>
		/// Returns a list of CPU registers and their current state
		/// </summary>
		IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters();

		/// <summary>
		/// Sets a given CPU register to the given value
		/// </summary>
		void SetCpuRegister(string register, int value);

		/// <summary>
		/// Gets a memory callback implementation that manages memory callback functionality
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
		/// Gets the total number of CPU cycles since the beginning of the core's lifecycle
		/// Note that the CPU in this case is the "main" CPU, for some cores that may be somewhat subjective
		/// </summary>
		long TotalExecutedCycles { get; } // TODO: this should probably be a long, but most cores were using int, oh well
	}

	public readonly struct RegisterValue
	{
		public readonly byte BitSize;

		public readonly ulong Value;

		/// <exception cref="ArgumentOutOfRangeException"><paramref name="bitSize"/> not in 1..64</exception>
		public RegisterValue(ulong val, byte bitSize)
		{
			if (bitSize == 64)
			{
				Value = val;
			}
			else if (bitSize > 64 || bitSize == 0)
			{
				throw new ArgumentOutOfRangeException(nameof(bitSize), $"{nameof(BitSize)} must be in 1..64");
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

		public static implicit operator RegisterValue(bool val) => new RegisterValue(val);
		public static implicit operator RegisterValue(byte val) => new RegisterValue(val);
		public static implicit operator RegisterValue(sbyte val) => new RegisterValue(val);
		public static implicit operator RegisterValue(ushort val) => new RegisterValue(val);
		public static implicit operator RegisterValue(short val) => new RegisterValue(val);
		public static implicit operator RegisterValue(uint val) => new RegisterValue(val);
		public static implicit operator RegisterValue(int val) => new RegisterValue(val);
		public static implicit operator RegisterValue(ulong val) => new RegisterValue(val);
		public static implicit operator RegisterValue(long val) => new RegisterValue(val);
	}
}
