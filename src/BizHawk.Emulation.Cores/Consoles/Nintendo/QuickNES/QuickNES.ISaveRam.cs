﻿using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES
{
	public sealed partial class QuickNES : ISaveRam
	{
		public byte[] CloneSaveRam(bool clearDirty)
		{
			if (!QN.qn_has_battery_ram(Context)) return null;

			LibQuickNES.ThrowStringError(QN.qn_battery_ram_save(Context, _saveRamBuff, _saveRamBuff.Length));
			return (byte[])_saveRamBuff.Clone();
		}

		public void StoreSaveRam(byte[] data)
		{
			if (!QN.qn_has_battery_ram(Context)) return;

			LibQuickNES.ThrowStringError(QN.qn_battery_ram_load(Context, data, data.Length));
		}

		public bool SaveRamModified => QN.qn_has_battery_ram(Context);

		private byte[] _saveRamBuff;

		private void InitSaveRamBuff()
		{
			int size = 0;
			LibQuickNES.ThrowStringError(QN.qn_battery_ram_size(Context, ref size));
			_saveRamBuff = new byte[size];
		}
	}
}
