using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Cartridge
{
	public abstract class CartridgeDevice : IDriveLight
	{
		public Func<int> ReadOpenBus;

		public static CartridgeDevice Load(byte[] crtFile)
		{
			using MemoryStream mem = new(crtFile);
			BinaryReader reader = new(mem);

			if (new string(reader.ReadChars(16)) != "C64 CARTRIDGE   ")
			{
				return null;
			}

			List<int> chipAddress = new();
			List<int> chipBank = new();
			List<int[]> chipData = new();
			List<int> chipType = new();

			int headerLength = ReadCRTInt(reader);
			int version = ReadCRTShort(reader);
			int mapper = ReadCRTShort(reader);
			bool exrom = reader.ReadByte() != 0;
			bool game = reader.ReadByte() != 0;

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

				int chipLength = ReadCRTInt(reader);
				chipType.Add(ReadCRTShort(reader));
				chipBank.Add(ReadCRTShort(reader));
				chipAddress.Add(ReadCRTShort(reader));
				int chipDataLength = ReadCRTShort(reader);
				chipData.Add(reader.ReadBytes(chipDataLength).Select(x => (int)x).ToArray());
				chipLength -= chipDataLength + 0x10;
				if (chipLength > 0)
				{
					reader.ReadBytes(chipLength);
				}
			}

			if (chipData.Count <= 0)
			{
				return null;
			}

			CartridgeDevice result = mapper switch
			{
				// Standard Cartridge
				0x0000 => new Mapper0000(chipAddress, chipData, game, exrom),
				// Action Replay (4.2 and up)
				0x0001 => new Mapper0001(chipAddress, chipBank, chipData),
				// Ocean
				0x0005 => new Mapper0005(chipAddress, chipBank, chipData),
				// Fun Play
				0x0007 => new Mapper0007(chipData, game, exrom),
				// SuperGame
				0x0008 => new Mapper0008(chipData),
				// Epyx FastLoad
				0x000A => new Mapper000A(chipData),
				// Westermann Learning
				0x000B => new Mapper000B(chipAddress, chipData),
				// C64 Game System / System 3
				0x000F => new Mapper000F(chipAddress, chipBank, chipData),
				// Dinamic
				0x0011 => new Mapper0011(chipAddress, chipBank, chipData),
				// Zaxxon / Super Zaxxon
				0x0012 => new Mapper0012(chipAddress, chipBank, chipData),
				// Domark
				0x0013 => new Mapper0013(chipAddress, chipBank, chipData),
				// EasyFlash
				0x0020 => new Mapper0020(chipAddress, chipBank, chipData),
				// Prophet 64
				0x002B => new Mapper002B(chipAddress, chipBank, chipData),
				_ => throw new Exception("This cartridge file uses an unrecognized mapper: " + mapper),
			};
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

		protected bool pinExRom;

		protected bool pinGame;

		protected bool pinIRQ;

		protected bool pinNMI;

		protected bool pinReset;

		protected bool validCartridge;

		public virtual void ExecutePhase()
		{
		}

		public bool ExRom => pinExRom;

		public bool Game => pinGame;

		public virtual void HardReset()
		{
			pinIRQ = true;
			pinNMI = true;
			pinReset = true;
		}

		public bool IRQ => pinIRQ;

		public bool NMI => pinNMI;

		public virtual int Peek8000(int addr) => ReadOpenBus();

		public virtual int PeekA000(int addr) => ReadOpenBus();

		public virtual int PeekDE00(int addr) => ReadOpenBus();

		public virtual int PeekDF00(int addr) => ReadOpenBus();

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

		public virtual int Read8000(int addr) => ReadOpenBus();

		public virtual int ReadA000(int addr) => ReadOpenBus();

		public virtual int ReadDE00(int addr) => ReadOpenBus();

		public virtual int ReadDF00(int addr) => ReadOpenBus();

		public bool Reset => pinReset;

		protected abstract void SyncStateInternal(Serializer ser);

		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(pinExRom), ref pinExRom);
			ser.Sync(nameof(pinGame), ref pinGame);
			ser.Sync(nameof(pinIRQ), ref pinIRQ);
			ser.Sync(nameof(pinNMI), ref pinNMI);
			ser.Sync(nameof(pinReset), ref pinReset);

			ser.Sync(nameof(_driveLightEnabled), ref _driveLightEnabled);
			ser.Sync(nameof(_driveLightOn), ref _driveLightOn);

			SyncStateInternal(ser);
		}

		public bool Valid => validCartridge;

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

		private bool _driveLightEnabled;
		private bool _driveLightOn;

		public bool DriveLightEnabled
		{
			get => _driveLightEnabled;
			protected set => _driveLightEnabled = value;
		}

		public bool DriveLightOn
		{
			get => _driveLightOn;
			protected set => _driveLightOn = value;
		}
	}
}
