using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Components.vr6502
{
	public partial class vr6502
	{
		private VrEmu6502State _6502s;
		private readonly VrEmu6502Model _model;
		private readonly VrEmu6502MemRead _readFn;
		private readonly VrEmu6502MemWrite _writeFn;

		private VrEmu6502MemRead _readDelegate;
		private VrEmu6502MemWrite _writeDelegate;

		public long TotalExecutedCycles;

		public string[] MnemonicNames;

		public vr6502(VrEmu6502Model model, VrEmu6502MemRead readFn, VrEmu6502MemWrite writeFn)
		{
			_model = model;
			_readFn = readFn;
			_writeFn = writeFn;

			_readDelegate = new VrEmu6502MemRead(_readFn);
			_writeDelegate = new VrEmu6502MemWrite(_writeFn);

			IntPtr cpuPtr = VrEmu6502Interop.vrEmu6502New(_model, _readDelegate, _writeDelegate);

			if (cpuPtr == IntPtr.Zero)
			{
				throw new Exception("Failed to create VrEmu6502 instance.");
			}

			_6502s = Marshal.PtrToStructure<VrEmu6502State>(cpuPtr);

			// dump the mnemonic names
			MnemonicNames = new string[256];
			for (int i = 0; i < 256; i++)
			{
				MnemonicNames[i] = GetOpcodeMnemonic((byte)i);
			}
		}

		public delegate byte VrEmu6502MemRead(ushort addr, bool isDbg);
		public delegate void VrEmu6502MemWrite(ushort addr, byte val);	
		
		public void SetNMI() => _6502s.nmiPin = VrEmu6502Interrupt.IntLow;

		public bool SetIRQ() => _6502s.intPin == VrEmu6502Interrupt.IntLow;

		public bool RDY;

		public void Reset()
		{
			VrEmu6502Interop.vrEmu6502Reset(ref _6502s);
		}

		public void ExecuteTick()
		{	
			if (RDY)
			{
				VrEmu6502Interop.vrEmu6502Tick(ref _6502s);
			}
			
			TotalExecutedCycles++;
		}

		public byte ExecuteInstruction()
		{
			int cycles = 0;

			if (RDY)
			{
				cycles = VrEmu6502Interop.vrEmu6502InstCycle(ref _6502s);
			}
			 
			TotalExecutedCycles += cycles;
			return (byte)cycles;
		}


		private string GetOpcodeMnemonic(byte opcode)
		{
			IntPtr strPtr = VrEmu6502Interop.vrEmu6502OpcodeToMnemonicStr(ref _6502s, opcode);
			return Marshal.PtrToStringAnsi(strPtr);
		}

		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, RegisterValue>
			{
				["A"] = _6502s.ac >> 8,
				["X"] = _6502s.ix & 0xFF,
				["Y"] = _6502s.iy & 0xFF,
				["SP"] = _6502s.sp,
				["PC"] = _6502s.pc,
				["Flag C"] = _6502s.flags.Bit(0),
				["Flag Z"] = _6502s.flags.Bit(1),
				["Flag I"] = _6502s.flags.Bit(2),
				["Flag D"] = _6502s.flags.Bit(3),
				["Flag B"] = _6502s.flags.Bit(4),
				["Flag V"] = _6502s.flags.Bit(5),
				["Flag N"] = _6502s.flags.Bit(6),
				["Flag T"] = _6502s.flags.Bit(7),
			};
		}


		public void SyncState(Serializer ser)
		{
			ser.BeginSection("vrEmu6502");

			ser.Sync(nameof(RDY), ref RDY);

			ser.SyncEnum(nameof(_6502s.Model), ref _6502s.Model);
			// ReadFn not serializable
			// WriteFn not serializable
			ser.SyncEnum(nameof(_6502s.intPin), ref _6502s.intPin);
			ser.SyncEnum(nameof(_6502s.nmiPin), ref _6502s.nmiPin);
			ser.Sync(nameof(_6502s.step), ref _6502s.step);
			ser.Sync(nameof(_6502s.currentOpcode), ref _6502s.currentOpcode);
			ser.Sync(nameof(_6502s.currentOpcodeAddr), ref _6502s.currentOpcodeAddr);
			ser.Sync(nameof(_6502s.wai), ref _6502s.wai);
			ser.Sync(nameof(_6502s.stp), ref _6502s.stp);
			ser.Sync(nameof(_6502s.pc), ref _6502s.pc);
			ser.Sync(nameof(_6502s.ac), ref _6502s.ac);
			ser.Sync(nameof(_6502s.ix), ref _6502s.ix);
			ser.Sync(nameof(_6502s.iy), ref _6502s.iy);
			ser.Sync(nameof(_6502s.sp), ref _6502s.sp);
			ser.Sync(nameof(_6502s.flags), ref _6502s.flags);
			ser.Sync(nameof(_6502s.zpBase), ref _6502s.zpBase);
			ser.Sync(nameof(_6502s.spBase), ref _6502s.spBase);
			ser.Sync(nameof(_6502s.tmpAddr), ref _6502s.tmpAddr);
			// Opcodes????
			// MnemonicNames??
			// AddrModes??

			ser.Sync(nameof(TotalExecutedCycles), ref TotalExecutedCycles);
			ser.EndSection();
		}
	}
}
