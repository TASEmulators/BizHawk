using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink
{
	public partial class GBHawkLink : IStatable
	{
		private readonly IStatable _lStates;
		private readonly IStatable _rStates;

		public void SaveStateBinary(BinaryWriter bw)
		{
			_lStates.SaveStateBinary(bw);
			_rStates.SaveStateBinary(bw);
			// other variables
			SyncState(new Serializer(bw));
		}

		public void LoadStateBinary(BinaryReader br)
		{
			_lStates.LoadStateBinary(br);
			_rStates.LoadStateBinary(br);
			// other variables
			SyncState(new Serializer(br));
		}

		private void SyncState(Serializer ser)
		{
			ser.Sync("Lag", ref _lagcount);
			ser.Sync("Frame", ref _frame);
			ser.Sync("IsLag", ref _islag);
			ser.Sync(nameof(_cableconnected), ref _cableconnected);
			ser.Sync(nameof(_cablediscosignal), ref _cablediscosignal);
			ser.Sync(nameof(do_r_next), ref do_r_next);
			ser.Sync(nameof(L_controller), ref L_controller);
			ser.Sync(nameof(R_controller), ref R_controller);
			_controllerDeck.SyncState(ser);

			if (ser.IsReader)
			{
				FillVideoBuffer();
			}
		}
	}
}
