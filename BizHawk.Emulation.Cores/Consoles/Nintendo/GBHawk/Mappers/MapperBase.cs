using BizHawk.Common;
using BizHawk.Emulation.Cores.Components.LR35902;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	public class MapperBase
	{
		public GBHawk Core { get; set; }

		public virtual byte ReadMemoryLow(ushort addr)
		{
			return 0;
		}

		public virtual byte ReadMemoryHigh(ushort addr)
		{
			return 0;
		}

		public virtual byte PeekMemoryLow(ushort addr)
		{
			return 0;
		}

		public virtual byte PeekMemoryHigh(ushort addr)
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

		public virtual void Reset()
		{
		}

		public virtual void Mapper_Tick()
		{
		}

		public virtual void RTC_Get(int value, int index)
		{
		}

		public virtual void MapCDL(ushort addr, LR35902.eCDLogMemFlags flags)
		{
		}

		protected void SetCDLROM(LR35902.eCDLogMemFlags flags, int cdladdr)
		{
			Core.SetCDL(flags, "ROM", cdladdr);
		}

		protected void SetCDLRAM(LR35902.eCDLogMemFlags flags, int cdladdr)
		{
			Core.SetCDL(flags, "CartRAM", cdladdr);
		}
	}
}
