using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.GGHawkLink
{
	public partial class GGHawkLink : ITextStatable
	{
		private readonly ITextStatable _lStates;
		private readonly ITextStatable _rStates;

		public void SaveStateText(TextWriter writer)
		{
			_lStates.SaveStateText(writer);
			_rStates.SaveStateText(writer);
			SyncState(new Serializer(writer));
		}

		public void LoadStateText(TextReader reader)
		{
			_lStates.LoadStateText(reader);
			_rStates.LoadStateText(reader);
			SyncState(new Serializer(reader));
		}

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
