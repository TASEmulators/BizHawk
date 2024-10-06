using System.Collections.Generic;
using System.Runtime.InteropServices;

using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
	public class LibFz80Wrapper : IDisposable
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

		/// <summary>
		/// Z80 pin configuration
		/// </summary>
		public ulong Pins
		{
			get { return _pins; }
			set { _pins = value; }
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
			set => Reset();					// the z80 implementation doesn't implement the RES pin properly
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
		{
			return (_pins & (1UL << pin)) != 0 ? 1 : 0;
		}

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

		private IntPtr instance;

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr CreateLibFz80();

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern void DestroyLibFz80(IntPtr instance);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern ulong LibFz80_Initialize(IntPtr instance);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern ulong LibFz80_Reset(IntPtr instance);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern ulong LibFz80_Tick(IntPtr instance, ulong pins);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern ulong LibFz80_Prefetch(IntPtr instance, ushort new_pc);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern bool LibFz80_InstructionDone(IntPtr instance);


		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern ushort LibFz80_GET_step(IntPtr instance);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern ushort LibFz80_GET_addr(IntPtr instance);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern byte LibFz80_GET_dlatch(IntPtr instance);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern byte LibFz80_GET_opcode(IntPtr instance);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern byte LibFz80_GET_hlx_idx(IntPtr instance);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool LibFz80_GET_prefix_active(IntPtr instance);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern ulong LibFz80_GET_pins(IntPtr instance);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern ulong LibFz80_GET_int_bits(IntPtr instance);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern ushort LibFz80_GET_pc(IntPtr instance);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern ushort LibFz80_GET_af(IntPtr instance);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern ushort LibFz80_GET_bc(IntPtr instance);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern ushort LibFz80_GET_de(IntPtr instance);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern ushort LibFz80_GET_hl(IntPtr instance);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern ushort LibFz80_GET_ix(IntPtr instance);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern ushort LibFz80_GET_iy(IntPtr instance);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern ushort LibFz80_GET_wz(IntPtr instance);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern ushort LibFz80_GET_sp(IntPtr instance);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern ushort LibFz80_GET_ir(IntPtr instance);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern ushort LibFz80_GET_af2(IntPtr instance);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern ushort LibFz80_GET_bc2(IntPtr instance);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern ushort LibFz80_GET_de2(IntPtr instance);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern ushort LibFz80_GET_hl2(IntPtr instance);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern byte LibFz80_GET_im(IntPtr instance);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool LibFz80_GET_iff1(IntPtr instance);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool LibFz80_GET_iff2(IntPtr instance);


		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void LibFz80_SET_step(IntPtr instance, ushort value);		

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void LibFz80_SET_addr(IntPtr instance, ushort value);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void LibFz80_SET_dlatch(IntPtr instance, byte value);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void LibFz80_SET_opcode(IntPtr instance, byte value);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void LibFz80_SET_hlx_idx(IntPtr instance, byte value);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void LibFz80_SET_prefix_active(IntPtr instance, bool value);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void LibFz80_SET_pins(IntPtr instance, ulong value);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void LibFz80_SET_addr(IntPtr instance, ulong value);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void LibFz80_SET_int_bits(IntPtr instance, ulong value);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void LibFz80_SET_pc(IntPtr instance, ushort value);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void LibFz80_SET_af(IntPtr instance, ushort value);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void LibFz80_SET_bc(IntPtr instance, ushort value);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void LibFz80_SET_de(IntPtr instance, ushort value);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void LibFz80_SET_hl(IntPtr instance, ushort value);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void LibFz80_SET_ix(IntPtr instance, ushort value);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void LibFz80_SET_iy(IntPtr instance, ushort value);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void LibFz80_SET_wz(IntPtr instance, ushort value);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void LibFz80_SET_sp(IntPtr instance, ushort value);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void LibFz80_SET_ir(IntPtr instance, ushort value);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void LibFz80_SET_af2(IntPtr instance, ushort value);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void LibFz80_SET_bc2(IntPtr instance, ushort value);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void LibFz80_SET_de2(IntPtr instance, ushort value);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void LibFz80_SET_hl2(IntPtr instance, ushort value);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void LibFz80_SET_im(IntPtr instance, byte value);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void LibFz80_SET_iff1(IntPtr instance, bool value);

		[DllImport("FlooohZ80.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void LibFz80_SET_iff2(IntPtr instance, bool value);

		/// <summary>
		/// Public Delegate
		/// </summary>
		public delegate void CallBack();

		/// <summary>
		/// Fired when the CPU acknowledges an interrupt
		/// </summary>
		private CallBack IRQACK_Callbacks;

		public void AttachIRQACKOnCallback(CallBack irqackCall) => IRQACK_Callbacks += irqackCall;

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
			instance = CreateLibFz80();
			_pins = Initialize();
			//Z80State = new Z80();
		}

		public void ExecuteOne()
		{
			ushort step = GetStep();

			if (MREQ == 1 && RD == 1)
			{
				DB = ReadMemory(ADDR);
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

			_pins = Tick(_pins);
		}

		public ulong Reset()
		{
			return LibFz80_Reset(instance);
		}

		public void Dispose()
		{
			if (instance != IntPtr.Zero)
			{
				DestroyLibFz80(instance);
				instance = IntPtr.Zero;
			}
		}

		public bool InstructionDone()
		{
			return LibFz80_InstructionDone(instance);
		}

		private ulong Initialize()
		{
			return LibFz80_Initialize(instance);
		}		

		private ulong Tick(ulong pins)
		{
			return LibFz80_Tick(instance, pins);
		}

		private ulong Prefetch(ushort new_pc)
		{
			return LibFz80_Prefetch(instance, new_pc);
		}


		private ushort GetStep()
		{
			return LibFz80_GET_step(instance);
		}

		private void SetStep(ushort value)
		{
			LibFz80_SET_step(instance, value);
		}

		ushort step;
		ushort addr;
		byte dlatch;
		byte opcode;
		byte hlx_idx;
		bool prefix_active;
		ulong pins;
		ulong int_bits;
		ushort pc;
		ushort af;
		ushort bc;
		ushort de;
		ushort hl;
		ushort ix;
		ushort iy;
		ushort wz;
		ushort sp;
		ushort ir;
		ushort af2;
		ushort bc2;
		ushort de2;
		ushort hl2;
		byte im;
		bool iff1;
		bool iff2;

		private void GetStateFromCpu()
		{
			step = LibFz80_GET_step(instance);
			addr = LibFz80_GET_addr(instance);
			dlatch = LibFz80_GET_dlatch(instance);
			opcode = LibFz80_GET_opcode(instance);
			hlx_idx = LibFz80_GET_hlx_idx(instance);
			prefix_active = LibFz80_GET_prefix_active(instance);
			pins = LibFz80_GET_pins(instance);
			int_bits = LibFz80_GET_int_bits(instance);
			pc = LibFz80_GET_pc(instance);
			af = LibFz80_GET_af(instance);
			bc = LibFz80_GET_bc(instance);
			de = LibFz80_GET_de(instance);
			hl = LibFz80_GET_hl(instance);
			ix = LibFz80_GET_ix(instance);
			iy = LibFz80_GET_iy(instance);
			wz = LibFz80_GET_wz(instance);
			sp = LibFz80_GET_sp(instance);
			ir = LibFz80_GET_ir(instance);
			af2 = LibFz80_GET_af2(instance);
			bc2 = LibFz80_GET_bc2(instance);
			de2 = LibFz80_GET_de2(instance);
			hl2 = LibFz80_GET_hl2(instance);
			im = LibFz80_GET_im(instance);
			iff1 = LibFz80_GET_iff1(instance);
			iff2 = LibFz80_GET_iff2(instance);
		}

		private void PutStateToCpu()
		{
			LibFz80_SET_step(instance, step);
			LibFz80_SET_addr(instance, addr);
			LibFz80_SET_dlatch(instance, dlatch);
			LibFz80_SET_opcode(instance, opcode);
			LibFz80_SET_hlx_idx(instance, hlx_idx);
			LibFz80_SET_prefix_active(instance, prefix_active);
			LibFz80_SET_pins(instance, pins);
			LibFz80_SET_int_bits(instance, int_bits);
			LibFz80_SET_pc(instance, pc);
			LibFz80_SET_af(instance, af);
			LibFz80_SET_bc(instance, bc);
			LibFz80_SET_de(instance, de);
			LibFz80_SET_hl(instance, hl);
			LibFz80_SET_ix(instance, ix);
			LibFz80_SET_iy(instance, iy);
			LibFz80_SET_wz(instance, wz);
			LibFz80_SET_sp(instance, sp);
			LibFz80_SET_ir(instance, ir);
			LibFz80_SET_af2(instance, af2);
			LibFz80_SET_bc2(instance, bc2);
			LibFz80_SET_de2(instance, de2);
			LibFz80_SET_hl2(instance, hl2);
			LibFz80_SET_im(instance, im);
			LibFz80_SET_iff1(instance, iff1);
			LibFz80_SET_iff2(instance, iff2);
		}

		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			GetStateFromCpu();

			return new Dictionary<string, RegisterValue>
			{
				["A"] = af >> 8,
				["AF"] = af,
				["B"] = bc >> 8,
				["BC"] = bc,
				["C"] = bc & 0xff,
				["D"] = de >> 8,
				["DE"] = de,
				["E"] = de & 0xff,
				["F"] = af * 0xff,
				["H"] = hl >> 8,
				["HL"] = hl,
				["I"] = ir >> 8,
				["IX"] = ix,
				["IY"] = iy,
				["L"] = hl & 0xff,
				["PC"] = pc,
				["R"] = ir & 0xff,
				["Shadow AF"] = af2,
				["Shadow BC"] = bc2,
				["Shadow DE"] = de2,
				["Shadow HL"] = hl2,
				["SP"] = sp,
				["Flag C"] = af.Bit(0),
				["Flag N"] = af.Bit(1),
				["Flag P/V"] = af.Bit(2),
				["Flag 3rd"] = af.Bit(3),
				["Flag H"] = af.Bit(4),
				["Flag 5th"] = af.Bit(5),
				["Flag Z"] = af.Bit(6),
				["Flag S"] = af.Bit(7)
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();
				case "A":
					af = LibFz80_GET_af(instance);
					af |= (ushort)(value << 8);
					LibFz80_SET_af(instance, af);
					break;
				case "AF":
					af = (ushort)value;
					LibFz80_SET_af(instance, af);
					break;
				case "B":
					bc = LibFz80_GET_bc(instance);
					bc |= (ushort)(value << 8);
					LibFz80_SET_bc(instance, bc);
					break;
				case "BC":
					bc = (ushort)value;
					LibFz80_SET_bc(instance, bc);
					break;
				case "C":
					bc = LibFz80_GET_bc(instance);
					bc |= (ushort)(value & 0xff);
					LibFz80_SET_bc(instance, bc);
					break;
				case "D":
					de = LibFz80_GET_de(instance);
					de |= (ushort)(value << 8);
					LibFz80_SET_de(instance, de);
					break;
				case "DE":
					de = (ushort)value;
					LibFz80_SET_de(instance, de);
					break;
				case "E":
					de = LibFz80_GET_de(instance);
					de |= (ushort)(value & 0xff);
					LibFz80_SET_de(instance, de);
					break;
				case "F":
					af = LibFz80_GET_af(instance);
					af |= (ushort)(value & 0xff);
					LibFz80_SET_af(instance, af);
					break;
				case "H":
					hl = LibFz80_GET_hl(instance);
					hl |= (ushort)(value << 8);
					LibFz80_SET_hl(instance, hl);
					break;
				case "HL":
					hl = (ushort)value;
					LibFz80_SET_hl(instance, hl);
					break;
				case "I":
					ir = LibFz80_GET_ir(instance);
					ir |= (ushort)(value << 8);
					LibFz80_SET_ir(instance, ir);
					break;
				case "IX":
					ix = (ushort)value;
					LibFz80_SET_ix(instance, ix);
					break;
				case "IY":
					iy = (ushort)value;
					LibFz80_SET_iy(instance, iy);
					break;
				case "L":
					hl = LibFz80_GET_hl(instance);
					hl |= (ushort)(value & 0xff);
					LibFz80_SET_hl(instance, hl);
					break;
				case "PC":
					pc = (ushort)value;
					LibFz80_SET_pc(instance, pc);
					break;
				case "R":
					ir = LibFz80_GET_ir(instance);
					ir |= (ushort)(value & 0xff);
					LibFz80_SET_ir(instance, ir);
					break;
				case "Shadow AF":
					af2 = (ushort)value;
					LibFz80_SET_af2(instance, af2);
					break;
				case "Shadow BC":
					bc2 = (ushort)value;
					LibFz80_SET_bc2(instance, bc2);
					break;
				case "Shadow DE":
					de2 = (ushort)value;
					LibFz80_SET_de2(instance, de2);
					break;
				case "Shadow HL":
					hl2 = (ushort)value;
					LibFz80_SET_hl2(instance, hl2);
					break;
				case "SP":
					sp = (ushort)value;
					LibFz80_SET_sp(instance, sp);
					break;
			}
		}

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("FlooohZ80");

			if (ser.IsWriter)
			{
				GetStateFromCpu();
			}

			ser.Sync(nameof(step), ref step);
			ser.Sync(nameof(addr), ref addr);
			ser.Sync(nameof(dlatch), ref dlatch);
			ser.Sync(nameof(opcode), ref opcode);
			ser.Sync(nameof(hlx_idx), ref hlx_idx);
			ser.Sync(nameof(prefix_active), ref prefix_active);
			ser.Sync(nameof(pins), ref pins);
			ser.Sync(nameof(int_bits), ref int_bits);
			ser.Sync(nameof(pc), ref pc);
			ser.Sync(nameof(af), ref af);
			ser.Sync(nameof(bc), ref bc);
			ser.Sync(nameof(de), ref de);
			ser.Sync(nameof(hl), ref hl);
			ser.Sync(nameof(ix), ref ix);
			ser.Sync(nameof(iy), ref iy);
			ser.Sync(nameof(wz), ref wz);
			ser.Sync(nameof(sp), ref sp);
			ser.Sync(nameof(ir), ref ir);
			ser.Sync(nameof(af2), ref af2);
			ser.Sync(nameof(bc2), ref bc2);
			ser.Sync(nameof(de2), ref de2);
			ser.Sync(nameof(hl2), ref hl2);
			ser.Sync(nameof(im), ref im);
			ser.Sync(nameof(iff1), ref iff1);
			ser.Sync(nameof(iff2), ref iff2);


			if (ser.IsReader)
			{
				PutStateToCpu();
			}

			ser.Sync(nameof(_pins), ref _pins);
			ser.EndSection();
		}
	}	
}
