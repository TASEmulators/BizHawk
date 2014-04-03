using BizHawk.Common;

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
	Only 256 bytes of RAM is accessable at time, but there are four different 256 byte
	banks making a total of 1K accessable here.

	Accessing 1FE8 through 1FEB select which 256 byte bank shows up.
	*/

	internal class mE7 : MapperBase
	{
		private int _rombank_1K;
		private int _rambank1Toggle;
		private ByteBuffer _rambank0 = new ByteBuffer(1024);
		private ByteBuffer _rambank1 = new ByteBuffer(1024);
		private bool _enableRam0;

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
						return _rambank0[addr & 0x3FF] = 0xFF; // Reading from 1k write port triggers an unwanted write
					}
					
					return _rambank0[addr & 0x3FF];
				}
				
				return core.rom[(_rombank_1K * 0x800) + (addr & 0x7FF)];
			}
			
			if (addr < 0x1900) // Ram 1 Read port
			{
				return _rambank1[(_rambank1Toggle * 0x100) + (addr & 0xFF)];
			}
			
			if (addr < 0x1A00) // Ram 1 Write port
			{
				return _rambank1[(_rambank1Toggle * 0x100) + (addr & 0xFF)] = 0xFF; // Reading from the 256b write port @1800 riggers an unwanted write
			}
			
			if (addr < 0x2000)
			{
				addr -= 0x1800;
				addr &= 0x7FF;
				int offset = core.rom.Length - 0x0800;
				return core.rom[offset + addr]; // Fixed to last 1.5K
			}
			
			return base.ReadMemory(addr);
		}

		public override byte ReadMemory(ushort addr)
		{
			return ReadMem(addr, false);
		}

		public override byte PeekMemory(ushort addr)
		{
			return ReadMem(addr, true);
		}

		public override void WriteMemory(ushort addr, byte value)
		{
			Address(addr);
			if (addr < 0x1000)
			{
				base.WriteMemory(addr, value);
			}
			else if (addr < 0x1400)
			{
				_rambank0[addr & 0x3FF] = value;
			}
			else if (addr >= 0x1800 && addr < 0x2000)
			{
				_rambank1[(addr & 0xFF) + (_rambank1Toggle * 0x100)] = value;
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("toggle", ref _rombank_1K);
			ser.Sync("rambank0", ref _rambank0);
			ser.Sync("rambank1", ref _rambank1);
			ser.Sync("EnableRam0", ref _enableRam0);
			ser.Sync("rambank1_toggle", ref _rambank1Toggle);
		}

		private void Address(ushort addr)
		{
			switch (addr)
			{
				case 0x1FE0:
					_rombank_1K = 0;
					break;
				case 0x1FE1:
					_rombank_1K = 1;
					break;
				case 0x1FE2:
					_rombank_1K = 2;
					break;
				case 0x1FE3:
					_rombank_1K = 3;
					break;
				case 0x1FE4:
					_rombank_1K = 4;
					break;
				case 0x1FE5:
					_rombank_1K = 5;
					break;
				case 0x1FE6:
					_rombank_1K = 6;
					break;
				case 0x1FE7:
					_enableRam0 = true;
					break;
				case 0x1FE8:
					_rambank1Toggle = 0;
					break;
				case 0x1FE9:
					_rambank1Toggle = 1;
					break;
				case 0x1FEA:
					_rambank1Toggle = 2;
					break;
				case 0x1FEB:
					_rambank1Toggle = 3;
					break;
			}
		}
	}
}
