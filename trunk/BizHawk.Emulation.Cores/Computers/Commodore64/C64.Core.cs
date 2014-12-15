using System;
using System.Collections.Generic;
using System.IO;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Computers.Commodore64.MOS;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	public enum Region
	{
		NTSC,
		PAL
	}

	sealed public partial class C64 : IEmulator, IDebuggable
	{
		private Motherboard board;
		private bool loadPrg;

		private byte[] GetFirmware(string name, int length)
		{
			byte[] result = CoreComm.CoreFileProvider.GetFirmware("C64", name, true);
			if (result.Length != length)
				throw new MissingFirmwareException(string.Format("Firmware {0} was {1} bytes, should be {2} bytes", name, result.Length, length));
			return result;
		}

		private void Init(Region initRegion)
		{
			board = new Motherboard(this, initRegion);
			InitRoms();
			board.Init();
			InitMedia();

			// configure video
			CoreComm.VsyncDen = board.vic.CyclesPerFrame;
			CoreComm.VsyncNum = board.vic.CyclesPerSecond;
		}

		private void InitMedia()
		{
			switch (inputFileInfo.Extension.ToUpper())
			{
				case @".CRT":
					Cart cart = Cart.Load(inputFileInfo.Data);
					if (cart != null)
					{
						board.cartPort.Connect(cart);
					}
					break;
				case @".PRG":
					if (inputFileInfo.Data.Length > 2)
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
				//return (disk.PeekVia1(0x00) & 0x08) != 0;
				return false;
			}
		}

		public void HardReset()
		{
			board.HardReset();
			//disk.HardReset();
		}

		public IDictionary<string, int> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, int>
			{
				{ "A", board.cpu.A },
				{ "X", board.cpu.X },
				{ "Y", board.cpu.Y },
				{ "S", board.cpu.S },
				{ "PC", board.cpu.PC },
				{ "Flag C", board.cpu.FlagC ? 1 : 0 },
				{ "Flag Z", board.cpu.FlagZ ? 1 : 0 },
				{ "Flag I", board.cpu.FlagI ? 1 : 0 },
				{ "Flag D", board.cpu.FlagD ? 1 : 0 },
				{ "Flag B", board.cpu.FlagB ? 1 : 0 },
				{ "Flag V", board.cpu.FlagV ? 1 : 0 },
				{ "Flag N", board.cpu.FlagN ? 1 : 0 },
				{ "Flag T", board.cpu.FlagT ? 1 : 0 }
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();
				case "A":
					board.cpu.A = (byte)value;
					break;
				case "X":
					board.cpu.X = (byte)value;
					break;
				case "Y":
					board.cpu.Y = (byte)value;
					break;
				case "S":
					board.cpu.S = (byte)value;
					break;
				case "PC":
					board.cpu.PC = (ushort)value;
					break;
			}
		}

		[FeatureNotImplemented]
		public void Step(StepType type) { throw new NotImplementedException(); }

		public ITracer Tracer
		{
			[FeatureNotImplemented]
			get { throw new NotImplementedException(); }
		}

		public IMemoryCallbackSystem MemoryCallbacks
		{
			[FeatureNotImplemented]
			get { throw new NotImplementedException(); }
		}
	}

	static public class C64Util
	{
		static public string ToBinary(int n, int charsmin)
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

		static public string ToHex(int n, int charsmin)
		{
			string result = "";

			while (n > 0 || charsmin > 0)
			{
				result = "0123456789ABCDEF".Substring((n & 0xF), 1) + result;
				n >>= 4;
				if (charsmin > 0)
					charsmin--;
			}

			return result;
		}
	}
}
