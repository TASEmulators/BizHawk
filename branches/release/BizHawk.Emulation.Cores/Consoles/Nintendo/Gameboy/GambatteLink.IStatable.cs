using System;
using System.IO;
using Newtonsoft.Json;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class GambatteLink : IStatable
	{
		public bool BinarySaveStatesPreferred { get { return true; } }

		public void SaveStateText(TextWriter writer)
		{
			var s = new DGBSerialized
			{
				L = L.SaveState(),
				R = R.SaveState(),
				IsLagFrame = IsLagFrame,
				LagCount = LagCount,
				Frame = Frame,
				overflowL = overflowL,
				overflowR = overflowR,
				LatchL = LatchL,
				LatchR = LatchR,
				cableconnected = cableconnected,
				cablediscosignal = cablediscosignal
			};
			ser.Serialize(writer, s);
			// write extra copy of stuff we don't use
			// is this needed anymore??
			writer.WriteLine();
			writer.WriteLine("Frame {0}", Frame);
		}

		public void LoadStateText(TextReader reader)
		{
			var s = (DGBSerialized)ser.Deserialize(reader, typeof(DGBSerialized));
			L.LoadState(s.L);
			R.LoadState(s.R);
			IsLagFrame = s.IsLagFrame;
			LagCount = s.LagCount;
			Frame = s.Frame;
			overflowL = s.overflowL;
			overflowR = s.overflowR;
			LatchL = s.LatchL;
			LatchR = s.LatchR;
			cableconnected = s.cableconnected;
			cablediscosignal = s.cablediscosignal;
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			L.SaveStateBinary(writer);
			R.SaveStateBinary(writer);
			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
			writer.Write(overflowL);
			writer.Write(overflowR);
			writer.Write(LatchL);
			writer.Write(LatchR);
			writer.Write(cableconnected);
			writer.Write(cablediscosignal);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			L.LoadStateBinary(reader);
			R.LoadStateBinary(reader);
			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
			overflowL = reader.ReadInt32();
			overflowR = reader.ReadInt32();
			LatchL = reader.ReadInt32();
			LatchR = reader.ReadInt32();
			cableconnected = reader.ReadBoolean();
			cablediscosignal = reader.ReadBoolean();
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
