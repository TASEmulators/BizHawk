using BizHawk.Emulation.CPUs.M6502;
using BizHawk.Emulation.Computers.Commodore64.Cartridge;
using BizHawk.Emulation.Computers.Commodore64.Disk;
using BizHawk.Emulation.Computers.Commodore64.MOS;
using BizHawk.Emulation.Computers.Commodore64.Tape;
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

	public partial class  C64 : IEmulator
	{
		// ------------------------------------

		private Motherboard board;
		private VIC1541 disk;
		private VIC1530 tape;

		// ------------------------------------

		private bool loadPrg;

		// ------------------------------------

		private void Init(Region initRegion)
		{
			board = new Motherboard(initRegion);
			InitRoms();
			board.Init();
			InitDisk(initRegion);
			InitMedia();

			// configure video
			CoreOutputComm.VsyncDen = board.vic.CyclesPerFrame;
			CoreOutputComm.VsyncNum = board.vic.CyclesPerSecond;
		}

		private void InitDisk(Region initRegion)
		{
			string sourceFolder = CoreInputComm.C64_FirmwaresPath;
			if (sourceFolder == null)
				sourceFolder = @".\C64\Firmwares";
			string diskFile = "dos1541";
			string diskPath = Path.Combine(sourceFolder, diskFile);
			if (!File.Exists(diskPath)) HandleFirmwareError(diskFile);
			byte[] diskRom = File.ReadAllBytes(diskPath);

			disk = new VIC1541(initRegion, diskRom);
		}

		private void InitMedia()
		{
			switch (extension.ToUpper())
			{
				case @".CRT":
					Cart cart = Cart.Load(inputFile);
					if (cart != null)
					{
						board.cartPort.Connect(cart);
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
			string diskFile = "dos1541";

			string basicPath = Path.Combine(sourceFolder, basicFile);
			string charPath = Path.Combine(sourceFolder, charFile);
			string kernalPath = Path.Combine(sourceFolder, kernalFile);

			if (!File.Exists(basicPath)) HandleFirmwareError(basicFile);
			if (!File.Exists(charPath)) HandleFirmwareError(charFile);
			if (!File.Exists(kernalPath)) HandleFirmwareError(kernalFile);

			byte[] basicRom = File.ReadAllBytes(basicPath);
			byte[] charRom = File.ReadAllBytes(charPath);
			byte[] kernalRom = File.ReadAllBytes(kernalPath);
			
			board.basicRom = new Chip23XX(Chip23XXmodel.Chip2364, basicRom);
			board.kernalRom = new Chip23XX(Chip23XXmodel.Chip2364, kernalRom);
			board.charRom = new Chip23XX(Chip23XXmodel.Chip2332, charRom);
		}

		// ------------------------------------

		public bool DriveLED
		{
			get
			{
				return false;
			}
		}

		public void HardReset()
		{
			board.HardReset();
			disk.HardReset();
		}

		// ------------------------------------
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
