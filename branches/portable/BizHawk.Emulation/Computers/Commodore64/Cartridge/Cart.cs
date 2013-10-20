using System;
using System.Collections.Generic;
using System.IO;

namespace BizHawk.Emulation.Computers.Commodore64.Cartridge
{
	// this is the base cartridge class

	public class Cart
	{
		// ---------------------------------

		static public Cart Load(byte[] crtFile)
		{
			Cart result = null;
			MemoryStream mem = new MemoryStream(crtFile);
			BinaryReader reader = new BinaryReader(mem);

			if (new string(reader.ReadChars(16)) == "C64 CARTRIDGE   ")
			{
				List<int> chipAddress = new List<int>();
				List<int> chipBank = new List<int>();
				List<byte[]> chipData = new List<byte[]>();
				List<int> chipType = new List<int>();

				int headerLength = ReadCRTInt(reader);
				int version = ReadCRTShort(reader);
				int mapper = ReadCRTShort(reader);
				bool exrom = (reader.ReadByte() != 0);
				bool game = (reader.ReadByte() != 0);

				// reserved
				reader.ReadBytes(6);

				// cartridge name
				reader.ReadBytes(0x20);

				// skip extra header bytes
				if (headerLength > 0x40)
				{
					reader.ReadBytes(headerLength - 0x40);
				}

				// read chips
				while (reader.PeekChar() >= 0)
				{
					if (new string(reader.ReadChars(4)) == "CHIP")
					{
						int chipLength = ReadCRTInt(reader);
						chipType.Add(ReadCRTShort(reader));
						chipBank.Add(ReadCRTShort(reader));
						chipAddress.Add(ReadCRTShort(reader));
						int chipDataLength = ReadCRTShort(reader);
						chipData.Add(reader.ReadBytes(chipDataLength));
						chipLength -= (chipDataLength + 0x10);
						if (chipLength > 0)
							reader.ReadBytes(chipLength);
					}
				}

				if (chipData.Count > 0)
				{
					switch (mapper)
					{
						case 0x0000:
							result = new Mapper0000(chipAddress, chipBank, chipData, game, exrom);
							break;
						case 0x0005:
							result = new Mapper0005(chipAddress, chipBank, chipData);
							break;
						case 0x000B:
							result = new Mapper000B(chipAddress, chipBank, chipData);
							break;
						case 0x000F:
							result = new Mapper000F(chipAddress, chipBank, chipData);
							break;
						case 0x0011:
							result = new Mapper0011(chipAddress, chipBank, chipData);
							break;
						case 0x0012:
							result = new Mapper0012(chipAddress, chipBank, chipData);
							break;
						case 0x0013:
							result = new Mapper0013(chipAddress, chipBank, chipData);
							break;
						case 0x0020:
							result = new Mapper0020(chipAddress, chipBank, chipData);
							break;
						default:
							throw new Exception("This cartridge file uses an unrecognized mapper: " + mapper);
					}
					result.HardReset();
				}
			}

			return result;
		}

		static private int ReadCRTShort(BinaryReader reader)
		{
			int result;
			result = (int)reader.ReadByte() << 8;
			result |= (int)reader.ReadByte();
			return result;
		}

		static private int ReadCRTInt(BinaryReader reader)
		{
			int result;
			result = (int)reader.ReadByte() << 24;
			result |= (int)reader.ReadByte() << 16;
			result |= (int)reader.ReadByte() << 8;
			result |= (int)reader.ReadByte();
			return result;
		}

		// ---------------------------------

		protected bool pinExRom;
		protected bool pinGame;
		protected bool pinIRQ;
		protected bool pinNMI;
		protected bool pinReset;
		protected bool validCartridge;

		public virtual void ExecutePhase1()
		{
		}

		public virtual void ExecutePhase2()
		{
		}

		public bool ExRom
		{
			get
			{
				return pinExRom;
			}
		}

		public bool Game
		{
			get
			{
				return pinGame;
			}
		}

		public virtual void HardReset()
		{
			pinIRQ = true;
			pinNMI = true;
			pinReset = true;
		}

		public bool IRQ
		{
			get
			{
				return pinIRQ;
			}
		}

		public bool NMI
		{
			get
			{
				return pinNMI;
			}
		}

		public virtual byte Peek8000(int addr)
		{
			return 0xFF;
		}

		public virtual byte PeekA000(int addr)
		{
			return 0xFF;
		}

		public virtual byte PeekDE00(int addr)
		{
			return 0xFF;
		}

		public virtual byte PeekDF00(int addr)
		{
			return 0xFF;
		}

		public virtual void Poke8000(int addr, byte val)
		{
		}

		public virtual void PokeA000(int addr, byte val)
		{
		}

		public virtual void PokeDE00(int addr, byte val)
		{
		}

		public virtual void PokeDF00(int addr, byte val)
		{
		}

		public virtual byte Read8000(int addr)
		{
			return 0xFF;
		}

		public virtual byte ReadA000(int addr)
		{
			return 0xFF;
		}

		public virtual byte ReadDE00(int addr)
		{
			return 0xFF;
		}

		public virtual byte ReadDF00(int addr)
		{
			return 0xFF;
		}

		public bool Reset
		{
			get
			{
				return pinReset;
			}
		}

		public virtual void SyncState(Serializer ser)
		{
            SaveState.SyncObject(ser, this);
		}

		public bool Valid
		{
			get
			{
				return validCartridge;
			}
		}

		public virtual void Write8000(int addr, byte val)
		{
		}

		public virtual void WriteA000(int addr, byte val)
		{
		}

		public virtual void WriteDE00(int addr, byte val)
		{
		}

		public virtual void WriteDF00(int addr, byte val)
		{
		}
	}
}
