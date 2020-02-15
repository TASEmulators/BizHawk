using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.GGHawkLink
{
	public partial class GGHawkLink : ITextStatable
	{
		public void SaveStateText(TextWriter writer)
		{
			L.SaveStateText(writer);
			R.SaveStateText(writer);
			SyncState(new Serializer(writer));
		}

		public void LoadStateText(TextReader reader)
		{
			L.LoadStateText(reader);
			R.LoadStateText(reader);
			SyncState(new Serializer(reader));
		}

		public void SaveStateBinary(BinaryWriter bw)
		{
			L.SaveStateBinary(bw);
			R.SaveStateBinary(bw);
			// other variables
			SyncState(new Serializer(bw));
		}

		public void LoadStateBinary(BinaryReader br)
		{
			L.LoadStateBinary(br);
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
