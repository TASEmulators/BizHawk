using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	/* From Kevtris docs
	4A50 (no name)
	-----

	Upon review, I don't think this method is terribly workable on real
	hardware.  There's so many problems that I kinda gave up trying to
	count them all.  Seems that this is more of a "pony" method than something
	actually usable. ("pony" referring to "I want this, and that, and that, and
	a pony too!")

	One major problem is that it specifies that memory can be read and written
	to at the same address, but this is nearly impossible to detect on a 2600
	cartridge.  You'd almost have to try and figure out what OpCodes are being
	run, and what cycle it's on somehow, all just by watching address and
	data bus state.  Not very practical.

	The other problem is just the sheer volume of things it is supposed to do.
	There's just tons and tons of unnecessary things like attempting to detect
	BIT instructions, handling page wraps and other silly things.

	This all supposedly fit into a Xilinx XC9536XL but I am not sure how the
	chip could handle the RAM issue above at all.  It almost needs to see R/W
	and M2 (clock) to be able to properly do most of the things it's doing.
	*/

	/* From Stella docs
	Bankswitching method as defined/created by John Payson (aka SuperCat),
	documented at http://www.casperkitty.com/stella/cartfmt.htm.

	In this bankswitching scheme the 2600's 4K cartridge address space 
	is broken into four segments.  The first 2K segment accesses any 2K
	region of RAM, or of the first 32K of ROM.  The second 1.5K segment
	accesses the first 1.5K of any 2K region of RAM, or of the last 32K
	of ROM.  The 3rd 256 byte segment points to any 256 byte page of
	RAM or ROM.  The last 256 byte segment always points to the last 256
	bytes of ROM.
	*/

	internal sealed class m4A50 : MapperBase 
	{
		private byte[] _ram = new byte[32768];

		private byte _lastData = 0xFF;
		private ushort _lastAddress = 0xFFFF;

		private bool _isRomHigh = true;
		private bool _isRomLow = true;
		private bool _isRomMiddle = true;

		private int _sliceHigh;
		private int _sliceLow;
		private int _sliceMiddle;

		private byte[] _romImage;

		public m4A50(Atari2600 core) : base(core)
		{
		}

		private byte[] RomImage
		{
			get
			{
				if (_romImage == null)
				{
					// Copy the ROM image into my buffer
					// Supported file sizes are 32/64/128K, which are duplicated if necessary
					_romImage = new byte[131072];
					
					if (Core.Rom.Length < 65536)
					{
						for (int i = 0; i < 4; i++)
						{
							Array.Copy(Core.Rom, 0, _romImage, 32768 * i, 32768);
						}
					}
					else if (Core.Rom.Length < 131072)
					{
						for (int i = 0; i < 2; i++)
						{
							Array.Copy(Core.Rom, 0, _romImage, 65536 * i, 65536);
						}
					}
				}

				return _romImage;
			}
		}

		public override byte[] CartRam => _ram;

		public override void SyncState(Serializer ser)
		{
			ser.Sync("cartRam", ref _ram, false);

			ser.Sync("lastData", ref _lastData);
			ser.Sync("lastAddress", ref _lastAddress);

			ser.Sync("isRomHigh", ref _isRomHigh);
			ser.Sync("isRomLow", ref _isRomLow);
			ser.Sync("isRomMiddle", ref _isRomMiddle);

			ser.Sync("sliceHigh", ref _sliceHigh);
			ser.Sync("sliceLow", ref _sliceLow);
			ser.Sync("sliceMiddle", ref _sliceMiddle);

			base.SyncState(ser);
		}

		public override void HardReset()
		{
			_ram = new byte[32768];

			_lastData = 0xFF;
			_lastAddress = 0xFFFF;

			_isRomHigh = true;
			_isRomLow = true;
			_isRomMiddle = true;

			_sliceHigh = 0;
			_sliceLow = 0;
			_sliceMiddle = 0;
		}

		public override byte ReadMemory(ushort addr) => ReadMem(addr, false);

		public override byte PeekMemory(ushort addr) => ReadMem(addr, true);

		public override void WriteMemory(ushort addr, byte value)
			=> WriteMem(addr, value, poke: false);

		public override void PokeMemory(ushort addr, byte value)
			=> WriteMem(addr, value, poke: true);

		private byte ReadMem(ushort addr, bool peek)
		{
			byte val = 0;
			if (addr < 0x1000)
			{
				val = base.ReadMemory(addr);
				if (!peek)
				{
					CheckBankSwitch(addr, val);
				}
			}
			else if (addr < 0x1800) // 2K region from 0x1000 - 0x17ff
			{
				val = _isRomLow
					? RomImage[(addr & 0x7ff) + _sliceLow]
					: _ram[(addr & 0x7ff) + _sliceLow];
			}
			else if (addr < 0x1E00) // 1.5K region from 0x1800 - 0x1dff
			{
				val = _isRomMiddle
					? RomImage[(addr & 0x7ff) + _sliceMiddle + 0x10000]
					: _ram[(addr & 0x7ff) + _sliceMiddle];
			}
			else if (addr < 0x1F00) // 256B region from 0x1e00 - 0x1eff
			{
				val = _isRomHigh
					? RomImage[(addr & 0xff) + _sliceHigh + 0x10000]
					: _ram[(addr & 0xff) + _sliceHigh];
			}
			else if (addr < 0x2000)      // 256B region from 0x1f00 - 0x1fff
			{
				val = RomImage[(addr & 0xff) + (RomImage.Length - 256)];
				if ((_lastData & 0xe0) == 0x60 && (_lastAddress >= 0x1000
					|| _lastAddress < 0x200) && !peek)
				{
					_sliceHigh = (_sliceHigh & 0xf0ff)
						| ((addr & 0x8) << 8)
						| ((addr & 0x70) << 4);
				}
			}

			if (!peek)
			{
				_lastData = val;
				_lastAddress = (ushort)(addr & 0x1fff);
			}

			return val;
		}

		private void WriteMem(ushort addr, byte value, bool poke)
		{
			if (addr < 0x1000) // Hotspots below 0x1000
			{
				base.WriteMemory(addr, value);
				if (!poke)
				{
					CheckBankSwitch(addr, value);
				}
			}
			else if (addr < 0x1800) // 2K region at 0x1000 - 0x17ff
			{
				if (!_isRomLow)
				{
					_ram[(addr & 0x7ff) + _sliceLow] = value;
				}
			}
			else if (addr < 0x1E00)
			{
				if (!_isRomMiddle)
				{
					_ram[(addr & 0x7ff) + _sliceMiddle] = value;
				}
			}
			else if (addr < 0x1F00) // 256B region at 0x1e00 - 0x1eff
			{
				if (!_isRomHigh)
				{
					_ram[(addr & 0xff) + _sliceHigh] = value;
				}
			}
			else if (addr < 0x2000 && !poke) // 256B region at 0x1f00 - 0x1fff
			{
				if ((_lastData & 0xe0) == 0x60
					&& (_lastAddress >= 0x1000 || _lastAddress < 0x200))
				{
					_sliceHigh = (_sliceHigh & 0xf0ff)
						| ((addr & 0x8) << 8)
						| ((addr & 0x70) << 4);
				}
			}

			if (!poke)
			{
				_lastData = value;
				_lastAddress = (ushort)(addr & 0x1fff);
			}
		}

		private void CheckBankSwitch(ushort address, byte value)
		{
			if ((_lastData & 0xe0) == 0x60 // Switch lower/middle/upper bank
				&& (_lastAddress >= 0x1000 || _lastAddress < 0x200))
			{
				if ((address & 0x0f00) == 0x0c00) // Enable 256B of ROM at 0x1e00 - 0x1eff
				{
					_isRomHigh = true;
					_sliceHigh = (address & 0xff) << 8;
				}
				else if ((address & 0x0f00) == 0x0d00) // Enable 256B of RAM at 0x1e00 - 0x1eff
				{
					_isRomHigh = false;
					_sliceHigh = (address & 0x7f) << 8;
				}
				else if ((address & 0x0f40) == 0x0e00) // Enable 2K of ROM at 0x1000 - 0x17ff
				{
					_isRomLow = true;
					_sliceLow = (address & 0x1f) << 11;
				}
				else if ((address & 0x0f40) == 0x0e40) // Enable 2K of RAM at 0x1000 - 0x17ff
				{
					_isRomLow = false;
					_sliceLow = (address & 0xf) << 11;
				}
				else if ((address & 0x0f40) == 0x0f00) // Enable 1.5K of ROM at 0x1800 - 0x1dff
				{
					_isRomMiddle = true;
					_sliceMiddle = (address & 0x1f) << 11;
				}
				else if ((address & 0x0f50) == 0x0f40)  // Enable 1.5K of RAM at 0x1800 - 0x1dff
				{
					_isRomMiddle = false;
					_sliceMiddle = (address & 0xf) << 11;
				}
				else if ((address & 0x0f00) == 0x0400) // Toggle bit A11 of lower block address
				{
					_sliceLow ^= 0x800;
				}
				else if ((address & 0x0f00) == 0x0500) // Toggle bit A12 of lower block address
				{
					_sliceLow ^= 0x1000;
				}
				else if ((address & 0x0f00) == 0x0800) // Toggle bit A11 of middle block address
				{
					_sliceMiddle ^= 0x800;
				}
				else if ((address & 0x0f00) == 0x0900) // Toggle bit A12 of middle block address
				{
					_sliceMiddle ^= 0x1000;
				}
			}

			// Zero-page hotspots for upper page
			// 0xf4, 0xf6, 0xfc, 0xfe for ROM
			// 0xf5, 0xf7, 0xfd, 0xff for RAM
			// 0x74 - 0x7f (0x80 bytes lower)
			if ((address & 0xf75) == 0x74) // Enable 256B of ROM at 0x1e00 - 0x1eff
			{
				_isRomHigh = true;
				_sliceHigh = value << 8;
			}
			else if ((address & 0xf75) == 0x75) // Enable 256B of RAM at 0x1e00 - 0x1eff
			{
				_isRomHigh = false;
				_sliceHigh = (value & 0x7f) << 8;
			}

			// Zero-page hotspots for lower and middle blocks
			// 0xf8, 0xf9, 0xfa, 0xfb
			// 0x78, 0x79, 0x7a, 0x7b (0x80 bytes lower)
			else if ((address & 0xf7c) == 0x78)
			{
				if ((value & 0xf0) == 0) // Enable 2K of ROM at 0x1000 - 0x17ff
				{
					_isRomLow = true;
					_sliceLow = (value & 0xf) << 11;
				}
				else if ((value & 0xf0) == 0x40) // Enable 2K of RAM at 0x1000 - 0x17ff
				{
					_isRomLow = false;
					_sliceLow = (value & 0xf) << 11;
				}
				else if ((value & 0xf0) == 0x90) // Enable 1.5K of ROM at 0x1800 - 0x1dff
				{
					_isRomMiddle = true;
					_sliceMiddle = ((value & 0xf) | 0x10) << 11;
				}
				else if ((value & 0xf0) == 0xc0) // Enable 1.5K of RAM at 0x1800 - 0x1dff
				{
					_isRomMiddle = false;
					_sliceMiddle = (value & 0xf) << 11;
				}
			}
		}
	}
}
