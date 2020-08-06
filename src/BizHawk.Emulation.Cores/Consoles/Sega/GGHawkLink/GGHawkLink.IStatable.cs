using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.GGHawkLink
{
	public partial class GGHawkLink : IStatable
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
			ser.Sync("Lag", ref _lagCount);
			ser.Sync("Frame", ref _frame);
			ser.Sync("IsLag", ref _isLag);
			ser.Sync(nameof(_cableconnected), ref _cableconnected);
			ser.Sync(nameof(_cablediscosignal), ref _cablediscosignal);
			ser.Sync(nameof(do_r_next), ref do_r_next);
			ser.Sync(nameof(L_NMI_CD), ref L_NMI_CD);
			ser.Sync(nameof(L_NMI_CD), ref R_NMI_CD);
			_controllerDeck.SyncState(ser);
		}
	}
}
