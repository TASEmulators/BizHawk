using System.Collections.Generic;
using System.Runtime.InteropServices;

using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
	public class LibFz80Wrapper
	{
		public const int Z80_PIN_M1 = 24;		// machine cycle 1
		public const int Z80_PIN_MREQ = 25;     // memory request
		public const int Z80_PIN_IORQ = 26;     // input/output request
		public const int Z80_PIN_RD = 27;		// read
		public const int Z80_PIN_WR = 28;		// write
		public const int Z80_PIN_HALT = 29;     // halt state
		public const int Z80_PIN_RFSH = 34;     // refresh

		public const int Z80_PIN_INT = 30;		// interrupt request
		public const int Z80_PIN_RES = 31;		// reset requested
		public const int Z80_PIN_NMI = 32;		// non-maskable interrupt
		public const int Z80_PIN_WAIT = 33;     // wait requested

		public const int Z80_FLAG_C = 0;		// carry
		public const int Z80_FLAG_N = 1;		// add/subtract
		public const int Z80_FLAG_P = 2;        // parity/overflow
		public const int Z80_FLAG_3 = 3;		// undocumented bit 3
		public const int Z80_FLAG_H = 4;        // half carry
		public const int Z80_FLAG_5 = 5;        // undocumented bit 5
		public const int Z80_FLAG_Z = 6;		// zero
		public const int Z80_FLAG_S = 7;		// sign

		/// <summary>
		/// Z80 pin configuration
		/// </summary>
		public ulong Pins
		{
			get => _pins;
			set => _pins = value;
		}

		private ulong _pins;

		// read only pins
		public int M1 => GetPin(Z80_PIN_M1);
		public int MREQ => GetPin(Z80_PIN_MREQ);
		public int IORQ => GetPin(Z80_PIN_IORQ);
		public int RD => GetPin(Z80_PIN_RD);
		public int WR => GetPin(Z80_PIN_WR);
		public int HALT => GetPin(Z80_PIN_HALT);
		public int RFSH => GetPin(Z80_PIN_RFSH);
		public ushort ADDR => (ushort)(_pins & 0xFFFF);

		// write only pins
		public int INT
		{
			get => GetPin(Z80_PIN_INT);
			set => ChangePin(30, value);
		}

		public int RES
		{
			get => GetPin(Z80_PIN_RES);
			set
			{
				_ = value;
				Reset(); // the z80 implementation doesn't implement the RES pin properly
			}
		}

		public int NMI
		{
			get => GetPin(Z80_PIN_NMI);
			set => ChangePin(Z80_PIN_NMI, value);
		}

		public int WAIT
		{
			get => GetPin(Z80_PIN_WAIT);
			set => ChangePin(Z80_PIN_WAIT, value);
		}

		// duplex
		public byte DB
		{
			get => (byte)((_pins >> 16) & 0xFF);
			set => _pins = (_pins & 0xFFFFFFFFFF00FFFF) | ((ulong)value << 16);
		}

		public long TotalExecutedCycles;

		private int GetPin(int pin)
			=> (_pins & (1UL << pin)) != 0 ? 1 : 0;

		private void ChangePin(int pin, int value)
		{
			if (value == 1)
			{
				_pins |= (1UL << pin);
			}
			else
			{
				_pins &= ~(1UL << pin);
			}
		}

		private Z80State Z80;

		[StructLayout(LayoutKind.Sequential)]
		public struct Z80State
		{
			public ushort step;
			public ushort addr;
			public byte dlatch;
			public byte opcode;
			public byte hlx_idx;
			public byte prefix_active;
			public ulong pins;
			public ulong int_bits;
			public ushort pc;
			public ushort af;
			public ushort bc;
			public ushort de;
			public ushort hl;
			public ushort ix;
			public ushort iy;
			public ushort wz;
			public ushort sp;
			public ushort ir;
			public ushort af2, bc2, de2, hl2;
			public byte im;
			public byte iff1, iff2;
		}

		[DllImport("FlooohZ80", CallingConvention = CallingConvention.Cdecl)]
		private static extern ulong LibFz80_Initialize(ref Z80State z80);

		[DllImport("FlooohZ80", CallingConvention = CallingConvention.Cdecl)]
		private static extern ulong LibFz80_Reset(ref Z80State z80);

		[DllImport("FlooohZ80", CallingConvention = CallingConvention.Cdecl)]
		private static extern ulong LibFz80_Tick(ref Z80State z80, ulong pins);

#if false
		[DllImport("FlooohZ80", CallingConvention = CallingConvention.Cdecl)]
		private static extern ulong LibFz80_Prefetch(ref Z80State z80, ushort new_pc);

		[DllImport("FlooohZ80", CallingConvention = CallingConvention.Cdecl)]
		private static extern bool LibFz80_InstructionDone(ref Z80State z80);
#endif

		/// <summary>
		/// Fired when the CPU acknowledges an interrupt
		/// </summary>
		private Action IRQACK_Callbacks;

		public void AttachIRQACKOnCallback(Action irqackCall)
			=> IRQACK_Callbacks += irqackCall;

		public Func<ushort, byte> ReadMemory;
		public Action<ushort, byte> WriteMemory;
		public Func<ushort, byte> PeekMemory;
		public Func<ushort, byte> DummyReadMemory;

		// Port Access
		public Func<ushort, byte> ReadPort;
		public Action<ushort, byte> WritePort;

		//this only calls when the first byte of an instruction is fetched.
		public Action<ushort> OnExecFetch;

		public LibFz80Wrapper()
		{
			_pins = LibFz80_Initialize(ref Z80);
		}

		public void ExecuteOne()
		{
			if (INT == 1 && Z80.iff1 == 0 && Z80.iff2 == 0)
			{
				TraceCallback?.Invoke(new(disassembly: "====IRQ====", registerInfo: string.Empty));
			}

			if (NMI == 1 && Z80.iff1 == 0 && Z80.iff2 == 0)
			{
				TraceCallback?.Invoke(new(disassembly: "====NMI====", registerInfo: string.Empty));
			}

			if (MREQ == 1 && RD == 1)
			{
				DB = ReadMemory(ADDR);

				if (M1 == 1)
				{
					TraceCallback?.Invoke(State());
				}
			}

			if (MREQ == 1 && WR == 1)
			{
				WriteMemory(ADDR, DB);
			}

			if (IORQ == 1 && RD == 1)
			{
				DB = ReadPort(ADDR);
			}

			if (IORQ == 1 && WR == 1)
			{
				WritePort(ADDR, DB);
			}

			if (M1 == 1 && IORQ == 1)
			{
				IRQACK_Callbacks();
			}

			TotalExecutedCycles++;

			_pins = LibFz80_Tick(ref Z80, _pins);
		}

		public ulong Reset()
			=> LibFz80_Reset(ref Z80);

#if false
		public bool InstructionDone()
			=> LibFz80_InstructionDone(ref Z80);

		private ulong Prefetch(ushort new_pc)
			=> LibFz80_Prefetch(ref Z80, new_pc);
#endif

		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, RegisterValue>
			{
				["A"] = Z80.af >> 8,
				["F"] = Z80.af & 0xFF,
				["AF"] = Z80.af,
				["B"] = Z80.bc >> 8,
				["C"] = Z80.bc & 0xFF,
				["BC"] = Z80.bc,
				["D"] = Z80.de >> 8,
				["E"] = Z80.de & 0xFF,
				["DE"] = Z80.de,
				["H"] = Z80.hl >> 8,
				["L"] = Z80.hl & 0xFF,
				["HL"] = Z80.hl,
				["I"] = Z80.ir >> 8,
				["R"] = Z80.ir & 0xFF,
				["IX"] = Z80.ix,
				["IY"] = Z80.iy,
				["PC"] = Z80.pc,
				["Shadow AF"] = Z80.af2,
				["Shadow BC"] = Z80.bc2,
				["Shadow DE"] = Z80.de2,
				["Shadow HL"] = Z80.hl2,
				["SP"] = Z80.sp,
				["Flag C"] = Z80.af.Bit(0),
				["Flag N"] = Z80.af.Bit(1),
				["Flag P/V"] = Z80.af.Bit(2),
				["Flag 3rd"] = Z80.af.Bit(3),
				["Flag H"] = Z80.af.Bit(4),
				["Flag 5th"] = Z80.af.Bit(5),
				["Flag Z"] = Z80.af.Bit(6),
				["Flag S"] = Z80.af.Bit(7)
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();
				case "A":
					Z80.af &= 0x00FF;
					Z80.af |= (ushort)(value << 8);
					break;
				case "F":
					Z80.af &= 0xFF00;
					Z80.af |= (ushort)(value & 0xFF);
					break;
				case "AF":
					Z80.af = (ushort)value;
					break;
				case "B":
					Z80.bc &= 0x00FF;
					Z80.bc |= (ushort)(value << 8);
					break;
				case "C":
					Z80.bc &= 0xFF00;
					Z80.bc |= (ushort)(value & 0xFF);
					break;
				case "BC":
					Z80.bc = (ushort)value;
					break;
				case "D":
					Z80.de &= 0x00FF;
					Z80.de |= (ushort)(value << 8);
					break;
				case "E":
					Z80.de &= 0xFF00;
					Z80.de |= (ushort)(value & 0xFF);
					break;
				case "DE":
					Z80.de = (ushort)value;
					break;
				case "H":
					Z80.hl &= 0xFF00;
					Z80.hl |= (ushort)(value << 8);
					break;
				case "L":
					Z80.hl &= 0x00FF;
					Z80.hl |= (ushort)(value & 0xFF);
					break;
				case "HL":
					Z80.hl = (ushort)value;
					break;
				case "I":
					Z80.ir &= 0x00FF;
					Z80.ir |= (ushort)(value << 8);
					break;
				case "R":
					Z80.ir &= 0xFF00;
					Z80.ir |= (ushort)(value & 0xFF);
					break;
				case "IX":
					Z80.ix = (ushort)value;
					break;
				case "IY":
					Z80.iy = (ushort)value;
					break;
				case "PC":
					Z80.pc = (ushort)value;
					break;
				case "Shadow AF":
					Z80.af2 = (ushort)value;
					break;
				case "Shadow BC":
					Z80.bc2 = (ushort)value;
					break;
				case "Shadow DE":
					Z80.de2 = (ushort)value;
					break;
				case "Shadow HL":
					Z80.hl2 = (ushort)value;
					break;
				case "SP":
					Z80.sp = (ushort)value;
					break;
			}
		}

		public Action<TraceInfo> TraceCallback;

		public string TraceHeader => "Z80A: PC, machine code, mnemonic, operands, registers (AF, BC, DE, HL, IX, IY, SP, Cy), flags (CNP3H5ZS)";

		public TraceInfo State(bool disassemble = true)
		{
			int bytes_read = 0;

			string disasm = disassemble ? BizHawk.Emulation.Cores.Components.Z80A.Z80ADisassembler.Disassemble(Z80.pc, ReadMemory, out bytes_read) : "---";
			string byte_code = null;

			for (ushort i = 0; i < bytes_read; i++)
			{
				byte_code += $"{ReadMemory((ushort)(Z80.pc + i)):X2}";
				if (i < (bytes_read - 1))
				{
					byte_code += " ";
				}
			}

			return new(
				disassembly: $"{Z80.pc:X4}: {byte_code.PadRight(12)} {disasm.PadRight(26)}",
				registerInfo: string.Join(" ",
					$"AF:{Z80.af:X4}",
					$"BC:{Z80.bc:X4}",
					$"DE:{Z80.de:X4}",
					$"HL:{Z80.hl:X4}",
					$"IX:{Z80.ix:X4}",
					$"IY:{Z80.iy:X4}",
					$"SP:{Z80.sp:X4}",
					$"Cy:{TotalExecutedCycles}",
					string.Concat(
						Z80.af.Bit(Z80_FLAG_C) ? "C" : "c",
						Z80.af.Bit(Z80_FLAG_N) ? "N" : "n",
						Z80.af.Bit(Z80_FLAG_P) ? "P" : "p",
						Z80.af.Bit(Z80_FLAG_3) ? "3" : "-",
						Z80.af.Bit(Z80_FLAG_H) ? "H" : "h",
						Z80.af.Bit(Z80_FLAG_5) ? "5" : "-",
						Z80.af.Bit(Z80_FLAG_Z) ? "Z" : "z",
						Z80.af.Bit(Z80_FLAG_S) ? "S" : "s")));
		}

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("FlooohZ80");

			ser.Sync(nameof(Z80.step), ref Z80.step);
			ser.Sync(nameof(Z80.addr), ref Z80.addr);
			ser.Sync(nameof(Z80.dlatch), ref Z80.dlatch);
			ser.Sync(nameof(Z80.opcode), ref Z80.opcode);
			ser.Sync(nameof(Z80.hlx_idx), ref Z80.hlx_idx);
			ser.Sync(nameof(Z80.prefix_active), ref Z80.prefix_active);
			ser.Sync(nameof(Z80.pins), ref Z80.pins);
			ser.Sync(nameof(Z80.int_bits), ref Z80.int_bits);
			ser.Sync(nameof(Z80.pc), ref Z80.pc);
			ser.Sync(nameof(Z80.af), ref Z80.af);
			ser.Sync(nameof(Z80.bc), ref Z80.bc);
			ser.Sync(nameof(Z80.de), ref Z80.de);
			ser.Sync(nameof(Z80.hl), ref Z80.hl);
			ser.Sync(nameof(Z80.ix), ref Z80.ix);
			ser.Sync(nameof(Z80.iy), ref Z80.iy);
			ser.Sync(nameof(Z80.wz), ref Z80.wz);
			ser.Sync(nameof(Z80.sp), ref Z80.sp);
			ser.Sync(nameof(Z80.ir), ref Z80.ir);
			ser.Sync(nameof(Z80.af2), ref Z80.af2);
			ser.Sync(nameof(Z80.bc2), ref Z80.bc2);
			ser.Sync(nameof(Z80.de2), ref Z80.de2);
			ser.Sync(nameof(Z80.hl2), ref Z80.hl2);
			ser.Sync(nameof(Z80.im), ref Z80.im);
			ser.Sync(nameof(Z80.iff1), ref Z80.iff1);
			ser.Sync(nameof(Z80.iff2), ref Z80.iff2);

			ser.Sync(nameof(_pins), ref _pins);
			ser.Sync(nameof(TotalExecutedCycles), ref TotalExecutedCycles);
			
			ser.EndSection();
		}
	}	
}
