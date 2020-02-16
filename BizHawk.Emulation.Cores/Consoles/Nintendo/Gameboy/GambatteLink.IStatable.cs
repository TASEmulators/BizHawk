using System;
using System.IO;
using Newtonsoft.Json;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class GambatteLink : ITextStatable
	{
		public void SaveStateText(TextWriter writer)
		{
			var s = new DGBSerialized
			{
				L = L.SaveState(),
				R = R.SaveState(),
				IsLagFrame = IsLagFrame,
				LagCount = LagCount,
				Frame = Frame,
				overflowL = _overflowL,
				overflowR = _overflowR,
				LatchL = _latchLeft,
				LatchR = _latchRight,
				cableconnected = _cableconnected,
				cablediscosignal = _cablediscosignal
			};
			ser.Serialize(writer, s);
		}

		public void LoadStateText(TextReader reader)
		{
			var s = (DGBSerialized)ser.Deserialize(reader, typeof(DGBSerialized));
			L.LoadState(s.L);
			R.LoadState(s.R);
			IsLagFrame = s.IsLagFrame;
			LagCount = s.LagCount;
			Frame = s.Frame;
			_overflowL = s.overflowL;
			_overflowR = s.overflowR;
			_latchLeft = s.LatchL;
			_latchRight = s.LatchR;
			_cableconnected = s.cableconnected;
			_cablediscosignal = s.cablediscosignal;
		}

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

		private JsonSerializer ser = new JsonSerializer { Formatting = Formatting.Indented };

		private class DGBSerialized
		{
			public TextState<Gameboy.TextStateData> L;
			public TextState<Gameboy.TextStateData> R;
			// other data
			public bool IsLagFrame;
			public int LagCount;
			public int Frame;
			public int overflowL;
			public int overflowR;
			public int LatchL;
			public int LatchR;
			public bool cableconnected;
			public bool cablediscosignal;
		}
	}
}
