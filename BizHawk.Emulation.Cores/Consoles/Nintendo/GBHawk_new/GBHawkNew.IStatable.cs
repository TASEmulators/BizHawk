using System.IO;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkNew
{
	public partial class GBHawkNew
	{
		private void SyncState(Serializer ser)
		{
			byte[] core = null;
			if (ser.IsWriter)
			{
				using var ms = new MemoryStream();
				ms.Close();
				core = ms.ToArray();
			}

			ser.Sync("Frame", ref _frame);
			ser.Sync("LagCount", ref _lagCount);

			ser.EndSection();

			if (ser.IsReader)
			{
				ser.Sync(nameof(GB_core), ref GB_core, false);
				LibGBHawk.GB_load_state(GB_Pntr, GB_core);
			}
			else
			{
				LibGBHawk.GB_save_state(GB_Pntr, GB_core);
				ser.Sync(nameof(GB_core), ref GB_core, false);
			}
		}
	}
}
