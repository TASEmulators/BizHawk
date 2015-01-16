using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES
{
	public partial class QuickNES : ISaveRam
	{
		public byte[] CloneSaveRam()
		{
			LibQuickNES.ThrowStringError(LibQuickNES.qn_battery_ram_save(Context, SaveRamBuff, SaveRamBuff.Length));
			return (byte[])SaveRamBuff.Clone();
		}

		public void StoreSaveRam(byte[] data)
		{
			LibQuickNES.ThrowStringError(LibQuickNES.qn_battery_ram_load(Context, data, data.Length));
		}

		public bool SaveRamModified
		{
			get
			{
				return LibQuickNES.qn_has_battery_ram(Context);
			}
		}

		private byte[] SaveRamBuff;

		private void InitSaveRamBuff()
		{
			int size = 0;
			LibQuickNES.ThrowStringError(LibQuickNES.qn_battery_ram_size(Context, ref size));
			SaveRamBuff = new byte[size];
		}
	}
}
