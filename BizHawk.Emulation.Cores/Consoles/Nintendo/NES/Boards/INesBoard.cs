using System.Collections.Generic;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public interface INesBoard
	{
		// base class pre-configuration
		void Create(NES nes);

		// one-time inherited classes configuration 
		bool Configure(NES.EDetectionOrigin origin);

		// one-time base class configuration (which can take advantage of any information setup by the more-informed Configure() method)
		void PostConfigure();

		// gets called once per PPU clock, for boards with complex behaviour which must be monitoring clock (i.e. mmc3 irq counter)
		void ClockPpu();

		// gets called once per CPU clock; typically for boards with M2 counters
		void ClockCpu();

		byte PeekCart(int addr);

		byte ReadPrg(int addr);
		byte ReadPpu(int addr);
		byte PeekPPU(int addr);
		void AddressPpu(int addr);
		byte ReadWram(int addr);
		byte ReadExp(int addr);
		byte ReadReg2xxx(int addr);
		byte PeekReg2xxx(int addr);
		void WritePrg(int addr, byte value);
		void WritePpu(int addr, byte value);
		void WriteWram(int addr, byte value);
		void WriteExp(int addr, byte value);
		void WriteReg2xxx(int addr, byte value);
		void NesSoftReset();
		void AtVsyncNmi();
		byte[] SaveRam { get; }
		byte[] Wram { get; set; }
		byte[] Vram { get; set; }
		byte[] Rom { get; set; }
		byte[] Vrom { get; set; }
		void SyncState(Serializer ser);
		bool IrqSignal { get; }

		//mixes the board's custom audio into the supplied sample buffer
		void ApplyCustomAudio(short[] samples);

		Dictionary<string, string> InitialRegisterValues { get; set; }
	}
}
