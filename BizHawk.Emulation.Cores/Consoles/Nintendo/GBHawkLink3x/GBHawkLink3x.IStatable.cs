using System.IO;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink3x
{
	public partial class GBHawkLink3x : ITextStatable
	{
		public void SaveStateText(TextWriter writer)
		{
			L.SaveStateText(writer);
			C.SaveStateText(writer);
			R.SaveStateText(writer);
			SyncState(new Serializer(writer));
		}

		public void LoadStateText(TextReader reader)
		{
			L.LoadStateText(reader);
			C.LoadStateText(reader);
			R.LoadStateText(reader);
			SyncState(new Serializer(reader));
		}

		public void SaveStateBinary(BinaryWriter bw)
		{
			L.SaveStateBinary(bw);
			C.SaveStateBinary(bw);
			R.SaveStateBinary(bw);
			// other variables
			SyncState(new Serializer(bw));
		}

		public void LoadStateBinary(BinaryReader br)
		{
			L.LoadStateBinary(br);
			C.LoadStateBinary(br);
			R.LoadStateBinary(br);
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
