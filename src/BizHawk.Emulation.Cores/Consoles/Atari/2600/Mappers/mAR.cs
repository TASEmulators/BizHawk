using System.Linq;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	/*
	This is the cartridge class for Arcadia (aka StarPath) Supercharger 
	games.  Christopher Salomon provided most of the technical details 
	used in creating this class.  A good description of the Supercharger
	is provided in the Cuttle Cart's manual.

	The Supercharger has four 2K banks.  There are three banks of RAM 
	and one bank of ROM.  All 6K of the RAM can be read and written.

	D7-D5 of this byte: Write Pulse Delay (n/a for emulator)

	D4-D0: RAM/ROM configuration:
	    $F000-F7FF    $F800-FFFF Address range that banks map into
	000wp     2            ROM
	001wp     0            ROM
	010wp     2            0      as used in Commie Mutants and many others
	011wp     0            2      as used in Suicide Mission
	100wp     2            ROM
	101wp     1            ROM
	110wp     2            1      as used in Killer Satellites
	111wp     1            2      as we use for 2k/4k ROM cloning

	w = Write Enable (1 = enabled; accesses to $F000-$F0FF cause writes
	 to happen.  0 = disabled, and the cart acts like ROM.)
	p = ROM Power (0 = enabled, 1 = off.)  Only power the ROM if you're
	 wanting to access the ROM for multi-loads.  Otherwise set to 1.
	*/
	internal sealed class mAR : MapperBase
	{
		private readonly byte[] _dummyRomCode =
		{
			0xa5, 0xfa, 0x85, 0x80, 0x4c, 0x18, 0xf8, 0xff,
			0xff, 0xff, 0x78, 0xd8, 0xa0, 0x00, 0xa2, 0x00,
			0x94, 0x00, 0xe8, 0xd0, 0xfb, 0x4c, 0x50, 0xf8,
			0xa2, 0x00, 0xbd, 0x06, 0xf0, 0xad, 0xf8, 0xff,
			0xa2, 0x00, 0xad, 0x00, 0xf0, 0xea, 0xbd, 0x00,
			0xf7, 0xca, 0xd0, 0xf6, 0x4c, 0x50, 0xf8, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xa2, 0x03, 0xbc, 0x22, 0xf9, 0x94, 0xfa, 0xca,
			0x10, 0xf8, 0xa0, 0x00, 0xa2, 0x28, 0x94, 0x04,
			0xca, 0x10, 0xfb, 0xa2, 0x1c, 0x94, 0x81, 0xca,
			0x10, 0xfb, 0xa9, 0xff, 0xc9, 0x00, 0xd0, 0x03,
			0x4c, 0x13, 0xf9, 0xa9, 0x00, 0x85, 0x1b, 0x85,
			0x1c, 0x85, 0x1d, 0x85, 0x1e, 0x85, 0x1f, 0x85,
			0x19, 0x85, 0x1a, 0x85, 0x08, 0x85, 0x01, 0xa9,
			0x10, 0x85, 0x21, 0x85, 0x02, 0xa2, 0x07, 0xca,
			0xca, 0xd0, 0xfd, 0xa9, 0x00, 0x85, 0x20, 0x85,
			0x10, 0x85, 0x11, 0x85, 0x02, 0x85, 0x2a, 0xa9,
			0x05, 0x85, 0x0a, 0xa9, 0xff, 0x85, 0x0d, 0x85,
			0x0e, 0x85, 0x0f, 0x85, 0x84, 0x85, 0x85, 0xa9,
			0xf0, 0x85, 0x83, 0xa9, 0x74, 0x85, 0x09, 0xa9,
			0x0c, 0x85, 0x15, 0xa9, 0x1f, 0x85, 0x17, 0x85,
			0x82, 0xa9, 0x07, 0x85, 0x19, 0xa2, 0x08, 0xa0,
			0x00, 0x85, 0x02, 0x88, 0xd0, 0xfb, 0x85, 0x02,
			0x85, 0x02, 0xa9, 0x02, 0x85, 0x02, 0x85, 0x00,
			0x85, 0x02, 0x85, 0x02, 0x85, 0x02, 0xa9, 0x00,
			0x85, 0x00, 0xca, 0x10, 0xe4, 0x06, 0x83, 0x66,
			0x84, 0x26, 0x85, 0xa5, 0x83, 0x85, 0x0d, 0xa5,
			0x84, 0x85, 0x0e, 0xa5, 0x85, 0x85, 0x0f, 0xa6,
			0x82, 0xca, 0x86, 0x82, 0x86, 0x17, 0xe0, 0x0a,
			0xd0, 0xc3, 0xa9, 0x02, 0x85, 0x01, 0xa2, 0x1c,
			0xa0, 0x00, 0x84, 0x19, 0x84, 0x09, 0x94, 0x81,
			0xca, 0x10, 0xfb, 0xa6, 0x80, 0xdd, 0x00, 0xf0,
			0xa9, 0x9a, 0xa2, 0xff, 0xa0, 0x00, 0x9a, 0x4c,
			0xfa, 0x00, 0xcd, 0xf8, 0xff, 0x4c
		};

		private readonly byte[] _defaultHeader =
		{
			0xac, 0xfa, 0x0f, 0x18, 0x62, 0x00, 0x24, 0x02,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0x00, 0x04, 0x08, 0x0c, 0x10, 0x14, 0x18, 0x1c,
			0x01, 0x05, 0x09, 0x0d, 0x11, 0x15, 0x19, 0x1d,
			0x02, 0x06, 0x0a, 0x0e, 0x12, 0x16, 0x1a, 0x1e,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00
		};

		private byte[] _superChargerImage = new byte[8192];
		private int[] _imageOffsets = new int[2];
		private bool _writePending;
		private int _distinctAccesses;
		private bool _writeEnabled;
		private byte _dataHoldRegister;
		private byte _numberOfLoadImages;
		private byte[] _loadedImages;
		private byte[] _header = new byte[256];
		private bool _powerIndicator; // Indicates if the ROM's power is on or off
		private int _powerRomCycle; // Indicates when the power was last turned on
		private int _size;
		private ulong _elapsedCycles;

		public mAR(Atari2600 core) : base(core)
		{
			InitializeSettings();
		}

		public override byte[] CartRam => _superChargerImage;

		public override void HardReset()
		{
			_superChargerImage = new byte[8192];
			_imageOffsets = new int[2];
			_writePending = false;
			_distinctAccesses = 0;

			_writeEnabled = false;
			_dataHoldRegister = 0;
			_numberOfLoadImages = 0;
			_loadedImages = null;
			
			_header = new byte[256];
			_powerIndicator = false;
			_powerRomCycle = 0;
			_size = 0;
			
			_elapsedCycles = 0;

			InitializeSettings();
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("superChargerImage", ref _superChargerImage, false);
			ser.Sync("imageOffsets", ref _imageOffsets, false);
			ser.Sync("writePending", ref _writePending);
			ser.Sync("distinctAccesses", ref _distinctAccesses);

			ser.Sync("writeEnabled", ref _writeEnabled);
			ser.Sync("dataHoldRegister", ref _dataHoldRegister);
			ser.Sync("numberOfLoadImages", ref _numberOfLoadImages);
			ser.Sync("loadedImages", ref _loadedImages, false);

			ser.Sync("header", ref _header, false);
			ser.Sync("powerIndicator", ref _powerIndicator);
			ser.Sync("powerRomCycle", ref _powerRomCycle);
			ser.Sync("size", ref _size);

			ser.Sync("elapsedCycles", ref _elapsedCycles);

			base.SyncState(ser);
		}

		public override void ClockCpu()
		{
			_elapsedCycles++;
		}

		public override byte ReadMemory(ushort addr) => ReadMem(addr, false);

		public override byte PeekMemory(ushort addr) => ReadMem(addr, true);

		public override void WriteMemory(ushort addr, byte value)
			=> WriteMem(addr, value, false);

		public override void PokeMemory(ushort addr, byte value)
			=> WriteMem(addr, value, true);

		private void InitializeSettings()
		{
			// TODO: clean this stuff up
			/*****************************************/
			int size = Core.Rom.Length;
			_size = Core.Rom.Length < 8448 ? 8448 : Core.Rom.Length; // 8448 or Rom size, whichever is bigger

			_numberOfLoadImages = (byte)(_size / 8448);

			// TODO: why are we making a redundant copy?
			_loadedImages = new byte[_size];
			for (int i = 0; i < size; i++)
			{
				_loadedImages[i] = Core.Rom[i];
			}

			if (size < 8448)
			{
				for (int i = size; i < _size; i++)
				{
					_loadedImages[i] = _defaultHeader[i];
				}
			}

			/*****************************************/

			InitializeRom();
			BankConfiguration(0);
		}

		private byte ReadMem(ushort addr, bool peek)
		{
			if (addr < 0x1000)
			{
				if (peek)
				{
					return base.PeekMemory(addr);
				}

				return base.ReadMemory(addr);
			}

			/*---------------------------*/

			if (addr == 0x1850 && _imageOffsets[1] == 3 << 11)
			{
				LoadIntoRam(Core.MemoryDomains["System Bus"]!.PeekByte(0x80)); // Get load that's being accessed (BIOS places load number at 0x80) // TODO: a better way to do this
				return _superChargerImage[(addr & 0x7FF) + _imageOffsets[1]];
			}

			if (_writePending // Cancel any pending write if more than 5 distinct accesses have occurred // TODO: Modify to handle when the distinct counter wraps around...
				&& Core.DistinctAccessCount > _distinctAccesses + 5)
			{
				_writePending = false;
			}

			/*---------------------------*/

			if (!((addr & 0x0F00) > 0) && (!_writeEnabled || !_writePending))
			{
				_dataHoldRegister = (byte)addr;
				_distinctAccesses = Core.DistinctAccessCount;
				_writePending = true;
			}
			else if ((addr & 0x1FFF) == 0x1FF8) // Is the bank configuration hotspot being accessed?
			{
				_writePending = false;
				BankConfiguration(_dataHoldRegister);
			}
			else if (_writeEnabled && _writePending && Core.DistinctAccessCount == _distinctAccesses + 5)
			{
				if ((addr & 0x800) == 0)
				{
					_superChargerImage[(addr & 0x07FF) + _imageOffsets[0]] = _dataHoldRegister;
				}
				else if (_imageOffsets[1] != (3 << 11)) // Don't poke Rom
				{
					_superChargerImage[(addr & 0x07FF) + _imageOffsets[1]] = _dataHoldRegister;
				}

				_writePending = false;
			}

			/*---------------------------*/

			return _superChargerImage[(addr & 0x07FF) + _imageOffsets[((addr & 0x800) > 0) ? 1 : 0]];
		}

		private void WriteMem(ushort addr, byte value, bool poke)
		{
			if (addr < 0x1000)
			{
				base.WriteMemory(addr, value);
				return;
			}

			if (!poke && _writePending && (Core.DistinctAccessCount > _distinctAccesses + 5))
			{
				_writePending = false;
			}

			// Is the data hold register being set?
			if (!poke && !((addr & 0x0F00) > 0) && (!_writeEnabled || !_writePending))
			{
				_dataHoldRegister = (byte)addr;
				_distinctAccesses = Core.DistinctAccessCount;
				_writePending = true;
			}

			// Is the bank configuration hotspot being accessed?
			else if (!poke && (addr & 0x1FFF) == 0x1FF8)
			{
				// Yes, so handle bank configuration
				_writePending = false;
				BankConfiguration(_dataHoldRegister);
			}

			// Handle poke if writing enabled
			else if (_writeEnabled && _writePending && Core.DistinctAccessCount == _distinctAccesses + 5)
			{
				if ((addr & 0x0800) == 0)
				{
					_superChargerImage[(addr & 0x07FF) + _imageOffsets[0]] = _dataHoldRegister;
				}
				else if (_imageOffsets[1] != (3 << 11))    // Can't poke to ROM
				{
					_superChargerImage[(addr & 0x07FF) + _imageOffsets[1]] = _dataHoldRegister;
				}

				_writePending = false;
			}
		}

		private void InitializeRom()
		{
			/* scrom.asm data borrowed from Stella:
			// Note that the following offsets depend on the 'scrom.asm' file
			// in src/emucore/misc.  If that file is ever recompiled (and its
			// contents placed in the ourDummyROMCode array), the offsets will
			// almost definitely change
			*/

			// The scrom.asm code checks a value at offset 109 as follows:
			//   0xFF -> do a complete jump over the SC BIOS progress bars code
			//   0x00 -> show SC BIOS progress bars as normal
			_dummyRomCode[109] = (byte)(Core.SyncSettings.FastScBios ? 0xFF : 0x00);

			// Stella does this, but randomness is bad for determinacy! Hopefully we don't really need it
			// ourDummyROMCode[281] = mySystem->randGenerator().next();

			// Initialize ROM with illegal 6502 opcode that causes a real 6502 to jam
			for (int i = 0; i < 2048; i++)
			{
				_superChargerImage[(3 << 11) + i] = 0x02;
			}

			// Copy the "dummy" Supercharger BIOS code into the ROM area
			for (int i = 0; i < _dummyRomCode.Length; i++)
			{
				_superChargerImage[(3 << 11) + i] = _dummyRomCode[i];
			}

			// Finally set 6502 vectors to point to initial load code at 0xF80A of BIOS
			_superChargerImage[(3 << 11) + 2044] = 0x0A;
			_superChargerImage[(3 << 11) + 2045] = 0xF8;
			_superChargerImage[(3 << 11) + 2046] = 0x0A;
			_superChargerImage[(3 << 11) + 2047] = 0xF8;
		}

		private void BankConfiguration(byte configuration)
		{
			// D7-D5 of this byte: Write Pulse Delay (n/a for emulator)
			//
			// D4-D0: RAM/ROM configuration:
			//       $F000-F7FF    $F800-FFFF Address range that banks map into
			//  000wp     2            ROM
			//  001wp     0            ROM
			//  010wp     2            0      as used in Commie Mutants and many others
			//  011wp     0            2      as used in Suicide Mission
			//  100wp     2            ROM
			//  101wp     1            ROM
			//  110wp     2            1      as used in Killer Satellites
			//  111wp     1            2      as we use for 2k/4k ROM cloning
			// 
			//  w = Write Enable (1 = enabled; accesses to $F000-$F0FF cause writes
			//    to happen.  0 = disabled, and the cart acts like ROM.)
			//  p = ROM Power (0 = enabled, 1 = off.)  Only power the ROM if you're
			//    wanting to access the ROM for multi-loads.  Otherwise set to 1.

			//_bank2k = configuration & 0x1F;  // remember for the bank() method
			_powerIndicator = !((configuration & 0x01) > 0);
			if (_powerIndicator)
			{
				_powerRomCycle = (int)_elapsedCycles;
			}

			_writeEnabled = (configuration & 0x02) > 0;

			switch ((configuration >> 2) & 0x07)
			{
				case 0x00:
					_imageOffsets[0] = 2 << 11;
					_imageOffsets[1] = 3 << 11;
					break;
				case 0x01:
					_imageOffsets[0] = 0;
					_imageOffsets[1] = 3 << 11;
					break;
				case 0x02:
					_imageOffsets[0] = 2 << 11;
					_imageOffsets[1] = 0;
					break;
				case 0x03:
					_imageOffsets[0] = 0;
					_imageOffsets[1] = 2 << 11;
					break;
				case 0x04:
					_imageOffsets[0] = 2 << 11;
					_imageOffsets[1] = 3 << 11;
					break;
				case 0x05:
					_imageOffsets[0] = 1 << 11;
					_imageOffsets[1] = 3 << 11;
					break;
				case 0x06:
					_imageOffsets[0] = 2 << 11;
					_imageOffsets[1] = 1 << 11;
					break;
				case 0x07:
					_imageOffsets[0] = 1 << 11;
					_imageOffsets[1] = 2 << 11;
					break;
			}
		}

		private void LoadIntoRam(byte load)
		{
			ushort image;

			for (image = 0; image < _numberOfLoadImages; image++)
			{
				if (_loadedImages[(image * 8448) + 8192 + 5] == load)
				{
					for (int i = 0; i < 256; i++)
					{
						_header[i] = _loadedImages[(image * 8448) + 8192 + i];
					}

					if (Checksum(_header.Take(8).ToArray()) != 0x55)
					{
						Console.WriteLine("WARNING: The Supercharger header checksum is invalid...");
					}

					// TODO: verify the load's header

					// Load all of the pages from the load
					bool invalidPageChecksumSeen = false;
					for (int j = 0; j < _header[3]; j++)
					{
						int bank = _header[16 + j] & 0x03;
						int page = (_header[16 + j] >> 2) & 0x07;
						var src = _loadedImages
							.Skip((image * 8448) + (j * 256))
							.Take(256)
							.ToArray();

						byte sum = (byte)(Checksum(src) + _header[16 + j] + _header[64 + j]);

						if (!invalidPageChecksumSeen && (sum != 0x55))
						{
							Console.WriteLine("WARNING: Some Supercharger page checksums are invalid...");
							invalidPageChecksumSeen = true;
						}

						if (bank < 3)
						{
							for (int k = 0; k < src.Length; k++)
							{
								_superChargerImage[(bank * 2048) + (page * 256) + k] = src[k];
							}
						}
					}

					// TODO: is this the correct Write to do?
					base.WriteMemory(0xFE, _header[0]);
					base.WriteMemory(0xFF, _header[1]);
					base.WriteMemory(0x80, _header[2]);
				}
			}
		}

		private byte Checksum(byte[] s)
		{
			byte sum = 0;

			for (int i = 0; i < s.Length; i++)
			{
				sum += s[i];
			}

			return sum;
		}
	}
}
