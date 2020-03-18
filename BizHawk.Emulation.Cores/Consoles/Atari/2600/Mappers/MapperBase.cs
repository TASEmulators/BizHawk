using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	public abstract class MapperBase
	{
		protected MapperBase(Atari2600 core)
		{
			Core = core;
		}

		protected readonly Atari2600 Core;

		public virtual byte[] CartRam => new byte[0];

		public virtual byte ReadMemory(ushort addr)
			=> Core.BaseReadMemory(addr);

		public virtual byte PeekMemory(ushort addr)
			=> Core.BasePeekMemory(addr);

		public virtual void WriteMemory(ushort addr, byte value)
			=> Core.BaseWriteMemory(addr, value);

		public virtual void PokeMemory(ushort addr, byte value)
			=> Core.BasePokeMemory(addr, value);

		public virtual void SyncState(Serializer ser)
		{
		}

		public virtual void ClockCpu()
		{
		}

		public abstract void HardReset();

		// This is here purely for mapper 3E because it needs the 13th bit to determine bankswitching (but only receives the first 12 on read memory)
		public bool Bit13 { protected get; set; }
	}
}
