using System.Text;
using System.IO;

using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	/*
	 * http://sourceforge.net/p/fceultra/code/2696/tree/fceu/src/fds.cpp - only used for timer info
	 * http://nesdev.com/FDS%20technical%20reference.txt - implementation is mostly a combination of
	 * http://wiki.nesdev.com/w/index.php/Family_Computer_Disk_System - these two documents
	 * http://nesdev.com/diskspec.txt - not useless
	 */
	[NesBoardImplCancel]
	internal sealed class FDS : NesBoardBase
	{
		/// <summary>FDS bios image; should be 8192 bytes</summary>
		public byte[] biosrom;
		/// <summary>.FDS disk image</summary>
		private byte[] diskimage;

		private RamAdapter diskdrive;
		private FDSAudio audio;
		/// <summary>currently loaded side of the .FDS image, 0 based</summary>
		private int? currentside = null;
		/// <summary>collection of diffs (as provided by the RamAdapter) for each side in the .FDS image</summary>
		private byte[][] diskdiffs;

		private bool _diskirq;
		private bool _timerirq;

		/// <summary>disk io ports enabled; see 4023.0</summary>
		private bool diskenable = false;
		/// <summary>sound io ports enabled; see 4023.1</summary>
		private bool soundenable = false;
		/// <summary>read on 4033, write on 4026</summary>
		private byte reg4026;

		/// <summary>timer reload</summary>
		private int timerlatch;
		/// <summary>timer current value</summary>
		private int timervalue;
		/// <summary>4022.0,1</summary>
		private byte timerreg;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.BeginSection(nameof(FDS));
			ser.BeginSection(nameof(RamAdapter));
			diskdrive.SyncState(ser);
			ser.EndSection();
			ser.BeginSection(nameof(audio));
			audio.SyncState(ser);
			ser.EndSection();
			{
				// silly little hack
				int tmp = currentside != null ? (int)currentside : 1234567;
				ser.Sync(nameof(currentside), ref tmp);
				currentside = tmp == 1234567 ? null : tmp;
			}
			for (int i = 0; i < NumSides; i++)
				ser.Sync("diskdiffs" + i, ref diskdiffs[i], true);
			ser.Sync(nameof(_timerirq), ref _timerirq);
			ser.Sync(nameof(timer_irq_active), ref timer_irq_active);
			ser.Sync(nameof(timerirq_cd), ref timerirq_cd);
			ser.Sync(nameof(_diskirq), ref _diskirq);
			ser.Sync(nameof(diskenable), ref diskenable);
			ser.Sync(nameof(soundenable), ref soundenable);
			ser.Sync(nameof(reg4026), ref reg4026);
			ser.Sync(nameof(timerlatch), ref timerlatch);
			ser.Sync(nameof(timervalue), ref timervalue);
			ser.Sync(nameof(timerreg), ref timerreg);
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
		public void SetDiskImage(byte[] diskimage)
		{
			// each FDS format is worse than the last
			if (diskimage.AsSpan(start: 0, length: 4).SequenceEqual("\x01*NI"u8))
			{
				int nsides = diskimage.Length / 65500;

				MemoryStream ms = new MemoryStream();
				ms.Write("FDS\x1A"u8.ToArray(), 0, 4);
				ms.WriteByte((byte)nsides);
				byte[] nulls = new byte[11];
				ms.Write(nulls, 0, 11);
				ms.Write(diskimage, 0, diskimage.Length);
				ms.Close();
				diskimage = ms.ToArray();
			}
			
			this.diskimage = diskimage;
			diskdiffs = new byte[NumSides][];
		}

		/// <summary>
		/// returns the currently set disk image.  no effect on emulation (provided the image is not modified).
		/// </summary>
		public byte[] GetDiskImage() => diskimage;

		// as we have [INESBoardImplCancel], this will only be called with an fds disk image
		public override bool Configure(EDetectionOrigin origin)
		{
			if (biosrom == null || biosrom.Length != 8192)
				throw new MissingFirmwareException("FDS bios image needed!");

			Cart.VramSize = 8;
			Cart.WramSize = 32;
			Cart.WramBattery = false;
			Cart.System = "Famicom";
			Cart.BoardType = "FAMICOM_DISK_SYSTEM";

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

		public int NumSides => diskimage[4];

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
			if (side >= NumSides) throw new ArgumentOutOfRangeException(paramName: nameof(side), side, message: "index out of range");
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
					bw.Write(0);
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
			if (!br.ReadBytes(4).SequenceEqual("FDSS"u8))
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

		public override byte[] SaveRam => throw new Exception("FDS Saveram: Must access with method api!");


		public MemoryDomain GetDiskPeeker()
		{
			return new MemoryDomainDelegate("FDS Side", diskdrive.NumBytes, MemoryDomain.Endian.Little, diskdrive.PeekData, null, 1);
		}

		private void SetIRQ()
		{
			IrqSignal = _diskirq || _timerirq;
		}

		private bool diskirq
		{
			get => _diskirq;
			set { _diskirq = value; SetIRQ(); }
		}

		private bool timerirq
		{
			get => _timerirq;
			set { _timerirq = value; SetIRQ(); }
		}

		private int timerirq_cd;
		private bool timer_irq_active;

		public override void WriteExp(int addr, byte value)
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
					break;
				case 0x0021:
					timerlatch &= 0x00ff;
					timerlatch |= value << 8;
					break;
				case 0x0022:
					if (diskenable)
					{
						timerreg = (byte)(value & 3);
						if ((value & 0x02) == 0x02)
						{
							timervalue = timerlatch;
						}
						else
						{
							timerirq = false;
							timer_irq_active = false;
						}
					}
					
					break;
				case 0x0023:
					diskenable = (value & 1) != 0;
					if (!diskenable)
					{
						timerirq = false;
						timer_irq_active = false;
					}
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

		public override byte ReadExp(int addr)
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
						timer_irq_active = false;
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

		public override void ClockCpu()
		{
			if ((timerreg & 2) != 0 && diskenable)
			{
				if (timervalue!=0)
				{
					timervalue--;
				}
				else
				{
					timervalue = timerlatch;
					//timerirq = true;
					if ((timerreg & 1) == 0)
					{
						timerreg -= 2;
					}

					if (!timer_irq_active)
					{
						timer_irq_active = true;
						timerirq_cd = 3;
					}

				}
			}

			if (timerirq_cd > 0)
			{
				timerirq_cd--;
			}

			if ((timerirq_cd == 0) && (timer_irq_active))
			{
				timerirq = true;
			}

			audio.Clock();
		}

		public override void ClockPpu()
		{
			diskdrive.Clock();
			diskirq = diskdrive.irq;
		}

		public override byte ReadWram(int addr)
		{
			return Wram[addr & 0x1fff];
		}

		public override void WriteWram(int addr, byte value)
		{
			Wram[addr & 0x1fff] = value;
		}

		public override byte ReadPrg(int addr)
		{
			if (addr >= 0x6000)
				return biosrom[addr & 0x1fff];
			else
				return Wram[addr + 0x2000];
		}

		public override void WritePrg(int addr, byte value)
		{
			if (addr < 0x6000)
				Wram[addr + 0x2000] = value;
		}
	}
}
