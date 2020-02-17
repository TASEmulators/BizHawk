using BizHawk.Common;
using BizHawk.Emulation.Cores.Components.LR35902;

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
