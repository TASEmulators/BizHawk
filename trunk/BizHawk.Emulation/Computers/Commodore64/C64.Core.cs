using BizHawk.Emulation.CPUs.M6502;
using BizHawk.Emulation.Computers.Commodore64.MOS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public enum Region
	{
		NTSC,
		PAL
	}

	// emulated chips:
	// U1:  6526 CIA0
	// U2:  6526 CIA1
	// U4:  KERNAL & BASIC ROM
	// U5:  CHARACTER ROM
	// U6:  6510 CPU
	// U7:  VIC 6567 (NTSC) or 6569 (PAL)
	// U8:  Memory multiplexer
	// U9:  SID 6581 or 8580
	// U10: RAM
	// U11: RAM
	// U19: 2114 color RAM

	public partial class  C64 : IEmulator
	{
		// ------------------------------------

		private C64Chips chips;

		// ------------------------------------

		private bool loadPrg;

		// ------------------------------------

		private void Init(Region initRegion)
		{
			chips = new C64Chips(initRegion);
			InitRoms();
			InitMedia();

			// configure video
			CoreOutputComm.VsyncDen = chips.vic.CyclesPerFrame;
			CoreOutputComm.VsyncNum = chips.vic.CyclesPerSecond;

			// configure input
			InitInput();
		}

		private void InitMedia()
		{
			switch (extension.ToUpper())
			{
				case @".CRT":
					Cartridges.Cartridge cart = Cartridges.Cartridge.Load(inputFile);
					if (cart != null)
					{
						chips.cartPort.Connect(cart);
					}
					break;
				case @".PRG":
					if (inputFile.Length > 2)
						loadPrg = true;
					break;
			}
		}

		private void InitRoms()
		{
			string sourceFolder = CoreInputComm.C64_FirmwaresPath;
			if (sourceFolder == null)
				sourceFolder = @".\C64\Firmwares";

			string basicFile = "basic";
			string charFile = "chargen";
			string kernalFile = "kernal";

			string basicPath = Path.Combine(sourceFolder, basicFile);
			string charPath = Path.Combine(sourceFolder, charFile);
			string kernalPath = Path.Combine(sourceFolder, kernalFile);

			if (!File.Exists(basicPath)) HandleFirmwareError(basicFile);
			if (!File.Exists(charPath)) HandleFirmwareError(charFile);
			if (!File.Exists(kernalPath)) HandleFirmwareError(kernalFile);

			byte[] basicRom = File.ReadAllBytes(basicPath);
			byte[] charRom = File.ReadAllBytes(charPath);
			byte[] kernalRom = File.ReadAllBytes(kernalPath);

			chips.basicRom = new Chip23XX(Chip23XXmodel.Chip2364, basicRom);
			chips.kernalRom = new Chip23XX(Chip23XXmodel.Chip2364, kernalRom);
			chips.charRom = new Chip23XX(Chip23XXmodel.Chip2332, charRom);
		}

		// ------------------------------------

		public bool DriveLED
		{
			get
			{
				return false;
			}
		}

		public void Execute(uint count)
		{
			for (; count > 0; count--)
			{
				WriteInputPort();
				chips.ExecutePhase1();
				chips.ExecutePhase2();
			}
		}

		public void HardReset()
		{
			chips.HardReset();
		}

		// ------------------------------------
	}

	public class C64Chips
	{
		public Chip23XX basicRom; //u4
		public CartridgePort cartPort; //cn6
		public Chip23XX charRom; //u5
		public MOS6526 cia0; //u1
		public MOS6526 cia1; //u2
		public Chip2114 colorRam; //u19
		public MOS6510 cpu; //u6
		public Chip23XX kernalRom; //u4
		public MOSPLA pla;
		public Chip4864 ram; //u10+11
		public Sid sid; //u9
		public UserPort userPort; //cn2 (probably won't be needed for games)
		public Vic vic; //u7

		public C64Chips(Region initRegion)
		{
			cartPort = new CartridgePort();
			cia0 = new MOS6526(initRegion);
			cia1 = new MOS6526(initRegion);
			pla = new MOSPLA(this);
			switch (initRegion)
			{
				case Region.NTSC:
					vic = new MOS6567(this);
					break;
				case Region.PAL:
					vic = new MOS6569(this);
					break;
			}
			colorRam = new Chip2114();
			cpu = new MOS6510(this);
			ram = new Chip4864();
			sid = new MOS6581();
			pla.UpdatePins();
		}

		public void ExecutePhase1()
		{
			pla.ExecutePhase1();
			cia0.ExecutePhase1();
			cia1.ExecutePhase1();
			sid.ExecutePhase1();
			vic.ExecutePhase1();
			cpu.ExecutePhase1();
		}

		public void ExecutePhase2()
		{
			pla.ExecutePhase2();
			cia0.ExecutePhase2();
			cia1.ExecutePhase2();
			sid.ExecutePhase2();
			vic.ExecutePhase2();
			cpu.ExecutePhase2();
		}

		public void HardReset()
		{
			// note about hard reset: NOT identical to cold start

			// reset all chips
			cia0.HardReset();
			cia1.HardReset();
			colorRam.HardReset();
			pla.HardReset();
			cpu.HardReset();
			ram.HardReset();
			sid.HardReset();
			vic.HardReset();
		}
	}

	static public class C64Util
	{
		static public string ToBinary(uint n, uint charsmin)
		{
			string result = "";

			while (n > 0 || charsmin > 0)
			{
				result = (((n & 0x1) != 0) ? "1" : "0") + result;
				n >>= 1;
				if (charsmin > 0)
					charsmin--;
			}

			return result;
		}

		static public string ToHex(uint n, uint charsmin)
		{
			string result = "";

			while (n > 0 || charsmin > 0)
			{
				result = "0123456789ABCDEF".Substring((int)(n & 0xF), 1) + result;
				n >>= 4;
				if (charsmin > 0)
					charsmin--;
			}

			return result;
		}
	}
}
