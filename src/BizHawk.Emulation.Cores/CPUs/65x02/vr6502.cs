
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
			ser.Sync(nameof(TotalExecutedCycles), ref TotalExecutedCycles);
			ser.EndSection();
		}
	}
}
