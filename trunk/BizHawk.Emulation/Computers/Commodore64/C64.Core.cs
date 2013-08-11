using BizHawk.Emulation.Computers.Commodore64.Cartridge;
using BizHawk.Emulation.Computers.Commodore64.Disk;
using BizHawk.Emulation.Computers.Commodore64.MOS;
using System.IO;

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

		// ------------------------------------

		private bool loadPrg;

		// ------------------------------------

        private byte[] GetFirmware(string name, int length)
        {
            byte[] result = new byte[length];
            using (Stream source = CoreComm.CoreFileProvider.OpenFirmware("C64", name))
            {
                source.Read(result, 0, length);
            }
            return result;
        }

		private void Init(Region initRegion)
		{
			board = new Motherboard(initRegion);
			InitRoms();
			board.Init();
			InitDisk(initRegion);
			InitMedia();

			// configure video
			CoreComm.VsyncDen = board.vic.CyclesPerFrame;
			CoreComm.VsyncNum = board.vic.CyclesPerSecond;
		}

		private void InitDisk(Region initRegion)
		{
            byte[] diskRom = new byte[0x4000]; //GetFirmware("dos1541", 0x4000);

			disk = new VIC1541(initRegion, diskRom);
			//disk.Connect(board.serPort);
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
            byte[] basicRom = GetFirmware("Basic", 0x2000);
            byte[] charRom = GetFirmware("Chargen", 0x1000);
            byte[] kernalRom = GetFirmware("Kernal", 0x2000);
			
			board.basicRom = new Chip23XX(Chip23XXmodel.Chip2364, basicRom);
			board.kernalRom = new Chip23XX(Chip23XXmodel.Chip2364, kernalRom);
			board.charRom = new Chip23XX(Chip23XXmodel.Chip2332, charRom);
		}

		// ------------------------------------

		public bool DriveLED
		{
			get
			{
				return (disk.PeekVia1(0x00) & 0x08) != 0;
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
