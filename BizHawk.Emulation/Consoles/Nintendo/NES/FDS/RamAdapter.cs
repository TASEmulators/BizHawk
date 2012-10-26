using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	/// <summary>
	/// implements the FDS disk drive hardware, more or less
	/// </summary>
	public class RamAdapter
	{
		static void WriteBlock(Stream dest, byte[] data, int pregap)
		{
			for (int i = 0; i < pregap - 1; i++)
				dest.WriteByte(0);
			dest.WriteByte(0x80); // end of gap marker
			dest.Write(data, 0, data.Length);
			dest.WriteByte(0xff); // CRC (todo)
			dest.WriteByte(0xff); 
		}

		static byte[] FixFDSSide(byte[] inputdisk)
		{
			// the current circulating .fds dumps are horribly broken.  here we attempt to fix them up as best as possible.
			// todo: implement CRC.  since the RamAdapter itself doesn't implement it, broken is not a problem
			// since its not contained in dumps, no way to be sure that the implementation is right

			MemoryStream inp = new MemoryStream(inputdisk, false);
			BinaryReader br = new BinaryReader(inp);

			MemoryStream ret = new MemoryStream();

			// block 1: header
			byte[] header = br.ReadBytes(56);
			byte[] compare = { 0x01, 0x2a, 0x4e, 0x49, 0x4e, 0x54, 0x45, 0x4e, 0x44, 0x4f, 0x2d, 0x48, 0x56, 0x43, 0x2a };
			for (int i = 0; i < compare.Length; i++)
			{
				if (compare[i] != header[i])
					throw new Exception("Corrupt FDS block 1");
			}
			// the rest of block 1 isn't terribly important to parse

			WriteBlock(ret, header, 3537);

			// block 2: number of files
			byte[] numfileblock = br.ReadBytes(2);
			if (numfileblock[0] != 0x02)
			{
				throw new Exception("Corrupt FDS block 2");
			}
			int numfiles = numfileblock[1];

			WriteBlock(ret, numfileblock, 122);

			// repeating block 3 and 4: file header and file data
			for (int i = 0; i < numfiles; i++)
			{
				byte[] fileheader = br.ReadBytes(16);
				if (fileheader[0] != 0x03)
				{
					throw new Exception("Corrupt FDS block 3");
				}
				int filesize = fileheader[13] + fileheader[14] * 256;

				byte[] file = br.ReadBytes(filesize + 1);
				if (file[0] != 0x04)
				{
					throw new Exception("Corrupt FDS block 4");
				}

				WriteBlock(ret, fileheader, 122);
				WriteBlock(ret, file, 122);
			}

			// fix length and return.
			ret.Close();
			byte[] tmp = ret.GetBuffer(); // don't care too much about actual "length" since extra is all 0
			Array.Resize(ref tmp, 65500); // might truncate
			return tmp;
		}


		/// <summary>currently loaded disk side (ca 65k bytes)</summary>
		byte[] disk = null;
		/// <summary>current disk location in BITS, not bytes</summary>
		int diskpos;
		/// <summary>size of current disk in BITS, not bytes</summary>
		int disksize;
		/// <summary>true if current disk is writeprotected</summary>
		bool writeprotect = true;

		public void Eject()
		{
			disk = null;
			state = RamAdapterState.IDLE;
			SetCycles();
			Console.WriteLine("FDS: Disk ejected");
		}

		public void Insert(byte[] side, int bitlength, bool writeprotect)
		{
			disk = side;
			disksize = bitlength;
			diskpos = 0;
			this.writeprotect = writeprotect;
			state = RamAdapterState.INSERTING;
			SetCycles();
			Console.WriteLine("FDS: Disk Inserted");
		}

		/// <summary>
		/// insert a side image from an fds disk
		/// </summary>
		/// <param name="side"></param>
		/// <param name="writeprotect"></param>
		public void InsertBrokenImage(byte[] side, bool writeprotect)
		{
			byte[] realside = FixFDSSide(side);
			Insert(realside, 65500 * 8, writeprotect);
			//File.WriteAllBytes("fdsdebug.bin", realside);
		}

		// all timings are in terms of PPU cycles (@5.37mhz)
		int cycleswaiting = 0;

		enum RamAdapterState
		{
			/// <summary>moving over the disk</summary>
			RUNNING,
			/// <summary>new disk/side into the drive</summary>
			INSERTING,
			/// <summary>motor starting</summary>
			SPINUP,
			/// <summary>head moving back to beginning</summary>
			RESET,
			/// <summary>nothing</summary>
			IDLE,
		};
		RamAdapterState state = RamAdapterState.IDLE;

		void SetCycles()
		{
			switch (state)
			{
				case RamAdapterState.RUNNING:
					cycleswaiting = 56; //82;
					break;
				case RamAdapterState.INSERTING:
					cycleswaiting = 5000000;
					break;
				case RamAdapterState.SPINUP:
					cycleswaiting = 2000;
					break;
				case RamAdapterState.IDLE:
					cycleswaiting = 50000;
					break;
				case RamAdapterState.RESET:
					cycleswaiting = 50000;
					break;
			}
		}

		// data write reg
		public void Write4024(byte value)
		{
			bytetransferflag = false;
			//irq = false; //??
		}

		byte cached4025;

		public bool irq;

		/// <summary>true if 4025.1 is set to true</summary>
		bool transferreset = false;

		// control reg
		public void Write4025(byte value)
		{
			if ((value & 1) != 0) // start motor
			{
				if (state == RamAdapterState.IDLE && disk != null) // no spinup when no disk
				{
					state = RamAdapterState.SPINUP;
					SetCycles();
				}
			}
			if ((value & 2) != 0)
				transferreset = true;
			if ((cached4025 & 0x40) == 0 && (value & 0x40) != 0)
				lookingforendofgap = true;
			irq = false; // ??

			cached4025 = value;
		}

		// some bits come from outside RamAdapter
		public byte Read4030()
		{
			byte ret = 0;
			if (bytetransferflag)
				ret |= 0x02;
			// bit 4 always 0: CRC not implemented
			if (diskpos == disksize)
				ret |= 0x40; // end of disk
			if (disk != null && !writeprotect)
				ret |= 0x80; // writable disk

			// acked
			bytetransferflag = false;
			irq = false;

			return ret;
		}

		/// <summary>
		/// DEBUG ONLY
		/// </summary>
		int lastreaddiskpos;

		public byte Read4031()
		{
			bytetransferflag = false;
			irq = false; //??
			//Console.WriteLine("{0:x2} @{1}", readreglatch, lastreaddiskpos);
			// it seems very hard to avoid this situation, hence the switch to latched shift regs
			//if (readregpos != 0)
			//{
			//	Console.WriteLine("FDS == BIT MISSED ==");
			//}
			return readreglatch;
		}

		public byte Read4032()
		{
			byte ret = 0xff;
			if (disk != null && state != RamAdapterState.INSERTING)
				ret &= unchecked((byte)~0x01);
			if (!transferreset && (state == RamAdapterState.RUNNING || state == RamAdapterState.IDLE))
				ret &= unchecked((byte)~0x02);

			return ret;
		}

		/// <summary>
		/// 5.37mhz
		/// </summary>
		public void Clock()
		{
			cycleswaiting--;
			if (cycleswaiting == 0)
			{
				switch (state)
				{
					case RamAdapterState.RUNNING:
						if (transferreset) // run head to end of disk
							MoveDummy();
						else if ((cached4025 & 4) != 0) // read mode
							Read();
						else
							Write();
						if (diskpos == disksize)
						{
							Console.WriteLine("FDS: End of Disk");
							state = RamAdapterState.RESET;
							transferreset = false;
							//numcrc = 0;
						}
						SetCycles();
						break;

					case RamAdapterState.RESET:
					case RamAdapterState.INSERTING:
						state = RamAdapterState.IDLE;
						diskpos = 0;
						SetCycles();
						transferreset = false;
						//numcrc = 0;
						Console.WriteLine("FDS: Return or Insert Complete");
						break;
					case RamAdapterState.SPINUP:
						state = RamAdapterState.RUNNING;
						SetCycles();
						//transferreset = false; // this definitely does not happen.
						//numcrc = 0;
						Console.WriteLine("FDS: Spin up complete!  Disk is running");
						break;

					case RamAdapterState.IDLE:
						SetCycles();
						break;

				}
			}
		}

		byte readreg;
		byte writereg;
		int readregpos;
		int writeregpos;
		byte readreglatch;
		byte writereglatch;

		bool _bytetransferflag;
		bool bytetransferflag { get { return _bytetransferflag; } set { _bytetransferflag = value; } }

		bool lookingforendofgap = false;

		/// <summary>number of CRC bytes read/written</summary>
		//int numcrc;

		void Read()
		{
			int bit = disk[diskpos >> 3] >> (diskpos & 7) & 1;

			diskpos++;

			if (lookingforendofgap /*(cached4025 & 0x40) != 0*/ && (cached4025 & 0x10) == 0) // looking for end of gap, but not when CRC is active
			{
				if (bit == 1) // found!
				{
					Console.WriteLine("FDS: End of Gap @{0}", diskpos);

					lookingforendofgap = false;//cached4025 &= unchecked((byte)~0x40); // stop looking for end of gap
					readregpos = 0;
					//bytetransferflag = true;
					//if ((cached4025 & 0x80) != 0)
					//	irq = true;
				}
				// else continue scanning gap
			}
			else // reading actual data
			{
				readreg &= (byte)~(1 << readregpos);
				readreg |= (byte)(bit << readregpos);
				readregpos++;
				if (readregpos == 8)
				{
					readregpos = 0;
					//if ((cached4025 & 0x10) == 0) // not in CRC
					//{
						bytetransferflag = true;
						if ((cached4025 & 0x80) != 0)
							irq = true;
						lastreaddiskpos = diskpos;
						//Console.WriteLine("{0:x2} {1} @{2}", readreg, (cached4025 & 0x80) != 0 ? "RAISE" : "    ", diskpos);
						readreglatch = readreg;
					//}
					//else // when in CRC, don't send results back to user
					//{
						if ((cached4025 & 0x10) != 0)
						{
							Console.WriteLine("FDS: crc byte {0:x2} @{1}", readreg, diskpos);
							cached4025 &= unchecked((byte)~0x10); // clear CRC reading
						}
					//}
				}
			}
		}

		void Write()
		{
			diskpos++;
		}

		void MoveDummy()
		{
			diskpos++;
		}


	}
}
