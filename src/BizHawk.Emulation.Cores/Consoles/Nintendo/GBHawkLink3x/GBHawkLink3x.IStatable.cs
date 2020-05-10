using System.IO;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink3x
{
	public partial class GBHawkLink3x : IStatable
	{
		private readonly IStatable _lStates;
		private readonly IStatable _cStates;
		private readonly IStatable _rStates;

		public void SaveStateBinary(BinaryWriter bw)
		{
			_lStates.SaveStateBinary(bw);
			_cStates.SaveStateBinary(bw);
			_rStates.SaveStateBinary(bw);
			// other variables
			SyncState(new Serializer(bw));
		}

		public void LoadStateBinary(BinaryReader br)
		{
			_lStates.LoadStateBinary(br);
			_cStates.LoadStateBinary(br);
			_rStates.LoadStateBinary(br);
			// other variables
			SyncState(new Serializer(br));
		}

		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		private void SyncState(Serializer ser)
		{
			ser.Sync("Lag", ref _lagcount);
			ser.Sync("Frame", ref _frame);
			ser.Sync("IsLag", ref _islag);
			ser.Sync(nameof(_cableconnected_LC), ref _cableconnected_LC);
			ser.Sync(nameof(_cableconnected_CR), ref _cableconnected_CR);
			ser.Sync(nameof(_cableconnected_RL), ref _cableconnected_RL);
			ser.Sync(nameof(do_2_next), ref do_2_next);
			ser.Sync(nameof(L_controller), ref L_controller);
			ser.Sync(nameof(C_controller), ref C_controller);
			ser.Sync(nameof(R_controller), ref R_controller);
			_controllerDeck.SyncState(ser);

			if (ser.IsReader)
			{
				FillVideoBuffer();
			}
		}
	}
}
