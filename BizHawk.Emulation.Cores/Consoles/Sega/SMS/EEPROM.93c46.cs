using System;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	class EEPROM93c46
	{
		enum EEPROMWriteMode
		{
			Instruction,
			WriteData,
			WriteAll,
			Read
		}

		enum EEPROMReadMode
		{
			ReadNew,
			ReadOld,
			Hold
		}

		[Flags]
		enum EEPROMFlags : byte
		{
			Ready = 1,
			Clock = 2,
			ChipSelect = 4
		}

		ushort Address = 0;
		ushort Value = 0;
		int BitsWritten = 0;
		int BitsRead = 0;
		bool WriteEnable = false;
		EEPROMWriteMode WriteMode = EEPROMWriteMode.Instruction;
		EEPROMReadMode ReadMode = EEPROMReadMode.Hold;
		EEPROMFlags Flags = 0;
		
		public byte Read(byte[] saveRAM)
		{
			switch (ReadMode)
			{
				case EEPROMReadMode.ReadNew:
					// A new value clocking out

					ReadMode = EEPROMReadMode.ReadOld;
					byte ret = Read(saveRAM);

					if (++BitsRead == 8)
					{
						// Increment address
						BitsRead = 0;
						++Address;

						if(Address % 2 == 0)
						{
							WriteMode = EEPROMWriteMode.Instruction;
							ReadMode = EEPROMReadMode.Hold;
							BitsWritten = 0;
							Value = 0;
						}
					}

					return ret;
				case EEPROMReadMode.ReadOld:
					// repeat old value

					byte bit = (byte)((saveRAM[Address % saveRAM.Length] >> (7 - BitsRead)) & 1);
										
					return (byte)((byte)(Flags | EEPROMFlags.Clock) | bit);
				default:
					// ready/busy flag is always ready in this emulation
					return (byte)(Flags | EEPROMFlags.Clock | EEPROMFlags.Ready);
			}
		}

		public void Write(byte bit, byte[] saveRAM)
		{
			// new instruction?
			if ((bit & 4) == 0)
			{
				WriteMode = EEPROMWriteMode.Instruction;
				ReadMode = EEPROMReadMode.Hold;
				BitsWritten = 0;
				Value = 0;

				Flags = (EEPROMFlags)bit & ~EEPROMFlags.Ready;
				return;
			}

			// clock low to high?
			if ((bit & (byte)EEPROMFlags.Clock) != 0 && (Flags & EEPROMFlags.Clock) == 0)
			{
				// all modes shift in a larger value
				Value = (ushort)((Value << 1) | (bit & 1));
				++BitsWritten;

				switch (WriteMode)
				{
					case EEPROMWriteMode.Instruction:
						// Process opcode including start bit

						// check start bit
						if ((Value & 0x100) == 0)
							return;

						byte op = (byte)Value;
						Value = 0;
						BitsWritten = 0;

						switch (op & 0xC0)
						{
							case 0x00:
								// non-addressed commands
								switch (op & 0xF0)
								{
									case 0x00:
										// EWDS: write disable
										WriteEnable = false;
										return;
									case 0x10:
										// WRAL: write to all addresses (silly)
										WriteMode = EEPROMWriteMode.WriteAll;
										ReadMode = EEPROMReadMode.Hold;
										return;
									case 0x20:
										// ERAL: erase all addresses
										if (WriteEnable)
										{
											for (int i = 0; i < saveRAM.Length; ++i)
											{
												saveRAM[i] = 0xFF;
											}
										}
										ReadMode = EEPROMReadMode.Hold;
										return;
									case 0x30:
										// EWEN: write enable
										WriteEnable = true;
										return;
									default:
										// impossible
										return;
								}
							case 0x40:
								// WRITE
								Address = (ushort)((op & 0x3F) << 1);
								WriteMode = EEPROMWriteMode.WriteData;
								ReadMode = EEPROMReadMode.Hold;
								return;
							case 0x80:
								// READ
								Address = (ushort)((op & 0x3F) << 1);
								ReadMode = EEPROMReadMode.Hold;
								WriteMode = EEPROMWriteMode.Read;
								BitsRead = 0;
								return;
							case 0xC0:
								// ERASE
								Address = (ushort)((op & 0x3F) << 1);
								if (WriteEnable)
								{
									saveRAM[Address % saveRAM.Length] = 0xFF;
									saveRAM[(Address + 1) % saveRAM.Length] = 0xFF;
								}
								ReadMode = EEPROMReadMode.Hold;
								return;
							default:
								// impossible
								return;
						}
					case EEPROMWriteMode.WriteData:
						// Write bits

						if (BitsWritten < 16)
							return;

						if (WriteEnable)
						{
							saveRAM[Address % saveRAM.Length] = (byte)(Value >> 8);
							saveRAM[(Address + 1) % saveRAM.Length] = (byte)Value;
						}
						WriteMode = EEPROMWriteMode.Instruction;

						Value = 0;
						BitsWritten = 0;
						return;
					case EEPROMWriteMode.WriteAll:
						// write to ALL addresses

						if (BitsWritten < 16)
							return;

						Value = 0;
						BitsWritten = 0;

						if (WriteEnable)
						{
							for (int i = 0; i < saveRAM.Length; i += 2)
							{
								saveRAM[i % saveRAM.Length] = (byte)Value;
								saveRAM[(i + 1) % saveRAM.Length] = (byte)(Value >> 8);
							}
						}
						WriteMode = EEPROMWriteMode.Instruction;
						return;
					case EEPROMWriteMode.Read:
						// Clock a new value out
						ReadMode = EEPROMReadMode.ReadNew;

						return;
				}
			}

			Flags = (EEPROMFlags)bit & ~EEPROMFlags.Ready;
		}

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("93c46");
			ser.Sync("Address", ref Address);
			ser.Sync("Value", ref Value);
			ser.Sync("BitsWritten", ref BitsWritten);
			ser.Sync("BitsRead", ref BitsRead);
			ser.Sync("WriteEnable", ref WriteEnable);
			ser.SyncEnum("WriteMode", ref WriteMode);
			ser.SyncEnum("ReadMode", ref ReadMode);
			ser.SyncEnum("Flags", ref Flags);
			ser.EndSection();
		}
	}
}
