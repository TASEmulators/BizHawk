using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	[NES.INESBoardImplCancel]
	public class FDS : NES.NESBoardBase
	{
		/// <summary>
		/// fds bios image; should be 8192 bytes
		/// </summary>
		public byte[] biosrom;

		/// <summary>
		/// .fds disk image
		/// </summary>
		public byte[] diskimage;


		RamAdapter diskdrive;

		// as we have [INESBoardImplCancel], this will only be called with an fds disk image
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			if (biosrom == null || biosrom.Length != 8192)
				throw new Exception("FDS bios image needed!");

			Cart.vram_size = 8;
			Cart.wram_size = 32;
			Cart.wram_battery = false;
			Cart.system = "FDS";
			Cart.board_type = "FAMICOM_DISK_SYSTEM";

			diskdrive = new RamAdapter();

			InsertSide(0);

			// set mirroring

			return true;
		}

		public int NumSides
		{
			get
			{
				return diskimage[4];
			}
		}

		public void Eject()
		{
			diskdrive.Eject();
		}

		public void InsertSide(int side)
		{
			byte[] buf = new byte[65500];
			Buffer.BlockCopy(diskimage, 16 + side * 65500, buf, 0, 65500);
			diskdrive.InsertBrokenImage(buf, true);
		}

		void SetIRQ()
		{
			IRQSignal = _diskirq || _timerirq;
		}
		bool _diskirq;
		bool _timerirq;
		bool diskirq { get { return _diskirq; } set { _diskirq = value; SetIRQ(); } }
		bool timerirq { get { return _timerirq; } set { _timerirq = value; SetIRQ(); } }

		bool diskenable = false;
		bool soundenable = false;

		
		int timerlatch;
		int timervalue;
		bool timeractive;

		byte reg4026;

		public override void WriteEXP(int addr, byte value)
		{

			Console.WriteLine("W{0:x4}:{1:x2} {2:x4}", addr + 0x4000, value, NES.cpu.PC);

			switch (addr)
			{
				case 0x0020:
					timerlatch &= 0xff00;
					timerlatch |= value;
					timerirq = false;
					break;
				case 0x0021:
					timerlatch &= 0xff00;
					timerlatch |= value << 8;
					timerirq = false;
					break;
				case 0x0022:
					if ((value & 1) != 0)
					{
						timeractive = true;
						timervalue = timerlatch * 3;
					}
					break;
				case 0x0023:
					diskenable = (value & 1) != 0;
					soundenable = (value & 2) != 0;
					break;
				case 0x0024:
					if (diskenable)
						diskdrive.Write4024(value);
					break;
				case 0x0025:
					if (diskenable)
						diskdrive.Write4025(value);
					SetMirrorType((value & 8) == 0 ? EMirrorType.Vertical : EMirrorType.Horizontal);
					break;
				case 0x0026:
					if (diskenable)
						reg4026 = value;
					break;
			}
			diskirq = diskdrive.irq;
		}

		public override byte ReadEXP(int addr)
		{
			byte ret = NES.DB;

			switch (addr)
			{
				case 0x0030:
					if (diskenable)
					{
						int tmp = diskdrive.Read4030() & 0xd2;
						ret &= 0x2c;
						if (timerirq)
							ret |= 1;
						ret |= (byte)tmp;
						timerirq = false;
					}
					break;
				case 0x0031:
					if (diskenable)
						ret = diskdrive.Read4031();
					break;
				case 0x0032:
					if (diskenable)
					{
						int tmp = diskdrive.Read4032() & 0x47;
						ret &= 0xb8;
						ret |= (byte)tmp;
					}
					break;
				case 0x0033:
					if (diskenable)
					{
						ret = reg4026;
						ret &= 0x80; // set battery flag
					}
					break;
			}
			diskirq = diskdrive.irq;
			if (addr != 0x0032)
				Console.WriteLine("R{0:x4}:{1:x2} {2:x4}", addr + 0x4000, ret, NES.cpu.PC);
			return ret;
		}

		public override void ClockPPU()
		{
			if (timeractive)
			{
				timervalue--;
				if (timervalue == 0)
				{
					timeractive = false;
					timerirq = true;
				}
			}
			diskdrive.Clock();
			diskirq = diskdrive.irq;
		}

		public override byte ReadWRAM(int addr)
		{
			return WRAM[addr & 0x1fff];
		}

		public override void WriteWRAM(int addr, byte value)
		{
			WRAM[addr & 0x1fff] = value;
		}

		public override byte ReadPRG(int addr)
		{
			if (addr >= 0x6000)
				return biosrom[addr & 0x1fff];
			else
				return WRAM[addr + 0x2000];
		}

		public override void WritePRG(int addr, byte value)
		{
			if (addr < 0x6000)
				WRAM[addr + 0x2000] = value;
		}


	}
}
