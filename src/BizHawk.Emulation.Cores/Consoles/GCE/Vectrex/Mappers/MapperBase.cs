using BizHawk.Common;
using BizHawk.Emulation.Cores.Components.MC6809;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	public class MapperBase
	{
		public VectrexHawk Core { get; set; }

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

		public virtual void Mapper_Tick()
		{
		}

		public virtual void RTC_Get(byte value, int index)
		{
		}

		public virtual void MapCDL(ushort addr, MC6809.eCDLogMemFlags flags)
		{
		}
	}
}
