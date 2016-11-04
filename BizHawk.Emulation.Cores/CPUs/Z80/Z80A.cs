using System;
using System.Globalization;
using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

// This Z80 emulator is a modified version of Ben Ryves 'Brazil' emulator.
// It is MIT licensed.

namespace BizHawk.Emulation.Cores.Components.Z80
{
	public sealed partial class Z80A
	{
		public Z80A()
		{
			InitialiseTables();
			Reset();
		}

		public void Reset()
		{
			ResetRegisters();
			ResetInterrupts();
			PendingCycles = 0;
			ExpectedExecutedCycles = 0;
			TotalExecutedCycles = 0;
		}

		public void SoftReset()
		{
			ResetRegisters();
			ResetInterrupts();
		}

		// Memory Access 

		public Func<ushort, bool, byte> FetchMemory;
		public Func<ushort, byte> ReadMemory;
		public Action<ushort, byte> WriteMemory;

		public byte ReadMemoryWrapper(ushort addr)
		{
			if (MemoryCallbacks != null)
			{
				MemoryCallbacks.CallReads(addr);
			}

			return ReadMemory(addr);
		}

		public byte FetchFirstMemoryWrapper(ushort addr)
		{
			if (MemoryCallbacks != null)
			{
				MemoryCallbacks.CallReads(addr);
			}

			if (FetchMemory != null)
			{
				return FetchMemory(addr, true);
			}

			return ReadMemory(addr);
		}

		public byte FetchMemoryWrapper(ushort addr)
		{
			if (MemoryCallbacks != null)
			{
				MemoryCallbacks.CallReads(addr);
			}

			if (FetchMemory != null)
			{
				return FetchMemory(addr, false);
			}

			return ReadMemory(addr);
		}

		public void WriteMemoryWrapper(ushort addr, byte value)
		{
			if (MemoryCallbacks != null)
			{
				MemoryCallbacks.CallWrites(addr);
			}

			WriteMemory(addr, value);
		}

		public IMemoryCallbackSystem MemoryCallbacks { get; set; }

		// Utility function, not used by core
		public ushort ReadWord(ushort addr)
		{
			ushort value = ReadMemory(addr++);
			value |= (ushort)(ReadMemory(addr) << 8);
			return value;
		}

		// Hardware I/O Port Access

		public Func<ushort, byte> ReadHardware;
		public Action<ushort, byte> WriteHardware;

		// State Save/Load

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("Z80");
			ser.Sync("AF", ref RegAF.Word);
			ser.Sync("BC", ref RegBC.Word);
			ser.Sync("DE", ref RegDE.Word);
			ser.Sync("HL", ref RegHL.Word);
			ser.Sync("ShadowAF", ref RegAltAF.Word);
			ser.Sync("ShadowBC", ref RegAltBC.Word);
			ser.Sync("ShadowDE", ref RegAltDE.Word);
			ser.Sync("ShadowHL", ref RegAltHL.Word);
			ser.Sync("I", ref RegI);
			ser.Sync("R", ref RegR);
			ser.Sync("IX", ref RegIX.Word);
			ser.Sync("IY", ref RegIY.Word);
			ser.Sync("SP", ref RegSP.Word);
			ser.Sync("PC", ref RegPC.Word);
			ser.Sync("IRQ", ref interrupt);
			ser.Sync("NMI", ref nonMaskableInterrupt);
			ser.Sync("NMIPending", ref nonMaskableInterruptPending);
			ser.Sync("IM", ref interruptMode);
			ser.Sync("IFF1", ref iff1);
			ser.Sync("IFF2", ref iff2);
			ser.Sync("Halted", ref halted);
			ser.Sync("ExecutedCycles", ref totalExecutedCycles);
			ser.Sync("PendingCycles", ref pendingCycles);
			ser.Sync("EI_pending", ref EI_pending);
			ser.EndSection();
		}
	}
}