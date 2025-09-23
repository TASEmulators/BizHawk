using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	[CLSCompliant(false)]
	public class MapperBase
	{
		public A7800Hawk Core { get; set; }
		public int mask;

		public virtual byte ReadMemory(ushort addr) => 0;

		public virtual void WriteMemory(ushort addr, byte value)
		{
		}

		public virtual void SyncState(Serializer ser)
		{
		}
	}
}
