using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	public class MapperBase
	{
		public Atari2600 Core { get; set; }

		public virtual bool HasCartRam
		{
			get { return false; }
		}

		public virtual ByteBuffer CartRam
		{
			get { return new ByteBuffer(0); }
		}

		public virtual byte ReadMemory(ushort addr)
		{
			return Core.BaseReadMemory(addr);
		}

		public virtual byte PeekMemory(ushort addr)
		{
			return Core.BasePeekMemory(addr);
		}

		public virtual void WriteMemory(ushort addr, byte value)
		{
			Core.BaseWriteMemory(addr, value);
		}

		public virtual void PokeMemory(ushort addr, byte value)
		{
			Core.BasePokeMemory(addr, value);
		}

		public virtual void SyncState(Serializer ser) { }

		public virtual void Dispose() { }

		public virtual void ClockCpu() { }

		public virtual void HardReset() { }

		// THis is here purely for mapper 3E because it needs the 13th bit to determine bankswitching (but only receives the first 12 on read memory)
		public bool Bit13 { get; set; }
	}
}
