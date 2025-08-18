﻿using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	/*
	E7 (M-Network)
	-----

	M-network wanted something of their own too, so they came up with what they called
	"Big Game" (this was printed on the prototype ASICs on the prototype carts).  It
	can handle up to 16K of ROM and 2K of RAM.

	1000-17FF is selectable
	1800-19FF is RAM
	1A00-1FFF is fixed to the last 1.5K of ROM

	Accessing 1FE0 through 1FE6 selects bank 0 through bank 6 of the ROM into 1000-17FF.
	Accessing 1FE7 enables 1K of the 2K RAM, instead.

	When the RAM is enabled, this 1K appears at 1000-17FF.  1000-13FF is the write port, 1400-17FF
	is the read port.

	1800-19FF also holds RAM. 1800-18FF is the write port, 1900-19FF is the read port.
	Only 256 bytes of RAM is accessible at time, but there are four different 256 byte
	banks making a total of 1K accessible here.

	Accessing 1FE8 through 1FEB select which 256 byte bank shows up.
	*/
	internal sealed class mE7 : MapperBase
	{
		private const int RamBank1Offset = 1024;
		private int _romBank1K;
		private int _ramBank1Toggle;
		private byte[] _ram = new byte[2048];

		private bool _enableRam0;

		public mE7(Atari2600 core) : base(core)
		{
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("toggle", ref _romBank1K);
			ser.Sync("ram", ref _ram, false);
			ser.Sync("EnableRam0", ref _enableRam0);
			ser.Sync("rambank1_toggle", ref _ramBank1Toggle);
		}

		public override void HardReset()
		{
			_romBank1K = 0;
			_ramBank1Toggle = 0;
			_ram = new byte[2048];
			_enableRam0 = false;
		}

		public override byte[] CartRam => _ram;

		public override byte ReadMemory(ushort addr) => ReadMem(addr, false);

		public override byte PeekMemory(ushort addr) => ReadMem(addr, true);

		private byte ReadMem(ushort addr, bool peek)
		{
			if (addr < 0x1000)
			{
				return base.ReadMemory(addr);
			}

			if (!peek)
			{
				Address(addr);
			}

			if (addr < 0x1800)
			{
				if (_enableRam0)
				{
					if (addr < 0x1400) // Reading from the write port
					{
						return _ram[addr & 0x3FF] = 0xFF; // Reading from 1k write port triggers an unwanted write
					}

					return _ram[addr & 0x3FF];
				}

				return Core.Rom[(_romBank1K * 0x800) + (addr & 0x7FF)];
			}

			if (addr < 0x1900) // Ram 1 Write port
			{
				return _ram[RamBank1Offset + (_ramBank1Toggle * 0x100) + (addr & 0xFF)] = 0xFF; // Reading from the 256b write port @1800 riggers an unwanted write
			}

			if (addr < 0x1A00) // Ram 1 Read port
			{
				return _ram[RamBank1Offset + (_ramBank1Toggle * 0x100) + (addr & 0xFF)];
			}

			if (addr < 0x2000)
			{
				addr -= 0x1800;
				addr &= 0x7FF;
				int offset = Core.Rom.Length - 0x0800;
				return Core.Rom[offset + addr]; // Fixed to last 1.5K
			}

			return base.ReadMemory(addr);
		}

		public override void WriteMemory(ushort addr, byte value)
			=> WriteMem(addr, value, false);

		public override void PokeMemory(ushort addr, byte value)
			=> WriteMem(addr, value, true);

		private void WriteMem(ushort addr, byte value, bool poke)
		{
			if (!poke)
			{
				Address(addr);
			}

			if (addr < 0x1000)
			{
				base.WriteMemory(addr, value);
			}
			else if (addr < 0x1400 && _enableRam0)
			{
				_ram[addr & 0x3FF] = value;
			}
			else if (addr >= 0x1800 && addr < 0x1900)
			{
				_ram[RamBank1Offset + (addr & 0xFF) + (_ramBank1Toggle * 0x100)] = value;
			}
		}

		private void Address(ushort addr)
		{
			_enableRam0 = false;
			switch (addr)
			{
				case 0x1FE0:
					_romBank1K = 0;
					break;
				case 0x1FE1:
					_romBank1K = 1;
					break;
				case 0x1FE2:
					_romBank1K = 2;
					break;
				case 0x1FE3:
					_romBank1K = 3;
					break;
				case 0x1FE4:
					_romBank1K = 4;
					break;
				case 0x1FE5:
					_romBank1K = 5;
					break;
				case 0x1FE6:
					_romBank1K = 6;
					break;
				case 0x1FE7:
					_romBank1K = 7;
					_enableRam0 = true;
					break;
				case 0x1FE8:
					_ramBank1Toggle = 0;
					break;
				case 0x1FE9:
					_ramBank1Toggle = 1;
					break;
				case 0x1FEA:
					_ramBank1Toggle = 2;
					break;
				case 0x1FEB:
					_ramBank1Toggle = 3;
					break;
			}
		}
	}
}
