using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	/// <summary>
	/// describes a 24C01 or 24C02 as connected to a BANDAI-FCG
	/// </summary>
	/// <remarks>
	/// <see href="http://pdf1.alldatasheet.com/datasheet-pdf/view/56094/ATMEL/24C01.html">24C01</see><br/>
	/// <see href="http://www.atmel.com/Images/doc0180.pdf">24C02 and others</see>
	/// </remarks>
	public sealed class SEEPROM
	{
		/// <summary>
		/// true if 256byte
		/// </summary>
		private readonly bool Big;

		private byte[] rom;
		/// <summary>aux circuitry? D7 of data byte</summary>
		private bool OutEnable = false;
		/// <summary>asserted by master</summary>
		private bool SCK = false;
		/// <summary>asserted by master</summary>
		private bool SDA = false;
		/// <summary>true if the SEEPROM is trying to pull down the SDA line</summary>
		private bool PullDown = false;


		/// <summary>number of bits left to send\recv of current byte</summary>
		private int BitsLeft;
		/// <summary>current data byte in progress</summary>
		private byte Data;
		/// <summary>current chip addr</summary>
		private byte Addr;


		private enum EState
		{
			Off, Select, Ignore, Address, Read, Write
		}

		private EState State;

		/// <summary>
		/// called on the 9th bit of a write
		/// </summary>
		private void ClockByteWrite()
		{
			if (State == EState.Write)
			{
				PullDown = true; // ack
				// commit
				Console.WriteLine("{1:x2} => rom[{0:x2}]", Addr, Data);
				rom[Addr] = Data;
				Addr++;
				Addr &= (byte)(rom.Length - 1);
				// next byte
				BitsLeft = 8;
			}
			else if (State == EState.Select)
			{
				if (Big) // 24C02: select contains a device selector, plus mode
				{
					Console.WriteLine("256B Select: {0:x2}", Data);

					// device selector byte should be 1010 000x
					// x = 0: write.  x = 1: read

					if ((Data & 0xfe) != 0xa0)
					{
						Console.WriteLine("STATE: IGNORE");
						State = EState.Ignore;
					}
					else
					{
						if (Data.Bit(0))
						{
							PullDown = true; // ack
							Console.WriteLine("STATE: READ");
							State = EState.Read;
							BitsLeft = 8;
							Data = rom[Addr];
						}
						else
						{
							PullDown = true; // ack
							Console.WriteLine("STATE: ADDRESS");
							State = EState.Address;
							BitsLeft = 8;
						}
					}
				}
				else // 24C01: select contains a 7 bit address, plus mode
				{
					Addr = (byte)(Data >> 1);
					Console.WriteLine("128B Addr: {0:x2}", Addr);
					if (Data.Bit(0))
					{
						PullDown = true; // ack
						Console.WriteLine("STATE: READ");
						State = EState.Read;
						BitsLeft = 8;
						Data = rom[Addr];
					}
					else
					{
						PullDown = true; // ack
						Console.WriteLine("STATE: WRITE");
						State = EState.Write;
						BitsLeft = 8;
					}
				}
			}
			else if (State == EState.Address) // (Only on 24C02): a byte of address
			{
				Addr = Data;
				Console.WriteLine("256B Addr: {0:x2}", Data);
				PullDown = true; // ack
				Console.WriteLine("STATE: WRITE"); // to random read, the device will be set to read mode right after this
				State = EState.Write;
				BitsLeft = 8;
			}
		}

		/// <summary>
		/// called on rising edge of SCK.  output bit, if any, can be set by PullDown
		/// </summary>
		/// <param name="bit">input bit</param>
		private void ClockBit(bool bit)
		{
			switch (State)
			{
				case EState.Off:
				case EState.Ignore:
					break;
				case EState.Select:
				case EState.Address:
				case EState.Write:
					if (BitsLeft > 0)
					{
						BitsLeft--;
						if (bit)
							Data |= (byte)(1 << BitsLeft);
						else
							Data &= (byte)~(1 << BitsLeft);
					}
					else // "9th" bit
						ClockByteWrite();
					break;
				case EState.Read:
					if (BitsLeft > 0)
					{
						BitsLeft--;
						PullDown = !Data.Bit(BitsLeft);
					}
					else // 0 bits left: master acknowledges, and prepare another byte
					{
						if (bit)
						{
							// master didn't acknowledge.  what to do?
						}
						Console.WriteLine("{1:x2} <= rom[{0:x2}]", Addr, Data); 
						Addr++;
						Addr &= (byte)(rom.Length - 1);
						Data = rom[Addr];
						BitsLeft = 8;
					}
					break;
			}
		}

		private void ClockStart()
		{
			State = EState.Select;
			BitsLeft = 8;
			Console.WriteLine("STATE: SELECT");
		}

		private void ClockStop()
		{
			State = EState.Off;
			Console.WriteLine("STATE: OFF");
			PullDown = false;
		}

		
		public void WriteByte(byte val)
		{
			OutEnable = val.Bit(7);
			bool newSDA = val.Bit(6);
			bool newSCK = val.Bit(5);

			if (!newSCK) // falling or inactive SCK: cancel any active ack / readback
			{
				PullDown = false;
			}
			else
			{
				if (!SCK) // rising edge
				{
					ClockBit(newSDA);
				}
				else // clock stays high; look for changes in SDA
				{
					if (!SDA && newSDA)
					{
						ClockStop();
					}
					else if (SDA && !newSDA)
					{
						ClockStart();
					}
				}

			}
			SCK = newSCK;
			SDA = newSDA;
		}

		/// <summary>
		/// read a bit back from eprom, might be mapped in 6000:7fff
		/// </summary>
		/// <param name="deadbit">bit from NES.DB</param>
		public bool ReadBit(bool deadbit)
		{
			if (!OutEnable)
				return deadbit;
			if (!SDA)
				return false;
			return !PullDown;
		}

		public byte[] GetSaveRAM() { return rom; }

		/// <param name="Big">256 byte instead of 128 byte</param>
		public SEEPROM(bool Big)
		{
			rom = new byte[Big ? 256 : 128];
			this.Big = Big;
		}

		public void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(SEEPROM));
			ser.Sync(nameof(rom), ref rom, false);
			ser.Sync(nameof(OutEnable), ref OutEnable);
			ser.Sync(nameof(SCK), ref SCK);
			ser.Sync(nameof(SDA), ref SDA);
			ser.Sync(nameof(PullDown), ref PullDown);
			ser.Sync(nameof(BitsLeft), ref BitsLeft);
			ser.Sync(nameof(Data), ref Data);
			ser.Sync(nameof(Addr), ref Addr);
			int tmp = (int)State;
			ser.Sync(nameof(State), ref tmp);
			State = (EState)tmp;
			ser.EndSection();
		}
	}
}
