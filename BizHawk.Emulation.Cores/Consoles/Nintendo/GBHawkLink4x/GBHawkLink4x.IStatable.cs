using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink4x
{
	public partial class GBHawkLink4x : IStatable
	{
		public void SaveStateText(TextWriter writer)
		{
			A.SaveStateText(writer);
			B.SaveStateText(writer);
			C.SaveStateText(writer);
			D.SaveStateText(writer);
			SyncState(new Serializer(writer));
		}

		public void LoadStateText(TextReader reader)
		{
			A.LoadStateText(reader);
			B.LoadStateText(reader);
			C.LoadStateText(reader);
			D.LoadStateText(reader);
			SyncState(new Serializer(reader));
		}

		public void SaveStateBinary(BinaryWriter bw)
		{
			A.SaveStateBinary(bw);
			B.SaveStateBinary(bw);
			C.SaveStateBinary(bw);
			D.SaveStateBinary(bw);
			// other variables
			SyncState(new Serializer(bw));
		}

		public void LoadStateBinary(BinaryReader br)
		{
			A.LoadStateBinary(br);
			B.LoadStateBinary(br);
			C.LoadStateBinary(br);
			D.LoadStateBinary(br);
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

		//private JsonSerializer ser = new JsonSerializer { Formatting = Formatting.Indented };

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
