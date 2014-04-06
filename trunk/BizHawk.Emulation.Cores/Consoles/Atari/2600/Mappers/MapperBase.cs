using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	public class MapperBase
	{
		public Atari2600 Core { get; set; }

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

		public virtual void SyncState(Serializer ser) { }

		public virtual void Dispose() { }

		public virtual void ClockCpu() { }

		public virtual void HardReset() { }
	}
}
