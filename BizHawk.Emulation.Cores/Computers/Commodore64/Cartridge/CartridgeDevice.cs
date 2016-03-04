using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Cartridge
{
	public abstract partial class CartridgeDevice
	{
		public static CartridgeDevice Load(byte[] crtFile)
		{
			var mem = new MemoryStream(crtFile);
			var reader = new BinaryReader(mem);

		    if (new string(reader.ReadChars(16)) != "C64 CARTRIDGE   ")
		    {
		        return null;
		    }

		    var chipAddress = new List<int>();
		    var chipBank = new List<int>();
		    var chipData = new List<int[]>();
		    var chipType = new List<int>();

		    var headerLength = ReadCRTInt(reader);
		    var version = ReadCRTShort(reader);
		    var mapper = ReadCRTShort(reader);
		    var exrom = reader.ReadByte() != 0;
		    var game = reader.ReadByte() != 0;

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
		        if (new string(reader.ReadChars(4)) != "CHIP")
		        {
		            break;
		        }

		        var chipLength = ReadCRTInt(reader);
		        chipType.Add(ReadCRTShort(reader));
		        chipBank.Add(ReadCRTShort(reader));
		        chipAddress.Add(ReadCRTShort(reader));
		        var chipDataLength = ReadCRTShort(reader);
		        chipData.Add(reader.ReadBytes(chipDataLength).Select(x => (int)x).ToArray());
		        chipLength -= chipDataLength + 0x10;
		        if (chipLength > 0)
		            reader.ReadBytes(chipLength);
		    }

		    if (chipData.Count <= 0)
		    {
		        return null;
		    }

            CartridgeDevice result;
            switch (mapper)
		    {
		        case 0x0000:
		            result = new Mapper0000(chipAddress, chipData, game, exrom);
		            break;
		        case 0x0005:
		            result = new Mapper0005(chipAddress, chipBank, chipData);
		            break;
                case 0x000A:
                    result = new Mapper000A(chipAddress, chipBank, chipData);
                    break;
                case 0x000B:
		            result = new Mapper000B(chipAddress, chipData);
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

		    return result;
		}

		private static int ReadCRTShort(BinaryReader reader)
		{
		    return (reader.ReadByte() << 8) |
                reader.ReadByte();
		}

		private static int ReadCRTInt(BinaryReader reader)
		{
			return (reader.ReadByte() << 24) |
                (reader.ReadByte() << 16) |
                (reader.ReadByte() << 8) |
                reader.ReadByte();
		}

        [SaveState.SaveWithName("ExRom")]
		protected bool pinExRom;
        [SaveState.SaveWithName("Game")]
        protected bool pinGame;
        [SaveState.SaveWithName("IRQ")]
        protected bool pinIRQ;
        [SaveState.SaveWithName("NMI")]
        protected bool pinNMI;
        [SaveState.SaveWithName("Reset")]
        protected bool pinReset;
        [SaveState.DoNotSave]
        protected bool validCartridge;

		public virtual void ExecutePhase()
		{
		}

        [SaveState.DoNotSave]
		public bool ExRom
		{
			get
			{
				return pinExRom;
			}
		}

        [SaveState.DoNotSave]
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

        [SaveState.DoNotSave]
        public bool IRQ
		{
			get
			{
				return pinIRQ;
			}
		}

        [SaveState.DoNotSave]
        public bool NMI
		{
			get
			{
				return pinNMI;
			}
		}

		public virtual int Peek8000(int addr)
		{
			return 0xFF;
		}

		public virtual int PeekA000(int addr)
		{
			return 0xFF;
		}

		public virtual int PeekDE00(int addr)
		{
			return 0xFF;
		}

		public virtual int PeekDF00(int addr)
		{
			return 0xFF;
		}

		public virtual void Poke8000(int addr, int val)
		{
		}

		public virtual void PokeA000(int addr, int val)
		{
		}

		public virtual void PokeDE00(int addr, int val)
		{
		}

		public virtual void PokeDF00(int addr, int val)
		{
		}

		public virtual int Read8000(int addr)
		{
			return 0xFF;
		}

		public virtual int ReadA000(int addr)
		{
			return 0xFF;
		}

		public virtual int ReadDE00(int addr)
		{
			return 0xFF;
		}

		public virtual int ReadDF00(int addr)
		{
			return 0xFF;
		}

        [SaveState.DoNotSave]
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

        [SaveState.DoNotSave]
        public bool Valid
		{
			get
			{
				return validCartridge;
			}
		}

		public virtual void Write8000(int addr, int val)
		{
		}

		public virtual void WriteA000(int addr, int val)
		{
		}

		public virtual void WriteDE00(int addr, int val)
		{
		}

		public virtual void WriteDF00(int addr, int val)
		{
		}
	}
}
