using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	/*
	UA (UA Ltd)
	-----

	This one was found out later on, lurking on a proto of Pleaides.  It works with 8K of ROM
	and banks it in 4K at a time.

	Accessing 0220 will select the first bank, and accessing 0240 will select the second.
	*/
	internal sealed class mUA : MapperBase 
	{
		private int _toggle;

		public mUA(Atari2600 core) : base(core)
		{
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("toggle", ref _toggle);
		}

		public override void HardReset()
		{
			_toggle = 0;
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
		}

		private void Address(ushort addr)
		{
			if (addr == 0x0220)
			{
				_toggle = 0;
			}
			else if (addr == 0x0240)
			{
				_toggle = 1;
			}
		}
	}
}
