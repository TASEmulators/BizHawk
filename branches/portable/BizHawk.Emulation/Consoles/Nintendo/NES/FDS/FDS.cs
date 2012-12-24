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
		#region configuration
		/// <summary>FDS bios image; should be 8192 bytes</summary>
		public byte[] biosrom;
		/// <summary>.FDS disk image</summary>
		byte[] diskimage;
		#endregion

		#region state
		RamAdapter diskdrive;
		FDSAudio audio;
		/// <summary>currently loaded side of the .FDS image, 0 based</summary>
		int? currentside = null;
		/// <summary>collection of diffs (as provided by the RamAdapter) for each side in the .FDS image</summary>
		byte[][] diskdiffs;

		bool _diskirq;
		bool _timerirq;

		/// <summary>disk io ports enabled; see 4023.0</summary>
		bool diskenable = false;
		/// <summary>sound io ports enabled; see 4023.1</summary>
		bool soundenable = false;
		/// <summary>read on 4033, write on 4026</summary>
		byte reg4026;

		/// <summary>timer reload</summary>
		int timerlatch;
		/// <summary>timer current value</summary>
		int timervalue;
		/// <summary>4022.0,1</summary>
		byte timerreg;
		#endregion

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.BeginSection("FDS");
			ser.BeginSection("RamAdapter");
			diskdrive.SyncState(ser);
			ser.EndSection();
			ser.BeginSection("audio");
			audio.SyncState(ser);
			ser.EndSection();
			{
				// silly little hack
				int tmp = currentside != null ? (int)currentside : 1234567;
				ser.Sync("currentside", ref tmp);
				currentside = tmp == 1234567 ? null : (int?)tmp;
			}
			for (int i = 0; i < NumSides; i++)
				ser.Sync("diskdiffs" + i, ref diskdiffs[i], true);
			ser.Sync("_timerirq", ref _timerirq);
			ser.Sync("_diskirq", ref _diskirq);
			ser.Sync("diskenable", ref diskenable);
			ser.Sync("soundenable", ref soundenable);
			ser.Sync("reg4026", ref reg4026);
			ser.Sync("timerlatch", ref timerlatch);
			ser.Sync("timervalue", ref timervalue);
			ser.Sync("timerreg", ref timerreg);
			ser.EndSection();

			SetIRQ();
		}

		public void SetDriveLightCallback(Action<bool> callback)
		{
			diskdrive.DriveLightCallback = callback;
		}

		/// <summary>
		/// should only be called once, before emulation begins
		/// </summary>
		/// <param name="diskimage"></param>
		public void SetDiskImage(byte[] diskimage)
		{
			this.diskimage = diskimage;
			diskdiffs = new byte[NumSides][];
		}

		/// <summary>
		/// returns the currently set disk image.  no effect on emulation (provided the image is not modified).
		/// </summary>
		/// <returns></returns>
		public byte[] GetDiskImage()
		{
			return diskimage;
		}

		// as we have [INESBoardImplCancel], this will only be called with an fds disk image
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			if (biosrom == null || biosrom.Length != 8192)
				throw new Exception("FDS bios image needed!");

			Cart.vram_size = 8;
			Cart.wram_size = 32;
			Cart.wram_battery = false;
			Cart.system = "Famicom";
			Cart.board_type = "FAMICOM_DISK_SYSTEM";

			diskdrive = new RamAdapter();
			if (NES.apu != null)
			{
				//audio = new FDSAudio(NES.cpuclockrate);
				audio = new FDSAudio(NES.apu.ExternalQueue);
			}

			InsertSide(0);
			// set mirroring??
			return true;
		}

		// with a bit of change, these methods could work with a better disk format

		public int NumSides { get { return diskimage[4]; } }

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
			if (side >= NumSides)
				throw new ArgumentOutOfRangeException();
			byte[] buf = new byte[65500];
			Buffer.BlockCopy(diskimage, 16 + side * 65500, buf, 0, 65500);
			diskdrive.InsertBrokenImage(buf, false /*true*/);
			if (diskdiffs[side] != null && diskdiffs[side].Length > 0)
				diskdrive.ApplyDiff(diskdiffs[side]);
			currentside = side;
		}

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
		bool diskirq { get { return _diskirq; } set { _diskirq = value; SetIRQ(); } }
		bool timerirq { get { return _timerirq; } set { _timerirq = value; SetIRQ(); } }


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
					timervalue = timerlatch;
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
						// uncomment to set low battery flag
						// ret &= 0x7f;
					}
					break;
			}
			diskirq = diskdrive.irq;
			//if (addr != 0x0032)
			//	Console.WriteLine("R{0:x4}:{1:x2} {2:x4}", addr + 0x4000, ret, NES.cpu.PC);
			return ret;
		}

		public override byte PeekCart(int addr)
		{
			if (addr >= 0x6000)
				return base.PeekCart(addr);
			else
				return 0; // lazy
		}

		public override void ClockCPU()
		{
			if ((timerreg & 2) != 0 && timervalue > 0)
			{
				timervalue--;
				if (timervalue == 0)
				{
					if ((timerreg & 1) != 0)
					{
						timervalue = timerlatch;
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
			audio.Clock();
		}

		public override void ClockPPU()
		{
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

		/*
		public override void ApplyCustomAudio(short[] samples)
		{
			audio.ApplyCustomAudio(samples);
		}

		public override void Dispose()
		{
			base.Dispose();
			if (audio != null)
			{
				audio.Dispose();
				audio = null;
			}
		}
		*/
	}
}
