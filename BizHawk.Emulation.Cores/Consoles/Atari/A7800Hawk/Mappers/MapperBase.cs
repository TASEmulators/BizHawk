using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	public class MapperBase
	{
		public A7800Hawk Core { get; set; }
		public int mask;

		public virtual byte ReadMemory(ushort addr)
		{
			return 0;
		}

		public virtual byte PeekMemory(ushort addr)
		{
			return 0;
		}

		public virtual void WriteMemory(ushort addr, byte value)
		{
		}

		public virtual void PokeMemory(ushort addr, byte value)
		{
		}

		public virtual void SyncState(Serializer ser)
		{
		}

		public virtual void Dispose()
		{
		}
	}
}
