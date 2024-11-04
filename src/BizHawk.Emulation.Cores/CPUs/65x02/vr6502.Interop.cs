using System.Runtime.InteropServices;
using static BizHawk.Emulation.Cores.Components.vr6502.vr6502;

namespace BizHawk.Emulation.Cores.Components.vr6502
{
	public static class VrEmu6502Interop
	{
		private const string lib = "vrEmu6502";
		private const CallingConvention cc = CallingConvention.Cdecl;

		/// <summary>
		/// Instantiate a new 6502 emulator
		/// </summary>
		[DllImport(lib, CallingConvention = cc)]
		public static extern IntPtr vrEmu6502New(
		VrEmu6502Model model,
		VrEmu6502MemRead readFn,
		VrEmu6502MemWrite writeFn);

		/// <summary>
		/// Destroy a 6502 emulator
		/// </summary>
		[DllImport(lib, CallingConvention = cc)]
		public static extern void vrEmu6502Destroy(ref VrEmu6502State state);

		/// <summary>
		/// Reset a 6502 emulator
		/// </summary>
		[DllImport(lib, CallingConvention = cc)]
		public static extern void vrEmu6502Reset(ref VrEmu6502State state);

		/// <summary>
		/// A single clock tick
		/// </summary>
		[DllImport(lib, CallingConvention = cc)]
		public static extern void vrEmu6502Tick(ref VrEmu6502State state);

		/// <summary>
		/// A single instruction cycle
		/// </summary>
		[DllImport(lib, CallingConvention = cc)]
		public static extern byte vrEmu6502InstCycle(ref VrEmu6502State state);

		/// <summary>
		/// Pointer to the NMI pin
		/// </summary>
		[DllImport(lib, CallingConvention = cc)]
		public static extern IntPtr vrEmu6502Nmi(ref VrEmu6502State state);

		/// <summary>
		/// Pointer to the INT pin
		/// </summary>
		[DllImport(lib, CallingConvention = cc)]
		public static extern IntPtr vrEmu6502Int(ref VrEmu6502State state);

		/// <summary>
		/// Return the mnemonic string for a given opcode
		/// </summary>
		[DllImport(lib, CallingConvention = cc)]
		public static extern IntPtr vrEmu6502OpcodeToMnemonicStr(ref VrEmu6502State state, byte opcode);

		/// <summary>
		/// Return the address mode for a given opcode
		/// </summary>
		[DllImport(lib, CallingConvention = cc)]
		public static extern IntPtr vrEmu6502GetOpcodeAddrMode(ref VrEmu6502State state, byte opcode);

		[DllImport(lib, CallingConvention = cc)]
		public static extern ushort vrEmu6502DisassembleInstruction(
		ref VrEmu6502State state, ushort addr, int bufferSize, IntPtr buffer,
		IntPtr refAddr, IntPtr labelMap);
	}
}
