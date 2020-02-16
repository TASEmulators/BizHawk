using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink4x
{
	public partial class GBHawkLink4x : IStatable
	{
		private readonly IStatable _aStates;
		private readonly IStatable _bStates;
		private readonly IStatable _cStates;
		private readonly IStatable _dStates;

		public void SaveStateBinary(BinaryWriter bw)
		{
			_aStates.SaveStateBinary(bw);
			_bStates.SaveStateBinary(bw);
			_cStates.SaveStateBinary(bw);
			_dStates.SaveStateBinary(bw);
			// other variables
			SyncState(new Serializer(bw));
		}

		public void LoadStateBinary(BinaryReader br)
		{
			_aStates.LoadStateBinary(br);
			_bStates.LoadStateBinary(br);
			_cStates.LoadStateBinary(br);
			_dStates.LoadStateBinary(br);
			// other variables
			SyncState(new Serializer(br));
		}

		public byte[] SaveStateBinary()
		{
			using var ms = new MemoryStream();
			using var bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		private void SyncState(Serializer ser)
		{
			ser.Sync("Lag", ref _lagcount);
			ser.Sync("Frame", ref _frame);
			ser.Sync("IsLag", ref _islag);
			ser.Sync(nameof(_cableconnected_UD), ref _cableconnected_UD);
			ser.Sync(nameof(_cableconnected_LR), ref _cableconnected_LR);
			ser.Sync(nameof(_cableconnected_X), ref _cableconnected_X);
			ser.Sync(nameof(_cableconnected_4x), ref _cableconnected_4x);
			ser.Sync(nameof(do_2_next), ref do_2_next);
			ser.Sync(nameof(A_controller), ref A_controller);
			ser.Sync(nameof(B_controller), ref B_controller);
			ser.Sync(nameof(C_controller), ref C_controller);
			ser.Sync(nameof(D_controller), ref D_controller);
			_controllerDeck.SyncState(ser);

			if (ser.IsReader)
			{
				FillVideoBuffer();
			}
		}
	}
}
