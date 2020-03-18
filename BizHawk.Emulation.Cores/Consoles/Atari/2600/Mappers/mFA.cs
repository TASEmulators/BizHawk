using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	/*
	FA (RAM Plus)
	-----

	CBS Thought they'd throw a few tricks of their own at the 2600 with   It's got
	12K of ROM and 256 bytes of RAM.

	This works similar to F8, except there's only 3 4K ROM banks.  The banks are selected by
	accessing 1FF8, 1FF9, and 1FFA.   There's also 256 bytes of RAM mapped into 1000-11FF.
	The write port is at 1000-10FF, and the read port is 1100-11FF.
	 */
	internal class mFA : MapperBase 
	{
		private int _toggle;
		private byte[] _ram = new byte[256];

		public mFA(Atari2600 core) : base(core)
		{
		}

		public override byte[] CartRam => _ram;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("toggle", ref _toggle);
			ser.Sync("auxRam", ref _ram, false);
		}

		public override void HardReset()
		{
			_toggle = 0;
			_ram = new byte[256];
		}

		public override byte ReadMemory(ushort addr) => ReadMem(addr, false);

		public override byte PeekMemory(ushort addr) => ReadMem(addr, true);

		public override void WriteMemory(ushort addr, byte value)
			=> WriteMem(addr, value, false);

		public override void PokeMemory(ushort addr, byte value)
			=> WriteMem(addr, value, true);	

		private byte ReadMem(ushort addr, bool peek)
		{
			if (!peek)
			{
				Address(addr);
			}

			if (addr < 0x1000)
			{
				return base.ReadMemory(addr);
			}
			
			if (addr < 0x1100)
			{
				return 0xFF;
			}
			
			if (addr < 0x1200)
			{
				return _ram[addr & 0xFF];
			}
			
			return Core.Rom[(_toggle << 12) + (addr & 0xFFF)];
		}

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
			else if (addr < 0x1100)
			{
				_ram[addr & 0xFF] = value;
			}
		}

		private void Address(ushort addr)
		{
			_toggle = addr switch
			{
				0x1FF8 => 0,
				0x1FF9 => 1,
				0x1FFA => 2,
				_ => _toggle
			};
		}
	}
}
