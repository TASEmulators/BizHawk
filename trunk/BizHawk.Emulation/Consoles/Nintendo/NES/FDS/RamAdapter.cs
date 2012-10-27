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
			ushort crc = 0;
			dest.WriteByte(0x80); // end of gap marker
			crc = CCITT_8(crc, 0x80);
			for (int i = 0; i < data.Length; i++)
			{
				dest.WriteByte(data[i]);
				crc = CCITT_8(crc, data[i]);
			}
			dest.WriteByte((byte)(crc & 0xff));
			dest.WriteByte((byte)(crc >> 8)); 
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

		static ushort CCITT(ushort crc, int bit)
		{
			int bitc = crc & 1;
			crc >>= 1;
			if ((bitc ^ bit) != 0)
				crc ^= 0x8408;
			return crc;
		}

		static ushort CCITT_8(ushort crc, byte b)
		{
			for (int i = 0; i < 8; i++)
			{
				int bit = (b >> i) & 1;
				crc = CCITT(crc, bit);
			}
			return crc;
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

		/// <summary>
		/// memorydomain debugging
		/// </summary>
		public int NumBytes { get { return 65500; } }
		/// <summary>
		/// memorydomain debugging
		/// </summary>
		/// <param name="addr"></param>
		/// <returns></returns>
		public byte PeekData(int addr)
		{
			if (disk != null && disk.Length > addr)
				return disk[addr];
			else
				return 0xff;
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
			// these are mostly guesses
			switch (state)
			{
				case RamAdapterState.RUNNING: // matches of-quoted 96khz data transfer rate
					// time to read/write one bit
					cycleswaiting = 56;
					break;
				case RamAdapterState.INSERTING: // 298ms
					// time for the disk drive to engage on disk after inserting
					cycleswaiting = 1600000;
					break;
				case RamAdapterState.SPINUP: // 199ms
					// time for motor to spinup and start
					cycleswaiting = 1070000;
					break;
				case RamAdapterState.IDLE: // irrelevant
					cycleswaiting = 50000;
					break;
				case RamAdapterState.RESET: // 1200ms
					// time for motor to re-park after reaching end of drive
					cycleswaiting = 6443100;
					break;
			}
		}

		// data write reg
		public void Write4024(byte value)
		{
			bytetransferflag = false;
			writereglatch = value;
			//Console.WriteLine("!!4024:{0:x2}", value);
		}

		byte cached4025;

		public bool irq;

		/// <summary>true if 4025.1 is set to true</summary>
		bool transferreset = false;

		ushort crc = 0;

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
			{
				lookingforendofgap = true;

				if ((value & 4) == 0)
				{
					// write mode: reload and go
					writeregpos = 0;
					writereg = writereglatch;
					bytetransferflag = true;
					// irq?
					Console.WriteLine("FDS: Startwrite @{0} Reload {1:x2}", diskpos, writereglatch);
					crc = 0;
					writecomputecrc = true;
				}
			}


			irq = false; // ??

			cached4025 = value;
			if ((cached4025 & 4) == 0)
				if ((cached4025 & 0x10) != 0)
					Console.WriteLine("FDS: Starting CRC");
		}

		// some bits come from outside RamAdapter
		public byte Read4030()
		{
			byte ret = 0;
			if (bytetransferflag)
				ret |= 0x02;
			if (crc != 0)
				ret |= 0x10;
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
			if (disk != null && state != RamAdapterState.INSERTING && !writeprotect)
				ret &= unchecked((byte)~0x04);

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

		/// <summary>
		/// true if data being written to disk is currently being computed in CRC
		/// </summary>
		bool writecomputecrc; // this has to be latched because the "flush CRC" call comes in the middle of a byte, of course

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
					crc = 0;
					// the first '1' is included in the CRC
					crc = CCITT(crc, 1);
					//bytetransferflag = true;
					//if ((cached4025 & 0x80) != 0)
					//	irq = true;
				}
				// else continue scanning gap
			}
			else // reading actual data
			{
				crc = CCITT(crc, bit);
				readreg &= (byte)~(1 << readregpos);
				readreg |= (byte)(bit << readregpos);
				readregpos++;
				if (readregpos == 8)
				{
					readregpos = 0;

					bytetransferflag = true;
					if ((cached4025 & 0x80) != 0)
						irq = true;
					lastreaddiskpos = diskpos;
					//Console.WriteLine("{0:x2} {1} @{2}", readreg, (cached4025 & 0x80) != 0 ? "RAISE" : "    ", diskpos);
					readreglatch = readreg;

					if ((cached4025 & 0x10) != 0)
					{
						Console.WriteLine("FDS: crc byte {0:x2} @{1}", readreg, diskpos);
						cached4025 &= unchecked((byte)~0x10); // clear CRC reading.  no real effect other than to silence debug??
						Console.WriteLine("FDS: Final CRC {0:x4}", crc);
					}
				}
			}
		}

		void Write()
		{
			if (writeprotect)
			{
				diskpos++;
				return;
			}

			bool bittowrite = false;

			// the variable is named for its function in read mode; in write mode, when not set,
			// write an endless stream of zeroes.
			if (!lookingforendofgap)
			{
				bittowrite = false;
			}
			else
			{
				bittowrite = (writereg & (1 << writeregpos)) != 0;
				//if ((cached4025 & 0x10) == 0)
				if (writecomputecrc)
					crc = CCITT(crc, bittowrite ? 1 : 0);
				writeregpos++;
				if (writeregpos == 8)
				{
					writeregpos = 0;
					writereg = writereglatch;
					bytetransferflag = true;
					if ((cached4025 & 0x80) != 0)
						irq = true;
					Console.WriteLine("FDS: Write @{0} Reload {1:x2}", diskpos + 1, writereglatch);

					if ((cached4025 & 0x10) != 0)
					{
						Console.WriteLine("FDS: write clear CRC", readreg, diskpos);
						
						if (crc == 0)
						{
							cached4025 &= unchecked((byte)~0x10); // clear CRC reading
							Console.WriteLine("FDS: write CRC commit finished");
							// it seems that after a successful CRC, the writereglatch is reset to 0 value.  this is needed?
							writereglatch = 0;
						}

						Console.WriteLine("{0:x4}", crc);
						writereg = (byte)crc;
						Console.WriteLine("{0:x2}", writereg);
						crc >>= 8;
						Console.WriteLine("{0:x4}", crc);
						// loaded the first CRC byte to write, so stop computing CRC on data
						writecomputecrc = false;
					}

				}
			}

			var tmp = disk[diskpos >> 3];
			tmp &= unchecked((byte)~(1 << (diskpos & 7)));
			if (bittowrite)
				tmp |= (byte)(1 << (diskpos & 7));
			disk[diskpos >> 3] = tmp;
			diskpos++;
		}

		void MoveDummy()
		{
			diskpos++;
		}


	}
}
