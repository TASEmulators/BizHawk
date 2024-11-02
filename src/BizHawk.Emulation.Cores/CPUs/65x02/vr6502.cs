
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Components.vr6502
{
	public partial class vr6502
	{
		private VrEmu6502State _6502s;
		private readonly VrEmu6502Model _model;

		public long TotalExecutedCycles;

		public vr6502(VrEmu6502Model model, VrEmu6502MemRead readFn, VrEmu6502MemWrite writeFn)
		{
			_model = model;
			_6502s = VrEmu6502Interop.vrEmu6502New(_model, readFn, writeFn);
		}

		public delegate byte VrEmu6502MemRead(ushort addr, bool isDbg);
		public delegate void VrEmu6502MemWrite(ushort addr, byte val);	
		
		public void SetNMI() => _6502s.NmiPin = VrEmu6502Interrupt.IntLow;

		public bool SetIRQ() => _6502s.IntPin == VrEmu6502Interrupt.IntLow;

		public void Reset()
		{
			VrEmu6502Interop.vrEmu6502Reset(ref _6502s);
		}

		public void ExecuteTick()
		{			
			VrEmu6502Interop.vrEmu6502Tick(ref _6502s);
			TotalExecutedCycles++;
		}

		public byte ExecuteInstruction()
		{
			int cycles = VrEmu6502Interop.vrEmu6502InstCycle(ref _6502s);
			TotalExecutedCycles += cycles;
			return (byte)cycles;
		}


		public void SyncState(Serializer ser)
		{
			ser.BeginSection("vrEmu6502");

			ser.SyncEnum("Model", ref _6502s.Model);
			// ReadFn not serializable
			// WriteFn not serializable
			ser.SyncEnum("IntPin", ref _6502s.IntPin);
			ser.SyncEnum("NmiPin", ref _6502s.NmiPin);
			ser.Sync(nameof(_6502s.Step), ref _6502s.Step);
			ser.Sync(nameof(_6502s.CurrentOpcode), ref _6502s.CurrentOpcode);
			ser.Sync(nameof(_6502s.CurrentOpcodeAddr), ref _6502s.CurrentOpcodeAddr);
			ser.Sync(nameof(_6502s.Wai), ref _6502s.Wai);
			ser.Sync(nameof(_6502s.Stp), ref _6502s.Stp);
			ser.Sync(nameof(_6502s.Pc), ref _6502s.Pc);
			ser.Sync(nameof(_6502s.Ac), ref _6502s.Ac);
			ser.Sync(nameof(_6502s.Ix), ref _6502s.Ix);
			ser.Sync(nameof(_6502s.Iy), ref _6502s.Iy);
			ser.Sync(nameof(_6502s.Sp), ref _6502s.Sp);
			ser.Sync(nameof(_6502s.Flags), ref _6502s.Flags);
			ser.Sync(nameof(_6502s.ZpBase), ref _6502s.ZpBase);
			ser.Sync(nameof(_6502s.SpBase), ref _6502s.SpBase);
			ser.Sync(nameof(_6502s.TmpAddr), ref _6502s.TmpAddr);
			// Opcodes????
			// MnemonicNames??
			// AddrModes??

			ser.Sync(nameof(TotalExecutedCycles), ref TotalExecutedCycles);
			ser.EndSection();
		}
	}
}
