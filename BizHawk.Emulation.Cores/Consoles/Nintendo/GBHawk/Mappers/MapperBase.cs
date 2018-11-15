using BizHawk.Common;
using System;

using BizHawk.Emulation.Common.Components.LR35902;

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

		public virtual void RTC_Get(byte value, int index)
		{
		}

		public virtual void MapCDL(ushort addr, LR35902.eCDLog_Flags flags)
		{
		}

		protected void SetCDLROM(LR35902.eCDLog_Flags flags, int cdladdr)
		{
			Core.DoCDL2(flags, "ROM", cdladdr);
		}

		protected void SetCDLRAM(LR35902.eCDLog_Flags flags, int cdladdr)
		{
			Core.DoCDL2(flags, "CartRAM", cdladdr);
		}
	}
}
