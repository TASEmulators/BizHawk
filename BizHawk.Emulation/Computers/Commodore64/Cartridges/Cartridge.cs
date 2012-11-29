using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Cartridges
{
	// this is the base cartridge class

	public class Cartridge
	{
		// ---------------------------------

		static public Cartridge Load(byte[] crtFile)
		{
			Cartridge result = null;
			MemoryStream mem = new MemoryStream(crtFile);
			BinaryReader reader = new BinaryReader(mem);

			if (new string(reader.ReadChars(16)) == "C64 CARTRIDGE   ")
			{
				List<uint> chipAddress = new List<uint>();
				List<uint> chipBank = new List<uint>();
				List<byte[]> chipData = new List<byte[]>();
				List<uint> chipType = new List<uint>();

				uint headerLength = ReadCRTInt(reader);
				uint version = ReadCRTShort(reader);
				uint mapper = ReadCRTShort(reader);
				bool exrom = (reader.ReadByte() != 0);
				bool game = (reader.ReadByte() != 0);

				// reserved
				reader.ReadBytes(6);

				// cartridge name
				reader.ReadBytes(0x20);

				// skip extra header bytes
				if (headerLength > 0x40)
				{
					reader.ReadBytes((int)headerLength - 0x40);
				}

				// read chips
				while (reader.PeekChar() >= 0)
				{
					if (new string(reader.ReadChars(4)) == "CHIP")
					{
						uint chipLength = ReadCRTInt(reader);
						chipType.Add(ReadCRTShort(reader));
						chipBank.Add(ReadCRTShort(reader));
						chipAddress.Add(ReadCRTShort(reader));
						uint chipDataLength = ReadCRTShort(reader);
						chipData.Add(reader.ReadBytes((int)chipDataLength));
						chipLength -= (chipDataLength + 0x10);
						if (chipLength > 0)
							reader.ReadBytes((int)chipLength);
					}
				}

				if (chipData.Count > 0)
				{
					switch (mapper)
					{
						case 0x0000:
							result = new Mapper0000(chipData[0], exrom, game);
							break;
						case 0x0005:
							result = new Mapper0005(chipAddress, chipBank, chipData);
							break;
						case 0x0012:
							result = new Mapper0012(chipAddress, chipBank, chipData);
							break;
						default:
							break;
					}
				}
			}

			return result;
		}

		static private uint ReadCRTShort(BinaryReader reader)
		{
			uint result;
			result = (uint)reader.ReadByte() << 8;
			result |= (uint)reader.ReadByte();
			return result;
		}

		static private uint ReadCRTInt(BinaryReader reader)
		{
			uint result;
			result = (uint)reader.ReadByte() << 24;
			result |= (uint)reader.ReadByte() << 16;
			result |= (uint)reader.ReadByte() << 8;
			result |= (uint)reader.ReadByte();
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

		public virtual byte Read8000(ushort addr)
		{
			return 0xFF;
		}

		public virtual byte ReadA000(ushort addr)
		{
			return 0xFF;
		}

		public virtual byte ReadDE00(ushort addr)
		{
			return 0xFF;
		}

		public virtual byte ReadDF00(ushort addr)
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

		public bool Valid
		{
			get
			{
				return validCartridge;
			}
		}

		public virtual void Write8000(ushort addr, byte val)
		{
		}

		public virtual void WriteA000(ushort addr, byte val)
		{
		}

		public virtual void WriteDE00(ushort addr, byte val)
		{
		}

		public virtual void WriteDF00(ushort addr, byte val)
		{
		}
	}
}
