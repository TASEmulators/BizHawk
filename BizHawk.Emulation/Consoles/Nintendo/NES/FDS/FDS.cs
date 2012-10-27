using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	/*
	 * http://sourceforge.net/p/fceultra/code/2696/tree/fceu/src/fds.cpp - only used for timer info
	 * http://nesdev.com/FDS%20technical%20reference.txt - implementation is mostly a combination of
	 * http://wiki.nesdev.com/w/index.php/Family_Computer_Disk_System - these two documents
	 * http://nesdev.com/diskspec.txt - not useless
	 */
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
		byte[] diskimage;

		/// <summary>
		/// should only be called once, before emulation begins
		/// </summary>
		/// <param name="diskimage"></param>
		public void SetDiskImage(byte[] diskimage)
		{
			this.diskimage = diskimage;
			diskdiffs = new byte[NumSides][];
		}


		RamAdapter diskdrive;
		FDSAudio audio;
		int audioclock;

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
			audio = new FDSAudio();

			InsertSide(0);

			// set mirroring

			return true;
		}

		// with a bit of change, these methods could work with a better disk format

		public int NumSides
		{
			get
			{
				return diskimage[4];
			}
		}

		public void Eject()
		{
			if (currentside != null)
			{
				diskdiffs[(int)currentside] = diskdrive.MakeDiff();
				diskdrive.Eject();
				currentside = null;
			}
		}

		public void InsertSide(int side)
		{
			byte[] buf = new byte[65500];
			Buffer.BlockCopy(diskimage, 16 + side * 65500, buf, 0, 65500);
			diskdrive.InsertBrokenImage(buf, false /*true*/);
			if (diskdiffs[side] != null)
				diskdrive.ApplyDiff(diskdiffs[side]);
			currentside = side;
		}

		int? currentside = null;

		byte[][] diskdiffs;

		public byte[] ReadSaveRam()
		{
			// update diff for currently loaded disk first!
			if (currentside != null)
				diskdiffs[(int)currentside] = diskdrive.MakeDiff();
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			bw.Write(Encoding.ASCII.GetBytes("FDSS"));
			bw.Write(NumSides);
			for (int i = 0; i < NumSides; i++)
			{
				if (diskdiffs[i] != null)
				{
					bw.Write(diskdiffs[i].Length);
					bw.Write(diskdiffs[i]);
				}
				else
				{
					bw.Write((int)0);
				}
			}
			bw.Close();
			return ms.ToArray();
		}

		public void StoreSaveRam(byte[] data)
		{
			// it's strange to modify a disk that's in the process of being read.
			// but in fact, StoreSaveRam() is only called once right at startup, so this is no big deal
			//if (currentside != null)
			//	throw new Exception("FDS Saveram: Can't load when a disk is active!");
			MemoryStream ms = new MemoryStream(data, false);
			BinaryReader br = new BinaryReader(ms);
			byte[] cmp = Encoding.ASCII.GetBytes("FDSS");
			byte[] tmp = br.ReadBytes(cmp.Length);
			if (!cmp.SequenceEqual(tmp))
				throw new Exception("FDS Saveram: bad header");
			int n = br.ReadInt32();
			if (n != NumSides)
				throw new Exception("FDS Saveram: wrong number of sides");
			for (int i = 0; i < NumSides; i++)
			{
				int l = br.ReadInt32();
				if (l > 0)
					diskdiffs[i] = br.ReadBytes(l);
				else
					diskdiffs[i] = null;
			}
			if (currentside != null && diskdiffs[(int)currentside] != null)
				diskdrive.ApplyDiff(diskdiffs[(int)currentside]);
		}

		public void ClearSaveRam()
		{
			if (currentside != null)
				throw new Exception("FDS Saveram: Can't clear when a disk is active!");
			for (int i = 0; i < diskdiffs.Length; i++)
				diskdiffs[i] = null;
		}

		public override byte[] SaveRam
		{ get { throw new Exception("FDS Saveram: Must access with method api!"); } }


		public MemoryDomain GetDiskPeeker()
		{
			return new MemoryDomain("FDS SIDE", diskdrive.NumBytes, Endian.Little, diskdrive.PeekData, null);
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
		byte timerreg;

		byte reg4026;

		public override void WriteEXP(int addr, byte value)
		{
			//if (addr == 0x0025)
			//	Console.WriteLine("W{0:x4}:{1:x2} {2:x4}", addr + 0x4000, value, NES.cpu.PC);

			if (addr >= 0x0040)
			{
				audio.WriteReg(addr + 0x4000, value);
				return;
			}

			switch (addr)
			{
				case 0x0020:
					timerlatch &= 0xff00;
					timerlatch |= value;
					timerirq = false;
					break;
				case 0x0021:
					timerlatch &= 0x00ff;
					timerlatch |= value << 8;
					timerirq = false;
					break;
				case 0x0022:
					timerreg = (byte)(value & 3);
					timervalue = timerlatch * 3;
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

			if (addr >= 0x0040)
				return audio.ReadReg(addr + 0x4000, ret);

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
			//if (addr != 0x0032)
			//	Console.WriteLine("R{0:x4}:{1:x2} {2:x4}", addr + 0x4000, ret, NES.cpu.PC);
			return ret;
		}

		public override void ClockPPU()
		{
			if ((timerreg & 2) != 0 && timervalue > 0)
			{
				timervalue--;
				if (timervalue == 0)
				{
					if ((timerreg & 1) != 0)
					{
						timervalue = timerlatch * 3;
					}
					else
					{
						timerreg &= unchecked((byte)~2);
						timervalue = 0;
						timerlatch = 0;
					}
					timerirq = true;
				}
			}
			diskdrive.Clock();
			diskirq = diskdrive.irq;
			audioclock++;
			if (audioclock == 3)
			{
				audioclock = 0;
				audio.Clock();
			}
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

		public override void ApplyCustomAudio(short[] samples)
		{
			audio.ApplyCustomAudio(samples);
		}
	}
}
