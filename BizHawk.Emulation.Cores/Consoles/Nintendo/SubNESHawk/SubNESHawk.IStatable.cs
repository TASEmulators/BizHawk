using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SubNESHawk
{
	public partial class SubNESHawk : ITextStatable
	{
		private readonly IStatable _nesStatable;

		public void SaveStateText(TextWriter writer)
		{
			_nesStatable.SaveStateText(writer);
			SyncState(new Serializer(writer));
		}

		public void LoadStateText(TextReader reader)
		{
			_nesStatable.LoadStateText(reader);
			SyncState(new Serializer(reader));
		}

		public void SaveStateBinary(BinaryWriter bw)
		{
			_nesStatable.SaveStateBinary(bw);
			// other variables
			SyncState(new Serializer(bw));
		}

		public void LoadStateBinary(BinaryReader br)
		{
			_nesStatable.LoadStateBinary(br);
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
			ser.Sync(nameof(pass_a_frame), ref pass_a_frame);
			ser.Sync(nameof(reset_frame), ref reset_frame);
			ser.Sync(nameof(pass_new_input), ref pass_new_input);
			ser.Sync(nameof(current_cycle), ref current_cycle);
			ser.Sync(nameof(reset_cycle), ref reset_cycle);
			ser.Sync(nameof(reset_cycle_int), ref reset_cycle_int);
			ser.Sync(nameof(VBL_CNT), ref VBL_CNT);
		}
	}
}
