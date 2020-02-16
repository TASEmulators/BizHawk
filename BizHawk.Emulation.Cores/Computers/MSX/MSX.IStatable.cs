using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.MSX
{
	public partial class MSX
	{
		private void SyncState(Serializer ser)
		{
			ser.BeginSection("MSX");

			if (SaveRAM != null)
			{
				ser.Sync(nameof(SaveRAM), ref SaveRAM, false);
			}

			ser.Sync(nameof(SaveRamBank), ref SaveRamBank);

			ser.Sync("Frame", ref _frame);
			ser.Sync("LagCount", ref _lagCount);
			ser.Sync("IsLag", ref _isLag);

			ser.EndSection();

			if (ser.IsReader)
			{
				ser.Sync(nameof(MSX_core), ref MSX_core, false);
				LibMSX.MSX_load_state(MSX_Pntr, MSX_core);
			}
			else
			{
				LibMSX.MSX_save_state(MSX_Pntr, MSX_core);
				ser.Sync(nameof(MSX_core), ref MSX_core, false);
			}
		}
	}
}
