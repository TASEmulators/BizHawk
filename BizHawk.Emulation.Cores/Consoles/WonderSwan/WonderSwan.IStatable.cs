using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;

using BizHawk.Emulation.Common;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.WonderSwan
{
	partial class WonderSwan: IStatable
	{
		void InitIStatable()
		{
			savebuff = new byte[BizSwan.bizswan_binstatesize(Core)];
			savebuff2 = new byte[savebuff.Length + 13];
		}

		JsonSerializer ser = new JsonSerializer() { Formatting = Formatting.Indented };

		[StructLayout(LayoutKind.Sequential)]
		class TextStateData
		{
			public bool IsLagFrame;
			public int LagCount;
			public int Frame;
		}

		private void LoadTextStateData(TextStateData d)
		{
			IsLagFrame = d.IsLagFrame;
			LagCount = d.LagCount;
			Frame = d.Frame;
		}
		private void SaveTextStateData(TextStateData d)
		{
			d.IsLagFrame = IsLagFrame;
			d.LagCount = LagCount;
			d.Frame = Frame;
		}

		public void SaveStateText(TextWriter writer)
		{
			var s = new TextState<TextStateData>();
			s.Prepare();
			var ff = s.GetFunctionPointersSave();
			BizSwan.bizswan_txtstatesave(Core, ref ff);
			SaveTextStateData(s.ExtraData);
			ser.Serialize(writer, s);
		}

		public void LoadStateText(TextReader reader)
		{
			var s = (TextState<TextStateData>)ser.Deserialize(reader, typeof(TextState<TextStateData>));
			s.Prepare();
			var ff = s.GetFunctionPointersLoad();
			BizSwan.bizswan_txtstateload(Core, ref ff);
			LoadTextStateData(s.ExtraData);
		}

		byte[] savebuff;
		byte[] savebuff2;

		public void SaveStateBinary(BinaryWriter writer)
		{
			if (!BizSwan.bizswan_binstatesave(Core, savebuff, savebuff.Length))
				throw new InvalidOperationException("bizswan_binstatesave() returned false!");
			writer.Write(savebuff.Length);
			writer.Write(savebuff);

			var d = new TextStateData();
			SaveTextStateData(d);
			BinaryQuickSerializer.Write(d, writer);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			int length = reader.ReadInt32();
			if (length != savebuff.Length)
				throw new InvalidOperationException("Save buffer size mismatch!");
			reader.Read(savebuff, 0, length);
			if (!BizSwan.bizswan_binstateload(Core, savebuff, savebuff.Length))
				throw new InvalidOperationException("bizswan_binstateload() returned false!");

			var d = BinaryQuickSerializer.Create<TextStateData>(reader);
			LoadTextStateData(d);
		}

		public byte[] SaveStateBinary()
		{
			var ms = new MemoryStream(savebuff2, true);
			var bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			if (ms.Position != savebuff2.Length)
				throw new InvalidOperationException();
			ms.Close();
			return savebuff2;
		}

		public bool BinarySaveStatesPreferred
		{
			get { return true; }
		}
	}
}
