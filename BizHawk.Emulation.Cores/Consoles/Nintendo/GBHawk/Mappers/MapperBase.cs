using BizHawk.Common;
using System;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	public class MapperBase
	{
		public GBHawk Core { get; set; }

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

		public virtual void Initialize()
		{
		}

		public virtual void RTC_Tick()
		{
		}

		public virtual void RTC_Get(byte value, int index)
		{
		}
	}
}
