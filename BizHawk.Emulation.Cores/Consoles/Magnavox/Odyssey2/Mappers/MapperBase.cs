using BizHawk.Common;
using BizHawk.Emulation.Cores.Components.I8048;

namespace BizHawk.Emulation.Cores.Consoles.O2Hawk
{
	public class MapperBase
	{
		public O2Hawk Core { get; set; }

		public virtual byte ReadMemory(ushort addr) => 0;

		public virtual void WriteMemory(ushort addr, byte value)
		{
		}

		public virtual void SyncState(Serializer ser)
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

		public virtual void MapCDL(ushort addr, I8048.eCDLogMemFlags flags)
		{
		}

		protected void SetCDLROM(I8048.eCDLogMemFlags flags, int cdladdr)
		{
			Core.SetCDL(flags, "ROM", cdladdr);
		}

		protected void SetCDLRAM(I8048.eCDLogMemFlags flags, int cdladdr)
		{
			Core.SetCDL(flags, "CartRAM", cdladdr);
		}
	}
}
