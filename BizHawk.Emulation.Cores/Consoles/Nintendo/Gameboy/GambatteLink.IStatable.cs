using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class GambatteLink : IStatable
	{
		public void SaveStateBinary(BinaryWriter writer)
		{
			L.SaveStateBinary(writer);
			R.SaveStateBinary(writer);
			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
			writer.Write(_overflowL);
			writer.Write(_overflowR);
			writer.Write(_latchLeft);
			writer.Write(_latchRight);
			writer.Write(_cableconnected);
			writer.Write(_cablediscosignal);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			L.LoadStateBinary(reader);
			R.LoadStateBinary(reader);
			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
			_overflowL = reader.ReadInt32();
			_overflowR = reader.ReadInt32();
			_latchLeft = reader.ReadInt32();
			_latchRight = reader.ReadInt32();
			_cableconnected = reader.ReadBoolean();
			_cablediscosignal = reader.ReadBoolean();
		}

		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}
	}
}
